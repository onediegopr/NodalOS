using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserSafeUploadM27Tests
{
    [TestMethod]
    public async Task BrowserSafeUploadAllowsSyntheticPdfFromControlledRoot()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        using var server = await BrowserSafeUploadFixtureServer.StartAsync();
        var file = Materialize(root.Path, "synthetic.pdf", "%PDF-1.4 synthetic");

        var result = await new BrowserSafeUploadManager().UploadAsync(Request(server.Url("/upload"), file, "synthetic.pdf", "application/pdf"), Policy(root.Path, server.Port));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Verified, result.Decision);
        Assert.IsTrue(result.AllowsDone, result.Reason);
        Assert.IsTrue(server.UploadReceived);
    }

    [TestMethod]
    public async Task BrowserSafeUploadAllowsTxtCsvJsonFromControlledRoot()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        using var server = await BrowserSafeUploadFixtureServer.StartAsync();
        foreach (var (name, mime) in new[] { ("note.txt", "text/plain"), ("data.csv", "text/csv"), ("payload.json", "application/json") })
        {
            var file = Materialize(root.Path, name, "synthetic fixture");
            var result = await new BrowserSafeUploadManager().UploadAsync(Request(server.Url("/upload"), file, name, mime), Policy(root.Path, server.Port));
            Assert.AreEqual(BrowserSafeUploadDecisionKind.Verified, result.Decision, name);
        }
    }

    [TestMethod]
    public void BrowserSafeUploadBlocksExecutableExtension() =>
        AssertBlocked("tool.exe", "application/octet-stream", "safe upload executable, macro, archive, or secret extension blocked");

    [TestMethod]
    public void BrowserSafeUploadBlocksScriptExtension() =>
        AssertBlocked("run.ps1", "text/plain", "safe upload executable, macro, archive, or secret extension blocked");

    [TestMethod]
    public void BrowserSafeUploadBlocksMacroDocuments() =>
        AssertBlocked("macro.docm", "application/octet-stream", "safe upload executable, macro, archive, or secret extension blocked");

    [TestMethod]
    public void BrowserSafeUploadBlocksEnvKeyPemPfx()
    {
        AssertBlocked(".env", "text/plain", "safe upload executable, macro, archive, or secret extension blocked");
        AssertBlocked("cert.pfx", "application/octet-stream", "safe upload executable, macro, archive, or secret extension blocked");
        AssertBlocked("private.key", "text/plain", "safe upload executable, macro, archive, or secret extension blocked");
        AssertBlocked("cert.pem", "text/plain", "safe upload executable, macro, archive, or secret extension blocked");
    }

    [TestMethod]
    public void BrowserSafeUploadBlocksFileOutsideControlledRoot()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        using var outside = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(outside.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain"), Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeUploadBlocksPathTraversalFilename()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "../note.txt", "text/plain"), Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeUploadBlocksDirectoryUpload()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), root.Path, "folder", "text/plain"), Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeUploadBlocksWildcardUpload()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "*.txt", "text/plain"), Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeUploadRequiresHash()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var policy = Policy(root.Path) with { RequireHash = false };

        Assert.IsFalse(policy.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserSafeUploadRequiresApproval()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain") with { Approval = null }, Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeUploadRequiresConsentPolicyGate()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var noConsent = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain") with { Consent = null }, Policy(root.Path));
        var gateFailed = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain") with { GateReport = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { CompanionAuthoritative = true }) }, Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, noConsent.Decision);
        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, gateFailed.Decision);
    }

    [TestMethod]
    public void BrowserSafeUploadDoesNotExposeFullLocalPath()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain"), Policy(root.Path));

        Assert.IsFalse(result.ToString()!.Contains(root.Path, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(result.Artifact!.RedactedPath.StartsWith("[CONTROLLED_UPLOAD_ROOT]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserSafeUploadDoesNotPersistFileContent()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic upload body must not persist");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain"), Policy(root.Path));

        Assert.IsFalse(result.ToString()!.Contains("synthetic upload body must not persist", StringComparison.Ordinal));
        Assert.IsFalse(result.Artifact!.ContentCaptured);
    }

    [TestMethod]
    public void BrowserSafeUploadDoesNotPersistCookiesOrHeaders()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload?access_token=opaque"), file, "note.txt", "text/plain"), Policy(root.Path));

        Assert.IsFalse(result.ToString()!.Contains("opaque", StringComparison.Ordinal));
        Assert.IsFalse(result.ToString()!.Contains("cookie", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserSafeUploadProducesEvidence()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain"), Policy(root.Path));

        Assert.IsTrue(result.EvidenceRefs.Count > 0);
        Assert.AreEqual(BrowserSafeUploadDecisionKind.Allowed, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeUploadRequiresVerificationBeforeDone()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "note.txt", "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "note.txt", "text/plain"), Policy(root.Path));

        Assert.IsFalse(result.AllowsDone);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsSafeUpload()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            SafeUploadState = BrowserRuntimeUploadState.SafeUploadActive,
            SafeUploadAllowlistValid = true,
            SafeUploadApprovalPresent = true,
            SafeUploadHashRequired = true
        });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsForUnsafeUpload() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { SafeUploadState = BrowserRuntimeUploadState.UnsafeUploadActive }, "safe upload policy");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenExecutableUploadAllowed() =>
        AssertGateFails(SafeUploadUnsafeState() with { SafeUploadExecutableAllowed = true }, "safe upload policy");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenUploadApprovalMissing() =>
        AssertGateFails(SafeUploadUnsafeState() with { SafeUploadApprovalPresent = false }, "safe upload policy");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenUploadOutsideControlledRoot() =>
        AssertGateFails(SafeUploadUnsafeState() with { SafeUploadControlledRoot = false }, "safe upload policy");

    [TestMethod]
    [TestCategory("BrowserSafeUploadLive")]
    public async Task BrowserSafeUploadLiveUploadsSyntheticFileToLocalFixture()
    {
        RequireSafeUploadLive();
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        using var server = await BrowserSafeUploadFixtureServer.StartAsync();
        var file = Materialize(root.Path, "live-upload.txt", "synthetic live upload");

        var result = await new BrowserSafeUploadManager().UploadAsync(Request(server.Url("/upload"), file, "live-upload.txt", "text/plain"), Policy(root.Path, server.Port));

        Assert.IsTrue(server.UploadReceived);
        Assert.IsTrue(result.AllowsDone, result.Reason);
    }

    [TestMethod]
    [TestCategory("BrowserSafeUploadLive")]
    public void BrowserSafeUploadLiveBlocksDisallowedExecutable()
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, "tool.exe", "not executable");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, "tool.exe", "application/octet-stream"), Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, result.Decision);
    }

    private static void AssertBlocked(string fileName, string mime, string reason)
    {
        using var root = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(root.Path, fileName, "synthetic");
        var result = new BrowserSafeUploadManager().Validate(Request(new Uri("https://upload.example.test/upload"), file, fileName, mime), Policy(root.Path));

        Assert.AreEqual(BrowserSafeUploadDecisionKind.Blocked, result.Decision);
        StringAssert.Contains(result.Reason, reason);
    }

    private static void AssertGateFails(BrowserRuntimeObservedState state, string check)
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, state);
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), check);
    }

    private static BrowserRuntimeObservedState SafeUploadUnsafeState() =>
        BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            SafeUploadState = BrowserRuntimeUploadState.SafeUploadActive,
            SafeUploadAllowlistValid = true,
            SafeUploadApprovalPresent = true,
            SafeUploadHashRequired = true
        };

    private static BrowserSafeUploadPolicy Policy(string root, int? port = null) =>
        new(
            root,
            port.HasValue ? Set("127.0.0.1") : Set("upload.example.test"),
            Set("/upload"),
            Set(".txt", ".csv", ".json", ".pdf"),
            Set("text/plain", "text/csv", "application/json", "application/pdf"),
            1024 * 1024,
            RequireHash: true,
            RequireApproval: true,
            AllowWildcardUpload: false,
            AllowDirectoryUpload: false);

    private static BrowserSafeUploadRequest Request(Uri endpoint, string filePath, string fileName, string mime) =>
        new("run-upload", "action-upload", "corr-upload", "session-upload", endpoint, filePath, fileName, mime, Consent(), BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true)), Approval());

    private static BrowserConsentGrant Consent()
    {
        var service = new BrowserConsentService();
        var request = service.CreateRequest(BrowserConsentCapability.SecretUse, BrowserConsentScope.Session, "run-upload", "action-upload", "corr-upload", "core-test", "safe upload fixture", TimeSpan.FromMinutes(5));
        return service.Decide(request, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant!;
    }

    private static BrowserSafeUploadApproval Approval() =>
        new($"approval-{Guid.NewGuid():N}", "core-policy", DateTimeOffset.UtcNow, Authoritative: true, Redacted: true);

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

    private static string Materialize(string root, string fileName, string content)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, Path.GetFileName(fileName));
        File.WriteAllText(path, content);
        return path;
    }

    private static void RequireSafeUploadLive()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_SAFE_UPLOAD_LIVE_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("Safe upload live tests are opt-in.");
    }

    internal sealed class BrowserSafeUploadFixtureServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _loop;

        private BrowserSafeUploadFixtureServer(HttpListener listener, int port)
        {
            _listener = listener;
            Port = port;
            _loop = Task.Run(ServeAsync);
        }

        public int Port { get; }
        public bool UploadReceived { get; private set; }
        public Uri Url(string path) => new($"http://127.0.0.1:{Port}{path}");

        public static Task<BrowserSafeUploadFixtureServer> StartAsync()
        {
            var port = GetFreeTcpPort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return Task.FromResult(new BrowserSafeUploadFixtureServer(listener, port));
        }

        public void Dispose()
        {
            _shutdown.Cancel();
            try
            {
                _listener.Stop();
                _listener.Close();
                _loop.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
            }
            finally
            {
                _shutdown.Dispose();
            }
        }

        private async Task ServeAsync()
        {
            while (!_shutdown.IsCancellationRequested)
            {
                HttpListenerContext context;
                try { context = await _listener.GetContextAsync(); }
                catch { return; }
                _ = Task.Run(() => HandleAsync(context), _shutdown.Token);
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            if (path == "/upload-page")
            {
                await Text(context, "<html><body><form action=\"/upload\" method=\"post\" enctype=\"multipart/form-data\"><input type=\"file\" name=\"file\" /></form></body></html>", "text/html");
                return;
            }

            if (path == "/upload" && context.Request.HttpMethod == "POST")
            {
                UploadReceived = context.Request.ContentLength64 > 0;
                context.Response.StatusCode = 200;
                await Text(context, "{\"uploaded\":true}", "application/json");
                return;
            }

            if (path == "/upload-status")
            {
                await Text(context, UploadReceived ? "{\"status\":\"received\"}" : "{\"status\":\"empty\"}", "application/json");
                return;
            }

            context.Response.StatusCode = 404;
            await Text(context, "not found", "text/plain");
        }

        private static async Task Text(HttpListenerContext context, string text, string contentType)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }

        private static int GetFreeTcpPort()
        {
            using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}

