namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsProductAdminPrivatePreviewStatus
{
    LocalPrivatePreviewReady,
    LocalPrivatePreviewBlocked,
    ExternalGeneralBlocked,
    SensitiveSurfaceBlocked,
    CredentialsBlocked,
    SubmitMutationBlocked,
    PublicSaasBlocked,
    BillingEmailBlocked,
    RecorderReplayBlocked
}

public sealed record NodalOsProductAdminPrivatePreviewHardeningReport(
    string ProductName,
    IReadOnlyList<NodalOsProductAdminPrivatePreviewStatus> VisibleStates,
    string M51Status,
    string M65Status,
    bool UiAdminAuthorityBlocked,
    bool CoreAuthorityRequired,
    bool ExternalGeneralReady,
    bool PublicSaasBlocked,
    bool PublicApiBlocked,
    bool BillingEmailBlocked,
    bool CredentialsBlocked,
    bool SensitiveSurfacesBlocked,
    bool SubmitPaySignDeleteBlocked,
    bool RecorderReplayBlocked,
    IReadOnlyList<string> NextRequiredActions,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted);

public sealed record NodalOsOperatorUxReadinessSummary(
    string ProductName,
    string CurrentState,
    IReadOnlyList<string> AllowedLocalActions,
    IReadOnlyList<string> BlockedExternalSensitiveActions,
    IReadOnlyList<string> EvidenceSummary,
    string LastProofSummary,
    IReadOnlyList<string> LedgerRefs,
    IReadOnlyList<string> ActiveBlockers,
    string NextAction,
    string RequiredHumanIntervention,
    string RiskLevel,
    bool Redacted);

public enum NodalOsLocalPrivatePreviewReleaseGateStatus
{
    NotReady,
    ReadyForInternalLocalPreview,
    ReadyWithRestrictions,
    BlockedByExternalGeneral,
    BlockedByMissingEvidence,
    BlockedByProductAdmin,
    BlockedBySecurity,
    BlockedByWorktree
}

public enum NodalOsLocalPrivatePreviewScope
{
    InternalLocalPrivatePreviewOnly,
    ExternalGeneralBlocked,
    PublicSaasBlocked,
    SensitiveSurfacesBlocked
}

public sealed record NodalOsLocalPrivatePreviewReleaseGateInput(
    bool BuildOk,
    bool TestsOk,
    bool CanonicalWorktreeOk,
    bool M51ClosedHttpScope,
    bool M65ClosedLimitedCdpScope,
    bool ProductAdminLocalReady,
    bool OperatorRunbookExists,
    bool BlockerExplanationsReady,
    bool EvidenceLogSummaryReady,
    bool ExternalGeneralReady,
    bool PublicSaasEnabled,
    bool PublicApiEnabled,
    bool RealBillingEnabled,
    bool RealEmailEnabled,
    bool RealCredentialsEnabled,
    bool SensitiveSitesEnabled,
    bool SubmitPaySignDeleteEnabled,
    bool RecorderReplayProductiveEnabled);

public sealed record NodalOsReleaseGateStateSnapshot(
    bool BuildOk,
    bool TestsOk,
    bool WorktreeCanonical,
    bool M51EvidenceAvailable,
    bool M65EvidenceAvailable,
    bool ProductAdminReady,
    bool OperatorRunbookExists,
    bool BlockerExplanationsReady,
    bool EvidenceLogSummaryReady,
    bool ExternalGeneralReady,
    bool PublicSaasEnabled,
    bool PublicApiEnabled,
    bool RealBillingEnabled,
    bool RealEmailEnabled,
    bool RealCredentialsEnabled,
    bool SensitiveSitesEnabled,
    bool SubmitPaySignDeleteEnabled,
    bool RecorderReplayProductiveEnabled);

public interface INodalOsRuntimeStateProbe
{
    NodalOsReleaseGateStateSnapshot Probe();
}

public sealed record NodalOsLocalPrivatePreviewReleaseGateDecision(
    NodalOsLocalPrivatePreviewReleaseGateStatus Status,
    NodalOsLocalPrivatePreviewScope Scope,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<string> AllowedInternalActions,
    IReadOnlyList<string> BlockedActions,
    IReadOnlyList<string> EvidenceRefs,
    string NextStep,
    bool ReadyWithRestrictions,
    bool ExternalGeneralReady,
    bool PublicSaasStillDisabled,
    bool PublicApiStillDisabled,
    bool RealBillingStillDisabled,
    bool RealEmailStillDisabled,
    bool RealCredentialsStillBlocked,
    bool SensitiveSurfacesStillBlocked,
    bool SubmitPaySignDeleteStillBlocked,
    bool RecorderReplayProductiveStillBlocked,
    bool Redacted);

public enum NodalOsPrivatePreviewEvidenceFreezeStatus
{
    EvidenceFrozen,
    EvidenceMissing,
    ScopeInflationDetected,
    ReleaseGateMismatch,
    WorktreeMismatch,
    SkippedTestsMismatch,
    ReadyForExternalAudit
}

public sealed record NodalOsReleaseEvidenceSnapshot(
    string ProductName,
    string Commit,
    string Worktree,
    string Branch,
    string M51EvidenceScope,
    string M65EvidenceScope,
    string ReleaseGateDecision,
    IReadOnlyList<string> AllowedLocalPrivatePreviewScope,
    IReadOnlyList<string> DeniedPublicSensitiveScope,
    bool ExternalCdpGeneralReady,
    bool CanonicalWorktree,
    int SkippedTestsActual,
    int SkippedTestsExpected,
    bool PublicSaasAllowed,
    bool PublicApiAllowed,
    bool RealBillingAllowed,
    bool RealEmailAllowed,
    bool RealCredentialsAllowed,
    bool SensitiveSitesAllowed,
    bool SubmitPaySignDeleteAllowed,
    bool EvidenceLedgerVerified,
    bool Redacted);

public sealed record NodalOsPrivatePreviewEvidenceFreezeResult(
    NodalOsPrivatePreviewEvidenceFreezeStatus Status,
    NodalOsReleaseEvidenceSnapshot Snapshot,
    IReadOnlyList<string> ReasonCodes,
    bool ReadyForExternalAudit,
    bool ScopeInflationDetected,
    bool Redacted);

public enum NodalOsEvidenceLedgerVerificationStatus
{
    Verified,
    MissingLedgerRef,
    LedgerHashMismatch,
    PersistenceStatusMismatch,
    ScopeMismatch,
    UnsafeLedgerContent
}

public sealed record NodalOsEvidenceLedgerVerificationRequest(
    string ExpectedLedgerRef,
    string ExpectedLedgerHash,
    NexaExternalEvidencePersistenceStatus PersistenceStatus,
    string ExpectedScope,
    NexaExternalProofProbeKind ExpectedProbeKind);

public sealed record NodalOsEvidenceLedgerVerificationResult(
    NodalOsEvidenceLedgerVerificationStatus Status,
    string? LedgerRef,
    string? LedgerHash,
    IReadOnlyList<string> ReasonCodes,
    bool Verified,
    bool Redacted);
