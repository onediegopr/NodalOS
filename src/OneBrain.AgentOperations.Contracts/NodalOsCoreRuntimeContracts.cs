namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsExecutionRegistryState
{
    Created,
    Registered,
    PolicyEvaluated,
    ApprovalRequired,
    Approved,
    Rejected,
    DryRunPlanned,
    ExecutionSkipped,
    HumanHandoffRequired,
    Completed,
    Failed,
    Cancelled
}

public enum NodalOsExecutionActorKind
{
    Unknown,
    HumanOperator,
    MissionControl,
    PolicyGate,
    ApprovalCenter,
    OrchestrationFacade,
    ScheduledReadOnly,
    AutomationLayer
}

public enum NodalOsExecutionSourceKind
{
    Unknown,
    MissionControl,
    OrchestrationCommand,
    OrchestrationFacade,
    ScheduledReadOnly,
    AutomationLayer,
    Manual
}

public enum NodalOsCoreEventKind
{
    ExecutionRequestRegistered,
    PolicyGateEvaluated,
    ApprovalRequired,
    ApprovalGranted,
    ApprovalRejected,
    DryRunPlanCreated,
    ExecutionCompleted,
    ExecutionFailed,
    EvidenceAttached,
    WarningRaised,
    HumanHandoffRequired,
    RedactionApplied
}

public sealed record NodalOsExecutionRequest
{
    public required string RequestId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RunId { get; init; }

    public string? RecipeId { get; init; }

    public string? SkillId { get; init; }

    public required string RequestedBy { get; init; }

    public required NodalOsExecutionActorKind ActorKind { get; init; }

    public required NodalOsExecutionSourceKind SourceKind { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsExecutionRegistryTransition
{
    public required string TransitionId { get; init; }

    public required NodalOsExecutionRegistryState FromState { get; init; }

    public required NodalOsExecutionRegistryState ToState { get; init; }

    public required string Actor { get; init; }

    public string? ReasonRedacted { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsExecutionRegistryEntry
{
    public required string RegistryEntryId { get; init; }

    public required string RequestId { get; init; }

    public required NodalOsExecutionRegistryState State { get; init; }

    public required NodalOsExecutionActorKind ActorKind { get; init; }

    public required NodalOsExecutionSourceKind SourceKind { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public string? PolicyDecisionRef { get; init; }

    public string? ApprovalRef { get; init; }

    public string? DryRunRef { get; init; }

    public string? VerificationReportRef { get; init; }

    public IReadOnlyList<string> SnapshotRefs { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public string? FailureReasonRedacted { get; init; }

    public IReadOnlyList<NodalOsExecutionRegistryTransition> Transitions { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsCoreEvent
{
    public required string EventId { get; init; }

    public required NodalOsCoreEventKind Kind { get; init; }

    public string? ExecutionRegistryEntryId { get; init; }

    public string? ExecutionRequestId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public IReadOnlyDictionary<string, string> MetadataRedacted { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public string? HumanSummaryRedacted { get; init; }

    public string? TechnicalSummaryRedacted { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsCoreTimelineProjection
{
    public required string ProjectionId { get; init; }

    public required string EventId { get; init; }

    public string? ExecutionRegistryEntryId { get; init; }

    public required NodalOsCoreEventKind Kind { get; init; }

    public required string SummaryRedacted { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsCoreRuntimeValidationResult
{
    public required bool IsValid { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
