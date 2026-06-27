using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("NoRuntimeReviewPackCloseoutAuditReadiness")]
public sealed class NoRuntimeReviewPackCloseoutAuditReadinessTests
{
    [TestMethod]
    public void CloseoutReportGeneratedForM1M11FixtureLine()
    {
        var report = HappyPath();

        Assert.AreEqual(ReliableRecipeNoRuntimeCloseoutDecision.ReadyForExternalAudit, report.OverallDecision);
        Assert.AreEqual("no-runtime-closeout.m1-m11", report.ReportId);
        Assert.IsTrue(report.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(report.RuntimeEnabled);
        Assert.IsFalse(report.AdapterRuntimeEnabled);
    }

    [TestMethod]
    public void CloseoutBlockSummariesIncludeM1M11()
    {
        var blocks = HappyPath().BlockSummaries;

        Assert.AreEqual(11, blocks.Count);
        CollectionAssert.AreEqual(
            Enumerable.Range(1, 11).Select(i => $"M{i}").ToArray(),
            blocks.Select(b => b.BlockId).ToArray());
    }

    [TestMethod]
    public void BlockIdsStable()
    {
        var first = ReliableRecipeNoRuntimeCloseoutReportGenerator.DefaultBlockSummaries().Select(b => b.BlockId).ToArray();
        var second = ReliableRecipeNoRuntimeCloseoutReportGenerator.DefaultBlockSummaries().Select(b => b.BlockId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void InvariantMatrixIncludesNoRuntimeInvariant()
    {
        var invariant = HappyPath().InvariantMatrix.Invariants.Single(i => i.Code == "no-runtime");

        Assert.AreEqual(ReliableRecipeCloseoutInvariantStatus.Passed, invariant.Status);
        Assert.IsTrue(invariant.Title.Contains("Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void InvariantMatrixIncludesProtectedScopeInvariant()
    {
        var invariant = HappyPath().InvariantMatrix.Invariants.Single(i => i.Code == "protected-scope");

        Assert.AreEqual(ReliableRecipeCloseoutInvariantStatus.Passed, invariant.Status);
        Assert.IsTrue(invariant.Title.Contains("Protected", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void InvariantMatrixIncludesExternalAuditBeforeRuntimeInvariant()
    {
        var invariant = HappyPath().InvariantMatrix.Invariants.Single(i => i.Code == "external-audit-before-runtime");

        Assert.AreEqual(ReliableRecipeCloseoutInvariantStatus.Passed, invariant.Status);
        Assert.IsTrue(invariant.Title.Contains("External audit", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ProtectedScopeProofHasNoViolationsInHappyPath()
    {
        var proof = HappyPath().ProtectedScopeProof;

        Assert.AreEqual("passed", proof.OverallStatus);
        Assert.AreEqual(0, proof.Violations.Count);
        Assert.IsTrue(proof.UntouchedScopes.Any(s => s.Contains("OCR", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(proof.UntouchedScopes.Any(s => s.Contains("Recorder", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void RuntimeLeakFixtureBlocksCloseout()
    {
        var report = ReliableRecipeNoRuntimeCloseoutReportGenerator.Generate(ReliableRecipeNoRuntimeCloseoutScenarioCatalog.Get("runtime_leak_blocks_closeout"));

        Assert.AreEqual(ReliableRecipeNoRuntimeCloseoutDecision.BlockedByRuntimeLeak, report.OverallDecision);
        Assert.AreEqual("failed", report.NoRuntimeProof.OverallStatus);
    }

    [TestMethod]
    public void ProtectedScopeViolationFixtureBlocksCloseout()
    {
        var report = ReliableRecipeNoRuntimeCloseoutReportGenerator.Generate(ReliableRecipeNoRuntimeCloseoutScenarioCatalog.Get("protected_scope_violation_blocks_closeout"));

        Assert.AreEqual(ReliableRecipeNoRuntimeCloseoutDecision.BlockedByProtectedScopeRisk, report.OverallDecision);
        Assert.AreEqual("failed", report.ProtectedScopeProof.OverallStatus);
        Assert.IsTrue(report.ProtectedScopeProof.Violations.Count > 0);
    }

    [TestMethod]
    public void NoRuntimeProofListsAbsentRuntimeCapabilities()
    {
        var absent = HappyPath().NoRuntimeProof.RuntimeCapabilitiesAbsent;

        CollectionAssert.Contains(absent.ToList(), "Browser live");
        CollectionAssert.Contains(absent.ToList(), "CDP live");
        CollectionAssert.Contains(absent.ToList(), "Desktop live");
        CollectionAssert.Contains(absent.ToList(), "Executable adapter");
    }

    [TestMethod]
    public void NoRuntimeProofListsBlockedCapabilities()
    {
        var blocked = HappyPath().NoRuntimeProof.BlockedCapabilities;

        CollectionAssert.Contains(blocked.ToList(), "OCR live");
        CollectionAssert.Contains(blocked.ToList(), "Recorder runtime");
        CollectionAssert.Contains(blocked.ToList(), "Sandbox runtime");
        CollectionAssert.Contains(blocked.ToList(), "Payment/publish/send/delete action");
    }

    [TestMethod]
    public void OperatorSignoffFixtureCatalogReturnsAllSignoffs()
    {
        var signoffs = ReliableRecipeOperatorSignoffFixtureCatalog.All();

        Assert.AreEqual(8, signoffs.Count);
        CollectionAssert.Contains(signoffs.Select(s => s.SignoffId).ToList(), "read_only_ui_signoff");
        CollectionAssert.Contains(signoffs.Select(s => s.SignoffId).ToList(), "no_runtime_regression_guard_signoff");
    }

    [TestMethod]
    public void OperatorSignoffCannotApproveRuntime()
    {
        foreach (var signoff in ReliableRecipeOperatorSignoffFixtureCatalog.All())
        {
            Assert.IsTrue(signoff.CannotApproveRuntime, signoff.SignoffId);
            Assert.IsFalse(signoff.RuntimeApprovalGranted, signoff.SignoffId);
            Assert.IsTrue(signoff.ReviewStatements.Any(s => s.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase)), signoff.SignoffId);
        }
    }

    [TestMethod]
    public void ExternalAuditHandoffPresent()
    {
        var handoff = HappyPath().ExternalAuditHandoff;

        Assert.IsTrue(handoff.Title.Contains("External audit", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(handoff.RuntimeProhibitedStatement.Contains("Runtime is prohibited", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ExternalAuditHandoffIncludesAuditQuestions()
    {
        var questions = HappyPath().ExternalAuditHandoff.AuditQuestions;

        Assert.IsTrue(questions.Count >= 5);
        Assert.IsTrue(questions.Any(q => q.Contains("no-runtime", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(questions.Any(q => q.Contains("protected", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void RecommendedNextPhaseIsReadOnlyUiOrExternalAuditNotRuntime()
    {
        var next = HappyPath().RecommendedNextPhase;

        Assert.IsTrue(next.AllowedScope.Contains("Read-only", StringComparison.OrdinalIgnoreCase) || next.AllowedScope.Contains("external audit", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(next.Title.Contains("runtime", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(next.ForbiddenScope.Contains("Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LabCloseoutPanelIncludesNoRuntimeNotice()
    {
        var panel = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel.CloseoutPanel;

        Assert.IsTrue(panel.NoRuntimeNotice.Contains("Runtime not enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(panel.RuntimeActionExposed);
    }

    [TestMethod]
    public void LabCloseoutPanelExposesNoLiveActionLabels()
    {
        var panel = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel.CloseoutPanel;

        Assert.IsFalse(panel.CanRun);
        Assert.IsFalse(panel.CanExecute);
        Assert.IsFalse(panel.CanEnableAdapter);
        Assert.IsFalse(panel.CanLaunchBrowser);
        Assert.IsFalse(panel.CanConnectCdp);
        Assert.IsFalse(panel.CanApproveRuntime);
        CollectionAssert.DoesNotContain(panel.ReadOnlyActionLabels.ToList(), "Run");
        CollectionAssert.DoesNotContain(panel.ReadOnlyActionLabels.ToList(), "Execute");
        CollectionAssert.DoesNotContain(panel.ReadOnlyActionLabels.ToList(), "Adapter enabled");
        CollectionAssert.DoesNotContain(panel.ReadOnlyActionLabels.ToList(), "Launch browser");
        CollectionAssert.DoesNotContain(panel.ReadOnlyActionLabels.ToList(), "Connect CDP");
        CollectionAssert.DoesNotContain(panel.ReadOnlyActionLabels.ToList(), "Runtime approved");
    }

    [TestMethod]
    public void CloseoutReportDeterministicAcrossRepeatedGeneration()
    {
        var first = HappyPath();
        var second = HappyPath();

        CollectionAssert.AreEqual(first.BlockSummaries.Select(b => b.BlockId).ToArray(), second.BlockSummaries.Select(b => b.BlockId).ToArray());
        CollectionAssert.AreEqual(first.InvariantMatrix.Invariants.Select(i => i.Code).ToArray(), second.InvariantMatrix.Invariants.Select(i => i.Code).ToArray());
        CollectionAssert.AreEqual(first.OperatorSignoffFixtures.Select(s => s.SignoffId).ToArray(), second.OperatorSignoffFixtures.Select(s => s.SignoffId).ToArray());
        Assert.AreEqual(first.ExternalAuditHandoff.RuntimeProhibitedStatement, second.ExternalAuditHandoff.RuntimeProhibitedStatement);
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var scenario in ReliableRecipeNoRuntimeCloseoutScenarioCatalog.All())
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
        var proof = HappyPath().ProtectedScopeProof;

        Assert.IsTrue(proof.UntouchedScopes.Any(s => s.Contains("OCR", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(0, proof.TouchedScopes.Count);
    }

    [TestMethod]
    public void NoPerceptionRuntimeAdded()
    {
        var proof = HappyPath().ProtectedScopeProof;

        Assert.IsTrue(proof.UntouchedScopes.Any(s => s.Contains("Perception", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(Copy(HappyPath()).Contains("live perception active", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var proof = HappyPath().ProtectedScopeProof;

        Assert.IsTrue(proof.UntouchedScopes.Any(s => s.Contains("Recorder", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(Copy(HappyPath()).Contains("record now", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        var proof = HappyPath().ProtectedScopeProof;

        Assert.IsTrue(proof.UntouchedScopes.Any(s => s.Contains("Sandbox", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(Copy(HappyPath()).Contains("launch sandbox", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoAdapterRuntimeAdded()
    {
        var report = HappyPath();

        Assert.IsFalse(report.AdapterRuntimeEnabled);
        Assert.IsFalse(report.RuntimeActionExposed);
        Assert.IsTrue(report.NoRuntimeProof.RuntimeCapabilitiesAbsent.Contains("Executable adapter"));
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        var copy = Copy(HappyPath());

        Assert.IsFalse(copy.Contains("Runtime-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Run now", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Adapter enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Automation ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Validated live", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Production-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Approved to run", StringComparison.OrdinalIgnoreCase));
    }

    private static ReliableRecipeNoRuntimeCloseoutReport HappyPath() =>
        ReliableRecipeNoRuntimeCloseoutReportGenerator.Generate(ReliableRecipeNoRuntimeCloseoutScenarioCatalog.Get("m1_m11_no_runtime_closeout"));

    private static string Copy(ReliableRecipeNoRuntimeCloseoutReport report) =>
        string.Join(" ", report.LineName, report.NoRuntimeNotice, report.OverallDecision, report.InvariantMatrix.OverallStatus, report.ProtectedScopeProof.OverallStatus, report.NoRuntimeProof.OverallStatus, report.ExternalAuditHandoff.Title, report.ExternalAuditHandoff.Scope, report.ExternalAuditHandoff.RuntimeProhibitedStatement, report.RecommendedNextPhase.Title, report.RecommendedNextPhase.AllowedScope, report.RecommendedNextPhase.ForbiddenScope, string.Join(" ", report.BlockSummaries.Select(b => b.Purpose)), string.Join(" ", report.OperatorSignoffFixtures.SelectMany(s => s.ReviewStatements)));
}
