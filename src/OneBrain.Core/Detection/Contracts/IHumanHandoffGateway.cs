namespace OneBrain.Core.Detection.Contracts;

public enum HandoffResult
{
    ResolvedByHuman,
    AbortedByUser,
    Timeout
}

public record StateHandoffRequest
{
    public string RecipeStepId { get; init; } = string.Empty;
    public string TargetDescription { get; init; } = string.Empty;
    public InteractionState DetectedState { get; init; }
    public double DetectionConfidence { get; init; }
    public string DetectorVersion { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = string.Empty;
    public string RedactedSnapshotRef { get; init; } = string.Empty;
    public string OperatorInstruction { get; init; } = string.Empty;
    public int TimeoutMinutes { get; init; } = 5;
}

public interface IHumanHandoffGateway
{
    Task<HandoffResult> RequestInterventionAsync(StateHandoffRequest request, CancellationToken ct = default);
}
