using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class LightweightMissionRuntimeTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void OrdinaryAuthorizedStepStartsWithoutAdditionalApproval()
    {
        var runtime = CreateRuntime(SingleStep());
        runtime.Start("corr-start");

        runtime.BeginStep("step-1", "corr-step");

        Assert.AreEqual(MissionStepStatus.InProgress, runtime.State.Steps["step-1"].Status);
        Assert.IsFalse(runtime.Events.Any(value => value.Kind == MissionEventKind.ApprovalRequired));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void MaterialRiskRequiresOneApprovalThenContinues()
    {
        var plan = SingleStep(risk: MissionRiskLevel.High, approvalRequired: true);
        var runtime = CreateRuntime(plan, maximumRisk: MissionRiskLevel.Medium);
        runtime.Start("corr-start");

        runtime.BeginStep("step-1", "corr-first");
        Assert.AreEqual(MissionEventKind.ApprovalRequired, runtime.Events.Last().Kind);

        runtime.ResolveApproval("step-1", approved: true, "corr-approval");
        runtime.BeginStep("step-1", "corr-second");

        Assert.AreEqual(MissionStepStatus.InProgress, runtime.State.Steps["step-1"].Status);
        Assert.AreEqual(1, runtime.Events.Count(value => value.Kind == MissionEventKind.ApprovalRequired));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void PlanCannotDeclareCompletionWithoutEvidenceAndVerification()
    {
        var runtime = CreateRuntime(SingleStep());
        runtime.Start("start");
        runtime.BeginStep("step-1", "begin");

        Assert.ThrowsException<InvalidOperationException>(() =>
            runtime.MarkReadyForVerification("step-1", "ready", Array.Empty<string>()));
        Assert.AreNotEqual(MissionStatus.Completed, runtime.State.Status);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void VerificationCompletesMissionOnlyAfterEvidence()
    {
        var runtime = CreateRuntime(SingleStep());
        runtime.Start("start");
        runtime.BeginStep("step-1", "begin");
        runtime.RecordToolCallCompleted("step-1", "filesystem.read", "tool", ["evidence:1"]);
        runtime.MarkReadyForVerification("step-1", "ready", ["evidence:1"]);

        Assert.AreEqual(MissionStatus.ReadyForVerification, runtime.State.Status);
        runtime.VerifyStep("step-1", passed: true, "verify", ["evidence:verified"]);

        Assert.AreEqual(MissionStatus.Completed, runtime.State.Status);
        Assert.AreEqual(CompactMissionMemoryStatus.Done, runtime.Memory.Status);
        Assert.AreEqual(MissionEventKind.RunCompleted, runtime.Events.Last().Kind);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void CompactMemoryIsBoundedDeduplicatedAndConvergesOnDone()
    {
        CompactMissionMemory? memory = null;
        memory = CompactMissionMemoryProjector.Apply(
            memory,
            new CompactMissionMemoryUpdate(
                Goal: "Goal",
                ConfirmedFacts: Enumerable.Range(0, 15).Select(value => $"fact-{value}").Append("fact-14").ToArray(),
                Blockers: Enumerable.Range(0, 8).Select(value => $"block-{value}").ToArray(),
                NextStep: "continue"),
            "event-1");

        Assert.AreEqual(CompactMissionMemoryProjector.ConfirmedFactsLimit, memory.ConfirmedFacts.Count);
        Assert.AreEqual(CompactMissionMemoryProjector.BlockersLimit, memory.Blockers.Count);
        Assert.AreEqual(1, memory.ConfirmedFacts.Count(value => value == "fact-14"));

        memory = CompactMissionMemoryProjector.Apply(
            memory,
            new CompactMissionMemoryUpdate(Status: CompactMissionMemoryStatus.Done),
            "event-2");

        Assert.AreEqual(0, memory.Blockers.Count);
        Assert.IsNull(memory.NextStep);
        Assert.IsNull(memory.LastFailureReason);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void CompactMemorySoftResetDoesNotCarryPriorMissionState()
    {
        var existing = CompactMissionMemoryProjector.Apply(
            null,
            new CompactMissionMemoryUpdate(
                Goal: "Old",
                ConfirmedFacts: ["old fact"],
                Blockers: ["old blocker"]),
            "old-event");

        var reset = CompactMissionMemoryProjector.Apply(
            existing,
            new CompactMissionMemoryUpdate(NewMission: true, Goal: "New"),
            "new-event");

        Assert.AreEqual("New", reset.Goal);
        Assert.AreEqual(0, reset.ConfirmedFacts.Count);
        Assert.AreEqual(0, reset.Blockers.Count);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void ReducerReplayIsDeterministicAndRejectsSequenceGaps()
    {
        var runtime = CreateRuntime(SingleStep());
        runtime.Start("start");
        runtime.BeginStep("step-1", "begin");
        var first = MissionReducer.Reduce(runtime.Plan, runtime.Events);
        var second = MissionReducer.Reduce(runtime.Plan, runtime.Events);

        Assert.AreEqual(first.Status, second.Status);
        Assert.AreEqual(first.CurrentStepId, second.CurrentStepId);
        Assert.AreEqual(first.VerifiedStepCount, second.VerifiedStepCount);
        CollectionAssert.AreEqual(first.Blockers.ToArray(), second.Blockers.ToArray());
        var invalid = runtime.Events.Select((value, index) => index == 1 ? value with { Sequence = 4 } : value).ToArray();
        Assert.ThrowsException<InvalidOperationException>(() => MissionReducer.Reduce(runtime.Plan, invalid));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void TerminalEventPreventsAdditionalRuntimeEvents()
    {
        var stream = new MissionEventStream("run", "mission");
        stream.Append(MissionEventKind.RunStarted, "runtime", "corr", "started");
        stream.Append(MissionEventKind.RunCancelled, "operator", "corr", "cancelled");

        Assert.ThrowsException<InvalidOperationException>(() =>
            stream.Append(MissionEventKind.StepStarted, "runtime", "corr", "late", "step"));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void ResumeCardShowsFallbackBlockerAndNextActionWithoutSecondTimeline()
    {
        var runtime = CreateRuntime(SingleStep());
        runtime.Start("start");
        runtime.BeginStep("step-1", "begin");
        runtime.RecordFallback("step-1", "Primary provider failed; authorized fallback applied.", "fallback");

        var card = runtime.ResumeCard;

        Assert.AreEqual("Read fixture", card.CurrentStep);
        Assert.IsNotNull(card.RecentFallback);
        Assert.IsTrue(card.RecentFallback.Contains("fallback", StringComparison.OrdinalIgnoreCase));
    }

    private static LightweightMissionRuntime CreateRuntime(
        MissionPlan plan,
        MissionRiskLevel maximumRisk = MissionRiskLevel.Medium)
    {
        var capabilities = plan.Steps.SelectMany(step => step.AllowedCapabilities).ToHashSet(StringComparer.Ordinal);
        var surfaces = plan.Steps.Select(step => step.ExecutionSurface).ToHashSet();
        return new LightweightMissionRuntime(
            plan,
            new MissionAuthorizationScope(plan.MissionId, capabilities, surfaces, maximumRisk),
            "run-1");
    }

    private static MissionPlan SingleStep(
        MissionRiskLevel risk = MissionRiskLevel.Low,
        bool approvalRequired = false) =>
        new(
            "mission-1",
            1,
            DateTimeOffset.UtcNow,
            "Inspect a test-owned fixture",
            [
                new MissionStep(
                    "step-1",
                    null,
                    "Read fixture",
                    MissionExecutionSurface.Filesystem,
                    ["filesystem.read"],
                    [new MissionExpectedEvidence("snapshot", "Fixture snapshot")],
                    risk,
                    approvalRequired,
                    Array.Empty<string>(),
                    MissionStepStatus.Pending,
                    0,
                    null,
                    Array.Empty<string>())
            ],
            MissionStatus.Active);
}
