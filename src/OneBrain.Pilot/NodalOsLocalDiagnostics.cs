using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Pilot;

public sealed class NodalOsLocalDiagnostics
{
    public const string RootEnvironmentVariable = "NODAL_OS_LOCAL_DIAGNOSTICS_ROOT";
    public const string OptInFileName = "opt-in.v1";
    public const string EventsFileName = "events.v1.jsonl";
    public const string StartupReadyMetricCode = "startup-ready";
    public const string FirstValueMetricCode = "time-to-first-value";
    public const string MissionCompletionMetricCode = "mission-completion";

    private const int SchemaVersion = 1;
    private const long MaximumEventsBytes = 128 * 1024;
    private const int MaximumRetainedEvents = 200;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object _sync = new();
    private readonly string _rootPath;
    private readonly string _optInPath;
    private readonly string _eventsPath;
    private readonly DateTimeOffset _processStartedAt;
    private readonly HashSet<string> _completedMissionIds = new(StringComparer.Ordinal);
    private bool _attached;
    private bool _firstValueRecorded;

    public NodalOsLocalDiagnostics(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = Path.GetFullPath(rootPath);
        _optInPath = Path.Combine(_rootPath, OptInFileName);
        _eventsPath = Path.Combine(_rootPath, EventsFileName);
        _processStartedAt = Process.GetCurrentProcess().StartTime.ToUniversalTime();
    }

    public bool Enabled => File.Exists(_optInPath);

    public static NodalOsLocalDiagnostics CreateDefault()
    {
        var configuredRoot = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            configuredRoot = Path.Combine(localAppData, "NodalOS", "diagnostics");
        }

