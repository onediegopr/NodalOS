using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class CloakBrowserRuntimeLiveTests
{
    private static readonly Lazy<Task<CloakBrowserCdpHealthcheckResult>> LiveHealthcheck = new(
        RunHealthcheckAsync,
        LazyThreadSafetyMode.ExecutionAndPublication);

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpConnection_Healthcheck_ReturnsBrowserVersion()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.AreEqual("PASS", result.Status);
        Assert.AreEqual("NODAL_OS_CLOAKBROWSER_CDP_DOM_SNAPSHOT_ACTION_CONTROLLER_READY", result.Decision);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.BrowserVersion));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ProtocolVersion));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpTargetManager_CanCreateAndTrackPage()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.TargetCreated);
        Assert.IsTrue(result.TargetClosed);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.TargetId));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpSessionRegistry_TracksLiveSessionLifecycle()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.SessionCreated);
        Assert.IsTrue(result.SessionClosed);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpInjectionManager_InjectsBootstrapMarker_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.BootstrapInjected);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpInjectionManager_DoesNotDoubleInject_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.DoubleInjectionPrevented);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpActionController_CanNavigateAndReadTitle()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.NavigationOk);
        Assert.IsTrue(result.TitleRead);
        Assert.AreEqual("NODAL OS CDP Controlled Action Test", result.Title);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpEvidenceAdapter_CapturesScreenshotMetadata()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ScreenshotCaptured);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ScreenshotPath));
        Assert.IsTrue(File.Exists(result.ScreenshotPath));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.EvidencePath));
        Assert.IsTrue(File.Exists(result.EvidencePath));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CloakBrowserRuntimeProvider_LaunchesPinnedArtifactOnly()
    {
        var repositoryRoot = FindRepositoryRoot();
        var lockPath = Path.Combine(repositoryRoot, "browser-runtime.lock.json");
        var runtimeLock = BrowserRuntimeLock.Load(lockPath);
        var localConfig = BrowserRuntimeLocalConfig.Discover(
            repositoryRoot,
            Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(entry => (string)entry.Key, entry => entry.Value?.ToString()));

        if (!localConfig.HasExecutablePath || !File.Exists(localConfig.CloakBrowserExecutablePath))
        {
            Assert.Inconclusive("CloakBrowser runtime artifact is not configured for live tests.");
        }

        var status = new CloakBrowserRuntimeProvider().GetStatus(runtimeLock, localConfig.CloakBrowserExecutablePath);
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(status.RuntimeArtifactPresent);
        Assert.IsTrue(status.LaunchAllowed, status.Reason);
        Assert.AreEqual("PASS", result.Status);
        Assert.AreEqual(runtimeLock.BinarySha256, result.BinarySha256);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task NoSystemBrowserProof_Passes_WithPinnedArtifact()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsTrue(result.CdpCommandsExecuted);
        Assert.IsFalse(result.CommandsExecuted);
        Assert.IsFalse(result.FilesModified);
        Assert.IsTrue(result.RuntimeShutdown);
        Assert.IsTrue(result.ProcessExited);
        Assert.IsFalse(result.ForcedKillUsed);
        Assert.IsFalse(result.OrphanProcessDetected);
        Assert.IsTrue(result.ProcessStarted);
        Assert.AreEqual("127.0.0.1", result.CdpEndpointHost);
        CollectionAssert.Contains(result.LaunchArgsRedacted.ToList(), "--user-data-dir=<local-verification-profile>");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpDomSnapshot_CapturesPageMetadata_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.DomSnapshotCaptured);
        Assert.IsNotNull(result.DomSnapshotEvidence);
        Assert.AreEqual("NODAL OS CDP Controlled Action Test", result.DomSnapshotEvidence.PageMetadata.Title);
        Assert.AreEqual("cloakbrowser-cdp-direct", result.DomSnapshotEvidence.Source);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpDomSnapshot_DetectsInteractiveElements_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.InteractiveElementCount >= 4);
        Assert.IsTrue(result.ButtonsCount >= 2);
        Assert.IsTrue(result.InputsCount >= 2);
        Assert.IsTrue(result.LinksCount >= 1);
        Assert.IsTrue(result.FormsCount >= 1);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpActionController_CanClickControlledButton_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ControlledClickOk);
        Assert.IsNotNull(result.ControlledActionEvidence);
        Assert.IsTrue(result.ControlledActionEvidence.Any(action =>
            action.ActionKind == CdpControlledActionKind.ClickElementByStableId
            && action.Status == "completed"));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpActionController_CanTypeIntoControlledInput_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ControlledTypeOk);
        Assert.IsNotNull(result.ControlledActionEvidence);
        Assert.IsTrue(result.ControlledActionEvidence.Any(action =>
            action.ActionKind == CdpControlledActionKind.TypeTextByStableId
            && action.Status == "completed"));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpActionController_BlocksExternalNavigation_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.ExternalNavigationAttempted);
        Assert.IsTrue(result.ExternalNavigationBlocked);
        Assert.IsNotNull(result.ControlledActionEvidence);
        Assert.IsTrue(result.ControlledActionEvidence.Any(action =>
            action.ActionKind == CdpControlledActionKind.NavigateExternalUrl
            && action.Status == "blocked"));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CdpEvidenceAdapter_CapturesDomActionEvidence_Live()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.DomActionEvidencePath));
        Assert.IsTrue(File.Exists(result.DomActionEvidencePath));
        Assert.IsTrue(result.SecretsRedacted);
        Assert.IsFalse(result.RawHtmlStored);
        Assert.IsFalse(result.InputValuesStored);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.SystemBrowserUsed);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CloakBrowserRuntimeProvider_ShutdownExitsProcess()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsTrue(result.RuntimeShutdown);
        Assert.IsTrue(result.ProcessExited);
        Assert.IsFalse(result.OrphanProcessDetected);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task CloakBrowserRuntimeProvider_ShutdownIsIdempotent()
    {
        var first = await RequireLiveResultAsync().ConfigureAwait(false);
        var second = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.AreSame(first, second);
        Assert.IsTrue(first.ProcessExited);
        Assert.IsFalse(second.OrphanProcessDetected);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntimeLive")]
    public async Task NoOrphanCloakBrowserProcessProof_Passes()
    {
        var result = await RequireLiveResultAsync().ConfigureAwait(false);

        Assert.IsNotNull(result.ProcessId);
        Assert.IsFalse(IsProcessAlive(result.ProcessId.Value));
    }

    private static async Task<CloakBrowserCdpHealthcheckResult> RequireLiveResultAsync()
    {
        var result = await LiveHealthcheck.Value.ConfigureAwait(false);
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

    private static Task<CloakBrowserCdpHealthcheckResult> RunHealthcheckAsync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var lockPath = Path.Combine(repositoryRoot, "browser-runtime.lock.json");
        return new CloakBrowserRuntimeProvider().RunLiveHealthcheckAsync(
            new CloakBrowserCdpHealthcheckOptions(repositoryRoot, lockPath, Timeout: TimeSpan.FromSeconds(45)));
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

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
