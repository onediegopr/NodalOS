namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserVaultThreatActorKind
{
    Core,
    Companion,
    Support,
    AdminDashboard,
    PublicApi,
    Diagnostics,
    AuditExport,
    CrossTenantActor,
    UnauthorizedWorker
}

public enum BrowserVaultThreatDecisionKind
{
    AllowedCoreOnly,
    Blocked,
    FailClosed
}

public sealed record BrowserVaultThreatRequest(
    string RequestId,
    BrowserVaultThreatActorKind ActorKind,
    string ActorTenantId,
    string TargetTenantId,
    string WorkerId,
    bool WorkerAuthorized,
    bool ProductiveVaultEntitlement,
    bool GatePassed,
    bool AttemptsRawSecretAccess,
    bool AttemptsPublicDtoSecret,
    bool AttemptsSerialization);

public sealed record BrowserVaultCoreOnlySecretHandle(
    string HandleId,
    BrowserSecretReference Reference,
    bool CoreOnly,
    bool PublicDto,
    bool Serializable,
    bool Exportable,
    string RedactedLabel);

public sealed record BrowserVaultThreatDecision(
    BrowserVaultThreatDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    BrowserVaultCoreOnlySecretHandle? Handle,
    bool RawSecretExposed,
    bool Redacted)
{
    public bool BlocksAccess => Decision is BrowserVaultThreatDecisionKind.Blocked or BrowserVaultThreatDecisionKind.FailClosed;
}
