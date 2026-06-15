namespace OneBrain.Core.Detection.Contracts;

public interface ICdpStructuralAnalyzer
{
    Task<StructuralFeatures> AnalyzeAsync(TargetContext ctx, CancellationToken ct = default);
}
