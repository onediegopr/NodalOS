namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsProjectUnderstandingReadinessState
{
    NotReady,
    ReadyForUserProvidedContextReview,
    ReadyForMockSummaryOnly,
    ReadyForSafeContextBoundaryReview,
    BlockedByMissingWorkspace,
    BlockedByMissingContext,
    BlockedBySensitiveContext,
    BlockedBySecretContext,
    BlockedByLlmPolicyMissing,
    BlockedByFilesystemPolicyMissing,
    BlockedByPositiveExecutionGate,
    UnknownRequiresReview
}

public sealed record NodalOsContextIntakeUiPreview
{
    public required string PreviewId { get; init; }

    public required string WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public IReadOnlyList<NodalOsUserProvidedContextCapture> ContextCaptures { get; init; } = [];

    public IReadOnlyList<NodalOsContextReviewCard> ReviewCards { get; init; } = [];

    public IReadOnlyList<NodalOsContextEvidenceLink> EvidenceLinks { get; init; } = [];

    public required int SafeCount { get; init; }

    public required int BlockedCount { get; init; }

    public required int RequiresReviewCount { get; init; }

    public IReadOnlyList<string> MissingInformationRedacted { get; init; } = [];

    public IReadOnlyList<string> QuestionsForUserRedacted { get; init; } = [];

    public IReadOnlyList<string> ProvenanceLabelsRedacted { get; init; } = [];

    public IReadOnlyList<string> ConfidenceLabelsRedacted { get; init; } = [];

    public IReadOnlyList<string> FreshnessLabelsRedacted { get; init; } = [];

    public IReadOnlyList<string> SensitivityLabelsRedacted { get; init; } = [];

    public IReadOnlyList<string> AllowedUsageChipsRedacted { get; init; } = [];

    public IReadOnlyList<string> DisallowedUsageChipsRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledFutureCapabilitiesRedacted { get; init; } = [];

    public required string NoFilesReadDisclosureRedacted { get; init; }

    public required string NoLlmDisclosureRedacted { get; init; }

    public required string NoPromptCreationDisclosureRedacted { get; init; }

    public required string NoRealProjectUnderstandingDisclosureRedacted { get; init; }

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailExplainersRedacted { get; init; } = [];

    public required bool StaticPreviewOnly { get; init; }

    public required bool ReadOnlyPreview { get; init; }

    public required bool UserProvidedAndUnverified { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool ReadsFiles { get; init; }

    public required bool VerifiesPaths { get; init; }

    public required bool MutatesProductiveState { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsContextValidationSummary
{
    public required string ValidationSummaryId { get; init; }

    public required string WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public required int TotalCaptures { get; init; }

    public required int SafeCaptures { get; init; }

    public required int BlockedCaptures { get; init; }

    public required int RequiresReviewCaptures { get; init; }

    public IReadOnlyDictionary<string, int> BlockedByReasonRedacted { get; init; } = new Dictionary<string, int>();

    public required int MissingInfoCount { get; init; }

    public required int QuestionsCount { get; init; }

    public required int EvidenceLinkedCount { get; init; }

    public required int UnverifiedClaimsCount { get; init; }

    public required int RawPayloadBlockedCount { get; init; }

    public required int CredentialBlockedCount { get; init; }

    public IReadOnlyDictionary<string, int> SensitivityDistributionRedacted { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> AllowedUsageDistributionRedacted { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> DisallowedUsageDistributionRedacted { get; init; } = new Dictionary<string, int>();

    public required string ReadinessImpactRedacted { get; init; }

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public required string HumanReadableSummaryRedacted { get; init; }

    public required string TechnicalSummaryRedacted { get; init; }

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> RecommendationsRedacted { get; init; } = [];

    public required string ReadinessDeltaRedacted { get; init; }

    public required bool NonAuthoritative { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool ConvertsClaimsToTruth { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool ReadsFiles { get; init; }

    public DateTimeOffset ValidationTimestamp { get; init; }
}

public sealed record NodalOsProjectUnderstandingReadinessReport
{
    public required string ReportId { get; init; }

    public string? WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public required NodalOsProjectUnderstandingReadinessState State { get; init; }

    public string? WorkspaceReadinessGateRef { get; init; }

    public string? ContextValidationSummaryRef { get; init; }

    public IReadOnlyList<string> SafeContextBoundaryRefs { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> SafeContextAvailableRedacted { get; init; } = [];

    public IReadOnlyList<string> MissingInformationRedacted { get; init; } = [];

    public IReadOnlyList<string> DisplayExportOnlyRedacted { get; init; } = [];

    public IReadOnlyList<string> HumanReviewRequiredRedacted { get; init; } = [];

    public IReadOnlyList<string> FutureLlmPolicyRequiredRedacted { get; init; } = [];

    public IReadOnlyList<string> FutureFilesystemPolicyRequiredRedacted { get; init; } = [];

    public IReadOnlyList<string> ApprovalConsentRequiredRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public required string UserFacingExplanationRedacted { get; init; }

    public required string TechnicalExplanationRedacted { get; init; }

    public required bool ReadinessOnly { get; init; }

    public required bool StartsRealProjectUnderstanding { get; init; }

    public required bool ScansFilesystem { get; init; }

    public required bool ReadsFiles { get; init; }

    public required bool UsesEmbeddings { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool MutatesWorkspaceProductively { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
