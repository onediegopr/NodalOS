namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPublicApiCategory
{
    Admin,
    Licensing,
    Onboarding,
    Diagnostics,
    AuditExport,
    RuntimeStatus,
    WorkflowRequest,
    FeatureFlags,
    Support
}

public enum NexaPublicApiOperation
{
    Query,
    Command,
    Export,
    SupportReadOnly
}

public enum NexaPublicApiExposureMode
{
    DesignOnly,
    NotPubliclyExposed,
    PublicNetworkExposed
}

public sealed record NexaPublicApiEndpoint(
    string EndpointId,
    NexaPublicApiCategory Category,
    NexaPublicApiOperation Operation,
    string RouteTemplate,
    bool DesignOnly,
    bool OpensNetworkListener)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EndpointId, nameof(EndpointId), errors);
        if (string.IsNullOrWhiteSpace(RouteTemplate))
            errors.Add("Public API route template is required.");
        if (!DesignOnly)
            errors.Add("Public API endpoint must remain design-only in M47.");
        if (OpensNetworkListener)
            errors.Add("Public API endpoint cannot open a network listener in M47.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaPublicApiBoundary(
    IReadOnlyList<NexaPublicApiEndpoint> Endpoints,
    NexaPublicApiExposureMode ExposureMode,
    bool NoNetworkListener,
    bool PubliclyExposed)
{
    public static NexaPublicApiBoundary DesignOnlyDefault() =>
        new(
            [
                new("api-admin", NexaPublicApiCategory.Admin, NexaPublicApiOperation.Command, "/api/admin/{command}", DesignOnly: true, OpensNetworkListener: false),
                new("api-licensing", NexaPublicApiCategory.Licensing, NexaPublicApiOperation.Query, "/api/licensing/{id}", DesignOnly: true, OpensNetworkListener: false),
                new("api-onboarding", NexaPublicApiCategory.Onboarding, NexaPublicApiOperation.Command, "/api/onboarding/{flow}", DesignOnly: true, OpensNetworkListener: false),
                new("api-diagnostics", NexaPublicApiCategory.Diagnostics, NexaPublicApiOperation.Query, "/api/diagnostics/{scope}", DesignOnly: true, OpensNetworkListener: false),
                new("api-audit-export", NexaPublicApiCategory.AuditExport, NexaPublicApiOperation.Export, "/api/audit/export", DesignOnly: true, OpensNetworkListener: false),
                new("api-runtime-status", NexaPublicApiCategory.RuntimeStatus, NexaPublicApiOperation.Query, "/api/runtime/status", DesignOnly: true, OpensNetworkListener: false),
                new("api-workflow-request", NexaPublicApiCategory.WorkflowRequest, NexaPublicApiOperation.Command, "/api/workflows/{workflowId}", DesignOnly: true, OpensNetworkListener: false),
                new("api-feature-flags", NexaPublicApiCategory.FeatureFlags, NexaPublicApiOperation.Command, "/api/features/{feature}", DesignOnly: true, OpensNetworkListener: false),
                new("api-support", NexaPublicApiCategory.Support, NexaPublicApiOperation.SupportReadOnly, "/api/support/{scope}", DesignOnly: true, OpensNetworkListener: false)
            ],
            NexaPublicApiExposureMode.DesignOnly,
            NoNetworkListener: true,
            PubliclyExposed: false);

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (ExposureMode == NexaPublicApiExposureMode.PublicNetworkExposed || PubliclyExposed)
            errors.Add("Public API cannot be publicly exposed in M47.");
        if (!NoNetworkListener)
            errors.Add("Public API cannot open a network listener in M47.");
        foreach (var endpoint in Endpoints)
            errors.AddRange(endpoint.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaPublicApiAuthContext(
    string ActorId,
    NexaRole Role,
    bool Authenticated);

public sealed record NexaPublicApiTenantContext(
    string TenantId,
    string AccountId,
    string OrganizationId,
    string WorkspaceId,
    string WorkerId);

public sealed record NexaPublicApiRequest(
    NexaPublicApiEndpoint Endpoint,
    NexaPublicApiAuthContext? AuthContext,
    NexaPublicApiTenantContext? ActorTenant,
    NexaPublicApiTenantContext? TargetTenant,
    NexaFeatureFlag RequestedFeature,
    bool ComplianceApproved,
    bool LicenseAllowsFeature,
    bool RequestContainsSecret,
    bool RequestContainsCookie,
    bool RequestContainsBody);

public sealed record NexaPublicApiResponse(
    bool Allowed,
    string Status,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyDictionary<string, string> RedactedDto,
    bool Redacted,
    bool ContainsSecret,
    bool ContainsCookie,
    bool ContainsBody);

public sealed record NexaPublicApiDecision(
    bool Allowed,
    IReadOnlyList<string> ReasonCodes,
    NexaPublicApiResponse Response);

public sealed record NexaPublicApiAuditEvent(
    string EventId,
    string TenantId,
    string EndpointId,
    bool Allowed,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(TenantId, nameof(TenantId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(EndpointId, nameof(EndpointId), errors);
        if (!Redacted)
            errors.Add("Public API audit must be redacted.");
        foreach (var reason in ReasonCodes)
        {
            if (BrowserCredentialRedactor.ContainsSecret(reason))
                errors.Add("Public API audit reason contains secret-like content.");
        }
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaPublicApiAuthPolicy(bool RequireAuthentication);
public sealed record NexaPublicApiTenantPolicy(bool BlockCrossTenant, bool RequireKnownTenant);
public sealed record NexaPublicApiPermissionPolicy(bool SupportCanAccessSecrets, bool WorkerCanManageLicenses);

public sealed record NexaPublicApiRateLimit(
    string LimitId,
    int BurstLimit,
    int DailyLimit,
    int MonthlyLimit);

public sealed record NexaPublicApiUsageCounter(
    string LimitId,
    int BurstCount,
    int DailyCount,
    int MonthlyCount);

public sealed record NexaPublicApiRateLimitDecision(
    bool Allowed,
    string Reason,
    bool Redacted);

public sealed record NexaPublicApiRateLimitPolicy(NexaPublicApiRateLimit Limit)
{
    public NexaPublicApiRateLimitDecision Evaluate(NexaPublicApiUsageCounter counter)
    {
        if (counter.BurstCount >= Limit.BurstLimit)
            return new(false, "burst limit exceeded", true);
        if (counter.DailyCount >= Limit.DailyLimit)
            return new(false, "daily limit exceeded", true);
        if (counter.MonthlyCount >= Limit.MonthlyLimit)
            return new(false, "monthly limit exceeded", true);
        return new(true, "rate limit allowed", true);
    }
}
