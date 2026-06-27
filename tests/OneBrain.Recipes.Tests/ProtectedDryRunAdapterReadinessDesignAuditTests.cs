using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ProtectedDryRunAdapterReadinessDesignAudit")]
public sealed class ProtectedDryRunAdapterReadinessDesignAuditTests
{
    [TestMethod]
    public void ScenarioCatalogReturnsAllScenarios()
    {
        var scenarios = ReliableRecipeDryRunAdapterReadinessScenarioCatalog.All();

        Assert.AreEqual(12, scenarios.Count);
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "complete_fixture_stack_design_only_ready");
        CollectionAssert.Contains(scenarios.Select(s => s.ScenarioId).ToList(), "ocr_live_requested_blocked");
    }

    [TestMethod]
    public void ScenarioIdsStable()
    {
        var first = ReliableRecipeDryRunAdapterReadinessScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();
        var second = ReliableRecipeDryRunAdapterReadinessScenarioCatalog.All().Select(s => s.ScenarioId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void ScenariosContainNoLiveUrlsOrSecrets()
    {
        foreach (var scenario in ReliableRecipeDryRunAdapterReadinessScenarioCatalog.All())
        {
            var text = string.Join(" ", scenario.ScenarioId, scenario.SubjectId, scenario.Summary);
            Assert.IsFalse(text.Contains("http://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("https://", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("password" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("token" + "=", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(text.Contains("authorization:", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
            Assert.IsFalse(scenario.RuntimeEnabled, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveBrowser, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesLiveDesktop, scenario.ScenarioId);
            Assert.IsFalse(scenario.UsesNetwork, scenario.ScenarioId);
            Assert.IsFalse(scenario.LaunchesAdapter, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void CompleteFixtureStackDesignOnlyReadyWithWarnings()
    {
        var report = Report("complete_fixture_stack_design_only_ready");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.DesignOnlyReadyWithWarnings, report.OverallDecision);
        Assert.IsTrue(report.ReadinessScore >= 0.7);
        AssertGate(report, ReliableRecipeDryRunAdapterGate.ExternalAuditRequiredBeforeRuntime);
    }

    [TestMethod]
    public void NoScenarioEnablesRuntime()
    {
        foreach (var scenario in ReliableRecipeDryRunAdapterReadinessScenarioCatalog.All())
        {
            var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(scenario);
            Assert.IsFalse(report.ExecutableAdapterAdded, scenario.ScenarioId);
            Assert.IsFalse(report.RuntimeCommandAdded, scenario.ScenarioId);
            Assert.IsFalse(report.RuntimeActionExposed, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void LiveBrowserCdpCloakCapabilitiesBlocked()
    {
        var report = Report("complete_fixture_stack_design_only_ready");

        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.BrowserLive);
        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.CdpLive);
        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.CloakLive);
        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.PlaywrightLive);
    }

    [TestMethod]
    public void DesktopLiveBlocked()
    {
        AssertBlocked(Report("complete_fixture_stack_design_only_ready"), ReliableRecipeDryRunAdapterBlockedCapability.DesktopLive);
    }

    [TestMethod]
    public void RecorderLiveBlocked()
    {
        AssertBlocked(Report("recorder_draft_unreviewed_blocked"), ReliableRecipeDryRunAdapterBlockedCapability.RecorderLive);
    }

    [TestMethod]
    public void OcrLiveBlocked()
    {
        AssertBlocked(Report("ocr_live_requested_blocked"), ReliableRecipeDryRunAdapterBlockedCapability.OcrLive);
    }

    [TestMethod]
    public void ScreenshotCaptureBlocked()
    {
        AssertBlocked(Report("ocr_live_requested_blocked"), ReliableRecipeDryRunAdapterBlockedCapability.ScreenshotCapture);
    }

    [TestMethod]
    public void SandboxRuntimeBlocked()
    {
        AssertBlocked(Report("complete_fixture_stack_design_only_ready"), ReliableRecipeDryRunAdapterBlockedCapability.SandboxRuntime);
    }

    [TestMethod]
    public void NetworkShellProviderBlocked()
    {
        var report = Report("complete_fixture_stack_design_only_ready");

        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.NetworkAccess);
        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.ShellExecution);
        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.ProviderCall);
    }

    [TestMethod]
    public void CredentialAutomationBlocked()
    {
        AssertBlocked(Report("captcha_two_factor_bypass_blocked"), ReliableRecipeDryRunAdapterBlockedCapability.CredentialAutomation);
    }

    [TestMethod]
    public void CaptchaTwoFactorBypassBlocked()
    {
        var report = Report("captcha_two_factor_bypass_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedByPolicy, report.OverallDecision);
        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.CaptchaOrTwoFactorBypass);
    }

    [TestMethod]
    public void MissingStructuredEvidenceBlocks()
    {
        var report = Report("missing_structured_evidence_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, report.OverallDecision);
        AssertMissing(report, ReliableRecipeDryRunAdapterGate.EvidenceExpectationsStructured);
    }

    [TestMethod]
    public void MissingStructuredValidationBlocks()
    {
        var report = Report("missing_structured_validation_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, report.OverallDecision);
        AssertMissing(report, ReliableRecipeDryRunAdapterGate.ValidationExpectationsStructured);
    }

    [TestMethod]
    public void MissingEvalBlocks()
    {
        var report = Report("eval_harness_missing_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, report.OverallDecision);
        AssertMissing(report, ReliableRecipeDryRunAdapterGate.EvalHarnessPasses);
    }

    [TestMethod]
    public void MissingSandboxReadinessBlocks()
    {
        var report = Report("sandbox_readiness_missing_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, report.OverallDecision);
        AssertMissing(report, ReliableRecipeDryRunAdapterGate.SandboxReadinessFixtureOnly);
    }

    [TestMethod]
    public void MissingPerceptionSignalsBlocks()
    {
        var report = Report("perception_signals_weak_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, report.OverallDecision);
        AssertMissing(report, ReliableRecipeDryRunAdapterGate.PerceptionSignalsSufficient);
    }

    [TestMethod]
    public void UnreviewedRecorderDraftBlocks()
    {
        var report = Report("recorder_draft_unreviewed_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedMissingGates, report.OverallDecision);
        AssertMissing(report, ReliableRecipeDryRunAdapterGate.RecorderDraftReviewed);
    }

    [TestMethod]
    public void HighRiskExternalSubmitBlocks()
    {
        var report = Report("high_risk_external_submit_blocked");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedByPolicy, report.OverallDecision);
        Assert.IsTrue(report.MissingPrerequisites.Any(m => m.Contains("ApprovalPolicyPresent", StringComparison.Ordinal) || m.Contains("ReliableRecipePreflightPasses", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ProtectedScopeAuditRequiredBeforeRuntime()
    {
        var report = Report("protected_scope_audit_required");

        Assert.AreEqual(ReliableRecipeDryRunAdapterReadinessDecision.BlockedProtectedScope, report.OverallDecision);
        AssertMissing(report, ReliableRecipeDryRunAdapterGate.ProtectedScopeAuditPassed);
        Assert.IsTrue(report.ProtectedScopes.All(s => s.AuditRequiredBeforeChange));
    }

    [TestMethod]
    public void ExternalAuditRequiredBeforeRuntime()
    {
        var report = Report("complete_fixture_stack_design_only_ready");

        AssertGate(report, ReliableRecipeDryRunAdapterGate.ExternalAuditRequiredBeforeRuntime);
        Assert.IsTrue(report.FutureAdapterBoundary.RequiredApprovals.Any(a => a.Contains("External audit", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void LabReadinessPanelIncludesNoRuntimeNotice()
    {
        var lab = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;

        Assert.IsTrue(lab.DryRunAdapterReadinessPanel.NoRuntimeNotice.Contains("No runtime enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(lab.DryRunAdapterReadinessPanel.ReadOnly);
        Assert.IsTrue(lab.DryRunAdapterReadinessPanel.DesignOnly);
    }

    [TestMethod]
    public void LabReadinessPanelExposesNoLiveActionLabels()
    {
        var lab = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;
        var labels = string.Join(" ", lab.DryRunAdapterReadinessPanel.ReadOnlyActionLabels);

        Assert.IsFalse(labels.Contains("Run dry-run", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Start adapter", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Execute", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Connect browser", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Launch Cloak", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(labels.Contains("Use CDP", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(lab.DryRunAdapterReadinessPanel.CanStartAdapter);
        Assert.IsFalse(lab.DryRunAdapterReadinessPanel.CanExecuteDryRun);
        Assert.IsFalse(lab.DryRunAdapterReadinessPanel.CanConnectBrowser);
    }

    [TestMethod]
    public void ReportDeterministicAcrossRepeatedRuns()
    {
        var first = Report("protected_scope_audit_required");
        var second = Report("protected_scope_audit_required");

        Assert.AreEqual(first.OverallDecision, second.OverallDecision);
        CollectionAssert.AreEqual(first.MissingPrerequisites.ToArray(), second.MissingPrerequisites.ToArray());
        CollectionAssert.AreEqual(first.BlockedCapabilities.ToArray(), second.BlockedCapabilities.ToArray());
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in ReliableRecipeDryRunAdapterReadinessScenarioCatalog.All())
        {
            var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(scenario);
            Assert.IsFalse(report.BrowserLiveAdded, scenario.ScenarioId);
            Assert.IsFalse(report.DesktopLiveAdded, scenario.ScenarioId);
            Assert.IsFalse(report.NetworkCallAdded, scenario.ScenarioId);
            Assert.IsFalse(report.ProviderCallAdded, scenario.ScenarioId);
        }
    }

    [TestMethod]
    public void NoOcrInternalsTouched()
    {
        var report = Report("ocr_live_requested_blocked");

        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.OcrLive);
        Assert.IsFalse(report.RuntimeActionExposed);
    }

    [TestMethod]
    public void NoPerceptionRuntimeAdded()
    {
        var report = Report("perception_signals_weak_blocked");

        Assert.IsFalse(report.RuntimeCommandAdded);
        Assert.IsFalse(report.RuntimeActionExposed);
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var report = Report("recorder_draft_unreviewed_blocked");

        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.RecorderLive);
        Assert.IsFalse(report.ExecutableAdapterAdded);
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        var report = Report("complete_fixture_stack_design_only_ready");

        AssertBlocked(report, ReliableRecipeDryRunAdapterBlockedCapability.SandboxRuntime);
        Assert.IsFalse(report.RuntimeCommandAdded);
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var scenario in ReliableRecipeDryRunAdapterReadinessScenarioCatalog.All())
        {
            var report = ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(scenario);
            Assert.IsTrue(report.ReadOnly, scenario.ScenarioId);
            Assert.IsTrue(report.DesignOnly, scenario.ScenarioId);
            Assert.IsFalse(report.ExecutableAdapterAdded, scenario.ScenarioId);
            Assert.IsFalse(report.RuntimeCommandAdded, scenario.ScenarioId);
            Assert.IsFalse(report.RuntimeActionExposed, scenario.ScenarioId);
            Assert.IsTrue(report.NoRuntimeNotice.Contains("No runtime enabled", StringComparison.OrdinalIgnoreCase), scenario.ScenarioId);
        }
    }

    private static ReliableRecipeDryRunAdapterReadinessReport Report(string scenarioId) =>
        ReliableRecipeDryRunAdapterReadinessEvaluator.Evaluate(ReliableRecipeDryRunAdapterReadinessScenarioCatalog.Get(scenarioId));

    private static void AssertBlocked(ReliableRecipeDryRunAdapterReadinessReport report, ReliableRecipeDryRunAdapterBlockedCapability capability) =>
        CollectionAssert.Contains(report.BlockedCapabilities.ToList(), capability);

    private static void AssertGate(ReliableRecipeDryRunAdapterReadinessReport report, ReliableRecipeDryRunAdapterGate gate) =>
        CollectionAssert.Contains(report.SatisfiedGates.ToList(), gate);

    private static void AssertMissing(ReliableRecipeDryRunAdapterReadinessReport report, ReliableRecipeDryRunAdapterGate gate) =>
        Assert.IsTrue(report.MissingPrerequisites.Any(m => m.Contains(gate.ToString(), StringComparison.Ordinal)), gate.ToString());
}
