using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AgentProgressReporting")]
[TestCategory("BlockerProgress")]
[TestCategory("VerificationBeforeDone")]
[TestCategory("MissionTaskDomain")]
[TestCategory("RunReport")]
[TestCategory("FailureTaxonomy")]
[TestCategory("AgentWorkboard")]
public sealed class NodalOsAgentProgressReportingM365M367Tests
{
    private readonly NodalOsAgentProgressReportBuilder builder = new();
    private readonly NodalOsAgentProgressReportValidator validator = new();
    private readonly NodalOsVerificationBeforeDoneGate gate = new();

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void ProgressReport_WithMissionTaskAndEvidence_IsValid()
    {
        var report = builder.CreateProgress(
            "report-progress-001",
            "mission-001",
            "task-001",
            "Progress recorded.",
            [NodalOsAgentProgressReportFixtures.Evidence("evidence-progress")]);

        var result = validator.Validate(report);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(NodalOsAgentProgressReportKind.Progress, report.Kind);
        Assert.AreEqual(1, report.EvidenceRefs.Count);
    }

    [TestMethod]
    public void BlockerReportKind_RequiresBlocker()
    {
        var report = builder.CreateProgress("report-blocker-empty", "mission-001", "task-001", "Missing blocker.") with
        {
            Kind = NodalOsAgentProgressReportKind.Blocker,
            Status = NodalOsAgentProgressReportStatus.Blocked
        };

        var result = validator.Validate(report);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "requires at least one blocker");
    }

    [TestMethod]
    public void BlockingBlocker_MakesReportNotReadyToClose()
    {
        var report = ReadyToCloseReport() with
        {
            Blockers = [NodalOsAgentWorkboardFixtures.Blocker(severity: NexaBlockerSeverity.Blocking)]
        };

        var result = validator.ValidateReadyToClose(report);

        Assert.IsFalse(result.ReadyToClose);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocking blocker");
    }

    [TestMethod]
    public void CriticalBlocker_MakesReportNotReadyToClose()
    {
        var report = ReadyToCloseReport() with
        {
            Blockers = [NodalOsAgentWorkboardFixtures.Blocker(severity: NexaBlockerSeverity.Critical)]
        };

        var result = validator.ValidateReadyToClose(report);

        Assert.IsFalse(result.ReadyToClose);
        StringAssert.Contains(string.Join(" ", result.Errors), "Critical blocker");
    }

    [TestMethod]
    public void CompletionCandidate_RequiresVerificationSummary()
    {
        var report = builder.CreateProgress("report-no-verification", "mission-001", "task-001", "Candidate.") with
        {
            Kind = NodalOsAgentProgressReportKind.CompletionCandidate,
            Status = NodalOsAgentProgressReportStatus.ReadyToClose
        };

        var result = validator.Validate(report);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "requires at least one verification summary");
    }

    [TestMethod]
    public void CompletionCandidate_WithCanMarkDoneTrue_AndEvidence_IsReadyToClose()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());
        var report = builder.CreateCompletionCandidate("report-ready", result);

        var validation = validator.Validate(report);

        Assert.IsTrue(validation.IsValid);
        Assert.IsTrue(validation.ReadyToClose);
    }

    [TestMethod]
    public void CompletionCandidate_WithCanMarkDoneFalse_IsNotReadyToClose()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithPendingVerification());
        var report = builder.CreateCompletionCandidate("report-not-ready", result);

        var validation = validator.ValidateReadyToClose(report);

        Assert.IsFalse(validation.ReadyToClose);
        StringAssert.Contains(string.Join(" ", validation.Errors), "is not ready to close");
    }

    [TestMethod]
    public void WaitingForHuman_RequiresHumanDecisionRequest()
    {
        var report = builder.CreateProgress("report-human", "mission-001", "task-001", "Need human.") with
        {
            Status = NodalOsAgentProgressReportStatus.WaitingForHuman
        };

        var result = validator.Validate(report);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "requires a human decision request");
    }

    [TestMethod]
    public void WaitingForApproval_RequiresApprovalDecisionRequest()
    {
        var report = builder.CreateProgress("report-approval", "mission-001", "task-001", "Need approval.") with
        {
            Status = NodalOsAgentProgressReportStatus.WaitingForApproval,
            HumanDecisionRequests = [NodalOsAgentProgressReportFixtures.HumanDecision(kind: NodalOsHumanDecisionKind.MissingContext)]
        };

        var result = validator.Validate(report);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "ApprovalRequired");
    }

    [TestMethod]
    public void Handoff_RequiresDetailOrEvidence()
    {
        var report = builder.CreateProgress("report-handoff-empty", "mission-001", "task-001", "Handoff.") with
        {
            Kind = NodalOsAgentProgressReportKind.Handoff,
            Status = NodalOsAgentProgressReportStatus.WaitingForHuman,
            Detail = null,
            EvidenceRefs = [],
            HumanDecisionRequests = [NodalOsAgentProgressReportFixtures.HumanDecision()]
        };

        var result = validator.Validate(report);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "Handoff report requires detail or evidence");
    }

    [TestMethod]
    public void ProgressReport_SerializesAndPreservesEvidenceRefs()
    {
        var report = builder.CreateProgress(
            "report-serialize",
            "mission-001",
            "task-001",
            "Serialize report.",
            [NodalOsAgentProgressReportFixtures.Evidence("evidence-serialize")]);

        var json = JsonSerializer.Serialize(report);
        var roundTrip = JsonSerializer.Deserialize<NodalOsAgentProgressReport>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(report.ReportId, roundTrip.ReportId);
        Assert.AreEqual("evidence-serialize", roundTrip.EvidenceRefs[0].EvidenceId);
    }

    [TestMethod]
    public void FromVerificationBeforeDoneResult_PreservesErrorsWarningsLabels()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithPendingVerification();
        var gateResult = gate.EvaluateTask(task);

        var report = builder.CreateCompletionCandidate("report-from-gate", gateResult);
        var summary = report.VerificationSummaries[0];

        Assert.IsFalse(summary.CanMarkDone);
        Assert.IsTrue(summary.Errors.Count > 0);
        CollectionAssert.Contains(summary.VerificationLabels.ToList(), "Verification fixture");
    }

    [TestMethod]
    public void FromTask_PreservesProgressNotesBlockersEvidence()
    {
        var task = NodalOsAgentWorkboardFixtures.TaskWithProgressNote() with
        {
            Blockers = [NodalOsAgentWorkboardFixtures.Blocker()],
            EvidenceRefs = [NodalOsAgentWorkboardFixtures.Evidence("evidence-task-root")]
        };

        var report = builder.FromTask(task);

        Assert.AreEqual(1, report.ProgressNotes.Count);
        Assert.AreEqual(1, report.Blockers.Count);
        Assert.IsTrue(report.EvidenceRefs.Any(e => e.EvidenceId == "evidence-task-root"));
        Assert.IsTrue(report.EvidenceRefs.Any(e => e.EvidenceId == "evidence-note"));
        Assert.IsTrue(report.EvidenceRefs.Any(e => e.EvidenceId == "evidence-blocker"));
    }

    [TestMethod]
    public void FromRunReport_PreservesRunFailuresAndEvidence()
    {
        var run = new NodalOsRunReportBuilder().CreateFailedRun("run-failed-progress", "Failed run");

        var report = builder.FromRunReport(run);

        Assert.AreEqual("run-failed-progress", report.RunId);
        Assert.AreEqual(1, report.Blockers.Count);
        Assert.IsTrue(report.EvidenceRefs.Any(e => e.EvidenceId == "evidence-failure-001"));
    }

    [TestMethod]
    public void Sanitizer_RemovesOrRejectsCookieAuthorizationTokens()
    {
        var unsafeReport = builder.CreateProgress("report-sanitize", "mission-001", "task-001", "authorization bearer token") with
        {
            Detail = "cookie=session; api_key=123"
        };

        Assert.IsFalse(NodalOsAgentProgressReportSanitizer.IsSafe(unsafeReport));

        var sanitized = NodalOsAgentProgressReportSanitizer.Sanitize(unsafeReport);

        Assert.IsTrue(NodalOsAgentProgressReportSanitizer.IsSafe(sanitized));
        Assert.IsFalse(sanitized.Summary.Contains("authorization", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(sanitized.Detail!.Contains("cookie", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Report_DoesNotContainSecretsAfterSanitize()
    {
        var report = builder.CreateProgress("report-safe-after-sanitize", "mission-001", "task-001", "secret password") with
        {
            EvidenceRefs = [NodalOsAgentProgressReportFixtures.Evidence("evidence-safe") with { Ref = "set-cookie=value" }]
        };

        var sanitized = NodalOsAgentProgressReportSanitizer.Sanitize(report);
        var validation = validator.Validate(sanitized);

        Assert.IsTrue(validation.IsValid);
        Assert.IsTrue(NodalOsAgentProgressReportSanitizer.IsSafe(sanitized));
    }

    [TestMethod]
    public void DiagnosticReport_CanExistWithoutMissionTaskRun()
    {
        var report = new NodalOsAgentProgressReport
        {
            ReportId = "report-diagnostic",
            Kind = NodalOsAgentProgressReportKind.Diagnostic,
            Status = NodalOsAgentProgressReportStatus.Informational,
            Summary = "Diagnostic report.",
            CreatedAt = NodalOsAgentProgressReportFixtures.FixedTimestamp
        };

        var result = validator.Validate(report);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ReadyToClose_FailsWhenBlockingHumanDecisionExists()
    {
        var report = ReadyToCloseReport() with
        {
            HumanDecisionRequests =
            [
                NodalOsAgentProgressReportFixtures.HumanDecision(
                    kind: NodalOsHumanDecisionKind.RiskAcceptance,
                    urgency: NodalOsHumanDecisionUrgency.Blocking)
            ]
        };

        var result = validator.ValidateReadyToClose(report);

        Assert.IsFalse(result.ReadyToClose);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocking human decision");
    }

    [TestMethod]
    public void ReportIncludesSuggestedResolutionFromBlocker()
    {
        var blocker = NodalOsAgentWorkboardFixtures.Blocker(
            severity: NexaBlockerSeverity.Blocking) with
        {
            SuggestedResolution = NexaBlockerResolutionMode.AskHuman
        };

        var report = builder.CreateBlocker("report-blocker-resolution", "mission-001", "task-001", blocker);

        Assert.AreEqual(NexaBlockerResolutionMode.AskHuman, report.Blockers[0].SuggestedResolution);
    }

    [TestMethod]
    public void NoUiOrRuntimeActionsIntroduced()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m367", "blocker-progress-reporting-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M365-M367", root.GetProperty("milestone").GetString());
        Assert.AreEqual("BLOCKER_PROGRESS_REPORTING_CONTRACT_READY", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("progressReportingContractCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("blockerReportingContractCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("humanDecisionRequestCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("verificationSummaryCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("reportValidatorCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("reportBuilderCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("reportSanitizerCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("readyToCloseRequiresVerification").GetBoolean());
        Assert.IsTrue(root.GetProperty("blockingBlockerPreventsReadyToClose").GetBoolean());
        Assert.IsTrue(root.GetProperty("criticalBlockerPreventsReadyToClose").GetBoolean());
        Assert.IsTrue(root.GetProperty("waitingForHumanRequiresDecisionRequest").GetBoolean());
        Assert.IsTrue(root.GetProperty("waitingForApprovalRequiresApprovalRequest").GetBoolean());
        Assert.IsTrue(root.GetProperty("handoffRequiresDetailOrEvidence").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceRefsPreserved").GetBoolean());
        Assert.IsTrue(root.GetProperty("sensitiveFieldsSanitized").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRuntimeActionsIntroduced").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRecipeExecutionImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
        Assert.AreEqual("M368-M370 Step Library V1 or Core Legacy Reference Graph", root.GetProperty("nextRecommendedMilestone").GetString());
    }

    private NodalOsAgentProgressReport ReadyToCloseReport()
    {
        var result = gate.EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());
        return builder.CreateCompletionCandidate("report-ready-to-close", result);
    }

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
