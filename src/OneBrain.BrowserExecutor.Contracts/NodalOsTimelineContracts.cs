namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsTimelineStepStatus
{
    Pending,
    Planned,
    Ready,
    Running,
    Done,
    Blocked,
    NeedsHuman,
    EvidenceRequired,
    EvidenceReady,
    Skipped,
    Warning,
    Failed,
    NotAllowed
}

public enum NodalOsTimelineNodeType
{
    UserRequest,
    StructuredTask,
    RecipeStep,
    ExecutionStep,
    EvidenceStep,
    BlockerStep,
    ApprovalStep,
    OperatorAction,
    CoreDecision,
    SafeAction,
    DeniedAction,
    HumanIntervention,
    StatusSummary
}

public enum NodalOsTimelineRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical,
    Prohibited
}

public sealed record NodalOsTimelineNode(
    string NodeId,
    NodalOsTimelineNodeType NodeType,
    string? IconId);

public sealed record NodalOsTimelineEvidenceRef(
    string RefId,
    string Label,
    bool Redacted);

public sealed record NodalOsTimelineBlocker(
    string Reason,
    string ExpectedOperatorAction,
    IReadOnlyList<string> BlockedOptions,
    bool NeedsHuman,
    bool Redacted);

public sealed record NodalOsTimelineStatusCard(
    string ScopeLabel,
    NodalOsTimelineRiskLevel RiskLevel,
    string Decision,
    string Summary,
    bool ReadyWithRestrictions,
    bool ProductionReady,
    bool GrantsAuthority,
    bool Redacted);

public sealed record NodalOsTimelineDecision(
    string SafeNextAction,
    IReadOnlyList<string> BlockedOptions,
    bool CoreAuthorityRequired,
    bool HumanInterventionRequired,
    bool GrantsAuthority);

public sealed record NodalOsTimelineSubStep(
    string Title,
    string Description,
    NodalOsTimelineStepStatus Status,
    int Order,
    bool Redacted);

public sealed record NodalOsTimelineStep(
    string StepId,
    string Title,
    string Description,
    NodalOsTimelineStepStatus Status,
    int Order,
    NodalOsTimelineNode Node,
    IReadOnlyList<NodalOsTimelineSubStep> SubSteps,
    NodalOsTimelineStatusCard StatusCard,
    IReadOnlyList<NodalOsTimelineEvidenceRef> EvidenceRefs,
    IReadOnlyList<NodalOsTimelineBlocker> Blockers,
    NodalOsTimelineDecision Decision,
    NodalOsTimelineRiskLevel RiskLevel,
    string ScopeLabel,
    string RedactionSummary,
    bool CoreAuthorityRequired,
    bool HumanInterventionRequired,
    bool GrantsAuthority,
    bool Redacted);

public sealed record NodalOsTimeline(
    string TimelineId,
    string ProductName,
    string Title,
    string OriginalUserIntentSummary,
    IReadOnlyList<NodalOsTimelineStep> Steps,
    NodalOsTimelineStatusCard StatusCard,
    bool ReadyWithRestrictions,
    bool ProductionReady,
    bool GrantsAuthority,
    bool Redacted);

public sealed record NodalOsRecipeTimelineInput(
    string RecipeId,
    string RecipeName,
    IReadOnlyList<string> Steps);

public sealed record NodalOsExecutionTimelineStepInput(
    string Label,
    string Status,
    string? Error);

public sealed record NodalOsExecutionTimelineInput(
    string RunId,
    IReadOnlyList<NodalOsExecutionTimelineStepInput> Steps);

public sealed record NodalOsEvidenceTimelineInput(
    string SummaryId,
    IReadOnlyList<string> EvidenceRefs);

public sealed record NodalOsBlockerTimelineInput(
    string Reason,
    string ExpectedOperatorAction,
    IReadOnlyList<string> BlockedOptions,
    bool NeedsHuman);

public sealed record NodalOsOperatorSummaryTimelineInput(
    string CurrentDecision,
    string SafeNextAction,
    IReadOnlyList<string> ActiveBlockers,
    IReadOnlyList<string> EvidenceRefs);

public sealed record NodalOsPrivatePreviewRunTimelineInput(
    string RunId,
    string Decision,
    IReadOnlyList<string> AllowedFlows,
    IReadOnlyList<string> BlockedFlows,
    IReadOnlyList<string> EvidenceRefs);

public sealed record NodalOsIssueTriageTimelineInput(
    string IssueId,
    string Severity,
    string Category,
    string Decision,
    bool BlocksRun);

public enum NodalOsTimelineUxIssueCategory
{
    TimelineLayout,
    TimelineReadability,
    TimelineStatusConfusion,
    TimelineEvidenceClarity,
    TimelineBlockerClarity,
    TimelineNeedsHumanClarity,
    TimelineRecipeMapping,
    TimelineTaskStructuring,
    TimelineAccessibility,
    TimelineScopeInflationRisk,
    TimelineSecurityLeakRisk
}

public enum NodalOsTimelineUxIssueSeverity
{
    Critical,
    High,
    Medium,
    Low,
    Info
}

public enum NodalOsTimelineUxIssueDecision
{
    MustFixBeforeNextRun,
    ShouldFixSoon,
    AcceptForInternalOnly,
    NotAProblem,
    NeedsAudit
}

public sealed record NodalOsTimelineUxIssue(
    string IssueId,
    NodalOsTimelineUxIssueCategory Category,
    NodalOsTimelineUxIssueSeverity Severity,
    NodalOsTimelineUxIssueDecision Decision,
    string Summary,
    bool BlocksTimelineStabilization,
    bool Redacted);

public enum NodalOsTimelineStabilizationDecision
{
    TimelineStableForInternalPreview,
    TimelineContinueWithMinorFixes,
    TimelineBlockedBySecurityLeak,
    TimelineBlockedByScopeInflation,
    TimelineNeedsAccessibilityFixes,
    TimelineNeedsLayoutFixes
}

public sealed record NodalOsTimelineInternalPreviewRun(
    string RunId,
    string Commit,
    string Scope,
    IReadOnlyList<string> TimelineSurfacesReviewed,
    IReadOnlyList<string> AllowedFlows,
    IReadOnlyList<string> BlockedFlows,
    IReadOnlyList<string> VisualUxFindings,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<NodalOsTimelineUxIssue> Issues,
    string Decision,
    bool ScopeExpanded,
    bool Redacted);

public sealed record NodalOsTimelineStabilizationReview(
    string ReviewId,
    NodalOsTimelineStabilizationDecision Decision,
    IReadOnlyList<NodalOsTimelineUxIssue> Issues,
    bool BlockersVisible,
    bool EvidenceRedacted,
    bool NeedsHumanClear,
    bool CoreAuthorityVisible,
    bool UiAuthorizesActions,
    bool ScopeExpanded,
    bool Redacted);
