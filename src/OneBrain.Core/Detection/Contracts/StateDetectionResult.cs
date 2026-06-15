namespace OneBrain.Core.Detection.Contracts;

/// <summary>Resultado de detección de estado (Capa 2).</summary>
public record StateDetectionResult
{
    public InteractionState DetectedState { get; init; }
    public double ConfidenceScore { get; init; }
    public StateVector Vector { get; init; } = new();
    public string ScoringConfigHash { get; init; } = string.Empty;
    public string EvidenceRef { get; init; } = string.Empty;
    public DateTimeOffset DetectedAt { get; init; }
}
