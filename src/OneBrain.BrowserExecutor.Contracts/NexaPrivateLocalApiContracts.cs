namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPrivateLocalApiMethod
{
    Get,
    Post
}

public enum NexaPrivateLocalApiAuthDecisionKind
{
    Allowed,
    MissingToken,
    UnknownToken,
    Blocked
}

public sealed record NexaPrivateLocalApiRoute(
    string RouteId,
    NexaPrivateLocalApiMethod Method,
    string Path,
    NexaRole MinimumRole,
    NexaFeatureFlag RequiredFeature,
    bool Mutation,
    bool SupportMetadataOnly);

public sealed record NexaPrivateLocalApiRouteCatalog(IReadOnlyList<NexaPrivateLocalApiRoute> Routes)
{
    public static NexaPrivateLocalApiRouteCatalog Default() =>
        new(
            [
                new("route-runtime-status", NexaPrivateLocalApiMethod.Get, "/runtime/status", NexaRole.Viewer, NexaFeatureFlag.BrowserRuntime, Mutation: false, SupportMetadataOnly: false),
                new("route-admin-dashboard", NexaPrivateLocalApiMethod.Get, "/admin/dashboard", NexaRole.Viewer, NexaFeatureFlag.AdminConsole, Mutation: false, SupportMetadataOnly: false),
                new("route-license-status", NexaPrivateLocalApiMethod.Get, "/license/status", NexaRole.Viewer, NexaFeatureFlag.AdminConsole, Mutation: false, SupportMetadataOnly: false),
                new("route-diagnostics-summary", NexaPrivateLocalApiMethod.Get, "/diagnostics/summary", NexaRole.Support, NexaFeatureFlag.AdminConsole, Mutation: false, SupportMetadataOnly: true),
                new("route-audit-export", NexaPrivateLocalApiMethod.Post, "/audit/export", NexaRole.Admin, NexaFeatureFlag.AdminConsole, Mutation: true, SupportMetadataOnly: false),
                new("route-onboarding-free-mock", NexaPrivateLocalApiMethod.Post, "/onboarding/free/mock", NexaRole.Admin, NexaFeatureFlag.AdminConsole, Mutation: true, SupportMetadataOnly: false),
                new("route-support-bundle-mock", NexaPrivateLocalApiMethod.Post, "/support/bundle/mock", NexaRole.Support, NexaFeatureFlag.AdminConsole, Mutation: true, SupportMetadataOnly: true)
            ]);
}

public sealed record NexaPrivateLocalApiAuthToken(string TokenValue, NexaRole Role, string ActorId, string TenantId, string WorkspaceId, string WorkerId);

public sealed record NexaPrivateLocalApiAuthContext(string ActorId, NexaRole Role, string TenantId, string WorkspaceId, string WorkerId, bool Authenticated);

public sealed record NexaPrivateLocalApiAuthPolicy(bool RequireToken, bool SupportCanAccessSecrets, bool ViewerCanMutate);

public sealed record NexaPrivateLocalApiAuthDecision(
    NexaPrivateLocalApiAuthDecisionKind Decision,
    NexaPrivateLocalApiAuthContext? Context,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted)
{
    public bool Allowed => Decision == NexaPrivateLocalApiAuthDecisionKind.Allowed;
}

public sealed record NexaPrivateLocalApiRateLimitPolicy(string PolicyId, int BurstLimit, int DailyLimit, int MonthlyLimit);
public sealed record NexaPrivateLocalApiRateLimitCounter(string TenantId, string WorkerId, string RouteId, NexaPlanKind Plan, int BurstCount, int DailyCount, int MonthlyCount);
public sealed record NexaPrivateLocalApiRateLimitDecision(bool Allowed, string Reason, bool Redacted);

public sealed record NexaPrivateLocalApiRequest(
    NexaPrivateLocalApiMethod Method,
    string Path,
    string? Token,
    NexaPublicApiTenantContext TargetTenant,
    bool LicenseAllowsFeature,
    bool RequestContainsSecret,
    bool RequestContainsCookie,
    bool RequestContainsBody,
    NexaPrivateLocalApiRateLimitCounter? RateLimitCounter = null);

public sealed record NexaPrivateLocalApiResponse(
    int StatusCode,
    IReadOnlyDictionary<string, string> Dto,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted,
    bool ContainsSecret,
    bool ContainsCookie,
    bool ContainsBody);

public sealed record NexaPrivateLocalApiAuditEvent(
    string EventId,
    string RouteId,
    string TenantId,
    string ActorId,
    int StatusCode,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record NexaPrivateLocalApiHost(
    bool InProcess,
    bool LoopbackOnly,
    bool PublicListenerEnabled,
    string BindAddress);
