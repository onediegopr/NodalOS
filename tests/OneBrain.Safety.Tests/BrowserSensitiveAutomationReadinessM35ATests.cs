using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserSensitiveAutomationReadinessM35ATests
{
    [TestMethod]
    public void BrowserSensitiveAutomationReadinessReportsSimulationOnlyWhenM25BBlocked()
    {
        var report = Report();

        Assert.AreEqual(BrowserSensitiveAutomationReadinessStatus.ReadyForSimulationOnly, report.Status);
        Assert.IsTrue(report.M25BBlocked);
        Assert.IsTrue(report.BlocksSensitiveRealPilot);
    }

    [TestMethod]
    public void BrowserSensitiveAutomationReadinessBlocksRealPilotWithoutDecision()
    {
        var report = Report(requestMutation: r => r with { SensitiveRealPilotDecisionApproved = false });

        Assert.IsTrue(report.BlocksSensitiveRealPilot);
        Assert.IsTrue(report.Checks.Any(c => c.Name == "sensitive real pilot status" && c.Status == BrowserSensitiveAutomationReadinessCheckStatus.Blocked));
    }

    [TestMethod]
    public void BrowserSensitiveAutomationReadinessReportsVaultSandboxNotProductive()
    {
        var report = Report();

        Assert.IsTrue(report.Checks.Any(c => c.Name == "vault mode" && c.Status == BrowserSensitiveAutomationReadinessCheckStatus.Passed));
        Assert.IsTrue(report.RiskRegister.Risks.Any(r => r.RiskId == "R3" && r.Title.Contains("SandboxLocalEncrypted", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void BrowserSensitiveAutomationReadinessReportsRecorderReplaySafeOnly()
    {
        var report = Report();

        Assert.IsTrue(report.Checks.Any(c => c.Name == "recorder mode" && c.Status == BrowserSensitiveAutomationReadinessCheckStatus.Passed));
        Assert.IsTrue(report.Checks.Any(c => c.Name == "replay mode" && c.Status == BrowserSensitiveAutomationReadinessCheckStatus.Passed));
        Assert.IsTrue(report.RiskRegister.Includes("R7"));
    }

    [TestMethod]
    public void BrowserSensitiveAutomationReadinessReportsSensitiveSimulationComplete()
    {
        var report = Report();

        Assert.IsTrue(report.Checks.Any(c => c.Name == "sensitive simulation status" && c.Status == BrowserSensitiveAutomationReadinessCheckStatus.Passed));
        Assert.IsTrue(report.IsCheckpointComplete);
    }

    [TestMethod]
    public void BrowserSensitiveAutomationReadinessRecommendsNextSteps()
    {
        var report = Report();

        Assert.AreEqual(BrowserSensitiveAutomationRoadmapOption.M25BExternalTestOwnedTarget, report.RecommendedNextStep.Option);
        Assert.AreEqual(BrowserSensitiveAutomationRecommendationKind.AdvanceWithConditions, report.RecommendedNextStep.Recommendation);
    }

    [TestMethod]
    public void BrowserSensitiveAutomationRiskRegisterIncludesM25BBlocked()
    {
        var report = Report();

        Assert.IsTrue(report.RiskRegister.Includes("R1"));
        Assert.AreEqual(BrowserSensitiveAutomationRiskSeverity.High, report.RiskRegister.Risks.Single(r => r.RiskId == "R1").Severity);
    }

    [TestMethod]
    public void BrowserSensitiveAutomationDecisionMatrixBlocksRealPilotWithoutPreconditions()
    {
        var report = Report();
        var m33b = report.DecisionMatrix.For(BrowserSensitiveAutomationRoadmapOption.M33BSensitiveReadOnlyRealPilot);
        var m34b = report.DecisionMatrix.For(BrowserSensitiveAutomationRoadmapOption.M34BSensitiveDocumentRealPilot);

        Assert.AreEqual(BrowserSensitiveAutomationRecommendationKind.DoNotAdvance, m33b.Recommendation);
        Assert.AreEqual(BrowserSensitiveAutomationRecommendationKind.DoNotAdvance, m34b.Recommendation);
        Assert.IsTrue(m33b.Blockers.Count > 0);
    }

    [TestMethod]
    public void BrowserSensitiveAutomationDecisionMatrixAllowsProductTrackWithConditions()
    {
        var report = Report(requestMutation: r => r with { ProductAdminTrackRequested = true });
        var product = report.DecisionMatrix.For(BrowserSensitiveAutomationRoadmapOption.AdminLicensingProductTrack);

        Assert.AreEqual(BrowserSensitiveAutomationReadinessStatus.ReadyForProductAdminTrack, report.Status);
        Assert.AreEqual(BrowserSensitiveAutomationRecommendationKind.AdvanceWithConditions, product.Recommendation);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveRealPilotWithoutCheckpointDecision()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, SafeState() with
        {
            SensitiveAutomationCheckpointCompleted = true,
            SensitiveSiteRealPilotActive = true,
            SensitiveRealPilotDecisionApproved = false
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "sensitive real pilot checkpoint decision");
    }

    private static BrowserSensitiveAutomationReadinessReport Report(Func<BrowserSensitiveAutomationReadinessRequest, BrowserSensitiveAutomationReadinessRequest>? requestMutation = null)
    {
        var request = new BrowserSensitiveAutomationReadinessRequest(
            BrowserVaultMinimalM23Tests.GateReport(SafeState()),
            M25BExternalLowRiskTargetAvailable: false,
            M28ExternalWorkflowEnabled: false,
            SensitiveReadOnlySimulationComplete: true,
            SensitiveDocumentSimulationComplete: true,
            SensitiveRealPilotDecisionApproved: false,
            ProductAdminTrackRequested: false);
        request = requestMutation?.Invoke(request) ?? request;
        return new BrowserSensitiveAutomationReadinessService().Evaluate(request);
    }

    private static BrowserRuntimeObservedState SafeState() =>
        BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProfileState = BrowserRuntimeProfileState.UserProfileControlledWithConsent,
            ControlledProfileConsentValid = true,
            DownloadState = BrowserRuntimeDownloadState.SafeDownloadActive,
            SafeDownloadAllowlistValid = true,
            SafeDownloadQuarantineEnabled = true,
            SafeDownloadHashRequired = true,
            SafeDownloadControlledRoot = true,
            SafeUploadState = BrowserRuntimeUploadState.SafeUploadActive,
            SafeUploadAllowlistValid = true,
            SafeUploadApprovalPresent = true,
            SafeUploadControlledRoot = true,
            SafeUploadHashRequired = true,
            RecorderState = BrowserRuntimeRecorderState.ReadOnlyPrototypeActive,
            ReplayState = BrowserRuntimeReplayState.SafeModeReadOnlyActive,
            SensitiveAutomationCheckpointCompleted = true,
            SensitiveReadOnlySimulationActive = true,
            SensitiveDocumentSimulationActive = true,
            ExternalLowRiskTargetAvailable = false,
            ProductTrackAllowed = false
        };
}
