using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPrivateLocalApiAuthService
{
    private readonly IReadOnlyDictionary<string, NexaPrivateLocalApiAuthToken> _tokens;

    public NexaPrivateLocalApiAuthService(IReadOnlyDictionary<string, NexaPrivateLocalApiAuthToken>? tokens = null)
    {
        _tokens = tokens ?? DefaultTokens();
    }

    public NexaPrivateLocalApiAuthDecision Authenticate(string? token, NexaPrivateLocalApiAuthPolicy policy)
    {
        if (policy.RequireToken && string.IsNullOrWhiteSpace(token))
            return new(NexaPrivateLocalApiAuthDecisionKind.MissingToken, null, ["missing auth token"], Redacted: true);
        if (token is null || !_tokens.TryGetValue(token, out var stored))
            return new(NexaPrivateLocalApiAuthDecisionKind.UnknownToken, null, ["unknown auth token"], Redacted: true);
        return new(
            NexaPrivateLocalApiAuthDecisionKind.Allowed,
            new NexaPrivateLocalApiAuthContext(stored.ActorId, stored.Role, stored.TenantId, stored.WorkspaceId, stored.WorkerId, Authenticated: true),
            ["authenticated"],
            Redacted: true);
    }

    public static IReadOnlyDictionary<string, NexaPrivateLocalApiAuthToken> DefaultTokens() =>
        new Dictionary<string, NexaPrivateLocalApiAuthToken>(StringComparer.Ordinal)
        {
            ["owner-test-token"] = new("owner-test-token", NexaRole.Owner, "owner-local", "tenant-local", "workspace-local", "worker-owner"),
            ["admin-test-token"] = new("admin-test-token", NexaRole.Admin, "admin-local", "tenant-local", "workspace-local", "worker-admin"),
            ["viewer-test-token"] = new("viewer-test-token", NexaRole.Viewer, "viewer-local", "tenant-local", "workspace-local", "worker-viewer"),
            ["worker-test-token"] = new("worker-test-token", NexaRole.Worker, "worker-local", "tenant-local", "workspace-local", "worker-local"),
            ["support-test-token"] = new("support-test-token", NexaRole.Support, "support-local", "tenant-local", "workspace-local", "worker-support")
        };
}

public sealed class NexaPrivateLocalApiRateLimitEvaluator
{
    public NexaPrivateLocalApiRateLimitDecision Evaluate(NexaPrivateLocalApiRateLimitPolicy policy, NexaPrivateLocalApiRateLimitCounter counter)
    {
        if (counter.BurstCount >= policy.BurstLimit)
            return new(false, "burst limit exceeded", Redacted: true);
        if (counter.DailyCount >= policy.DailyLimit)
            return new(false, "daily limit exceeded", Redacted: true);
        if (counter.MonthlyCount >= policy.MonthlyLimit)
            return new(false, "monthly limit exceeded", Redacted: true);
        return new(true, "rate limit allowed", Redacted: true);
    }
}

public sealed class NexaPrivateLocalApiService
{
    private readonly NexaPrivateLocalApiRouteCatalog _catalog;
    private readonly NexaPrivateLocalApiAuthService _auth;
    private readonly NexaPrivateLocalApiRateLimitEvaluator _rateLimit = new();

    public NexaPrivateLocalApiService(NexaPrivateLocalApiRouteCatalog? catalog = null, NexaPrivateLocalApiAuthService? auth = null)
    {
        _catalog = catalog ?? NexaPrivateLocalApiRouteCatalog.Default();
        _auth = auth ?? new NexaPrivateLocalApiAuthService();
    }

    public NexaPrivateLocalApiHost Host { get; } = new(InProcess: true, LoopbackOnly: true, PublicListenerEnabled: false, BindAddress: "in-process");

