using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivateLocalApiDiagnosticsM61Tests
{
    [TestMethod]
    public void NexaPrivateLocalApiDiagnosticsReportsRouteHealth()
    {
        var report = Report();

        Assert.IsTrue(report.RouteHealth.Any(route => route.Route == "/runtime/status" && route.Healthy));
    }

    [TestMethod]
    public void NexaPrivateLocalApiDiagnosticsReportsAuthTenantRateLimitDecisions()
    {
        var audit = Report().AuditEvents.Single(audit => audit.Route == "/runtime/status");

        Assert.AreEqual("auth-evaluated", audit.AuthStatus);
        Assert.AreEqual("tenant-scoped", audit.TenantDecision);
        Assert.AreEqual("rate-limit-evaluated", audit.RateLimitStatus);
    }

    [TestMethod]
    public void NexaPrivateLocalApiDiagnosticsDoesNotExposeSecrets()
    {
        var json = NexaLeakHardeningSerialization.ToSafeJson(Report());

        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(json));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NexaPrivateLocalApiDiagnosticsDoesNotExposeCookies()
    {
        var json = NexaLeakHardeningSerialization.ToSafeJson(Report());

        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NexaPrivateLocalApiDiagnosticsDoesNotExposeBodies()
    {
        var json = NexaLeakHardeningSerialization.ToSafeJson(Report());

        Assert.IsFalse(json.Contains("request body", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaPrivateLocalApiAuditEventIsRedacted()
    {
        Assert.IsTrue(Report().AuditEvents.All(audit => audit.Redacted));
    }

    [TestMethod]
    public void NexaPrivateLocalApiUsageReportIsTenantScoped()
    {
        var usage = Report().UsageReport;

        Assert.AreEqual("tenant-local", usage.TenantId);
        Assert.AreEqual("workspace-local", usage.WorkspaceId);
    }

    [TestMethod]
    public void NexaPrivateLocalApiIncidentCandidateDoesNotContainSensitiveData()
    {
        var report = Report(includeBlocked: true);

        Assert.IsTrue(report.IncidentCandidates.Any());
        Assert.IsTrue(report.IncidentCandidates.All(candidate => !candidate.ContainsSensitiveData && candidate.Redacted));
    }

    private static NexaPrivateLocalApiDiagnosticsReport Report(bool includeBlocked = false)
    {
        var api = new NexaPrivateLocalApiService();
        var samples = new List<(NexaPrivateLocalApiRequest, NexaPrivateLocalApiResponse, NexaPrivateLocalApiAuditEvent)>();
        var okRequest = Request("/runtime/status");
        var ok = api.Handle(okRequest);
        samples.Add((okRequest, ok.Response, ok.Audit));
        if (includeBlocked)
        {
            var blockedRequest = Request("/runtime/status", target: Tenant() with { TenantId = "tenant-other" });
            var blocked = api.Handle(blockedRequest);
            samples.Add((blockedRequest, blocked.Response, blocked.Audit));
        }

        return new NexaPrivateLocalApiDiagnosticsCollector().Collect(samples);
    }

    private static NexaPrivateLocalApiRequest Request(string path, NexaPublicApiTenantContext? target = null) =>
        new(NexaPrivateLocalApiMethod.Get, path, "owner-test-token", target ?? Tenant(), LicenseAllowsFeature: true, RequestContainsSecret: false, RequestContainsCookie: false, RequestContainsBody: false);

    private static NexaPublicApiTenantContext Tenant() =>
        new("tenant-local", "account-local", "org-local", "workspace-local", "worker-owner");
}
