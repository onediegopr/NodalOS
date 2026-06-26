using System.Text.Json;
using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CdpUiLiveStatusAdapter")]
public sealed class CdpUiLiveStatusAdapterTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-26T09:30:00Z");

    [TestMethod]
    public void CdpUiLiveStatusAdapter_ReadsLatestRedactedEvidence()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteBrowserSkills("2026-06-26T09:21:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.AreEqual("listo", result.Snapshot.Status);
        Assert.AreEqual(CdpUiLiveStatusFreshness.Fresh, result.Snapshot.Freshness);
        Assert.AreEqual("cloakbrowser", result.RuntimeProvider);
        Assert.AreEqual("cloakbrowser-cdp-direct", result.Source);
        Assert.IsTrue(result.Snapshot.EvidenceAvailable);
        Assert.AreEqual("cloakbrowser-cdp-browser-skills-session-2026-06-26T09-22-00-000Z.redacted.json", result.Snapshot.LastEvidenceName);
        Assert.AreEqual(6, result.Snapshot.BrowserSkillsSession?.InteractiveElementCount);
        Assert.AreEqual(5, result.Snapshot.BrowserSkillsSession?.FrictionSignalCount);
        Assert.AreEqual(6, result.Snapshot.BrowserSkillsSession?.ActionMapCount);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_HandlesMissingEvidenceClearly()
    {
        using var fixture = CdpLiveStatusFixture.Create();

        var result = Build(fixture);

        Assert.AreEqual("sin captura reciente", result.Snapshot.Status);
        Assert.AreEqual(CdpUiLiveStatusFreshness.Missing, result.Snapshot.Freshness);
        Assert.IsFalse(result.Snapshot.EvidenceAvailable);
        Assert.AreEqual("sin-evidencia-cdp", result.Snapshot.LastEvidenceName);
        Assert.AreEqual(CdpUiLiveStatusFreshness.Missing, result.Snapshot.Healthcheck?.Freshness);
        Assert.AreEqual(CdpUiLiveStatusFreshness.Missing, result.Snapshot.BrowserSkillsSession?.Freshness);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_IgnoresNonRedactedArtifacts()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteSession("2026-06-26T09:22:00Z");
        fixture.WriteArtifact(
            "cloakbrowser-cdp-browser-skills-session-2026-06-26T09-29-00-000Z.json",
            """
            { "status": "PASS", "timestamp": "2026-06-26T09:29:00Z", "secret": "should-not-load" }
            """);

        var result = Build(fixture);
        var serialized = JsonSerializer.Serialize(result);

        Assert.AreEqual("cloakbrowser-cdp-browser-skills-session-2026-06-26T09-22-00-000Z.redacted.json", result.Snapshot.BrowserSkillsSession?.LastEvidenceName);
        Assert.IsFalse(serialized.Contains("should-not-load", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_DoesNotExposeRawPathsOrSecrets()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);
        var serialized = JsonSerializer.Serialize(result);

        Assert.IsFalse(serialized.Contains(fixture.Root, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains(@"C:\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("document.cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("<html", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_FreshEvidenceIsFresh()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.AreEqual(CdpUiLiveStatusFreshness.Fresh, result.Snapshot.Freshness);
        Assert.AreEqual("listo", result.Snapshot.Status);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_OldEvidenceIsStale()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-25T09:20:00Z");
        fixture.WriteSession("2026-06-25T09:22:00Z");

        var result = Build(fixture, TimeSpan.FromHours(1));

        Assert.AreEqual(CdpUiLiveStatusFreshness.Stale, result.Snapshot.Freshness);
        Assert.AreEqual("revisar", result.Snapshot.Status);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_MissingEvidenceIsClear()
    {
        using var fixture = CdpLiveStatusFixture.Create();

        var result = Build(fixture);

        Assert.AreEqual(CdpUiLiveStatusFreshness.Missing, result.Snapshot.Freshness);
        Assert.AreEqual("sin captura reciente", result.Snapshot.Status);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_CorruptEvidenceIsError()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteArtifact("cloakbrowser-cdp-healthcheck-2026-06-26T09-20-00-000Z.redacted.json", "{ not-json");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.AreEqual(CdpUiLiveStatusFreshness.Error, result.Snapshot.Freshness);
        Assert.AreEqual("revisar verificación CDP", result.Snapshot.Status);
        Assert.AreEqual("EVIDENCE_READ_ERROR", result.Snapshot.Healthcheck?.ErrorCode);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_MergesRuntimeBoundaryStatus()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.IsTrue(result.Snapshot.Health.RuntimeConfigured);
        Assert.IsTrue(result.Snapshot.Health.ArtifactPinned);
        Assert.AreEqual("verificado", result.Snapshot.HashVerifiedStatus);
        Assert.IsTrue(result.Snapshot.BoundaryReadOnly);
        Assert.IsTrue(result.Snapshot.ExternalNavigationBlocked);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_MarksExtensionLegacyNoDefault()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.AreEqual("legacy/no-default", result.Snapshot.Health.ExtensionMode);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.Snapshot.ExtensionUsed);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_MarksSystemBrowserFalse()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.Snapshot.SystemBrowserUsed);
        Assert.IsFalse(result.Snapshot.Health.SystemBrowserAllowed);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_BuildsProductSurfaceStatus()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.AreEqual("listo", result.Snapshot.Status);
        Assert.AreEqual("verificado", result.Snapshot.HashVerifiedStatus);
        Assert.AreEqual("cloakbrowser-cdp-direct", result.Snapshot.Source);
        Assert.IsTrue(result.Snapshot.RuntimeShutdown);
        Assert.IsTrue(result.Snapshot.ProcessExited);
        Assert.IsFalse(result.Snapshot.OrphanProcessDetected);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_CopySummaryIsMetadataOnly()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);
        var summary = string.Join('\n',
            $"status: {result.Snapshot.Status}",
            $"freshness: {result.Snapshot.Freshness}",
            $"lastHealthcheckAt: {result.Snapshot.LastHealthcheckAt:O}",
            $"lastSessionAt: {result.Snapshot.LastBrowserSkillsSessionAt:O}",
            $"evidenceAvailable: {result.Snapshot.EvidenceAvailable}",
            $"runtimeShutdown: {result.Snapshot.RuntimeShutdown}",
            $"processExited: {result.Snapshot.ProcessExited}",
            $"orphanProcessDetected: {result.Snapshot.OrphanProcessDetected}",
            $"extensionUsed: {result.ExtensionUsed}",
            $"systemBrowserUsed: {result.SystemBrowserUsed}",
            $"boundaryReadOnly: {result.BoundaryReadOnly}",
            $"externalNavigationBlocked: {result.ExternalNavigationBlocked}",
            $"productFilesModified: {result.ProductFilesModified}");

        StringAssert.Contains(summary, "boundaryReadOnly: True");
        Assert.IsFalse(summary.Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("document.cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_DoesNotLaunchRuntimeFromStatusRefresh()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var before = Directory.GetDirectories(fixture.Root).Length;
        var result = Build(fixture);
        var after = Directory.GetDirectories(fixture.Root).Length;

        Assert.AreEqual(before, after);
        Assert.AreEqual("listo", result.Snapshot.Status);
        Assert.IsTrue(result.ReadOnly);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_DoesNotFallbackToExtension()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteArtifact(
            "installed-sidepanel-2026-06-26T09-29-00-000Z.redacted.json",
            """
            { "status": "PASS", "extensionUsed": true, "timestamp": "2026-06-26T09:29:00Z" }
            """);

        var result = Build(fixture);

        Assert.AreEqual(CdpUiLiveStatusFreshness.Missing, result.Snapshot.Freshness);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.Snapshot.EvidenceAvailable);
    }

    [TestMethod]
    public void CdpUiLiveStatusAdapter_DoesNotUseSystemBrowser()
    {
        using var fixture = CdpLiveStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T09:20:00Z");
        fixture.WriteSession("2026-06-26T09:22:00Z");

        var result = Build(fixture);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.Snapshot.SystemBrowserUsed);
    }

    private static CdpUiLiveStatusResult Build(CdpLiveStatusFixture fixture, TimeSpan? freshWithin = null) =>
        new CdpUiLiveStatusAdapter().Build(new CdpUiLiveStatusRequest(
            fixture.Root,
            fixture.LockfilePath,
            Now,
            freshWithin));

    private sealed class CdpLiveStatusFixture : IDisposable
    {
        private CdpLiveStatusFixture(string root)
        {
            Root = root;
            ArtifactsRoot = Path.Combine(root, "artifacts", "local-verification");
            Directory.CreateDirectory(ArtifactsRoot);
            LockfilePath = Path.Combine(root, "browser-runtime.lock.json");
            File.Copy(Path.Combine(RepoRoot(), "browser-runtime.lock.json"), LockfilePath);
        }

        public string Root { get; }

        public string ArtifactsRoot { get; }

        public string LockfilePath { get; }

        public static CdpLiveStatusFixture Create() =>
            new(Path.Combine(Path.GetTempPath(), $"nodal-cdp-live-status-{Guid.NewGuid():N}"));

        public void WriteHealthcheck(string timestamp) =>
            WriteArtifact(
                $"cloakbrowser-cdp-healthcheck-{FileStamp(timestamp)}.redacted.json",
                $$"""
                {
                  "status": "PASS",
                  "decision": "NODAL_OS_CLOAKBROWSER_CDP_DOM_SNAPSHOT_ACTION_CONTROLLER_READY",
                  "runtimeProvider": "cloakbrowser",
                  "source": "cloakbrowser-cdp-direct",
                  "binarySha256": "03f53661a5c47e7b0a661bee2bce8a0d302b7a60834c328df417561fa0636d80",
                  "screenshotCaptured": true,
                  "runtimeShutdown": true,
                  "processExited": true,
                  "orphanProcessDetected": false,
                  "extensionUsed": false,
                  "systemBrowserUsed": false,
                  "externalNavigationBlocked": true,
                  "productFilesModified": false,
                  "timestamp": "{{timestamp}}"
                }
                """);

        public void WriteBrowserSkills(string timestamp) =>
            WriteArtifact(
                $"cloakbrowser-cdp-browser-skills-{FileStamp(timestamp)}.redacted.json",
                $$"""
                {
                  "status": "PASS",
                  "decision": "NODAL_OS_CLOAKBROWSER_CDP_BROWSER_SKILLS_CORE_PARITY_READY",
                  "runtimeProvider": "cloakbrowser",
                  "source": "cloakbrowser-cdp-direct",
                  "captureOk": true,
                  "interactiveElements": 6,
                  "frictionSignals": 5,
                  "actionMapEntries": 6,
                  "screenshotCaptured": true,
                  "extensionUsed": false,
                  "systemBrowserUsed": false,
                  "externalNavigationBlocked": true,
                  "productFilesModified": false,
                  "timestamp": "{{timestamp}}"
                }
                """);

        public void WriteSession(string timestamp) =>
            WriteArtifact(
                $"cloakbrowser-cdp-browser-skills-session-{FileStamp(timestamp)}.redacted.json",
                $$"""
                {
                  "status": "PASS",
                  "decision": "NODAL_OS_CLOAKBROWSER_CDP_BROWSER_SKILLS_SESSION_API_UI_BRIDGE_READY",
                  "runtimeProvider": "cloakbrowser",
                  "source": "cloakbrowser-cdp-direct",
                  "sessionCreated": true,
                  "captureOk": true,
                  "uiBridgeModelOk": true,
                  "domIndexOk": true,
                  "frictionSignals": { "Count": 5 },
                  "actionMap": { "EntriesCount": 6 },
                  "interactiveElements": 6,
                  "screenshotCaptured": true,
                  "externalNavigationBlocked": true,
                  "extensionUsed": false,
                  "systemBrowserUsed": false,
                  "processExited": true,
                  "runtimeShutdown": true,
                  "orphanProcessDetected": false,
                  "productFilesModified": false,
                  "timestamp": "{{timestamp}}"
                }
                """);

        public void WriteArtifact(string fileName, string json)
        {
            Directory.CreateDirectory(ArtifactsRoot);
            var path = Path.Combine(ArtifactsRoot, fileName);
            File.WriteAllText(path, json);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(Random.Shared.Next(0, 10)));
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static string FileStamp(string timestamp) =>
            DateTimeOffset.Parse(timestamp).UtcDateTime.ToString("yyyy-MM-ddTHH-mm-ss-fffZ");
    }

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
