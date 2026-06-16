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

public enum NodalOsRuntimeServiceState
{
    DisabledByDesign,
    DisabledByAbsence,
    Enabled,
    VerifiedReady,
    Unknown
}

public sealed record NodalOsRuntimeServiceEvidence(
    string ServiceName,
    NodalOsRuntimeServiceState State,
    bool DangerousIfEnabled,
    string EvidenceRef);

public sealed record NodalOsRuntimeStateVerificationResult(
    NodalOsReleaseGateStateSnapshot Snapshot,
    IReadOnlyList<NodalOsRuntimeServiceEvidence> Services,
    IReadOnlyList<string> ReasonCodes,
    bool VerifiedFromRuntimeState,
    bool DangerousServiceEnabled,
    bool Redacted);

public enum NodalOsLedgerLiveVerificationStatus
{
    Verified,
    MissingLedgerRef,
    LedgerHashMismatch,
    ProbeKindMismatch,
    ToolingMismatch,
    PersistenceStatusMismatch,
    UnsafeLedgerContent,
    ScopeMismatch
}

public sealed record NodalOsExpectedLedgerProof(
    string ProofName,
    string LedgerRef,
    string LedgerHash,
    NexaExternalProofProbeKind ProbeKind,
    string Tooling,
    NexaExternalEvidencePersistenceStatus PersistenceStatus,
    string ExpectedScope);

public sealed record NodalOsLedgerLiveVerificationResult(
    string ProofName,
    NodalOsLedgerLiveVerificationStatus Status,
    string? LedgerRef,
    string? LedgerHash,
    IReadOnlyList<string> ReasonCodes,
    bool Verified,
    bool Redacted);

public sealed record NodalOsM51M65LedgerVerificationResult(
    NodalOsLedgerLiveVerificationResult M51,
    NodalOsLedgerLiveVerificationResult M65,
    bool Verified,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public enum NodalOsSkippedCategoryRuntimeAuditStatus
{
    Passed,
    CountMismatch,
    CategoryMismatch,
    LocalPreviewSkipDetected,
    MissingRuntimeReport
}

public sealed record NodalOsSkippedCategoryRuntimeAuditResult(
    NodalOsSkippedCategoryRuntimeAuditStatus Status,
    int ExpectedCount,
    int ActualCount,
    IReadOnlySet<NexaSkippedTestCategory> ExpectedCategories,
    IReadOnlySet<NexaSkippedTestCategory> ActualCategories,
    IReadOnlyList<string> ReasonCodes,
    bool Passed,
    bool Redacted);

public enum NodalOsVerifiedReleaseCandidateFreezeState
{
    FrozenReadyForInternalLocalUseVerified,
    FrozenReadyForExternalAuditVerified,
    BlockedByRuntimeState,
    BlockedByMissingEvidence,
    BlockedByTests,
    BlockedByScopeInflation,
    BlockedByWorktree,
    SelfReportedSnapshotRejected
}

public sealed record NodalOsVerifiedReleaseCandidateFreezeResult(
    NodalOsVerifiedReleaseCandidateFreezeState State,
    NodalOsReleaseCandidateFreeze Freeze,
    NodalOsRuntimeStateVerificationResult RuntimeVerification,
    NodalOsM51M65LedgerVerificationResult LedgerVerification,
    NodalOsSkippedCategoryRuntimeAuditResult SkippedAudit,
    IReadOnlyList<string> ReasonCodes,
    bool VerifiedForInternalLocalUse,
    bool Redacted);
