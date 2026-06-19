using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CompletionGateCanonicalization")]
[TestCategory("VerificationBeforeDone")]
[TestCategory("AgentProgressReporting")]
[TestCategory("MissionTaskDomain")]
[TestCategory("RunReport")]
[TestCategory("CoreLegacyReferenceGraph")]
public sealed class NodalOsCompletionGateCanonicalizationM377M379Tests
{
    private readonly NodalOsVerificationBeforeDoneGate gate = new();
    private readonly NodalOsAgentWorkboardValidator workboardValidator = new();
    private readonly NodalOsAgentProgressReportBuilder progressBuilder = new();
    private readonly NodalOsAgentProgressReportValidator progressValidator = new();
    private readonly NodalOsRunReportBuilder runBuilder = new();

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void WorkboardValidator_DelegatesOrMatches_VerificationBeforeDone_ForValidCompletedTask()
    {
        AssertTaskCompletionMatchesGate(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());
    }

    [TestMethod]
    public void WorkboardValidator_DelegatesOrMatches_ForBlockingBlocker()
    {
        AssertTaskCompletionMatchesGate(NodalOsAgentWorkboardFixtures.CompletedTaskWithBlockingBlocker());
    }

    [TestMethod]
    public void WorkboardValidator_DelegatesOrMatches_ForCriticalBlocker()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification() with
        {
            Blockers = [NodalOsAgentWorkboardFixtures.Blocker(severity: NexaBlockerSeverity.Critical)]
        };

