namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsProjectUnderstandingPreconditionStatus
{
    NotStarted,
    PartiallyDefined,
    FullyDefined,
    BlockedMissingConsent,
    BlockedMissingPathJail,
    BlockedMissingRedactionPolicy,
    BlockedMissingSecretPolicy,
    BlockedMissingExclusionPolicy,
    BlockedMissingCancellationSemantics,
    BlockedMissingAudit,
    ReadyForGovernanceReview
}

public sealed record NodalOsProjectUnderstandingPreconditions
{
    public required string PreconditionsId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required string AssignmentGovernanceRef { get; init; }

    public required string RequiredConsent { get; init; }

    public required string RequiredPathJailValidation { get; init; }

    public required string RequiredScanScopePreview { get; init; }

    public required string RequiredRedactionPolicy { get; init; }

    public required string RequiredSecretDetectionPolicy { get; init; }

    public required string RequiredExclusionPolicy { get; init; }

    public required string RequiredSymlinkPolicy { get; init; }

    public required string RequiredCaseSensitivityPolicy { get; init; }

    public required string RequiredMaxFileLimits { get; init; }

    public required string RequiredCancellationSemantics { get; init; }

    public required string RequiredEvidencePlan { get; init; }

    public required string RequiredTimelineEvents { get; init; }

    public required string RequiredNoMutationGuarantee { get; init; }

    public required string RequiredNoCloudDefault { get; init; }

    public required string RequiredNoLlmBeforeContextApproval { get; init; }

    public required string RequiredAuditBeforeRealScan { get; init; }

    public required NodalOsProjectUnderstandingPreconditionStatus Status { get; init; }

    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsProjectUnderstandingReadinessResult
{
    public required string ReadinessId { get; init; }

    public required string PreconditionsRef { get; init; }

    public required bool ReadyForRealProjectUnderstanding { get; init; }

    public required bool ReadyForFilesystemScan { get; init; }

    public required bool ReadyForLlmContextBuild { get; init; }

    public required bool ReadyForEmbeddings { get; init; }

    public required bool ReadyForIndexing { get; init; }

    public required bool ReadyForCloudSync { get; init; }

    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public required string UserFacingExplanationRedacted { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];
}
