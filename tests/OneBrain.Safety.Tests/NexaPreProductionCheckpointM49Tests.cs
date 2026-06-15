using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPreProductionCheckpointM49Tests
{
    [TestMethod]
    public void NexaPreProductionCheckpointReportsCurrentCapabilities()
    {
        var report = Report();

        Assert.IsTrue(report.Capabilities.Any(capability => capability.Capability == NexaPreProductionCapabilityKind.BrowserRuntime && capability.Available));
        Assert.IsTrue(report.Capabilities.Any(capability => capability.Capability == NexaPreProductionCapabilityKind.PublicApiBoundary && capability.Available));
    }

    [TestMethod]
    public void NexaPreProductionCheckpointBlocksSensitiveRealPilot()
    {
        Assert.IsTrue(Report().Blockers.Any(blocker => blocker.BlockerId == "blocker-sensitive-real-pilot"));
    }

    [TestMethod]
    public void NexaPreProductionCheckpointBlocksPublicSaas()
    {
        Assert.IsTrue(Report().Blockers.Any(blocker => blocker.BlockerId == "blocker-public-saas"));
    }

    [TestMethod]
    public void NexaPreProductionCheckpointBlocksRealBilling()
    {
        Assert.IsTrue(Report().Blockers.Any(blocker => blocker.BlockerId == "blocker-real-billing"));
    }

    [TestMethod]
    public void NexaPreProductionCheckpointReportsM25BBlocked()
    {
        Assert.IsTrue(Report().RiskRegister.Risks.Any(risk => risk.RiskId == "risk-m25b-blocked" && risk.Open));
    }

    [TestMethod]
    public void NexaPreProductionCheckpointAllowsProductAdminPrivatePreview()
    {
        Assert.AreEqual(NexaPreProductionStatus.ReadyForProductAdminPrivatePreview, Report().Recommendation.Status);
    }

    [TestMethod]
    public void NexaPreProductionCheckpointRecommendsNextRoadmap()
    {
        Assert.IsTrue(Report().Recommendation.NextRoadmapPath.Contains("Product/Admin", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaPreProductionCheckpointRequiresExternalAuditBeforeRealSensitivePilot()
    {
        Assert.IsTrue(Report().Recommendation.ExternalAuditRequired);
    }

    [TestMethod]
    public void NexaPreProductionCheckpointDoesNotExposeSecretsCookiesBodies()
    {
        var report = Report();

        Assert.IsTrue(report.Validate().IsValid);
        Assert.IsFalse(report.ContainsSecretsCookiesBodies);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenSensitiveRealPilotStillEnabled()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(NexaLocalProductShellM48Tests.SafeState() with { SensitiveRealPilotStillDisabled = false });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "local product shell pre-production checkpoint safe");
    }

    private static NexaPreProductionCheckpointReport Report() =>
        new NexaPreProductionCheckpointService().Create(new NexaPreProductionCheckpointRequest(
            M25BExternalLowRiskTargetAvailable: false,
            SensitiveRealPilotDecisionApproved: false,
            PublicSaasEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            AutoUpdateRealEnabled: false,
            ProductiveRecorderReplayEnabled: false,
            ProfileRawEnabled: false,
            RealClientCredentialsEnabled: false));
}
