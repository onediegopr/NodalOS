using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPublicApiBoundaryEvaluator
{
    public (NexaPublicApiDecision Decision, NexaPublicApiAuditEvent AuditEvent) Evaluate(NexaPublicApiBoundary boundary, NexaPublicApiRequest request, NexaPublicApiRateLimitDecision? rateLimit = null)
    {
        var reasons = new List<string>();
        reasons.AddRange(boundary.Validate().Errors);
        if (request.Endpoint.Validate().Errors.Count > 0)
            reasons.AddRange(request.Endpoint.Validate().Errors);
        if (request.AuthContext is null || !request.AuthContext.Authenticated)
            reasons.Add("missing auth context");
        if (request.ActorTenant is null || request.TargetTenant is null)
            reasons.Add("unknown tenant");
        else if (!SameTenant(request.ActorTenant, request.TargetTenant))
            reasons.Add("cross-tenant request blocked");
        if (request.AuthContext?.Role == NexaRole.Support && (request.RequestContainsSecret || request.RequestContainsCookie || request.RequestContainsBody))
            reasons.Add("support secret access blocked");
        if (request.RequestedFeature is NexaFeatureFlag.SensitiveRealPilot && !request.ComplianceApproved)
            reasons.Add("sensitive capability requires compliance");
        if (!request.LicenseAllowsFeature)
            reasons.Add("license feature disabled");
        if (request.RequestContainsSecret)
            reasons.Add("request contains secret-like content");
        if (request.RequestContainsCookie)
            reasons.Add("request contains cookie material");
        if (request.RequestContainsBody)
            reasons.Add("request contains body content");
        if (rateLimit is { Allowed: false })
            reasons.Add(rateLimit.Reason);

        var allowed = reasons.Count == 0;
        var response = new NexaPublicApiResponse(
            allowed,
            allowed ? "Allowed" : "Blocked",
            reasons,
            new Dictionary<string, string>
            {
                ["endpoint"] = request.Endpoint.EndpointId,
                ["feature"] = request.RequestedFeature.ToString(),
                ["tenant"] = request.TargetTenant?.TenantId ?? "[UNKNOWN]"
            },
            Redacted: true,
            ContainsSecret: false,
            ContainsCookie: false,
            ContainsBody: false);

        var audit = new NexaPublicApiAuditEvent(
            $"api-audit-{Guid.NewGuid():N}",
            request.TargetTenant?.TenantId ?? "unknown-tenant",
            request.Endpoint.EndpointId,
            allowed,
            reasons,
            Redacted: true);

        return (new NexaPublicApiDecision(allowed, reasons, response), audit);
    }

    private static bool SameTenant(NexaPublicApiTenantContext actor, NexaPublicApiTenantContext target) =>
        string.Equals(actor.TenantId, target.TenantId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(actor.AccountId, target.AccountId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(actor.OrganizationId, target.OrganizationId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(actor.WorkspaceId, target.WorkspaceId, StringComparison.OrdinalIgnoreCase);
}
