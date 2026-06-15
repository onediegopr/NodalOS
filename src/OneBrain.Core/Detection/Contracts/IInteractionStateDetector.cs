namespace OneBrain.Core.Detection.Contracts;

/// <summary>
/// Detector de estados de interacción (sensor puro).
/// No decide, no resuelve, no evade. Solo percibe.
/// </summary>
public interface IInteractionStateDetector
{
    Task<StateDetectionResult> AssessPreFlightAsync(TargetContext ctx, CancellationToken ct = default);
    Task<StateDetectionResult> AssessInFlightAsync(TargetContext ctx, TimeSpan timeout, CancellationToken ct = default);
}

/// <summary>Contexto mínimo del target para evaluación de estado.</summary>
public record TargetContext
{
    public string? Url { get; init; }
    public string? TargetSelector { get; init; }
    public string? FrameId { get; init; }
    public bool UseAccessibilityTree { get; init; }
    public int Generation { get; init; }
}
