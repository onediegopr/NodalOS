using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Channels;
using OneBrain.Core.Runtime;

namespace OneBrain.BrowserRuntime.Reliability;

public enum BrowserBridgeConnectionState
{
    Disconnected,
    Connecting,
    Ready,
    Degraded,
    Reconnecting,
    Stopped
}

public sealed record BrowserReconnectPolicy(
    int MaximumAttempts,
    TimeSpan InitialDelay,
    TimeSpan MaximumDelay,
    double Multiplier,
    TimeSpan MaximumJitter)
{
    public static BrowserReconnectPolicy Default { get; } = new(
        5,
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromSeconds(5),
        2,
        TimeSpan.FromMilliseconds(120));

    public TimeSpan DelayForAttempt(int attempt, Random? random = null)
    {
        if (MaximumAttempts < 1 || InitialDelay <= TimeSpan.Zero || MaximumDelay < InitialDelay ||
            Multiplier < 1 || MaximumJitter < TimeSpan.Zero)
            throw new InvalidOperationException("Reconnect policy is invalid.");
        if (attempt < 1 || attempt > MaximumAttempts)
            throw new ArgumentOutOfRangeException(nameof(attempt));

        var rawMilliseconds = InitialDelay.TotalMilliseconds * Math.Pow(Multiplier, attempt - 1);
        var boundedMilliseconds = Math.Min(rawMilliseconds, MaximumDelay.TotalMilliseconds);
        var jitterMilliseconds = MaximumJitter == TimeSpan.Zero
            ? 0
            : (random ?? Random.Shared).NextDouble() * MaximumJitter.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(boundedMilliseconds + jitterMilliseconds);
    }
}

public sealed class BrowserConnectionHealthTracker
{
    private readonly object _gate = new();
    private BrowserBridgeConnectionState _state = BrowserBridgeConnectionState.Disconnected;
    private int _reconnectCount;
    private string? _lastError;
    private DateTimeOffset? _lastChangedAt;

    public BrowserConnectionHealthSnapshot Snapshot()
    {
        lock (_gate)
            return new BrowserConnectionHealthSnapshot(_state, _reconnectCount, _lastError, _lastChangedAt);
    }

    public void Mark(BrowserBridgeConnectionState state, string? error = null)
    {
        lock (_gate)
        {
            if (state == BrowserBridgeConnectionState.Reconnecting)
                _reconnectCount++;
            if (state == BrowserBridgeConnectionState.Ready)
                _lastError = null;
            else if (!string.IsNullOrWhiteSpace(error))
                _lastError = SafeRuntimeText.Sanitize(error, 240);
            _state = state;
            _lastChangedAt = DateTimeOffset.UtcNow;
        }
    }
}

public sealed record BrowserConnectionHealthSnapshot(
    BrowserBridgeConnectionState State,
    int ReconnectCount,
    string? LastError,
    DateTimeOffset? LastChangedAt);

public sealed record BrowserCommandRequest(
    string RequestId,
    string CorrelationId,
    string Command,
    int MaximumResponseBytes,
    TimeSpan Timeout);

public sealed record BrowserCommandResponse(
    string RequestId,
    string CorrelationId,
    bool Success,
    string Payload,
    string? Error);

public sealed record BrowserCommandWaitResult(
    bool Success,
    bool TimedOut,
    bool Cancelled,
    BrowserCommandResponse? Response,
    string Decision,
    string SafeMessage);

public sealed class PendingBrowserCommandRegistry
{
    private sealed record Pending(
        BrowserCommandRequest Request,
        TaskCompletionSource<BrowserCommandResponse> Completion);

    private readonly ConcurrentDictionary<string, Pending> _pending = new(StringComparer.Ordinal);
    private readonly HashSet<string> _allowedCommands;

