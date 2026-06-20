using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsPlannerUxAcceptanceService
{
    public NodalOsPlannerUxAcceptancePack CreateAcceptancePack()
    {
        return new()
        {
            AcceptancePackId = "planner-ux-acceptance-m525-m527",
            Criteria =
            [
                Criterion("criterion-draft-understood", "User understands assignment is draft.", true),
                Criterion("criterion-workgraph-non-executable", "User understands TaskGraph is non-executable.", true),
                Criterion("criterion-blockers-visible", "User sees why work items are blocked.", true),
                Criterion("criterion-refs-visible", "User sees evidence, timeline, and context refs.", true),
                Criterion("criterion-visual-no-side-effects", "User can interact visually without side effects.", true),
                Criterion("criterion-no-runtime-authorization", "User cannot accidentally authorize runtime.", true),
                Criterion("criterion-no-provider-network-filesystem", "User cannot trigger model, provider, network, or filesystem.", true),
                Criterion("criterion-missing-readiness-explained", "UI explains missing readiness gates.", true)
            ],
            CoveredStates = Enum.GetValues<NodalOsPlannerUxAcceptanceState>(),
            UserFacingExplanationsRedacted =
            [
                "Assignment review is draft-only.",
                "TaskGraph is non-executable.",
                "Approval display does not unlock runtime execution.",
                "Planner runtime is not implemented.",
                "Model, provider, network, and filesystem access remain blocked."
            ],
            GuardrailRefs =
            [
                "guardrail-assignment-ui-no-op",
                "guardrail-taskgraph-non-executable",
                "guardrail-approval-no-runtime-unlock",
                "guardrail-no-provider-network-filesystem"
            ],
            DraftOnly = true,
            UiInteractionsAreNoOp = true,
            CanAuthorizeRuntime = false,
            CanTriggerLlmProvider = false,
            CanAccessFilesystem = false,
            CanCallNetwork = false,
            ApprovalUnlocksRuntime = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static NodalOsPlannerUxAcceptanceCriterion Criterion(
        string id,
        string text,
        bool passed) =>
        new()
        {
            CriterionId = id,
            UserFacingTextRedacted = text,
            PassedByContract = passed
        };
}

public sealed class NodalOsPlannerUxAcceptanceJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsPlannerUxAcceptancePack pack) =>
        JsonSerializer.Serialize(pack, Options);
}

public static class NodalOsAssignmentUiPreviewFixtures
{
    public static NodalOsAssignmentUiPreview Preview()
    {
        var assignmentService = new NodalOsAssignmentEngineDraftService();
        var missionPlanService = new NodalOsMissionPlanPreviewService();
        var previewService = new NodalOsAssignmentUiPreviewService();
        var request = assignmentService.CreateAssignmentRequest();
        var taskGraph = assignmentService.CreateTaskGraphDraft(request);
        var readiness = assignmentService.CreatePlannerReadinessGate();
        var missionPlan = missionPlanService.CreateMissionPlanDraftPreview(request, taskGraph, readiness);
        return previewService.CreatePreview(request, taskGraph, readiness, missionPlan);
    }

    public static NodalOsTaskGraphInteractionNoOpResult Interaction(NodalOsTaskGraphInteractionKind kind)
    {
        var service = new NodalOsTaskGraphInteractionNoOpService();
        return service.Apply(service.CreateRequest(kind));
    }

    public static NodalOsPlannerUxAcceptancePack AcceptancePack() =>
        new NodalOsPlannerUxAcceptanceService().CreateAcceptancePack();
}