        AssertTaskCompletionMatchesGate(task);
    }

    [TestMethod]
    public void WorkboardValidator_DelegatesOrMatches_ForPendingVerification()
    {
        AssertTaskCompletionMatchesGate(NodalOsAgentWorkboardFixtures.CompletedTaskWithPendingVerification());
    }

    [TestMethod]
    public void WorkboardValidator_DelegatesOrMatches_ForFailedVerification()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification() with
        {
            VerificationChecks = [NodalOsAgentWorkboardFixtures.Verification(status: NexaVerificationStatus.Failed)]
        };

        AssertTaskCompletionMatchesGate(task);
    }

    [TestMethod]
    public void WorkboardValidator_DelegatesOrMatches_ForMissingEvidenceOrReason()
    {
        AssertTaskCompletionMatchesGate(NodalOsAgentWorkboardFixtures.CompletedTaskWithoutEvidence());
    }

    [TestMethod]
    public void ProgressReport_ReadyToClose_RequiresGatePositiveSummary()
    {
        var gateResult = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());
        var report = progressBuilder.CreateCompletionCandidate("report-gate-positive", gateResult);

        var validation = progressValidator.ValidateReadyToClose(report);

        Assert.IsTrue(gateResult.CanMarkDone);
        Assert.IsTrue(validation.ReadyToClose);
    }

    [TestMethod]
    public void ProgressReport_ReadyToClose_FailsWhenGateSummaryCannotMarkDone()
    {
        var gateResult = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithPendingVerification());
        var report = progressBuilder.CreateCompletionCandidate("report-gate-negative", gateResult) with
        {
            Status = NodalOsAgentProgressReportStatus.ReadyToClose
        };

        var validation = progressValidator.ValidateReadyToClose(report);

        Assert.IsFalse(gateResult.CanMarkDone);
        Assert.IsFalse(validation.ReadyToClose);
    }

    [TestMethod]
    public void ProgressReport_ReadyToClose_FailsWithoutVerificationSummary()
    {
        var report = progressBuilder.CreateProgress("report-no-summary", "mission-001", "task-001", "Candidate.") with
        {
            Kind = NodalOsAgentProgressReportKind.CompletionCandidate,
            Status = NodalOsAgentProgressReportStatus.ReadyToClose,
            EvidenceRefs = [NodalOsAgentProgressReportFixtures.Evidence("evidence-no-summary")]
        };

        var validation = progressValidator.ValidateReadyToClose(report);

        Assert.IsFalse(validation.ReadyToClose);
        StringAssert.Contains(string.Join(" ", validation.Errors), "verification summary");
    }

    [TestMethod]
    public void ProgressReport_ReadyToClose_FailsWithBlockingBlockerEvenIfSummaryPositive()
    {
        var gateResult = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());
        var report = progressBuilder.CreateCompletionCandidate("report-blocking-with-positive-summary", gateResult) with
        {
            Blockers = [NodalOsAgentWorkboardFixtures.Blocker(severity: NexaBlockerSeverity.Blocking)]
        };

        var validation = progressValidator.ValidateReadyToClose(report);

        Assert.IsTrue(gateResult.CanMarkDone);
        Assert.IsFalse(validation.ReadyToClose);
    }

    [TestMethod]
    public void ProgressReport_ReadyToClose_FailsWithBlockingHumanDecision()
    {
        var gateResult = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());
        var report = progressBuilder.CreateCompletionCandidate("report-blocking-human", gateResult) with
        {
            HumanDecisionRequests =
            [
                NodalOsAgentProgressReportFixtures.HumanDecision(
                    kind: NodalOsHumanDecisionKind.RiskAcceptance,
                    urgency: NodalOsHumanDecisionUrgency.Blocking)
            ]
        };

        var validation = progressValidator.ValidateReadyToClose(report);

        Assert.IsFalse(validation.ReadyToClose);
    }

    [TestMethod]
    public void RunReportBuilder_CompletedSemantics_MatchesVerificationGate()
    {
        var report = runBuilder.CreateSuccessfulRun("run-completion-canonical", "Complete run");

        var builderValidation = runBuilder.Validate(report);
        var gateResult = gate.EvaluateRunReport(report);

        Assert.IsTrue(builderValidation.IsValid);
        Assert.IsTrue(gateResult.CanMarkDone);
    }

    [TestMethod]
    public void RunReportBuilder_FailedRunSemantics_MatchesVerificationGate()
    {
        var report = runBuilder.CreateFailedRun("run-failed-canonical", "Failed run");

        var builderValidation = runBuilder.Validate(report);
        var gateResult = gate.EvaluateRunReport(report);

        Assert.IsTrue(builderValidation.IsValid);
        Assert.IsFalse(gateResult.CanMarkDone);
    }

    [TestMethod]
    public void RunReportBuilder_BlockedRunSemantics_MatchesVerificationGate()
    {
        var report = runBuilder.CreateBlockedByPolicyRun("run-blocked-canonical", "Blocked run");

        var builderValidation = runBuilder.Validate(report);
        var gateResult = gate.EvaluateRunReport(report);

        Assert.IsTrue(builderValidation.IsValid);
        Assert.IsFalse(gateResult.CanMarkDone);
    }

    [TestMethod]
    public void CompletedWithWarnings_Semantics_MatchesVerificationGate()
    {
        var report = new NodalOsRunReportBuilder()
            .Start("run-warning-canonical", "Warning run")
            .AddStep(NodalOsRunReportFixtures.Step())
            .AddFailure(NodalOsRunReportFixtures.Failure(severity: NexaFailureSeverity.Recoverable))
            .Complete(NexaRunStatus.CompletedWithWarnings, "completed with recoverable warning")
            .Build();

        var builderValidation = runBuilder.Validate(report);
        var gateResult = gate.EvaluateRunReport(report);

        Assert.IsTrue(builderValidation.IsValid);
        Assert.IsTrue(gateResult.CanMarkDone);
    }

    [TestMethod]
    public void FailedStep_PreventsDoneAcrossBuilderAndGate()
    {
        AssertInvalidCompletedRunMatchesGate(CompletedRunReport(
            "run-failed-step-canonical",
            "Failed step",
            steps: [NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.Failed)]));
    }

    [TestMethod]
    public void WaitingForHumanStep_PreventsDoneAcrossBuilderAndGate()
    {
        AssertInvalidCompletedRunMatchesGate(CompletedRunReport(
            "run-waiting-human-canonical",
            "Waiting human step",
            steps: [NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.WaitingForHuman)]));
    }

    [TestMethod]
    public void PendingApproval_PreventsDoneAcrossBuilderAndGate()
    {
        AssertInvalidCompletedRunMatchesGate(CompletedRunReport(
            "run-pending-approval-canonical",
            "Pending approval",
            approvals: [NodalOsRunReportFixtures.Approval(status: "Requested")]));
    }

    [TestMethod]
    public void GateResult_StillSerializes()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());

        var json = JsonSerializer.Serialize(result);
        var roundTrip = JsonSerializer.Deserialize<NodalOsVerificationBeforeDoneResult>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(result.CanMarkDone, roundTrip.CanMarkDone);
        Assert.AreEqual(result.SubjectId, roundTrip.SubjectId);
    }

    [TestMethod]
    public void NoUiOrRuntimeActionsIntroduced()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m379", "completion-gate-canonicalization-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M377-M379", root.GetProperty("milestone").GetString());
        Assert.IsTrue(root.GetProperty("verificationBeforeDoneCanonical").GetBoolean());
        Assert.IsTrue(root.GetProperty("workboardValidatorAligned").GetBoolean());
        Assert.IsTrue(root.GetProperty("progressReportReadyToCloseAligned").GetBoolean());
        Assert.IsTrue(root.GetProperty("runReportCompletionAligned").GetBoolean());
        Assert.IsTrue(root.GetProperty("noDivergenceTestsCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRuntimeBehaviorChange").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRecipeExecutionImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
    }

    private void AssertTaskCompletionMatchesGate(NexaAgentTask task)
    {
        var workboardResult = workboardValidator.ValidateTaskCanComplete(task);
        var gateResult = gate.EvaluateTask(task);
        var compatibilityResult = NodalOsCompletionGateCompatibilityAdapter.ToTaskValidationResult(gateResult);

        Assert.AreEqual(gateResult.CanMarkDone, workboardResult.CanComplete);
        CollectionAssert.AreEquivalent(compatibilityResult.Errors.ToList(), workboardResult.Errors.ToList());
    }

    private void AssertInvalidCompletedRunMatchesGate(NexaRunReport report)
    {
        var builderValidation = runBuilder.Validate(report);
        var gateResult = gate.EvaluateRunReport(report);

        Assert.IsFalse(builderValidation.IsValid);
        Assert.IsFalse(gateResult.CanMarkDone);
        Assert.IsTrue(builderValidation.Errors.Any(error => error.StartsWith("Run completion gate:", StringComparison.Ordinal)));
    }

    private static NexaRunReport CompletedRunReport(
        string runId,
        string goal,
        IReadOnlyList<NexaRunStepReport>? steps = null,
        IReadOnlyList<NexaApprovalReport>? approvals = null,
        IReadOnlyList<NexaEvidenceRef>? evidenceRefs = null) =>
        new()
        {
            RunId = runId,
            Goal = goal,
            Status = NexaRunStatus.Completed,
            StartedAt = NodalOsRunReportFixtures.FixedTimestamp,
            CompletedAt = NodalOsRunReportFixtures.FixedTimestamp.AddMinutes(1),
            Steps = steps ?? [NodalOsRunReportFixtures.Step()],
            Approvals = approvals ?? [],
            EvidenceRefs = evidenceRefs ?? [NodalOsRunReportFixtures.Evidence($"evidence-{runId}")],
            FinalSummary = "done"
        };

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
