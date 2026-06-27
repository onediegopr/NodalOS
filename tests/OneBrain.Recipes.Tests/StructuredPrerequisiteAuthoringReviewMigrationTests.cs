using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("StructuredPrerequisiteAuthoringReviewMigration")]
public sealed class StructuredPrerequisiteAuthoringReviewMigrationTests
{
    [TestMethod]
    public void AuthoringScenarioCatalogReturnsAllScenarios()
    {
        var scenarios = StructuredPrerequisiteAuthoringScenarioCatalog.All();

        Assert.AreEqual(12, scenarios.Count);
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "complete_explicit_no_changes_needed");
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "accepted_fixture_requirements_still_runtime_blocked");
    }

    [TestMethod]
    public void ScenarioIdsStable()
    {
        var first = StructuredPrerequisiteAuthoringScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();
        var second = StructuredPrerequisiteAuthoringScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ScenariosContainNoLiveUrlsOrSecrets()
    {
        foreach (var scenario in StructuredPrerequisiteAuthoringScenarioCatalog.All())
        {
            var report = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);
            var text = string.Join(" ", scenario.ScenarioId, scenario.Summary, report.NoRuntimeNotice, string.Join(" ", report.Proposals.Select(p => p.Reason)));
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
    public void CompleteExplicitProfileNoChangesNeeded()
    {
        var report = Report("complete_explicit_no_changes_needed");

        Assert.AreEqual(StructuredPrerequisiteAuthoringDecision.NoChangesNeeded, report.OverallDecision);
        Assert.AreEqual(0, report.Proposals.Count);
        Assert.AreEqual(0, report.MigrationSummary.StillBlockingCount);
    }

    [TestMethod]
    public void InferredDownloadEvidenceProducesProposal()
    {
        var report = Report("inferred_download_evidence_proposal");

        Assert.AreEqual(StructuredPrerequisiteAuthoringDecision.ProposalsNeedReview, report.OverallDecision);
        Assert.IsTrue(report.Proposals.Any(p => p.RequirementType == StructuredPrerequisiteRequirementType.Evidence));
        Assert.IsTrue(report.Proposals.Any(p => p.RecommendedReviewDecision == StructuredPrerequisiteReviewDecision.PendingReview));
    }

    [TestMethod]
    public void MissingPostSubmitValidationProducesBlockingProposal()
    {
        var report = Report("missing_post_submit_validation_proposal");

        Assert.AreEqual(StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, report.OverallDecision);
        Assert.IsTrue(report.Proposals.Any(p => p.RequirementType == StructuredPrerequisiteRequirementType.Validation && p.WouldBlockAdapterIfRejected));
    }

    [TestMethod]
    public void ExternalSideEffectApprovalEvidenceProducesProposal()
    {
        var report = Report("external_side_effect_approval_evidence_proposal");

        Assert.IsTrue(report.Proposals.Any(p => p.RequirementType == StructuredPrerequisiteRequirementType.Evidence && p.RequirementKind.Contains("Approval", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.ReviewChecklist.Any(i => i.IsBlocking));
    }

    [TestMethod]
    public void OcrOnlySensitivePerceptionValidationProposalGenerated()
    {
        var report = Report("ocr_only_sensitive_perception_validation_proposal");

        Assert.IsTrue(report.Proposals.Any(p => p.RequirementKind.Contains("Perception", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RecorderDraftHumanReviewEvidenceProposalGenerated()
    {
        var report = Report("recorder_draft_human_review_evidence_proposal");

        Assert.IsTrue(report.Proposals.Any(p => p.RequirementKind.Contains("Human", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(StructuredPrerequisiteAuthoringDecision.BlockedMissingCriticalRequirements, report.OverallDecision);
    }

    [TestMethod]
    public void CaptchaHandoffEvidenceProposalGenerated()
    {
        var report = Report("captcha_handoff_evidence_proposal");

        Assert.IsTrue(report.Proposals.Any(p => p.RequirementKind.Contains("HumanIntervention", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(report.Proposals.Any(p => p.Reason.Contains("bypass", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void SandboxReadinessAssertionProposalGenerated()
    {
        var report = Report("sandbox_readiness_assertion_proposal");

        Assert.IsTrue(report.Proposals.Any(p => p.RequirementKind.Contains("Sandbox", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(StructuredPrerequisiteAdapterGateImpact.BlocksUntilReview, report.AdapterGateImpact);
    }

    [TestMethod]
    public void EvalExpectedOutcomeAssertionProposalGenerated()
    {
        var report = Report("eval_expected_outcome_assertion_proposal");

        Assert.IsTrue(report.Proposals.Any(p => p.RequirementKind.Contains("EvalExpectedOutcome", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.ReviewChecklist.Any(i => i.Description.Contains("review", StringComparison.OrdinalIgnoreCase) || i.Description.Contains("Missing", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void MappedLegacyAcceptedWithWarning()
    {
        var report = Report("mapped_legacy_accepted_with_warning");

        Assert.AreEqual(StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements, report.OverallDecision);
        Assert.IsTrue(report.MigrationSummary.MappedLegacyCount > 0);
        Assert.IsTrue(report.MigrationSummary.AcceptedCount > 0);
    }

    [TestMethod]
    public void RejectedCriticalRequirementBlocks()
    {
        var report = Report("rejected_critical_requirement_blocks");

        Assert.AreEqual(StructuredPrerequisiteAuthoringDecision.BlockedRejectedCriticalRequirements, report.OverallDecision);
        Assert.AreEqual(StructuredPrerequisiteAdapterGateImpact.BlocksUntilExplicit, report.AdapterGateImpact);
        Assert.IsTrue(report.RejectedRequirements.Count > 0);
    }

    [TestMethod]
    public void AcceptedFixtureRequirementsStillRuntimeBlocked()
    {
        var report = Report("accepted_fixture_requirements_still_runtime_blocked");

        Assert.AreEqual(StructuredPrerequisiteAuthoringDecision.DesignOnlyReadyWithReviewedRequirements, report.OverallDecision);
        Assert.AreEqual(StructuredPrerequisiteAdapterGateImpact.RuntimeAlwaysBlockedInM10, report.AdapterGateImpact);
        Assert.IsFalse(report.RuntimeEnabled);
        Assert.IsFalse(report.AdapterRuntimeEnabled);
    }

    [TestMethod]
    public void PendingCriticalProposalsBlockM8Gate()
    {
        var scenario = StructuredPrerequisiteAuthoringScenarioCatalog.Get("missing_post_submit_validation_proposal");
        var authoring = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);
        var recipe = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("submit_without_explicit_post_validation_blocks").Recipe;
        var preflight = ReliableRecipePreflightComposer.Compose(recipe, ReliableRecipeRunMode.DryRun);
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(recipe, preflight, structuredPrerequisiteProfile: scenario.Profile, authoringReport: authoring);

        CollectionAssert.DoesNotContain(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.ValidationExpectationsStructured);
    }

    [TestMethod]
    public void AcceptedFixtureProposalsImproveDesignOnlyReadiness()
    {
        var scenario = StructuredPrerequisiteAuthoringScenarioCatalog.Get("accepted_fixture_requirements_still_runtime_blocked");
        var authoring = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);
        var recipe = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("submit_without_explicit_post_validation_blocks").Recipe;
        var preflight = ReliableRecipePreflightComposer.Compose(recipe, ReliableRecipeRunMode.DryRun);
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(recipe, preflight, structuredPrerequisiteProfile: scenario.Profile, authoringReport: authoring);

        CollectionAssert.Contains(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.ValidationExpectationsStructured);
        CollectionAssert.Contains(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.EvidenceExpectationsStructured);
    }

    [TestMethod]
    public void RejectedCriticalProposalsBlockAdapterReadiness()
    {
        var scenario = StructuredPrerequisiteAuthoringScenarioCatalog.Get("rejected_critical_requirement_blocks");
        var authoring = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);
        var recipe = ReliableRecipeStructuredPrerequisiteScenarioCatalog.Get("submit_without_explicit_post_validation_blocks").Recipe;
        var preflight = ReliableRecipePreflightComposer.Compose(recipe, ReliableRecipeRunMode.DryRun);
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(recipe, preflight, structuredPrerequisiteProfile: scenario.Profile, authoringReport: authoring);

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, report.OverallDecision);
        CollectionAssert.DoesNotContain(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.ValidationExpectationsStructured);
    }

    [TestMethod]
    public void RuntimeRemainsBlockedEvenWhenAllProposalsAccepted()
    {
        var scenario = StructuredPrerequisiteAuthoringScenarioCatalog.Get("accepted_fixture_requirements_still_runtime_blocked");
        var authoring = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);

        Assert.IsFalse(authoring.RuntimeEnabled);
        Assert.IsFalse(authoring.AdapterRuntimeEnabled);
        Assert.IsTrue(authoring.NoRuntimeNotice.Contains("cannot enable an adapter", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ExternalAuditRemainsRequired()
    {
        var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(ReliableRecipeDryRunAdapterReadinessScenarioCatalog.Get("complete_fixture_stack_design_only_ready"));

        CollectionAssert.Contains(report.SatisfiedGates.ToList(), ReliableRecipeDryRunAdapterGate.ExternalAuditRequiredBeforeRuntime);
        Assert.IsTrue(report.NoRuntimeNotice.Contains("No runtime enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LabAuthoringPanelIncludesProposals()
    {
        var authoring = Report("missing_post_submit_validation_proposal");
        var panel = StructuredPrerequisiteAuthoringReportMapper.ToLabPanel(authoring);

        Assert.IsTrue(panel.ProposalCount > 0);
        Assert.IsTrue(panel.TopProposals.Any(p => p.Contains("Proposed structured requirement", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void LabAuthoringPanelExposesNoLiveActionLabels()
    {
        var panel = ReliableRecipeLabFixtureCatalog.Get("submit_without_validation_quality_blocked").ViewModel.StructuredPrerequisiteAuthoringPanel;
        var labels = string.Join(" ", panel.ReadOnlyActionLabels.Concat(panel.TopProposals).Concat(panel.ReviewChecklist));

        Assert.IsFalse(panel.RuntimeActionExposed);
        Assert.IsFalse(panel.CanApplyLive);
        Assert.IsFalse(panel.CanRunMigration);
        Assert.IsFalse(panel.CanEnableAdapter);
        Assert.IsFalse(panel.CanExecute);
        Assert.IsFalse(labels.Contains("Apply live", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Run migration", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Enable adapter", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Execute", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Validated live", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void MigrationSummaryCountsSourcesAndProposals()
    {
        var report = Report("missing_post_submit_validation_proposal");

        Assert.IsTrue(report.MigrationSummary.ExplicitCount + report.MigrationSummary.FixtureExplicitCount > 0);
        Assert.IsTrue(report.MigrationSummary.MissingCount + report.MigrationSummary.InferredCount > 0);
        Assert.AreEqual(report.Proposals.Count, report.MigrationSummary.ProposedCount);
    }

    [TestMethod]
    public void AdapterGateImpactComputed()
    {
        Assert.AreEqual(StructuredPrerequisiteAdapterGateImpact.BlocksUntilReview, Report("missing_post_submit_validation_proposal").AdapterGateImpact);
        Assert.AreEqual(StructuredPrerequisiteAdapterGateImpact.BlocksUntilExplicit, Report("rejected_critical_requirement_blocks").AdapterGateImpact);
        Assert.AreEqual(StructuredPrerequisiteAdapterGateImpact.NoImpact, Report("complete_explicit_no_changes_needed").AdapterGateImpact);
    }

    [TestMethod]
    public void NoRuntimeNoticeAlwaysPresent()
    {
        foreach (var scenario in StructuredPrerequisiteAuthoringScenarioCatalog.All())
        {
            var report = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);
            Assert.IsTrue(report.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsTrue(report.Proposals.All(p => p.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase)), scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void AuthoringReportDeterministicAcrossRepeatedRuns()
    {
        var first = Report("external_side_effect_approval_evidence_proposal");
        var second = Report("external_side_effect_approval_evidence_proposal");

        CollectionAssert.AreEqual(first.Proposals.Select(p => p.ProposalId).ToArray(), second.Proposals.Select(p => p.ProposalId).ToArray());
        CollectionAssert.AreEqual(first.ReviewChecklist.Select(i => i.Code + i.ProposalId).ToArray(), second.ReviewChecklist.Select(i => i.Code + i.ProposalId).ToArray());
        Assert.AreEqual(first.AdapterGateImpact, second.AdapterGateImpact);
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in StructuredPrerequisiteAuthoringScenarioCatalog.All())
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
        var report = Report("ocr_only_sensitive_perception_validation_proposal");

        Assert.IsTrue(report.Proposals.Any(p => p.RequirementKind.Contains("Perception", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(report.Proposals.Any(p => p.RequirementKind.Contains("OcrLive", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NoPerceptionRuntimeAdded()
    {
        var report = Report("ocr_only_sensitive_perception_validation_proposal");

        Assert.IsFalse(report.Proposals.Any(p => p.Reason.Contains("live perception", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var report = Report("recorder_draft_human_review_evidence_proposal");

        Assert.IsFalse(report.Proposals.Any(p => p.Reason.Contains("record now", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(report.Proposals.Any(p => p.Reason.Contains("playback", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        var report = Report("sandbox_readiness_assertion_proposal");

        Assert.IsFalse(report.Proposals.Any(p => p.Reason.Contains("launch sandbox", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(report.Proposals.Any(p => p.RequirementKind.Contains("Sandbox", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NoAdapterRuntimeAdded()
    {
        foreach (var scenario in StructuredPrerequisiteAuthoringScenarioCatalog.All())
        {
            var report = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);
            Assert.IsFalse(report.AdapterRuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(report.CanEnableAdapter, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var scenario in StructuredPrerequisiteAuthoringScenarioCatalog.All())
        {
            var report = StructuredPrerequisiteAuthoringEvaluator.Evaluate(scenario);
            var copy = string.Join(" ", report.NoRuntimeNotice, string.Join(" ", report.Proposals.Select(p => p.Reason)), string.Join(" ", report.ReviewChecklist.Select(i => i.Description)));

            Assert.IsFalse(copy.Contains("Ready to execute", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Adapter enabled", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Run now", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Auto-migrate live", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Validated live", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Evidence captured live", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Production-ready", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
        }
    }

    private static StructuredPrerequisiteAuthoringReport Report(string scenarioId) =>
        StructuredPrerequisiteAuthoringEvaluator.Evaluate(StructuredPrerequisiteAuthoringScenarioCatalog.Get(scenarioId));
}
