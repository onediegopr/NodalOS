using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserSafeDownloadM26Tests
{
    [TestMethod]
    public void BrowserSafeDownloadAllowsPdfFromAllowlistedHost()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(temp.Path, "report.pdf", "%PDF-1.4 synthetic");

        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(Request("https://docs.example.test/report.pdf", "report.pdf", "application/pdf"), Policy(temp.Path, Set("docs.example.test")), file);

        Assert.AreEqual(BrowserSafeDownloadDecisionKind.Verified, result.Decision);
        Assert.IsTrue(result.AllowsDone);
    }

    [TestMethod]
    public void BrowserSafeDownloadBlocksExecutableExtension() =>
        AssertBlocked("https://docs.example.test/setup.exe", "setup.exe", "application/octet-stream", "download executable or archive extension blocked");

    [TestMethod]
    public void BrowserSafeDownloadBlocksScriptExtension() =>
        AssertBlocked("https://docs.example.test/run.ps1", "run.ps1", "text/plain", "download executable or archive extension blocked");

    [TestMethod]
    public void BrowserSafeDownloadBlocksUnknownHost()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(temp.Path, "report.pdf", "%PDF-1.4 synthetic");
        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(Request("https://unknown.example.test/report.pdf", "report.pdf", "application/pdf"), Policy(temp.Path, Set("docs.example.test")), file);

        Assert.AreEqual(BrowserSafeDownloadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeDownloadBlocksPathTraversalFilename()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(temp.Path, "report.pdf", "%PDF-1.4 synthetic");
        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(Request("https://docs.example.test/report.pdf", "../report.pdf", "application/pdf"), Policy(temp.Path, Set("docs.example.test")), file);

        Assert.AreEqual(BrowserSafeDownloadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeDownloadRequiresControlledDirectory()
    {
        var policy = Policy("", Set("docs.example.test"));
        var result = new BrowserSafeDownloadManager().Block(Request("https://docs.example.test/report.pdf", "report.pdf", "application/pdf"), policy, string.Join("; ", policy.Validate().Errors));

        Assert.AreEqual(BrowserSafeDownloadDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserSafeDownloadRequiresHash()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var policy = Policy(temp.Path, Set("docs.example.test")) with { RequireHash = false };

        Assert.IsFalse(policy.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserSafeDownloadRequiresQuarantine()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var policy = Policy(temp.Path, Set("docs.example.test")) with { RequireQuarantine = false };

        Assert.IsFalse(policy.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserSafeDownloadDoesNotAutoOpen()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var policy = Policy(temp.Path, Set("docs.example.test")) with { AllowAutoOpen = true };

        Assert.IsFalse(policy.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserSafeDownloadDoesNotPersistCookiesOrHeaders()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(temp.Path, "report.pdf", "%PDF-1.4 synthetic");
        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(Request("https://docs.example.test/report.pdf?access_token=opaque", "report.pdf", "application/pdf"), Policy(temp.Path, Set("docs.example.test")), file);

        Assert.IsFalse(result.ToString()!.Contains("opaque", StringComparison.Ordinal));
        Assert.IsFalse(result.ToString()!.Contains("cookie", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserSafeDownloadProducesEvidence()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(temp.Path, "report.pdf", "%PDF-1.4 synthetic");
        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(Request("https://docs.example.test/report.pdf", "report.pdf", "application/pdf"), Policy(temp.Path, Set("docs.example.test")), file);

        Assert.IsTrue(result.EvidenceRefs.Count > 0);
        Assert.IsTrue(result.Verification!.AllowsStepDone());
    }

    [TestMethod]
    public void BrowserSafeDownloadRequiresVerificationBeforeDone()
    {
        var result = new BrowserSafeDownloadResult(BrowserSafeDownloadDecisionKind.Allowed, null, "allowed is not done", null, [], BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.DownloadRequested, "run-download", "action-download", "corr-download", "profile-controlled", "session-download", null, null, null, "Allowed", "allowed is not done"), true);

        Assert.IsFalse(result.AllowsDone);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsExternalLowRiskAuth()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ExternalAuthState = BrowserRuntimeExternalAuthState.LowRiskActive,
            ExternalAuthConsentPolicyGateValid = true,
            ExternalAuthTargetLowRisk = true,
            ExternalAuthReadOnlyGuardActive = true
        });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsForSensitiveExternalAuth() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { ExternalAuthState = BrowserRuntimeExternalAuthState.SensitiveOrCriticalActive }, "external auth low-risk only");

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsSafeDownload()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            DownloadState = BrowserRuntimeDownloadState.SafeDownloadActive,
            SafeDownloadAllowlistValid = true,
            SafeDownloadQuarantineEnabled = true,
            SafeDownloadHashRequired = true
        });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsForUnsafeDownload() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { DownloadState = BrowserRuntimeDownloadState.UnsafeDownloadActive }, "safe download policy");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenAutoOpenEnabled() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { DownloadState = BrowserRuntimeDownloadState.SafeDownloadActive, SafeDownloadAllowlistValid = true, SafeDownloadQuarantineEnabled = true, SafeDownloadHashRequired = true, SafeDownloadAutoOpenEnabled = true }, "safe download policy");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenExecutableDownloadAllowed() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { DownloadState = BrowserRuntimeDownloadState.SafeDownloadActive, SafeDownloadAllowlistValid = true, SafeDownloadQuarantineEnabled = true, SafeDownloadHashRequired = true, SafeDownloadExecutableAllowed = true }, "safe download policy");

    [TestMethod]
    [TestCategory("BrowserSafeDownloadLive")]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserSafeDownloadLiveDownloadsAllowedPdfToQuarantine()
    {
        RequireSafeDownloadLive();
        var browserPath = RequireLiveBrowser();
        using var server = await SafeDownloadFixtureServer.StartAsync();
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        using var rawDownload = BrowserVaultMinimalM23Tests.TempDir();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/download-page"));
        await page.EnableLiveReadOnlyCaptureAsync(rawDownload.Path);

        var action = CreateClick("run-safe-download-live", await page.GetCurrentTargetContextAsync("run-safe-download-live"), "#pdf-download");
        var click = await page.ExecuteActionAsync(action);
        await page.DrainEventsAsync(TimeSpan.FromSeconds(2));
        var materialized = Directory.GetFiles(rawDownload.Path).Single(path => Path.GetFileName(path).Equals("m26-report.pdf", StringComparison.OrdinalIgnoreCase));
        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(
            Request(server.Url("/download-pdf").ToString(), "m26-report.pdf", "application/pdf"),
            Policy(temp.Path, Set("127.0.0.1")),
            materialized);

        Assert.IsTrue(click.Executed);
        Assert.IsTrue(page.DownloadEvents.Any(e => e.SuggestedFilename == "m26-report.pdf"));
        Assert.IsTrue(result.AllowsDone, result.Reason);
        Assert.IsTrue(result.Artifact!.QuarantinePath.Contains("\\quarantine\\", StringComparison.OrdinalIgnoreCase) || result.Artifact.QuarantinePath.Contains("/quarantine/", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("BrowserSafeDownloadLive")]
    public void BrowserSafeDownloadLiveBlocksDisallowedExecutable()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(temp.Path, "tool.exe", "not executable");
        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(Request("https://docs.example.test/tool.exe", "tool.exe", "application/octet-stream"), Policy(temp.Path, Set("docs.example.test")), file);

        Assert.AreEqual(BrowserSafeDownloadDecisionKind.Blocked, result.Decision);
    }

    private static void AssertBlocked(string url, string fileName, string mime, string expectedReason)
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var file = Materialize(temp.Path, fileName, "fixture");
        var result = new BrowserSafeDownloadManager().ValidateMaterializedDownload(Request(url, fileName, mime), Policy(temp.Path, Set("docs.example.test")), file);

        Assert.AreEqual(BrowserSafeDownloadDecisionKind.Blocked, result.Decision);
        StringAssert.Contains(result.Reason, expectedReason);
    }

    private static void AssertGateFails(BrowserRuntimeObservedState state, string check)
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = PhaseReport(temp.Path, state);
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), check);
    }

    private static BrowserRuntimePhaseCloseReport PhaseReport(string tempPath, BrowserRuntimeObservedState state)
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

    private static BrowserSafeDownloadRequest Request(string url, string fileName, string mime) =>
        new("run-download", "action-download", "corr-download", "session-download", new Uri(url), fileName, mime, null);

    private static BrowserSafeDownloadPolicy Policy(string root, IReadOnlySet<string> hosts) =>
        new(root, hosts, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt", ".csv", ".json" }, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/pdf", "text/plain", "text/csv", "application/json", "application/octet-stream" }, 1024 * 1024, true, true, false, false);

    private static IReadOnlySet<string> Set(params string[] hosts) =>
        new HashSet<string>(hosts, StringComparer.OrdinalIgnoreCase);

    private static string Materialize(string root, string fileName, string content)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, Path.GetFileName(fileName));
        File.WriteAllText(path, content);
        return path;
    }

    private static string RequireLiveBrowser()
    {
        var path = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (path is null)
            Assert.Inconclusive("Chrome/Edge executable is not available.");
        return path;
    }

    private static void RequireSafeDownloadLive()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_SAFE_DOWNLOAD_LIVE_TESTS"), "1", StringComparison.Ordinal) ||
            !string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_CDP_LIVE_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("Safe download live tests are opt-in.");
    }

    private static BrowserAction CreateClick(string runId, BrowserTargetContext target, string selector) =>
        new($"action-{Guid.NewGuid():N}", $"idem-{Guid.NewGuid():N}", runId, "step-safe-download-live", target, target.FrameId, BrowserActionType.Click, new BrowserActionTarget(selector.TrimStart('#'), selector, selector, null), null, new BrowserExpectedOutcome("download observed", null, null, null), BrowserRiskClass.Low, 8000, false, DateTimeOffset.UtcNow);

    private sealed class SafeDownloadFixtureServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _loop;

        private SafeDownloadFixtureServer(HttpListener listener, int port)
        {
            _listener = listener;
            Port = port;
            _loop = Task.Run(ServeAsync);
        }

        public int Port { get; }
        public Uri Url(string path) => new($"http://127.0.0.1:{Port}{path}");

        public static Task<SafeDownloadFixtureServer> StartAsync()
        {
            var port = GetFreeTcpPort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return Task.FromResult(new SafeDownloadFixtureServer(listener, port));
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

        private static async Task HandleAsync(HttpListenerContext context)
        {
            if ((context.Request.Url?.AbsolutePath ?? "/") == "/download-page")
            {
                await Text(context, "<html><body><a id=\"pdf-download\" href=\"/download-pdf\">pdf</a></body></html>", "text/html; charset=utf-8");
                return;
            }

            if ((context.Request.Url?.AbsolutePath ?? "/") == "/download-pdf")
            {
                var bytes = Encoding.UTF8.GetBytes("%PDF-1.4 m26 synthetic pdf\n");
                context.Response.ContentType = "application/pdf";
                context.Response.Headers["Content-Disposition"] = "attachment; filename=\"m26-report.pdf\"";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
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
