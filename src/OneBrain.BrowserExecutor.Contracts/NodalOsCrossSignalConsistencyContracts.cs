namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsCrossSignalMismatch
{
    None,
    IdentityPerceptionMismatch,
    PerceptionActionMismatch,
    ActionMemoryMismatch,
    ScopeInflationDetected,
    UnsafeSignalPromotion,
    MissingCoreAuthority,
    RequiresHumanReview,
    UnredactedEvidence,
    SensitiveSurface
}

public enum NodalOsCrossSignalConsistencyStatus
{
    Consistent,
    IdentityPerceptionMismatch,
    PerceptionActionMismatch,
    ActionMemoryMismatch,
    ScopeInflationDetected,
    UnsafeSignalPromotion,
    MissingCoreAuthority,
    RequiresHumanReview
}

public enum NodalOsHito162ReplacementReadiness
{
    Unknown,
    StableLocalFixtureFirst,
    NeedsMoreFixtureHardening,
    BlockedByScopeInflation,
    BlockedByUnsafeSignalPromotion
}

public sealed record NodalOsCrossSignalConsistencyInput(
    NodalOsIdentityConfidence IdentityConfidence,
    bool IdentityActionAuthorityGranted,
    NodalOsPerceptionReadiness PerceptionReadiness,
    bool PerceptionActionAuthorityGranted,
    NodalOsActionDecision ActionDecision,
    IReadOnlyList<NodalOsActionDeniedReason> ActionDeniedReasons,
    bool CoreAuthorityRequired,
    bool CoreApproved,
    bool ObserveOnly,
    bool SafeActionSensitiveAuthorized,
    NodalOsMemoryConfidence MemoryConfidence,
    bool MemoryAccepted,
    bool MemoryActionAuthorityGranted,
    bool MemoryContainsSensitiveRawValues,
    bool MemoryRedacted,
    bool OverlayBlocked,
    bool AmbiguousState,
    bool SensitiveSurface,
    bool ExternalGeneralReady,
    bool ProductionEnabled);

public sealed record NodalOsCrossSignalConsistencyResult(
    NodalOsCrossSignalConsistencyStatus Status,
    NodalOsHito162ReplacementReadiness Readiness,
    IReadOnlyList<NodalOsCrossSignalMismatch> Mismatches,
    IReadOnlyList<string> EvidenceRefs,
    string RecommendedNextStep,
    bool Consistent,
    bool ScopeInflationDetected,
    bool ActionAuthorityGranted,
    bool Redacted);

