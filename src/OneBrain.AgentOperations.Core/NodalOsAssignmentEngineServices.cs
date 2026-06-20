using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAssignmentEngineDraftService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsAssignmentRequestDraft CreateAssignmentRequest(
        NodalOsAssignmentState state = NodalOsAssignmentState.DraftOnly,
        NodalOsAssignmentPurpose purpose = NodalOsAssignmentPurpose.MissionPlanningDraft,
        string workspaceId = "workspace-readonly-fixture",
        string missionId = "mission-readonly-fixture",
        string userContextRef = "user-context-ref-safe")
    {
        return new()
        {
            AssignmentRequestId = $"assignment-request-{purpose}-{state}",
            WorkspaceId = SafeValue(workspaceId),
            MissionId = SafeValue(missionId),
            UserProvidedContextRefsRedacted = [SafeValue(userContextRef)],
            SafeContextBoundaryRefs = ["safe-context-boundary-required"],
            ProjectUnderstandingReadinessRefs = ["project-understanding-readiness-ref"],
            PromptGovernanceRefs = ["prompt-governance-ref"],
            BudgetPolicyRefs = ["budget-policy-ref"],
            ModelCapabilityProfileRefs = ["model-capability-profile-ref"],
            PlannerReadinessRef = "planner-readiness-ref",
            RequestedPlanningPurpose = purpose,
            AllowedPlanningScopeRedacted = ["manual draft preview", "risk review draft", "next safe step draft"],
            DeniedPlanningScopeRedacted = ["runtime execution", "authoritative task creation", "provider-driven plan"],
            AssignmentState = state,
            HumanReviewRequirementRedacted = "Human review required before any future assignment planning can become operational.",
            EvidenceRefs = [EvidenceRef("evidence-assignment-request-ref-only")],
            TimelineRefs = ["timeline-assignment-request-ref-only"],
            GuardrailRefs =
            [
                "guardrail-no-model-call",
                "guardrail-no-prompt",
                "guardrail-no-runtime",
                "guardrail-taskgraph-draft-only"
            ],
            DisclosuresRedacted =
            [
                "Assignment Engine runtime is not implemented.",
                "No model was called.",
                "No prompt was generated.",
                "No task is executable.",
                "TaskGraph is draft-only."
            ],
            DraftOnly = true,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            CallsModelPlanner = false,
            ExecutesTasks = false,
            CreatesAuthoritativeTasks = false,
            MutatesWorkspace = false,
            MutatesRuntimeRegistry = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsTaskGraphDraft CreateTaskGraphDraft(NodalOsAssignmentRequestDraft request)
    {
        var tasks = new[]
        {
            Task("workitem-analysis-draft", "Analyze provided context", NodalOsAssignmentTaskKind.AnalysisDraft, NodalOsAssignmentRiskLevel.Low),
            Task("workitem-documentation-draft", "Draft documentation notes", NodalOsAssignmentTaskKind.DocumentationDraft, NodalOsAssignmentRiskLevel.Low, ["workitem-analysis-draft"]),
            Task("workitem-planning-draft", "Prepare manual planning outline", NodalOsAssignmentTaskKind.PlanningDraft, NodalOsAssignmentRiskLevel.Medium, ["workitem-analysis-draft"]),
            Task("workitem-hazard-assessment-draft", "Review risk notes", NodalOsAssignmentTaskKind.RiskAssessmentDraft, NodalOsAssignmentRiskLevel.Medium),
            Task("workitem-handoff-draft", "Prepare handoff summary", NodalOsAssignmentTaskKind.HandoffDraft, NodalOsAssignmentRiskLevel.Low, ["workitem-documentation-draft"]),
            Task("workitem-advisor-suggestion-draft", "Collect advisor suggestion placeholders", NodalOsAssignmentTaskKind.AdvisorSuggestionDraft, NodalOsAssignmentRiskLevel.Medium),
            Task("workitem-future-execution-placeholder", "Future execution placeholder blocked", NodalOsAssignmentTaskKind.FutureExecutionPlaceholder, NodalOsAssignmentRiskLevel.High, status: NodalOsAssignmentTaskStatus.Blocked, requiresRuntimeFuture: true)
        };

        return new()
        {
            TaskGraphId = $"taskgraph-draft-{request.AssignmentRequestId}",
            AssignmentRequestId = request.AssignmentRequestId,
            WorkspaceId = request.WorkspaceId,
            MissionId = request.MissionId,
            GraphStatus = NodalOsAssignmentTaskGraphStatus.DraftOnly,
            Tasks = tasks,
            DependenciesRedacted = ["workitem-documentation-draft depends on workitem-analysis-draft", "workitem-planning-draft depends on workitem-analysis-draft"],
            RiskNotesRedacted = ["FutureExecutionPlaceholder remains blocked.", "No task can execute."],
            EvidenceRefs = [EvidenceRef("evidence-taskgraph-draft-ref-only")],
            TimelineRefs = ["timeline-taskgraph-draft-ref-only"],
            ApprovalRefs = ["approval-ref-preview-only"],
            ContextRefsRedacted = request.UserProvidedContextRefsRedacted,
            GuardrailRefs =
            [
                "guardrail-all-tasks-can-execute-false",
                "guardrail-no-runtime",
                "guardrail-no-provider-call",
                "guardrail-no-authoritative-approval"
            ],
            HumanReviewRequirementRedacted = "Human review required before any future assignment output can be promoted.",
            ReadinessGateResultRedacted = "Planner readiness gate required; draft-only result.",
            DraftOnly = true,
            Executable = false,
            ResolvesDependenciesProductively = false,
            CallsLlmProvider = false,
            CallsRuntime = false,
            TouchesFilesystem = false,
            CreatesAuthoritativeApproval = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsPlannerReadinessGateResult CreatePlannerReadinessGate(
        NodalOsPlannerReadinessState state = NodalOsPlannerReadinessState.ReadyForManualDraftOnly)
    {
        return new()
        {
            PlannerReadinessId = $"planner-readiness-{state}",
            WorkspaceReadinessRef = "workspace-readiness-ref",
            ProjectUnderstandingReadinessRef = "project-understanding-readiness-ref",
            ContextValidationSummaryRef = "context-validation-summary-ref",
            PromptGovernanceRef = "prompt-governance-ref",
            BudgetGuardrailsRef = "budget-guardrails-ref",
            ByokProviderSettingsRef = "byok-provider-settings-ref",
            ModelCapabilityMatrixRef = "model-capability-matrix-ref",
            SafeContextBoundaryRef = "safe-context-boundary-ref",
            HumanReviewRequirementRedacted = "Human review required for future LLM planning and any promotion beyond draft.",
            ReadinessState = state,
            BlockersRedacted = BlockersFor(state),
            WarningsRedacted = ["FutureRuntimeExecution is blocked.", "Future LLM planning is consent-gated and still no-op."],
            AllowedNextPlanningModes = AllowedModesFor(state),
            DisabledPlanningModes = DisabledModesFor(state),
            EvidenceRefs = [EvidenceRef("evidence-planner-readiness-ref-only")],
            TimelineRefs = ["timeline-planner-readiness-ref-only"],
            GuardrailRefs =
            [
                "guardrail-no-llm-call",
                "guardrail-no-prompt",
                "guardrail-no-authoritative-plan",
                "guardrail-future-runtime-execution-blocked"
            ],
            CallsLlmProvider = false,
            CreatesPrompt = false,
            CreatesAuthoritativePlan = false,
            ExecutesRuntime = false,
            PerformsWorkDispatch = false,
            FutureRuntimeExecutionBlocked = true,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private NodalOsAssignmentTaskDraft Task(
        string id,
        string title,
        NodalOsAssignmentTaskKind kind,
        NodalOsAssignmentRiskLevel risk,
        IReadOnlyList<string>? dependencies = null,
        NodalOsAssignmentTaskStatus status = NodalOsAssignmentTaskStatus.Draft,
        bool requiresRuntimeFuture = false)
    {
        var blocked = kind == NodalOsAssignmentTaskKind.FutureExecutionPlaceholder
            ? ["positive-execution-gate", "runtime-not-implemented"]
            : Array.Empty<string>();

        return new()
        {
            TaskId = id,
            TitleRedacted = SafeValue(title),
            SummaryRedacted = "Draft-only assignment task. No execution authority.",
            TaskKind = kind,
            Status = kind == NodalOsAssignmentTaskKind.FutureExecutionPlaceholder ? NodalOsAssignmentTaskStatus.Blocked : status,
            DependencyIds = dependencies ?? [],
            BlockedByIds = blocked,
            RiskLevel = risk,
            AllowedCapabilitiesRedacted = ["manual review", "read-only preview"],
            DisabledCapabilitiesRedacted = ["execution", "runtime", "LLM call", "filesystem access"],
            SuggestedAssigneeType = kind == NodalOsAssignmentTaskKind.AdvisorSuggestionDraft
                ? NodalOsSuggestedAssigneeType.FutureAdvisor
                : NodalOsSuggestedAssigneeType.HumanOperator,
            EvidenceRefs = [EvidenceRef($"evidence-{id}")],
            TimelineRefs = [$"timeline-{id}"],
            RequiresApproval = true,
            RequiresLlmFuture = kind == NodalOsAssignmentTaskKind.AdvisorSuggestionDraft,
            RequiresRuntimeFuture = requiresRuntimeFuture,
            RequiresFilesystemFuture = false,
            CanExecute = false
        };
    }

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("sk-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }

    private static IReadOnlyList<string> BlockersFor(NodalOsPlannerReadinessState state) => state switch
    {
        NodalOsPlannerReadinessState.BlockedByMissingWorkspace => ["Workspace readiness missing."],
        NodalOsPlannerReadinessState.BlockedByMissingContext => ["Safe user context missing."],
        NodalOsPlannerReadinessState.BlockedByPromptGovernance => ["Prompt governance missing."],
        NodalOsPlannerReadinessState.BlockedByBudgetPolicy => ["Budget policy missing."],
        NodalOsPlannerReadinessState.BlockedByByokProvider => ["BYOK provider settings missing."],
        NodalOsPlannerReadinessState.BlockedByModelCapability => ["Model capability matrix missing."],
        NodalOsPlannerReadinessState.BlockedBySensitiveContext => ["Sensitive context blocked."],
        NodalOsPlannerReadinessState.BlockedBySecretContext => ["Credential-like context blocked."],
        NodalOsPlannerReadinessState.BlockedByPositiveExecutionGate => ["Positive execution gate missing."],
        NodalOsPlannerReadinessState.UnknownRequiresReview => ["Unknown readiness requires review."],
        _ => []
    };

    private static IReadOnlyList<NodalOsPlanningMode> AllowedModesFor(NodalOsPlannerReadinessState state) => state switch
    {
        NodalOsPlannerReadinessState.ReadyForManualDraftOnly => [NodalOsPlanningMode.ManualDraftOnly, NodalOsPlanningMode.MockDraftOnly],
        NodalOsPlannerReadinessState.ReadyForContextReviewOnly => [NodalOsPlanningMode.MockDraftOnly],
        NodalOsPlannerReadinessState.ReadyForFutureLlmPlanningWithConsent => [NodalOsPlanningMode.ManualDraftOnly, NodalOsPlanningMode.MockDraftOnly, NodalOsPlanningMode.FutureLlmDraftWithConsent],
        _ => [NodalOsPlanningMode.NotAllowed]
    };

    private static IReadOnlyList<NodalOsPlanningMode> DisabledModesFor(NodalOsPlannerReadinessState state)
    {
        var allowed = AllowedModesFor(state).ToHashSet();
        return Enum.GetValues<NodalOsPlanningMode>()
            .Where(mode => !allowed.Contains(mode) || mode == NodalOsPlanningMode.FutureRuntimeExecution)
            .Distinct()
            .ToArray();
    }

    private static NodalOsEvidenceBridgeRef EvidenceRef(string evidenceId) =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = "assignment-engine-ref-only",
            SourceKind = NodalOsEvidenceBridgeSourceKind.Manual,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = $"ledger:{evidenceId}",
            Provenance = "M519-M521 assignment engine draft contract",
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsAssignmentEngineJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeAssignmentRequest(NodalOsAssignmentRequestDraft request) =>
        JsonSerializer.Serialize(request, Options);

    public string SerializeTaskGraph(NodalOsTaskGraphDraft taskGraph) =>
        JsonSerializer.Serialize(taskGraph, Options);

    public string SerializePlannerReadiness(NodalOsPlannerReadinessGateResult readiness) =>
        JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsAssignmentEngineFixtures
{
    public static NodalOsAssignmentRequestDraft AssignmentRequest() =>
        new NodalOsAssignmentEngineDraftService().CreateAssignmentRequest();

    public static NodalOsTaskGraphDraft TaskGraphDraft() =>
        new NodalOsAssignmentEngineDraftService().CreateTaskGraphDraft(AssignmentRequest());

    public static NodalOsPlannerReadinessGateResult PlannerReadiness() =>
        new NodalOsAssignmentEngineDraftService().CreatePlannerReadinessGate();
}
