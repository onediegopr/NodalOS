using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("StructuredEvidenceValidationPrerequisites")]
public sealed class StructuredEvidenceValidationPrerequisitesTests
{
    [TestMethod]
    public void ScenarioCatalogReturnsAllScenarios()
    {
        var scenarios = ReliableRecipeStructuredPrerequisiteScenarioCatalog.All();

        Assert.AreEqual(12, scenarios.Count);
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "complete_explicit_invoice_download_prerequisites");
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "legacy_mapped_requirements_pass_with_warning");
    }

    [TestMethod]
    public void ScenarioIdsStable()
    {
        var first = ReliableRecipeStructuredPrerequisiteScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();
        var second = ReliableRecipeStructuredPrerequisiteScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ScenariosContainNoLiveUrlsOrSecrets()
    {
        foreach (var scenario in ReliableRecipeStructuredPrerequisiteScenarioCatalog.All())
        {
            var text = string.Join(" ", scenario.ScenarioId, scenario.Summary, scenario.Recipe.Id, string.Join(" ", scenario.Recipe.Blocks.SelectMany(b => b.EvidenceExpectations.Concat(b.ValidationRequirements))));
            Assert.IsFalse(text.Contains("http://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("https://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("password=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("token=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("authorization:", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(scenario.RuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveBrowser, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveDesktop, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesNetwork, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void CompleteExplicitInvoicePrerequisitesPass()
    {
        var profile = Profile("complete_explicit_invoice_download_prerequisites");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.PassDesignOnly, profile.AdapterGateDecision);
        Assert.AreEqual(1, profile.CompletenessReport.ExplicitRequirementRatio);
        Assert.AreEqual(0, profile.CompletenessReport.MissingCriticalRequirements.Count);
    }

    [TestMethod]
    public void InferredDownloadRequirementsWarn()
    {
        var profile = Profile("inferred_download_requirements_warn");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.PassWithWarnings, profile.AdapterGateDecision);
        Assert.IsTrue(profile.CompletenessReport.Warnings.Any(w => w.Contains("Inferred requirement", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void MissingDownloadArtifactEvidenceBlocks()
    {
        var profile = Profile("missing_download_artifact_evidence_blocks");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        AssertMissing(profile, "DownloadArtifactRef");
    }

    [TestMethod]
    public void SubmitWithoutExplicitPostValidationBlocks()
    {
        var profile = Profile("submit_without_explicit_post_validation_blocks");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        AssertMissing(profile, "ExternalSideEffectConfirmation");
    }

    [TestMethod]
    public void ExternalSideEffectMissingApprovalEvidenceBlocks()
    {
        var profile = Profile("external_side_effect_missing_approval_evidence_blocks");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        Assert.IsTrue(profile.CompletenessReport.BlockingFindings.Any(f => f.Contains("approval evidence", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void OcrOnlySensitiveMissingPerceptionValidationBlocks()
    {
        var scenario = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("ocr_only_sensitive_missing_perception_validation_blocks");
        var perception = ReliableRecipePerceptionIntegrationEvaluator.Evaluate(ReliableRecipePerceptionScenarioCatalog.Get("ocr_only_sensitive_target_blocked"));
        var preflight = ReliableRecipePreflightComposer.Compose(scenario.Recipe, ReliableRecipeRunMode.DryRun);
        var profile = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(scenario.Recipe, preflight, perceptionReport: perception, subjectId: scenario.ScenarioId);

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        AssertMissing(profile, "PerceptionConfidenceAssertion");
        Assert.IsTrue(profile.EvidenceRequirements.Any(r => r.Kind == StructuredEvidenceRequirementKind.OcrSupportingSignalRef));
    }

    [TestMethod]
    public void RecorderDraftMissingHumanReviewEvidenceBlocks()
    {
        var profile = Profile("recorder_draft_missing_human_review_evidence_blocks");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        AssertMissing(profile, "HumanIntervention");
    }

    [TestMethod]
    public void CaptchaHandoffMissingHumanInterventionEvidenceBlocks()
    {
        var profile = Profile("captcha_handoff_missing_human_intervention_evidence_blocks");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        AssertMissing(profile, "HumanIntervention");
    }

    [TestMethod]
    public void SandboxFutureMissingReadinessAssertionBlocks()
    {
        var profile = Profile("sandbox_future_missing_readiness_assertion_blocks");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        AssertMissing(profile, "SandboxReadinessAssertion");
    }

    [TestMethod]
    public void EvalScenarioMissingExpectedOutcomeAssertionBlocks()
    {
        var scenario = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("eval_scenario_missing_expected_outcome_assertion_blocks");
        var preflight = ReliableRecipePreflightComposer.Compose(scenario.Recipe, ReliableRecipeRunMode.DryRun);
        var eval = ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get("safe_invoice_download_dry_run_candidate_eval"));
        var profile = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(scenario.Recipe, preflight, evalRun: eval, subjectKind: ReliableRecipeStructuredPrerequisiteSubjectKind.EvalScenario, subjectId: scenario.ScenarioId);

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedMissingCriticalRequirements, profile.AdapterGateDecision);
        AssertMissing(profile, "EvalExpectedOutcomeAssertion");
    }

    [TestMethod]
    public void HighRiskWithExplicitRequirementsStillPolicyBlocked()
    {
        var profile = Profile("high_risk_with_explicit_requirements_still_policy_blocked");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.BlockedByPolicy, profile.AdapterGateDecision);
        Assert.AreEqual(0, profile.CompletenessReport.MissingCriticalRequirements.Count);
    }

    [TestMethod]
    public void LegacyMappedRequirementsPassWithWarning()
    {
        var profile = Profile("legacy_mapped_requirements_pass_with_warning");

        Assert.AreEqual(ReliableRecipeStructuredAdapterGateDecision.PassWithWarnings, profile.AdapterGateDecision);
        Assert.IsTrue(profile.EvidenceRequirements.Any(r => r.Source == ReliableRecipeRequirementSource.MappedFromLegacyContract));
        Assert.IsTrue(profile.CompletenessReport.Warnings.Any(w => w.Contains("legacy", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ExplicitRequirementRatioComputed()
    {
        var profile = Profile("complete_explicit_invoice_download_prerequisites");

        Assert.AreEqual(1, profile.CompletenessReport.ExplicitRequirementRatio);
    }

    [TestMethod]
    public void InferredRequirementRatioComputed()
    {
        var profile = Profile("missing_download_artifact_evidence_blocks");

        Assert.IsTrue(profile.CompletenessReport.InferredRequirementRatio > 0);
    }

    [TestMethod]
    public void MissingCriticalRequirementsListed()
    {
        var profile = Profile("submit_without_explicit_post_validation_blocks");

        Assert.IsTrue(profile.CompletenessReport.MissingCriticalRequirements.Count > 0);
    }

    [TestMethod]
    public void M2QualityIncludesStructuredCompleteness()
    {
        var fixture = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass");

        Assert.IsNotNull(fixture.PreflightReport.QualityReport.StructuredPrerequisites);
        Assert.IsTrue(fixture.PreflightReport.QualityReport.StructuredPrerequisites.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void M5EvalIncludesStructuredSummary()
    {
        var run = ReliableRecipeFixtureEvalRunner.Evaluate(ReliableRecipeEvalScenarioCatalog.Get("safe_invoice_download_dry_run_candidate_eval"));

        Assert.IsNotNull(run.Report.StructuredPrerequisiteSummary);
        Assert.IsTrue(run.Report.StructuredPrerequisiteSummary.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void M6SandboxFutureCandidateBlockedWithoutStructuredRequirements()
    {
        var scenario = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("missing_download_artifact_evidence_blocks");
        var preflight = ReliableRecipePreflightComposer.Compose(scenario.Recipe, ReliableRecipeRunMode.DryRun);
        var sandbox = ComputerUseSandboxReadinessEvaluator.Evaluate(scenario.Recipe, preflight);

        Assert.IsTrue(sandbox.StructuredPrerequisiteSummary.MissingCriticalRequirements.Count > 0);
        Assert.IsTrue(sandbox.FutureUnlockConditions.Any(c => c.Contains("structured prerequisite", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M8AdapterGateRequiresStructuredValidationAndEvidence()
    {
        var scenario = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("missing_download_artifact_evidence_blocks");
        var preflight = ReliableRecipePreflightComposer.Compose(scenario.Recipe, ReliableRecipeRunMode.DryRun);
        var profile = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(scenario.Recipe, preflight);
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(scenario.Recipe, preflight, structuredPrerequisiteProfile: profile);

        CollectionAssert.DoesNotContain(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.EvidenceExpectationsStructured);
    }

    [TestMethod]
    public void InferredCriticalBlocksFutureAdapterGate()
    {
        var scenario = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("submit_without_explicit_post_validation_blocks");
        var preflight = ReliableRecipePreflightComposer.Compose(scenario.Recipe, ReliableRecipeRunMode.DryRun);
        var profile = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(scenario.Recipe, preflight);
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(scenario.Recipe, preflight, structuredPrerequisiteProfile: profile);

        Assert.AreEqual(ReliableRecipeRequirementAdapterGateImpact.BlocksFutureAdapter, profile.CompletenessReport.AdapterReadinessImpact);
        CollectionAssert.DoesNotContain(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.ValidationExpectationsStructured);
    }

    [TestMethod]
    public void ExternalAuditStillRequiredBeforeRuntime()
    {
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(ReliableRecipeDryRunAdapterReadinessScenarioCatalog.Get("complete_fixture_stack_design_only_ready"));

        CollectionAssert.Contains(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.ExternalAuditRequiredBeforeRuntime);
        Assert.IsFalse(report.RuntimeActionExposed);
    }

    [TestMethod]
    public void LabStructuredPrerequisitesPanelHasNoRuntimeNotice()
    {
        var fixture = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass");

        Assert.IsTrue(fixture.ViewModel.StructuredPrerequisitesPanel.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(fixture.ViewModel.StructuredPrerequisitesPanel.ReadOnly);
    }

    [TestMethod]
    public void LabPanelExposesNoLiveActionLabels()
    {
        var panel = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel.StructuredPrerequisitesPanel;
        var labels = string.Join(" ", panel.ReadOnlyActionLabels);

        Assert.IsFalse(labels.Contains("Run now", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Execute", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Adapter enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(panel.RuntimeActionExposed);
        Assert.IsFalse(panel.CanExecute);
        Assert.IsFalse(panel.CanEnableAdapter);
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in ReliableRecipeStructuredPrerequisiteScenarioCatalog.All())
        {
            Assert.IsFalse(scenario.RuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveBrowser, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveDesktop, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesNetwork, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoOcrInternalsTouched()
    {
        var profile = Profile("ocr_only_sensitive_missing_perception_validation_blocks");

        Assert.IsTrue(profile.EvidenceRequirements.Any(r => r.Kind == StructuredEvidenceRequirementKind.OcrSupportingSignalRef || r.Kind == StructuredEvidenceRequirementKind.PerceptionSnapshot));
        Assert.IsFalse(profile.RuntimeEnabled);
    }

    [TestMethod]
    public void NoPerceptionRuntimeAdded()
    {
        var fixture = ReliableRecipeLabFixtureCatalog.Get("ocr_only_sensitive_submit_blocked");

        Assert.IsFalse(fixture.ViewModel.CanActivateOcrRuntime);
        Assert.IsFalse(fixture.ViewModel.CanCaptureScreenshot);
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var fixture = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration");

        Assert.IsFalse(fixture.Draft.LabViewModel.RecorderDraftReview.CanStartRecorder);
        Assert.IsFalse(fixture.Draft.LabViewModel.RecorderDraftReview.CanCaptureMouseOrKeyboard);
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        var sandbox = ComputerUseSandboxReadinessEvaluator.Evaluate(ComputerUseSandboxReadinessScenarioCatalog.Get("safe_invoice_fixture_ready"));

        Assert.IsFalse(sandbox.SandboxRuntimeEnabled);
        Assert.IsFalse(sandbox.VmOrContainerCreated);
    }

    [TestMethod]
    public void NoAdapterRuntimeAdded()
    {
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(ReliableRecipeDryRunAdapterReadinessScenarioCatalog.Get("complete_fixture_stack_design_only_ready"));

        Assert.IsFalse(report.ExecutableAdapterAdded);
        Assert.IsFalse(report.RuntimeCommandAdded);
        Assert.IsFalse(report.RuntimeActionExposed);
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var scenario in ReliableRecipeStructuredPrerequisiteScenarioCatalog.All())
        {
            var profile = ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(scenario);
            Assert.IsFalse(profile.RuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(profile.AdapterRuntimeAdded, scenario.ScenarioId);
            Assert.IsTrue(profile.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
        }
    }

    private static ReliableRecipeStructuredPrerequisiteProfile Profile(string scenarioId) =>
        ReliableRecipeStructuredPrerequisiteEvaluator.Evaluate(ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get(scenarioId));

    private static void AssertMissing(ReliableRecipeStructuredPrerequisiteProfile profile, string expected) =>
        Assert.IsTrue(
            profile.CompletenessReport.MissingCriticalRequirements.Any(m => m.Contains(expected, StringComparison.OrdinalIgnoreCase)) ||
            profile.CompletenessReport.BlockingFindings.Any(m => m.Contains(expected, StringComparison.OrdinalIgnoreCase)),
            $"Expected missing structured requirement containing {expected}.");
}
