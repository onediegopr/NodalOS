using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("FailureTaxonomy")]
[TestCategory("RunReport")]
[TestCategory("Troubleshooting")]
[TestCategory("SelectiveAbsorption")]
[TestCategory("MissionTaskDomain")]
[TestCategory("AgentWorkboard")]
public sealed class NodalOsFailureTaxonomyRunReportM350M352Tests
{
    private readonly NodalOsTroubleshootingRecommendationMapper mapper = new();
    private readonly NodalOsRunReportBuilder builder = new();

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void EveryFailureKind_HasTroubleshootingRecommendation()
    {
        Assert.IsTrue(mapper.ValidateCoverage());

        foreach (var kind in Enum.GetValues<NexaFailureKind>())
            Assert.AreEqual(kind, mapper.GetRecommendation(kind).FailureKind);
    }

    [TestMethod]
    public void CaptchaDetected_RequiresHumanInput_AndNoAutoRetry()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.CaptchaDetected);

        Assert.IsTrue(recommendation.RequiresHumanInput);
        Assert.IsFalse(recommendation.CanRetryAutomatically);
        Assert.IsTrue(recommendation.MustStop);
    }

    [TestMethod]
    public void TwoFactorRequired_RequiresHumanInput_AndNoAutoRetry()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.TwoFactorRequired);

        Assert.IsTrue(recommendation.RequiresHumanInput);
        Assert.IsFalse(recommendation.CanRetryAutomatically);
        Assert.IsTrue(recommendation.MustStop);
    }

    [TestMethod]
    public void LoginRequired_RequiresHumanInput()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.LoginRequired);

        Assert.IsTrue(recommendation.RequiresHumanInput);
        Assert.IsFalse(recommendation.CanRetryAutomatically);
    }

    [TestMethod]
    public void PolicyBlocked_DoesNotAutoRetry()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.PolicyBlocked);

        Assert.IsFalse(recommendation.CanRetryAutomatically);
        Assert.IsFalse(recommendation.CanReplan);
        Assert.IsTrue(recommendation.MustStop);
    }

    [TestMethod]
    public void NoProgressDetected_SuggestsReplanOrHuman()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.NoProgressDetected);

        Assert.IsFalse(recommendation.CanRetryAutomatically);
        Assert.IsTrue(recommendation.CanReplan);
        StringAssert.Contains(recommendation.SuggestedAction, "Replan");
    }

    [TestMethod]
    public void RepeatedActionDetected_BlocksRepetition()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.RepeatedActionDetected);

        Assert.IsFalse(recommendation.CanRetryAutomatically);
        Assert.IsFalse(recommendation.CanReplan);
        Assert.IsTrue(recommendation.MustStop);
    }

    [TestMethod]
    public void SensitiveDataRisk_FailsClosedOrRequiresApproval()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.SensitiveDataRisk);

        Assert.IsTrue(recommendation.RequiresHumanInput);
        Assert.IsFalse(recommendation.CanRetryAutomatically);
        Assert.IsTrue(recommendation.MustStop);
    }

    [TestMethod]
    public void Unknown_IsRecoverableOnlyWithLimits()
    {
        var recommendation = mapper.GetRecommendation(NexaFailureKind.Unknown);

        Assert.IsTrue(recommendation.CanRetryAutomatically);
        Assert.IsTrue(recommendation.CanReplan);
        Assert.IsFalse(recommendation.MustStop);
        StringAssert.Contains(recommendation.SuggestedAction, "bounded limits");
    }

    [TestMethod]
    public void CanCreateSuccessfulRunReport()
    {
        var report = builder.CreateSuccessfulRun(
            "run-success",
            "Read fixture",
            missionId: "mission-001",
            taskId: "task-001",
            recipeId: "recipe-001");

        Assert.AreEqual(NexaRunStatus.Completed, report.Status);
        Assert.AreEqual(1, report.Steps.Count);
        Assert.AreEqual(0, report.Failures.Count);
        Assert.IsTrue(report.EvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void CanCreateBlockedByPolicyRunReport()
    {
        var report = builder.CreateBlockedByPolicyRun("run-blocked", "Attempt governed step");

        Assert.AreEqual(NexaRunStatus.Blocked, report.Status);
        Assert.AreEqual(1, report.PolicyDecisions.Count);
        Assert.AreEqual(NexaFailureKind.PolicyBlocked, report.Failures[0].Kind);
    }

    [TestMethod]
    public void CanCreateFailedRunReportWithSelectorFailure()
    {
        var report = builder.CreateFailedRun("run-failed", "Find selector");

        Assert.AreEqual(NexaRunStatus.Failed, report.Status);
        Assert.AreEqual(NexaFailureKind.SelectorNotFound, report.Failures[0].Kind);
        Assert.AreEqual(NexaFailureSeverity.Blocking, report.Failures[0].Severity);
    }

    [TestMethod]
    public void RunReport_SerializesAndDeserializes()
    {
        var report = builder.CreateSuccessfulRun("run-serialize", "Serialize run");

        var json = JsonSerializer.Serialize(report);
        var roundTrip = JsonSerializer.Deserialize<NexaRunReport>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(report.RunId, roundTrip.RunId);
        Assert.AreEqual(report.Status, roundTrip.Status);
    }

    [TestMethod]
    public void RunReport_PreservesMissionTaskRecipeIds()
    {
        var report = builder.CreateSuccessfulRun("run-ids", "Preserve ids", "mission-123", "task-456", "recipe-789");

        Assert.AreEqual("mission-123", report.MissionId);
        Assert.AreEqual("task-456", report.TaskId);
        Assert.AreEqual("recipe-789", report.RecipeId);
    }

    [TestMethod]
    public void RunReport_PreservesEvidenceRefs()
    {
        var evidence = NodalOsRunReportFixtures.Evidence("evidence-extra");
        var report = new NodalOsRunReportBuilder()
            .Start("run-evidence", "Preserve evidence")
            .AddStep(NodalOsRunReportFixtures.Step())
            .AddEvidence(evidence)
            .Complete(NexaRunStatus.Completed, "done")
            .Build();

        Assert.IsTrue(report.EvidenceRefs.Any(e => e.EvidenceId == "evidence-extra"));
        Assert.IsTrue(report.Steps[0].EvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void FailedRun_RequiresFailureReport()
    {
        var report = new NexaRunReport
        {
            RunId = "run-invalid-failed",
            Goal = "Invalid failed run",
            Status = NexaRunStatus.Failed,
            StartedAt = NodalOsRunReportFixtures.FixedTimestamp,
            CompletedAt = NodalOsRunReportFixtures.FixedTimestamp.AddMinutes(1),
            Steps = [NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.Failed)]
        };

        var result = builder.Validate(report);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "Failed run requires");
    }

    [TestMethod]
    public void BlockedRun_IncludesPolicyDecision()
    {
        var report = builder.CreateBlockedByPolicyRun("run-policy", "Policy block");

        Assert.AreEqual("Blocked", report.PolicyDecisions[0].Decision);
        Assert.AreEqual(NexaRunStatus.Blocked, report.Status);
    }

    [TestMethod]
    public void ApprovalReport_CanBeIncluded()
    {
        var report = new NodalOsRunReportBuilder()
            .Start("run-approval", "Approval report", status: NexaRunStatus.AwaitingApproval)
            .AddStep(NodalOsRunReportFixtures.Step(status: NexaRunStepStatus.WaitingForHuman))
            .AddApproval(NodalOsRunReportFixtures.Approval(status: "Requested"))
            .Build();

        Assert.AreEqual(1, report.Approvals.Count);
        Assert.AreEqual("Requested", report.Approvals[0].Status);
    }

    [TestMethod]
    public void Sanitizer_RemovesSensitiveHeadersOrTokens()
    {
        var value = "Authorization: Bearer abc; cookie=session; api_key=123; password=x";

        var sanitized = NodalOsRunReportSanitizer.Sanitize(value);

        Assert.IsFalse(sanitized.Contains("Authorization", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(sanitized.Contains("Bearer", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(sanitized.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(sanitized.Contains("api_key", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(sanitized.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RunReport_DoesNotIncludeSecrets()
    {
        var report = new NexaRunReport
        {
            RunId = "run-secret",
            Goal = "Inspect report",
            Status = NexaRunStatus.Completed,
            StartedAt = NodalOsRunReportFixtures.FixedTimestamp,
            CompletedAt = NodalOsRunReportFixtures.FixedTimestamp.AddMinutes(1),
            Steps = [NodalOsRunReportFixtures.Step()],
            FinalSummary = "authorization token leaked"
        };

        var result = builder.Validate(report);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "sensitive fields");
    }

    [TestMethod]
    public void Artifact_ValidatesM352Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m352", "failure-taxonomy-run-report-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M350-M352", root.GetProperty("milestone").GetString());
        Assert.AreEqual("RUN_REPORT_AND_FAILURE_TAXONOMY_READY", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("failureTaxonomyCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("troubleshootingMapperCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("allFailureKindsHaveRecommendation").GetBoolean());
        Assert.IsTrue(root.GetProperty("captchaRequiresHuman").GetBoolean());
        Assert.IsTrue(root.GetProperty("twoFactorRequiresHuman").GetBoolean());
        Assert.IsTrue(root.GetProperty("loginRequiresHuman").GetBoolean());
        Assert.IsTrue(root.GetProperty("policyBlockedNoInfiniteRetry").GetBoolean());
        Assert.IsTrue(root.GetProperty("repeatedActionBlocksLoop").GetBoolean());
        Assert.IsTrue(root.GetProperty("sensitiveDataRiskFailClosed").GetBoolean());
        Assert.IsTrue(root.GetProperty("runReportModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("runReportBuilderCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("runReportSerializerCovered").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceRefsPreserved").GetBoolean());
        Assert.IsTrue(root.GetProperty("missionTaskRecipeIdsPreserved").GetBoolean());
        Assert.IsTrue(root.GetProperty("sensitiveFieldsSanitized").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRuntimeActionsIntroduced").GetBoolean());
        Assert.AreEqual("M353-M355 Recipe Manifest / Automation JSON V1", root.GetProperty("nextRecommendedMilestone").GetString());
    }

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
