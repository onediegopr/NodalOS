namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentStorageBoundaryCategory
{
    StorageDisabledByDefault,
    LocalOnlyDefault,
    WorkspaceBoundRecord,
    MissionBoundRecord,
    CapabilityBoundRecord,
    ScopeBoundRecord,
    NoSensitiveMaterial,
    NoContentPayload,
    NoBroadUnredactedPath,
    NoCloudSync,
    NoProviderSync,
    NoRuntimeAccess,
    NoImplicitCapabilityInheritance,
    ContentAccessDoesNotImplyIndexing,
    ContentAccessDoesNotImplyRepresentationBuild,
    ContentAccessDoesNotImplyLlmContext,
    RevokedConsentFailsClosed,
    StaleConsentFailsClosed,
    MissingConsentFailsClosed,
    ConflictingConsentStatesFailClosed,
    ReplayedConsentBlocked,
    CopiedConsentAcrossWorkspaceBlocked,
    CopiedConsentAcrossMissionBlocked,
    MigrationDisabledUntilFutureGate,
    RollbackDisableStrategyRequired
}

public sealed record NodalOsConsentStorageBoundaryTestPack
{
    public required string TestPackId { get; init; }
    public required string ProductiveConsentStorageAdrRef { get; init; }
    public required string ConsentGovernanceCloseoutRef { get; init; }
    public required string DisabledConsentStorageContractRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsBoundaryTestPackOnly { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool ReadsProductiveStorage { get; init; }
    public required bool WritesProductiveStorage { get; init; }
    public required bool DeletesProductiveStorage { get; init; }
    public required bool MigratesProductiveStorage { get; init; }
    public required bool SyncsToCloud { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
    public IReadOnlyList<NodalOsConsentStorageBoundaryTestCase> TestCases { get; init; } = [];
    public required NodalOsConsentStorageBoundaryTestPackDecision Decision { get; init; }
}

public sealed record NodalOsConsentStorageBoundaryTestCase
{
    public required string CaseId { get; init; }
    public required NodalOsConsentStorageBoundaryCategory Category { get; init; }
    public required string SyntheticRecordDraftRef { get; init; }
    public required string ExpectedDecisionRedacted { get; init; }
    public required string ExpectedBlockedReasonRedacted { get; init; }
    public required bool RequiresFailClosed { get; init; }
    public required bool NeverAuthorizesRealUse { get; init; }
    public required bool NeverPersistsProductively { get; init; }
    public required bool NeverSendsToLlm { get; init; }
    public required bool NeverSendsToCloud { get; init; }
    public required bool IsSyntheticOnly { get; init; }
}

public sealed record NodalOsConsentStorageBoundaryTestPackDecision
{
    public required string DecisionId { get; init; }
    public required bool ReadyForBoundaryReview { get; init; }
    public required bool ReadyForProductiveStorageImplementation { get; init; }
    public required bool ReadyForProductivePersistence { get; init; }
    public required bool ReadyForConsentEnforcement { get; init; }
    public required bool ReadyForCapabilityAuthorization { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
}
