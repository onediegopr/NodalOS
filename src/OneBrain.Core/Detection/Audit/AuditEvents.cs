namespace OneBrain.Core.Detection.Audit;

using OneBrain.Core.Detection.Contracts;

/// <summary>Evento de auditoría para scoring (pre-flight / in-flight).</summary>
public record StateScoringEvent
{
    public string Phase { get; init; } = string.Empty;
    public InteractionState DetectedState { get; init; }
    public double ConfidenceScore { get; init; }
    public string ConfigHash { get; init; } = string.Empty;
    public StateVector Vector { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>Evento de auditoría para decisión de política.</summary>
public record StateDecisionEvent
{
    public StateDecisionType DecisionType { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public StateDetectionResult? DetectionResult { get; init; }
    public string StepId { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string? PreviousEventHash { get; init; }
    public string EventHash { get; init; } = string.Empty;
}
