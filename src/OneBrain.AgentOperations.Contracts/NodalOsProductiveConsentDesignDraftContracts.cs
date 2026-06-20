namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsFutureConsentComponent
{
    LedgerStorageBoundary,
    ConsentScopeModel,
    ConsentFreshnessPolicy,
    RevocationPolicy,
    PerCapabilityConsentRequirements,
    UserFacingDisclosureRequirements,
    AuditTrailRequirements,
    EvidenceTimelineRequirements,
    FailClosedBehavior,
    MigrationRollbackDisableStrategy,
    LocalOnlyDefault,
    NoCloudByDefault
}

public enum NodalOsConsentDataSafetyRuleKind
{
    NoSensitiveMaterialInConsentRecords,
    NoContentPayloadInConsentRecords,
    NoBroadPathLeakageInReports,
    PathRefsRemainRedacted,
    ConsentCannotImplyLlmOrCloudPermission,
    ContentAccessConsentCannotImplyRepresentationOrLlmContext,
    ConsentMustBeRevocable,
    MissingStaleRevokedConsentFailsClosed
}

public sealed record NodalOsProductiveConsentDesignDraft
{
    public required string DesignId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required string BasedOnLedgerMockRef { get; init; }
    public IReadOnlyList<string> BasedOnCapabilityGateRefs { get; init; } = [];
    public required bool IsDesignOnly { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool PersistsConsent { get; init; }
    public required bool EnforcesConsent { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanBuildLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public IReadOnlyList<NodalOsFutureConsentComponent> FutureComponents { get; init; } = [];
    public IReadOnlyList<NodalOsConsentDataSafetyRule> DataSafetyRules { get; init; } = [];
    public required NodalOsProductiveConsentDesignDecision Decision { get; init; }
}

public sealed record NodalOsConsentDataSafetyRule
{
    public required string RuleId { get; init; }
    public required NodalOsConsentDataSafetyRuleKind Kind { get; init; }
    public required bool Required { get; init; }
    public required bool BlocksProductiveUseIfMissing { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsProductiveConsentDesignDecision
{
    public required string DecisionId { get; init; }
    public required bool ReadyForDesignReview { get; init; }
    public required bool ReadyForProductiveImplementation { get; init; }
    public required bool ReadyForConsentPersistence { get; init; }
    public required bool ReadyForConsentEnforcement { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public IReadOnlyList<string> RequiredBeforeImplementationRedacted { get; init; } = [];
}