    public PendingBrowserCommandRegistry(IEnumerable<string> allowedCommands)
    {
        ArgumentNullException.ThrowIfNull(allowedCommands);
        _allowedCommands = new HashSet<string>(
            allowedCommands.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()),
            StringComparer.Ordinal);
        if (_allowedCommands.Count == 0)
            throw new ArgumentException("At least one allowed browser command is required.", nameof(allowedCommands));
    }

    public BrowserCommandRequest Begin(
        string requestId,
        string correlationId,
        string command,
        TimeSpan timeout,
        int maximumResponseBytes = 1024 * 1024)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        if (!_allowedCommands.Contains(command))
            throw new InvalidOperationException($"Browser command '{command}' is not allowed.");
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        if (maximumResponseBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));

        var request = new BrowserCommandRequest(
            SafeRuntimeText.Sanitize(requestId, 120),
            SafeRuntimeText.Sanitize(correlationId, 120),
            command.Trim(),
            maximumResponseBytes,
            timeout);
        var pending = new Pending(
            request,
            new TaskCompletionSource<BrowserCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously));
        if (!_pending.TryAdd(request.RequestId, pending))
            throw new InvalidOperationException("A browser request with the same id is already pending.");
        return request;
    }

    public bool TryComplete(BrowserCommandResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (!_pending.TryGetValue(response.RequestId, out var pending))
            return false;
        if (!string.Equals(pending.Request.CorrelationId, response.CorrelationId, StringComparison.Ordinal))
            return false;
        if (System.Text.Encoding.UTF8.GetByteCount(response.Payload ?? string.Empty) > pending.Request.MaximumResponseBytes)
        {
            return pending.Completion.TrySetResult(new BrowserCommandResponse(
                pending.Request.RequestId,
                pending.Request.CorrelationId,
                false,
                string.Empty,
                "Browser response exceeded the configured payload limit."));
        }

        return pending.Completion.TrySetResult(response with
        {
            Payload = response.Payload ?? string.Empty,
            Error = string.IsNullOrWhiteSpace(response.Error) ? null : SafeRuntimeText.Sanitize(response.Error, 240)
        });
    }

    public async ValueTask<BrowserCommandWaitResult> WaitAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        if (!_pending.TryGetValue(requestId, out var pending))
            throw new KeyNotFoundException("Browser request is not pending.");

        try
        {
            var response = await pending.Completion.Task
                .WaitAsync(pending.Request.Timeout, cancellationToken)
                .ConfigureAwait(false);
            return new BrowserCommandWaitResult(
                response.Success,
                false,
                false,
                response,
                response.Success ? "BROWSER_COMMAND_COMPLETED" : "BROWSER_COMMAND_FAILED",
                response.Success ? "Browser command completed." : response.Error ?? "Browser command failed.");
        }
        catch (TimeoutException)
        {
            return new BrowserCommandWaitResult(
                false,
                true,
                false,
                null,
                "BROWSER_COMMAND_TIMEOUT",
                "Browser command timed out.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new BrowserCommandWaitResult(
                false,
                false,
                true,
                null,
                "BROWSER_COMMAND_CANCELLED",
                "Browser command was cancelled.");
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    public int PendingCount => _pending.Count;
}

public sealed record BrowserFrameDescriptor(
    string FrameId,
    string? ParentFrameId,
    Uri Url,
    double OffsetX,
    double OffsetY,
    bool Attached,
    bool Accessible);

public sealed record BrowserFrameElementCandidate(
    string FrameId,
    string Selector,
    double LocalX,
    double LocalY,
    double Width,
    double Height);

public sealed record BrowserGlobalElementTarget(
    string FrameId,
    string Selector,
    double GlobalX,
    double GlobalY,
    double Width,
    double Height,
    IReadOnlyList<string> FramePath);

public static class BrowserFrameLocator
{
    public static BrowserGlobalElementTarget ResolveGlobalTarget(
        IReadOnlyCollection<BrowserFrameDescriptor> frames,
        BrowserFrameElementCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(candidate);
        var byId = frames.ToDictionary(frame => frame.FrameId, StringComparer.Ordinal);
        if (!byId.TryGetValue(candidate.FrameId, out var current))
            throw new InvalidOperationException("Element references an unknown frame.");

        var x = candidate.LocalX;
        var y = candidate.LocalY;
        var path = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            if (!visited.Add(current.FrameId))
                throw new InvalidOperationException("Browser frame tree contains a cycle.");
            if (!current.Attached)
                throw new InvalidOperationException("Browser frame detached before target resolution.");
            if (!current.Accessible)
                throw new InvalidOperationException("Browser frame is not accessible within the authorized bridge scope.");

            path.Add(current.FrameId);
            x += current.OffsetX;
            y += current.OffsetY;
            if (string.IsNullOrWhiteSpace(current.ParentFrameId))
                break;
            if (!byId.TryGetValue(current.ParentFrameId, out var parent))
                throw new InvalidOperationException("Browser frame parent is missing.");
            current = parent;
        }

