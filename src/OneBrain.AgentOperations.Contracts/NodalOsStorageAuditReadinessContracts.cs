namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsStorageAuditReadinessStatus
{
    ReadyForAuditPlanningOnly,
    Blocked,
    Failed
}

public enum NodalOsStorageAuditReadinessCriterionKind
{
    AdrExists,
    DisabledByDefaultStrategyExists,
    BoundaryTestPackExists,
    AdversarialConsentMatrixExists,
    RedactionRulesExist,
    EvidenceTimelineRequirementsExist,
    RollbackDisableStrategyExists,
    MigrationBlockedUntilFutureGate,
    CloudSyncBlocked,
    ProviderRuntimeBlocked,
    FutureExplicitMilestoneRequired,
    FullSuiteRequired,
    GuardChecksRequired,
    SecurityAuditRequired
}

public sealed record NodalOsStorageAuditReadiness
{
    public required string ReadinessId { get; init; }
    public required string BoundaryTestPackRef { get; init; }
    public required string DisabledStorageUiPreviewRef { get; init; }
    public required string ProductiveConsentStorageAdrRef { get; init; }
    public required string ConsentGovernanceCloseoutRef { get; init; }
    public required NodalOsStorageAuditReadinessStatus ReadinessStatus { get; init; }
    public required bool IsReadinessOnly { get; init; }
    public required bool CanAuthorizeImplementation { get; init; }
    public required bool CanEnableProductiveStorage { get; init; }
    public required bool CanPersistConsent { get; init; }
    public required bool CanEnforceConsent { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAccessFilesystem { get; init; }
    public required bool CanBuildLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public IReadOnlyList<NodalOsStorageAuditReadinessCriterion> Criteria { get; init; } = [];
    public required NodalOsStorageAuditReadinessDecision Decision { get; init; }
}

public sealed record NodalOsStorageAuditReadinessCriterion
{
    public required string CriterionId { get; init; }
    public required NodalOsStorageAuditReadinessCriterionKind Kind { get; init; }
    public required bool Required { get; init; }
    public required bool SatisfiedForPlanning { get; init; }
    public required bool BlocksImplementationIfMissing { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsStorageAuditReadinessDecision
{
    public required string DecisionId { get; init; }
    public required bool ReadyForAuditPlanning { get; init; }
    public required bool ReadyForProductiveStorageImplementation { get; init; }
    public required bool ReadyForProductivePersistence { get; init; }
    public required bool ReadyForConsentEnforcement { get; init; }
    public required bool ReadyForCapabilityAuthorization { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public required string RecommendedNextMilestoneRedacted { get; init; }
}
