namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsRealScanPreconditionState
{
    NotEligible,
    EligibleForPreviewOnly,
    EligibleForUserConsentRequest,
    BlockedByMissingPathJail,
    BlockedByMissingConsent,
    BlockedBySensitiveScope,
    BlockedBySecretsPolicy,
    BlockedByCloudPolicy,
    BlockedByLlmPolicy,
    BlockedBySymlinkPolicy,
    BlockedByBinaryPolicy,
    BlockedBySizeLimitPolicy,
    UnknownRequiresReview
}

public enum NodalOsContextToLlmGovernanceState
{
    NotAllowed,
    AllowedForDisplayOnly,
    AllowedForExportOnly,
    EligibleForFutureLlmWithConsent,
    BlockedUntilByokConfigured,
    BlockedUntilPromptPolicyDefined,
    BlockedUntilBudgetPolicyDefined,
    BlockedBySecret,
    BlockedByRawPayload,
    BlockedBySensitiveContext,
    RequiresHumanReview,
    UnknownRequiresReview
}

public sealed record NodalOsRealScanPreconditions
{
    public required string PreconditionsId { get; init; }

    public required string WorkspaceId { get; init; }

    public string? PathJailBindingId { get; init; }

    public required NodalOsRealScanPreconditionState State { get; init; }

    public required string ExplicitConsentPlaceholderRedacted { get; init; }

    public required string ScanScopeRedacted { get; init; }

    public IReadOnlyList<string> ExcludedPatternsRedacted { get; init; } = [];

    public required int MaxFileCount { get; init; }

    public required long MaxFileSizeBytes { get; init; }

    public required string BinaryFilePolicyRedacted { get; init; }

    public required string CredentialDetectionPolicyRedacted { get; init; }

    public required string RedactionPolicyRedacted { get; init; }

    public required string SymlinkPolicyRedacted { get; init; }

    public required string CaseSensitivityPolicyRedacted { get; init; }

    public IReadOnlyList<string> AuditEvidenceRequirementsRedacted { get; init; } = [];

    public required bool WorkspaceValidated { get; init; }

    public required bool PathJailValidated { get; init; }

    public required bool ExplicitConsentRecorded { get; init; }

    public required bool PreviewBeforeScanRequired { get; init; }

    public required bool CancelStopRequired { get; init; }

    public required bool NoMutationGuaranteed { get; init; }

    public required bool NoCloudUpload { get; init; }

    public required bool NoLlmCall { get; init; }

    public required bool NoEmbeddingsUntilSeparatePolicy { get; init; }

    public required bool AllowsSymlinkFollowing { get; init; }

    public required bool PerformsRealScan { get; init; }

    public required bool ListsFilesystem { get; init; }

    public required bool ReadsFiles { get; init; }

    public required bool HashesFiles { get; init; }

    public required bool UsesGit { get; init; }

    public required bool MutatesFilesystem { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsContextToLlmGovernanceDraft
{
    public required string GovernanceId { get; init; }

    public required string WorkspaceId { get; init; }

    public required NodalOsContextToLlmGovernanceState State { get; init; }

    public required NodalOsSafeContextUsageTarget UsageTarget { get; init; }

    public IReadOnlyList<string> PotentialFutureContextRedacted { get; init; } = [];

    public IReadOnlyList<string> ProhibitedContextRedacted { get; init; } = [];

    public required string RedactionRequirementRedacted { get; init; }

    public required string UserConsentRequirementRedacted { get; init; }

    public required string ByokRequirementRedacted { get; init; }

    public required string BudgetPolicyRequirementRedacted { get; init; }

    public required string PromptGovernanceRequirementRedacted { get; init; }

    public IReadOnlyList<string> EvidenceRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> ProvenanceConfidenceFreshnessRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> HumanReviewRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> TimelineRegistrationRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockReasonsRedacted { get; init; } = [];

    public required bool RequiresRedaction { get; init; }

    public required bool RequiresUserConsent { get; init; }

    public required bool RequiresFutureByok { get; init; }

    public required bool RequiresPromptGovernance { get; init; }

    public required bool RequiresBudgetGuardrails { get; init; }

    public required bool RequiresHumanReview { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool SendsNetworkData { get; init; }

    public required bool CallsCloud { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
