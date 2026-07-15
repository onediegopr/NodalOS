using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class SelectiveRuntimeIntegrationTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task FixtureMissionContinuesThroughFallbackAndCompletesWithoutExtraApproval()
    {
        var result = await new NodalOsSelectiveRuntimeFixtureScenario().RunAsync();

        Assert.AreEqual(MissionStatus.Completed, result.Mission.Status);
        Assert.AreEqual(1d, result.Mission.Progress);
        Assert.IsTrue(result.ModelRouting.Success);
        Assert.AreEqual("MODEL_ROUTE_FALLBACK_SUCCEEDED", result.ModelRouting.Decision);
        Assert.IsFalse(result.ApprovalRequested);
        Assert.IsFalse(result.ExternalIoUsed);
        Assert.IsFalse(result.NetworkUsed);
        Assert.IsTrue(result.Timeline.Any(item => item.Kind == NodalOsCoreEventKind.WarningRaised));
        Assert.IsTrue(result.Timeline.Any(item => item.Kind == NodalOsCoreEventKind.ExecutionCompleted));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void TaskGraphProjectionRemainsGuidanceAndDropsFutureExecutionPlaceholder()
    {
        var graph = new NodalOsTaskGraphDraft
        {
            TaskGraphId = "graph",
            AssignmentRequestId = "assignment",
            WorkspaceId = "workspace",
            MissionId = "mission",
            GraphStatus = NodalOsAssignmentTaskGraphStatus.DraftOnly,
            Tasks =
            [
                Task("safe", NodalOsAssignmentTaskKind.AnalysisDraft, ["reasoning.plan"]),
                Task("future", NodalOsAssignmentTaskKind.FutureExecutionPlaceholder, ["terminal.execute"])
            ],
            HumanReviewRequirementRedacted = "review",
            ReadinessGateResultRedacted = "draft-only",
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

        var plan = NodalOsMissionPlanProjector.Project(graph, "Fixture mission");

        Assert.AreEqual(1, plan.Steps.Count);
        Assert.AreEqual("safe", plan.Steps.Single().Id);
        Assert.AreEqual(MissionStepStatus.Pending, plan.Steps.Single().Status);
        Assert.AreEqual(MissionStatus.Active, plan.Status);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void MissionEventProjectionIsIdempotentAndUsesCanonicalTimeline()
    {
        var stream = new MissionEventStream("run", "mission");
        stream.Append(MissionEventKind.RunStarted, "runtime", "corr", "started");
        stream.Append(MissionEventKind.FallbackApplied, "runtime", "corr", "fallback", "step");
        var bus = new NodalOsCoreEventBus();
        var projector = new NodalOsMissionEventProjectionService();

        var first = projector.Project(stream.Snapshot(), bus);
        var second = projector.Project(stream.Snapshot(), bus);

        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(2, second.Count);
        Assert.AreEqual(2, bus.Snapshot().Count);
        Assert.AreEqual(1, second.Count(item => item.Kind == NodalOsCoreEventKind.WarningRaised));
    }

    private static NodalOsAssignmentTaskDraft Task(
        string id,
        NodalOsAssignmentTaskKind kind,
        IReadOnlyList<string> capabilities) =>
        new()
        {
            TaskId = id,
            TitleRedacted = id,
            SummaryRedacted = "fixture",
            TaskKind = kind,
            Status = NodalOsAssignmentTaskStatus.Draft,
            RiskLevel = NodalOsAssignmentRiskLevel.Low,
            AllowedCapabilitiesRedacted = capabilities,
            SuggestedAssigneeType = NodalOsSuggestedAssigneeType.HumanOperator,
            RequiresApproval = false,
            RequiresLlmFuture = false,
            RequiresRuntimeFuture = kind == NodalOsAssignmentTaskKind.FutureExecutionPlaceholder,
            RequiresFilesystemFuture = false,
            CanExecute = false
        };
}
