namespace OneBrain.Core.Detection.Perception;

using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Stub del analizador estructural CDP.
/// Fase A: opera sobre StructuralFeatures inyectadas para tests.
/// Fase B+: reemplazar con implementación DOM.getDocument + querySelectorAll real.
/// </summary>
public class CdpStructuralAnalyzer : ICdpStructuralAnalyzer
{
    private readonly StructuralFeatures? _injectedFeatures;

    public CdpStructuralAnalyzer(StructuralFeatures? injectedFeatures = null)
    {
        _injectedFeatures = injectedFeatures;
    }

    public Task<StructuralFeatures> AnalyzeAsync(TargetContext ctx, CancellationToken ct = default)
    {
        return Task.FromResult(_injectedFeatures ?? new StructuralFeatures());
    }
}
