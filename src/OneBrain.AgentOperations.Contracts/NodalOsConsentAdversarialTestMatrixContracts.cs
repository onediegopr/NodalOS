namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentAdversarialCategory
{
    MissingConsent,
    StaleConsent,
    RevokedConsent,
    MismatchedCapability,
    ScopeTooBroad,
    ScopeMismatch,
    ContentAccessAttemptingIndexing,
    ContentAccessAttemptingRepresentationBuild,
    ContentAccessAttemptingLlmContext,
    FolderEnumerationAttemptingContentAccess,
    LocalAccessAttemptingCloudSync,
    SensitiveMaterialInRecordAttempt,
    ContentPayloadInRecordAttempt,
    UnredactedBroadPathInRecordAttempt,
    ReplayedConsent,
    CopiedConsentAcrossWorkspace,
    CopiedConsentAcrossMission,
    BypassViaUiReview,
    BypassViaLedgerMock,
    BypassViaMissingDependency,
    BypassViaProviderOrCloudRequest,
    BypassViaRuntimeRequest,
    ConflictingConsentStates,
    ConsentRevocationAfterDraft,
    ConsentFreshnessExpired
}

public sealed record NodalOsConsentAdversarialTestMatrix
{
    public required string MatrixId { get; init; }
    public required string DesignReviewRef { get; init; }
    public required string DisabledConsentStorageContractRef { get; init; }
    public required string ConsentAuditAcceptanceRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsSyntheticOnly { get; init; }
    public required bool IsAdversarialMatrixOnly { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool PersistsConsent { get; init; }
    public required bool EnforcesConsent { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public IReadOnlyList<NodalOsConsentAdversarialTestCase> TestCases { get; init; } = [];
    public required NodalOsConsentAdversarialMatrixDecision Decision { get; init; }
}

public sealed record NodalOsConsentAdversarialTestCase
{
    public required string CaseId { get; init; }
    public required NodalOsConsentAdversarialCategory Category { get; init; }
    public required string SyntheticInputRef { get; init; }
    public required string ExpectedDecisionRedacted { get; init; }
    public required string ExpectedBlockedReasonRedacted { get; init; }
    public required string ExpectedUserFacingExplanationRedacted { get; init; }
    public required string ExpectedEvidenceRef { get; init; }
    public required string ExpectedTimelineRef { get; init; }
    public required bool RequiresFailClosed { get; init; }
    public required bool NeverAuthorizesRealUse { get; init; }
    public required bool NeverPersistsProductively { get; init; }
    public required bool NeverSendsToLlm { get; init; }
    public required bool NeverSendsToCloud { get; init; }
    public required bool IsSyntheticOnly { get; init; }
}

public sealed record NodalOsConsentAdversarialMatrixDecision
{
    public required string DecisionId { get; init; }
    public required bool ReadyForAdversarialReview { get; init; }
    public required bool ReadyForProductiveConsentImplementation { get; init; }
    public required bool ReadyForProductivePersistence { get; init; }
    public required bool ReadyForConsentEnforcement { get; init; }
    public required bool ReadyForCapabilityAuthorization { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
}
