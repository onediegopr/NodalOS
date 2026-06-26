using System.Text.Json;
using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CdpUiStatusRefresh")]
public sealed class CdpUiStatusRefreshTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-26T10:30:00Z");

    [TestMethod]
    public void CdpUiStatusRefresh_IsReadOnly()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T10:20:00Z");
        fixture.WriteSession("2026-06-26T10:22:00Z");

        var result = Refresh(fixture);

        Assert.IsTrue(result.BoundaryReadOnly);
        Assert.IsFalse(result.RuntimeLaunched);
        Assert.IsFalse(result.CdpLiveExecuted);
        Assert.IsFalse(result.ProductFilesModified);
        Assert.AreEqual(CdpUiStatusRefreshStatus.Refreshed, result.Status);
    }

    [TestMethod]
    public void CdpUiStatusRefresh_RefreshesFromLocalEvidence()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T10:20:00Z");
        fixture.WriteSession("2026-06-26T10:22:00Z");

        var result = Refresh(fixture);

        Assert.IsTrue(result.EvidenceRead);
        Assert.AreEqual("listo", result.StatusAfter.Status);
        Assert.AreEqual(CdpUiLiveStatusFreshness.Fresh, result.StatusAfter.Freshness);
        Assert.AreEqual("cloakbrowser-cdp-browser-skills-session-2026-06-26T10-22-00-000Z.redacted.json", result.StatusAfter.LastEvidenceName);
        CollectionAssert.Contains(result.Sources.ToArray(), CdpUiStatusRefreshSource.LocalRedactedEvidence);
    }

    [TestMethod]
    public void CdpUiStatusRefresh_DoesNotLaunchRuntimeFromUi()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T10:20:00Z");
        fixture.WriteSession("2026-06-26T10:22:00Z");
        var directoriesBefore = Directory.GetDirectories(fixture.Root, "*", SearchOption.AllDirectories).Length;

        var result = Refresh(fixture);
        var directoriesAfter = Directory.GetDirectories(fixture.Root, "*", SearchOption.AllDirectories).Length;

        Assert.AreEqual(directoriesBefore, directoriesAfter);
        Assert.IsFalse(result.RuntimeLaunched);
    }

    [TestMethod]
    public void CdpUiStatusRefresh_DoesNotExecuteCdpLive()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T10:20:00Z");
        fixture.WriteSession("2026-06-26T10:22:00Z");

        var result = Refresh(fixture);

        Assert.IsFalse(result.CdpLiveExecuted);
        Assert.IsTrue(result.Sources.All(CdpUiStatusRefreshCommand.IsAllowedSource));
    }

    [TestMethod]
    public void CdpUiStatusRefresh_DoesNotFallbackToExtension()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteArtifact(
            "installed-sidepanel-2026-06-26T10-29-00-000Z.redacted.json",
            """
            { "status": "PASS", "extensionUsed": true, "timestamp": "2026-06-26T10:29:00Z" }
            """);

        var result = Refresh(fixture);

        Assert.AreEqual(CdpUiStatusRefreshStatus.MissingEvidence, result.Status);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.StatusAfter.ExtensionUsed);
        Assert.IsFalse(result.EvidenceRead);
    }

    [TestMethod]
    public void CdpUiStatusRefresh_DoesNotUseSystemBrowser()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T10:20:00Z");
        fixture.WriteSession("2026-06-26T10:22:00Z");

        var result = Refresh(fixture);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.StatusAfter.SystemBrowserUsed);
        Assert.IsFalse(result.StatusAfter.Health.SystemBrowserAllowed);
    }

    [TestMethod]
    public void CdpUiStatusRefresh_CopySummaryIsMetadataOnly()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T10:20:00Z");
        fixture.WriteSession("2026-06-26T10:22:00Z");

        var summary = CdpUiStatusRefreshCommand.BuildCopySummary(Refresh(fixture));

        StringAssert.Contains(summary, "lastRefreshAt:");
        StringAssert.Contains(summary, "refreshSource: local-redacted-evidence");
        StringAssert.Contains(summary, "runtimeLaunched: False");
        StringAssert.Contains(summary, "cdpLiveExecuted: False");
        StringAssert.Contains(summary, "extensionUsed: False");
        StringAssert.Contains(summary, "systemBrowserUsed: False");
        Assert.IsFalse(summary.Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("document.cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains(fixture.Root, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CdpUiStatusRefresh_MissingEvidenceShowsClearState()
    {
        using var fixture = CdpStatusRefreshFixture.Create();

        var result = Refresh(fixture);

        Assert.AreEqual(CdpUiStatusRefreshStatus.MissingEvidence, result.Status);
        Assert.AreEqual("sin captura reciente", result.StatusAfter.Status);
        Assert.AreEqual(CdpUiLiveStatusFreshness.Missing, result.StatusAfter.Freshness);
        Assert.IsFalse(result.EvidenceRead);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void CdpUiStatusRefresh_HandlesCorruptEvidence()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteArtifact("cloakbrowser-cdp-healthcheck-2026-06-26T10-20-00-000Z.redacted.json", "{ not-json");
        fixture.WriteSession("2026-06-26T10:22:00Z");

        var result = Refresh(fixture);

        Assert.AreEqual(CdpUiStatusRefreshStatus.Error, result.Status);
        Assert.AreEqual("EVIDENCE_REFRESH_ERROR", result.Error?.Code);
        Assert.IsFalse(result.EvidenceRead);
        Assert.IsFalse(result.RuntimeLaunched);
    }

    [TestMethod]
    public void CdpUiStatusRefresh_IgnoresNonRedactedArtifacts()
    {
        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteSession("2026-06-26T10:22:00Z");
        fixture.WriteArtifact(
            "cloakbrowser-cdp-browser-skills-session-2026-06-26T10-29-00-000Z.json",
            """
            { "status": "PASS", "secret": "should-not-load", "timestamp": "2026-06-26T10:29:00Z" }
            """);

        var result = Refresh(fixture);
        var serialized = JsonSerializer.Serialize(result);

        Assert.AreEqual("cloakbrowser-cdp-browser-skills-session-2026-06-26T10-22-00-000Z.redacted.json", result.StatusAfter.LastEvidenceName);
        Assert.IsFalse(serialized.Contains("should-not-load", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CdpUiStatusRefresh_BlocksDangerousActions()
    {
        foreach (var source in new[]
        {
            CdpUiStatusRefreshSource.ExtensionFallback,
            CdpUiStatusRefreshSource.SystemBrowserFallback,
            CdpUiStatusRefreshSource.RuntimeLaunchFromUi,
            CdpUiStatusRefreshSource.BridgeWebSocketProtectedChannel
        })
        {
            Assert.IsFalse(CdpUiStatusRefreshCommand.IsAllowedSource(source), source.ToString());
        }

        using var fixture = CdpStatusRefreshFixture.Create();
        fixture.WriteSession("2026-06-26T10:22:00Z");
        var result = Refresh(fixture);

        Assert.IsTrue(result.Sources.All(CdpUiStatusRefreshCommand.IsAllowedSource));
        Assert.IsFalse(result.RuntimeLaunched);
        Assert.IsFalse(result.CdpLiveExecuted);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.SystemBrowserUsed);
    }

    private static CdpUiStatusRefreshResult Refresh(CdpStatusRefreshFixture fixture) =>
        new CdpUiStatusRefreshCommand().Refresh(new CdpUiStatusRefreshRequest(
            fixture.Root,
            fixture.LockfilePath,
            RequestedAt: Now,
            Now: Now));

    private sealed class CdpStatusRefreshFixture : IDisposable
    {
        private CdpStatusRefreshFixture(string root)
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

        public static CdpStatusRefreshFixture Create() =>
            new(Path.Combine(Path.GetTempPath(), $"nodal-cdp-status-refresh-{Guid.NewGuid():N}"));

        public void WriteHealthcheck(string timestamp) =>
            WriteArtifact(
                $"cloakbrowser-cdp-healthcheck-{FileStamp(timestamp)}.redacted.json",
                $$"""
                {
                  "status": "PASS",
                  "decision": "NODAL_OS_CLOAKBROWSER_CDP_UI_LIVE_STATUS_ADAPTER_READY",
                  "runtimeProvider": "cloakbrowser",
                  "source": "cloakbrowser-cdp-direct",
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

        public void WriteSession(string timestamp) =>
            WriteArtifact(
                $"cloakbrowser-cdp-browser-skills-session-{FileStamp(timestamp)}.redacted.json",
                $$"""
                {
                  "status": "PASS",
                  "decision": "NODAL_OS_CLOAKBROWSER_CDP_BROWSER_SKILLS_SESSION_API_UI_BRIDGE_READY",
                  "runtimeProvider": "cloakbrowser",
                  "source": "cloakbrowser-cdp-direct",
                  "captureOk": true,
                  "uiBridgeModelOk": true,
                  "interactiveElements": 6,
                  "frictionSignals": { "Count": 5 },
                  "actionMap": { "EntriesCount": 6 },
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
