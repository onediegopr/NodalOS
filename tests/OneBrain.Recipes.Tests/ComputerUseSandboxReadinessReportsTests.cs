using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ComputerUseSandboxReadinessReports")]
public sealed class ComputerUseSandboxReadinessReportsTests
{
    [TestMethod]
    public void SandboxReadinessScenarioCatalogReturnsAllScenarios()
    {
        var scenarios = ComputerUseSandboxReadinessScenarioCatalog.All();

        Assert.AreEqual(11, scenarios.Count);
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "safe_invoice_fixture_ready");
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "unexpected_pass_regression_blocked");
    }

    [TestMethod]
    public void ScenarioIdsStable()
    {
        var first = ComputerUseSandboxReadinessScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();
        var second = ComputerUseSandboxReadinessScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ScenariosContainNoLiveUrlsOrSecrets()
    {
        foreach (var scenario in ComputerUseSandboxReadinessScenarioCatalog.All())
        {
            var text = string.Join(" ", scenario.ScenarioId, scenario.SubjectId, scenario.Summary);
            Assert.IsFalse(text.Contains("http://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("https://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("password" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("token" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("authorization:", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(scenario.RuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveSandbox, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void SafeInvoiceFixtureReady()
    {
        var report = Report("safe_invoice_fixture_ready");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.FixtureReady, report.OverallDecision);
        Assert.AreEqual(ComputerUseAllowedAssessmentMode.FixtureEvaluationOnly, report.AllowedAssessmentMode);
        Assert.IsTrue(report.ReadinessScore >= 0.8);
    }

    [TestMethod]
    public void InvoiceMissingValidationNeedsReview()
    {
        var report = Report("invoice_missing_validation_needs_review");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview, report.OverallDecision);
        CollectionAssert.Contains(report.MissingRequirements.ToList(), ComputerUseSandboxRequirementKind.ValidationPolicy);
    }

    [TestMethod]
    public void OcrOnlySensitiveTargetBlocked()
    {
        var report = Report("ocr_only_sensitive_blocked");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedByPolicy, report.OverallDecision);
        CollectionAssert.Contains(report.RiskReasons.ToList(), "ocr-only-sensitive-action");
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.OcrLiveCapture);
    }

    [TestMethod]
    public void PasswordCredentialPolicyBlocked()
    {
        var report = Report("password_credential_policy_blocked");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedMissingRequirements, report.OverallDecision);
        CollectionAssert.Contains(report.MissingRequirements.ToList(), ComputerUseSandboxRequirementKind.CredentialPolicy);
        CollectionAssert.Contains(report.MissingRequirements.ToList(), ComputerUseSandboxRequirementKind.SecretHandlingPolicy);
    }

    [TestMethod]
    public void CaptchaTwoFactorHandoffBlockedAndNoBypassCapability()
    {
        var report = Report("captcha_two_factor_handoff_blocked");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedMissingRequirements, report.OverallDecision);
        CollectionAssert.Contains(report.MissingRequirements.ToList(), ComputerUseSandboxRequirementKind.ChallengeHandlingPolicy);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.CaptchaBypass);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.TwoFactorBypass);
    }

    [TestMethod]
    public void AmbiguousTargetNeedsReview()
    {
        var report = Report("ambiguous_target_needs_review");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.DesignOnlyNeedsReview, report.OverallDecision);
        Assert.IsTrue(report.RiskReasons.Contains("preflight-blocked", StringComparer.Ordinal));
    }

    [TestMethod]
    public void GovernmentSubmitPolicyBlocked()
    {
        var report = Report("government_submit_policy_blocked");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedByPolicy, report.OverallDecision);
        Assert.IsTrue(report.RiskReasons.Any(r => r.Contains("government", StringComparison.OrdinalIgnoreCase) || r.Contains("high-risk", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void DesktopFutureLiveBlocked()
    {
        var report = Report("desktop_future_live_blocked");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedLiveRuntimeNotAllowed, report.OverallDecision);
        Assert.AreEqual(ComputerUseRequiredIsolationMode.DesktopProfileFuture, report.RequiredIsolationMode);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.DesktopLive);
    }

    [TestMethod]
    public void BrowserFutureProfileRequiredButNotEnabled()
    {
        var report = Report("browser_future_profile_required");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.FixtureReadyWithWarnings, report.OverallDecision);
        Assert.AreEqual(ComputerUseRequiredIsolationMode.BrowserProfileFuture, report.RequiredIsolationMode);
        Assert.IsFalse(report.BrowserLiveLaunched);
        Assert.IsTrue(report.FutureUnlockConditions.Any(c => c.Contains("browser profile", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void HighQualityHighRiskStillBlocked()
    {
        var report = Report("high_quality_high_risk_still_blocked");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedByPolicy, report.OverallDecision);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.PaymentOrPublish);
    }

    [TestMethod]
    public void UnexpectedPassRegressionBlocked()
    {
        var report = Report("unexpected_pass_regression_blocked");

        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedByPolicy, report.OverallDecision);
        CollectionAssert.Contains(report.RiskReasons.ToList(), "unexpected-pass-regression");
        Assert.AreEqual(ComputerUseRequiredIsolationMode.NotAllowed, report.RequiredIsolationMode);
    }

    [TestMethod]
    public void PreflightBlockedCannotBeOverriddenBySandboxReadiness()
    {
        var report = Report("government_submit_policy_blocked");

        Assert.AreNotEqual(ComputerUseSandboxReadinessDecision.FixtureReady, report.OverallDecision);
        Assert.AreNotEqual(ComputerUseAllowedAssessmentMode.FixtureEvaluationOnly, report.AllowedAssessmentMode);
    }

    [TestMethod]
    public void EvalRegressionCannotBeMadeReady()
    {
        var scenario = ReliableRecipeEvalScenarioCatalog.Get("unexpected_pass_regression_fixture");
        var run = ReliableRecipeFixtureEvalRunner.Evaluate(scenario);
        var report = ComputerUseSandboxReadinessEvaluator.Evaluate(scenario, run);

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.RegressionDetected, run.FinalDecision);
        Assert.AreEqual(ComputerUseSandboxReadinessDecision.BlockedByPolicy, report.OverallDecision);
    }

    [TestMethod]
    public void FutureCandidateDoesNotImplyRuntimeEnabled()
    {
        var report = Report("browser_future_profile_required");

        Assert.AreEqual(ComputerUseAllowedAssessmentMode.FutureSandboxCandidate, report.AllowedAssessmentMode);
        Assert.IsFalse(report.SandboxRuntimeEnabled);
        Assert.IsFalse(report.BrowserLiveLaunched);
        Assert.IsFalse(report.NetworkCallEnabled);
    }

    [TestMethod]
    public void ReportIncludesBlockedCapabilities()
    {
        var report = Report("safe_invoice_fixture_ready");

        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.BrowserLive);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.ShellExecution);
    }

    [TestMethod]
    public void ReportIncludesMissingRequirements()
    {
        var report = Report("invoice_missing_validation_needs_review");

        Assert.IsTrue(report.MissingRequirements.Count > 0);
    }

    [TestMethod]
    public void ReportIncludesFutureUnlockConditions()
    {
        var report = Report("desktop_future_live_blocked");

        Assert.IsTrue(report.FutureUnlockConditions.Any(c => c.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase) || c.Contains("desktop", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ReportIncludesFixtureOnlyNoRuntimeNotice()
    {
        var report = Report("safe_invoice_fixture_ready");

        Assert.IsTrue(report.FixtureOnlyNotice.Contains("Fixture-only", StringComparison.Ordinal));
        Assert.IsTrue(report.FixtureOnlyNotice.Contains("runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LabSandboxPanelIncludesFixtureOnlyNotice()
    {
        var lab = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;

        Assert.IsTrue(lab.SandboxReadinessReportPanel.FixtureOnlyNotice.Contains("Fixture-only", StringComparison.Ordinal));
        Assert.IsTrue(lab.SandboxReadinessReportPanel.ReadOnly);
        Assert.IsTrue(lab.SandboxReadinessReportPanel.FixtureOnly);
    }

    [TestMethod]
    public void LabSandboxPanelExposesNoLiveActionLabels()
    {
        var lab = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;
        var labels = string.Join(" ", lab.SandboxReadinessReportPanel.ReadOnlyActionLabels);

        Assert.IsFalse(labels.Contains("Run in sandbox", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Launch sandbox", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Execute isolated", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(lab.SandboxReadinessReportPanel.CanRunInSandbox);
        Assert.IsFalse(lab.SandboxReadinessReportPanel.CanLaunchSandbox);
        Assert.IsFalse(lab.SandboxReadinessReportPanel.CanExecuteIsolated);
    }

    [TestMethod]
    public void EvalReportSandboxSummaryIncludesExpectedBlockExplanation()
    {
        var run = ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get("ocr_only_sensitive_submit_blocked_eval"));

        Assert.AreEqual("BlockedByPolicy", run.Report.SandboxReadinessSummary.DecisionLabel);
        Assert.IsTrue(run.Report.SandboxReadinessSummary.BlockedCapabilities.Contains(nameof(ComputerUseSandboxBlockedCapability.OcrLiveCapture), StringComparer.Ordinal));
        Assert.IsTrue(run.Report.SandboxReadinessSummary.FixtureOnlyNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void DeterministicRepeatedReadinessReport()
    {
        var first = Report("desktop_future_live_blocked");
        var second = Report("desktop_future_live_blocked");

        Assert.AreEqual(first.OverallDecision, second.OverallDecision);
        CollectionAssert.AreEqual(first.MissingRequirements.ToArray(), second.MissingRequirements.ToArray());
        CollectionAssert.AreEqual(first.RiskReasons.ToArray(), second.RiskReasons.ToArray());
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in ComputerUseSandboxReadinessScenarioCatalog.All())
        {
            var report = ComputerUseSandboxReadinessEvaluator.Evaluate(scenario);
            Assert.IsFalse(report.BrowserLiveLaunched, scenario.ScenarioId);
            Assert.IsFalse(report.DesktopLiveLaunched, scenario.ScenarioId);
            Assert.IsFalse(report.NetworkCallEnabled, scenario.ScenarioId);
            Assert.IsFalse(report.ShellOrProcessRunnerEnabled, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoOcrInternalsTouched()
    {
        var report = Report("ocr_only_sensitive_blocked");

        Assert.IsFalse(report.OcrLiveActivationEnabled);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.OcrLiveCapture);
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var report = Report("password_credential_policy_blocked");

        Assert.IsFalse(report.RecorderRuntimeEnabled);
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), ComputerUseSandboxBlockedCapability.RecorderLive);
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        foreach (var scenario in ComputerUseSandboxReadinessScenarioCatalog.All())
        {
            var report = ComputerUseSandboxReadinessEvaluator.Evaluate(scenario);
            Assert.IsFalse(report.SandboxRuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(report.VmOrContainerCreated, scenario.ScenarioId);
            Assert.IsFalse(report.DockerEnabled, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var scenario in ComputerUseSandboxReadinessScenarioCatalog.All())
        {
            var report = ComputerUseSandboxReadinessEvaluator.Evaluate(scenario);
            Assert.IsTrue(report.ReadOnly, scenario.ScenarioId);
            Assert.IsTrue(report.FixtureOnly, scenario.ScenarioId);
            Assert.IsFalse(report.FilesystemWriteEnabled, scenario.ScenarioId);
            Assert.IsFalse(report.ShellOrProcessRunnerEnabled, scenario.ScenarioId);
        }
    }

    private static ComputerUseSandboxReadinessReport Report(string scenarioId) =>
        ComputerUseSandboxReadinessEvaluator.Evaluate(ComputerUseSandboxReadinessScenarioCatalog.Get(scenarioId));
}
