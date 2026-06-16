using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsPrivatePreviewReleaseM115M117Tests
{
    [TestMethod]
    public void ProductAdminPrivatePreviewShowsNodalOsVisibleName()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();

        Assert.AreEqual("NODAL OS", report.ProductName);
        Assert.IsTrue(report.VisibleStates.Contains(NodalOsProductAdminPrivatePreviewStatus.LocalPrivatePreviewReady));
        Assert.IsTrue(report.Redacted);
    }

    [TestMethod]
    public void ProductAdminPrivatePreviewReflectsM51M65AndBlocksExternalGeneral()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();

        StringAssert.Contains(report.M51Status, "HTTP read-only");
        StringAssert.Contains(report.M65Status, "target-owned Chrome/CDP/DOM read-only");
        Assert.IsFalse(report.ExternalGeneralReady);
        Assert.IsTrue(report.EvidenceRefs.Any(r => r.Contains("audit-ledger-edb3e2fbb0a0446788dae17a269c0058", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ProductAdminPrivatePreviewKeepsDangerousSurfacesBlocked()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();

        Assert.IsTrue(report.PublicSaasBlocked);
        Assert.IsTrue(report.PublicApiBlocked);
        Assert.IsTrue(report.BillingEmailBlocked);
        Assert.IsTrue(report.CredentialsBlocked);
        Assert.IsTrue(report.SensitiveSurfacesBlocked);
        Assert.IsTrue(report.SubmitPaySignDeleteBlocked);
        Assert.IsTrue(report.RecorderReplayBlocked);
    }

    [TestMethod]
    public void ProductAdminPrivatePreviewUiAdminHasNoAuthorityOutsideCore()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();

        Assert.IsTrue(report.UiAdminAuthorityBlocked);
        Assert.IsTrue(report.CoreAuthorityRequired);
    }

    [TestMethod]
    public void OperatorUxReadinessSummaryIncludesEvidenceAndActiveBlockers()
    {
        var summary = new NodalOsOperatorUxReadinessService().BuildDefaultSummary();

        Assert.AreEqual("NODAL OS", summary.ProductName);
        Assert.IsTrue(summary.LedgerRefs.Any(r => r.Contains("audit-ledger-edb3e2fbb0a0446788dae17a269c0058", StringComparison.Ordinal)));
        Assert.IsTrue(summary.ActiveBlockers.Any(b => b.Contains("public SaaS", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(summary.BlockedExternalSensitiveActions.Any(b => b.Contains("submit/pay/sign/delete", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void OperatorUxReadinessSummaryDoesNotExposeSecretsCookiesTokens()
    {
        var json = JsonSerializer.Serialize(new NodalOsOperatorUxReadinessService().BuildDefaultSummary());

        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-bearer-token", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-refresh-token", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OperatorUxRunbookExistsAndContainsAllowedAndBlockedInstructions()
    {
        var text = File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-private-preview-operator-runbook.md"));

        StringAssert.Contains(text, "NODAL OS");
        StringAssert.Contains(text, "What Can Be Tested");
        StringAssert.Contains(text, "What Cannot Be Tested");
        StringAssert.Contains(text, "Stop the flow");
        StringAssert.Contains(text, "target-owned proof");
    }

    [TestMethod]
    public void OperatorUxReadinessInstructionsAreClearForHumanIntervention()
    {
        var summary = new NodalOsOperatorUxReadinessService().BuildDefaultSummary();

        StringAssert.Contains(summary.RequiredHumanIntervention, "stop");
        StringAssert.Contains(summary.NextAction, "internal local private preview");
        Assert.AreEqual("medium-local-preview", summary.RiskLevel);
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateBlocksNonCanonicalWorktree()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(
            SafeInput() with { CanonicalWorktreeOk = false });

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByWorktree, decision.Status);
        Assert.IsFalse(decision.ReadyWithRestrictions);
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateBlocksMissingEvidence()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(
            SafeInput() with { M65ClosedLimitedCdpScope = false });

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByMissingEvidence, decision.Status);
        Assert.IsTrue(decision.ReasonCodes.Any(r => r.Contains("M65", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateReadyWithRestrictionsWhenLocalConditionsPass()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(SafeInput());

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions, decision.Status);
        Assert.IsTrue(decision.ReadyWithRestrictions);
        Assert.AreEqual(NodalOsLocalPrivatePreviewScope.InternalLocalPrivatePreviewOnly, decision.Scope);
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateMaintainsDangerousSurfacesBlocked()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(SafeInput());

        Assert.IsTrue(decision.PublicSaasStillDisabled);
        Assert.IsTrue(decision.PublicApiStillDisabled);
        Assert.IsTrue(decision.RealBillingStillDisabled);
        Assert.IsTrue(decision.RealEmailStillDisabled);
        Assert.IsTrue(decision.RealCredentialsStillBlocked);
        Assert.IsTrue(decision.SensitiveSurfacesStillBlocked);
        Assert.IsTrue(decision.SubmitPaySignDeleteStillBlocked);
        Assert.IsTrue(decision.RecorderReplayProductiveStillBlocked);
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateBlocksExternalGeneralInflation()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(
            SafeInput() with { ExternalGeneralReady = true });

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByExternalGeneral, decision.Status);
        Assert.IsFalse(decision.ExternalGeneralReady);
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateExposesNextSteps()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(SafeInput());

        StringAssert.Contains(decision.NextStep, "internal local private preview");
        Assert.IsTrue(decision.AllowedInternalActions.Any(a => a.Contains("Product/Admin", StringComparison.Ordinal)));
        Assert.IsTrue(decision.BlockedActions.Any(a => a.Contains("public SaaS", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateAdrExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "local-private-preview-release-gate-m115-m117.md"));

        StringAssert.Contains(text, "Ready with restrictions");
        StringAssert.Contains(text, "Core decides");
        StringAssert.Contains(text, "not production readiness");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateReflectsLocalPrivatePreviewReleaseGate()
    {
        var state = new BrowserRuntimeObservedState(
            CompanionAuthoritative: false,
            LegacyRunnerEnabled: false,
            RealProfileActive: false,
            RealVaultActive: false,
            LoginRealActive: false,
            BrowserNetworkCaptureMode.MetadataOnly,
            RequestBodyCaptureSupported: false,
            ResponseBodyCaptureSupported: false,
            SensitiveHeaderValueCaptureSupported: false,
            ReplayExecutableEnabled: false,
            DownloadMode: "disabled",
            UploadMode: "disabled",
            TargetFrameManagerHealthy: true,
            AuditLedgerIntegrityProviderKind: "hmac",
            AuditLedgerHeadSealAvailable: true,
            AuditLedgerHeadSealValid: true,
            CdpLiveProofAvailable: false,
            Browser004xLegacyIsolated: true,
            Capabilities: [],
            ProductAdminPrivatePreviewHardeningDefined: true,
            OperatorUxReadinessDefined: true,
            LocalPrivatePreviewReleaseGateDefined: true,
            LocalPrivatePreviewReadyWithRestrictions: true,
            LocalPrivatePreviewReleaseGateOpensDangerousSurfaces: false,
            LocalPrivatePreviewReleaseGateInflatesExternalCdp: false,
            ExternalCdpGeneralReady: false,
            PublicSaasStillDisabled: true,
            RealBillingStillDisabled: true,
            RealEmailDeliveryDisabled: true,
            RealClientCredentialsStillBlocked: true,
            SensitiveRealPilotStillDisabled: true);

        Assert.IsTrue(state.LocalPrivatePreviewReleaseGateAllowed);
    }

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }

    private static NodalOsLocalPrivatePreviewReleaseGateInput SafeInput() =>
        new(
            BuildOk: true,
            TestsOk: true,
            CanonicalWorktreeOk: true,
            M51ClosedHttpScope: true,
            M65ClosedLimitedCdpScope: true,
            ProductAdminLocalReady: true,
            OperatorRunbookExists: true,
            BlockerExplanationsReady: true,
            EvidenceLogSummaryReady: true,
            ExternalGeneralReady: false,
            PublicSaasEnabled: false,
            PublicApiEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSitesEnabled: false,
            SubmitPaySignDeleteEnabled: false,
            RecorderReplayProductiveEnabled: false);
}
