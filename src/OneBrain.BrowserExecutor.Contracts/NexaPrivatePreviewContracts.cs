namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPrivatePreviewLocalStatus
{
    Allowed,
    Blocked
}

public sealed record NexaPrivatePreviewLocalProfile(
    string ProfileId,
    NexaConfigurationProfileKind ConfigurationProfile,
    bool LocalMachineOnly,
    bool SingleTenant,
    bool SyntheticDataOnly,
    bool MockBillingOnly,
    bool MockEmailOnly,
    bool PublicApiListenerDisabled,
    bool PublicSaasActivationDisabled,
    bool SensitiveRealPilotDisabled,
    bool ProductiveRecorderReplayDisabled);

public sealed record NexaPrivatePreviewLocalSession(
    string SessionId,
    string TenantId,
    string OperatorId,
    DateTimeOffset StartedAtUtc,
    bool M51ExternalProofDeferred);

public sealed record NexaPrivatePreviewLocalReadiness(
    bool ConfigProfileCompatible,
    bool LicenseMockValid,
    bool AdminRuntimeAvailable,
    bool TenantGovernanceAvailable,
    bool DiagnosticsRedacted,
    bool AuditKeyCustodyAvailable,
    bool ProductShellRoutesAvailable,
    bool PublicApiDesignOnly,
    bool BillingEmailMockOnly,
    bool SensitiveFeaturesDisabled,
    bool LeakHardeningCompleted,
    bool SkippedTestsAuditCompleted);

public sealed record NexaPrivatePreviewLocalResult(
    NexaPrivatePreviewLocalStatus Status,
    NexaPrivatePreviewLocalProfile Profile,
    NexaPrivatePreviewLocalSession Session,
    NexaPrivatePreviewLocalReadiness Readiness,
    IReadOnlyList<string> Violations,
    bool Redacted)
{
    public bool Allowed => Status == NexaPrivatePreviewLocalStatus.Allowed && Violations.Count == 0 && Redacted;
}

public sealed record NexaPrivatePreviewOperationalChecklist(IReadOnlyList<string> Steps);
public sealed record NexaPrivatePreviewSupportProcedure(IReadOnlyList<string> Steps);
public sealed record NexaPrivatePreviewRollbackProcedure(IReadOnlyList<string> Steps, bool ManualOnly, bool ExecutesRollback);
public sealed record NexaPrivatePreviewKnownLimitations(IReadOnlyList<string> Items);

public sealed record NexaPrivatePreviewRunbook(
    string RunbookId,
    NexaPrivatePreviewOperationalChecklist OperationalChecklist,
    NexaPrivatePreviewSupportProcedure SupportProcedure,
    NexaPrivatePreviewRollbackProcedure RollbackProcedure,
    NexaPrivatePreviewKnownLimitations KnownLimitations,
    bool DeclaresM51Deferred,
    bool DeclaresNoPublicSaas,
    bool DeclaresNoRealBilling,
    bool DeclaresNoRealCredentials,
    bool DeclaresNoSensitiveSites,
    bool Redacted)
{
    public bool IsSafe =>
        Redacted &&
        DeclaresM51Deferred &&
        DeclaresNoPublicSaas &&
        DeclaresNoRealBilling &&
        DeclaresNoRealCredentials &&
        DeclaresNoSensitiveSites &&
        RollbackProcedure.ManualOnly &&
        !RollbackProcedure.ExecutesRollback &&
        !KnownLimitations.Items.Any(BrowserCredentialRedactor.ContainsSecret);
}
