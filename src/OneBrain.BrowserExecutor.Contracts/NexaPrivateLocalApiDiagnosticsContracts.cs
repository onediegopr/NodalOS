namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NexaPrivateLocalApiRequestAuditEvent(
    string EventId,
    string Route,
    NexaRole ActorRole,
    string TenantId,
    string WorkspaceId,
    string AuthStatus,
    string TenantDecision,
    string RateLimitStatus,
    string LicenseDecision,
    int ResponseStatus,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<string> AuditRefs,
    bool Redacted);

public sealed record NexaPrivateLocalApiRouteHealth(string Route, bool Healthy, int SuccessCount, int FailureCount, bool Redacted);

public sealed record NexaPrivateLocalApiUsageReport(string TenantId, string WorkspaceId, int RequestCount, int BlockedCount, bool Redacted);

public sealed record NexaPrivateLocalApiSecuritySummary(
    bool AuthEnforced,
    bool TenantEnforced,
    bool RateLimitEnforced,
    bool LicenseEnforced,
    bool SecretsCookiesBodiesBlocked,
    bool Redacted);

public sealed record NexaPrivateLocalApiIncidentCandidate(
    string IncidentId,
    string Route,
    string TenantId,
    IReadOnlyList<string> ReasonCodes,
    bool ContainsSensitiveData,
    bool Redacted);

public sealed record NexaPrivateLocalApiDiagnosticsReport(
    IReadOnlyList<NexaPrivateLocalApiRouteHealth> RouteHealth,
    IReadOnlyList<NexaPrivateLocalApiRequestAuditEvent> AuditEvents,
    NexaPrivateLocalApiUsageReport UsageReport,
    NexaPrivateLocalApiSecuritySummary SecuritySummary,
    IReadOnlyList<NexaPrivateLocalApiIncidentCandidate> IncidentCandidates,
    bool Redacted)
{
    public bool IsSafe =>
        Redacted &&
        RouteHealth.All(route => route.Redacted) &&
        AuditEvents.All(audit => audit.Redacted && !BrowserCredentialRedactor.ContainsSecret(string.Join(' ', audit.ReasonCodes))) &&
        UsageReport.Redacted &&
        SecuritySummary.Redacted &&
        SecuritySummary.SecretsCookiesBodiesBlocked &&
        IncidentCandidates.All(candidate => candidate.Redacted && !candidate.ContainsSensitiveData);
}
