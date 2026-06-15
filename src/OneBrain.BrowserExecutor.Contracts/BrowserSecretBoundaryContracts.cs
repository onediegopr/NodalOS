namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserSecretKind
{
    Password,
    Otp,
    AccessToken,
    RefreshToken,
    IdToken,
    ApiKey,
    Cookie,
    SessionCookie,
    ClientCertificate,
    ClaveFiscal,
    BankCredential,
    UnknownSensitiveSecret
}

public enum BrowserSecretScope
{
    Tenant,
    Company,
    Person,
    Worker,
    Portal,
    Profile,
    Session,
    Recipe,
    Runtime,
    Temporary
}

public enum BrowserSecretAccessDecisionKind
{
    Allowed,
    Denied,
    RequiresHuman,
    RequiresApproval,
    RequiresVault,
    RequiresConsent,
    FailClosed
}

public enum BrowserSecretUsageIntent
{
    ReadOnlyReference,
    FillCredential,
    SubmitCredential,
    RefreshSession,
    AttachCookie,
    AttachClientCertificate,
    HumanHandoff,
    Diagnostics,
    Unknown
}

public sealed record BrowserSecretReference(
    string SecretId,
    BrowserSecretKind Kind,
    BrowserSecretScope Scope,
    string Owner,
    string Portal,
    DateTimeOffset CreatedAtUtc,
    string RedactedLabel)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(SecretId, nameof(SecretId), errors);
        Require(Owner, nameof(Owner), errors);
        Require(RedactedLabel, nameof(RedactedLabel), errors);
        if (Kind == BrowserSecretKind.UnknownSensitiveSecret)
            errors.Add("Unknown secret references cannot be used directly.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedLabel) ||
            BrowserCredentialRedactor.ContainsSecret(Owner) ||
            BrowserCredentialRedactor.ContainsSecret(Portal))
            errors.Add("Secret reference contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }
}

public sealed record BrowserSecretAccessPolicy(
    bool DenyByDefault,
    IReadOnlySet<BrowserSecretKind> AllowedKinds,
    IReadOnlySet<BrowserSecretScope> AllowedScopes,
    bool AllowCredentialFill,
    bool AllowCookieAccess,
    bool RequiresHumanForPassword,
    bool RequiresApprovalForSensitiveSubmit,
    bool AllowSyntheticTestSecretsOnly)
{
    public static BrowserSecretAccessPolicy DenyAll { get; } = new(
        DenyByDefault: true,
        AllowedKinds: new HashSet<BrowserSecretKind>(),
        AllowedScopes: new HashSet<BrowserSecretScope>(),
        AllowCredentialFill: false,
        AllowCookieAccess: false,
        RequiresHumanForPassword: true,
        RequiresApprovalForSensitiveSubmit: true,
        AllowSyntheticTestSecretsOnly: false);
}

public sealed record BrowserSecretAccessRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserSecretReference Secret,
    BrowserSecretUsageIntent Intent,
    DateTimeOffset RequestedAtUtc,
    string Reason)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(RequestId, nameof(RequestId), errors);
        Require(RunId, nameof(RunId), errors);
        Require(ActionId, nameof(ActionId), errors);
        Require(CorrelationId, nameof(CorrelationId), errors);
        Require(ProfileId, nameof(ProfileId), errors);
        Require(SessionId, nameof(SessionId), errors);
        errors.AddRange(Secret.Validate().Errors);
        if (Intent == BrowserSecretUsageIntent.Unknown)
            errors.Add("Unknown secret usage intent must fail closed.");
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("Secret access reason contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }
}

public sealed record BrowserSecretAuditEvent(
    string EventId,
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string SecretId,
    BrowserSecretKind Kind,
    BrowserSecretScope Scope,
    BrowserSecretUsageIntent Intent,
    BrowserSecretAccessDecisionKind Decision,
    DateTimeOffset CreatedAtUtc,
    string RedactedSummary,
    bool RedactionApplied)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(EventId))
            errors.Add("EventId is required.");
        if (!RedactionApplied)
            errors.Add("Secret audit event requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedSummary))
            errors.Add("Secret audit event contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSecretAccessDecision(
    BrowserSecretAccessDecisionKind Decision,
    string Message,
    BrowserSecretAccessRequest Request,
    BrowserSecretAuditEvent AuditEvent)
{
    public bool AllowsAccess => Decision == BrowserSecretAccessDecisionKind.Allowed;
    public bool BlocksAccess => Decision is BrowserSecretAccessDecisionKind.Denied or BrowserSecretAccessDecisionKind.FailClosed;
    public bool RequiresHuman => Decision == BrowserSecretAccessDecisionKind.RequiresHuman;
    public bool RequiresConsent => Decision == BrowserSecretAccessDecisionKind.RequiresConsent;
}

public sealed record BrowserSecretVaultResult(
    BrowserSecretAccessDecision Decision,
    BrowserSecretReference? Reference,
    string Diagnostic,
    bool RedactionApplied)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (!RedactionApplied)
            errors.Add("Vault result requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(Diagnostic))
            errors.Add("Vault result diagnostic contains unredacted secret-like content.");
        if (Reference is not null)
            errors.AddRange(Reference.Validate().Errors);
        errors.AddRange(Decision.AuditEvent.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public interface IBrowserSecretVault
{
    Task<BrowserSecretVaultResult> RequestAccessAsync(BrowserSecretAccessRequest request, BrowserSecretAccessPolicy policy, CancellationToken cancellationToken = default);
    IReadOnlyList<BrowserSecretAuditEvent> AuditEvents { get; }
}

public enum BrowserProfileConsentScope
{
    Tenant,
    Company,
    Person,
    Worker,
    Portal,
    Profile,
    Session,
    Runtime
}

public enum BrowserProfileConsentStatus
{
    NotRequested,
    Requested,
    Granted,
    Denied,
    Expired,
    Revoked,
    Invalid
}

public sealed record BrowserProfileConsentRequest(
    string RequestId,
    string ProfileId,
    string SessionId,
    string CorrelationId,
    BrowserProfileConsentScope Scope,
    string Owner,
    string Purpose,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool RedactionApplied)
{
    public bool IsExpired(DateTimeOffset now) => ExpiresAtUtc <= now;
}

public sealed record BrowserProfileConsentAuditEvent(
    string EventId,
    string RequestId,
    string ProfileId,
    BrowserProfileConsentScope Scope,
    BrowserProfileConsentStatus Status,
    DateTimeOffset CreatedAtUtc,
    string RedactedSummary,
    bool RedactionApplied);

public sealed record BrowserProfileConsentDecision(
    BrowserProfileConsentStatus Status,
    BrowserProfileConsentRequest Request,
    BrowserProfileConsentAuditEvent AuditEvent,
    string Message)
{
    public bool AllowsRealProfile(DateTimeOffset now) =>
        Status == BrowserProfileConsentStatus.Granted && !Request.IsExpired(now);
}

