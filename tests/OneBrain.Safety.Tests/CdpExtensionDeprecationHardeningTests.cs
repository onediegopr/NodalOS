using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ExtensionDeprecationHardening")]
public sealed class CdpExtensionDeprecationHardeningTests
{
    private const string DeprecationScriptPath = "scripts/verify-cloakbrowser-cdp-extension-deprecation-hardening.ps1";
    private const string NoExtensionHarnessPath = "scripts/verify-cloakbrowser-cdp-no-extension-default.ps1";
    private const string MinimalSurfaceScriptPath = "scripts/verify-cloakbrowser-cdp-minimal-product-surface.ps1";
    private const string InstalledSidepanelHarnessPath = "scripts/verify-installed-sidepanel.mjs";

    [TestMethod]
    public void ExtensionDeprecationHardening_EvaluatorMarksCdpNoExtensionAsDefault()
    {
        var root = RepoRoot();
        var result = new CdpExtensionDeprecationHardening().Evaluate(
            new CdpExtensionDeprecationHardeningRequest(
                RepositoryRoot: root,
                LockfilePath: Path.Combine(root, "browser-runtime.lock.json"),
                NoExtensionDefaultHarnessPath: Path.Combine(root, NoExtensionHarnessPath),
                MinimalProductSurfaceScriptPath: Path.Combine(root, MinimalSurfaceScriptPath),
                InstalledSidepanelHarnessPath: Path.Combine(root, InstalledSidepanelHarnessPath)));

        Assert.IsTrue(result.IsHardened, string.Join(", ", result.Errors));
        Assert.AreEqual("cloakbrowser-cdp-direct", result.DefaultRuntime);
        Assert.AreEqual("cloakbrowser-cdp-no-extension", result.DefaultHarness);
        Assert.AreEqual("minimal-no-extension-runtime-bridge", result.MinimalProductSurface);
        Assert.AreEqual("legacy/no-default", result.ExtensionMode);
        Assert.IsFalse(result.ExtensionDefaultRuntime);
        Assert.IsFalse(result.InstalledSidepanelHarnessDefault);
        Assert.IsFalse(result.ExtensionFallbackAllowed);
        Assert.IsFalse(result.SystemBrowserFallbackAllowed);
    }

    [TestMethod]
    public void ExtensionDeprecationHardening_LockDisablesExtensionAndSystemBrowser()
    {
        var runtimeLock = BrowserRuntimeLock.Load(Path.Combine(RepoRoot(), "browser-runtime.lock.json"));

        Assert.AreEqual("cloakbrowser", runtimeLock.Provider);
        Assert.AreEqual("cdp-direct", runtimeLock.Mode);
        Assert.IsFalse(runtimeLock.ExtensionEnabled);
        Assert.IsFalse(runtimeLock.SystemBrowserAllowed);
        Assert.IsTrue(runtimeLock.HasPinnedRuntimeArtifact);
        Assert.IsTrue(runtimeLock.Validate().IsValid);
    }

    [TestMethod]
    public void ExtensionDeprecationHardening_InstalledSidepanelHarnessIsCompatOnly()
    {
        var harness = ReadRepoText(InstalledSidepanelHarnessPath);

        StringAssert.Contains(harness, "browserRuntimeDefault: 'cloakbrowser-cdp-no-extension'");
        StringAssert.Contains(harness, "defaultRuntimeHarness: 'legacy-installed-sidepanel-compat-only'");
        StringAssert.Contains(harness, "installedSidepanelHarnessDefault: false");
        StringAssert.Contains(harness, "extensionRuntimeDefault: false");
        StringAssert.Contains(harness, "installed sidepanel harness remains legacy compatibility only");
    }

    [TestMethod]
    public void ExtensionDeprecationHardening_DefaultHarnessDoesNotUseExtension()
    {
        var harness = ReadRepoText(NoExtensionHarnessPath);

        foreach (var expected in new[]
        {
            "defaultHarness = \"cloakbrowser-cdp-no-extension\"",
            "extensionOpened = $false",
            "installedSidepanelHarnessUsed = $false",
            "extensionUsed = $false",
            "fallbackUsed = $false",
            "runtimeLaunchedFromUi = $false",
            "cdpLiveExecutedFromUi = $false"
        })
        {
            StringAssert.Contains(harness, expected);
        }

        Assert.IsFalse(harness.Contains("verify-installed-sidepanel.mjs", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(harness.Contains("chrome-extension", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ExtensionDeprecationHardening_MinimalSurfaceDoesNotRequireExtensionRuntime()
    {
        var script = ReadRepoText(MinimalSurfaceScriptPath);
        var bridge = ReadRepoText("src/OneBrain.BrowserRuntime/CdpMinimalNoExtensionProductSurface.cs");

        foreach (var expected in new[]
        {
            "productSurface = \"minimal-no-extension-runtime-bridge\"",
            "extensionRequired = $false",
            "extensionOpened = $false",
            "installedSidepanelHarnessUsed = $false",
            "runtimeLaunchedFromSurface = $false",
            "cdpLiveExecutedFromSurface = $false",
            "bridgeWebSocketUsed = $false"
        })
        {
            StringAssert.Contains(script, expected);
        }

        StringAssert.Contains(bridge, "ExtensionRequired: false");
        StringAssert.Contains(bridge, "InstalledSidepanelHarnessUsed: false");
        StringAssert.Contains(bridge, "RuntimeLaunchedFromSurface: false");
        StringAssert.Contains(bridge, "BridgeWebSocketUsed: false");
    }

    [TestMethod]
    public void ExtensionDeprecationHardening_ScriptReadsEvidenceOnly()
    {
        var script = ReadRepoText(DeprecationScriptPath);

        StringAssert.Contains(script, "cloakbrowser-cdp-no-extension-default-*.redacted.json");
        StringAssert.Contains(script, "cloakbrowser-cdp-minimal-product-surface-*.redacted.json");
        StringAssert.Contains(script, "legacy-installed-sidepanel-compat-only");
        StringAssert.Contains(script, "NODAL_OS_CLOAKBROWSER_CDP_EXTENSION_DEPRECATION_HARDENED_FINAL");
        StringAssert.Contains(script, "verify-installed-sidepanel.mjs");
        Assert.IsFalse(script.Contains("node scripts/verify-installed-sidepanel.mjs", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("node", StringComparison.OrdinalIgnoreCase) && script.Contains("verify-installed-sidepanel.mjs", StringComparison.OrdinalIgnoreCase) && script.Contains("&", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("verify-cloakbrowser-cdp-runtime.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("verify-cloakbrowser-cdp-browser-skills.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("Start-Process", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("remote-debugging", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("chrome-extension", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ExtensionDeprecationHardening_ScriptEvidenceIsSafeAndMetadataOnly()
    {
        var script = ReadRepoText(DeprecationScriptPath);

        foreach (var expected in new[]
        {
            "extensionDefaultRuntime = $false",
            "installedSidepanelHarnessDefault = $false",
            "extensionFallbackAllowed = $false",
            "systemBrowserFallbackAllowed = $false",
            "extensionRequired = $false",
            "extensionOpened = $false",
            "extensionUsed = $false",
            "systemBrowserUsed = $false",
            "bridgeWebSocketUsed = $false",
            "runtimeLaunchedFromUi = $false",
            "cdpLiveExecutedFromUi = $false",
            "metadataOnly = $true"
        })
        {
            StringAssert.Contains(script, expected);
        }

        Assert.IsFalse(script.Contains("innerHTML", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("document.cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("sessionStorage", StringComparison.OrdinalIgnoreCase));
    }

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
}
