using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("VerificationBeforeDone")]
[TestCategory("MissionTaskDomain")]
[TestCategory("RunReport")]
[TestCategory("FailureTaxonomy")]
[TestCategory("AgentWorkboard")]
[TestCategory("SelectiveAbsorption")]
public sealed class NodalOsVerificationBeforeDoneGateM362M364Tests
{
    private readonly NodalOsVerificationBeforeDoneGate gate = new();

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void CompletedTask_WithPassedVerificationAndEvidence_CanMarkDone()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());

        Assert.IsTrue(result.CanMarkDone);
        Assert.AreEqual(NodalOsVerificationBeforeDoneSubjectKind.AgentTask, result.SubjectKind);
    }

    [TestMethod]
    public void CompletedTask_WithCompletionReasonAndPassedVerification_CanMarkDone()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [NodalOsAgentWorkboardFixtures.Verification(status: NexaVerificationStatus.Passed)],
            CompletionReason = "Closed with operator-reviewed result."
        };

        var result = gate.EvaluateTask(task);

        Assert.IsTrue(result.CanMarkDone);
        Assert.AreEqual("Closed with operator-reviewed result.", result.CompletionReason);
    }

    [TestMethod]
    public void CompletedTask_WithBlockingBlocker_CannotMarkDone()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithBlockingBlocker());

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocking blocker");
    }

    [TestMethod]
    public void CompletedTask_WithCriticalBlocker_CannotMarkDone()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification() with
        {
            Blockers = [NodalOsAgentWorkboardFixtures.Blocker(severity: NexaBlockerSeverity.Critical)]
        };

        var result = gate.EvaluateTask(task);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Critical blocker");
    }

    [TestMethod]
    public void CompletedTask_WithPendingRequiredVerification_CannotMarkDone()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithPendingVerification());

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "pending");
    }

    [TestMethod]
    public void CompletedTask_WithFailedRequiredVerification_CannotMarkDone()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification() with
        {
            VerificationChecks = [NodalOsAgentWorkboardFixtures.Verification(status: NexaVerificationStatus.Failed)]
        };

        var result = gate.EvaluateTask(task);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "failed");
    }

    [TestMethod]
    public void CompletedTask_WithSkippedVerificationWithoutReason_CannotMarkDone()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithSkippedVerificationNoReason());

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "skipped without a reason");
    }

    [TestMethod]
    public void CompletedTask_WithSkippedVerificationWithReason_CanMarkDone()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [NodalOsAgentWorkboardFixtures.Verification(status: NexaVerificationStatus.SkippedWithReason, detail: "Covered by signed run report.")],
            CompletionReason = "Closed with explicit verification skip reason."
        };

        var result = gate.EvaluateTask(task);

        Assert.IsTrue(result.CanMarkDone);
    }

    [TestMethod]
    public void CompletedTask_WithoutEvidenceOrReason_CannotMarkDone()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithoutEvidence());

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "evidence refs or an explicit completion reason");
    }

    [TestMethod]
    public void GateResult_PreservesEvidenceRefs()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());

        Assert.IsTrue(result.EvidenceRefs.Any(e => e.EvidenceId == "evidence-completion"));
        Assert.IsTrue(result.EvidenceRefs.Any(e => e.EvidenceId == "evidence-verification"));
    }

    [TestMethod]
    public void GateResult_ListsVerificationLabels()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());

        CollectionAssert.Contains(result.VerificationLabels.ToList(), "Verification fixture");
    }

    [TestMethod]
    public void CompletedMission_WithAllTasksDone_CanMarkDone()
    {
        var mission = NodalOsAgentWorkboardFixtures.ValidMission() with
        {
            Status = NexaMissionStatus.Completed,
            Tasks = [NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification()]
        };

        var result = gate.EvaluateMission(mission);

        Assert.IsTrue(result.CanMarkDone);
    }

    [TestMethod]
    public void CompletedMission_WithOneInvalidTask_CannotMarkDone()
    {
        var mission = NodalOsAgentWorkboardFixtures.ValidMission() with
        {
            Status = NexaMissionStatus.Completed,
            Tasks = [NodalOsAgentWorkboardFixtures.CompletedTaskWithPendingVerification()]
        };

        var result = gate.EvaluateMission(mission);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Task task-agent-ops-001");
    }

    [TestMethod]
    public void CompletedMission_WithoutTasksOrEvidence_CannotMarkDone()
    {
        var mission = NodalOsAgentWorkboardFixtures.ValidMission() with
        {
            Status = NexaMissionStatus.Completed,
            Tasks = [],
            EvidenceRefs = []
        };

        var result = gate.EvaluateMission(mission);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "requires tasks or mission evidence");
    }

    [TestMethod]
    public void MissionGate_AggregatesTaskErrors()
    {
        var invalidTask = NodalOsAgentWorkboardFixtures.CompletedTaskWithBlockingBlocker() with { TaskId = "task-blocked" };
        var mission = NodalOsAgentWorkboardFixtures.ValidMission() with
        {
            Status = NexaMissionStatus.Completed,
            Tasks = [invalidTask]
        };

        var result = gate.EvaluateMission(mission);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Task task-blocked");
    }

    [TestMethod]
    public void CompletedRun_WithAllStepsCompletedAndEvidence_CanMarkDone()
    {
        var report = new NodalOsRunReportBuilder().CreateSuccessfulRun("run-done", "Complete run");

        var result = gate.EvaluateRunReport(report);

        Assert.IsTrue(result.CanMarkDone);
        Assert.AreEqual(NodalOsVerificationBeforeDoneSubjectKind.RunReport, result.SubjectKind);
    }

    [TestMethod]
    public void CompletedRun_WithFailedStep_CannotMarkDone()
    {
        var report = SuccessfulRunWithStep(NexaRunStepStatus.Failed);

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Failed prevents done");
    }

    [TestMethod]
    public void CompletedRun_WithBlockedStep_CannotMarkDone()
    {
        var report = SuccessfulRunWithStep(NexaRunStepStatus.Blocked);

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocked prevents done");
    }

    [TestMethod]
    public void CompletedRun_WithWaitingForHumanStep_CannotMarkDone()
    {
        var report = SuccessfulRunWithStep(NexaRunStepStatus.WaitingForHuman);

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "WaitingForHuman prevents done");
    }

    [TestMethod]
    public void CompletedRun_WithPendingApproval_CannotMarkDone()
    {
        var report = CompletedRunReport(
            "run-pending-approval",
            "Approval pending",
            approvals: [NodalOsRunReportFixtures.Approval(status: "Requested")]);

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Approval approval-001 status Requested prevents done");
    }

    [TestMethod]
    public void CompletedRun_WithBlockingFailure_CannotMarkDone()
    {
        var report = CompletedRunReport(
            "run-blocking-failure",
            "Blocking failure",
            failures: [NodalOsRunReportFixtures.Failure(severity: NexaFailureSeverity.Blocking)]);

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocking prevents done");
    }

    [TestMethod]
    public void CompletedWithWarnings_WithRecoverableFailureAndSummary_CanMarkDone()
    {
        var report = new NodalOsRunReportBuilder()
            .Start("run-warning", "Warning run")
            .AddStep(NodalOsRunReportFixtures.Step())
            .AddFailure(NodalOsRunReportFixtures.Failure(severity: NexaFailureSeverity.Recoverable))
            .Complete(NexaRunStatus.CompletedWithWarnings, "completed with recoverable warning")
            .Build();

        var result = gate.EvaluateRunReport(report);

        Assert.IsTrue(result.CanMarkDone);
        Assert.IsTrue(result.Warnings.Count > 0);
    }

    [TestMethod]
    public void CompletedWithWarnings_WithCriticalFailure_CannotMarkDone()
    {
        var report = CompletedRunReport(
            "run-critical-warning",
            "Critical warning",
            status: NexaRunStatus.CompletedWithWarnings,
            finalSummary: "cannot close",
            failures: [NodalOsRunReportFixtures.Failure(severity: NexaFailureSeverity.Critical)]);

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
    }

    [TestMethod]
    public void FailedRun_CannotBeMarkedDone()
    {
        var result = gate.EvaluateRunReport(new NodalOsRunReportBuilder().CreateFailedRun("run-failed-done", "Failed run"));

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Failed run");
    }

    [TestMethod]
    public void BlockedRun_CannotBeMarkedDone()
    {
        var result = gate.EvaluateRunReport(new NodalOsRunReportBuilder().CreateBlockedByPolicyRun("run-blocked-done", "Blocked run"));

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocked run");
    }

    [TestMethod]
    public void RunningRun_CannotBeMarkedDone()
    {
        var report = new NodalOsRunReportBuilder()
            .Start("run-running", "Running run")
            .AddStep(NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.Running))
            .Build();

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "Running run is not complete");
    }

    [TestMethod]
    public void CompletedRun_WithoutCompletedAt_CannotMarkDone()
    {
        var report = new NodalOsRunReportBuilder().CreateSuccessfulRun("run-no-completed-at", "Missing timestamp") with
        {
            CompletedAt = null
        };

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CanMarkDone);
        StringAssert.Contains(string.Join(" ", result.Errors), "CompletedAt");
    }

    [TestMethod]
    public void RunGate_PreservesEvidenceRefs()
    {
        var report = new NodalOsRunReportBuilder().CreateSuccessfulRun("run-evidence-preserved", "Evidence run");

        var result = gate.EvaluateRunReport(report);

        Assert.IsTrue(result.EvidenceRefs.Any(e => e.EvidenceId == "evidence-success"));
        Assert.IsTrue(result.EvidenceRefs.Any(e => e.EvidenceId == "evidence-step-001"));
    }

    [TestMethod]
    public void GateResult_SerializesToJson()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());

        var json = JsonSerializer.Serialize(result);
        var roundTrip = JsonSerializer.Deserialize<NodalOsVerificationBeforeDoneResult>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(result.SubjectId, roundTrip.SubjectId);
        Assert.AreEqual(result.CanMarkDone, roundTrip.CanMarkDone);
    }

    [TestMethod]
    public void Gate_DoesNotIntroduceSensitiveData()
    {
        var report = new NodalOsRunReportBuilder().CreateSuccessfulRun("run-redaction-safe", "Safe goal") with
        {
            FinalSummary = "authorization bearer abc password=x"
        };

        var result = gate.EvaluateRunReport(report);

        Assert.IsFalse(result.CompletionReason!.Contains("authorization", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.CompletionReason!.Contains("bearer", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.CompletionReason!.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Artifact_ValidatesM364Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m364", "verification-before-done-gate-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M362-M364", root.GetProperty("milestone").GetString());
        Assert.AreEqual("VERIFICATION_BEFORE_DONE_GATE_READY", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("verificationBeforeDoneGateCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("taskGateCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("missionGateCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("runReportGateCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("blockingBlockerPreventsDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("criticalBlockerPreventsDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("pendingVerificationPreventsDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("failedVerificationPreventsDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("skippedVerificationRequiresReason").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceOrReasonRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("missionAggregatesTaskErrors").GetBoolean());
        Assert.IsTrue(root.GetProperty("failedRunCannotBeDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("blockedRunCannotBeDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("runningRunCannotBeDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("completedRunRequiresCompletedAt").GetBoolean());
        Assert.IsTrue(root.GetProperty("completedRunWithFailedStepCannotBeDone").GetBoolean());
        Assert.IsTrue(root.GetProperty("completedWithWarningsRequiresNonCriticalFailures").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceRefsPreserved").GetBoolean());
        Assert.IsTrue(root.GetProperty("gateResultSerializable").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRuntimeActionsIntroduced").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRecipeExecutionImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
        Assert.AreEqual("M365-M367 Blocker + Progress Reporting Contract", root.GetProperty("nextRecommendedMilestone").GetString());
    }

    private static NexaRunReport SuccessfulRunWithStep(NexaRunStepStatus status) =>
        CompletedRunReport(
            $"run-step-{status}",
            $"Step {status}",
            steps: [NodalOsRunReportFixtures.Step(status: status)],
            evidenceRefs: [NodalOsRunReportFixtures.Evidence($"evidence-{status}")]);

    private static NexaRunReport CompletedRunReport(
        string runId,
        string goal,
        NexaRunStatus status = NexaRunStatus.Completed,
        string finalSummary = "done",
        IReadOnlyList<NexaRunStepReport>? steps = null,
        IReadOnlyList<NexaFailureReport>? failures = null,
        IReadOnlyList<NexaApprovalReport>? approvals = null,
        IReadOnlyList<NexaEvidenceRef>? evidenceRefs = null) =>
        new()
        {
            RunId = runId,
            Goal = goal,
            Status = status,
            StartedAt = NodalOsRunReportFixtures.FixedTimestamp,
            CompletedAt = NodalOsRunReportFixtures.FixedTimestamp.AddMinutes(1),
            Steps = steps ?? [NodalOsRunReportFixtures.Step()],
            Failures = failures ?? [],
            Approvals = approvals ?? [],
            EvidenceRefs = evidenceRefs ?? [NodalOsRunReportFixtures.Evidence($"evidence-{runId}")],
            FinalSummary = finalSummary
        };

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
