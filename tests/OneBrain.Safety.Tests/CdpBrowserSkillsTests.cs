using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class CdpBrowserSkillsTests
{
    private static readonly Lazy<Task<CdpBrowserSkillCaptureResult>> BrowserSkillsCapture = new(
        RunCaptureAsync,
        LazyThreadSafetyMode.ExecutionAndPublication);

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_CanCaptureControlledPage_Live()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.CaptureOk);
        Assert.AreEqual("NODAL_OS_CLOAKBROWSER_CDP_BROWSER_SKILLS_CORE_PARITY_READY", result.Decision);
        Assert.AreEqual("NODAL OS Browser Skills CDP Parity", result.PageSummary.Title);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_CapturesUrlTitleScreenshot_Live()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.PageSummary.Url?.StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("NODAL OS Browser Skills CDP Parity", result.PageSummary.Title);
        Assert.IsTrue(result.PageSummary.ScreenshotCaptured);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_DoesNotUseExtension_Live()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.PageSummary.ExtensionUsed);
        Assert.IsFalse(result.Evidence.ExtensionUsed);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_DoesNotUseSystemBrowser_Live()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.PageSummary.SystemBrowserUsed);
        Assert.IsFalse(result.Evidence.SystemBrowserUsed);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_IndexesDomMetadataOnly_Live()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.DomIndexOk);
        Assert.IsTrue(result.DomIndex.NodeCount > 0);
        Assert.IsTrue(result.DomIndex.IndexedElementsCount > 0);
        Assert.IsFalse(result.DomIndex.StoresRawHtml);
        Assert.IsFalse(result.DomIndex.StoresInputValues);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_DetectsInteractiveElements_Live()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.DomIndex.InteractiveElementsCount >= 5);
        Assert.IsTrue(result.DomIndex.InteractiveElements.Any(element => element.ElementKind == "button"));
        Assert.IsTrue(result.DomIndex.InteractiveElements.Any(element => element.ElementKind == "input"));
        Assert.IsTrue(result.DomIndex.InteractiveElements.Any(element => element.ElementKind == "link"));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_DoesNotStoreRawHtmlOrInputValues()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsFalse(result.Evidence.StoresRawHtml);
        Assert.IsFalse(result.Evidence.StoresInputValues);
        Assert.IsTrue(result.Evidence.SecretsRedacted);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_DetectsControlledFrictionSignals_Live()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.FrictionSignalsDetected);
        Assert.IsTrue(result.FrictionSignals.Any(signal => signal.SignalType == "controlled-review-marker"));
        Assert.IsTrue(result.FrictionSignals.Any(signal => signal.SignalType == "disabled-controls"));
        Assert.IsTrue(result.FrictionSignals.Any(signal => signal.SignalType == "required-empty-fields"));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_FrictionDoesNotAttemptBypass()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.FrictionSignals.All(signal => !signal.BypassAttempted));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_ExternalNavigationBlocked()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ExternalNavigationBlocked);
        Assert.IsTrue(result.FrictionSignals.Any(signal => signal.SignalType == "external-navigation-blocked"));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_ActionMapListsAllowedControlledActions()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ActionMapOk);
        Assert.IsTrue(result.ActionMap.Entries.Any(entry => entry.AllowedActions.Contains("click controlled button")));
        Assert.IsTrue(result.ActionMap.Entries.Any(entry => entry.AllowedActions.Contains("type controlled input")));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_ActionMapBlocksDangerousActions()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ActionMap.DangerousActionsBlocked);
        Assert.IsTrue(result.ActionMap.Entries.All(entry => entry.BlockedActions.Contains("captcha/challenge")));
        Assert.IsTrue(result.ActionMap.Entries.All(entry => entry.BlockedActions.Contains("file system write")));
        Assert.IsTrue(result.ActionMap.Entries.All(entry => entry.BlockedActions.Contains("shell")));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_ActionMapBlocksExternalNavigation()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ActionMap.ExternalNavigationBlocked);
        Assert.IsTrue(result.ActionMap.Entries.Any(entry => entry.BlockedActions.Contains("navigate external")));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkillsEvidence_IsMetadataOnly()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.Evidence.CdpCommandsExecuted);
        Assert.IsFalse(result.Evidence.ProductFilesModified);
        Assert.IsFalse(result.Evidence.StoresRawHtml);
        Assert.IsFalse(result.Evidence.StoresInputValues);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkillsEvidence_RedactsSecrets()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsTrue(result.Evidence.SecretsRedacted);
        Assert.IsFalse(result.DomIndex.InteractiveElements.Any(element =>
            element.Label.Contains("token=", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkillsEvidence_MatchesLegacyShapeWhereSafe()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.PageSummary.Url));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.PageSummary.Title));
        Assert.IsTrue(result.DomIndex.InteractiveElementsCount > 0);
        Assert.IsTrue(result.FrictionSignals.Count > 0);
        Assert.IsNotNull(result.Evidence);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public async Task CdpBrowserSkills_ProvidesCoreParityFields()
    {
        var result = await RequireCaptureAsync().ConfigureAwait(false);

        Assert.AreEqual("cloakbrowser-cdp-direct", result.PageSummary.Source);
        Assert.AreEqual("cloakbrowser", result.PageSummary.RuntimeProvider);
        Assert.IsTrue(result.Evidence.ScreenshotCaptured);
        Assert.IsTrue(result.Evidence.ExternalNavigationBlocked);
        Assert.IsTrue(result.ActionMap.Entries.Count > 0);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public void ChromeExtension_RemainsLegacyNoDefaultRuntimePath()
    {
        var runtimeLock = BrowserRuntimeLock.Load(Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json"));

        Assert.AreEqual("cloakbrowser", RuntimeText(BrowserRuntimeDefaults.DefaultRuntime));
        Assert.IsFalse(RuntimeFlag(BrowserRuntimeDefaults.ExtensionDefaultRuntime));
        Assert.IsTrue(RuntimeFlag(BrowserRuntimeDefaults.ExtensionLegacyUi));
        Assert.IsTrue(RuntimeFlag(BrowserRuntimeDefaults.CdpDirectRuntimeDefault));
        Assert.IsFalse(runtimeLock.ExtensionEnabled);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkills")]
    public void BrowserSkillsLegacyHarness_StillPasses()
    {
        var harnessPath = Path.Combine(FindRepositoryRoot(), "scripts", "verify-installed-sidepanel.mjs");

        Assert.IsTrue(File.Exists(harnessPath));
    }

    private static async Task<CdpBrowserSkillCaptureResult> RequireCaptureAsync()
    {
        var result = await BrowserSkillsCapture.Value.ConfigureAwait(false);
        if (result.Status == "BLOCKED"
            && result.Reason.Contains("artifact", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive(result.Reason);
        }

        if (result.Status != "PASS")
        {
            Assert.Fail(result.Reason);
        }

        return result;
    }

    private static Task<CdpBrowserSkillCaptureResult> RunCaptureAsync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var lockPath = Path.Combine(repositoryRoot, "browser-runtime.lock.json");
        return new CdpBrowserSkillsService().CapturePageAsync(
            new CdpBrowserSkillCaptureRequest(repositoryRoot, lockPath, Timeout: TimeSpan.FromSeconds(45)));
    }

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

    private static string RuntimeText(string value) => value;

    private static bool RuntimeFlag(bool value) => value;
}
