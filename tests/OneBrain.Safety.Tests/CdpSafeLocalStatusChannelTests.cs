using System.Text.Json;
using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CdpSafeLocalStatusChannel")]
public sealed class CdpSafeLocalStatusChannelTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-26T11:30:00Z");

    [TestMethod]
    public void CdpSafeLocalStatusChannel_WritesMetadataOnlySnapshot()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T11:20:00Z");
        fixture.WriteSession("2026-06-26T11:22:00Z");

        var result = Export(fixture);
        var read = new CdpSafeLocalStatusSnapshotReader().ReadSnapshot(fixture.SnapshotPath);

        Assert.AreEqual(CdpSafeLocalStatusChannelStatus.Ready, result.Status);
        Assert.IsTrue(result.SnapshotWritten);
        Assert.AreEqual(CdpSafeLocalStatusSnapshotWriter.FileName, result.SnapshotName);
        Assert.AreEqual(CdpSafeLocalStatusSnapshotWriter.SchemaVersion, read.Snapshot?.SchemaVersion);
        Assert.AreEqual(CdpSafeLocalStatusSnapshotWriter.Channel, read.Snapshot?.Channel);
        Assert.AreEqual("cloakbrowser-cdp-direct", read.Snapshot?.Source);
        Assert.AreEqual("cloakbrowser", read.Snapshot?.RuntimeProvider);
        Assert.AreEqual(6, read.Snapshot?.InteractiveElements);
        Assert.AreEqual(5, read.Snapshot?.FrictionSignals);
        Assert.AreEqual(6, read.Snapshot?.ActionMapEntries);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_RejectsRawDom()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T11:20:00Z");
        fixture.WriteSession(
            "2026-06-26T11:22:00Z",
            """
            ,
            "rawHtml": "<html><body>raw document should not be exported</body></html>",
            "domNodes": [{ "tag": "input", "value": "should-not-export" }]
            """);

        Export(fixture);
        var snapshotJson = File.ReadAllText(fixture.SnapshotPath);

        Assert.IsFalse(snapshotJson.Contains("rawHtml", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshotJson.Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshotJson.Contains("should-not-export", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_DoesNotIncludeSecretsOrPrivatePaths()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T11:20:00Z");
        fixture.WriteSession(
            "2026-06-26T11:22:00Z",
            $$"""
            ,
            "secretMaterial": "redacted-source-value",
            "privatePath": "{{fixture.Root.Replace("\\", "\\\\")}}"
            """);

        Export(fixture);
        var snapshotJson = File.ReadAllText(fixture.SnapshotPath);

        Assert.IsFalse(snapshotJson.Contains("redacted-source-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshotJson.Contains(fixture.Root, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshotJson.Contains(@"C:\", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_ReadsNestedUiBridgeSummaryCounts()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T11:20:00Z");
        fixture.WriteArtifact(
            "cloakbrowser-cdp-browser-skills-session-2026-06-26T11-22-00-000Z.redacted.json",
            """
            {
              "status": "PASS",
              "runtimeProvider": "cloakbrowser",
              "source": "cloakbrowser-cdp-direct",
              "captureOk": true,
              "screenshotCaptured": true,
              "extensionUsed": false,
              "systemBrowserUsed": false,
              "processExited": true,
              "runtimeShutdown": true,
              "orphanProcessDetected": false,
              "externalNavigationBlocked": true,
              "productFilesModified": false,
              "uiBridgeModel": {
                "Summary": {
                  "ElementCount": 6,
                  "FrictionCount": 5,
                  "ActionMapCount": 6
                }
              },
              "timestamp": "2026-06-26T11:22:00Z"
            }
            """);

        var result = Export(fixture);

        Assert.AreEqual(6, result.Snapshot?.InteractiveElements);
        Assert.AreEqual(5, result.Snapshot?.FrictionSignals);
        Assert.AreEqual(6, result.Snapshot?.ActionMapEntries);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_DoesNotLaunchRuntime()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T11:20:00Z");
        fixture.WriteSession("2026-06-26T11:22:00Z");

        var result = Export(fixture);

        Assert.IsFalse(result.RuntimeLaunched);
        Assert.IsFalse(result.Snapshot?.RuntimeLaunchedFromUi);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_DoesNotExecuteCdpLive()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T11:20:00Z");
        fixture.WriteSession("2026-06-26T11:22:00Z");

        var result = Export(fixture);

        Assert.IsFalse(result.CdpLiveExecuted);
        Assert.IsFalse(result.Snapshot?.CdpLiveExecutedFromUi);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_DoesNotFallbackToExtension()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteArtifact(
            "installed-sidepanel-2026-06-26T11-29-00-000Z.redacted.json",
            """
            { "status": "PASS", "extensionUsed": true, "timestamp": "2026-06-26T11:29:00Z" }
            """);

        var result = Export(fixture);

        Assert.AreEqual(CdpSafeLocalStatusChannelStatus.MissingEvidence, result.Status);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.Snapshot?.ExtensionUsed);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_DoesNotUseSystemBrowser()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteHealthcheck("2026-06-26T11:20:00Z");
        fixture.WriteSession("2026-06-26T11:22:00Z");

        var result = Export(fixture);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.Snapshot?.SystemBrowserUsed);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_HandlesMissingEvidence()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();

        var result = Export(fixture);
        var read = new CdpSafeLocalStatusSnapshotReader().ReadSnapshot(fixture.SnapshotPath);

        Assert.AreEqual(CdpSafeLocalStatusChannelStatus.MissingEvidence, result.Status);
        Assert.IsTrue(result.SnapshotWritten);
        Assert.IsFalse(result.Snapshot?.EvidenceAvailable);
        Assert.AreEqual("sin captura reciente", result.Snapshot?.RuntimeStatus);
        Assert.AreEqual(CdpSafeLocalStatusChannelStatus.MissingEvidence, read.Status);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_HandlesCorruptEvidence()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        fixture.WriteArtifact("cloakbrowser-cdp-healthcheck-2026-06-26T11-20-00-000Z.redacted.json", "{ not-json");
        fixture.WriteSession("2026-06-26T11:22:00Z");

        var result = Export(fixture);

        Assert.AreEqual(CdpSafeLocalStatusChannelStatus.Error, result.Status);
        Assert.AreEqual("SNAPSHOT_SOURCE_ERROR", result.Error?.Code);
        Assert.IsTrue(result.SnapshotWritten);
    }

    [TestMethod]
    public void CdpSafeLocalStatusChannel_SnapshotSchemaVersionIsValidated()
    {
        using var fixture = CdpSafeLocalStatusFixture.Create();
        File.WriteAllText(
            fixture.SnapshotPath,
            """
            {
              "schemaVersion": "0.9",
              "channel": "safe-local-status-snapshot",
              "source": "cloakbrowser-cdp-direct",
              "runtimeProvider": "cloakbrowser",
              "extensionUsed": false,
              "systemBrowserUsed": false,
              "runtimeLaunchedFromUi": false,
              "cdpLiveExecutedFromUi": false,
              "productFilesModified": false
            }
            """);

        var result = new CdpSafeLocalStatusSnapshotReader().ReadSnapshot(fixture.SnapshotPath);

        Assert.AreEqual(CdpSafeLocalStatusChannelStatus.InvalidSnapshot, result.Status);
        Assert.AreEqual("SNAPSHOT_SCHEMA_VERSION_INVALID", result.Error?.Code);
    }

    private static CdpSafeLocalStatusChannelResult Export(CdpSafeLocalStatusFixture fixture) =>
        new CdpSafeLocalStatusSnapshotWriter().Export(
            new CdpUiStatusRefreshRequest(
                fixture.Root,
                fixture.LockfilePath,
                RequestedAt: Now,
                Now: Now),
            fixture.SnapshotPath);

    private sealed class CdpSafeLocalStatusFixture : IDisposable
    {
        private CdpSafeLocalStatusFixture(string root)
        {
            Root = root;
            ArtifactsRoot = Path.Combine(root, "artifacts", "local-verification");
            GeneratedRoot = Path.Combine(root, "browser-extension", "onebrain-chrome-lab", "generated");
            Directory.CreateDirectory(ArtifactsRoot);
            Directory.CreateDirectory(GeneratedRoot);
            LockfilePath = Path.Combine(root, "browser-runtime.lock.json");
            SnapshotPath = Path.Combine(GeneratedRoot, CdpSafeLocalStatusSnapshotWriter.FileName);
            File.Copy(Path.Combine(RepoRoot(), "browser-runtime.lock.json"), LockfilePath);
        }

        public string Root { get; }

        public string ArtifactsRoot { get; }

        public string GeneratedRoot { get; }

        public string LockfilePath { get; }

        public string SnapshotPath { get; }

        public static CdpSafeLocalStatusFixture Create() =>
            new(Path.Combine(Path.GetTempPath(), $"nodal-cdp-safe-local-status-{Guid.NewGuid():N}"));

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

        public void WriteSession(string timestamp, string extraJsonProperties = "") =>
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
                  {{extraJsonProperties}}
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
