namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsPromptPurpose
{
    ProjectUnderstandingFuture,
    AssignmentPlanningFuture,
    ExpertAdvisorFuture,
    SummaryFuture,
    ReportDraftFuture,
    CodeAssistanceFuture,
    Unknown
}

public enum NodalOsPromptGovernanceState
{
    NotAllowed,
    AllowedForPreviewOnly,
    EligibleForFuturePromptWithConsent,
    BlockedByMissingByok,
    BlockedBySecretContext,
    BlockedByRawPayload,
    BlockedBySensitiveContext,
    BlockedByMissingPromptPolicy,
    BlockedByMissingBudgetPolicy,
    RequiresHumanReview,
    UnknownRequiresReview
}

public enum NodalOsBudgetPolicyScope
{
    GlobalFuture,
    WorkspaceFuture,
    MissionFuture,
    ProviderFuture
}

public enum NodalOsBudgetGuardrailStatus
{
    NotConfigured,
    DraftOnly,
    RequiredBeforeLlm,
    BlockedByMissingPolicy,
    BlockedByMissingUserConsent,
    BlockedByUnknownCost,
    EligibleForFutureUse,
    UnknownRequiresReview
}

public enum NodalOsModelCapabilityKind
{
    ChatFuture,
    ReasoningFuture,
    SummarizationFuture,
    ProjectUnderstandingFuture,
    AssignmentPlanningFuture,
    ExpertAdvisorFuture,
    CodeAssistanceFuture,
    VisionFuture,
    EmbeddingsFuture,
    ToolUseFuture,
    BrowserAutomationFuture,
    Unknown
}

public sealed record NodalOsPromptGovernancePolicy
{
    public required string PromptGovernancePolicyId { get; init; }

    public string? WorkspaceId { get; init; }

    public string? MissionId { get; init; }

    public string? ProviderSettingsRef { get; init; }

    public required string SafeContextBoundaryRef { get; init; }

    public IReadOnlyList<string> AllowedContextRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> DeniedContextRefsRedacted { get; init; } = [];

    public required string RequiredRedactionPolicyRef { get; init; }

    public required string RequiredConsentRef { get; init; }

    public IReadOnlyList<string> RequiredProvenanceLabelsRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredConfidenceFreshnessLabelsRedacted { get; init; } = [];

    public required NodalOsPromptPurpose AllowedPromptPurpose { get; init; }

    public IReadOnlyList<NodalOsPromptPurpose> DeniedPromptPurposes { get; init; } = [];

    public required NodalOsPromptGovernanceState PromptConstructionStatus { get; init; }

    public required string HumanReviewRequirementRedacted { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> DisclosuresRedacted { get; init; } = [];

    public required bool RequiresSafeContextBoundary { get; init; }

    public required bool RequiresRedaction { get; init; }

    public required bool RequiresConsent { get; init; }

    public required bool RequiresProvenanceConfidenceFreshness { get; init; }

    public required bool RequiresBudgetGuardrails { get; init; }

    public required bool RequiresByokPolicy { get; init; }

    public required bool RequiresHumanReview { get; init; }

    public required bool GeneratesFinalPromptText { get; init; }

    public required bool CallsProvider { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CallsCloud { get; init; }

    public required bool IncludesRawPayload { get; init; }

    public required bool IncludesRawEvidence { get; init; }

    public required bool ReadsFilesystem { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsBudgetGuardrailsDraft
{
    public required string BudgetPolicyId { get; init; }

    public required NodalOsBudgetPolicyScope Scope { get; init; }

    public required NodalOsBudgetGuardrailStatus BudgetStatus { get; init; }

    public required string CurrencyPlaceholderRedacted { get; init; }

    public required string MaxSpendPlaceholderRedacted { get; init; }

    public required string MaxTokensPlaceholderRedacted { get; init; }

    public required string MaxCallsPlaceholderRedacted { get; init; }

    public required string MaxRetriesPlaceholderRedacted { get; init; }

    public required string MaxConcurrentRequestsPlaceholderRedacted { get; init; }

    public IReadOnlyList<string> ModelTierRestrictionsRedacted { get; init; } = [];

    public required bool RequiresUserConfirmationAboveThreshold { get; init; }

    public required string CostVisibilityRequirementRedacted { get; init; }

    public required string StopCancelRequirementRedacted { get; init; }

    public IReadOnlyList<string> EvidenceTimelineRequirementsRedacted { get; init; } = [];

    public required string DisabledStateReasonRedacted { get; init; }

    public IReadOnlyList<string> DisclosuresRedacted { get; init; } = [];

    public required bool DraftOnly { get; init; }

    public required bool PerformsTokenCounting { get; init; }

    public required bool PerformsLiveCostLookup { get; init; }

    public required bool CallsProvider { get; init; }

    public required bool PerformsBilling { get; init; }

    public required bool CallsCloud { get; init; }

    public required bool UsesProviderSdk { get; init; }

    public required bool SendsNetworkRequest { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsModelCapabilityProfile
{
    public required string ModelCapabilityProfileId { get; init; }

    public required NodalOsByokProviderKind ProviderKind { get; init; }

    public required string ModelIdPlaceholderRedacted { get; init; }

    public IReadOnlyList<NodalOsModelCapabilityKind> CapabilityFlags { get; init; } = [];

    public IReadOnlyList<string> AllowedUseCasesRedacted { get; init; } = [];

    public IReadOnlyList<string> DeniedUseCasesRedacted { get; init; } = [];

    public IReadOnlyList<string> RiskNotesRedacted { get; init; } = [];

    public required string CostTierPlaceholderRedacted { get; init; }

    public required string LatencyTierPlaceholderRedacted { get; init; }

    public required string ContextWindowTierPlaceholderRedacted { get; init; }

    public required string ReliabilityTierPlaceholderRedacted { get; init; }

    public required string PrivacyModeCompatibilityRedacted { get; init; }

    public required bool ByokRequired { get; init; }

    public required bool LocalModelPossible { get; init; }

    public required bool ManagedAiPossible { get; init; }

    public required bool PromptGovernanceRequired { get; init; }

    public required bool BudgetGuardrailsRequired { get; init; }

    public required bool HumanReviewRequired { get; init; }

    public required bool BrowserAutomationFutureEnabledByDefault { get; init; }

    public required bool ToolUseFutureEnabledByDefault { get; init; }

    public required bool EmbeddingsFutureEnabledBeforePolicy { get; init; }

    public required bool ExpertAdvisorCanExecute { get; init; }

    public required bool PerformsLiveModelCheck { get; init; }

    public required bool PerformsLiveCostLookup { get; init; }

    public required bool CreatesRoutingDecision { get; init; }

    public required bool CallsProvider { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}
