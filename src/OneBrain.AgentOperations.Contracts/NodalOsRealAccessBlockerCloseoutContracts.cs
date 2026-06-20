namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsRealAccessBlockerStatus
{
    GovernanceBaselineClosed,
    Incomplete,
    Failed
}

public enum NodalOsRealAccessBlockerCategory
{
    PathJailNotImplemented,
    CanonicalizationNotImplemented,
    ProductiveConsentLedgerNotImplemented,
    ConsentEnforcementNotImplemented,
    FolderEnumerationGateNotImplemented,
    ContentAccessGateNotImplemented,
    ContentFingerprintGateNotImplemented,
    RedactionSensitiveDataEnforcementNotImplemented,
    ExclusionEnforcementNotImplemented,
    NoMutationRuntimeProofNotImplemented,
    CancellationRuntimeProofNotImplemented,
    EvidenceTimelineEmissionNotImplemented,
    KillSwitchRollbackNotImplemented,
    AdversarialTestsNotImplemented,
    IndexingRepresentationLlmContextGovernanceNotImplemented,
    CloudProviderRuntimeGovernanceNotImplemented
}

public sealed record NodalOsRealAccessBlockerCloseout
{
    public required string CloseoutId { get; init; }
    public required string LedgerUiPreviewRef { get; init; }
    public required string CapabilityAuditChecklistRef { get; init; }
    public required string FailClosedAcceptancePackRef { get; init; }
    public required string OperationalAccessAuditAdrRef { get; init; }
    public required string RealScanReadinessAdrRef { get; init; }
    public required NodalOsRealAccessBlockerStatus BlockerStatus { get; init; }
    public required bool ClosedAsGovernanceBaseline { get; init; }
    public required bool RealAccessStillBlocked { get; init; }
    public IReadOnlyList<NodalOsRealAccessBlocker> Blockers { get; init; } = [];
    public required NodalOsRealAccessBlockerCloseoutDecision Decision { get; init; }
}

public sealed record NodalOsRealAccessBlocker
{
    public required string BlockerId { get; init; }
    public required NodalOsRealAccessBlockerCategory Category { get; init; }
    public required bool BlocksRealAccess { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
    public required string EvidenceRef { get; init; }
    public required string TimelineRef { get; init; }
}

public sealed record NodalOsRealAccessBlockerCloseoutDecision
{
    public required string DecisionId { get; init; }
    public required bool GovernanceBaselineReady { get; init; }
    public required bool RealAccessStillBlocked { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForRealPathJail { get; init; }
    public required bool ReadyForDirectoryListing { get; init; }
    public required bool ReadyForFileRead { get; init; }
    public required bool ReadyForFileHash { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForRepresentationBuild { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public required bool ReadyForCloud { get; init; }
    public required bool ReadyForRuntime { get; init; }
    public required string RecommendedNextPhaseRedacted { get; init; }
}
