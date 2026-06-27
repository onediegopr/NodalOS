using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("PerceptionStackFormalIntegrationReports")]
public sealed class PerceptionStackFormalIntegrationReportsTests
{
    [TestMethod]
    public void PerceptionScenarioCatalogReturnsAllScenarios()
    {
        var scenarios = ReliableRecipePerceptionScenarioCatalog.All();

        Assert.AreEqual(11, scenarios.Count);
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "dom_accessibility_ocr_agreement_fixture");
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "captcha_challenge_detected");
    }

    [TestMethod]
    public void ScenarioIdsStable()
    {
        var first = ReliableRecipePerceptionScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();
        var second = ReliableRecipePerceptionScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ScenariosContainNoLiveUrlsSecretsOrScreenshots()
    {
        foreach (var scenario in ReliableRecipePerceptionScenarioCatalog.All())
        {
            var text = ScenarioText(scenario);
            Assert.IsFalse(text.Contains("http://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("https://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("password" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("token" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("authorization:", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains(".png", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains(".jpg", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveBrowser, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveDesktop, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesScreenshotCapture, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesOcrRuntime, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void DomAccessibilityOcrAgreementYieldsHighConfidence()
    {
        var report = Report("dom_accessibility_ocr_agreement_fixture");

        Assert.AreEqual(ReliableRecipePerceptionDecision.FixtureSignalsSufficient, report.OverallDecision);
        Assert.IsTrue(report.SignalAgreementReport.AgreementScore >= 0.9);
        Assert.IsTrue(report.OverallConfidence >= 0.85);
    }

    [TestMethod]
    public void OcrOnlySensitiveTargetBlocked()
    {
        var report = Report("ocr_only_sensitive_target_blocked");

        Assert.AreEqual(ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, report.OverallDecision);
        AssertBlocking(report, ReliableRecipePerceptionBlockingReason.OcrOnlySensitiveTarget);
        Assert.IsFalse(report.ActionAuthorityReports.Single().CanAuthorizeForSensitiveAction);
    }

    [TestMethod]
    public void VisualOnlySensitiveTargetBlocked()
    {
        var report = Report("visual_only_target_blocked");

        Assert.AreEqual(ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, report.OverallDecision);
        AssertBlocking(report, ReliableRecipePerceptionBlockingReason.VisualOnlyTargetNotEnough);
    }

    [TestMethod]
    public void SetOfMarksOnlyTargetNotEnough()
    {
        var report = Report("set_of_marks_only_not_enough");

        Assert.AreEqual(ReliableRecipePerceptionDecision.NeedsMoreSignals, report.OverallDecision);
        AssertBlocking(report, ReliableRecipePerceptionBlockingReason.SetOfMarksOnlyTargetNotEnough);
    }

    [TestMethod]
    public void ContradictoryDomOcrLowersConfidenceAndNeedsReviewOrBlock()
    {
        var report = Report("contradictory_dom_ocr_text");

        Assert.AreEqual(ReliableRecipePerceptionDecision.BlockedContradictorySignals, report.OverallDecision);
        Assert.IsTrue(report.OverallConfidence <= 0.45);
        Assert.IsTrue(report.SignalContradictionReport.Count > 0);
    }

    [TestMethod]
    public void AmbiguousContinueTargetsNeedReview()
    {
        var report = Report("ambiguous_continue_targets");

        Assert.AreEqual(ReliableRecipePerceptionDecision.NeedsHumanReview, report.OverallDecision);
        AssertBlocking(report, ReliableRecipePerceptionBlockingReason.AmbiguousTarget);
    }

    [TestMethod]
    public void HumanCorrectedTargetNeedsReview()
    {
        var report = Report("human_corrected_target_needs_review");

        Assert.AreEqual(ReliableRecipePerceptionDecision.NeedsHumanReview, report.OverallDecision);
        Assert.IsTrue(report.HumanReviewReasons.Count > 0);
    }

    [TestMethod]
    public void CredentialFieldDetectedCreatesHumanReviewSensitiveBlock()
    {
        var report = Report("credential_field_detected");

        Assert.AreEqual(ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, report.OverallDecision);
        Assert.IsTrue(report.SignalReports.Any(s => s.IsSensitive));
        Assert.IsTrue(report.HumanReviewReasons.Any(r => r.Contains("Sensitive action", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void CaptchaChallengeCreatesHumanHandoff()
    {
        var report = Report("captcha_challenge_detected");

        Assert.AreEqual(ReliableRecipePerceptionDecision.NeedsHumanReview, report.OverallDecision);
        Assert.IsTrue(report.HumanReviewReasons.Any(r => r.Contains("Challenge", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void HighConfidenceHighRiskStillBlocked()
    {
        var report = Report("high_confidence_high_risk_still_blocked");

        Assert.AreEqual(ReliableRecipePerceptionDecision.BlockedSensitiveActionAuthority, report.OverallDecision);
        Assert.IsTrue(report.SignalAgreementReport.AgreementScore >= 0.9);
        Assert.IsFalse(report.ActionAuthorityReports.Single().CanAuthorizeForSensitiveAction);
    }

    [TestMethod]
    public void MissingAccessibilityLowRiskWarning()
    {
        var report = Report("missing_accessibility_signal_warning");

        Assert.AreEqual(ReliableRecipePerceptionDecision.FixtureSignalsSufficientWithWarnings, report.OverallDecision);
        CollectionAssert.Contains(report.MissingSignals.ToList(), ReliableRecipePerceptionSignalKind.AccessibilityFixture);
    }

    [TestMethod]
    public void PerceptionBlockedLowersM2Quality()
    {
        var preflight = ReliableRecipeLabFixtureCatalog.Get("ocr_only_sensitive_submit_blocked").PreflightReport;

        Assert.AreEqual(ReliableRecipeQualityDecision.Blocked, preflight.QualityReport.Decision);
        Assert.IsTrue(preflight.QualityReport.OverallScore <= 0.49);
    }

    [TestMethod]
    public void PerceptionFindingAppearsInQualityReport()
    {
        var preflight = ReliableRecipeLabFixtureCatalog.Get("ocr_only_sensitive_submit_blocked").PreflightReport;

        Assert.IsTrue(preflight.QualityReport.BlockingFindings.Any(f => f.Code.Contains("target-resolution-blocked", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void EvalReportIncludesPerceptionSummary()
    {
        var run = ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get("ocr_only_sensitive_submit_blocked_eval"));

        Assert.AreEqual("BlockedSensitiveActionAuthority", run.Report.PerceptionSummary.DecisionLabel);
        Assert.IsTrue(run.Report.PerceptionSummary.TopPerceptionFailureKinds.Contains(nameof(ReliableRecipePerceptionBlockingReason.OcrOnlySensitiveTarget), StringComparer.Ordinal));
    }

    [TestMethod]
    public void ExpectedPerceptionBlockCountsAsExpected()
    {
        var run = ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get("ocr_only_sensitive_submit_blocked_eval"));

        Assert.IsTrue(run.IterationResults.Single().PassedExpectedOutcome);
        CollectionAssert.AreEqual(new[] { 1 }, new[] { run.Metrics.ExpectedBlockIterations });
    }

    [TestMethod]
    public void SandboxReadinessIncludesPerceptionRequirements()
    {
        var report = ComputerUseSandboxReadinessEvaluator.Evaluate(ComputerUseSandboxReadinessScenarioCatalog.Get("ocr_only_sensitive_blocked"));

        Assert.AreEqual("BlockedSensitiveActionAuthority", report.PerceptionSummary.DecisionLabel);
        Assert.IsTrue(report.PerceptionSummary.BlockedReasons.Contains(nameof(ReliableRecipePerceptionBlockingReason.OcrOnlySensitiveTarget), StringComparer.Ordinal));
    }

    [TestMethod]
    public void OcrOnlySensitiveBlocksSandboxFutureCandidate()
    {
        var report = ComputerUseSandboxReadinessEvaluator.Evaluate(ComputerUseSandboxReadinessScenarioCatalog.Get("ocr_only_sensitive_blocked"));

        Assert.AreNotEqual(ComputerUseAllowedAssessmentMode.FutureSandboxCandidate, report.AllowedAssessmentMode);
        Assert.IsTrue(report.FutureUnlockConditions.Any(c => c.Contains("perception blocker", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void LabPerceptionPanelIncludesFixtureOnlyNotice()
    {
        var lab = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;

        Assert.IsTrue(lab.PerceptionIntegrationPanel.FixtureOnlyNotice.Contains("Fixture perception report", StringComparison.Ordinal));
        Assert.IsTrue(lab.PerceptionIntegrationPanel.ReadOnly);
        Assert.IsTrue(lab.PerceptionIntegrationPanel.FixtureOnly);
    }

    [TestMethod]
    public void LabPerceptionPanelExposesNoLiveActionLabels()
    {
        var lab = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;
        var labels = string.Join(" ", lab.PerceptionIntegrationPanel.ReadOnlyActionLabels);

        Assert.IsFalse(labels.Contains("Run perception", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Capture screen", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Analyze browser now", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(lab.PerceptionIntegrationPanel.CanRunPerception);
        Assert.IsFalse(lab.PerceptionIntegrationPanel.CanCaptureScreen);
        Assert.IsFalse(lab.PerceptionIntegrationPanel.CanAnalyzeBrowserNow);
        Assert.IsFalse(lab.PerceptionIntegrationPanel.CanAuthorizeClick);
    }

    [TestMethod]
    public void OcrDisplayedOnlyAsSupportingSignal()
    {
        var report = Report("dom_accessibility_ocr_agreement_fixture");
        var ocr = report.SignalReports.Single(s => s.SignalKind == ReliableRecipePerceptionSignalKind.OcrFixture);

        Assert.IsFalse(ocr.IsActionAuthorityEligible);
        Assert.IsTrue(ocr.Notes.Contains("supporting", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ActionAuthorityBlockedForSensitiveWeakSignals()
    {
        var report = Report("ocr_only_sensitive_target_blocked");
        var authority = report.ActionAuthorityReports.Single();

        Assert.IsFalse(authority.CanAuthorizeForFixtureDryRun);
        Assert.IsFalse(authority.CanAuthorizeForSensitiveAction);
        Assert.IsTrue(authority.RequiredHumanReview);
    }

    [TestMethod]
    public void RepeatedPerceptionReportDeterministic()
    {
        var first = Report("contradictory_dom_ocr_text");
        var second = Report("contradictory_dom_ocr_text");

        Assert.AreEqual(first.OverallDecision, second.OverallDecision);
        Assert.AreEqual(first.OverallConfidence, second.OverallConfidence);
        CollectionAssert.AreEqual(first.SignalContradictionReport.ToArray(), second.SignalContradictionReport.ToArray());
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in ReliableRecipePerceptionScenarioCatalog.All())
        {
            var report = ReliableRecipePerceptionIntegrationEvaluator.Evaluate(scenario);
            Assert.IsFalse(report.LivePerceptionEnabled, scenario.ScenarioId);
            Assert.IsFalse(report.BrowserDomLiveCaptureEnabled, scenario.ScenarioId);
            Assert.IsFalse(report.AccessibilityLiveCaptureEnabled, scenario.ScenarioId);
            Assert.IsFalse(report.ProviderOrVlmCallEnabled, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoOcrInternalsTouched()
    {
        var report = Report("ocr_only_sensitive_target_blocked");

        Assert.IsFalse(report.OcrLiveActivationEnabled);
        Assert.IsTrue(report.SignalReports.All(s => s.SignalKind != ReliableRecipePerceptionSignalKind.OcrFixture || !s.IsActionAuthorityEligible));
    }

    [TestMethod]
    public void NoScreenshotLiveCaptureAdded()
    {
        foreach (var scenario in ReliableRecipePerceptionScenarioCatalog.All())
        {
            var report = ReliableRecipePerceptionIntegrationEvaluator.Evaluate(scenario);
            Assert.IsFalse(report.ScreenshotCaptureEnabled, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesScreenshotCapture, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        foreach (var scenario in ReliableRecipePerceptionScenarioCatalog.All())
            Assert.IsFalse(scenario.UsesLiveDesktop, scenario.ScenarioId);
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        var report = ComputerUseSandboxReadinessEvaluator.Evaluate(ComputerUseSandboxReadinessScenarioCatalog.Get("safe_invoice_fixture_ready"));

        Assert.IsFalse(report.SandboxRuntimeEnabled);
        Assert.IsTrue(report.PerceptionSummary.FixtureOnly);
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var scenario in ReliableRecipePerceptionScenarioCatalog.All())
        {
            var report = ReliableRecipePerceptionIntegrationEvaluator.Evaluate(scenario);
            Assert.IsTrue(report.ReadOnly, scenario.ScenarioId);
            Assert.IsTrue(report.FixtureOnly, scenario.ScenarioId);
            Assert.IsFalse(report.ActionAuthorityGranted, scenario.ScenarioId);
            Assert.IsTrue(report.FixtureOnlyNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
        }
    }

    private static ReliableRecipePerceptionIntegrationReport Report(string scenarioId) =>
        ReliableRecipePerceptionIntegrationEvaluator.Evaluate(ReliableRecipePerceptionScenarioCatalog.Get(scenarioId));

    private static void AssertBlocking(ReliableRecipePerceptionIntegrationReport report, ReliableRecipePerceptionBlockingReason reason) =>
        CollectionAssert.Contains(report.ActionAuthorityReports.SelectMany(a => a.BlockingReasons).ToList(), reason);

    private static string ScenarioText(ReliableRecipePerceptionScenario scenario) =>
        string.Join(" ",
            scenario.ScenarioId,
            scenario.SubjectId,
            scenario.TargetLabel,
            scenario.Summary,
            string.Join(" ", scenario.Signals.Select(s => $"{s.SignalId} {s.Notes}")));
}