        return new NodalOsLocalDiagnostics(configuredRoot);
    }

    public void Enable(bool packaged)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(_rootPath);
            WriteAtomic(_optInPath, "local-redacted-diagnostics-enabled-v1\n");
            TryAppendEventLocked("diagnostics", "enabled", "operator-opt-in", packaged);
        }
    }

    public void Disable(bool packaged)
    {
        lock (_sync)
        {
            var wasEnabled = File.Exists(_optInPath);
            DeleteRequired(_optInPath);
            if (wasEnabled)
                TryAppendEventLocked("diagnostics", "disabled", "operator-opt-out", packaged);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            DeleteRequired(_eventsPath);
        }
    }

    public void RecordStartup(bool packaged) =>
        RecordDuration(
            "startup",
            "ready",
            StartupReadyMetricCode,
            DateTimeOffset.UtcNow - _processStartedAt,
            packaged);

    public void RecordFirstValue(bool packaged)
    {
        lock (_sync)
        {
            if (!Enabled || _firstValueRecorded)
                return;

            _firstValueRecorded = TryAppendEventLocked(
                "product-metric",
                "measured",
                FirstValueMetricCode,
                packaged,
                DurationMilliseconds(DateTimeOffset.UtcNow - _processStartedAt));
        }
    }

    public void RecordMissionCompletion(
        string? missionId,
        DateTimeOffset? createdAt,
        DateTimeOffset? executedAt,
        bool packaged)
    {
        if (string.IsNullOrWhiteSpace(missionId) ||
            createdAt is null ||
            executedAt is null ||
            executedAt < createdAt ||
            executedAt < _processStartedAt)
        {
            return;
        }

        lock (_sync)
        {
            if (!Enabled || !_completedMissionIds.Add(missionId))
                return;

            if (!TryAppendEventLocked(
                    "product-metric",
                    "measured",
                    MissionCompletionMetricCode,
                    packaged,
                    DurationMilliseconds(executedAt.Value - createdAt.Value)))
            {
                _completedMissionIds.Remove(missionId);
            }
        }
    }

    public void RecordShutdown(bool packaged) =>
        Record("shutdown", "requested", "host-stopping", packaged);

    public void RecordRequestError(Exception exception, bool packaged)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Record("request-error", "failed", ExceptionCode(exception), packaged);
    }

    public void RecordUnhandledError(Exception? exception, bool terminating, bool packaged) =>
        Record("process-error", terminating ? "terminating" : "observed", ExceptionCode(exception), packaged);

    public void Attach(
        WebApplication app,
        bool packaged,
        Func<NodalOsWorkspaceMissionDraftService>? missionDraftServiceFactory = null,
        Func<NodalOsWorkspaceHandoffExecutionService>? handoffExecutionServiceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        lock (_sync)
        {
            if (_attached)
                return;
            _attached = true;
        }

        UnhandledExceptionEventHandler unhandled = (_, eventArgs) =>
            RecordUnhandledError(eventArgs.ExceptionObject as Exception, eventArgs.IsTerminating, packaged);
        EventHandler<UnobservedTaskExceptionEventArgs> unobserved = (_, eventArgs) =>
            RecordUnhandledError(eventArgs.Exception, terminating: false, packaged);

        AppDomain.CurrentDomain.UnhandledException += unhandled;
        TaskScheduler.UnobservedTaskException += unobserved;

        app.Lifetime.ApplicationStarted.Register(() => RecordStartup(packaged));
        app.Lifetime.ApplicationStopping.Register(() => RecordShutdown(packaged));
        app.Lifetime.ApplicationStopped.Register(() =>
        {
            AppDomain.CurrentDomain.UnhandledException -= unhandled;
            TaskScheduler.UnobservedTaskException -= unobserved;
        });

        app.Use(async (context, next) =>
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                RecordRequestError(exception, packaged);
                throw;
            }

            await TryRecordProductMetricAsync(
                    context,
                    packaged,
                    missionDraftServiceFactory,
                    handoffExecutionServiceFactory)
                .ConfigureAwait(false);
        });
    }

    public NodalOsLocalMetricsSnapshot ReadMetrics()
    {
        lock (_sync)
        {
            var events = ReadEventsLocked();
            return new NodalOsLocalMetricsSnapshot(
                events.Count(value => value.Code == MissionCompletionMetricCode),
                LatestDuration(events, StartupReadyMetricCode),
                LatestDuration(events, FirstValueMetricCode),
                LatestDuration(events, MissionCompletionMetricCode));
        }
    }

    internal NodalOsLocalDiagnosticsSnapshot ReadSnapshot()
    {
        lock (_sync)
        {
            var events = ReadEventsLocked();
            return new NodalOsLocalDiagnosticsSnapshot(
                Enabled,
                events.Count,
                events.Count(value => value.Kind == "startup"),
                events.Count(value => value.Kind is "request-error" or "process-error"),
                events.Count(value => value.Code == MissionCompletionMetricCode),
                LatestDuration(events, StartupReadyMetricCode),
                LatestDuration(events, FirstValueMetricCode),
                LatestDuration(events, MissionCompletionMetricCode),
                events.LastOrDefault()?.RecordedAt,
                events.TakeLast(20).Reverse().ToArray());
        }
    }

    private async ValueTask TryRecordProductMetricAsync(
        HttpContext context,
        bool packaged,
        Func<NodalOsWorkspaceMissionDraftService>? missionDraftServiceFactory,
        Func<NodalOsWorkspaceHandoffExecutionService>? handoffExecutionServiceFactory)
    {
        if (!Enabled || context.Response.StatusCode >= StatusCodes.Status400BadRequest)
            return;

        var path = context.Request.Path.Value;
        if (HttpMethods.IsGet(context.Request.Method) &&
            context.Response.StatusCode == StatusCodes.Status200OK &&
            string.Equals(path, MissionControlProductHandoffExportEndpointMapper.MarkdownRoute, StringComparison.Ordinal))
        {
            RecordFirstValue(packaged);
            return;
        }

        if (!HttpMethods.IsPost(context.Request.Method) ||
            context.Response.StatusCode is < StatusCodes.Status300MultipleChoices or >= StatusCodes.Status400BadRequest ||
            !string.Equals(path, RealWorkspaceHandoffExecutionEndpointMapper.HtmlRoute, StringComparison.Ordinal) ||
            missionDraftServiceFactory is null ||
            handoffExecutionServiceFactory is null)
        {
            return;
        }

        try
        {
            var execution = await handoffExecutionServiceFactory()
                .GetCurrentAsync(CancellationToken.None)
                .ConfigureAwait(false);
            if (!execution.Accepted ||
                execution.State != NodalOsWorkspaceHandoffExecutionState.Completed ||
                !execution.Executed ||
                !execution.Verified ||
                execution.ExecutedAt is null)
            {
                return;
            }

            var mission = await missionDraftServiceFactory()
                .GetCurrentAsync(CancellationToken.None)
                .ConfigureAwait(false);
            RecordMissionCompletion(mission.MissionId, mission.CreatedAt, execution.ExecutedAt, packaged);
        }
        catch
        {
            // Product metrics are best-effort and may never alter the completed product response.
        }
    }

    private void Record(string kind, string outcome, string code, bool packaged)
    {
        lock (_sync)
        {
            if (!Enabled)
                return;
            TryAppendEventLocked(kind, outcome, code, packaged);
        }
    }

    private void RecordDuration(
        string kind,
        string outcome,
        string code,
        TimeSpan duration,
        bool packaged)
    {
        lock (_sync)
        {
            if (!Enabled)
                return;
            TryAppendEventLocked(kind, outcome, code, packaged, DurationMilliseconds(duration));
        }
    }

    private bool TryAppendEventLocked(
        string kind,
        string outcome,
        string code,
        bool packaged,
        long? durationMilliseconds = null)
    {
        try
        {
            AppendEventLocked(kind, outcome, code, packaged, durationMilliseconds);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private void AppendEventLocked(
        string kind,
        string outcome,
        string code,
        bool packaged,
        long? durationMilliseconds)
    {
        Directory.CreateDirectory(_rootPath);
        var item = new NodalOsLocalDiagnosticEvent(
            SchemaVersion,
            DateTimeOffset.UtcNow,
            SafeToken(kind),
            SafeToken(outcome),
            SafeToken(code),
            packaged,
            typeof(NodalOsLocalDiagnostics).Assembly.GetName().Version?.ToString() ?? "unknown",
            durationMilliseconds);
        var line = JsonSerializer.Serialize(item, JsonOptions);
        var lineBytes = Utf8NoBom.GetByteCount(line) + 1;
        var retained = ReadEventsLocked();
        var currentBytes = File.Exists(_eventsPath) ? new FileInfo(_eventsPath).Length : 0;

        if (retained.Count < MaximumRetainedEvents && currentBytes + lineBytes <= MaximumEventsBytes)
        {
            File.AppendAllText(_eventsPath, line + "\n", Utf8NoBom);
            return;
        }

        var bounded = retained
            .TakeLast(MaximumRetainedEvents - 1)
            .Append(item)
            .Select(value => JsonSerializer.Serialize(value, JsonOptions))
            .ToArray();
        WriteAtomic(_eventsPath, string.Join('\n', bounded) + "\n");
    }

    private IReadOnlyList<NodalOsLocalDiagnosticEvent> ReadEventsLocked()
    {
        if (!File.Exists(_eventsPath))
            return [];

        try
        {
            var info = new FileInfo(_eventsPath);
            if (info.Length is <= 0 or > MaximumEventsBytes * 2)
                return [];

            var events = new List<NodalOsLocalDiagnosticEvent>();
            foreach (var line in File.ReadLines(_eventsPath, Utf8NoBom).TakeLast(MaximumRetainedEvents))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                try
                {
                    var item = JsonSerializer.Deserialize<NodalOsLocalDiagnosticEvent>(line, JsonOptions);
                    if (item is not null && item.SchemaVersion == SchemaVersion)
                        events.Add(item);
                }
                catch (JsonException)
                {
                }
            }
            return events;
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static long? LatestDuration(
        IReadOnlyList<NodalOsLocalDiagnosticEvent> events,
        string code) =>
        events.LastOrDefault(value => value.Code == code)?.DurationMilliseconds;

    private static long DurationMilliseconds(TimeSpan duration) =>
        Math.Max(0, (long)Math.Round(duration.TotalMilliseconds, MidpointRounding.AwayFromZero));

    private static string ExceptionCode(Exception? exception) =>
        SafeToken(exception?.GetBaseException().GetType().Name ?? "unknown-exception");

    private static string SafeToken(string? value)
    {
        var token = new string((value ?? string.Empty)
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
            .Take(96)
            .ToArray());
        return string.IsNullOrWhiteSpace(token) ? "unknown" : token;
    }

    private static void WriteAtomic(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Local diagnostics directory is unavailable.");
        Directory.CreateDirectory(directory);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, content, Utf8NoBom);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void DeleteRequired(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void TryDelete(string path)
    {
        try
        {
            DeleteRequired(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

public sealed record NodalOsLocalMetricsSnapshot(
    int MissionCompletionCount,
    long? StartupMilliseconds,
    long? FirstValueMilliseconds,
    long? MissionCompletionMilliseconds);

internal sealed record NodalOsLocalDiagnosticsSnapshot(
    bool Enabled,
    int EventCount,
    int StartupCount,
    int ErrorCount,
    int MissionCompletionCount,
    long? StartupMilliseconds,
    long? FirstValueMilliseconds,
    long? MissionCompletionMilliseconds,
    DateTimeOffset? LastRecordedAt,
    IReadOnlyList<NodalOsLocalDiagnosticEvent> RecentEvents);

internal sealed record NodalOsLocalDiagnosticEvent(
    int SchemaVersion,
    DateTimeOffset RecordedAt,
    string Kind,
    string Outcome,
    string Code,
    bool Packaged,
    string ProductVersion,
    long? DurationMilliseconds);
