namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsByokProviderScope
{
    GlobalFuture,
    WorkspaceFuture,
    MissionFuture,
    UserProfileFuture
}

public enum NodalOsByokProviderKind
{
    OpenAiFuture,
    AnthropicFuture,
    GeminiFuture,
    LocalModelFuture,
    OllamaFuture,
    LmStudioFuture,
    CustomOpenAiCompatibleFuture,
    Unknown
}

public enum NodalOsByokProviderKeyStatus
{
    NotConfigured,
    ReferenceOnlyConfigured,
    RequiresSecretStoragePolicy,
    RequiresUserConsent,
    Disabled,
    BlockedByPolicy
}

public enum NodalOsByokProviderCapability
{
    ChatFuture,
    ReasoningFuture,
    EmbeddingsFuture,
    VisionFuture,
    CodeAssistanceFuture,
    ProjectUnderstandingFuture,
    AssignmentFuture,
    AdvisorFuture
}

public enum NodalOsProviderTestConnectionState
{
    NotAvailable,
    MockOnly,
    ReadyForFuturePreflight,
    BlockedByMissingSecretRef,
    BlockedBySecretStoragePolicy,
    BlockedByNetworkDisabled,
    BlockedByProviderPolicy,
    BlockedByUserConsentMissing,
    BlockedByBudgetPolicyMissing,
    BlockedByPromptGovernanceMissing,
    UnknownRequiresReview
}

public sealed record NodalOsByokProviderSettings
{
    public required string ProviderSettingsId { get; init; }

    public string? WorkspaceId { get; init; }

    public required NodalOsByokProviderScope Scope { get; init; }

    public required NodalOsByokProviderKind ProviderKind { get; init; }

    public required string ModelSelectionPlaceholderRedacted { get; init; }

    public required string EndpointPolicyPlaceholderRedacted { get; init; }

    public required string CredentialReferencePlaceholderRedacted { get; init; }

    public required NodalOsByokProviderKeyStatus ProviderKeyStatus { get; init; }

    public IReadOnlyList<NodalOsByokProviderCapability> CapabilitiesDeclared { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public required string BudgetPolicyRef { get; init; }

    public required string PromptGovernanceRef { get; init; }

    public required string ConsentRef { get; init; }

    public required string RedactionPolicyRef { get; init; }

    public required string SafeContextBoundaryRef { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool ReferenceOnly { get; init; }

    public required bool StoresRawCredential { get; init; }

    public required bool CallsProvider { get; init; }

    public required bool SendsNetworkRequest { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CreatesEmbeddings { get; init; }

    public required bool RoutesLlmTraffic { get; init; }

    public required bool CallsCloud { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsProviderTestConnectionPreview
{
    public required string TestConnectionPreviewId { get; init; }

    public required string ProviderSettingsRef { get; init; }

    public required NodalOsProviderTestConnectionState State { get; init; }

    public required string CredentialRefStatusRedacted { get; init; }

    public required string ModelTargetPlaceholderRedacted { get; init; }

    public required string EndpointTargetRedacted { get; init; }

    public IReadOnlyList<string> PreflightChecksRedacted { get; init; } = [];

    public required string UserConsentRequirementRedacted { get; init; }

    public required string NetworkDisabledStatusRedacted { get; init; }

    public required string DryRunMockStatusRedacted { get; init; }

    public required string ExpectedSafeResultRedacted { get; init; }

    public required string ErrorRedactionPolicyRedacted { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ObservabilityRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool ActionDisabled { get; init; }

    public required bool MockOnly { get; init; }

    public required bool PerformsNetworkRequest { get; init; }

    public required bool UsesProviderSdk { get; init; }

    public required bool ReadsEnvironmentVariables { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool StoresRawCredential { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
