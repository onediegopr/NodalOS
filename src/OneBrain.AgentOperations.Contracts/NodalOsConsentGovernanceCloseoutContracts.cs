namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentGovernanceCoveredDecision
{
    AccessImplementationCheckpointReady,
    ProductiveConsentDesignReviewReady,
    ProductiveConsentStorageNotImplementedAdrReady
}

public enum NodalOsConsentGovernanceFindingKind
{
    DesignCompleteAsGovernanceBaseline,
    StorageContractDefinedButDisabled,
    AdversarialMatrixDefined,
    AdrReady,
    ImplementationStillBlocked,
    PersistenceStillBlocked,
    EnforcementStillBlocked,
    FilesystemAccessStillBlocked,
    LlmCloudRuntimeStillBlocked
}

public sealed record NodalOsConsentGovernanceCloseout
{
    public required string CloseoutId { get; init; }
    public required string AuditCheckpointRef { get; init; }
    public required string ProductiveConsentDesignDraftRef { get; init; }
    public required string ProductiveConsentDesignReviewRef { get; init; }
    public required string DisabledConsentStorageContractRef { get; init; }
    public required string ConsentAuditAcceptanceRef { get; init; }
    public required string ConsentAdversarialMatrixRef { get; init; }
    public required string ProductiveConsentStorageAdrRef { get; init; }
    public required bool ClosedAsGovernanceBaseline { get; init; }
    public required bool ProductiveConsentStillBlocked { get; init; }
    public required bool ProductiveStorageStillBlocked { get; init; }
    public required bool ConsentEnforcementStillBlocked { get; init; }
    public IReadOnlyList<NodalOsConsentGovernanceCoveredDecision> CoveredDecisions { get; init; } = [];
    public IReadOnlyList<NodalOsConsentGovernanceFinding> Findings { get; init; } = [];
    public required NodalOsConsentGovernanceCloseoutDecision Decision { get; init; }
}

public sealed record NodalOsConsentGovernanceFinding
{
    public required string FindingId { get; init; }
    public required NodalOsConsentGovernanceFindingKind Kind { get; init; }
    public required bool BlocksProductiveUse { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsConsentGovernanceCloseoutDecision
{
    public required string DecisionId { get; init; }
    public required bool ConsentGovernanceBaselineReady { get; init; }
    public required bool ReadyForProductiveConsentImplementation { get; init; }
    public required bool ReadyForProductiveConsentStorage { get; init; }
    public required bool ReadyForProductiveConsentEnforcement { get; init; }
    public required bool ReadyForCapabilityAuthorization { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public required bool ReadyForCloud { get; init; }
    public required bool ReadyForRuntime { get; init; }
    public required string RecommendedNextMilestoneRedacted { get; init; }
}
