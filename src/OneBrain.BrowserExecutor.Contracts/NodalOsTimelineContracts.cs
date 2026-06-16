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

public enum NodalOsPlanRisk
{
    None,
    Low,
    Medium,
    High,
    Critical,
    Prohibited
}

public enum NodalOsPlanApprovalRequirement
{
    None,
    CoreApprovalRequired,
    HumanApprovalRequired,
    DedicatedEvidenceRequired,
    AlwaysBlocked
}

public enum NodalOsPlanDomainScope
{
    LocalPrivatePreview,
    TargetOwnedReadOnly,
    ExternalGeneralBlocked,
    SensitiveBlocked,
    ProductionBlocked
}

public enum NodalOsPlanSensitiveAction
{
    None,
    CredentialEntry,
    Login,
    Captcha,
    TwoFactor,
    Submit,
    Payment,
    Sign,
    Delete,
    SensitiveSite,
    ProductiveRecorderReplay
}

public enum NodalOsPlanPreviewStatus
{
    PlanDrafted,
    PlanPreviewReady,
    PlanAwaitingApproval,
    PlanApproved,
    PlanRejected,
    PlanEditedByHuman,
    ExecutionStarted,
    ExecutionBlockedByPolicy
}

public enum NodalOsPlanEvidenceRequirement
{
    None,
    RedactedEvidenceRef,
    LedgerRef,
    PolicyDecisionRef,
    HumanApprovalRef,
    DedicatedEvidenceRequired
}

public sealed record NodalOsPlanPolicySummary(
    bool CoreAuthorityRequired,
    bool UiAuthorityBlocked,
    bool AutoExecutionBlocked,
    bool SensitiveActionsBlocked,
    bool ProductionBlocked,
    bool ExternalGeneralBlocked,
    IReadOnlyList<string> BlockedOptions,
    bool Redacted);

public sealed record NodalOsExecutionPlanStep(
    string StepId,
    int Order,
    string Title,
    string Description,
    NodalOsPlanRisk Risk,
    NodalOsPlanApprovalRequirement ApprovalRequirement,
    IReadOnlyList<NodalOsPlanEvidenceRequirement> EvidenceRequirements,
    IReadOnlyList<NodalOsPlanSensitiveAction> SensitiveActionsDetected,
    bool HumanApprovalRequired,
    bool CoreAuthorityRequired,
    bool ExecutesAutomatically,
    string TimelineNodeType,
    string RedactionSummary);

public sealed record NodalOsExecutionPlanPreview(
    string PlanId,
    string Goal,
    NodalOsPlanPreviewStatus Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<NodalOsExecutionPlanStep> Steps,
    IReadOnlyList<string> AllowedDomains,
    IReadOnlyList<string> DeniedDomains,
    IReadOnlyList<NodalOsPlanRisk> Risks,
    IReadOnlyList<NodalOsPlanApprovalRequirement> ApprovalRequirements,
    IReadOnlyList<NodalOsPlanEvidenceRequirement> EvidenceRequirements,
    NodalOsPlanPolicySummary PolicySummary,
    IReadOnlyList<NodalOsPlanSensitiveAction> SensitiveActionsDetected,
    bool HumanApprovalRequired,
    bool CoreAuthorityRequired,
    bool UiAuthorityBlocked,
    bool ExecutesAutomatically,
    string TimelineCompatibilityMapping,
    string RedactionSummary,
    bool Redacted);

public enum NodalOsStagnationKind
{
    RepeatedUrl,
    RepeatedDomHash,
    RepeatedScreenshotHash,
    RepeatedAction,
    SelectorRepeatedFailure,
    ScrollNoProgress,
    ClickNoVisualChange,
    InputAlreadyApplied,
    RepeatedRuntimeError,
    ModalUnexpected,
    PageNotLoaded,
    CaptchaLoginTwoFactorDetected,
    SameTargetRepeatedAction
}

public enum NodalOsStagnationSeverity
{
    Info,
    Warning,
    Blocked
}

public enum NodalOsRecoveryRecommendation
{
    Retry,
    Replan,
    AskHuman,
    StopWithEvidence
}

public sealed record NodalOsRuntimeProgressSnapshot(
    string RuntimeId,
    string? TabId,
    string? StepId,
    string Url,
    string DomHash,
    string ScreenshotHash,
    string Action,
    string Selector,
    string Error,
    bool VisualChanged,
    bool PageLoaded,
    bool CaptchaLoginTwoFactorDetected,
    DateTimeOffset CreatedAtUtc,
    bool Redacted);

public sealed record NodalOsRuntimeStagnationSignal(
    string SignalId,
    string RuntimeId,
    string? TabId,
    string? StepId,
    NodalOsStagnationKind Kind,
    NodalOsStagnationSeverity Severity,
    int ObservedCount,
    int Threshold,
    NodalOsRecoveryRecommendation Recommendation,
    IReadOnlyList<string> EvidenceRefs,
    DateTimeOffset CreatedAtUtc,
    string RedactionSummary,
    bool GrantsAuthority,
    bool Redacted);

public enum NodalOsRecoveryState
{
    RecoveryRequired,
    WaitingForHumanInput,
    ReplanSuggested,
    RetrySuggested,
    StopSuggested,
    PartialResultAvailable,
    BlockedByPolicy,
    BlockedBySensitiveSurface,
    BlockedByCredentials,
    BlockedByCaptchaLoginTwoFactor
}

public sealed record NodalOsRecoveryOption(
    string OptionId,
    string Label,
    bool Safe,
    bool RequiresCoreAuthority,
    bool RequiresHumanInput,
    bool ExecutesSensitiveWorkaround);

public sealed record NodalOsRecoveryExplanation(
    string Cause,
    string OperatorMessage,
    string RequiredHumanAction,
    IReadOnlyList<string> EvidenceRefs,
    string RedactionSummary,
    bool Redacted);

public sealed record NodalOsRecoveryDecision(
    string RecoveryId,
    NodalOsRecoveryState State,
    NodalOsRuntimeStagnationSignal Signal,
    NodalOsRecoveryExplanation Explanation,
    IReadOnlyList<NodalOsRecoveryOption> Options,
    string NextSafeAction,
    bool CoreAuthorityRequired,
    bool UiAuthorityBlocked,
    bool GrantsAuthority,
    bool Redacted);