internal static class BrowserSafeDownloadM26TestAccess
{
    public static BrowserRuntimePhaseCloseReport PhaseReport(string tempPath, BrowserRuntimeObservedState state)
    {
        var ledger = new BrowserPersistentAuditLedger(new BrowserAuditLedgerPolicy(tempPath, true, true, new BrowserAuditLedgerRetentionPolicy(null, null, true)));
        ledger.Append(BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.PhaseCloseGateEvaluated, "run-phase", "action-phase", "corr-phase", "profile-runtime", "session-phase", null, null, null, "Observed", "phase gate observed"));
        var fixtureRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "browser-executor"));
        var download = new BrowserDownloadManager().CompleteFixtureDownload(new BrowserDownloadRequest("run-phase", "action-download", "corr-phase", "session-phase", "https://fixture.local/download", "sample-data.csv", "text/csv", null), new BrowserDownloadPolicy(tempPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, true, true, false), Path.Combine(fixtureRoot, "downloads", "sample-data.csv"));
        var uploadPath = Path.Combine(fixtureRoot, "upload", "sample-upload.txt");
        var upload = new BrowserUploadManager().PrepareFixtureUpload(new BrowserUploadRequest("run-phase", "action-upload", "corr-phase", "session-phase", uploadPath, "fixture upload target", true), new BrowserUploadPolicy(Path.GetDirectoryName(uploadPath)!, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, true, false));
        var network = new BrowserNetworkCapture().Capture(new BrowserNetworkCapturePolicy(BrowserNetworkCaptureMode.MetadataOnly, true, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST" }), [new BrowserNetworkCaptureEvent("request-phase", "corr-phase", "GET", "https://fixture.local/api/items", 200, "fetch", TimeSpan.FromMilliseconds(3), [new BrowserNetworkHeaderMetadata("content-type", true, true, "application/json", BrowserNetworkHeaderRedactionReason.None)], true, false, false, true)]);
        var manifest = new BrowserSessionReplayManifest("run-phase", "corr-phase", DateTimeOffset.UtcNow, [new BrowserSessionReplayStep("step-phase", "Verified", "Observe", "Verified", ["proof-phase"], true, false)], [BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.SessionExportCreated, "run-phase", "action-export", "corr-phase", "profile-runtime", "session-phase", null, null, null, "Completed", "session export created")], network.Events, [], [], "core-governed-browser-runtime", true, true);
        return new BrowserRuntimePhaseCloseGate().Evaluate(new StaticBrowserRuntimeSecurityProbe(state, ["evidence:m18-cdp-live"], ["audit:m17-hmac-head"]), ledger.ExportSafe(), download, upload, network, new BrowserSessionExportService().CreatePackage(manifest));
    }
}

