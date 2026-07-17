using System.Text;
using System.Text.Json;

namespace OneBrain.Pilot;

public sealed class NodalOsLocalDiagnostics
{
    public const string RootEnvironmentVariable = "NODAL_OS_LOCAL_DIAGNOSTICS_ROOT";
    public const string OptInFileName = "opt-in.v1";
    public const string EventsFileName = "events.v1.jsonl";

    private const int SchemaVersion = 1;
    private const long MaximumEventsBytes = 128 * 1024;
    private const int MaximumRetainedEvents = 200;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object _sync = new();
    private readonly string _rootPath;
    private readonly string _optInPath;
    private readonly string _eventsPath;
    private bool _attached;

    public NodalOsLocalDiagnostics(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = Path.GetFullPath(rootPath);
        _optInPath = Path.Combine(_rootPath, OptInFileName);
        _eventsPath = Path.Combine(_rootPath, EventsFileName);
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
            AppendEventLocked("diagnostics", "enabled", "operator-opt-in", packaged);
        }
    }

    public void Disable(bool packaged)
    {
        lock (_sync)
        {
            if (File.Exists(_optInPath))
                AppendEventLocked("diagnostics", "disabled", "operator-opt-out", packaged);
            TryDelete(_optInPath);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            TryDelete(_eventsPath);
        }
    }

    public void RecordStartup(bool packaged) =>
        Record("startup", "ready", "pilot-ready", packaged);

    public void RecordShutdown(bool packaged) =>
        Record("shutdown", "requested", "host-stopping", packaged);

    public void RecordRequestError(Exception exception, bool packaged)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Record("request-error", "failed", ExceptionCode(exception), packaged);
    }

    public void RecordUnhandledError(Exception? exception, bool terminating, bool packaged) =>
        Record("process-error", terminating ? "terminating" : "observed", ExceptionCode(exception), packaged);

    public void Attach(WebApplication app, bool packaged)
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
        });
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
                events.LastOrDefault()?.RecordedAt,
                events.TakeLast(20).Reverse().ToArray());
        }
    }

    private void Record(string kind, string outcome, string code, bool packaged)
    {
        lock (_sync)
        {
            if (!Enabled)
                return;
            AppendEventLocked(kind, outcome, code, packaged);
        }
    }

    private void AppendEventLocked(string kind, string outcome, string code, bool packaged)
    {
        Directory.CreateDirectory(_rootPath);
        var item = new NodalOsLocalDiagnosticEvent(
            SchemaVersion,
            DateTimeOffset.UtcNow,
            SafeToken(kind),
            SafeToken(outcome),
            SafeToken(code),
            packaged,
            typeof(NodalOsLocalDiagnostics).Assembly.GetName().Version?.ToString() ?? "unknown");
        var line = JsonSerializer.Serialize(item, JsonOptions);
        var lineBytes = Utf8NoBom.GetByteCount(line) + 1;

        if (!File.Exists(_eventsPath) || new FileInfo(_eventsPath).Length + lineBytes <= MaximumEventsBytes)
        {
            File.AppendAllText(_eventsPath, line + "\n", Utf8NoBom);
            return;
        }

        var retained = ReadEventsLocked()
            .TakeLast(MaximumRetainedEvents - 1)
            .Append(item)
            .Select(value => JsonSerializer.Serialize(value, JsonOptions))
            .ToArray();
        WriteAtomic(_eventsPath, string.Join('\n', retained) + "\n");
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

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed record NodalOsLocalDiagnosticsSnapshot(
    bool Enabled,
    int EventCount,
    int StartupCount,
    int ErrorCount,
    DateTimeOffset? LastRecordedAt,
    IReadOnlyList<NodalOsLocalDiagnosticEvent> RecentEvents);

internal sealed record NodalOsLocalDiagnosticEvent(
    int SchemaVersion,
    DateTimeOffset RecordedAt,
    string Kind,
    string Outcome,
    string Code,
    bool Packaged,
    string ProductVersion);
