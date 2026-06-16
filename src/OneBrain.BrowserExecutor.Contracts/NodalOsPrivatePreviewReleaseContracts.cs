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
