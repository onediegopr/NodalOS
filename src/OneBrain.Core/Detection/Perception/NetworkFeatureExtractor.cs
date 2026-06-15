namespace OneBrain.Core.Detection.Perception;

using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Stub del extractor de features de red.
/// Fase A: opera sobre NetworkFeatures inyectadas para tests.
/// Fase B+: reemplazar con listener real de Network.responseReceived.
/// </summary>
public class NetworkFeatureExtractor : INetworkFeatureExtractor
{
    private NetworkFeatures? _injectedFeatures;

    public NetworkFeatureExtractor(NetworkFeatures? injectedFeatures = null)
    {
        _injectedFeatures = injectedFeatures;
    }

    public Task<NetworkFeatures> ExtractAsync(TargetContext ctx, CancellationToken ct = default)
    {
        var result = _injectedFeatures ?? new NetworkFeatures();
        _injectedFeatures = null;
        return Task.FromResult(result);
    }
}
