using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("MinimalNoExtensionProductSurface")]
public sealed class CdpMinimalNoExtensionProductSurfaceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-26T12:30:00Z");
    private const string ScriptPath = "scripts/verify-cloakbrowser-cdp-minimal-product-surface.ps1";

    [TestMethod]
    public void MinimalNoExtensionProductSurface_BuildsModelFromSafeSnapshot()
    {
        using var fixture = MinimalSurfaceFixture.Create();
        fixture.WriteSafeSnapshot();

        var result = Build(fixture);

        Assert.AreEqual(CdpMinimalNoExtensionProductSurfaceStatus.Ready, result.Status);
        Assert.IsTrue(result.SnapshotRead);
        Assert.IsTrue(result.MetadataOnly);
        Assert.AreEqual("minimal-no-extension-runtime-bridge", result.Model.Surface);
        Assert.AreEqual("CloakBrowser CDP", result.Model.Runtime.RuntimeLabel);
        Assert.AreEqual("cloakbrowser-cdp-direct", result.Model.Runtime.Source);
        Assert.AreEqual(6, result.Model.BrowserSkills.InteractiveElements);
        Assert.AreEqual(5, result.Model.BrowserSkills.FrictionSignals);
        Assert.AreEqual(6, result.Model.BrowserSkills.ActionMapEntries);
        Assert.IsTrue(result.Model.Evidence.EvidenceAvailable);
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_DoesNotRequireExtensionOrSidepanel()
    {
        using var fixture = MinimalSurfaceFixture.Create();
        fixture.WriteSafeSnapshot();

        var result = Build(fixture);

        Assert.IsFalse(result.Model.ExtensionRequired);
        Assert.IsFalse(result.Model.ExtensionOpened);
        Assert.IsFalse(result.Model.InstalledSidepanelHarnessUsed);
        Assert.IsFalse(result.Model.ExtensionUsed);
        Assert.IsFalse(result.Model.FallbackUsed);
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_DoesNotLaunchRuntimeOrCdpLiveFromSurface()
    {
        using var fixture = MinimalSurfaceFixture.Create();
        fixture.WriteSafeSnapshot();

        var result = Build(fixture);

        Assert.IsFalse(result.Model.RuntimeLaunchedFromSurface);
        Assert.IsFalse(result.Model.CdpLiveExecutedFromSurface);
        Assert.IsFalse(result.Model.BridgeWebSocketUsed);
        Assert.IsTrue(result.Model.ReadOnly);
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_DoesNotUseSystemBrowser()
    {
        using var fixture = MinimalSurfaceFixture.Create();
        fixture.WriteSafeSnapshot();

        var result = Build(fixture);

        Assert.IsFalse(result.Model.SystemBrowserUsed);
        Assert.IsTrue(result.Model.ExternalNavigationBlocked);
        Assert.IsFalse(result.Model.ProductFilesModified);
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_CopySummaryIsMetadataOnly()
    {
        using var fixture = MinimalSurfaceFixture.Create();
        fixture.WriteSafeSnapshot();

        var summary = CdpMinimalNoExtensionProductSurfaceBridge.BuildCopySummary(Build(fixture));

        foreach (var expected in new[]
        {
            "surface: minimal-no-extension-runtime-bridge",
            "runtime: CloakBrowser CDP",
            "source: cloakbrowser-cdp-direct",
            "extensionRequired: False",
            "extensionOpened: False",
            "installedSidepanelHarnessUsed: False",
            "extensionUsed: False",
            "systemBrowserUsed: False",
            "runtimeLaunchedFromSurface: False",
            "cdpLiveExecutedFromSurface: False",
            "bridgeWebSocketUsed: False",
            "fallbackUsed: False",
            "rawDomStored: False",
            "rawHtmlStored: False",
            "inputValuesStored: False",
            "cookiesOrStorageStored: False",
            "secretsStored: False"
        })
        {
            StringAssert.Contains(summary, expected);
        }

        foreach (var forbidden in new[] { "innerHTML", "document.cookie", "localStorage", "sessionStorage", "password", "<html" })
        {
            Assert.IsFalse(summary.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_HandlesMissingSnapshotClearly()
    {
        using var fixture = MinimalSurfaceFixture.Create();

        var result = Build(fixture);

        Assert.AreEqual(CdpMinimalNoExtensionProductSurfaceStatus.MissingSnapshot, result.Status);
        Assert.IsFalse(result.Model.Evidence.EvidenceAvailable);
        Assert.AreEqual("sin snapshot local", result.Model.Runtime.RuntimeStatus);
        Assert.IsFalse(result.Model.ExtensionRequired);
        Assert.IsFalse(result.Model.SystemBrowserUsed);
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_RejectsUnsafeSnapshotFlags()
    {
        using var fixture = MinimalSurfaceFixture.Create();
        fixture.WriteUnsafeSnapshot();

        var result = Build(fixture);

        Assert.AreEqual(CdpMinimalNoExtensionProductSurfaceStatus.InvalidSnapshot, result.Status);
        Assert.AreEqual("SNAPSHOT_UNSAFE_FLAGS", result.Error?.Code);
        Assert.IsFalse(result.Model.ExtensionRequired);
        Assert.IsFalse(result.Model.ExtensionUsed);
        Assert.IsFalse(result.Model.SystemBrowserUsed);
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_ScriptExistsAndReadsSnapshotOnly()
    {
        var script = ReadRepoText(ScriptPath);

        StringAssert.Contains(script, "cdp-status.snapshot.json");
        StringAssert.Contains(script, "minimal-no-extension-runtime-bridge");
        StringAssert.Contains(script, "NODAL_OS_CLOAKBROWSER_CDP_MINIMAL_NO_EXTENSION_PRODUCT_SURFACE_READY");
        StringAssert.Contains(script, "runtimeLaunchedFromSurface = $false");
        StringAssert.Contains(script, "cdpLiveExecutedFromSurface = $false");
        StringAssert.Contains(script, "bridgeWebSocketUsed = $false");
        Assert.IsFalse(script.Contains("verify-installed-sidepanel.mjs", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("verify-cloakbrowser-cdp-runtime.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("verify-cloakbrowser-cdp-browser-skills.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("chrome-extension://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("--load-extension", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("remote-debugging", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("Start-Process", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void MinimalNoExtensionProductSurface_ScriptEvidenceIsMetadataOnly()
    {
        var script = ReadRepoText(ScriptPath);

        foreach (var expected in new[]
        {
            "metadataOnly = $true",
            "rawDomStored = $false",
            "rawHtmlStored = $false",
            "inputValuesStored = $false",
            "cookiesOrStorageStored = $false",
            "secretsStored = $false"
        })
        {
            StringAssert.Contains(script, expected);
        }

        Assert.IsFalse(script.Contains("innerHTML", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("document.cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("sessionStorage", StringComparison.OrdinalIgnoreCase));
    }

    private static CdpMinimalNoExtensionProductSurfaceResult Build(MinimalSurfaceFixture fixture) =>
        new CdpMinimalNoExtensionProductSurfaceBridge().Build(
            new CdpMinimalNoExtensionProductSurfaceRequest(fixture.SnapshotPath, Now));

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private sealed class MinimalSurfaceFixture : IDisposable
    {
        private MinimalSurfaceFixture(string root)
        {
            Root = root;
            GeneratedRoot = Path.Combine(root, "browser-extension", "onebrain-chrome-lab", "generated");
            Directory.CreateDirectory(GeneratedRoot);
            SnapshotPath = Path.Combine(GeneratedRoot, CdpSafeLocalStatusSnapshotWriter.FileName);
        }

        public string Root { get; }

        public string GeneratedRoot { get; }

        public string SnapshotPath { get; }

        public static MinimalSurfaceFixture Create() =>
            new(Path.Combine(Path.GetTempPath(), $"nodal-cdp-minimal-surface-{Guid.NewGuid():N}"));

        public void WriteSafeSnapshot() =>
            new CdpSafeLocalStatusSnapshotWriter().WriteSnapshot(
                new CdpSafeLocalStatusSnapshot(
                    SchemaVersion: CdpSafeLocalStatusSnapshotWriter.SchemaVersion,
                    GeneratedAt: Now,
                    Channel: CdpSafeLocalStatusSnapshotWriter.Channel,
                    Source: "cloakbrowser-cdp-direct",
                    RuntimeProvider: "cloakbrowser",
                    RuntimeStatus: "listo",
                    ArtifactHashVerified: true,
                    Freshness: "Fresh",
                    LastHealthcheckAt: Now.AddMinutes(-5),
                    LastSessionAt: Now.AddMinutes(-2),
                    LastEvidenceName: "cloakbrowser-cdp-browser-skills-session.redacted.json",
                    EvidenceAvailable: true,
                    CaptureOk: true,
                    ScreenshotCaptured: true,
                    InteractiveElements: 6,
                    FrictionSignals: 5,
                    ActionMapEntries: 6,
                    RuntimeShutdown: true,
                    ProcessExited: true,
                    OrphanProcessDetected: false,
                    ExtensionUsed: false,
                    SystemBrowserUsed: false,
                    BoundaryReadOnly: true,
                    RuntimeLaunchedFromUi: false,
                    CdpLiveExecutedFromUi: false,
                    ExternalNavigationBlocked: true,
                    ProductFilesModified: false),
                SnapshotPath);

        public void WriteUnsafeSnapshot()
        {
            WriteSafeSnapshot();
            var json = File.ReadAllText(SnapshotPath).Replace("\"extensionUsed\": false", "\"extensionUsed\": true");
            File.WriteAllText(SnapshotPath, json);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
