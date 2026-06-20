namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsAssignmentPurpose
{
    MissionPlanningDraft,
    TaskBreakdownDraft,
    RiskReviewDraft,
    NextStepsDraft,
    HandoffPlanningDraft,
    ExpertAdvisorSuggestionDraft,
    Unknown
}

public enum NodalOsAssignmentState
{
    NotAllowed,
    DraftOnly,
    EligibleForManualDraft,
    EligibleForFutureLlmDraftWithConsent,
    BlockedByMissingWorkspace,
    BlockedByMissingContext,
    BlockedBySafeContextBoundary,
    BlockedByPromptGovernance,
    BlockedByBudgetPolicy,
    BlockedByByokPolicy,
    BlockedByModelCapability,
    BlockedByHumanReview,
    UnknownRequiresReview
}

public enum NodalOsAssignmentTaskGraphStatus
{
    DraftOnly,
    NeedsReview,
    Blocked,
    Deferred,
    ReadyForManualReview,
    NotExecutable,
    ArchivedMock
}

public enum NodalOsAssignmentTaskKind
{
    AnalysisDraft,
    DocumentationDraft,
    PlanningDraft,
    ReviewDraft,
    RiskAssessmentDraft,
    HandoffDraft,
    AdvisorSuggestionDraft,
    FutureExecutionPlaceholder,
    Unknown
}

public enum NodalOsAssignmentTaskStatus
{
    Draft,
    NeedsReview,
    Blocked,
    Deferred,
    ReadyForManualReview,
    NotExecutable,
    ArchivedMock
}

public enum NodalOsAssignmentRiskLevel
{
    Low,
    Medium,
    High,
    UnknownRequiresReview
}

public enum NodalOsSuggestedAssigneeType
{
    HumanOperator,
    FutureAdvisor,
    FutureAssignmentPlanner,
    Unknown
}

public enum NodalOsPlannerReadinessState
{
    NotReady,
    ReadyForManualDraftOnly,
    ReadyForContextReviewOnly,
    ReadyForFutureLlmPlanningWithConsent,
    BlockedByMissingWorkspace,
    BlockedByMissingContext,
    BlockedByPromptGovernance,
    BlockedByBudgetPolicy,
    BlockedByByokProvider,
    BlockedByModelCapability,
    BlockedBySensitiveContext,
    BlockedBySecretContext,
    BlockedByPositiveExecutionGate,
    UnknownRequiresReview
}

public enum NodalOsPlanningMode
{
    ManualDraftOnly,
    MockDraftOnly,
    FutureLlmDraftWithConsent,
    FutureAdvisorSuggestion,
    FutureAssignmentPlanner,
    FutureRuntimeExecution,
    NotAllowed
}

public sealed record NodalOsAssignmentRequestDraft
{
    public required string AssignmentRequestId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string MissionId { get; init; }

    public IReadOnlyList<string> UserProvidedContextRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> SafeContextBoundaryRefs { get; init; } = [];

    public IReadOnlyList<string> ProjectUnderstandingReadinessRefs { get; init; } = [];

    public IReadOnlyList<string> PromptGovernanceRefs { get; init; } = [];

    public IReadOnlyList<string> BudgetPolicyRefs { get; init; } = [];

    public IReadOnlyList<string> ModelCapabilityProfileRefs { get; init; } = [];

    public required string PlannerReadinessRef { get; init; }

    public required NodalOsAssignmentPurpose RequestedPlanningPurpose { get; init; }

    public IReadOnlyList<string> AllowedPlanningScopeRedacted { get; init; } = [];

    public IReadOnlyList<string> DeniedPlanningScopeRedacted { get; init; } = [];

    public required NodalOsAssignmentState AssignmentState { get; init; }

    public required string HumanReviewRequirementRedacted { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> DisclosuresRedacted { get; init; } = [];

    public required bool DraftOnly { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CallsModelPlanner { get; init; }

    public required bool ExecutesTasks { get; init; }

    public required bool CreatesAuthoritativeTasks { get; init; }

    public required bool MutatesWorkspace { get; init; }

    public required bool MutatesRuntimeRegistry { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsAssignmentTaskDraft
{
    public required string TaskId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string SummaryRedacted { get; init; }

    public required NodalOsAssignmentTaskKind TaskKind { get; init; }

    public required NodalOsAssignmentTaskStatus Status { get; init; }

    public IReadOnlyList<string> DependencyIds { get; init; } = [];

    public IReadOnlyList<string> BlockedByIds { get; init; } = [];

    public required NodalOsAssignmentRiskLevel RiskLevel { get; init; }

    public IReadOnlyList<string> AllowedCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public required NodalOsSuggestedAssigneeType SuggestedAssigneeType { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public required bool RequiresApproval { get; init; }

    public required bool RequiresLlmFuture { get; init; }

    public required bool RequiresRuntimeFuture { get; init; }

    public required bool RequiresFilesystemFuture { get; init; }

    public required bool CanExecute { get; init; }
}

public sealed record NodalOsTaskGraphDraft
{
    public required string TaskGraphId { get; init; }

    public required string AssignmentRequestId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string MissionId { get; init; }

    public required NodalOsAssignmentTaskGraphStatus GraphStatus { get; init; }

    public IReadOnlyList<NodalOsAssignmentTaskDraft> Tasks { get; init; } = [];

    public IReadOnlyList<string> DependenciesRedacted { get; init; } = [];

    public IReadOnlyList<string> RiskNotesRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ApprovalRefs { get; init; } = [];

    public IReadOnlyList<string> ContextRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required string HumanReviewRequirementRedacted { get; init; }

    public required string ReadinessGateResultRedacted { get; init; }

    public required bool DraftOnly { get; init; }

    public required bool Executable { get; init; }

    public required bool ResolvesDependenciesProductively { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CallsRuntime { get; init; }

    public required bool TouchesFilesystem { get; init; }

    public required bool CreatesAuthoritativeApproval { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsPlannerReadinessGateResult
{
    public required string PlannerReadinessId { get; init; }

    public required string WorkspaceReadinessRef { get; init; }

    public required string ProjectUnderstandingReadinessRef { get; init; }

    public required string ContextValidationSummaryRef { get; init; }

    public required string PromptGovernanceRef { get; init; }

    public required string BudgetGuardrailsRef { get; init; }

    public required string ByokProviderSettingsRef { get; init; }

    public required string ModelCapabilityMatrixRef { get; init; }

    public required string SafeContextBoundaryRef { get; init; }

    public required string HumanReviewRequirementRedacted { get; init; }

    public required NodalOsPlannerReadinessState ReadinessState { get; init; }

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsPlanningMode> AllowedNextPlanningModes { get; init; } = [];

    public IReadOnlyList<NodalOsPlanningMode> DisabledPlanningModes { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CreatesAuthoritativePlan { get; init; }

    public required bool ExecutesRuntime { get; init; }

    public required bool PerformsWorkDispatch { get; init; }

    public required bool FutureRuntimeExecutionBlocked { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
