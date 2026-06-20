using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsPlannerGovernanceCloseoutService
{
    public NodalOsPlannerGovernanceCloseoutPack CreateCloseout(
        NodalOsAssignmentReviewHistoryCollection history,
        NodalOsHandoffCompareResult compare,
        NodalOsAssignmentSafetyAuditPack audit)
    {
        return new()
        {
            CloseoutPackId = "planner-governance-closeout-m531-m533",
            Status = new()
            {
                AssignmentContractsReady = true,
                TaskGraphDraftReady = true,
                MissionPlanPreviewReady = true,
                ReviewCardsReady = true,
                UiPreviewReady = true,
                NoOpInteractionsReady = true,
                MockPersistenceReady = true,
                HandoffReady = true,
                SafetyAuditReady = audit.OverallStatus != NodalOsAssignmentSafetyAuditStatus.Fail,
                HistoryMockReady = history.MockStoreOnly,
                HandoffComparePreviewReady = compare.RefOnly,
                PlannerRuntimeImplemented = false,
                RuntimeExecutionBlocked = true,
                LlmPromptBlocked = true,
                FilesystemBlocked = true,
                CloudBlocked = true
            },
            Decisions =
            [
                NodalOsPlannerGovernanceCloseoutDecision.ReadyForNextGovernedPhase,
                NodalOsPlannerGovernanceCloseoutDecision.NotReadyForRuntime,
                NodalOsPlannerGovernanceCloseoutDecision.NotReadyForRealPlanner,
                NodalOsPlannerGovernanceCloseoutDecision.NotReadyForLlmCalls,
                NodalOsPlannerGovernanceCloseoutDecision.NotReadyForFilesystem,
                NodalOsPlannerGovernanceCloseoutDecision.RequiredNextAudit,
                NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeRealPlanner,
                NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeLlm,
                NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeFilesystem,
                NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeRuntime
            ],
            CompletedScopeRedacted =
            [
                "Assignment contracts M519-M521.",
                "TaskGraph draft and planner readiness.",
                "Mission plan preview and review cards.",
                "Assignment UI preview and no-op interactions.",
                "Review persistence mock, handoff, safety audit, history mock, and compare preview."
            ],
            StillBlockedRedacted =
            [
                "Runtime execution.",
                "Planner runtime.",
                "Model/prompt usage.",
                "Filesystem access.",
                "Cloud services."
            ],
            BlockReasonsRedacted =
            [
                "Execution authority is intentionally absent.",
                "Required policy, consent, and audit gates remain future work.",
                "All current surfaces are draft-only and ref-only."
            ],
            RecommendedNextStagesRedacted =
            [
                "Assignment review history governance can be archived as mock-safe.",
                "Future high-risk phases require a separate audit before any operational capability."
            ],
            RisksRedacted =
            [
                "Future implementation could accidentally promote draft state if not guarded.",
                "Future model or filesystem work must not reuse mock contracts as execution authority."
            ],
            AuditTriggersRedacted =
            [
                "Any future planner runtime implementation.",
                "Any future prompt/model integration.",
                "Any future filesystem or network access.",
                "Any future productive persistence."
            ],
            DecisionRecordRedacted = "M531-M533 closes Assignment/Planner preview governance and allows only the next governed phase.",
            DraftOnly = true,
            CanAuthorizeExecution = false,
            CanCallPlanner = false,
            CanCallLlm = false,
            CanAccessFilesystem = false,
            CanCallCloud = false
        };
    }
}

public sealed class NodalOsPlannerGovernanceCloseoutJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsPlannerGovernanceCloseoutPack closeout) =>
        JsonSerializer.Serialize(closeout, Options);
}

public static class NodalOsAssignmentPlannerGovernanceFixtures
{
    public static NodalOsAssignmentReviewHistoryCollection History()
    {
        var snapshot = NodalOsAssignmentReviewHandoffSafetyFixtures.Snapshot();
        var handoff = NodalOsAssignmentReviewHandoffSafetyFixtures.Handoff();
        var service = new NodalOsAssignmentReviewHistoryMockService();
        service.Store(service.CreateEntry(snapshot, handoff, "initial review"));
        return service.Store(service.CreateEntry(snapshot, handoff, "second review"));
    }

    public static NodalOsHandoffCompareResult Compare()
    {
        var handoff = NodalOsAssignmentReviewHandoffSafetyFixtures.Handoff();
        var changed = handoff with
        {
            OpenQuestionsRedacted = [..handoff.OpenQuestionsRedacted, "Which blocker should be clarified next?"],
            MissingReadinessGatesRedacted = [..handoff.MissingReadinessGatesRedacted, "future readiness gate ref"]
        };
        var service = new NodalOsHandoffComparePreviewService();
        return service.Compare(service.CreateRequest(handoff.HandoffPackId, changed.HandoffPackId), handoff, changed);
    }

    public static NodalOsPlannerGovernanceCloseoutPack Closeout()
    {
        var history = History();
        var compare = Compare();
        var audit = NodalOsAssignmentReviewHandoffSafetyFixtures.Audit();
        return new NodalOsPlannerGovernanceCloseoutService().CreateCloseout(history, compare, audit);
    }
}
