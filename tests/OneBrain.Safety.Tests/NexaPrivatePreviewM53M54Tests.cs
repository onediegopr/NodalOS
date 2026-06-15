using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivatePreviewM53M54Tests
{
    [TestMethod]
    public void NexaPrivatePreviewLocalReadinessPassesWithSafeProfile()
    {
        var result = SafeResult();

        Assert.IsTrue(result.Allowed);
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalBlocksPublicSaas()
    {
        var result = Evaluate(profile: NexaPrivatePreviewLocalEvaluator.SafeProfile() with { PublicSaasActivationDisabled = false });

        Assert.IsFalse(result.Allowed);
        CollectionAssert.Contains(result.Violations.ToList(), "public SaaS/API activation is blocked");
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalBlocksRealBilling()
    {
        var result = Evaluate(profile: NexaPrivatePreviewLocalEvaluator.SafeProfile() with { MockBillingOnly = false });

        Assert.IsFalse(result.Allowed);
        CollectionAssert.Contains(result.Violations.ToList(), "real billing is blocked");
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalBlocksRealEmail()
    {
        var result = Evaluate(profile: NexaPrivatePreviewLocalEvaluator.SafeProfile() with { MockEmailOnly = false });

        Assert.IsFalse(result.Allowed);
        CollectionAssert.Contains(result.Violations.ToList(), "real email is blocked");
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalBlocksSensitiveRealPilot()
    {
        var result = Evaluate(profile: NexaPrivatePreviewLocalEvaluator.SafeProfile() with { SensitiveRealPilotDisabled = false });

        Assert.IsFalse(result.Allowed);
        CollectionAssert.Contains(result.Violations.ToList(), "sensitive real pilot is blocked");
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalRequiresAuditKeyCustody()
    {
        var result = Evaluate(readiness: NexaPrivatePreviewLocalEvaluator.SafeReadiness() with { AuditKeyCustodyAvailable = false });

        Assert.IsFalse(result.Allowed);
        CollectionAssert.Contains(result.Violations.ToList(), "audit key custody missing");
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalRequiresDiagnosticsRedaction()
    {
        var result = Evaluate(readiness: NexaPrivatePreviewLocalEvaluator.SafeReadiness() with { DiagnosticsRedacted = false });

        Assert.IsFalse(result.Allowed);
        CollectionAssert.Contains(result.Violations.ToList(), "diagnostics redaction missing");
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalRequiresTenantGovernance()
    {
        var result = Evaluate(readiness: NexaPrivatePreviewLocalEvaluator.SafeReadiness() with { TenantGovernanceAvailable = false });

        Assert.IsFalse(result.Allowed);
        CollectionAssert.Contains(result.Violations.ToList(), "tenant governance unavailable");
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalDoesNotExposeSecrets()
    {
        var json = NexaLeakHardeningSerialization.ToSafeJson(SafeResult());

        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(json));
        foreach (var secret in NexaLeakHardeningCorpus.Default().SecretValues)
            Assert.IsFalse(json.Contains(secret, StringComparison.Ordinal));
    }

    [TestMethod]
    public void NexaPrivatePreviewLocalReportsM51Deferred()
    {
        Assert.IsTrue(SafeResult().Session.M51ExternalProofDeferred);
    }

    [TestMethod]
    public void NexaPrivatePreviewRunbookExistsAndIsSafe()
    {
        var runbook = new NexaPrivatePreviewRunbookFactory().Create();

        Assert.IsTrue(runbook.IsSafe);
        Assert.IsTrue(runbook.KnownLimitations.Items.Any(item => item.Contains("M51 external read-only target proof is deferred", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithPrivatePreviewLocalSafe()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(PrivatePreviewState());

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
        CollectionAssert.Contains(report.PassedChecks.ToList(), "private preview local safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenPrivatePreviewAttemptsPublicSaas()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(PrivatePreviewState() with { PrivatePreviewAttemptsPublicSaas = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "private preview local safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenPrivatePreviewAuditKeyMissing()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(PrivatePreviewState() with { PrivatePreviewAuditKeyCustodyMissing = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "private preview local safe");
    }

    private static NexaPrivatePreviewLocalResult SafeResult() => Evaluate();

    private static NexaPrivatePreviewLocalResult Evaluate(
        NexaPrivatePreviewLocalProfile? profile = null,
        NexaPrivatePreviewLocalSession? session = null,
        NexaPrivatePreviewLocalReadiness? readiness = null) =>
        new NexaPrivatePreviewLocalEvaluator().Evaluate(
            profile ?? NexaPrivatePreviewLocalEvaluator.SafeProfile(),
            session ?? NexaPrivatePreviewLocalEvaluator.SafeSession(),
            readiness ?? NexaPrivatePreviewLocalEvaluator.SafeReadiness());

    private static BrowserRuntimeObservedState PrivatePreviewState() =>
        NexaLocalProductShellM48Tests.SafeState() with
        {
            LeakHardeningCompleted = true,
            SkippedTestsAuditCompleted = true,
            PrivatePreviewLocalAllowed = true,
            PrivatePreviewLocalSafe = true,
            M51ExternalProofDeferred = true,
            DiagnosticsBundleDefined = true,
            DiagnosticsBundleLeaksSecrets = false,
            PublicApiDesignOnly = true,
            PublicApiNetworkExposureDisabled = true,
            PublicSaasStillDisabled = true,
            RealBillingStillDisabled = true,
            RealBillingDisabled = true,
            RealEmailDeliveryDisabled = true,
            SensitiveRealPilotStillDisabled = true,
            FeatureFlagSensitiveRealPilotEnabled = false,
            FeatureFlagReplayProductiveEnabled = false,
            FeatureFlagRecorderProductiveEnabled = false,
            AuditIntegrityKeyProviderConfigured = true,
            AuditIntegrityDefaultFailClosed = true,
            AuditIntegrityDevFixtureExplicitOnly = true,
            AuditIntegrityKeyHealthOk = true,
            AuditLedgerHeadSealIncludesKeyId = true
        };
}
