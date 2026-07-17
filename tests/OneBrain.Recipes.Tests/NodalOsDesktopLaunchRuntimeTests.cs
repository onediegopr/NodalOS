using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("DesktopPackaging")]
[TestCategory("MvpVerticalSlice")]
public sealed class NodalOsDesktopLaunchRuntimeTests
{
    [TestMethod]
    public void ResolveProductDataRootUsesWritableLocalApplicationData()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), "nodal-os-product-data-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var root = NodalOsDesktopLaunchRuntime.ResolveProductDataRoot(localApplicationData: baseDirectory);

            Assert.AreEqual(Path.Combine(baseDirectory, "NodalOS", "ProductData"), root);
            Assert.IsTrue(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(baseDirectory))
                Directory.Delete(baseDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void ResolveLoopbackUrlsAcceptsOnlyLocalHttpOrigins()
    {
        Assert.AreEqual(
            NodalOsDesktopLaunchRuntime.DefaultLoopbackUrl,
            NodalOsDesktopLaunchRuntime.ResolveLoopbackUrls(null));
        Assert.AreEqual(
            "http://localhost:5112;http://127.0.0.1:5113",
            NodalOsDesktopLaunchRuntime.ResolveLoopbackUrls("http://localhost:5112;http://127.0.0.1:5113"));

        Assert.ThrowsExactly<ArgumentException>(() =>
            NodalOsDesktopLaunchRuntime.ResolveLoopbackUrls("http://0.0.0.0:5112"));
        Assert.ThrowsExactly<ArgumentException>(() =>
            NodalOsDesktopLaunchRuntime.ResolveLoopbackUrls("https://127.0.0.1:5112"));
        Assert.ThrowsExactly<ArgumentException>(() =>
            NodalOsDesktopLaunchRuntime.ResolveLoopbackUrls("http://127.0.0.1:5112/path"));
        Assert.ThrowsExactly<ArgumentException>(() =>
            NodalOsDesktopLaunchRuntime.ResolveLoopbackUrls("http://127.0.0.1:5112?source=external"));
    }

    [TestMethod]
    public void PackagedProductSurfaceAllowsOnlyCanonicalProductRoutes()
    {
        foreach (var route in new[]
        {
            "/",
            "/api/mission-control",
            "/workspace/select",
            "/mission/new",
            "/mission/execution",
            "/models/config"
        })
        {
            Assert.IsTrue(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(route), route);
        }

        foreach (var route in new[]
        {
            "/pilot/legacy",
            "/recording/demo",
            "/executor-harness",
            "/runs",
            "/recipes",
            "/api/runtime/inspector",
            "/guia",
            "/workspace/select/"
        })
        {
            Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(route), route);
        }

        Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(null));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(string.Empty));
    }

    [TestMethod]
    public void BrowserLaunchDefaultsToPackagedModeAndHonorsExplicitOverrides()
    {
        Assert.IsTrue(NodalOsDesktopLaunchRuntime.ShouldOpenBrowser([], packaged: true));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.ShouldOpenBrowser([], packaged: false));
        Assert.IsTrue(NodalOsDesktopLaunchRuntime.ShouldOpenBrowser(["--open-browser"], packaged: false));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.ShouldOpenBrowser(["--no-open-browser"], packaged: true));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.ShouldOpenBrowser(
            ["--open-browser", "--no-open-browser"],
            packaged: true));
    }

    [TestMethod]
    public void RegisterBrowserLaunchWaitsForApplicationStartedAndUsesFirstOrigin()
    {
        using var lifetime = new TestLifetime();
        string? opened = null;
        NodalOsDesktopLaunchRuntime.RegisterBrowserLaunch(
            lifetime,
            "http://127.0.0.1:5114;http://localhost:5115",
            target => opened = target);

        Assert.IsNull(opened);
        lifetime.Start();
        Assert.AreEqual("http://127.0.0.1:5114", opened);
    }

    private sealed class TestLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource started = new();
        private readonly CancellationTokenSource stopping = new();
        private readonly CancellationTokenSource stopped = new();

        public CancellationToken ApplicationStarted => started.Token;
        public CancellationToken ApplicationStopping => stopping.Token;
        public CancellationToken ApplicationStopped => stopped.Token;

        public void StopApplication() => stopping.Cancel();
        public void Start() => started.Cancel();

        public void Dispose()
        {
            started.Dispose();
            stopping.Dispose();
            stopped.Dispose();
        }
    }
}
