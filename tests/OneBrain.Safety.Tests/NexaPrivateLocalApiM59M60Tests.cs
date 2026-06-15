using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivateLocalApiM59M60Tests
{
    [TestMethod]
    public void NexaPrivateLocalApiDefinesExpectedRoutes()
    {
        var paths = NexaPrivateLocalApiRouteCatalog.Default().Routes.Select(route => route.Path).ToHashSet();

        CollectionAssert.IsSubsetOf(new[] { "/runtime/status", "/admin/dashboard", "/license/status", "/diagnostics/summary", "/audit/export", "/onboarding/free/mock", "/support/bundle/mock" }, paths.ToList());
    }

    [TestMethod]
    public void NexaPrivateLocalApiDoesNotExposePublicListener()
    {
        var host = new NexaPrivateLocalApiService().Host;

        Assert.IsTrue(host.InProcess);
        Assert.IsTrue(host.LoopbackOnly);
        Assert.IsFalse(host.PublicListenerEnabled);
    }

    [TestMethod]
    public void NexaPrivateLocalApiStatusIsRedacted()
    {
        var response = Handle(Request("/runtime/status")).Response;

        Assert.AreEqual(200, response.StatusCode);
        Assert.IsTrue(response.Redacted);
    }

    [TestMethod]
    public void NexaPrivateLocalApiAdminDashboardIsTenantScoped()
    {
        var response = Handle(Request("/admin/dashboard")).Response;

        Assert.AreEqual("tenant-local", response.Dto["tenant"]);
        Assert.AreEqual("workspace-local", response.Dto["workspace"]);
    }

    [TestMethod]
    public void NexaPrivateLocalApiLicenseStatusIsLicenseAware()
    {
        var response = Handle(Request("/license/status", licenseAllows: false)).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "license feature disabled");
    }

    [TestMethod]
    public void NexaPrivateLocalApiDiagnosticsSummaryDoesNotExposeSecrets()
    {
        var response = Handle(Request("/diagnostics/summary", token: "support-test-token")).Response;
        var json = NexaLeakHardeningSerialization.ToSafeJson(response);

        Assert.AreEqual(200, response.StatusCode);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(json));
        Assert.IsFalse(response.ContainsSecret);
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuditExportIsRedacted()
    {
        var response = Handle(Request("/audit/export", NexaPrivateLocalApiMethod.Post, token: "admin-test-token")).Response;

        Assert.AreEqual(200, response.StatusCode);
        Assert.IsTrue(response.Redacted);
    }

    [TestMethod]
    public void NexaPrivateLocalApiSupportBundleIsMetadataOnly()
    {
        var response = Handle(Request("/support/bundle/mock", NexaPrivateLocalApiMethod.Post, token: "support-test-token")).Response;

        Assert.AreEqual("metadata-only", response.Dto["mode"]);
        Assert.IsFalse(response.ContainsBody);
    }

    [TestMethod]
    public void NexaPrivateLocalApiRejectsUnknownRoute()
    {
        var response = Handle(Request("/unknown")).Response;

        Assert.AreEqual(404, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "unknown route");
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuthRejectsMissingToken()
    {
        var decision = new NexaPrivateLocalApiAuthService().Authenticate(null, new NexaPrivateLocalApiAuthPolicy(true, false, false));

        Assert.AreEqual(NexaPrivateLocalApiAuthDecisionKind.MissingToken, decision.Decision);
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuthRejectsUnknownToken()
    {
        var decision = new NexaPrivateLocalApiAuthService().Authenticate("unknown-token", new NexaPrivateLocalApiAuthPolicy(true, false, false));

        Assert.AreEqual(NexaPrivateLocalApiAuthDecisionKind.UnknownToken, decision.Decision);
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuthAllowsOwnerDashboard()
    {
        var response = Handle(Request("/admin/dashboard", token: "owner-test-token")).Response;

        Assert.AreEqual(200, response.StatusCode);
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuthBlocksViewerMutation()
    {
        var response = Handle(Request("/audit/export", NexaPrivateLocalApiMethod.Post, token: "viewer-test-token")).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "viewer mutation blocked");
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuthBlocksSupportSecrets()
    {
        var response = Handle(Request("/diagnostics/summary", token: "support-test-token", containsSecret: true)).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "support secret access blocked");
    }

    [TestMethod]
    public void NexaPrivateLocalApiTenantBlocksCrossTenantAccess()
    {
        var response = Handle(Request("/admin/dashboard", target: Tenant() with { TenantId = "tenant-other" })).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "cross-tenant request blocked");
    }

    [TestMethod]
    public void NexaPrivateLocalApiTenantBlocksUnknownTenant()
    {
        var response = Handle(Request("/admin/dashboard", target: Tenant() with { TenantId = "" })).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "unknown tenant");
    }

    [TestMethod]
    public void NexaPrivateLocalApiTenantBlocksUnauthorizedWorker()
    {
        var response = Handle(Request("/runtime/status", token: "worker-test-token", target: Tenant() with { WorkerId = "worker-other" })).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "worker unauthorized");
    }

    [TestMethod]
    public void NexaPrivateLocalApiRateLimitAllowsWithinLimit()
    {
        var decision = new NexaPrivateLocalApiRateLimitEvaluator().Evaluate(new NexaPrivateLocalApiRateLimitPolicy("local-api", 5, 100, 1000), new NexaPrivateLocalApiRateLimitCounter("tenant-local", "worker-owner", "route-runtime-status", NexaPlanKind.Trial, 1, 1, 1));

        Assert.IsTrue(decision.Allowed);
    }

    [TestMethod]
    public void NexaPrivateLocalApiRateLimitBlocksWhenExceeded()
    {
        var response = Handle(Request("/runtime/status", counter: new NexaPrivateLocalApiRateLimitCounter("tenant-local", "worker-owner", "route-runtime-status", NexaPlanKind.Trial, 5, 0, 0))).Response;

        Assert.AreEqual(403, response.StatusCode);
        CollectionAssert.Contains(response.ReasonCodes.ToList(), "burst limit exceeded");
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuditEventIsRedacted()
    {
        var audit = Handle(Request("/runtime/status")).Audit;

        Assert.IsTrue(audit.Redacted);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(NexaLeakHardeningSerialization.ToSafeJson(audit)));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithPrivateLocalApi()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State());

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
        CollectionAssert.Contains(report.PassedChecks.ToList(), "private local api safe");
        CollectionAssert.Contains(report.PassedChecks.ToList(), "client credential readiness safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenPrivateLocalApiBindsPublicNetwork()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State() with { PrivateLocalApiBindsPublicNetwork = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "private local api safe");
    }

    private static (NexaPrivateLocalApiResponse Response, NexaPrivateLocalApiAuditEvent Audit) Handle(NexaPrivateLocalApiRequest request) =>
        new NexaPrivateLocalApiService().Handle(request);

    private static NexaPrivateLocalApiRequest Request(
        string path,
        NexaPrivateLocalApiMethod method = NexaPrivateLocalApiMethod.Get,
        string token = "owner-test-token",
        NexaPublicApiTenantContext? target = null,
        bool licenseAllows = true,
        bool containsSecret = false,
        NexaPrivateLocalApiRateLimitCounter? counter = null) =>
        new(method, path, token, target ?? Tenant(), licenseAllows, containsSecret, RequestContainsCookie: false, RequestContainsBody: false, counter);

    private static NexaPublicApiTenantContext Tenant() =>
        new("tenant-local", "account-local", "org-local", "workspace-local", "worker-owner");

    private static BrowserRuntimeObservedState State() =>
        BrowserVaultThreatLifecycleM56M57Tests.StateForM60();
}
