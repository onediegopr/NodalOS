using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaLeakHardeningM52Tests
{
    [TestMethod]
    public void NexaLeakHardeningAdminAuditDoesNotExposeOpaqueSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.AdminAudit);

    [TestMethod]
    public void NexaLeakHardeningDiagnosticsDoesNotExposeOpaqueSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.DiagnosticsBundle);

    [TestMethod]
    public void NexaLeakHardeningSupportBundleDoesNotExposeOpaqueSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.SupportBundle);

    [TestMethod]
    public void NexaLeakHardeningAuditExportDoesNotExposeOpaqueSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.AuditExport);

    [TestMethod]
    public void NexaLeakHardeningPublicApiDtosDoNotExposeOpaqueSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.PublicApiDto);

    [TestMethod]
    public void NexaLeakHardeningLocalProductShellDoesNotExposeOpaqueSecrets() =>
        AssertSurfaceSafe(NexaLeakHardeningSurface.LocalProductShellRenderModel);

    [TestMethod]
    public void NexaLeakHardeningOnboardingBillingEmailMocksDoNotExposeOpaqueSecrets()
    {
        AssertSurfaceSafe(NexaLeakHardeningSurface.OnboardingAudit);
        AssertSurfaceSafe(NexaLeakHardeningSurface.BillingMockInvoicePreview);
        AssertSurfaceSafe(NexaLeakHardeningSurface.EmailOutboxMock);
    }

    [TestMethod]
    public void NexaLeakHardeningInstallerReleaseReportsDoNotExposeOpaqueSecrets()
    {
        AssertSurfaceSafe(NexaLeakHardeningSurface.ReleaseUpdateManifest);
        AssertSurfaceSafe(NexaLeakHardeningSurface.InstallerDryRunReport);
        AssertSurfaceSafe(NexaLeakHardeningSurface.PreProductionCheckpointReport);
    }

    [TestMethod]
    public void NexaRedactionFuzzRedactsOpaqueTokens()
    {
        var results = new NexaLeakHardeningEvaluator().RunDefaultFuzz();

        Assert.IsTrue(results.Where(result => result.Input.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                                              result.Input.Contains("Cookie", StringComparison.OrdinalIgnoreCase) ||
                                              result.Input.Contains("Authorization", StringComparison.OrdinalIgnoreCase) ||
                                              result.Input.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
                                              result.Input.Contains("Users", StringComparison.OrdinalIgnoreCase))
            .All(result => result.Redacted.Contains(BrowserCredentialRedactor.Redacted, StringComparison.Ordinal)));
    }

    [TestMethod]
    public void NexaRedactionFuzzPreservesSafeHostnames()
    {
        var results = new NexaLeakHardeningEvaluator().RunDefaultFuzz();

        Assert.IsTrue(results.Any(result => result.Input.Contains("preview.nexa.local", StringComparison.Ordinal) &&
                                            result.Redacted.Contains("preview.nexa.local", StringComparison.Ordinal)));
        Assert.IsTrue(results.Any(result => result.Input.Contains("synthetic-report.pdf", StringComparison.Ordinal) &&
                                            result.Redacted.Contains("synthetic-report.pdf", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void NexaSkippedTestsAuditReportExists()
    {
        var report = new NexaSkippedTestsAuditReporter().CreateReport();

        Assert.IsTrue(report.Completed);
        Assert.IsTrue(report.Redacted);
        Assert.IsFalse(report.BlocksLocalPrivatePreview);
        Assert.IsTrue(report.Items.Any(item => item.TestName.Contains("BrowserExternalReadOnlyLive", StringComparison.Ordinal)));
    }

    private static void AssertSurfaceSafe(NexaLeakHardeningSurface surface)
    {
        var evaluator = new NexaLeakHardeningEvaluator();
        var report = evaluator.Evaluate(evaluator.CreateSafeSurfaceArtifacts());
        var check = report.Checks.Single(check => check.Surface == surface);

        Assert.IsTrue(report.IsSafe);
        Assert.IsTrue(check.Passed);
        Assert.AreEqual(0, check.LeakedValues.Count);
    }
}
