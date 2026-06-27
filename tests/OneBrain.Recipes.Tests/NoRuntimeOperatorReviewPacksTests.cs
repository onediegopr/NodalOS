using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("NoRuntimeOperatorReviewPacks")]
public sealed class NoRuntimeOperatorReviewPacksTests
{
    [TestMethod]
    public void ReviewPackScenarioCatalogReturnsAllScenarios()
    {
        var scenarios = ReliableRecipeOperatorReviewPackScenarioCatalog.All();

        Assert.AreEqual(12, scenarios.Count);
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "complete_explicit_review_pack");
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "external_audit_required_review_pack");
    }

    [TestMethod]
    public void ScenarioIdsStable()
    {
        var first = ReliableRecipeOperatorReviewPackScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();
        var second = ReliableRecipeOperatorReviewPackScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ScenariosContainNoLiveUrlsOrSecrets()
    {
        foreach (var scenario in ReliableRecipeOperatorReviewPackScenarioCatalog.All())
        {
            var pack = ReliableRecipeOperatorReviewPackGenerator.Generate(scenario);
            var text = string.Join(" ", scenario.ScenarioId, scenario.Summary, pack.ExecutiveSummary, pack.NoRuntimeNotice);
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
    public void CompleteExplicitReviewPackGenerated()
    {
        var pack = Pack("complete_explicit_review_pack");

        Assert.AreEqual(ReliableRecipeOperatorReviewDecision.ReadyForHumanReview, pack.OverallDecision);
        Assert.IsTrue(pack.ExecutiveSummary.Contains("audit-ready", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void PendingInferredRequirementYieldsNeedsOperatorDecision()
    {
        var pack = Pack("pending_inferred_requirement_review_pack");

        Assert.AreEqual(ReliableRecipeOperatorReviewDecision.NeedsOperatorDecision, pack.OverallDecision);
        Assert.IsTrue(pack.ReviewRows.Any(r => r.Status == ReliableRecipeOperatorReviewStatus.PendingReview));
    }

    [TestMethod]
    public void MissingCriticalRequirementBlocks()
    {
        var pack = Pack("missing_critical_requirement_review_pack");

        Assert.AreEqual(ReliableRecipeOperatorReviewDecision.BlockedByCriticalGaps, pack.OverallDecision);
        Assert.IsTrue(pack.ReviewRows.Any(r => r.Status == ReliableRecipeOperatorReviewStatus.MissingCritical));
    }

    [TestMethod]
    public void RejectedCriticalRequirementBlocks()
    {
        var pack = Pack("rejected_critical_requirement_review_pack");

        Assert.AreEqual(ReliableRecipeOperatorReviewDecision.BlockedByRejectedCriticalRequirement, pack.OverallDecision);
        Assert.IsTrue(pack.ReviewRows.Any(r => r.Status == ReliableRecipeOperatorReviewStatus.RejectedUnsafe));
    }

    [TestMethod]
    public void AcceptedFixtureOnlyStillRuntimeBlocked()
    {
        var pack = Pack("accepted_fixture_only_review_pack");

        Assert.AreEqual(ReliableRecipeOperatorReviewDecision.DesignOnlyAcceptedForFixture, pack.OverallDecision);
        Assert.IsFalse(pack.RuntimeEnabled);
        Assert.IsFalse(pack.AdapterRuntimeEnabled);
        Assert.IsTrue(pack.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void MappedLegacyWarningSurfaced()
    {
        var pack = Pack("mapped_legacy_warning_review_pack");

        Assert.IsTrue(pack.ReviewRows.Any(r => r.Status == ReliableRecipeOperatorReviewStatus.MappedLegacyWarning));
        Assert.IsTrue(pack.ReviewRows.Any(r => r.Message.Contains("Mapped legacy", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ExternalSideEffectApprovalLanguageGenerated()
    {
        var pack = Pack("external_side_effect_approval_review_pack");

        Assert.IsTrue(pack.ApprovalLanguage.RiskStatement.Contains("Critical gaps", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(pack.ReviewRows.Any(r => r.Category == ReliableRecipeOperatorReviewCategory.HumanApproval));
    }

    [TestMethod]
    public void OcrSupportingSignalReviewCopyDoesNotImplyAuthority()
    {
        var pack = Pack("ocr_supporting_signal_review_pack");
        var copy = Copy(pack);

        Assert.IsTrue(copy.Contains("OCR", StringComparison.OrdinalIgnoreCase) || copy.Contains("Perception", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("OCR-authorized", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("action authority", StringComparison.OrdinalIgnoreCase) && copy.Contains("granted", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CaptchaHandoffReviewSummaryGenerated()
    {
        var pack = Pack("captcha_handoff_review_pack");

        Assert.IsTrue(pack.HandoffSummary.Title.Contains("handoff", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(pack.HandoffSummary.PendingHumanDecisions.Count > 0 || pack.HandoffSummary.CriticalBlockers.Count > 0);
    }

    [TestMethod]
    public void SandboxReadinessReviewSummaryGenerated()
    {
        var pack = Pack("sandbox_readiness_review_pack");

        Assert.IsTrue(pack.ReviewRows.Any(r => r.Category == ReliableRecipeOperatorReviewCategory.SandboxReadiness));
        Assert.IsTrue(pack.HandoffSummary.CriticalBlockers.Any(b => b.Contains("Sandbox", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void AdapterGateBlockedReviewSummaryGenerated()
    {
        var pack = Pack("adapter_gate_blocked_review_pack");

        Assert.IsTrue(pack.AdapterGateSummary.Contains("blocked", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(pack.ReviewRows.Any(r => r.Category == ReliableRecipeOperatorReviewCategory.AdapterGate));
    }

    [TestMethod]
    public void ExternalAuditRequiredReviewSummaryGenerated()
    {
        var pack = Pack("external_audit_required_review_pack");

        Assert.IsTrue(pack.HandoffSummary.ExternalAuditRequired);
        Assert.IsTrue(pack.AdapterGateSummary.Contains("External audit required before runtime", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ApprovalLanguageIncludesWhatWillChange()
    {
        var pack = Pack("pending_inferred_requirement_review_pack");

        Assert.IsTrue(pack.ApprovalLanguage.WhatWillChange.Contains("fixture", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ApprovalLanguageIncludesWhatWillNotChange()
    {
        var pack = Pack("accepted_fixture_only_review_pack");

        Assert.IsTrue(pack.ApprovalLanguage.WhatWillNotChange.Contains("will not enable runtime", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ApprovalLanguageForbiddenLabelsIncludeRuntimeLabels()
    {
        var forbidden = Pack("complete_explicit_review_pack").ApprovalLanguage.ForbiddenDecisionLabels;

        CollectionAssert.Contains(forbidden.ToList(), "Run now");
        CollectionAssert.Contains(forbidden.ToList(), "Execute");
        CollectionAssert.Contains(forbidden.ToList(), "Enable adapter");
        CollectionAssert.Contains(forbidden.ToList(), "Launch browser");
        CollectionAssert.Contains(forbidden.ToList(), "Connect CDP");
        CollectionAssert.Contains(forbidden.ToList(), "Replay");
        CollectionAssert.Contains(forbidden.ToList(), "Record live");
    }

    [TestMethod]
    public void RecommendedActionsContainNoRuntimeActions()
    {
        foreach (var scenario in ReliableRecipeOperatorReviewPackScenarioCatalog.All())
        {
            var pack = ReliableRecipeOperatorReviewPackGenerator.Generate(scenario);
            Assert.IsTrue(pack.RecommendedOperatorActions.All(a => !a.IsRuntimeAction), scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void ExportCopyActionsAreNotRuntimeActions()
    {
        var actions = Pack("pending_inferred_requirement_review_pack").RecommendedOperatorActions;

        Assert.IsFalse(actions.Single(a => a.ActionKind == ReliableRecipeOperatorActionKind.CopySummary).IsRuntimeAction);
        Assert.IsFalse(actions.Single(a => a.ActionKind == ReliableRecipeOperatorActionKind.ExportReviewPack).IsRuntimeAction);
    }

    [TestMethod]
    public void LabReviewPackPanelIncludesNoRuntimeNotice()
    {
        var panel = ReliableRecipeLabFixtureCatalog.Get("submit_without_validation_quality_blocked").ViewModel.OperatorReviewPackPanel;

        Assert.IsTrue(panel.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LabReviewPackPanelExposesNoLiveActionLabels()
    {
        var panel = ReliableRecipeLabFixtureCatalog.Get("submit_without_validation_quality_blocked").ViewModel.OperatorReviewPackPanel;
        var actionLabels = panel.ReadOnlyActionLabels.Concat(panel.RecommendedActions).ToArray();

        Assert.IsFalse(panel.RuntimeActionExposed);
        Assert.IsFalse(panel.CanRun);
        Assert.IsFalse(panel.CanExecute);
        Assert.IsFalse(panel.CanEnableAdapter);
        Assert.IsFalse(panel.CanLaunchBrowser);
        Assert.IsFalse(panel.CanConnectCdp);
        Assert.IsFalse(panel.CanReplay);
        Assert.IsFalse(panel.CanRecordLive);
        CollectionAssert.DoesNotContain(actionLabels.ToList(), "Run");
        CollectionAssert.DoesNotContain(actionLabels.ToList(), "Execute");
        CollectionAssert.DoesNotContain(actionLabels.ToList(), "Enable adapter");
        CollectionAssert.DoesNotContain(actionLabels.ToList(), "Launch browser");
        CollectionAssert.DoesNotContain(actionLabels.ToList(), "Connect CDP");
        CollectionAssert.DoesNotContain(actionLabels.ToList(), "Replay");
        CollectionAssert.DoesNotContain(actionLabels.ToList(), "Record live");
    }

    [TestMethod]
    public void AuditSummaryListsProtectedScopesUntouched()
    {
        var protectedScopes = Pack("complete_explicit_review_pack").AuditSummary.ProtectedScopesUntouched;

        Assert.IsTrue(protectedScopes.Any(s => s.Contains("OCR", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(protectedScopes.Any(s => s.Contains("Recorder", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(protectedScopes.Any(s => s.Contains("Sandbox", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void AuditSummaryListsRuntimeCapabilitiesAbsent()
    {
        var absent = Pack("complete_explicit_review_pack").AuditSummary.RuntimeCapabilitiesAbsent;

        CollectionAssert.Contains(absent.ToList(), "Browser live");
        CollectionAssert.Contains(absent.ToList(), "CDP live");
        CollectionAssert.Contains(absent.ToList(), "Desktop live");
        CollectionAssert.Contains(absent.ToList(), "Provider/LLM call");
    }

    [TestMethod]
    public void RuntimeRemainsBlockedEvenWhenAcceptedForFixture()
    {
        var pack = Pack("accepted_fixture_only_review_pack");

        Assert.AreEqual(ReliableRecipeOperatorReviewDecision.DesignOnlyAcceptedForFixture, pack.OverallDecision);
        Assert.IsFalse(pack.RuntimeEnabled);
        Assert.IsFalse(pack.AdapterRuntimeEnabled);
        Assert.IsTrue(pack.AuditSummary.RequiredBeforeRuntime.Any(r => r.Contains("External audit", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ReviewPackDeterministicAcrossRepeatedGeneration()
    {
        var first = Pack("external_side_effect_approval_review_pack");
        var second = Pack("external_side_effect_approval_review_pack");

        CollectionAssert.AreEqual(first.ReviewRows.Select(r => r.RowId).ToArray(), second.ReviewRows.Select(r => r.RowId).ToArray());
        CollectionAssert.AreEqual(first.RecommendedOperatorActions.Select(a => a.Label).ToArray(), second.RecommendedOperatorActions.Select(a => a.Label).ToArray());
        Assert.AreEqual(first.AdapterGateSummary, second.AdapterGateSummary);
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in ReliableRecipeOperatorReviewPackScenarioCatalog.All())
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
        var pack = Pack("ocr_supporting_signal_review_pack");

        Assert.IsTrue(Copy(pack).Contains("OCR", StringComparison.OrdinalIgnoreCase) || Copy(pack).Contains("Perception", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(Copy(pack).Contains("OCR live activation", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoPerceptionRuntimeAdded()
    {
        var pack = Pack("ocr_supporting_signal_review_pack");

        Assert.IsFalse(Copy(pack).Contains("live perception", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(pack.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var pack = Pack("captcha_handoff_review_pack");

        Assert.IsFalse(Copy(pack).Contains("record now", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(Copy(pack).Contains("recorder runtime added", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        var pack = Pack("sandbox_readiness_review_pack");

        Assert.IsFalse(Copy(pack).Contains("launch sandbox", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(Copy(pack).Contains("sandbox", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoAdapterRuntimeAdded()
    {
        foreach (var scenario in ReliableRecipeOperatorReviewPackScenarioCatalog.All())
        {
            var pack = ReliableRecipeOperatorReviewPackGenerator.Generate(scenario);
            Assert.IsFalse(pack.AdapterRuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(pack.RuntimeActionExposed, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var scenario in ReliableRecipeOperatorReviewPackScenarioCatalog.All())
        {
            var pack = ReliableRecipeOperatorReviewPackGenerator.Generate(scenario);
            var copy = Copy(pack);

            Assert.IsFalse(copy.Contains("Automation ready", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Validated live", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Evidence captured live", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Production-ready", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(copy.Contains("Approved to run", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
        }
    }

    private static ReliableRecipeOperatorReviewPack Pack(string scenarioId) =>
        ReliableRecipeOperatorReviewPackGenerator.Generate(ReliableRecipeOperatorReviewPackScenarioCatalog.Get(scenarioId));

    private static string Copy(ReliableRecipeOperatorReviewPack pack) =>
        string.Join(" ", pack.ExecutiveSummary, pack.NoRuntimeNotice, pack.AdapterGateSummary, pack.ApprovalLanguage.ShortSummary, pack.ApprovalLanguage.WhatWillChange, pack.ApprovalLanguage.WhatWillNotChange, pack.ApprovalLanguage.RiskStatement, string.Join(" ", pack.ReviewRows.Select(r => r.Message)), string.Join(" ", pack.RecommendedOperatorActions.Select(a => a.Label)));
}
