using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ReliableRecipeEvalHarnessFixtureScenarios")]
public sealed class ReliableRecipeEvalHarnessFixtureScenariosTests
{
    [TestMethod]
    public void EvalScenarioCatalogReturnsAllScenarios()
    {
        var scenarios = ReliableRecipeEvalScenarioCatalog.All();

        Assert.AreEqual(12, scenarios.Count);
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "safe_invoice_download_dry_run_candidate_eval");
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "unexpected_pass_regression_fixture");
    }

    [TestMethod]
    public void ScenarioIdsAreStable()
    {
        var first = ReliableRecipeEvalScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();
        var second = ReliableRecipeEvalScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ScenariosContainNoLiveUrlsOrSecrets()
    {
        foreach (var scenario in ReliableRecipeEvalScenarioCatalog.All())
        {
            var text = ScenarioText(scenario);
            Assert.IsFalse(text.Contains("http://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("https://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("password" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("token" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("authorization:", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void SafeInvoiceScenarioEvaluatesFixturePass()
    {
        var run = Run("safe_invoice_download_dry_run_candidate_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePass, run.FinalDecision);
        Assert.IsTrue(Math.Abs(run.Metrics.ExpectedOutcomeMatchRate - 1.0) < 0.001);
        Assert.IsTrue(Math.Abs(run.Metrics.FlakinessScore) < 0.001);
    }

    [TestMethod]
    public void MissingValidationScenarioMatchesExpectedValidationFailure()
    {
        var run = Run("invoice_download_missing_validation_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.ValidationMissing);
        Assert.IsTrue(run.IterationResults.All(r => r.PassedExpectedOutcome));
    }

    [TestMethod]
    public void OcrOnlySensitiveScenarioBlockedAsExpected()
    {
        var run = Run("ocr_only_sensitive_submit_blocked_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.OcrOnlySensitiveTarget);
        Assert.IsTrue(run.Report.ProductFacingSummaries.Any(s => s.Contains("Fixture eval only", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PasswordRecorderScenarioRedactsAndNeedsReviewDraftOnly()
    {
        var run = Run("recorder_password_redaction_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.SecretExposureBlocked);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.RecorderDraftNotReviewed);
    }

    [TestMethod]
    public void CaptchaTwoFactorScenarioProducesHumanHandoffExpectedOutcome()
    {
        var run = Run("captcha_two_factor_handoff_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.HumanHandoffRequired);
        Assert.IsTrue(run.Metrics.HumanInterventionRate > 0);
    }

    [TestMethod]
    public void AmbiguousTargetScenarioNeedsReview()
    {
        var run = Run("ambiguous_target_needs_review_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.TargetAmbiguous);
    }

    [TestMethod]
    public void GovernmentSubmitHighRiskBlocked()
    {
        var run = Run("government_submit_high_risk_blocked_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.RiskBlocked);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.PolicyBlocked);
    }

    [TestMethod]
    public void DesktopFutureSandboxBlocked()
    {
        var run = Run("desktop_future_sandbox_blocked_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.SandboxNotReady);
    }

    [TestMethod]
    public void CorrectedClickScenarioNeedsReview()
    {
        var run = Run("corrected_user_click_review_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.RecorderDraftNotReviewed);
    }

    [TestMethod]
    public void HighQualityHighRiskRemainsBlocked()
    {
        var run = Run("high_quality_high_risk_still_blocked_eval");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.FixturePassWithWarnings, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.RiskBlocked);
        Assert.IsTrue(run.IterationResults.Single().TargetResolutionScore >= 0.85);
    }

    [TestMethod]
    public void ExpectedBlockCountedAsExpectedOutcomeMatch()
    {
        var run = Run("ocr_only_sensitive_submit_blocked_eval");

        CollectionAssert.AreEqual(new[] { 1 }, new[] { run.Metrics.ExpectedBlockIterations });
        Assert.IsTrue(Math.Abs(run.Metrics.ExpectedOutcomeMatchRate - 1.0) < 0.001);
        Assert.IsTrue(run.IterationResults.Single().PassedExpectedOutcome);
    }

    [TestMethod]
    public void UnexpectedPassFlaggedAsRegression()
    {
        var run = Run("unexpected_pass_regression_fixture");

        Assert.AreEqual(ReliableRecipeFixtureEvalFinalDecision.RegressionDetected, run.FinalDecision);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.UnexpectedPass);
        Assert.AreEqual(1, run.Metrics.UnexpectedPassIterations);
    }

    [TestMethod]
    public void FailureTaxonomyIncludesCorrectFailureKind()
    {
        var run = Run("desktop_future_sandbox_blocked_eval");

        CollectionAssert.Contains(run.Report.FailureTaxonomy.ToList(), ReliableRecipeFixtureEvalFailureKind.SandboxNotReady);
    }

    [TestMethod]
    public void EvidenceCompletenessContributesToMetrics()
    {
        var run = Run("invoice_download_missing_validation_eval");

        Assert.AreEqual(run.IterationResults.Average(r => r.EvidenceScore), run.Metrics.EvidenceCompletenessScore, 0.001);
        Assert.IsTrue(run.Metrics.AverageEvidenceScore <= 1);
    }

    [TestMethod]
    public void ValidationCompletenessContributesToMetrics()
    {
        var run = Run("invoice_download_missing_validation_eval");

        Assert.AreEqual(run.IterationResults.Average(r => r.ValidationScore), run.Metrics.ValidationCompletenessScore, 0.001);
        Assert.IsTrue(run.Metrics.AverageValidationScore < 1);
    }

    [TestMethod]
    public void TargetResolutionScoreContributesToMetrics()
    {
        var run = Run("safe_invoice_download_dry_run_candidate_eval");

        Assert.AreEqual(run.IterationResults.Average(r => r.TargetResolutionScore), run.Metrics.AverageTargetResolutionScore, 0.001);
        Assert.IsTrue(run.Metrics.AverageTargetResolutionScore >= 0.85);
    }

    [TestMethod]
    public void SandboxReadinessScoreContributesToMetrics()
    {
        var run = Run("desktop_future_sandbox_blocked_eval");

        Assert.AreEqual(run.IterationResults.Average(r => r.SandboxReadinessScore), run.Metrics.AverageSandboxReadinessScore, 0.001);
        Assert.IsTrue(run.Metrics.AverageSandboxReadinessScore < 0.9);
    }

    [TestMethod]
    public void HumanInterventionRateComputed()
    {
        var run = Run("captcha_two_factor_handoff_eval");

        Assert.AreEqual(1.0, run.Metrics.HumanInterventionRate);
    }

    [TestMethod]
    public void FlakinessScoreDeterministic()
    {
        var first = Run("predefined_flaky_fixture_eval");
        var second = Run("predefined_flaky_fixture_eval");

        Assert.AreEqual(first.Metrics.FlakinessScore, second.Metrics.FlakinessScore);
        Assert.AreEqual(first.Report.Flakiness.FlakinessLevel, second.Report.Flakiness.FlakinessLevel);
    }

    [TestMethod]
    public void PredefinedVariantScenarioProducesNonZeroFlakiness()
    {
        var run = Run("predefined_flaky_fixture_eval");

        Assert.IsTrue(run.Metrics.FlakinessScore > 0);
        Assert.IsTrue(run.Report.Flakiness.InconsistentSignals.Count > 0);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.FixtureMismatch);
    }

    [TestMethod]
    public void RepeatedEvalRunDeterministic()
    {
        var first = Run("government_submit_high_risk_blocked_eval");
        var second = Run("government_submit_high_risk_blocked_eval");

        Assert.AreEqual(first.FinalDecision, second.FinalDecision);
        Assert.IsTrue(Math.Abs(first.Metrics.ExpectedOutcomeMatchRate - second.Metrics.ExpectedOutcomeMatchRate) < 0.001);
        CollectionAssert.AreEqual(first.Report.FailureTaxonomy.ToArray(), second.Report.FailureTaxonomy.ToArray());
    }

    [TestMethod]
    public void LabEvalPanelIncludesFixtureOnlyNotice()
    {
        var run = Run("safe_invoice_download_dry_run_candidate_eval");

        Assert.AreEqual("Fixture-only evaluation. Runtime not enabled.", run.Report.LabEvalPanel.EvalNotice.Message);
        Assert.IsTrue(run.Report.LabEvalPanel.ReadOnly);
        Assert.IsTrue(run.Report.LabEvalPanel.FixtureOnly);
    }

    [TestMethod]
    public void LabEvalPanelExposesNoLiveActionLabels()
    {
        foreach (var scenario in ReliableRecipeEvalScenarioCatalog.All())
        {
            var panel = ReliableRecipeFixtureEvalRunner.Evaluate(scenario).Report.LabEvalPanel;
            var labels = string.Join(" ", panel.ReadOnlyActionLabels);
            Assert.IsFalse(labels.Contains("Run eval live", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(labels.Contains("Execute recipe", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(labels.Contains("Replay", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(panel.CanRunEvalLive, scenario.ScenarioId);
            Assert.IsFalse(panel.CanExecuteRecipe, scenario.ScenarioId);
            Assert.IsFalse(panel.CanReplayRecording, scenario.ScenarioId);
            Assert.IsFalse(panel.CanCreateBrowserOrDesktopRuntime, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void EvalReportSaysRuntimeNotEnabled()
    {
        var run = Run("safe_invoice_download_dry_run_candidate_eval");

        Assert.IsTrue(run.Report.ProductFacingSummaries.Any(s => s.Contains("Runtime not enabled", StringComparison.Ordinal)));
        Assert.IsFalse(run.Report.RuntimeEnabled);
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in ReliableRecipeEvalScenarioCatalog.All())
        {
            var run = ReliableRecipeFixtureEvalRunner.Evaluate(scenario);
            Assert.IsTrue(run.FixtureOnly, scenario.ScenarioId);
            Assert.IsFalse(run.Report.BrowserExecutionEnabled, scenario.ScenarioId);
            Assert.IsFalse(run.Report.DesktopExecutionEnabled, scenario.ScenarioId);
            Assert.IsFalse(run.Report.ProviderCallEnabled, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoOcrInternalsTouched()
    {
        var run = Run("ocr_only_sensitive_submit_blocked_eval");

        Assert.IsFalse(run.Report.OcrLiveActivationEnabled);
        AssertFailure(run, ReliableRecipeFixtureEvalFailureKind.OcrOnlySensitiveTarget);
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var run = Run("recorder_password_redaction_eval");

        Assert.IsFalse(run.Report.RecorderRuntimeEnabled);
        Assert.IsTrue(run.Report.FixtureOnly);
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var scenario in ReliableRecipeEvalScenarioCatalog.All())
        {
            var run = ReliableRecipeFixtureEvalRunner.Evaluate(scenario);
            Assert.IsFalse(scenario.LiveRuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesNetwork, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesProviderOrLlm, scenario.ScenarioId);
            Assert.IsFalse(run.LiveEvalAdded, scenario.ScenarioId);
            Assert.IsTrue(run.RuntimeNotEnabled, scenario.ScenarioId);
            Assert.IsTrue(run.Report.Deterministic, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void AttachEvalPanelKeepsLabReadOnly()
    {
        var lab = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;
        var run = Run("safe_invoice_download_dry_run_candidate_eval");
        var updated = ReliableRecipeFixtureEvalRunner.AttachEvalPanel(lab, run);

        Assert.AreEqual(run.Report.LabEvalPanel.ScenarioId, updated.EvalPanel.ScenarioId);
        Assert.IsTrue(updated.ReadOnly);
        Assert.IsFalse(updated.CanStartRecipeRun);
        Assert.IsFalse(updated.EvalPanel.CanRunEvalLive);
    }

    private static ReliableRecipeFixtureEvalRun Run(string scenarioId) =>
        ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get(scenarioId));

    private static void AssertFailure(ReliableRecipeFixtureEvalRun run, ReliableRecipeFixtureEvalFailureKind kind) =>
        CollectionAssert.Contains(run.Report.FailureTaxonomy.ToList(), kind, $"{run.ScenarioId}:{kind}");

    private static string ScenarioText(ReliableRecipeFixtureEvalScenario scenario) =>
        string.Join(" ",
            scenario.ScenarioId,
            scenario.Name,
            scenario.Description,
            scenario.FixtureId,
            scenario.InitialFixtureState,
            string.Join(" ", scenario.Tags),
            string.Join(" ", scenario.IterationProfiles.Select(p => $"{p.VariantId} {p.VariantSummary}")));
}
