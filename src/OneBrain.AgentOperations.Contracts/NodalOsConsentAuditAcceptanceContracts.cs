namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentAuditAcceptanceStatus
{
    GovernanceBaselineAccepted,
    ConditionalPass,
    Failed
}

public enum NodalOsConsentAuditCriterionKind
{
    DesignReviewed,
    StorageContractDisabledByDefault,
    NoProductivePersistence,
    NoConsentEnforcement,
    NoCapabilityAuthorization,
    NoImplicitCapabilityInheritance,
    NoLlmCloudImplication,
    MissingStaleRevokedFailsClosed,
    RedactionRulesDefined,
    AuditTrailRequirementsDefined,
    EvidenceTimelineRequirementsDefined,
    RollbackDisableStrategyDefined,
    AdversarialTestsRequired,
    ImplementationStillBlocked
}

public sealed record NodalOsConsentAuditAcceptancePack
{
    public required string AcceptanceId { get; init; }
    public required string DesignReviewRef { get; init; }
    public required string StorageContractRef { get; init; }
    public required string AuditCheckpointRef { get; init; }
    public required string CapabilityAuditChecklistRef { get; init; }
    public required NodalOsConsentAuditAcceptanceStatus AcceptanceStatus { get; init; }
    public required bool IsAcceptanceOnly { get; init; }
    public required bool CanApproveImplementation { get; init; }
    public required bool CanEnableProductiveConsent { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAccessFilesystem { get; init; }
    public required bool CanBuildLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public IReadOnlyList<NodalOsConsentAuditAcceptanceCriterion> AcceptanceCriteria { get; init; } = [];
    public required NodalOsConsentAuditAcceptanceDecision Decision { get; init; }
}

public sealed record NodalOsConsentAuditAcceptanceCriterion
{
    public required string CriterionId { get; init; }
    public required NodalOsConsentAuditCriterionKind Kind { get; init; }
    public required bool Required { get; init; }
    public required bool Satisfied { get; init; }
    public required bool BlocksImplementationIfMissing { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsConsentAuditAcceptanceDecision
{
    public required string DecisionId { get; init; }
    public required bool ConsentDesignAcceptedAsGovernanceBaseline { get; init; }
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
