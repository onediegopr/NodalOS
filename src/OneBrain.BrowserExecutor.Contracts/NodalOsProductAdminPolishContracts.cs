namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsProductAdminPolishState
{
    ProductAdminPreviewStable,
    Hito162ReplacementStable,
    LocalFixtureSignalsReady,
    ActionAuthorityCoreOnly,
    ExternalGeneralStillBlocked,
    ProductionStillBlocked
}

public enum NodalOsOperatorUxDecision
{
    ReadyWithRestrictions,
    ContinueInternalPreviewStable,
    ContinueWithMinorFixes,
    NeedsProductAdminPolish,
    NeedsOperatorUxFixes,
    BlockedByScopeInflation,
    BlockedBySecurityIssue
}

public sealed record NodalOsProductAdminPolishSummary(
    string ProductName,
    string CurrentState,
    string M51Status,
    string M65Status,
    string Hito162ReplacementStatus,
    IReadOnlyList<string> ReadinessSignals,
    IReadOnlyList<NodalOsProductAdminPolishState> VisibleStates,
    bool ExternalGeneralCdpReady,
    bool ProductionReady,
    bool PublicSaasReady,
    bool RecorderReplayProductiveEnabled,
    bool CoreAuthorityRequired,
    IReadOnlyList<string> ActiveBlockers,
    string NextRecommendedAction,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted);

public sealed record NodalOsOperatorUxDecisionClaritySummary(
    string ProductName,
    NodalOsOperatorUxDecision CurrentDecision,
    string AllowedNextAction,
    string BlockedNextAction,
    string WhyBlocked,
    IReadOnlyList<string> EvidenceRefs,
    bool HumanInterventionRequired,
    string IssueReportingPath,
    IReadOnlyList<string> StopConditions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> ActiveBlockers,
    bool Redacted);

public sealed record NodalOsInternalPreviewIterationRunRecord(
    string RunId,
    DateTimeOffset TimestampUtc,
    string Commit,
    string Scope,
    NodalOsProductAdminPolishSummary ProductAdminSummary,
    NodalOsOperatorUxDecisionClaritySummary OperatorSummary,
    IReadOnlyList<string> Hito162ReplacementSignals,
    IReadOnlyList<string> AllowedFlowsExecuted,
    IReadOnlyList<string> BlockedFlowsObserved,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<NodalOsPrivatePreviewIssue> Issues,
    NodalOsOperatorUxDecision Decision,
    bool ScopeExpanded,
    bool ProofLiveExecuted,
    bool Redacted);

