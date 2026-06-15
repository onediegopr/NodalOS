namespace OneBrain.Core.Detection.Contracts;

public interface INetworkFeatureExtractor
{
    Task<NetworkFeatures> ExtractAsync(TargetContext ctx, CancellationToken ct = default);
}
