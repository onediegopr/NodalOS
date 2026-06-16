using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserEmbeddedRuntimeM69Tests
{
    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxIsDisabledByDefault()
    {
        var sandbox = new BrowserEmbeddedRuntimeSandboxRunner().CreateSandbox();

        Assert.IsFalse(sandbox.EnabledByDefault);
        Assert.IsTrue(sandbox.ProductionDisabled);
    }

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxUsesLocalFixtureOnly()
    {
        var result = RunSafe();

        Assert.IsTrue(result.NavigatedLocalFixture);
        Assert.IsTrue(result.SemanticProof.Contains("NEXA_EMBEDDED_RUNTIME_SANDBOX_OK", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxDoesNotAccessExternalSites()
    {
        var result = RunSafeRequest() with { AllowExternalSites = true };
        var blocked = new BrowserEmbeddedRuntimeSandboxRunner().Run(result);

        Assert.AreEqual(BrowserEmbeddedRuntimeSafetyDecisionKind.Blocked, blocked.Decision.Decision);
        CollectionAssert.Contains(blocked.Decision.ReasonCodes.ToList(), "embedded runtime sandbox local fixture only");
    }

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxDoesNotExposeCookies() =>
        Assert.IsFalse(RunSafe().Evidence.CookiesExposed);

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxDoesNotCaptureBodies() =>
        Assert.IsFalse(RunSafe().Evidence.BodiesCaptured);

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxDoesNotCaptureSensitiveHeaders() =>
        Assert.IsFalse(RunSafe().Evidence.SensitiveHeaderValuesCaptured);

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxProducesEvidence()
    {
        var result = RunSafe();

        Assert.IsTrue(result.Evidence.SemanticProofVerified);
        Assert.IsTrue(result.Evidence.EvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxBlocksDownloadsByDefault()
    {
        var blocked = new BrowserEmbeddedRuntimeSandboxRunner().Run(RunSafeRequest() with { AllowDownloads = true });

        CollectionAssert.Contains(blocked.Decision.ReasonCodes.ToList(), "embedded runtime downloads disabled by default");
    }

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxBlocksUploadsByDefault()
    {
        var blocked = new BrowserEmbeddedRuntimeSandboxRunner().Run(RunSafeRequest() with { AllowUploads = true });

        CollectionAssert.Contains(blocked.Decision.ReasonCodes.ToList(), "embedded runtime uploads disabled by default");
    }

    [TestMethod]
    public void BrowserEmbeddedRuntimeSandboxDoesNotReplaceChromeCdp()
    {
        var result = RunSafe();

        Assert.IsFalse(result.ReplacedChromeCdp);
        Assert.IsFalse(result.ProductionActivated);
    }

    private static BrowserEmbeddedRuntimeSandboxResult RunSafe() =>
        new BrowserEmbeddedRuntimeSandboxRunner().Run(RunSafeRequest());

    private static BrowserEmbeddedRuntimeSandboxRequest RunSafeRequest() =>
        new(
            "embedded-sandbox-test",
            BrowserEmbeddedRuntimeKind.WebView2Embedded,
            "/embedded/status",
            EnableSandbox: true,
            AllowExternalSites: false,
            AllowDownloads: false,
            AllowUploads: false,
            RuntimeAuthoritative: false);
}
