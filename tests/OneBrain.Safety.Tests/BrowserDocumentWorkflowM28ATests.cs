using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserDocumentWorkflowM28ATests
{
    [TestMethod]
    [TestCategory("BrowserDocumentWorkflowSandbox")]
    public async Task BrowserDocumentWorkflowSandboxCompletesEndToEnd()
    {
        RequireWorkflowSandbox();
        var result = await RunWorkflowAsync();

        Assert.IsTrue(result.AllowsDone, result.Reason);
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowSandboxProducesAuditSummary()
    {
        var result = await RunWorkflowAsync();

        Assert.IsTrue(result.AuditSummary.IsSafe);
        Assert.IsTrue(result.AuditSummary.Events.Count >= 2);
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowSandboxVerifiesFinalStatus()
    {
        var result = await RunWorkflowAsync();

        Assert.IsTrue(result.FinalStatusVerified);
        Assert.IsTrue(result.Evidence.SemanticProofPresent);
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowSandboxDownloadsToQuarantine()
    {
        var result = await RunWorkflowAsync();

        Assert.IsTrue(result.DownloadVerified);
        Assert.IsTrue(result.Steps.Any(s => s.Kind == BrowserDocumentWorkflowStepKind.SafeDownload && s.Verified));
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowSandboxUploadsFromControlledRoot()
    {
        var result = await RunWorkflowAsync();

        Assert.IsTrue(result.UploadVerified);
        Assert.IsTrue(result.Steps.Any(s => s.Kind == BrowserDocumentWorkflowStepKind.SafeUpload && s.Verified));
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowSandboxCleansTempArtifacts()
    {
        var result = await RunWorkflowAsync();

        Assert.IsTrue(result.CleanupCompleted);
        Assert.IsTrue(result.Steps.Any(s => s.Kind == BrowserDocumentWorkflowStepKind.Cleanup && s.Verified));
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowFailsWithoutConsent()
    {
        var result = await RunWorkflowAsync(requestMutation: r => r with { Consent = null });

        Assert.AreEqual(BrowserDocumentWorkflowStatus.Failed, result.Status);
        StringAssert.Contains(result.Reason, "consent");
        Assert.IsTrue(result.CleanupCompleted);
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowFailsWhenGateFails()
    {
        var result = await RunWorkflowAsync(requestMutation: r => r with { GateReport = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { CompanionAuthoritative = true }) });

        Assert.AreEqual(BrowserDocumentWorkflowStatus.Failed, result.Status);
        StringAssert.Contains(result.Reason, "phase gate");
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowFailsWhenVaultRevoked()
    {
        var result = await RunWorkflowAsync(requestMutation: r => r with { VaultReferenceRevoked = true });

        Assert.AreEqual(BrowserDocumentWorkflowStatus.Failed, result.Status);
        StringAssert.Contains(result.Reason, "revoked");
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowFailsWhenDownloadBlocked()
    {
        var result = await RunWorkflowAsync(downloadPolicyMutation: p => p with { AllowlistedHosts = Set("not-localhost.test") });

        Assert.AreEqual(BrowserDocumentWorkflowStatus.Failed, result.Status);
        StringAssert.Contains(result.Reason, "download");
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowFailsWhenUploadApprovalMissing()
    {
        var result = await RunWorkflowAsync(requestMutation: r => r with { UploadApproval = null });

        Assert.AreEqual(BrowserDocumentWorkflowStatus.Failed, result.Status);
        StringAssert.Contains(result.Reason, "upload");
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowFailsWhenUploadUnsafe()
    {
        var result = await RunWorkflowAsync(uploadFileName: "tool.exe", uploadMime: "application/octet-stream");

        Assert.AreEqual(BrowserDocumentWorkflowStatus.Failed, result.Status);
        StringAssert.Contains(result.Reason, "upload");
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowFailsWithoutSemanticProof()
    {
        var result = await RunWorkflowAsync(policyMutation: p => p with { RequireFinalSemanticProof = false });

        Assert.IsFalse(result.AllowsDone);
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowDoesNotExposeCookiesSecretsOrBodies()
    {
        var result = await RunWorkflowAsync();
        var text = result.ToString()!;

        Assert.IsFalse(text.Contains("session=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("request body", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task BrowserDocumentWorkflowCleanupRunsAfterFailure()
    {
        var result = await RunWorkflowAsync(requestMutation: r => r with { ProfileControlled = false });

        Assert.AreEqual(BrowserDocumentWorkflowStatus.Failed, result.Status);
        Assert.IsTrue(result.CleanupCompleted);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsDocumentWorkflowSandbox()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            DocumentWorkflowState = BrowserRuntimeDocumentWorkflowState.SandboxActive,
            DocumentWorkflowSandboxVerified = true
        });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsExternalWorkflowWhenM25BBlocked()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            DocumentWorkflowState = BrowserRuntimeDocumentWorkflowState.ExternalActive,
            ExternalDocumentWorkflowAllowed = false
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "document workflow sandbox only");
    }

    private static async Task<BrowserDocumentWorkflowResult> RunWorkflowAsync(
        Func<BrowserDocumentWorkflowRequest, BrowserDocumentWorkflowRequest>? requestMutation = null,
        Func<BrowserDocumentWorkflowPolicy, BrowserDocumentWorkflowPolicy>? policyMutation = null,
        Func<BrowserSafeDownloadPolicy, BrowserSafeDownloadPolicy>? downloadPolicyMutation = null,
        string uploadFileName = "workflow-upload.txt",
        string uploadMime = "text/plain")
    {
        using var downloadRoot = BrowserVaultMinimalM23Tests.TempDir();
        using var uploadRoot = BrowserVaultMinimalM23Tests.TempDir();
        using var quarantineRoot = BrowserVaultMinimalM23Tests.TempDir();
        using var server = await BrowserDocumentWorkflowSandboxFixtureServer.StartAsync();

        var downloadFixture = Materialize(downloadRoot.Path, "workflow-document.txt", "synthetic document");
        var uploadPath = Path.Combine(uploadRoot.Path, uploadFileName);
        var request = new BrowserDocumentWorkflowRequest(
            "run-document-workflow",
            "action-document-workflow",
            "corr-document-workflow",
            "session-document-workflow",
            UploadConsent(),
            BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true)),
            BrowserVaultMinimalM23Tests.Reference(BrowserSecretKind.Password),
            VaultReferenceRevoked: false,
            ProfileControlled: true,
            Approval());
        request = requestMutation?.Invoke(request) ?? request;

        var downloadPolicy = new BrowserSafeDownloadPolicy(quarantineRoot.Path, Set("127.0.0.1"), Set(".txt"), Set("text/plain"), 1024 * 1024, true, true, false, false);
        downloadPolicy = downloadPolicyMutation?.Invoke(downloadPolicy) ?? downloadPolicy;
        var uploadPolicy = new BrowserSafeUploadPolicy(uploadRoot.Path, Set("127.0.0.1"), Set("/upload"), Set(".txt", ".pdf", ".csv", ".json"), Set("text/plain", "application/pdf", "text/csv", "application/json"), 1024 * 1024, true, true, false, false);
        var policy = new BrowserDocumentWorkflowPolicy(true, true, true, true, downloadPolicy, uploadPolicy, RequireFinalSemanticProof: true, FailOnAuditLeak: true);
        policy = policyMutation?.Invoke(policy) ?? policy;

        return await new BrowserDocumentWorkflowSandboxRunner().RunAsync(request, policy, server.Url("/download"), downloadFixture, server.Url("/upload"), uploadPath);
    }

    private static BrowserConsentGrant UploadConsent()
    {
        var service = new BrowserConsentService();
        var request = service.CreateRequest(BrowserConsentCapability.SecretUse, BrowserConsentScope.Session, "run-document-workflow", "action-document-workflow", "corr-document-workflow", "core-test", "document workflow sandbox", TimeSpan.FromMinutes(5));
        return service.Decide(request, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant!;
    }

    private static BrowserSafeUploadApproval Approval() =>
        new($"approval-{Guid.NewGuid():N}", "core-policy", DateTimeOffset.UtcNow, true, true);

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

    private static string Materialize(string root, string fileName, string content)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, Path.GetFileName(fileName));
        File.WriteAllText(path, content);
        return path;
    }

    private static void RequireWorkflowSandbox()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_DOCUMENT_WORKFLOW_SANDBOX_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("Document workflow sandbox tests are opt-in.");
    }

    private sealed class BrowserDocumentWorkflowSandboxFixtureServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _loop;

        private BrowserDocumentWorkflowSandboxFixtureServer(HttpListener listener, int port)
        {
            _listener = listener;
            Port = port;
            _loop = Task.Run(ServeAsync);
        }

        public int Port { get; }
        public bool Uploaded { get; private set; }
        public Uri Url(string path) => new($"http://127.0.0.1:{Port}{path}");

        public static Task<BrowserDocumentWorkflowSandboxFixtureServer> StartAsync()
        {
            var port = GetFreeTcpPort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return Task.FromResult(new BrowserDocumentWorkflowSandboxFixtureServer(listener, port));
        }

        public void Dispose()
        {
            _shutdown.Cancel();
            try { _listener.Stop(); _listener.Close(); _loop.Wait(TimeSpan.FromSeconds(1)); }
            catch { }
            finally { _shutdown.Dispose(); }
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
            if (path == "/login" || path == "/dashboard" || path == "/upload-page" || path == "/status" || path == "/logout")
            {
                await Text(context, "<html><body>document workflow sandbox</body></html>", "text/html");
                return;
            }
            if (path == "/document" || path == "/download")
            {
                await Text(context, "synthetic document", "text/plain");
                return;
            }
            if (path == "/upload" && context.Request.HttpMethod == "POST")
            {
                Uploaded = context.Request.ContentLength64 > 0;
                await Text(context, "{\"uploaded\":true}", "application/json");
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

