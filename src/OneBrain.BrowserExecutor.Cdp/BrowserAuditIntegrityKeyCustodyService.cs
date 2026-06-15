using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserAuditIntegrityKeyCustodyService
{
    public BrowserAuditIntegrityKeyHealthCheck EvaluateForProfile(NexaConfigurationProfileKind profile, IBrowserAuditLedgerIntegrityProvider? provider)
    {
        if (provider is null)
            return new BrowserAuditIntegrityKeyHealthCheck(BrowserAuditIntegrityKeyProviderKind.Disabled, "missing", 0, BrowserAuditIntegrityKeyStatus.Unavailable, Healthy: false, RawKeyExposed: false, "audit integrity key provider required");

        var health = provider.HealthCheck();
        if (!health.Healthy)
            return health;

        if (health.ProviderKind == BrowserAuditIntegrityKeyProviderKind.DevFixtureExplicit &&
            profile is NexaConfigurationProfileKind.ProductionLocked or NexaConfigurationProfileKind.EnterpriseControlled)
            return health with { Healthy = false, Status = BrowserAuditIntegrityKeyStatus.Unavailable, Reason = "dev fixture audit key is prohibited for production/enterprise profiles" };

        if (profile is NexaConfigurationProfileKind.LocalSandbox or NexaConfigurationProfileKind.InternalPreview or NexaConfigurationProfileKind.PrivateBeta &&
            health.ProviderKind == BrowserAuditIntegrityKeyProviderKind.Disabled)
            return health with { Healthy = false, Reason = "local/private profiles require explicit audit key provider" };

        if (profile is NexaConfigurationProfileKind.Development or NexaConfigurationProfileKind.Test)
            return health;

        if (health.ProviderKind is BrowserAuditIntegrityKeyProviderKind.OsBackedDpapiCurrentUser or BrowserAuditIntegrityKeyProviderKind.ExternalFuture)
            return health;

        return health with { Healthy = false, Status = BrowserAuditIntegrityKeyStatus.Unavailable, Reason = "audit key provider not allowed for profile" };
    }
}
