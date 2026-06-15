using System.Text.RegularExpressions;

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
        BrowserSafeIdentifierValidator.RequireSafe(SecretId, nameof(SecretId), errors);
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
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProfileId, nameof(ProfileId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);
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
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SecretId, nameof(SecretId), errors);
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
        errors.AddRange(Decision.Request.Validate().Errors);
        errors.AddRange(Decision.AuditEvent.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public static partial class BrowserSafeIdentifierValidator
{
    public static void RequireSafe(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (BrowserCredentialRedactor.ContainsSecret(value) || JwtLikeIdentifierPattern().IsMatch(value))
            errors.Add($"{name} contains secret-like content.");
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex JwtLikeIdentifierPattern();
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

public enum BrowserProfileConsentAuthorityKind
{
    CorePolicy,
    UserViaCompanionIntent,
    AdminPolicy,
    TestHarness,
    Unknown
}

public sealed record BrowserProfileConsentRequest(
    string RequestId,
    string ProfileId,
    string SessionId,
    string CorrelationId,
    BrowserProfileConsentScope Scope,
    string RequestingActor,
    string ConsentChallengeId,
    string Owner,
    string Purpose,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool RedactionApplied)
{
    public bool IsExpired(DateTimeOffset now) => ExpiresAtUtc <= now;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProfileId, nameof(ProfileId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentChallengeId, nameof(ConsentChallengeId), errors);
        if (string.IsNullOrWhiteSpace(RequestingActor))
            errors.Add("RequestingActor is required.");
        if (string.IsNullOrWhiteSpace(ConsentChallengeId))
            errors.Add("ConsentChallengeId is required.");
        if (!RedactionApplied)
            errors.Add("Profile consent request requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(RequestingActor) ||
            BrowserCredentialRedactor.ContainsSecret(Owner) ||
            BrowserCredentialRedactor.ContainsSecret(Purpose))
            errors.Add("Profile consent request contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserProfileConsentAuditEvent(
    string EventId,
    string RequestId,
    string ProfileId,
    BrowserProfileConsentScope Scope,
    BrowserProfileConsentStatus Status,
    BrowserProfileConsentAuthorityKind AuthorityKind,
    DateTimeOffset CreatedAtUtc,
    string RedactedSummary,
    bool RedactionApplied)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProfileId, nameof(ProfileId), errors);
        if (!RedactionApplied)
            errors.Add("Profile consent audit requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedSummary))
            errors.Add("Profile consent audit contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserProfileConsentDecision(
    BrowserProfileConsentStatus Status,
    BrowserProfileConsentRequest Request,
    BrowserProfileConsentAuditEvent AuditEvent,
    string Message,
    string ApprovingActor,
    string ApprovalSource,
    BrowserProfileConsentAuthorityKind AuthorityKind,
    string ConsentProofRef,
    string ConsentChallengeId,
    bool CompanionAuthoritative,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? RevokedAtUtc)
{
    public bool AllowsRealProfile(DateTimeOffset now) =>
        Status == BrowserProfileConsentStatus.Granted &&
        !Request.IsExpired(now) &&
        RevokedAtUtc is null &&
        HasAuthoritativeProof();

    public bool HasAuthoritativeProof() =>
        !CompanionAuthoritative &&
        AuthorityKind is BrowserProfileConsentAuthorityKind.CorePolicy or BrowserProfileConsentAuthorityKind.AdminPolicy or BrowserProfileConsentAuthorityKind.TestHarness &&
        !string.IsNullOrWhiteSpace(ApprovingActor) &&
        !string.IsNullOrWhiteSpace(ApprovalSource) &&
        !string.IsNullOrWhiteSpace(ConsentProofRef) &&
        !string.IsNullOrWhiteSpace(ConsentChallengeId) &&
        ConsentChallengeId == Request.ConsentChallengeId;

    public ContractValidationResult Validate(DateTimeOffset now)
    {
        var errors = new List<string>();
        errors.AddRange(Request.Validate().Errors);
        errors.AddRange(AuditEvent.Validate().Errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentProofRef, nameof(ConsentProofRef), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentChallengeId, nameof(ConsentChallengeId), errors);
        if (Status == BrowserProfileConsentStatus.Granted && !AllowsRealProfile(now))
            errors.Add("Granted profile consent requires authoritative proof, trusted source, challenge binding, and non-companion authority.");
        if (BrowserCredentialRedactor.ContainsSecret(Message) ||
            BrowserCredentialRedactor.ContainsSecret(ApprovingActor) ||
            BrowserCredentialRedactor.ContainsSecret(ApprovalSource))
            errors.Add("Profile consent decision contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

