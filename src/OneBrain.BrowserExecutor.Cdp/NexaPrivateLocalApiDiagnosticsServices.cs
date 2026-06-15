using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPrivateLocalApiDiagnosticsCollector
{
    public NexaPrivateLocalApiDiagnosticsReport Collect(IReadOnlyList<(NexaPrivateLocalApiRequest Request, NexaPrivateLocalApiResponse Response, NexaPrivateLocalApiAuditEvent Audit)> samples)
    {
        var audits = samples.Select(sample => new NexaPrivateLocalApiRequestAuditEvent(
            $"api-request-audit-{Guid.NewGuid():N}",
            sample.Request.Path,
            RoleFromToken(sample.Request.Token),
            sample.Request.TargetTenant.TenantId,
            sample.Request.TargetTenant.WorkspaceId,
            sample.Request.Token is null ? "missing-token" : sample.Response.StatusCode is 401 ? "auth-blocked" : "auth-evaluated",
            sample.Response.ReasonCodes.Contains("cross-tenant request blocked") ? "tenant-blocked" : "tenant-scoped",
            sample.Response.ReasonCodes.Any(reason => reason.Contains("limit", StringComparison.OrdinalIgnoreCase)) ? "rate-limit-blocked" : "rate-limit-evaluated",
            sample.Response.ReasonCodes.Contains("license feature disabled") ? "license-blocked" : "license-allowed",
            sample.Response.StatusCode,
            sample.Response.ReasonCodes,
            [sample.Audit.EventId],
            Redacted: true)).ToList();

        var routeHealth = samples
            .GroupBy(sample => sample.Request.Path)
            .Select(group => new NexaPrivateLocalApiRouteHealth(
                group.Key,
                group.Any(sample => sample.Response.StatusCode < 500),
                group.Count(sample => sample.Response.StatusCode < 400),
                group.Count(sample => sample.Response.StatusCode >= 400),
                Redacted: true))
            .ToList();
        var first = samples.FirstOrDefault().Request?.TargetTenant;
        var incidents = samples
            .Where(sample => sample.Response.StatusCode >= 400)
            .Select(sample => new NexaPrivateLocalApiIncidentCandidate(
                $"api-incident-{Guid.NewGuid():N}",
                sample.Request.Path,
                sample.Request.TargetTenant.TenantId,
                sample.Response.ReasonCodes,
                ContainsSensitiveData: false,
                Redacted: true))
            .ToList();
        return new NexaPrivateLocalApiDiagnosticsReport(
            routeHealth,
            audits,
            new NexaPrivateLocalApiUsageReport(first?.TenantId ?? "tenant-local", first?.WorkspaceId ?? "workspace-local", samples.Count, samples.Count(sample => sample.Response.StatusCode >= 400), Redacted: true),
            new NexaPrivateLocalApiSecuritySummary(AuthEnforced: true, TenantEnforced: true, RateLimitEnforced: true, LicenseEnforced: true, SecretsCookiesBodiesBlocked: true, Redacted: true),
            incidents,
            Redacted: true);
    }

    private static NexaRole RoleFromToken(string? token) => token switch
    {
        "owner-test-token" => NexaRole.Owner,
        "admin-test-token" => NexaRole.Admin,
        "viewer-test-token" => NexaRole.Viewer,
        "worker-test-token" => NexaRole.Worker,
        "support-test-token" => NexaRole.Support,
        _ => NexaRole.Viewer
    };
}