        path.Reverse();
        return new BrowserGlobalElementTarget(
            candidate.FrameId,
            SafeRuntimeText.Sanitize(candidate.Selector, 240),
            x,
            y,
            candidate.Width,
            candidate.Height,
            path);
    }

    public static IReadOnlyList<BrowserFrameDescriptor> SearchOrder(IReadOnlyCollection<BrowserFrameDescriptor> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var byId = frames.ToDictionary(frame => frame.FrameId, StringComparer.Ordinal);
        return frames
            .Where(frame => frame.Attached)
            .OrderBy(frame => Depth(frame, byId))
            .ThenBy(frame => frame.FrameId, StringComparer.Ordinal)
            .ToArray();
    }

    private static int Depth(
        BrowserFrameDescriptor frame,
        IReadOnlyDictionary<string, BrowserFrameDescriptor> frames)
    {
        var depth = 0;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = frame;
        while (!string.IsNullOrWhiteSpace(current.ParentFrameId))
        {
            if (!visited.Add(current.FrameId) || !frames.TryGetValue(current.ParentFrameId, out var parent))
                return int.MaxValue;
            current = parent;
            depth++;
        }
        return depth;
    }
}

public sealed record BrowserElementProbeResult(
    bool Found,
    BrowserFrameElementCandidate? Candidate,
    string? LastError);

public sealed record BrowserWaitForElementResult(
    bool Found,
    bool TimedOut,
    bool Cancelled,
    int Attempts,
    BrowserFrameElementCandidate? Candidate,
    string? LastError,
    string Decision);

public static class BrowserElementWaiter
{
    public static async ValueTask<BrowserWaitForElementResult> WaitAsync(
        Func<CancellationToken, ValueTask<BrowserElementProbeResult>> probe,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken = default,
        Func<TimeSpan, CancellationToken, ValueTask>? delay = null)
    {
        ArgumentNullException.ThrowIfNull(probe);
        if (timeout <= TimeSpan.Zero || pollInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        delay ??= static (duration, token) => new ValueTask(Task.Delay(duration, token));
        var started = Stopwatch.GetTimestamp();
        var attempts = 0;
        string? lastError = null;

        try
        {
            while (Stopwatch.GetElapsedTime(started) < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempts++;
                BrowserElementProbeResult result;
                try
                {
                    result = await probe(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result = new BrowserElementProbeResult(false, null, SafeRuntimeText.Sanitize(ex.Message, 240));
                }

                if (result.Found && result.Candidate is not null)
                {
                    return new BrowserWaitForElementResult(
                        true,
                        false,
                        false,
                        attempts,
                        result.Candidate,
                        null,
                        "BROWSER_ELEMENT_FOUND");
                }

                lastError = string.IsNullOrWhiteSpace(result.LastError)
                    ? lastError
                    : SafeRuntimeText.Sanitize(result.LastError, 240);
                var elapsed = Stopwatch.GetElapsedTime(started);
                var remaining = timeout - elapsed;
                if (remaining <= TimeSpan.Zero)
                    break;
                await delay(remaining < pollInterval ? remaining : pollInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new BrowserWaitForElementResult(
                false,
                false,
                true,
                attempts,
                null,
                lastError,
                "BROWSER_ELEMENT_WAIT_CANCELLED");
        }

        return new BrowserWaitForElementResult(
            false,
            true,
            false,
            attempts,
            null,
            lastError,
            "BROWSER_ELEMENT_WAIT_TIMEOUT");
    }
}

public sealed record BrowserControlMessage(string Kind, string CorrelationId, string Payload);

public sealed record BrowserDataMessage(string Kind, string CorrelationId, ReadOnlyMemory<byte> Payload);

public sealed class BrowserTransportChannels
{
    private readonly Channel<BrowserControlMessage> _control;
    private readonly Channel<BrowserDataMessage> _data;

    public BrowserTransportChannels(int controlCapacity = 64, int dataCapacity = 8)
    {
        if (controlCapacity < 1 || dataCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(controlCapacity));
        _control = Channel.CreateBounded<BrowserControlMessage>(new BoundedChannelOptions(controlCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
        _data = Channel.CreateBounded<BrowserDataMessage>(new BoundedChannelOptions(dataCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ValueTask WriteControlAsync(BrowserControlMessage message, CancellationToken cancellationToken = default) =>
        _control.Writer.WriteAsync(message, cancellationToken);

    public bool TryWriteData(BrowserDataMessage message) => _data.Writer.TryWrite(message);

    public ValueTask<BrowserControlMessage> ReadControlAsync(CancellationToken cancellationToken = default) =>
        _control.Reader.ReadAsync(cancellationToken);

    public ValueTask<BrowserDataMessage> ReadDataAsync(CancellationToken cancellationToken = default) =>
        _data.Reader.ReadAsync(cancellationToken);
}
