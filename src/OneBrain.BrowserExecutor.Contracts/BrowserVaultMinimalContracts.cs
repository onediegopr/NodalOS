namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserVaultMinimalProviderKind
{
    Disabled,
    SandboxLocalEncrypted,
    WindowsDpapiCurrentUserFuture,
    WindowsCredentialManagerFuture,
    Unknown
}

public enum BrowserVaultAccessDecisionKind
{
    Allowed,
    Denied,
    RequiresConsent,
    FailClosed,
    UnknownSecret,
    Revoked,
    Disabled
}

public enum BrowserVaultMinimalOperationKind
{
    Store,
    Retrieve,
    Revoke,
    Rotate
}

public enum BrowserVaultSecretLifecycleState
{
    Active,
    Revoked,
    Rotated
}

public sealed record BrowserVaultSecretReference(
    string ReferenceId,
    BrowserSecretKind Kind,
    BrowserSecretScope Scope,
    string Owner,
    string Portal,
    BrowserVaultMinimalProviderKind ProviderKind,
    DateTimeOffset CreatedAtUtc,
    string RedactedLabel)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ReferenceId, nameof(ReferenceId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Owner, nameof(Owner), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Portal, nameof(Portal), errors);
        if (Kind == BrowserSecretKind.UnknownSensitiveSecret)
            errors.Add("Unknown vault references fail closed.");
        if (ProviderKind == BrowserVaultMinimalProviderKind.Unknown)
            errors.Add("Unknown vault provider fails closed.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedLabel))
            errors.Add("Vault reference label contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserVaultAccessPolicy(
    bool AllowSandboxProvider,
    bool RequireConsent,
    BrowserConsentCapability RequiredCapability,
    BrowserConsentScope RequiredConsentScope,
    BrowserVaultMinimalProviderKind RequiredProviderKind,
    bool RequirePhaseGatePassed,
    bool AllowCompanionExposure,
    bool AllowPublicValueReturn)
{
    public static BrowserVaultAccessPolicy SandboxRetrieval { get; } = new(
        AllowSandboxProvider: true,
        RequireConsent: true,
        RequiredCapability: BrowserConsentCapability.SecretRetrieval,
        RequiredConsentScope: BrowserConsentScope.Profile,
        RequiredProviderKind: BrowserVaultMinimalProviderKind.SandboxLocalEncrypted,
        RequirePhaseGatePassed: true,
        AllowCompanionExposure: false,
        AllowPublicValueReturn: false);
}

public sealed record BrowserVaultStoreRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserVaultSecretReference Reference,
    string Purpose,
    DateTimeOffset RequestedAtUtc)
{
    public ContractValidationResult Validate()
    {
        var errors = ValidateIds(RequestId, RunId, ActionId, CorrelationId, ProfileId, SessionId);
        errors.AddRange(Reference.Validate().Errors);
        if (BrowserCredentialRedactor.ContainsSecret(Purpose))
            errors.Add("Vault store purpose contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    internal static List<string> ValidateIds(params string[] ids)
    {
        var errors = new List<string>();
        foreach (var id in ids)
            BrowserSafeIdentifierValidator.RequireSafe(id, "vault id", errors);
        return errors;
    }
}

public sealed record BrowserVaultRetrieveRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserVaultSecretReference Reference,
    BrowserConsentGrant? Consent,
    BrowserRuntimePhaseCloseReport? PhaseGateReport,
    BrowserVaultAccessPolicy Policy,
    string Purpose,
    DateTimeOffset RequestedAtUtc)
{
    public ContractValidationResult Validate()
    {
        var errors = BrowserVaultStoreRequest.ValidateIds(RequestId, RunId, ActionId, CorrelationId, ProfileId, SessionId);
        errors.AddRange(Reference.Validate().Errors);
        if (BrowserCredentialRedactor.ContainsSecret(Purpose))
            errors.Add("Vault retrieve purpose contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserVaultRevocationRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserVaultSecretReference Reference,
    DateTimeOffset RequestedAtUtc);

public sealed record BrowserVaultRotationRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserVaultSecretReference Reference,
    DateTimeOffset RequestedAtUtc);

public sealed record BrowserVaultAuditEvent(
    string EventId,
    BrowserVaultMinimalOperationKind Operation,
    BrowserVaultAccessDecisionKind Decision,
    BrowserVaultSecretReference Reference,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    string Reason,
    DateTimeOffset CreatedAtUtc,
    bool RedactionApplied)
{
    public ContractValidationResult Validate()
    {
        var errors = BrowserVaultStoreRequest.ValidateIds(EventId, RunId, ActionId, CorrelationId, ProfileId, SessionId);
        errors.AddRange(Reference.Validate().Errors);
        if (!RedactionApplied)
            errors.Add("Vault audit requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("Vault audit contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserVaultStoreResult(
    BrowserVaultAccessDecisionKind Decision,
    BrowserVaultSecretReference? Reference,
    BrowserVaultAuditEvent AuditEvent,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted)
{
    public bool SecretValueReturned => false;
    public bool Allowed => Decision == BrowserVaultAccessDecisionKind.Allowed && Reference is not null && Redacted;
}

public sealed record BrowserVaultRetrieveResult(
    BrowserVaultAccessDecisionKind Decision,
    BrowserVaultSecretReference? Reference,
    BrowserVaultAuditEvent AuditEvent,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted)
{
    public bool SecretValueReturned => false;
    public bool AllowsCoreUse => Decision == BrowserVaultAccessDecisionKind.Allowed && Reference is not null && Redacted;
}

public sealed record BrowserVaultMutationResult(
    BrowserVaultAccessDecisionKind Decision,
    BrowserVaultSecretReference? Reference,
    BrowserVaultAuditEvent AuditEvent,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted)
{
    public bool SecretValueReturned => false;
}
