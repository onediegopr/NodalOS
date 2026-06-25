using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class CloakBrowserRuntimeFoundationTests
{
    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void BrowserRuntimeGuard_RejectsChromeExecutablePath()
    {
        var result = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(ExecutablePath: @"C:\Program Files\Google\Chrome\Application\chrome.exe"),
            ValidLock());

        Assert.IsFalse(result.IsAllowed);
        Assert.AreEqual("SystemBrowserExecutableRejected", result.Reason);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void BrowserRuntimeGuard_RejectsEdgeExecutablePath()
    {
        var result = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(ExecutablePath: @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
            ValidLock());

        Assert.IsFalse(result.IsAllowed);
        Assert.AreEqual("SystemBrowserExecutableRejected", result.Reason);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void BrowserRuntimeGuard_RejectsPlaywrightDefaultChromium()
    {
        var result = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(UsesPlaywrightDefaultChromium: true),
            ValidLock());

        Assert.IsFalse(result.IsAllowed);
        Assert.AreEqual("PlaywrightDefaultChromiumRejected", result.Reason);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void BrowserRuntimeGuard_RejectsChromeAndEdgeChannels()
    {
        var chrome = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(PlaywrightChannel: "chrome"),
            ValidLock());
        var edge = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(PlaywrightChannel: "msedge"),
            ValidLock());

        Assert.IsFalse(chrome.IsAllowed);
        Assert.IsFalse(edge.IsAllowed);
        Assert.AreEqual("PlaywrightSystemBrowserChannelRejected", chrome.Reason);
        Assert.AreEqual("PlaywrightSystemBrowserChannelRejected", edge.Reason);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void BrowserRuntimeGuard_RejectsPlaywrightInstallChromium()
    {
        var result = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(RequestedInstallerCommand: "npx playwright install chromium"),
            ValidLock());

        Assert.IsFalse(result.IsAllowed);
        Assert.AreEqual("PlaywrightChromiumInstallRejected", result.Reason);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void NoSystemBrowserProof_Passes()
    {
        var guard = BrowserRuntimeGuard.ValidateLaunch(new BrowserRuntimeLaunchRequest(), ValidLock());
        var lockValidation = ValidLock().Validate();

        Assert.IsTrue(guard.IsAllowed, guard.Reason);
        Assert.IsTrue(lockValidation.IsValid, string.Join(",", lockValidation.Errors));
        Assert.IsFalse(ValidLock().SystemBrowserAllowed);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CloakBrowserRuntimeProvider_RequiresLockProviderCloakBrowser()
    {
        var invalid = ValidLock() with { Provider = "chrome" };
        var status = new CloakBrowserRuntimeProvider().GetStatus(invalid);

        Assert.IsFalse(status.RuntimeConfigured);
        Assert.IsFalse(status.LaunchAllowed);
        StringAssert.Contains(status.Reason, "BrowserRuntimeProviderMustBeCloakBrowser");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CloakBrowserRuntimeLock_LoadsAndRejectsSystemBrowserAllowedTrue()
    {
        var lockPath = Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json");
        var runtimeLock = BrowserRuntimeLock.Load(lockPath);
        var invalid = runtimeLock with { SystemBrowserAllowed = true };

        Assert.AreEqual("cloakbrowser", runtimeLock.Provider);
        Assert.AreEqual("cdp-direct", runtimeLock.Mode);
        Assert.AreEqual("env-or-local-config", runtimeLock.RuntimePathPolicy);
        Assert.IsFalse(runtimeLock.SystemBrowserAllowed);
        Assert.IsFalse(invalid.Validate().IsValid);
        CollectionAssert.Contains(invalid.Validate().Errors.ToList(), "SystemBrowserMustNotBeAllowed");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CloakBrowserRuntimeLock_RejectsExtensionEnabledAsCdpDirectDefault()
    {
        var invalid = ValidLock() with { ExtensionEnabled = true };
        var validation = invalid.Validate();

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Errors.ToList(), "ExtensionMustNotBeDefaultRuntimeForCdpDirect");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CloakBrowserRuntimeProvider_BlocksLaunchWhenArtifactMissing()
    {
        var status = new CloakBrowserRuntimeProvider().GetStatus(ValidLock());

        Assert.IsTrue(status.RuntimeConfigured);
        Assert.IsFalse(status.RuntimeArtifactPresent);
        Assert.IsFalse(status.LaunchAllowed);
        Assert.IsTrue(status.CdpDirectMode);
        Assert.IsFalse(status.SystemBrowserAllowed);
        Assert.AreEqual("BLOCKED_RUNTIME_ARTIFACT_REQUIRED", status.State);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void BrowserRuntimeLocalConfig_DiscoversEnvironmentPathBeforeLocalFile()
    {
        var environment = new Dictionary<string, string?>
        {
            ["NODAL_CLOAKBROWSER_RUNTIME_PATH"] = @"D:\runtimes\cloakbrowser\cloakbrowser.exe"
        };

        var config = BrowserRuntimeLocalConfig.Discover(FindRepositoryRoot(), environment);

        Assert.AreEqual(@"D:\runtimes\cloakbrowser\cloakbrowser.exe", config.CloakBrowserExecutablePath);
        Assert.IsTrue(config.HasExecutablePath);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void BrowserRuntimeLocalConfig_LoadsLocalJsonWithoutCommittingPrivatePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "nodal-runtime-local-config-" + Guid.NewGuid().ToString("N"));
        var localDirectory = Path.Combine(tempRoot, ".local");
        Directory.CreateDirectory(localDirectory);
        var configPath = Path.Combine(localDirectory, "browser-runtime.local.json");
        File.WriteAllText(
            configPath,
            """
            {
              "cloakbrowser_executable_path": "D:\\runtimes\\cloakbrowser\\cloakbrowser.exe",
              "cdp_port": "ephemeral-or-reserved"
            }
            """);

        try
        {
            var config = BrowserRuntimeLocalConfig.Discover(tempRoot, new Dictionary<string, string?>());

            Assert.AreEqual(@"D:\runtimes\cloakbrowser\cloakbrowser.exe", config.CloakBrowserExecutablePath);
            Assert.AreEqual("ephemeral-or-reserved", config.CdpPort);
            Assert.IsTrue(config.HasExecutablePath);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CloakBrowserRuntimeVerificationScriptExistsAndReportsBlockedWhenArtifactMissing()
    {
        var scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "verify-cloakbrowser-cdp-runtime.ps1");
        var script = File.ReadAllText(scriptPath);

        StringAssert.Contains(script, "NODAL_CLOAKBROWSER_RUNTIME_PATH");
        StringAssert.Contains(script, ".local/browser-runtime.local.json");
        StringAssert.Contains(script, "NODAL_OS_CLOAKBROWSER_CDP_LIVE_STILL_BLOCKED_RUNTIME_ARTIFACT_REQUIRED");
        StringAssert.Contains(script, "CloakBrowser runtime artifact required.");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void ChromeExtension_IsNotDefaultRuntimePath()
    {
        var runtimeLock = ValidLock();

        Assert.AreEqual("cloakbrowser", RuntimeText(BrowserRuntimeDefaults.DefaultRuntime));
        Assert.IsFalse(RuntimeFlag(BrowserRuntimeDefaults.ExtensionDefaultRuntime));
        Assert.IsTrue(RuntimeFlag(BrowserRuntimeDefaults.ExtensionLegacyUi));
        Assert.IsTrue(RuntimeFlag(BrowserRuntimeDefaults.CdpDirectRuntimeDefault));
        Assert.IsFalse(RuntimeFlag(runtimeLock.ExtensionEnabled));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpInjectionManager_InjectsBootstrapMarker()
    {
        var script = new CdpInjectionManager().BuildBootstrapScript();

        StringAssert.Contains(script, CdpInjectionManager.MarkerName);
        StringAssert.Contains(script, CdpInjectionManager.BootstrapVersion);
        StringAssert.Contains(script, "__NODAL_OS_CDP_PAGE_METADATA__");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpInjectionManager_DoesNotDoubleInject()
    {
        var manager = new CdpInjectionManager();
        var script = manager.BuildBootstrapScript();

        Assert.IsTrue(manager.ContainsDoubleInjectionGuard(script));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpEvidenceAdapter_UsesCloakBrowserCdpDirectSource()
    {
        var evidence = new CdpEvidenceAdapter().CreateBlockedEvidence("about:blank", "Blank");

        Assert.AreEqual("cloakbrowser-cdp-direct", evidence.Source);
        Assert.IsFalse(evidence.ExtensionUsed);
        Assert.IsFalse(evidence.SystemBrowserUsed);
        Assert.IsFalse(evidence.BootstrapInjected);
        Assert.IsFalse(evidence.ScreenshotCaptured);
    }

    private static BrowserRuntimeLock ValidLock() =>
        new()
        {
            Provider = "cloakbrowser",
            Mode = "cdp-direct",
            ExtensionEnabled = false,
            RuntimeSource = "fork",
            RuntimeRepo = "nodal-cloakbrowser-runtime",
            RuntimeChannel = "nodal-runtime",
            RuntimePathPolicy = "env-or-local-config",
            RuntimeVersion = "pending",
            RuntimeCommit = "pending",
            UpstreamCommit = "pending",
            BinarySha256 = "pending",
            CdpHost = "127.0.0.1",
            CdpPortPolicy = "ephemeral-or-reserved",
            SystemBrowserAllowed = false
        };

    private static bool RuntimeFlag(bool value) => value;

    private static string RuntimeText(string value) => value;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "browser-runtime.lock.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Repository root with browser-runtime.lock.json was not found.");
        return string.Empty;
    }
}
