using System.Threading.Channels;

namespace OneBrain.Core.Runtime;

public sealed record RuntimeSignal(
    string Category,
    string Name,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string? MissionId,
    string? StepId,
    double? Value,
    TimeSpan? Duration,
    IReadOnlyDictionary<string, string> Dimensions)
{
    public static RuntimeSignal Create(
        string category,
        string name,
        string correlationId,
        string? missionId = null,
        string? stepId = null,
        double? value = null,
        TimeSpan? duration = null,
        IEnumerable<KeyValuePair<string, string?>>? dimensions = null) =>
        new(
            Category: SafeRuntimeText.Sanitize(category, 80),
            Name: SafeRuntimeText.Sanitize(name, 120),
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: SafeRuntimeText.Sanitize(correlationId, 120),
            MissionId: string.IsNullOrWhiteSpace(missionId) ? null : SafeRuntimeText.Sanitize(missionId, 120),
            StepId: string.IsNullOrWhiteSpace(stepId) ? null : SafeRuntimeText.Sanitize(stepId, 120),
            Value: value,
            Duration: duration,
            Dimensions: SafeRuntimeText.SanitizeDimensions(dimensions));
}

public interface IRuntimeSignalSink
{
    ValueTask WriteAsync(RuntimeSignal signal, CancellationToken cancellationToken);
}

public interface IRuntimeSignalObserver
{
    void TryObserve(RuntimeSignal signal);
}

public sealed class NullRuntimeSignalObserver : IRuntimeSignalObserver
{
    public static NullRuntimeSignalObserver Instance { get; } = new();

    private NullRuntimeSignalObserver()
    {
    }

    public void TryObserve(RuntimeSignal signal) => ArgumentNullException.ThrowIfNull(signal);
}

public sealed class BestEffortRuntimeSignalObserver : IRuntimeSignalObserver, IAsyncDisposable
{
    private readonly Channel<RuntimeSignal> _channel;
    private readonly IRuntimeSignalSink _sink;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _pump;
    private long _droppedSignals;

    public BestEffortRuntimeSignalObserver(IRuntimeSignalSink sink, int capacity = 256)
    {
        ArgumentNullException.ThrowIfNull(sink);
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _sink = sink;
        _channel = Channel.CreateBounded<RuntimeSignal>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _pump = Task.Run(PumpAsync);
    }

    public long DroppedSignals => Interlocked.Read(ref _droppedSignals);

    public void TryObserve(RuntimeSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        if (!_channel.Writer.TryWrite(signal))
            Interlocked.Increment(ref _droppedSignals);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        _shutdown.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await _pump.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _shutdown.Dispose();
        }
    }

    private async Task PumpAsync()
    {
        await foreach (var signal in _channel.Reader.ReadAllAsync(_shutdown.Token).ConfigureAwait(false))
        {
            try
            {
                await _sink.WriteAsync(signal, _shutdown.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Observability is best-effort and must never stop mission work.
            }
        }
    }
}
