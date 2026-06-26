using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class CdpBrowserSkillsSessionTests
{
    private static readonly Lazy<Task<SessionFixture>> CapturedSession = new(
        RunCapturedSessionAsync,
        LazyThreadSafetyMode.ExecutionAndPublication);

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_CanCreateCaptureAndClose_Live()
    {
        var repositoryRoot = FindRepositoryRoot();
        var service = new CdpBrowserSkillsSessionService();
        var created = await service.CreateSessionAsync(CreateRequest(repositoryRoot)).ConfigureAwait(false);

        Assert.AreEqual("PASS", created.Status);
        Assert.IsTrue(created.SessionCreated);
        Assert.AreEqual(CdpBrowserSkillsSessionState.Created, created.Snapshot.Status);

        var captured = await service.CaptureControlledPageAsync(created.Snapshot.SessionId).ConfigureAwait(false);
        RequireSessionPass(captured);
        Assert.IsTrue(captured.CaptureOk);
        Assert.AreEqual(CdpBrowserSkillsSessionState.EvidenceReady, captured.Snapshot.Status);

        var closed = await service.CloseSessionAsync(created.Snapshot.SessionId).ConfigureAwait(false);
        Assert.AreEqual(CdpBrowserSkillsSessionState.Closed, closed.Snapshot.Status);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_BuildsUiBridgeModel_Live()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);
        var model = await fixture.Service.BuildUiBridgeModelAsync(fixture.Result.Snapshot.SessionId).ConfigureAwait(false);

        Assert.IsTrue(model.ReadOnly);
        Assert.AreEqual("CloakBrowser CDP", model.Summary.RuntimeLabel);
        Assert.AreEqual("NODAL OS Browser Skills CDP Parity", model.Summary.Title);
        Assert.IsTrue(model.Summary.ElementCount >= 6);
        Assert.IsTrue(model.Summary.FrictionCount >= 5);
        Assert.IsTrue(model.Summary.ActionMapCount >= 6);
        Assert.IsTrue(model.Summary.ScreenshotCaptured);
        Assert.IsTrue(model.Summary.EvidenceAvailable);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_EvidenceIsMetadataOnly()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);
        var snapshot = fixture.Result.Snapshot;

        Assert.IsTrue(snapshot.SecretsRedacted);
        Assert.IsFalse(snapshot.RawHtmlStored);
        Assert.IsFalse(snapshot.InputValuesStored);
        Assert.IsFalse(snapshot.ProductFilesModified);
        Assert.IsTrue(snapshot.EvidenceRefs.Count >= 2);
        Assert.IsTrue(snapshot.EvidenceRefs.All(reference => reference.Redacted && reference.LocalOnly));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_GetLatestCaptureReturnsMetadataOnly()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);
        var latest = await fixture.Service.GetLatestCaptureAsync(fixture.Result.Snapshot.SessionId).ConfigureAwait(false);

        Assert.IsNotNull(latest);
        Assert.IsTrue(latest.DomIndexOk);
        Assert.IsFalse(latest.Evidence.StoresRawHtml);
        Assert.IsFalse(latest.Evidence.StoresInputValues);
        Assert.IsTrue(latest.Evidence.SecretsRedacted);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_CloseIsIdempotent()
    {
        var repositoryRoot = FindRepositoryRoot();
        var service = new CdpBrowserSkillsSessionService();
        var created = await service.CreateSessionAsync(CreateRequest(repositoryRoot)).ConfigureAwait(false);

        var first = await service.CloseSessionAsync(created.Snapshot.SessionId).ConfigureAwait(false);
        var second = await service.CloseSessionAsync(created.Snapshot.SessionId).ConfigureAwait(false);

        Assert.AreEqual(CdpBrowserSkillsSessionState.Closed, first.Snapshot.Status);
        Assert.AreEqual(CdpBrowserSkillsSessionState.Closed, second.Snapshot.Status);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_DoesNotFallbackToExtension()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);

        Assert.IsFalse(fixture.Result.ExtensionUsed);
        Assert.IsFalse(fixture.Result.Snapshot.ExtensionUsed);
        Assert.IsFalse(fixture.Result.UiBridgeModel?.ExtensionUsed ?? true);
        Assert.AreEqual("cloakbrowser-cdp-direct", fixture.Result.Snapshot.Source);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_DoesNotUseSystemBrowser()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);

        Assert.IsFalse(fixture.Result.SystemBrowserUsed);
        Assert.IsFalse(fixture.Result.Snapshot.SystemBrowserUsed);
        Assert.IsFalse(fixture.Result.UiBridgeModel?.SystemBrowserUsed ?? true);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_ExternalNavigationBlocked()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);

        Assert.IsTrue(fixture.Result.ExternalNavigationBlocked);
        Assert.IsTrue(fixture.Result.Snapshot.ExternalNavigationBlocked);
        Assert.IsTrue(fixture.Result.UiBridgeModel?.ExternalNavigationBlocked ?? false);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_ShutdownHasNoOrphan()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);

        Assert.IsTrue(fixture.Result.RuntimeShutdown);
        Assert.IsTrue(fixture.Result.ProcessExited);
        Assert.IsFalse(fixture.Result.OrphanProcessDetected);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsUiBridge_ContainsNoRawDomOrSecrets()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);
        var model = fixture.Result.UiBridgeModel;

        Assert.IsNotNull(model);
        Assert.IsFalse(model.ContainsRawDom);
        Assert.IsFalse(model.ContainsSecrets);
        Assert.IsTrue(model.Summary.Flags.Contains("Solo lectura"));
        Assert.IsTrue(model.Summary.Flags.Contains("Sin extensión"));
        Assert.IsTrue(model.Summary.Flags.Contains("Sin navegador del sistema"));
        Assert.IsTrue(model.Summary.Flags.Contains("Sin navegación externa"));
        Assert.IsTrue(model.Summary.Flags.Contains("No se modificaron archivos"));
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public async Task CdpBrowserSkillsSession_SourceIsCloakBrowserCdpDirect()
    {
        var fixture = await RequireCapturedSessionAsync().ConfigureAwait(false);

        Assert.AreEqual("cloakbrowser", fixture.Result.Snapshot.RuntimeProvider);
        Assert.AreEqual("cloakbrowser-cdp-direct", fixture.Result.Snapshot.Source);
        Assert.AreEqual("cloakbrowser-cdp-direct", fixture.Result.LatestCapture?.PageSummary.Source);
    }

    [TestMethod]
    [TestCategory("CdpBrowserSkillsSession")]
    public void ChromeExtension_RemainsLegacyNoDefaultRuntimePath()
    {
        var runtimeLock = BrowserRuntimeLock.Load(Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json"));

        Assert.AreEqual("cloakbrowser", RuntimeText(BrowserRuntimeDefaults.DefaultRuntime));
        Assert.IsFalse(RuntimeFlag(BrowserRuntimeDefaults.ExtensionDefaultRuntime));
        Assert.IsTrue(RuntimeFlag(BrowserRuntimeDefaults.ExtensionLegacyUi));
        Assert.IsTrue(RuntimeFlag(BrowserRuntimeDefaults.CdpDirectRuntimeDefault));
        Assert.IsFalse(runtimeLock.ExtensionEnabled);
    }

    private static async Task<SessionFixture> RequireCapturedSessionAsync()
    {
        var fixture = await CapturedSession.Value.ConfigureAwait(false);
        RequireSessionPass(fixture.Result);
        return fixture;
    }

    private static void RequireSessionPass(CdpBrowserSkillsSessionResult result)
    {
        if (result.Status == "BLOCKED"
            && result.Reason.Contains("artifact", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive(result.Reason);
        }

        if (result.Status != "PASS")
        {
            Assert.Fail(result.Reason);
        }
    }

    private static async Task<SessionFixture> RunCapturedSessionAsync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var service = new CdpBrowserSkillsSessionService();
        var created = await service.CreateSessionAsync(CreateRequest(repositoryRoot)).ConfigureAwait(false);
        var captured = await service.CaptureControlledPageAsync(created.Snapshot.SessionId).ConfigureAwait(false);
        return new SessionFixture(service, captured);
    }

    private static CdpBrowserSkillsSessionRequest CreateRequest(string repositoryRoot) =>
        new(
            repositoryRoot,
            Path.Combine(repositoryRoot, "browser-runtime.lock.json"),
            Timeout: TimeSpan.FromSeconds(45));

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

    private sealed record SessionFixture(
        CdpBrowserSkillsSessionService Service,
        CdpBrowserSkillsSessionResult Result);
}
