using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaRealSurfaceLeakHardeningM67Tests
{
    [TestMethod]
    public void NexaRealSurfaceLeakHardeningPrivatePreviewDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.OnboardingAudit);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningFeedbackDoesNotExposeSecrets() =>
        AssertRealObjectSafe(new NexaPrivatePreviewFeedbackEvaluator().CreateSummary("preview-session", "tenant-local", "workspace-local", []));

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningPrivateLocalApiDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.PublicApiDto);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningApiDiagnosticsDoesNotExposeSecrets() =>
        AssertRealObjectSafe(new NexaPrivateLocalApiDiagnosticsCollector().Collect([]));

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningEmailTemplatesDoNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.EmailOutboxMock);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningEmailOutboxDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.EmailOutboxMock);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningBillingLedgerDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.BillingMockInvoicePreview);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningDiagnosticsBundleDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.DiagnosticsBundle);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningSupportBundleDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.SupportBundle);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningAuditExportDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.AuditExport);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningLocalShellDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.LocalProductShellRenderModel);

    [TestMethod]
    public void NexaRealSurfaceLeakHardeningCheckpointDoesNotExposeSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.PreProductionCheckpointReport);

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithM67Hardening()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState());

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
        CollectionAssert.Contains(report.PassedChecks.ToList(), "M67 role skipped leak hardening safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenRealSurfaceLeakHardeningMissing()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState() with { RealSurfaceLeakHardeningCompleted = false });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "M67 role skipped leak hardening safe");
    }

    private static void AssertSurfaceSafe(NexaLeakHardeningSurface surface)
    {
        var evaluator = new NexaLeakHardeningEvaluator();
        var report = evaluator.Evaluate(evaluator.CreateRealSurfaceArtifacts());
        var check = report.Checks.Single(check => check.Surface == surface);

        Assert.IsTrue(report.IsSafe);
        Assert.IsTrue(check.Passed);
        Assert.AreEqual(0, check.LeakedValues.Count);
    }

    private static void AssertRealObjectSafe(object value)
    {
        var json = NexaLeakHardeningSerialization.ToSafeJson(value);

        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(json), json);
        foreach (var secret in NexaLeakHardeningCorpus.Default().SecretValues)
            Assert.IsFalse(json.Contains(secret, StringComparison.Ordinal), secret);
    }
}
