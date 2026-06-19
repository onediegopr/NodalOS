namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsApprovalStatus
{
    Draft,
    PendingHumanDecision,
    Approved,
    Rejected,
    ChangesRequested,
    ExplanationRequested,
    Deferred,
    HumanHandoffRequired,
    Cancelled
}

public enum NodalOsApprovalSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum NodalOsApprovalActionKind
{
    Observation,
    DryRun,
    ReadOnlyReview,
    FormFillFuture,
    SubmitFuture,
    ExternalMutationFuture,
    CredentialUseFuture,
    DestructiveActionFuture,
    HumanHandoff
}

public enum NodalOsApprovalDecisionKind
{
    Approve,
    Reject,
    RequestChanges,
    RequestExplanation,
    Defer,
    HumanHandoffRequired
}

public enum NodalOsApprovalUserOptionKind
{
    Approve,
    Reject,
    RequestChanges,
    RequestExplanation,
    Defer,
    Pause,
    CopyTechnicalLog,
    HumanHandoffRequired
}

public enum NodalOsTimelineEntrySeverity
{
    Info,
    Success,
    Warning,
    Error,
    Critical
}

public enum NodalOsTimelineEntryStatus
{
    Recorded,
    RequiresAttention,
    Completed,
    Failed,
    Blocked,
    Redacted
}

public enum NodalOsEvidenceAttachmentKind
{
    RegistryEntry,
    CoreEvent,
    ApprovalCard,
    TimelineEntry
}

public sealed record NodalOsApprovalCard
{
    public required string ApprovalCardId { get; init; }

    public string? ApprovalRequestId { get; init; }

    public string? ExecutionRegistryEntryId { get; init; }

    public string? EventId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public required NodalOsApprovalStatus Status { get; init; }

    public required NodalOsApprovalSeverity Severity { get; init; }

    public required NodalOsApprovalActionKind RequestedAction { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required string HumanExplanationRedacted { get; init; }

    public required string PolicyGateReasonRedacted { get; init; }

    public IReadOnlyList<string> AffectedResourcesRedacted { get; init; } = [];

    public string? NoAffectedResourcesReasonRedacted { get; init; }

    public string? RollbackPlanRedacted { get; init; }

    public string? EvidencePlanRedacted { get; init; }

    public IReadOnlyList<NodalOsApprovalUserOptionKind> UserOptions { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsApprovalDecision
{
    public required string DecisionId { get; init; }

    public required string ApprovalCardId { get; init; }

    public required NodalOsApprovalDecisionKind DecisionKind { get; init; }

    public required string DecidedByRedacted { get; init; }

    public required string DecisionReasonRedacted { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsTimelineEntry
{
    public required string TimelineEntryId { get; init; }

    public required string EventId { get; init; }

    public string? ExecutionRegistryEntryId { get; init; }

    public string? ApprovalCardId { get; init; }

    public string? ApprovalDecisionId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public required NodalOsCoreEventKind SourceEventKind { get; init; }

    public required NodalOsTimelineEntrySeverity Severity { get; init; }

    public required NodalOsTimelineEntryStatus Status { get; init; }

    public required string TitleRedacted { get; init; }

    public required string MessageRedacted { get; init; }

    public required bool RequiresHumanAttention { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsEvidenceRegistryAttachment
{
    public required string AttachmentId { get; init; }

    public required NodalOsEvidenceAttachmentKind AttachmentKind { get; init; }

    public string? ExecutionRegistryEntryId { get; init; }

    public string? EventId { get; init; }

    public string? ApprovalCardId { get; init; }

    public string? TimelineEntryId { get; init; }

    public required NodalOsEvidenceBridgeRef EvidenceRef { get; init; }

    public required bool RawPayloadPersisted { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public IReadOnlyDictionary<string, string> MetadataRedacted { get; init; } = new Dictionary<string, string>();

    public DateTimeOffset CreatedAt { get; init; }
}
