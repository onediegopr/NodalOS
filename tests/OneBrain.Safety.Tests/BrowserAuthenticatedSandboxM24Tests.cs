using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserAuthenticatedSandboxM24Tests
{
    private const string SandboxUserValue = "sandbox-user";
    private const string SandboxPassValue = "synthetic-local-passphrase";
    private const string DashboardProof = "Sandbox dashboard authenticated";

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxFailsWithoutConsent()
    {
        var setup = await VaultSetupAsync();
        var result = await setup.Provider.RetrieveAsync(BrowserVaultMinimalM23Tests.Retrieve(setup.PassReference, null, BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxFailsWithExpiredConsent()
    {
        var setup = await VaultSetupAsync();
        var consent = BrowserVaultMinimalM23Tests.Consent(TimeSpan.FromMilliseconds(-1));
        var result = await setup.Provider.RetrieveAsync(BrowserVaultMinimalM23Tests.Retrieve(setup.PassReference, consent, BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxFailsWithRevokedConsent()
    {
        var setup = await VaultSetupAsync();
        var service = new BrowserConsentService();
        var request = service.CreateRequest(BrowserConsentCapability.SecretRetrieval, BrowserConsentScope.Profile, "run-vault", "action-vault", "corr-vault", "core-test", "sandbox form fill", TimeSpan.FromMinutes(5));
        var granted = service.Decide(request, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant!;
        var revoked = service.Revoke(granted, "core-test", DateTimeOffset.UtcNow).Grant;
        var result = await setup.Provider.RetrieveAsync(BrowserVaultMinimalM23Tests.Retrieve(setup.PassReference, revoked, BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxFailsWithWrongScope()
    {
        var setup = await VaultSetupAsync();
        var result = await setup.Provider.RetrieveAsync(BrowserVaultMinimalM23Tests.Retrieve(setup.PassReference, BrowserVaultMinimalM23Tests.Consent(scope: BrowserConsentScope.Session), BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxFailsWhenGateFails()
    {
        var setup = await VaultSetupAsync();
        var result = await setup.Provider.RetrieveAsync(BrowserVaultMinimalM23Tests.Retrieve(setup.PassReference, BrowserVaultMinimalM23Tests.Consent(), BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { SensitiveHeaderValueCaptureSupported = true })));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.FailClosed, result.Decision);
    }

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxFailsWhenVaultSecretUnknown()
    {
        var provider = new BrowserVaultMinimalSandboxProvider();
        var result = await provider.RetrieveAsync(BrowserVaultMinimalM23Tests.Retrieve(BrowserVaultMinimalM23Tests.Reference(BrowserSecretKind.Password), BrowserVaultMinimalM23Tests.Consent(), BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.UnknownSecret, result.Decision);
    }

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxFailsWhenVaultSecretRevoked()
    {
        var setup = await VaultSetupAsync();
        await setup.Provider.RevokeAsync(new BrowserVaultRevocationRequest($"vault-request-{Guid.NewGuid():N}", "run-vault", "action-vault", "corr-vault", "profile-controlled", "session-controlled", setup.PassReference, DateTimeOffset.UtcNow));
        var result = await setup.Provider.RetrieveAsync(BrowserVaultMinimalM23Tests.Retrieve(setup.PassReference, BrowserVaultMinimalM23Tests.Consent(), BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.Revoked, result.Decision);
    }

    [TestMethod]
    public void BrowserAuthenticatedSandboxFailsWithoutSemanticProof()
    {
        var context = new BrowserTargetContext("run-auth", "chrome-cdp", "session-controlled", null, null, "target", "page", null, "main", null, new Uri("http://127.0.0.1/"), "Sandbox", 0, BrowserTargetContext.CreateLivenessToken("target", "main", 0), DateTimeOffset.UtcNow, null, null, null, "complete", BrowserTargetSource.Cdp);
        var verification = new BrowserVerification($"verification-{Guid.NewGuid():N}", "run-auth", "step-auth", "action-auth", context, new BrowserExpectedOutcome("dashboard visible", null, DashboardProof, null), null, null, BrowserVerificationStatus.Verified, 0.95, ["evidence-only"], null, DateTimeOffset.UtcNow, []);
        var result = new BrowserAuthenticatedSandboxResult(true, verification, [], [], false, false, false, "fixture");

        Assert.IsFalse(result.AllowsStepDone);
        Assert.IsFalse(verification.AllowsStepDone());
    }

    [TestMethod]
    public async Task BrowserAuthenticatedSandboxDoesNotPersistCookieValues()
    {
        var summary = new BrowserNetworkCapture().Capture(
            new BrowserNetworkCapturePolicy(BrowserNetworkCaptureMode.MetadataOnly, true, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET" }),
            [new BrowserNetworkCaptureEvent("request-auth", "corr-auth", "GET", "http://127.0.0.1/dashboard", 200, "document", TimeSpan.FromMilliseconds(1), [new BrowserNetworkHeaderMetadata("set-cookie", true, true, "sandbox_session=local-session-value", BrowserNetworkHeaderRedactionReason.None)], false, false, false, false)]);

        var text = string.Join("\n", summary.Events.Select(e => e.ToString()));
        Assert.IsTrue(summary.IsSafe);
        Assert.IsFalse(text.Contains("local-session-value", StringComparison.Ordinal));
        await Task.CompletedTask;
    }

    [TestMethod]
    public void BrowserAuthenticatedSandboxDoesNotExposeSessionStorage()
    {
        var result = new BrowserAuthenticatedSandboxResult(false, FailedVerification(), [], [], CookieValuesExposed: false, SessionStorageExposed: false, SecretValueExposed: false, "blocked");

        Assert.IsFalse(result.SessionStorageExposed);
    }

    [TestMethod]
    public void BrowserAuthenticatedSandboxSensitiveHeadersRemainPresenceOnly()
    {
        var summary = new BrowserNetworkCapture().Capture(
            new BrowserNetworkCapturePolicy(BrowserNetworkCaptureMode.MetadataOnly, true, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET" }),
            [new BrowserNetworkCaptureEvent("request-auth", "corr-auth", "GET", "http://127.0.0.1/dashboard", 200, "document", TimeSpan.FromMilliseconds(1), [new BrowserNetworkHeaderMetadata("authorization", true, true, "Bearer opaque-auth-value", BrowserNetworkHeaderRedactionReason.None)], false, false, false, false)]);

        var header = summary.Events.Single().ResponseHeaders.Single();
        Assert.IsFalse(header.ValueCaptured);
        Assert.AreEqual("[NOT_CAPTURED]", header.Value);
        Assert.IsFalse(summary.ToString()!.Contains("opaque-auth-value", StringComparison.Ordinal));
    }

    [TestMethod]
    [TestCategory("BrowserAuthenticatedSandbox")]
    [TestCategory("BrowserCdpLive")]
    public async Task BrowserAuthenticatedSandboxLoginSandboxWithCdpProfileVaultConsentGate()
    {
        RequireAuthSandbox();
        var browserPath = RequireLiveBrowser();
        using var server = await BrowserAuthenticatedSandboxFixtureServer.StartAsync();
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var vault = new BrowserVaultMinimalSandboxProvider();
        var userRef = BrowserVaultMinimalM23Tests.Reference(BrowserSecretKind.Password);
        var passRef = BrowserVaultMinimalM23Tests.Reference(BrowserSecretKind.Password);
        await vault.StoreSandboxAsync(BrowserVaultMinimalM23Tests.Store(userRef), SandboxUserValue);
        await vault.StoreSandboxAsync(BrowserVaultMinimalM23Tests.Store(passRef), SandboxPassValue);

        var consentService = new BrowserConsentService();
        var profileConsentRequest = consentService.CreateRequest(BrowserConsentCapability.ProfileControlledActivation, BrowserConsentScope.Profile, "run-auth", "action-auth", "corr-auth", "core-test", "controlled profile activation", TimeSpan.FromMinutes(10));
        var profileConsent = consentService.Decide(profileConsentRequest, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant!;
        var vaultConsentRequest = consentService.CreateRequest(BrowserConsentCapability.SecretRetrieval, BrowserConsentScope.Profile, "run-auth", "action-auth", "corr-auth", "core-test", "sandbox form fill", TimeSpan.FromMinutes(10));
        var vaultConsent = consentService.Decide(vaultConsentRequest, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant!;
        var profileGate = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: false));
        var activationService = new BrowserControlledProfileActivationService();
        var activation = activationService.Activate(
            new BrowserControlledProfileActivationRequest("run-auth", "action-auth", "corr-auth", "sandbox-owner", BrowserControlledProfileMode.PersistentControlled, profileConsent, profileGate, DateTimeOffset.UtcNow),
            new BrowserControlledProfileActivationPolicy(true, false, temp.Path, BrowserStorageScope.Person),
            DateTimeOffset.UtcNow);

        Assert.IsTrue(activation.IsActivated, activation.Reason);
        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath, ControlledProfile: activation.Profile));
        await using var page = await session.CreatePageAsync(server.Url("/login"));
        using var downloads = BrowserVaultMinimalM23Tests.TempDir();
        await page.EnableLiveReadOnlyCaptureAsync(downloads.Path);

        var result = await new BrowserAuthenticatedSandboxScenario().RunAsync(
            page,
            vault,
            new BrowserAuthenticatedSandboxRequest("run-auth", "action-auth", "corr-auth", userRef, passRef, vaultConsent, BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true)), server.Url("/login"), "#username", "#passcode", "#login-button", DashboardProof));

        var publicSurface = string.Join("\n", result.ToString(), string.Join("\n", page.NetworkEvents.Select(e => e.ToString())), string.Join("\n", vault.AuditEvents.Select(e => e.ToString())));
        Assert.IsTrue(result.AllowsStepDone, result.Reason);
        Assert.IsTrue(result.Verification.AllowsStepDone());
        Assert.IsTrue(page.NetworkEvents.All(e => !e.RequestBodyCaptured && !e.ResponseBodyCaptured));
        Assert.IsTrue(page.NetworkEvents.SelectMany(e => e.ResponseHeaders).Where(h => h.HeaderName.Equals("set-cookie", StringComparison.OrdinalIgnoreCase)).All(h => !h.ValueCaptured));
        Assert.IsFalse(publicSurface.Contains(SandboxPassValue, StringComparison.Ordinal));
        Assert.IsFalse(publicSurface.Contains("sandbox-session-value", StringComparison.Ordinal));
        Assert.IsFalse(result.CookieValuesExposed);
        Assert.IsFalse(result.SessionStorageExposed);
        await activationService.CleanupAsync(activation, BrowserControlledProfileLifecycleState.CleanupRequested);
    }

    private static async Task<(BrowserVaultMinimalSandboxProvider Provider, BrowserVaultSecretReference UserReference, BrowserVaultSecretReference PassReference)> VaultSetupAsync()
    {
        var provider = new BrowserVaultMinimalSandboxProvider();
        var user = BrowserVaultMinimalM23Tests.Reference(BrowserSecretKind.Password);
        var pass = BrowserVaultMinimalM23Tests.Reference(BrowserSecretKind.Password);
        await provider.StoreSandboxAsync(BrowserVaultMinimalM23Tests.Store(user), SandboxUserValue);
        await provider.StoreSandboxAsync(BrowserVaultMinimalM23Tests.Store(pass), SandboxPassValue);
        return (provider, user, pass);
    }

    private static BrowserVerification FailedVerification()
    {
        var context = new BrowserTargetContext("run-auth", "chrome-cdp", "session-controlled", null, null, "target", "page", null, "main", null, new Uri("http://127.0.0.1/"), "Sandbox", 0, BrowserTargetContext.CreateLivenessToken("target", "main", 0), DateTimeOffset.UtcNow, null, null, null, "blocked", BrowserTargetSource.Cdp);
        return new BrowserVerification($"verification-{Guid.NewGuid():N}", "run-auth", "step-auth", "action-auth", context, new BrowserExpectedOutcome("dashboard visible", null, DashboardProof, null), null, null, BrowserVerificationStatus.Failed, 0, [], "blocked", DateTimeOffset.UtcNow, []);
    }

    private static string RequireLiveBrowser()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_CDP_LIVE_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("BrowserAuthenticatedSandbox CDP test is opt-in. Set ONEBRAIN_RUN_CDP_LIVE_TESTS=1.");
        var path = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (path is null)
            Assert.Inconclusive("Chrome/Edge executable is not available in this environment.");
        return path;
    }

    private static void RequireAuthSandbox()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_AUTH_SANDBOX_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("BrowserAuthenticatedSandbox live login test is opt-in. Set ONEBRAIN_RUN_AUTH_SANDBOX_TESTS=1.");
    }

    private sealed class BrowserAuthenticatedSandboxFixtureServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _loop;
        private readonly HashSet<string> _sessions = new(StringComparer.Ordinal);

        private BrowserAuthenticatedSandboxFixtureServer(HttpListener listener, int port)
        {
            _listener = listener;
            Port = port;
            _loop = Task.Run(ServeAsync);
        }

        public int Port { get; }

        public static Task<BrowserAuthenticatedSandboxFixtureServer> StartAsync()
        {
            var port = GetFreeTcpPort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return Task.FromResult(new BrowserAuthenticatedSandboxFixtureServer(listener, port));
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

        private async Task HandleAsync(HttpListenerContext context)
        {
            switch (context.Request.Url?.AbsolutePath ?? "/")
            {
                case "/login":
                    await Html(context, """
<!doctype html>
<html><head><title>Sandbox Login</title></head>
<body>
  <form method="post" action="/session">
    <input id="username" name="username" autocomplete="off" />
    <input id="passcode" name="passcode" type="password" autocomplete="off" />
    <button id="login-button" type="submit">login</button>
  </form>
</body></html>
""");
                    break;
                case "/session":
                    var body = await ReadBodyAsync(context.Request);
                    if (body.Contains($"username={Uri.EscapeDataString(SandboxUserValue)}", StringComparison.Ordinal) &&
                        body.Contains($"passcode={Uri.EscapeDataString(SandboxPassValue)}", StringComparison.Ordinal))
                    {
                        var sessionId = $"sandbox-session-value-{Guid.NewGuid():N}";
                        _sessions.Add(sessionId);
                        context.Response.StatusCode = 302;
                        context.Response.Headers["Location"] = "/dashboard";
                        context.Response.Headers["Set-Cookie"] = $"sandbox_session={sessionId}; HttpOnly; SameSite=Lax";
                        context.Response.Close();
                        return;
                    }

                    context.Response.StatusCode = 403;
                    await Text(context, "blocked", "text/plain");
                    break;
                case "/dashboard":
                    if (IsAuthenticated(context.Request))
                        await Html(context, $"<!doctype html><html><head><title>Sandbox Dashboard</title></head><body><main>{DashboardProof}</main><a href=\"/logout\">logout</a></body></html>");
                    else
                    {
                        context.Response.StatusCode = 401;
                        await Text(context, "not authenticated", "text/plain");
                    }
                    break;
                case "/logout":
                    context.Response.Headers["Set-Cookie"] = "sandbox_session=; Max-Age=0; HttpOnly";
                    await Text(context, "logged out", "text/plain");
                    break;
                default:
                    context.Response.StatusCode = 404;
                    await Text(context, "not found", "text/plain");
                    break;
            }
        }

        private bool IsAuthenticated(HttpListenerRequest request)
        {
            var value = request.Cookies["sandbox_session"]?.Value;
            return value is not null && _sessions.Contains(value);
        }

        private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return await reader.ReadToEndAsync();
        }

        private static Task Html(HttpListenerContext context, string html) => Text(context, html, "text/html; charset=utf-8");

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
