namespace OneBrain.Core.Detection.Perception;

using System.Collections.Concurrent;
using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Stub del extractor de features de red.
/// Fase A: opera sobre NetworkFeatures inyectadas para tests.
/// Fase B+: reemplazar con listener real de Network.responseReceived.
/// </summary>
public sealed class NetworkFeatureExtractor : INetworkFeatureExtractor
{
    private readonly ConcurrentQueue<NetworkFeatures> _queued = new();
    private volatile NetworkFeatures _last = new();

    public NetworkFeatureExtractor(NetworkFeatures? injectedFeatures = null)
    {
        if (injectedFeatures is not null)
        {
            _queued.Enqueue(injectedFeatures);
            _last = injectedFeatures;
        }
    }

    public void Enqueue(NetworkFeatures features) =>
        _queued.Enqueue(features ?? new NetworkFeatures());

    public Task<NetworkFeatures> ExtractAsync(TargetContext ctx, CancellationToken ct = default)
    {
        // BUG M-3: lectura thread-safe e idempotente. Si hay features encoladas se consume una
        // (drenaje atómico vía ConcurrentQueue); si no, se mantiene la última conocida en vez de
        // borrar la señal o corromperse en lecturas concurrentes.
        if (_queued.TryDequeue(out var next))
        {
            _last = next;
            return Task.FromResult(next);
        }

        return Task.FromResult(_last);
    }
}
