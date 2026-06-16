namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsLocalPreviewReleaseCandidateState
{
    LocalPreviewReleaseCandidate,
    FrozenReadyForExternalAudit,
    BlockedByScopeInflation,
    BlockedByMissingEvidence,
    BlockedBySecurity,
    BlockedByTests,
    BlockedByWorktree
}

public enum NodalOsReleaseCandidateDecision
{
    FrozenReadyForExternalAudit,
    BlockedByScopeInflation,
    BlockedByMissingEvidence,
    BlockedBySecurity,
    BlockedByTests,
    BlockedByWorktree
}

public sealed record NodalOsReleaseCandidateScope(
    IReadOnlyList<string> AllowedScope,
    IReadOnlyList<string> DeniedScope,
    bool ExternalGeneralCdpReady,
    bool ProductionEnabled,
    bool PublicSaasEnabled,
    bool PublicApiEnabled,
    bool BillingEmailEnabled,
    bool RealCredentialsEnabled,
    bool SensitiveSitesEnabled,
    bool SubmitPaySignDeleteEnabled,
    bool RecorderReplayProductiveEnabled);

public sealed record NodalOsReleaseCandidateBlocker(
    string Code,
    string Description,
    bool BlocksReleaseCandidate);

public sealed record NodalOsLocalPreviewReleaseCandidate(
    string ProductName,
    string Commit,
    string Worktree,
    string Branch,
    string ReadinessState,
    string M51Status,
    string M65Status,
    string Hito162Status,
    string ProductAdminStatus,
    string OperatorUxStatus,
    bool TestsOk,
    bool SkippedCategoriesOk,
    bool WorktreeCanonical,
    bool M51M65EvidenceVerified,
    bool ProductAdminStable,
    bool OperatorUxStable,
    bool Hito162ReplacementStable,
    bool HasHighOrCriticalIssues,
    NodalOsReleaseCandidateScope Scope,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> PrivatePreviewRuns,
    bool Redacted);

public sealed record NodalOsReleaseCandidateFreeze(
    NodalOsLocalPreviewReleaseCandidateState State,
    NodalOsReleaseCandidateDecision Decision,
    NodalOsLocalPreviewReleaseCandidate Candidate,
    IReadOnlyList<NodalOsReleaseCandidateBlocker> Blockers,
    IReadOnlyList<string> ReasonCodes,
    bool ReadyForClaudeAudit,
    bool ScopeInflationDetected,
    bool Redacted);
