namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaCanonicalWorkspaceGuardDecisionKind
{
    Allowed,
    Blocked
}

public sealed record NexaCanonicalWorkspaceGuardConfig(
    string ExpectedWorkspacePath,
    string ExpectedRemoteBranch,
    string ExpectedRemoteHead,
    IReadOnlyList<string> LegacyWorkspacePaths);

public sealed record NexaCanonicalWorkspaceSnapshot(
    string WorkspacePath,
    string? CurrentBranch,
    string Head,
    string ExpectedRemoteHead,
    bool IsDirty,
    bool IsDetachedHead,
    IReadOnlyList<string> StatusEntries);

public sealed record NexaCanonicalWorkspaceGuardResult(
    NexaCanonicalWorkspaceGuardDecisionKind Decision,
    string WorkspacePath,
    string ExpectedWorkspacePath,
    string Head,
    string ExpectedRemoteHead,
    string ExpectedRemoteBranch,
    bool IsDirty,
    bool IsLegacyPath,
    bool MatchesRemoteHead,
    bool DetachedHeadAccepted,
    IReadOnlyList<string> BlockingReasons,
    string OperatorMessage,
    bool ModifiedWorkspace);

public enum NexaReadinessArea
{
    Engineering,
    BrowserRuntimeLocal,
    PrivateLocalApi,
    Vault,
    SecurityLeak,
    Operational,
    ExternalLive
}

public sealed record NexaPrivatePreviewReadinessMetric(
    NexaReadinessArea Area,
    int Percent,
    bool Estimated,
    string Summary);

public sealed record NexaPrivatePreviewReadinessDecision(
    bool LocalPreviewAllowed,
    bool ExternalLiveAllowed,
    bool PublicSaasAllowed,
    bool RealBillingAllowed,
    bool RealEmailAllowed,
    bool RealClientCredentialsAllowed,
    bool SensitiveRealPilotAllowed,
    bool SubmitPaySignDeleteAllowed,
    string GoNoGoLocal,
    string GoNoGoExternalLive);

public sealed record NexaPrivatePreviewReadinessDashboard(
    string DashboardId,
    IReadOnlyList<NexaPrivatePreviewReadinessMetric> Metrics,
    IReadOnlyList<string> ActiveBlockers,
    IReadOnlyList<NexaSkippedTestAuditItem> RelevantSkippedTests,
    NexaPrivatePreviewReadinessDecision Decision,
    bool M51Deferred,
    bool M65Blocked,
    bool Redacted);

public enum NexaOperatorBlockerCategory
{
    Local,
    ExternalTargetMissing,
    Credential,
    Security,
    Compliance,
    Worktree,
    Billing,
    Email,
    PublicSaas,
    CorePermission
}

public enum NexaOperatorBlockerScenario
{
    MissingTestOwnedExternalTarget,
    RealCredentialsBlocked,
    IrreversibleActionBlocked,
    RealBillingBlocked,
    RealEmailBlocked,
    PublicSaasBlocked,
    NonCanonicalWorktree,
    SkippedTestsBlockExternalLive,
    CorePermissionMissing
}

public sealed record NexaOperatorBlockerExplanation(
    NexaOperatorBlockerScenario Scenario,
    NexaOperatorBlockerCategory Category,
    string Cause,
    string UserExpectedAction,
    IReadOnlyList<string> SafeOptions,
    IReadOnlyList<string> BlockedOptions,
    IReadOnlyList<string> EvidenceRefs,
    string RecommendedNextStep,
    string OperatorReadableMessage,
    bool Redacted);
