using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaProofDryRunBindingM82Tests
{
    [TestMethod]
    public void ProofDryRunReadOnlyGeneratesEvidencePack()
    {
        var result = Run(NexaSyntheticExternalScenarioKind.LandingReadOnly);

        Assert.AreEqual(NexaProofDryRunStatus.DryRunEvidenceGenerated, result.Status);
        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PreparedButNotExecuted, result.EvidencePack.Status);
        Assert.IsTrue(result.EvidencePack.Redacted);
    }

    [TestMethod]
    public void ProofDryRunBlockedScenariosGenerateExplanations()
    {
        var result = Run(NexaSyntheticExternalScenarioKind.LoginBlocked);

        Assert.AreEqual(NexaProofDryRunStatus.DryRunBlockedByPolicy, result.Status);
        Assert.IsNotNull(result.Scenario.ExpectedBlockerExplanation);
        Assert.AreEqual(NexaOperatorBlockerScenario.RealCredentialsBlocked, result.Scenario.ExpectedBlockerExplanation.Value);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.HarnessDecision.Explanation.Cause));
    }

    [TestMethod]
    public void ProofDryRunDoesNotExecuteExternalNetwork()
    {
        var result = Run(NexaSyntheticExternalScenarioKind.ProductListReadOnly);

        Assert.IsFalse(result.ExecutedNetwork);
        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PreparedButNotExecuted, result.EvidencePack.Status);
    }

    [TestMethod]
    public void ProofDryRunDoesNotCloseM51M65()
    {
        var result = Run(NexaSyntheticExternalScenarioKind.DocumentReadOnly);

        Assert.IsFalse(result.ClosesM51M65);
        Assert.IsFalse(result.EvidencePack.CandidateForM51M65Closure);
        Assert.IsTrue(result.EvidencePack.FinalGoNoGo.Contains("blocked or deferred", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PrivatePreviewReadinessSeparatesFixtureReadyFromExternalProofPassed()
    {
        var result = Run(NexaSyntheticExternalScenarioKind.TableReportReadOnly);
        var dashboard = new NexaPrivatePreviewReadinessDashboardService().Build(
            new NexaSkippedTestsAuditReporter().CreateReport(),
            new NexaPrivatePreviewGoNoGoService().Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria(), []),
            GuardAllowed(),
            result.EvidencePack);

        Assert.IsTrue(dashboard.Decision.LocalPreviewAllowed);
        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
        Assert.IsTrue(dashboard.M65Blocked);
    }

    [TestMethod]
    public void ProofDryRunKeepsSkippedAuditConsistent()
    {
        var report = new NexaSkippedTestsAuditReporter().CreateReport();

        Assert.AreEqual(29, report.Items.Count);
        Assert.IsFalse(report.BlocksLocalPrivatePreview);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateReflectsDryRunReadinessWithoutOpeningLive()
    {
        var state = BrowserVaultMinimalM23Tests.SafeState() with
        {
            TestOwnedExternalTargetFixtureDefined = true,
            SyntheticExternalScenarioCatalogDefined = true,
            ProofDryRunBindingDefined = true,
            ProofDryRunExecutesNetwork = false,
            ProofDryRunClosesM51M65 = false,
            ProofDryRunLeaksSecrets = false
        };

        Assert.IsTrue(state.ExternalProofDryRunAllowed);
    }

    private static NexaProofDryRunResult Run(NexaSyntheticExternalScenarioKind kind)
    {
        var scenario = new NexaSyntheticExternalScenarioCatalogService().CreateDefault().Scenarios.Single(s => s.Kind == kind);
        return new NexaProofDryRunBinding().Run(NexaTestOwnedExternalTargetFixtureFactory.Create(), scenario);
    }

    private static NexaCanonicalWorkspaceGuardResult GuardAllowed() =>
        new(
            NexaCanonicalWorkspaceGuardDecisionKind.Allowed,
            "canonical",
            "canonical",
            "head",
            "head",
            "origin/chrome-lab-001-extension-local-ai-bridge",
            IsDirty: false,
            IsLegacyPath: false,
            MatchesRemoteHead: true,
            DetachedHeadAccepted: true,
            BlockingReasons: [],
            OperatorMessage: "allowed",
            ModifiedWorkspace: false);
}
