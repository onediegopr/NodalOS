using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserCdpLiveTests
{
    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveCanNavigateToLocalFixtureReadOnly()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/"));

        var observation = await page.ObserveAsync("run-cdp-live-navigation");
        var action = CreateReadAction("run-cdp-live-navigation", await page.GetCurrentTargetContextAsync("run-cdp-live-navigation"), "M18 live fixture ready");
        var verification = await page.VerifyAsync(action, observation.ObservationId);

        Assert.AreEqual("M18 CDP Live Fixture", observation.Title);
        StringAssert.Contains(observation.VisibleTextSummary, "M18 live fixture ready");
        Assert.AreEqual(BrowserVerificationStatus.Verified, verification.Status);
        Assert.IsTrue(verification.HasSemanticProof);
        Assert.IsTrue(verification.AllowsStepDone());
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveCanReadDomFromLocalFixture()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/"));

        var observation = await page.ObserveAsync("run-cdp-live-dom");

        Assert.AreEqual(BrowserTargetSource.Cdp, observation.TargetContext.Source);
        Assert.AreEqual("complete", observation.ReadyState);
        StringAssert.Contains(observation.VisibleTextSummary, "Read-only DOM proof");
        Assert.IsTrue(observation.TargetContext.Validate().IsValid);
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveNetworkCaptureRecordsMetadataOnly()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/"));
        using var temp = TempDirectory.Create();

        await page.EnableLiveReadOnlyCaptureAsync(temp.Path);
        await page.NavigateAsync(server.Url("/network"));
        await page.DrainEventsAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(page.NetworkEvents.Count > 0);
        Assert.IsTrue(page.NetworkEvents.All(e => !e.RequestBodyCaptured && !e.ResponseBodyCaptured));
        Assert.IsTrue(page.NetworkEvents.All(e => e.Redacted));
        Assert.IsTrue(page.NetworkEvents.Any(e => e.RedactedUrl.Contains("/headers", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(page.NetworkEvents.All(e => e.Validate().IsValid));
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveNetworkCaptureDoesNotRecordSensitiveHeaderValues()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/"));
        using var temp = TempDirectory.Create();

        await page.EnableLiveReadOnlyCaptureAsync(temp.Path);
        await page.NavigateAsync(server.Url("/network"));
        await page.DrainEventsAsync(TimeSpan.FromSeconds(2));

        var serialized = string.Join("\n", page.NetworkEvents.Select(e => e.ToString()));
        Assert.IsFalse(serialized.Contains("abcdef123456opaque", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("opaquesecretvalue", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("Bearer abcdef", StringComparison.Ordinal));
        Assert.IsTrue(page.NetworkEvents
            .SelectMany(e => e.ResponseHeaders)
            .Where(h => h.HeaderName is "authorization" or "x-api-key")
            .All(h => h.Present && !h.ValueCaptured && h.Value == "[NOT_CAPTURED]" && h.RedactionReason == BrowserNetworkHeaderRedactionReason.SensitiveHeaderValueNotCaptured));
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveBuildsFrameTreeFromLocalFixture()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/frames"));

        await page.DrainEventsAsync(TimeSpan.FromSeconds(1));
        var tree = page.LiveTargetManager.Frames.GetTree(page.TargetId);

        Assert.IsTrue(page.RuntimeEvents.Any(e => e.EventType is BrowserTargetEventType.FrameAttached or BrowserTargetEventType.FrameNavigated));
        Assert.IsTrue(tree.Frames.Values.Any(frame => frame.Url.AbsolutePath.Equals("/frame-child", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveDetectsFrameDetachAndBlocksStaleFrameRead()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/frames"));

        await page.DrainEventsAsync(TimeSpan.FromSeconds(1));
        var click = CreateClickAction("run-cdp-live-frame-stale", await page.GetCurrentTargetContextAsync("run-cdp-live-frame-stale"), "#detach-frame");
        var clickResult = await page.ExecuteActionAsync(click);
        await page.DrainEventsAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(clickResult.Executed);
        var tree = page.LiveTargetManager.Frames.GetTree(page.TargetId);
        var target = page.LiveTargetManager.SelectTarget(BrowserTargetSelectionPolicy.Explicit(), page.TargetId);
        var detached = tree.Frames.Values.FirstOrDefault(frame => frame.State == BrowserTargetState.Detached);
        Assert.IsNotNull(detached);

        var action = CreateReadAction("run-cdp-live-frame-stale", page.LiveTargetManager.ToTargetContext("run-cdp-live-frame-stale", session.BrowserSessionId, target, tree.MainFrame), "child frame");

        Assert.IsFalse(detached.CanUseForVerification);
        Assert.IsFalse(page.LiveTargetManager.CanExecute(action, target, detached));
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveObservesDownloadWillBeginFromLocalFixture()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/download-page"));
        using var temp = TempDirectory.Create();

        await page.EnableLiveReadOnlyCaptureAsync(temp.Path);
        var action = CreateClickAction("run-cdp-live-download", await page.GetCurrentTargetContextAsync("run-cdp-live-download"), "#download-link");
        var result = await page.ExecuteActionAsync(action);
        await page.DrainEventsAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(result.Executed);
        Assert.IsTrue(page.DownloadEvents.Any(e => e.SuggestedFilename == "m18-sample.txt"));
        Assert.IsTrue(page.DownloadEvents.All(e => e.Url.Contains("/download", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(Directory.GetFiles(temp.Path).All(path => path.StartsWith(temp.Path, StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveAuditLedgerVerifiesAfterNetworkEvents()
    {
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserCdpLiveFixtureServer.StartAsync();
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath));
        await using var page = await session.CreatePageAsync(server.Url("/"));
        using var temp = TempDirectory.Create();

        await page.EnableLiveReadOnlyCaptureAsync(temp.Path);
        await page.NavigateAsync(server.Url("/network"));
        await page.DrainEventsAsync(TimeSpan.FromSeconds(2));

        var ledger = new BrowserPersistentAuditLedger(new BrowserAuditLedgerPolicy(temp.Path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)), BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("onebrain-m50-explicit-test-fixture-hmac-key"));
        ledger.Append(BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.NetworkCaptureRecorded,
            "run-cdp-live-audit",
            "action-network-read",
            "corr-cdp-live-audit",
            "profile-runtime",
            session.BrowserSessionId,
            null,
            null,
            null,
            "Completed",
            "cdp live network metadata captured",
            new Dictionary<string, string> { ["eventCount"] = page.NetworkEvents.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
        var seal = ledger.HeadSeal;
        var export = ledger.ExportSafe();

        Assert.IsTrue(page.NetworkEvents.Count > 0);
        Assert.IsTrue(ledger.VerifyIntegrity(seal));
        Assert.IsTrue(export.Validate().IsValid);
        var serialized = File.ReadAllText(ledger.LedgerFile);
        Assert.IsFalse(serialized.Contains("abcdef123456opaque", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("opaquesecretvalue", StringComparison.Ordinal));
    }

    [TestMethod]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserCdpLiveClosesBrowserProcessAfterTest()
    {
        var browserPath = RequireLiveBrowser();
        int pid;
        await using (var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath)))
        {
            pid = session.ProcessId;
            Assert.IsTrue(session.IsProcessAlive);
        }

        await Task.Delay(500);
        Assert.IsFalse(IsProcessAlive(pid));
    }

    private static string RequireLiveBrowser()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_CDP_LIVE_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("BrowserCdpLive tests are opt-in. Set ONEBRAIN_RUN_CDP_LIVE_TESTS=1 to run them.");

        var path = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (path is null)
            Assert.Inconclusive("Chrome/Edge executable is not available in this environment.");
        return path!;
    }

    private static BrowserAction CreateReadAction(string runId, BrowserTargetContext target, string expectedText) =>
        new(
            ActionId: $"action-{Guid.NewGuid():N}",
            IdempotencyKey: "",
            RunId: runId,
            StepId: "step-read",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: BrowserActionType.Read,
            Target: new BrowserActionTarget("body", "body", "body", null),
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("read-only text visible", null, expectedText, null),
            RiskClass: BrowserRiskClass.ReadOnly,
            TimeoutMs: 8000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserAction CreateClickAction(string runId, BrowserTargetContext target, string selector) =>
        new(
            ActionId: $"action-{Guid.NewGuid():N}",
            IdempotencyKey: $"idem-{Guid.NewGuid():N}",
            RunId: runId,
            StepId: "step-download-event",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: BrowserActionType.Click,
            Target: new BrowserActionTarget(selector.TrimStart('#'), selector, selector, null),
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("download event observed", null, null, null),
            RiskClass: BrowserRiskClass.Low,
            TimeoutMs: 8000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-m18-{Guid.NewGuid():N}");

        private TempDirectory() => Directory.CreateDirectory(Path);

        public static TempDirectory Create() => new();

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Test cleanup is best effort; assertions use controlled temp directories only.
            }
        }
    }

    private sealed class BrowserCdpLiveFixtureServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _loop;

        private BrowserCdpLiveFixtureServer(HttpListener listener, int port)
        {
            _listener = listener;
            Port = port;
            _loop = Task.Run(ServeAsync);
        }

        public int Port { get; }

        public static Task<BrowserCdpLiveFixtureServer> StartAsync()
        {
            var port = GetFreeTcpPort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return Task.FromResult(new BrowserCdpLiveFixtureServer(listener, port));
        }

        public Uri Url(string path) => new($"http://127.0.0.1:{Port}{path}");

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
                // Fixture shutdown is best effort.
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
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch when (_shutdown.IsCancellationRequested)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (HttpListenerException)
                {
                    return;
                }

                _ = Task.Run(() => HandleAsync(context), _shutdown.Token);
            }
        }

        private static async Task HandleAsync(HttpListenerContext context)
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            switch (path)
            {
                case "/":
                    await Html(context, """
<!doctype html>
<html><head><title>M18 CDP Live Fixture</title></head>
<body>
  <main id="ready">M18 live fixture ready. Read-only DOM proof.</main>
</body></html>
""");
                    break;
                case "/network":
                    await Html(context, """
<!doctype html>
<html><head><title>M18 Network Fixture</title></head>
<body>
  <main>Network metadata fixture</main>
  <script>
    fetch('/headers?access_token=synthetic-query-token', {
      headers: {
        'authorization': 'Bearer abcdef123456opaque',
        'x-api-key': 'opaquesecretvalue'
      }
    }).then(r => r.text()).then(t => {
      const node = document.createElement('p');
      node.id = 'network-result';
      node.textContent = 'network complete';
      document.body.appendChild(node);
    });
  </script>
</body></html>
""");
                    break;
                case "/headers":
                    context.Response.Headers["x-api-key"] = "opaquesecretvalue";
                    await Text(context, "{\"ok\":true}", "application/json");
                    break;
                case "/frames":
                    var autoDetach = string.Equals(context.Request.QueryString["autoDetach"], "1", StringComparison.Ordinal);
                    await Html(context, $$"""
<!doctype html>
<html><head><title>M18 Frames Fixture</title></head>
<body>
  <main>Frame fixture</main>
  <iframe id="live-frame" src="/frame-child"></iframe>
  <button id="detach-frame" onclick="document.getElementById('live-frame')?.remove()">detach frame</button>
  {{(autoDetach ? "<script>setTimeout(() => document.getElementById('live-frame')?.remove(), 400);</script>" : "")}}
</body></html>
""");
                    break;
                case "/frame-child":
                    await Html(context, """
<!doctype html>
<html><head><title>M18 Frame Child</title></head>
<body><main>child frame read-only content</main></body></html>
""");
                    break;
                case "/download-page":
                    await Html(context, """
<!doctype html>
<html><head><title>M18 Download Fixture</title></head>
<body>
  <a id="download-link" href="/download">download synthetic file</a>
</body></html>
""");
                    break;
                case "/download":
                    var bytes = Encoding.UTF8.GetBytes("m18 synthetic download\n");
                    context.Response.ContentType = "text/plain";
                    context.Response.Headers["Content-Disposition"] = "attachment; filename=\"m18-sample.txt\"";
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.Close();
                    break;
                default:
                    context.Response.StatusCode = 404;
                    await Text(context, "not found", "text/plain");
                    break;
            }
        }

        private static Task Html(HttpListenerContext context, string html) =>
            Text(context, html, "text/html; charset=utf-8");

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