    public (NexaPrivateLocalApiResponse Response, NexaPrivateLocalApiAuditEvent Audit) Handle(NexaPrivateLocalApiRequest request)
    {
        var reasons = new List<string>();
        var route = _catalog.Routes.SingleOrDefault(route => route.Method == request.Method && string.Equals(route.Path, request.Path, StringComparison.OrdinalIgnoreCase));
        if (route is null)
            return Block("unknown-route", "unknown", "unknown", 404, ["unknown route"]);

        var auth = _auth.Authenticate(request.Token, new NexaPrivateLocalApiAuthPolicy(RequireToken: true, SupportCanAccessSecrets: false, ViewerCanMutate: false));
        reasons.AddRange(auth.ReasonCodes.Where(reason => reason != "authenticated"));
        if (!auth.Allowed || auth.Context is null)
            return Block(route.RouteId, request.TargetTenant.TenantId, "unknown", 401, reasons);

        if (!SameTenant(auth.Context, request.TargetTenant))
            reasons.Add("cross-tenant request blocked");
        if (string.IsNullOrWhiteSpace(request.TargetTenant.TenantId))
            reasons.Add("unknown tenant");
        if (route.Mutation && auth.Context.Role == NexaRole.Viewer)
            reasons.Add("viewer mutation blocked");
        if (auth.Context.Role == NexaRole.Worker && !string.Equals(auth.Context.WorkerId, request.TargetTenant.WorkerId, StringComparison.OrdinalIgnoreCase))
            reasons.Add("worker unauthorized");
        if (auth.Context.Role == NexaRole.Support && (request.RequestContainsSecret || request.RequestContainsCookie || request.RequestContainsBody))
            reasons.Add("support secret access blocked");
        if (!request.LicenseAllowsFeature)
            reasons.Add("license feature disabled");
        if (request.RequestContainsSecret)
            reasons.Add("request contains secret-like content");
        if (request.RequestContainsCookie)
            reasons.Add("request contains cookie material");
        if (request.RequestContainsBody)
            reasons.Add("request body content blocked");
        if (request.RateLimitCounter is not null)
        {
            var rate = _rateLimit.Evaluate(new NexaPrivateLocalApiRateLimitPolicy("local-api-default", BurstLimit: 5, DailyLimit: 100, MonthlyLimit: 1000), request.RateLimitCounter);
            if (!rate.Allowed)
                reasons.Add(rate.Reason);
        }

        if (reasons.Count > 0)
            return Block(route.RouteId, request.TargetTenant.TenantId, auth.Context.ActorId, 403, reasons);

        var dto = Dto(route, request.TargetTenant);
        var response = new NexaPrivateLocalApiResponse(200, dto, ["allowed"], Redacted: true, ContainsSecret: false, ContainsCookie: false, ContainsBody: false);
        var audit = new NexaPrivateLocalApiAuditEvent($"local-api-audit-{Guid.NewGuid():N}", route.RouteId, request.TargetTenant.TenantId, auth.Context.ActorId, 200, ["allowed"], Redacted: true);
        return (response, audit);
    }

    private static IReadOnlyDictionary<string, string> Dto(NexaPrivateLocalApiRoute route, NexaPublicApiTenantContext tenant) =>
        new Dictionary<string, string>
        {
            ["route"] = route.RouteId,
            ["tenant"] = tenant.TenantId,
            ["workspace"] = tenant.WorkspaceId,
            ["status"] = "redacted-local-private-api",
            ["mode"] = route.SupportMetadataOnly ? "metadata-only" : "redacted"
        };

    private static (NexaPrivateLocalApiResponse Response, NexaPrivateLocalApiAuditEvent Audit) Block(string routeId, string tenantId, string actorId, int status, IReadOnlyList<string> reasons)
    {
        var response = new NexaPrivateLocalApiResponse(status, new Dictionary<string, string> { ["status"] = "blocked" }, reasons, Redacted: true, ContainsSecret: false, ContainsCookie: false, ContainsBody: false);
        var audit = new NexaPrivateLocalApiAuditEvent($"local-api-audit-{Guid.NewGuid():N}", routeId, string.IsNullOrWhiteSpace(tenantId) ? "unknown-tenant" : tenantId, actorId, status, reasons, Redacted: true);
        return (response, audit);
    }

    private static bool SameTenant(NexaPrivateLocalApiAuthContext actor, NexaPublicApiTenantContext target) =>
        string.Equals(actor.TenantId, target.TenantId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(actor.WorkspaceId, target.WorkspaceId, StringComparison.OrdinalIgnoreCase);
}
