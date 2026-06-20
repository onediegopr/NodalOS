namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsMissionPlanDraftStatus
{
    DraftOnly,
    NeedsReview,
    ReadyForManualReview,
    Blocked,
    ArchivedMock
}

public enum NodalOsTaskGraphReviewCardState
{
    Draft,
    NeedsReview,
    ReadyForManualReview,
    Blocked,
    Deferred,
    DiscardedMock,
    FutureExecutionBlocked,
    UnknownRequiresReview
}

public enum NodalOsTaskGraphReviewOptionKind
{
    MarkNeedsClarification,
    MarkReviewedMock,
    RequestExplanation,
    LinkEvidenceRef,
    Defer,
    DiscardDraft,
    OpenGuardrails
}

public enum NodalOsAssignmentEvidenceLinkType
{
    SupportsPlanDraft,
    SupportsWorkItem,
    UserContextReference,
    RiskEvidenceReference,
    DependencyEvidenceReference,
    ClarificationNeeded,
    ContradictionSuspected,
    FutureVerificationNeeded,
    RelatedTimelineEvent
}

public enum NodalOsAssignmentEvidenceLinkStatus
{
    DraftLink,
    LinkedRefOnly,
    RequiresReview,
    BlockedUnsafeEvidence,
    RemovedMock
}

public sealed record NodalOsMissionPlanDraftPreview
{
    public required string MissionPlanPreviewId { get; init; }

    public required string AssignmentRequestId { get; init; }

    public required string TaskGraphId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string MissionId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string SummaryRedacted { get; init; }

    public required NodalOsAssignmentPurpose PlanningPurpose { get; init; }

    public required NodalOsPlannerReadinessState ReadinessStatus { get; init; }

    public required NodalOsMissionPlanDraftStatus DraftStatus { get; init; }

    public required string WorkItemsSummaryRedacted { get; init; }

    public required string DependencySummaryRedacted { get; init; }

    public required string RiskSummaryRedacted { get; init; }

    public required string BlockedItemsSummaryRedacted { get; init; }

    public IReadOnlyList<string> HumanReviewRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> DisclosuresRedacted { get; init; } = [];

    public required bool DraftOnly { get; init; }

    public required bool ReadOnly { get; init; }

    public required bool TaskGraphExecutable { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CreatesRuntimeAction { get; init; }

    public required bool TouchesFilesystem { get; init; }

    public required bool SchedulesWork { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsMissionPlanDraftPreviewRender
{
    public required string RenderId { get; init; }

    public required string MissionPlanPreviewId { get; init; }

    public required string HtmlRedacted { get; init; }

    public required bool StaticOnly { get; init; }

    public required bool ReadOnly { get; init; }

    public required bool ContainsRawSecrets { get; init; }

    public required bool ContainsRuntimeControl { get; init; }
}

public sealed record NodalOsTaskGraphReviewCard
{
    public required string ReviewCardId { get; init; }

    public required string TaskGraphId { get; init; }

    public required string WorkItemId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string SummaryRedacted { get; init; }

    public required NodalOsAssignmentTaskKind TaskKind { get; init; }

    public required NodalOsTaskGraphReviewCardState ReviewState { get; init; }

    public required NodalOsAssignmentRiskLevel RiskLevel { get; init; }

    public IReadOnlyList<string> DependencyIds { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public required bool RequiresHumanReview { get; init; }

    public required bool RequiresFutureLlm { get; init; }

    public required bool RequiresFutureApproval { get; init; }

    public required bool RequiresFutureFilesystem { get; init; }

    public required bool RequiresFutureRuntime { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ContextRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<NodalOsTaskGraphReviewOptionKind> UserOptions { get; init; } = [];

    public required bool UserOptionsAreNoOp { get; init; }

    public required bool NonAuthoritative { get; init; }

    public required bool CanExecute { get; init; }
}

public sealed record NodalOsAssignmentEvidenceLink
{
    public required string AssignmentEvidenceLinkId { get; init; }

    public required string AssignmentRequestId { get; init; }

    public required string TaskGraphId { get; init; }

    public string? WorkItemId { get; init; }

    public required string EvidenceRefId { get; init; }

    public string? TimelineRefId { get; init; }

    public string? ContextRefId { get; init; }

    public required NodalOsAssignmentEvidenceLinkType LinkType { get; init; }

    public required NodalOsAssignmentEvidenceLinkStatus LinkStatus { get; init; }

    public required string LinkReasonRedacted { get; init; }

    public required string ProvenanceRedacted { get; init; }

    public required string ValidationResultRedacted { get; init; }

    public required bool RefOnly { get; init; }

    public required bool ContainsRawEvidencePayload { get; init; }

    public required bool ContainsInlineScreenshot { get; init; }

    public required bool ContainsRawDom { get; init; }

    public required bool ContainsRawNetwork { get; init; }

    public required bool ReadsFiles { get; init; }

    public required bool VerifiesRealContent { get; init; }

    public required bool ConvertsPlanToAuthoritativeTruth { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CallsCloud { get; init; }

    public required bool ExecutesRuntime { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
