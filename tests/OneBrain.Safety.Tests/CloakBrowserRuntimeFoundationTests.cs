using OneBrain.BrowserRuntime;
using System.Text.Json;

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
    public void BrowserRuntimeGuard_AllowsPinnedCloakBrowserArtifactNamedChromeExe()
    {
        var pinnedLock = ValidLock() with
        {
            RuntimeVersion = "146.0.7680.177.5",
            RuntimeCommit = "8432254124667a3d2742b1727132d8a045e115da",
            UpstreamCommit = "0bb3737a29d9133f6207793eb0eeeefe36c9d910",
            BinarySha256 = "03f53661a5c47e7b0a661bee2bce8a0d302b7a60834c328df417561fa0636d80"
        };

        var result = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(
                ExecutablePath: @"C:\DESARROLLO\NodalOS\nodal-cloakbrowser-runtime\.cloakbrowser\chromium-146.0.7680.177.5\chrome.exe"),
            pinnedLock);

        Assert.IsTrue(result.IsAllowed, result.Reason);
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
    public void CdpConnection_EndpointDiscoveryRejectsExternalHost()
    {
        var error = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CdpEndpointDiscovery.ValidateWebSocketDebuggerUrl("ws://example.com/devtools/browser/1"));

        StringAssert.Contains(error.Message, "127.0.0.1");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpConnection_EndpointDiscoveryTimesOutClearly()
    {
        var message = CdpEndpointDiscovery.CreateTimeoutMessage(
            new Uri("http://127.0.0.1:65530/json/version"),
            new InvalidOperationException("connection refused"));

        StringAssert.Contains(message, "127.0.0.1");
        StringAssert.Contains(message, "connection refused");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpConnection_CommandTimeoutIsClear()
    {
        var lifecycle = new CdpConnectionLifecycle();
        var error = lifecycle.CreateCommandTimeout("Runtime.evaluate", TimeSpan.FromSeconds(2));

        StringAssert.Contains(error.Message, "Runtime.evaluate");
        StringAssert.Contains(error.Message, "2");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpConnection_DisposeIsIdempotent()
    {
        var lifecycle = new CdpConnectionLifecycle();

        lifecycle.Dispose();
        lifecycle.Dispose();
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpConnection_RejectsCommandAfterDispose()
    {
        var lifecycle = new CdpConnectionLifecycle();
        lifecycle.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => lifecycle.NextCommandId());
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpSessionRegistry_TracksTargetSessionLifecycle()
    {
        var registry = new CdpSessionRegistry();
        var session = registry.Register("target-1", "session-1", DateTimeOffset.UtcNow);
        registry.MarkState(session.SessionId, "navigated");
        var closed = registry.MarkClosed(session.SessionId, DateTimeOffset.UtcNow);

        Assert.AreEqual("target-1", session.TargetId);
        Assert.AreEqual("closed", closed.State);
        Assert.IsNotNull(closed.ClosedAt);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpSessionRegistry_DoesNotDuplicateSessions()
    {
        var registry = new CdpSessionRegistry();
        var first = registry.Register("target-1", DateTimeOffset.UtcNow);
        var second = registry.Register("target-1", DateTimeOffset.UtcNow);

        Assert.AreEqual(first.SessionId, second.SessionId);
        Assert.AreEqual(1, registry.Snapshot().Count);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpSessionRegistry_CleansClosedSession()
    {
        var registry = new CdpSessionRegistry();
        var session = registry.Register("target-1", "session-1", DateTimeOffset.UtcNow);
        registry.MarkClosed(session.SessionId, DateTimeOffset.UtcNow);

        Assert.AreEqual("closed", registry.Find(session.SessionId)?.State);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpTargetManager_CanCreateNavigateAndClosePage()
    {
        var targets = new CdpTargetManager();
        targets.TrackPageTarget("target-1", "about:blank", string.Empty);
        targets.MarkNavigated("target-1", "data:text/html,ok", "OK");
        var closed = targets.Close("target-1", DateTimeOffset.UtcNow);

        Assert.AreEqual("closed", closed.State);
        Assert.IsNotNull(closed.ClosedAt);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpTargetManager_CloseIsIdempotent()
    {
        var targets = new CdpTargetManager();
        var first = targets.Close("target-1", DateTimeOffset.UtcNow);
        var second = targets.Close("target-1", DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.AreEqual(first.ClosedAt, second.ClosedAt);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpTargetManager_RejectsExternalNavigationInHealthcheck()
    {
        var controller = new CdpActionController();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            controller.CreateHealthcheckNavigation(new Uri("https://example.com")));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpInjectionManager_RequiresReadySession()
    {
        var manager = new CdpInjectionManager();

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.EnsureReadySession(null));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpInjectionManager_ReturnsPageMetadata()
    {
        var manager = new CdpInjectionManager();

        StringAssert.Contains(manager.BuildBootstrapScript(), "__NODAL_OS_CDP_PAGE_METADATA__");
        StringAssert.Contains(manager.BuildPageMetadataExpression(), "__NODAL_OS_CDP_PAGE_METADATA__");
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

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpEvidenceAdapter_RecordsLifecycleWithoutSecrets()
    {
        var result = new CloakBrowserCdpHealthcheckResult(
            Status: "PASS",
            Decision: "NODAL_OS_CLOAKBROWSER_CDP_SESSION_LIFECYCLE_HARDENED",
            Reason: "ok",
            RuntimeProvider: "cloakbrowser",
            CdpMode: "cdp-direct",
            RuntimeVersion: "146.0.7680.177.5",
            BinarySha256: "hash",
            BrowserVersion: "Chrome/146.0.7680.177",
            ProtocolVersion: "1.3",
            TargetId: "target-1",
            Url: "data:text/html,ok",
            Title: "OK",
            TargetCreated: true,
            TargetClosed: true,
            SessionCreated: true,
            SessionClosed: true,
            NavigationOk: true,
            TitleRead: true,
            BootstrapInjected: true,
            DoubleInjectionPrevented: true,
            ScreenshotCaptured: true,
            ProcessStarted: true,
            LaunchArgsRedacted: ["--user-data-dir=<local-verification-profile>"],
            LaunchTimeout: false,
            CdpEndpointHost: "127.0.0.1",
            RuntimeShutdown: true,
            ProcessExited: true,
            ForcedKillUsed: false,
            OrphanProcessDetected: false,
            SystemBrowserUsed: false,
            ExtensionUsed: false,
            CommandsExecuted: false,
            CdpCommandsExecuted: true,
            FilesModified: false,
            EvidencePath: null,
            ScreenshotPath: null,
            ProcessId: 123);

        var evidence = new CdpEvidenceAdapter().CreateLifecycleEvidence(result);

        Assert.IsTrue(evidence.TargetCreated);
        Assert.IsTrue(evidence.TargetClosed);
        Assert.IsTrue(evidence.SessionCreated);
        Assert.IsTrue(evidence.SessionClosed);
        Assert.IsFalse(evidence.StoresRawLogs);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpEvidenceAdapter_MarksExtensionAndSystemBrowserFalse()
    {
        var evidence = new CdpEvidenceAdapter().CreateBlockedEvidence();

        Assert.IsFalse(evidence.ExtensionUsed);
        Assert.IsFalse(evidence.SystemBrowserUsed);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpEvidenceAdapter_DoesNotStoreRawLogs()
    {
        var evidence = new CdpEvidenceAdapter().CreateBlockedEvidence();

        Assert.IsFalse(evidence.StoresRawLogs);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpDomSnapshot_CapturesPageMetadata()
    {
        var snapshot = ParseSnapshot(SampleSnapshotJson());

        Assert.AreEqual("data:text/html,controlled", snapshot.PageMetadata.Url);
        Assert.AreEqual("NODAL OS CDP Controlled Action Test", snapshot.PageMetadata.Title);
        Assert.AreEqual("complete", snapshot.PageMetadata.ReadyState);
        Assert.AreEqual("cloakbrowser-cdp-direct", snapshot.Source);
        Assert.IsFalse(snapshot.ExtensionUsed);
        Assert.IsFalse(snapshot.SystemBrowserUsed);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpDomSnapshot_LimitsTextPreview()
    {
        var manager = new CdpDomSnapshotManager();
        var script = manager.BuildDomSnapshotExpression(maxTextPreviewCharacters: 64);

        StringAssert.Contains(script, "slice(0, limit)");
        StringAssert.Contains(script, "maxTextPreviewCharacters = 64");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpDomSnapshot_DoesNotStoreInputValues()
    {
        var snapshot = ParseSnapshot(SampleSnapshotJson());

        Assert.IsFalse(snapshot.StoresInputValues);
        Assert.IsTrue(snapshot.SecretsRedacted);
        Assert.IsFalse(snapshot.InteractiveElements.Any(element => element.Label.Contains("never-store-this", StringComparison.Ordinal)));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpDomSnapshot_DoesNotStoreRawHtml()
    {
        var manager = new CdpDomSnapshotManager();
        var script = manager.BuildDomSnapshotExpression();
        var snapshot = ParseSnapshot(SampleSnapshotJson());

        Assert.IsFalse(manager.ContainsForbiddenRawDataReads(script));
        Assert.IsFalse(snapshot.StoresRawHtml);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpDomSnapshot_DetectsButtonsInputsLinks()
    {
        var snapshot = ParseSnapshot(SampleSnapshotJson());

        Assert.AreEqual(1, snapshot.ButtonsCount);
        Assert.AreEqual(2, snapshot.InputsCount);
        Assert.AreEqual(1, snapshot.LinksCount);
        Assert.IsTrue(snapshot.InteractiveElements.Any(element => element.Tag == "button"));
        Assert.IsTrue(snapshot.InteractiveElements.Any(element => element.Tag == "input"));
        Assert.IsTrue(snapshot.InteractiveElements.Any(element => element.Tag == "a"));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpDomSnapshot_RedactsPasswordInputs()
    {
        var snapshot = ParseSnapshot(SampleSnapshotJson());
        var sensitiveInput = snapshot.InteractiveElements.Single(element => element.InputType == "password");

        Assert.AreEqual("[redacted input]", sensitiveInput.Label);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpDomSnapshot_LimitsInteractiveElementCount()
    {
        var script = new CdpDomSnapshotManager().BuildDomSnapshotExpression(maxInteractiveElements: 7);

        StringAssert.Contains(script, "maxInteractiveElements = 7");
        StringAssert.Contains(script, ".slice(0, maxInteractiveElements)");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpActionController_CanClickControlledButton()
    {
        var controller = new CdpActionController();
        var request = controller.CreateClickElementByStableId("cdp-el-1");
        var script = controller.BuildControlledActionExpression(request);

        Assert.AreEqual(CdpControlledActionKind.ClickElementByStableId, request.ActionKind);
        StringAssert.Contains(script, "element.click()");
        StringAssert.Contains(script, "Controlled click completed.");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpActionController_CanTypeIntoControlledInput()
    {
        var controller = new CdpActionController();
        var request = controller.CreateTypeTextByStableId("cdp-el-2", "NODAL OS controlled text");
        var script = controller.BuildControlledActionExpression(request);

        Assert.AreEqual(CdpControlledActionKind.TypeTextByStableId, request.ActionKind);
        StringAssert.Contains(script, "element.value = text");
        StringAssert.Contains(script, "Controlled text entered.");
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpActionController_RejectsExternalNavigation()
    {
        var controller = new CdpActionController();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            controller.CreateExternalNavigation(new Uri("https://example.invalid/blocked")));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpActionController_RejectsFormSubmit()
    {
        var controller = new CdpActionController();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            controller.CreateForbiddenFormSubmit("cdp-el-4"));
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpActionController_RejectsUnknownStableId()
    {
        var controller = new CdpActionController();
        var request = controller.CreateClickElementByStableId("cdp-el-99");
        var result = controller.RejectUnknownStableId(request, ParseSnapshot(SampleSnapshotJson()));

        Assert.AreEqual("blocked", result.Status);
        Assert.AreEqual("Unknown stable id.", result.Reason);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpActionController_DoesNotUseExtension()
    {
        var controller = new CdpActionController();
        var evidence = controller.CreateEvidence(
            CdpControlledActionKind.ClickElementByStableId,
            "cdp-el-1",
            "completed",
            "ok");

        Assert.IsFalse(evidence.ExtensionUsed);
        Assert.IsFalse(evidence.SystemBrowserUsed);
        Assert.IsFalse(evidence.ProductCommandsExecuted);
        Assert.IsFalse(evidence.FilesModified);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpEvidenceAdapter_RecordsDomSnapshotWithoutRawHtml()
    {
        var snapshot = ParseSnapshot(SampleSnapshotJson());
        var evidence = new CdpEvidenceAdapter().CreateDomActionEvidence(
            snapshot,
            [],
            screenshotCaptured: false,
            externalNavigationAttempted: false,
            externalNavigationBlocked: false);

        Assert.IsFalse(evidence.StoresRawHtml);
        Assert.IsFalse(evidence.StoresInputValues);
        Assert.IsTrue(evidence.SecretsRedacted);
        Assert.IsNotNull(evidence.DomSnapshot);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpEvidenceAdapter_RecordsControlledActions()
    {
        var snapshot = ParseSnapshot(SampleSnapshotJson());
        var actionEvidence = new CdpActionController().CreateEvidence(
            CdpControlledActionKind.ClickElementByStableId,
            "cdp-el-1",
            "completed",
            "ok");
        var evidence = new CdpEvidenceAdapter().CreateDomActionEvidence(
            snapshot,
            [actionEvidence],
            screenshotCaptured: true,
            externalNavigationAttempted: true,
            externalNavigationBlocked: true);

        Assert.IsTrue(evidence.ScreenshotCaptured);
        Assert.IsTrue(evidence.ExternalNavigationAttempted);
        Assert.IsTrue(evidence.ExternalNavigationBlocked);
        Assert.AreEqual(1, evidence.ActionResults?.Count);
    }

    [TestMethod]
    [TestCategory("CloakBrowserRuntime")]
    public void CdpEvidenceAdapter_RedactsInputsAndSecrets()
    {
        var snapshot = ParseSnapshot(SampleSnapshotJson());
        var evidence = new CdpEvidenceAdapter().CreateDomActionEvidence(
            snapshot,
            [],
            screenshotCaptured: false,
            externalNavigationAttempted: false,
            externalNavigationBlocked: false);

        Assert.IsTrue(evidence.SecretsRedacted);
        Assert.IsFalse(evidence.StoresInputValues);
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

    private static CdpDomSnapshot ParseSnapshot(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new CdpDomSnapshotManager().ParseRuntimeEvaluateResult(document.RootElement.Clone());
    }

    private static string SampleSnapshotJson() =>
        """
        {
          "pageMetadata": {
            "url": "data:text/html,controlled",
            "title": "NODAL OS CDP Controlled Action Test",
            "readyState": "complete"
          },
          "nodeCount": 12,
          "textPreview": "Controlled local data URL.",
          "interactiveElements": [
            {
              "stableId": "cdp-el-1",
              "tag": "button",
              "role": "button",
              "label": "Record controlled click",
              "inputType": null,
              "href": null,
              "enabled": true,
              "visible": true,
              "selectorHint": "[data-nodal-cdp-stable-id=\"cdp-el-1\"]"
            },
            {
              "stableId": "cdp-el-2",
              "tag": "input",
              "role": "input",
              "label": "safe text",
              "inputType": "text",
              "href": null,
              "enabled": true,
              "visible": true,
              "selectorHint": "[data-nodal-cdp-stable-id=\"cdp-el-2\"]"
            },
            {
              "stableId": "cdp-el-3",
              "tag": "input",
              "role": "input",
              "label": "[redacted input]",
              "inputType": "password",
              "href": null,
              "enabled": true,
              "visible": true,
              "selectorHint": "[data-nodal-cdp-stable-id=\"cdp-el-3\"]"
            },
            {
              "stableId": "cdp-el-4",
              "tag": "a",
              "role": "link",
              "label": "External link blocked by controller",
              "inputType": null,
              "href": "external-url-redacted",
              "enabled": true,
              "visible": true,
              "selectorHint": "[data-nodal-cdp-stable-id=\"cdp-el-4\"]"
            }
          ],
          "formsCount": 1,
          "linksCount": 1,
          "buttonsCount": 1,
          "inputsCount": 2,
          "screenshotsAvailable": true,
          "source": "cloakbrowser-cdp-direct",
          "extensionUsed": false,
          "systemBrowserUsed": false
        }
        """;
}
