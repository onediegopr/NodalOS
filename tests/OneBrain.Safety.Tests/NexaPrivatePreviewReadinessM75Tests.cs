using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivatePreviewReadinessM75Tests
{
    [TestMethod]
    public void PrivatePreviewReadinessAllowsLocalPreviewWhenOnlyExternalBlockersExist()
    {
        var dashboard = Dashboard();

        Assert.IsTrue(dashboard.Decision.LocalPreviewAllowed);
        Assert.AreEqual("GO local private preview only", dashboard.Decision.GoNoGoLocal);
        Assert.IsTrue(dashboard.M51Deferred);
        Assert.IsTrue(dashboard.M65Blocked);
    }

    [TestMethod]
    public void PrivatePreviewReadinessDeniesExternalLiveWhenExternalTargetBlocked()
    {
        var dashboard = Dashboard();

        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
        Assert.IsTrue(dashboard.Decision.GoNoGoExternalLive.Contains("NO-GO external/live", StringComparison.Ordinal));
        Assert.IsTrue(dashboard.ActiveBlockers.Any(blocker => blocker.Contains("M51/M65 external target proof blocked", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PrivatePreviewReadinessDeniesPublicSaasBillingEmailAndSensitivePilot()
    {
        var decision = Dashboard().Decision;

        Assert.IsFalse(decision.PublicSaasAllowed);
        Assert.IsFalse(decision.RealBillingAllowed);
        Assert.IsFalse(decision.RealEmailAllowed);
        Assert.IsFalse(decision.RealClientCredentialsAllowed);
        Assert.IsFalse(decision.SensitiveRealPilotAllowed);
        Assert.IsFalse(decision.SubmitPaySignDeleteAllowed);
    }

    [TestMethod]
    public void PrivatePreviewReadinessMetricsAreMarkedEstimated()
    {
        var dashboard = Dashboard();

        Assert.IsTrue(dashboard.Metrics.All(metric => metric.Estimated));
        Assert.IsTrue(dashboard.Metrics.Any(metric => metric.Area == NexaReadinessArea.ExternalLive && metric.Percent == 0));
    }

    [TestMethod]
    public void PrivatePreviewReadinessSkippedTestsDoNotBlockLocalPreviewWhenMarkedNonBlocking()
    {
        var dashboard = Dashboard();

        Assert.IsFalse(dashboard.RelevantSkippedTests.Any(item => item.BlocksLocalPrivatePreview));
        Assert.IsTrue(dashboard.Decision.LocalPreviewAllowed);
    }

    [TestMethod]
    public void PrivatePreviewReadinessSkippedTestsBlockExternalLiveWhenExternalTargetBlocked()
    {
        var dashboard = Dashboard();

        Assert.IsTrue(dashboard.RelevantSkippedTests.Any(item => item.Category == NexaSkippedTestCategory.ExternalTargetBlocked));
        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateModelsPrivatePreviewControlSurface()
    {
        var state = BrowserVaultMinimalM23Tests.SafeState() with
        {
            CanonicalWorkspaceGuardDefined = true,
            CanonicalWorkspaceGuardSafe = true,
            CanonicalWorkspaceGuardBlocksNonCanonical = true,
            CanonicalWorkspaceGuardModifiesWorkspace = false,
            PrivatePreviewReadinessDashboardDefined = true,
            PrivatePreviewReadinessDashboardLocalAllowed = true,
            PrivatePreviewReadinessDashboardExternalDenied = true,
            PrivatePreviewReadinessDashboardHidesM51M65Blocked = false,
            PrivatePreviewReadinessDashboardEnablesRealSurfaces = false,
            OperatorBlockerExplanationDefined = true,
            OperatorBlockerExplanationSpecific = true,
            OperatorBlockerExplanationLeaksSecrets = false,
            PublicSaasStillDisabled = true,
            RealBillingStillDisabled = true,
            RealEmailDeliveryDisabled = true,
            M51ExternalProofDeferred = true
        };

        Assert.IsTrue(state.PrivatePreviewControlSurfaceAllowed);
    }

    private static NexaPrivatePreviewReadinessDashboard Dashboard() =>
        new NexaPrivatePreviewReadinessDashboardService().Build(
            new NexaSkippedTestsAuditReporter().CreateReport(),
            new NexaPrivatePreviewGoNoGoService().Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria(), []),
            new NexaCanonicalWorkspaceGuardResult(
                NexaCanonicalWorkspaceGuardDecisionKind.Allowed,
                @"C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit",
                @"C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit",
                "45ec42a9372b0bdb3f54f4ae159f506548cb37aa",
                "45ec42a9372b0bdb3f54f4ae159f506548cb37aa",
                "origin/chrome-lab-001-extension-local-ai-bridge",
                IsDirty: false,
                IsLegacyPath: false,
                MatchesRemoteHead: true,
                DetachedHeadAccepted: true,
                BlockingReasons: [],
                OperatorMessage: "allowed",
                ModifiedWorkspace: false));
}
