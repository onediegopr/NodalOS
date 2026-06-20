namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsDryRunPlannedEvidenceKind
{
    ConsentReview,
    ScopePreview,
    PolicyPreview,
    ExclusionPreview,
    AuditGateDecision,
    GuardrailExplanation,
    TimelinePreview
}

public enum NodalOsDryRunTimelineEventType
{
    PreviewOpened,
    ConsentCardReviewed,
    ScopeExplained,
    PolicyExplained,
    AuditGateBlocked,
    EvidencePlanReviewed
}

public sealed record NodalOsDryRunPlannedEvidenceItem
{
    public required string PlannedEvidenceId { get; init; }

    public required NodalOsDryRunPlannedEvidenceKind Kind { get; init; }

    public required string SourceRef { get; init; }

    public required string DescriptionRedacted { get; init; }

    public required string RedactionRequirementRedacted { get; init; }

    public required string UserFacingExplanationRedacted { get; init; }

    public required bool CanContainRawContent { get; init; }

    public required bool CanContainRawSecret { get; init; }

    public required bool CanVerifyFilesystemContent { get; init; }
}

public sealed record NodalOsDryRunPlannedTimelineEvent
{
    public required string PlannedEventId { get; init; }

    public required NodalOsDryRunTimelineEventType EventType { get; init; }

    public required string DisplayTitleRedacted { get; init; }

    public required string DescriptionRedacted { get; init; }

    public required string SeverityRedacted { get; init; }

    public IReadOnlyList<string> SourceRefs { get; init; } = [];

    public required bool IsPreviewOnly { get; init; }

    public required bool Emitted { get; init; }
}

public sealed record NodalOsDryRunEvidencePlan
{
    public required string EvidencePlanId { get; init; }

    public required string DryRunContractRef { get; init; }

    public IReadOnlyList<string> ConsentReviewCardRefs { get; init; } = [];

    public IReadOnlyList<NodalOsDryRunPlannedEvidenceItem> PlannedEvidenceItems { get; init; } = [];

    public IReadOnlyList<NodalOsDryRunPlannedTimelineEvent> PlannedTimelineEvents { get; init; } = [];

    public IReadOnlyList<string> PlannedAuditRefs { get; init; } = [];

    public IReadOnlyList<string> PlannedRedactionRefs { get; init; } = [];

    public required bool IsPlanOnly { get; init; }

    public required bool EmitsRealEvidence { get; init; }

    public required bool VerifiesRealContent { get; init; }

    public required bool UsesRealFilesystem { get; init; }

    public required bool BuildsLlmContext { get; init; }
}

public sealed record NodalOsDryRunEvidencePlanReadiness
{
    public required string ReadinessId { get; init; }

    public required string EvidencePlanRef { get; init; }

    public required bool ReadyForRealDryRunEvidence { get; init; }

    public required bool ReadyForRealScan { get; init; }

    public required bool ReadyForRealEvidenceVerification { get; init; }

    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}
