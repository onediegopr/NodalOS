namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserProductiveVaultProviderKind
{
    Null,
    InMemoryTestOnly,
    WindowsDpapi,
    WindowsCredentialManager,
    ExternalVaultFuture,
    Unsupported
}

public enum BrowserProductiveVaultCapability
{
    StoreReference,
    RetrieveReference,
    UseReference,
    RotateReference,
    DeleteReference,
    ExportSecret,
    OsBackedStorage,
    SyntheticTestOnly
}

public enum BrowserProductiveVaultDecisionKind
{
    Allowed,
    Denied,
    RequiresConsent,
    RequiresApproval,
    FailClosed,
    Unsupported,
    Unconfigured
}

public enum BrowserVaultOperationKind
{
    Storage,
    Retrieval,
    Use,
    Rotation,
    Deletion,
    Export
}

public enum BrowserVaultConsentType
{
    ProfileRealConsent,
    SecretStorageConsent,
    SecretRetrievalConsent,
    SecretUseConsent,
    CredentialAutofillConsent,
    CookieAccessConsent,
    SecretDeletionConsent,
    SecretRotationConsent,
    SecretExportConsent
}

public enum BrowserVaultConsentStatus
{
    NotRequested,
    Requested,
    Granted,
    Denied,
    Expired,
    Revoked,
    Invalid
}

public enum BrowserVaultConsentScope
{
    Tenant,
    Company,
    Person,
    Worker,
    Portal,
    Profile,
    Session,
    Secret,
    Runtime,
    Temporary
}

public enum BrowserVaultConsentAuthorityKind
{
    CorePolicy,
    AdminPolicy,
    TestHarness,
    UserViaCompanionIntent,
    Unknown
}

public sealed record BrowserProductiveVaultConfiguration(
    BrowserProductiveVaultProviderKind ProviderKind,
    bool IsConfigured,
    bool EnableOsBackedStorage,
    bool AllowSyntheticTestMode,
    string ConfigurationId,
    string ProviderInstanceId)
{
    public static BrowserProductiveVaultConfiguration Null { get; } = new(
        BrowserProductiveVaultProviderKind.Null,
        IsConfigured: false,
        EnableOsBackedStorage: false,
        AllowSyntheticTestMode: false,
        ConfigurationId: "vault-config-null",
        ProviderInstanceId: "vault-provider-null");

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ConfigurationId, nameof(ConfigurationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProviderInstanceId, nameof(ProviderInstanceId), errors);
        if (ProviderKind is BrowserProductiveVaultProviderKind.Unsupported or BrowserProductiveVaultProviderKind.ExternalVaultFuture && IsConfigured)
            errors.Add("Unsupported vault providers cannot be configured.");
        if (ProviderKind is BrowserProductiveVaultProviderKind.WindowsDpapi or BrowserProductiveVaultProviderKind.WindowsCredentialManager)
        {
            if (!EnableOsBackedStorage)
                errors.Add("OS-backed vault provider is design-only unless explicitly enabled.");
            if (!AllowSyntheticTestMode)
                errors.Add("M13/M14 OS-backed provider requires synthetic test mode.");
        }
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserProductiveVaultPolicy(
    bool DenyByDefault,
    bool RequireConsent,
    bool AllowSyntheticTestOnly,
    bool AllowOsBackedStorage,
    bool AllowSecretExport,
    IReadOnlySet<BrowserSecretKind> AllowedKinds,
    IReadOnlySet<BrowserSecretScope> AllowedScopes)
{
    public static BrowserProductiveVaultPolicy DenyAll { get; } = new(
        DenyByDefault: true,
        RequireConsent: true,
        AllowSyntheticTestOnly: false,
        AllowOsBackedStorage: false,
        AllowSecretExport: false,
        AllowedKinds: new HashSet<BrowserSecretKind>(),
        AllowedScopes: new HashSet<BrowserSecretScope>());
}

public sealed record BrowserVaultConsentRequest(
    string ConsentId,
    BrowserVaultConsentType ConsentType,
    BrowserVaultConsentScope Scope,
    BrowserSecretReference? SecretReference,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    string RequestingActor,
    string ConsentChallengeId,
    string Purpose,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool RedactionApplied)
{
    public bool IsExpired(DateTimeOffset now) => ExpiresAtUtc <= now;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProfileId, nameof(ProfileId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentChallengeId, nameof(ConsentChallengeId), errors);
        if (SecretReference is not null)
            errors.AddRange(SecretReference.Validate().Errors);
        if (string.IsNullOrWhiteSpace(RequestingActor))
            errors.Add("RequestingActor is required.");
        if (string.IsNullOrWhiteSpace(ConsentChallengeId))
            errors.Add("ConsentChallengeId is required.");
        if (!RedactionApplied)
            errors.Add("Vault consent request requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(RequestingActor) || BrowserCredentialRedactor.ContainsSecret(Purpose))
            errors.Add("Vault consent request contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserVaultConsentAuditEvent(
    string EventId,
    string ConsentId,
    BrowserVaultConsentType ConsentType,
    BrowserVaultConsentScope Scope,
    BrowserVaultConsentStatus Status,
    BrowserVaultConsentAuthorityKind AuthorityKind,
    DateTimeOffset CreatedAtUtc,
    string RedactedSummary,
    bool RedactionApplied)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        if (!RedactionApplied)
            errors.Add("Vault consent audit requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedSummary))
            errors.Add("Vault consent audit contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserVaultConsentDecision(
    BrowserVaultConsentStatus Status,
    BrowserVaultConsentRequest Request,
    BrowserVaultConsentAuditEvent AuditEvent,
    BrowserVaultConsentAuthorityKind AuthorityKind,
    string ApprovingActor,
    string ApprovalSource,
    string ConsentProofRef,
    string ConsentChallengeId,
    bool CompanionAuthoritative,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string Message)
{
    public bool Allows(DateTimeOffset now, BrowserVaultConsentType consentType, BrowserSecretReference? secretReference = null) =>
        Status == BrowserVaultConsentStatus.Granted &&
        Request.ConsentType == consentType &&
        !Request.IsExpired(now) &&
        RevokedAtUtc is null &&
        HasAuthoritativeProof() &&
        SecretMatches(secretReference);

    public bool HasAuthoritativeProof() =>
        !CompanionAuthoritative &&
        AuthorityKind is BrowserVaultConsentAuthorityKind.CorePolicy or BrowserVaultConsentAuthorityKind.AdminPolicy or BrowserVaultConsentAuthorityKind.TestHarness &&
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
        if (Status == BrowserVaultConsentStatus.Granted && !HasAuthoritativeProof())
            errors.Add("Granted vault consent requires authoritative proof, trusted source, challenge binding, and non-companion authority.");
        if (Status == BrowserVaultConsentStatus.Granted && Request.IsExpired(now))
            errors.Add("Granted vault consent is expired.");
        if (BrowserCredentialRedactor.ContainsSecret(ApprovingActor) ||
            BrowserCredentialRedactor.ContainsSecret(ApprovalSource) ||
            BrowserCredentialRedactor.ContainsSecret(Message))
            errors.Add("Vault consent decision contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private bool SecretMatches(BrowserSecretReference? secretReference)
    {
        if (Request.SecretReference is null)
            return secretReference is null || Request.Scope != BrowserVaultConsentScope.Secret;
        return secretReference is not null &&
               string.Equals(Request.SecretReference.SecretId, secretReference.SecretId, StringComparison.Ordinal) &&
               Request.SecretReference.Kind == secretReference.Kind &&
               Request.SecretReference.Scope == secretReference.Scope;
    }
}

public sealed record BrowserVaultConsentPresentation(
    string ConsentId,
    BrowserVaultConsentType ConsentType,
    BrowserVaultConsentStatus Status,
    BrowserVaultConsentScope Scope,
    string SafeTitle,
    string Instruction,
    IReadOnlyList<string> AllowedOptions,
    IReadOnlyList<string> BlockedOptions,
    DateTimeOffset ExpiresAtUtc,
    bool Authoritative,
    bool Redacted,
    string Source,
    string RuntimeKind)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        if (Authoritative)
            errors.Add("Companion consent presentation must be non-authoritative.");
        if (!Redacted)
            errors.Add("Companion consent presentation must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(SafeTitle) ||
            BrowserCredentialRedactor.ContainsSecret(Instruction) ||
            AllowedOptions.Any(BrowserCredentialRedactor.ContainsSecret) ||
            BlockedOptions.Any(BrowserCredentialRedactor.ContainsSecret))
            errors.Add("Companion consent presentation contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserVaultUiEvent(
    string Type,
    string ConsentId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string Source,
    string RuntimeKind,
    bool Authoritative,
    bool Redacted,
    BrowserVerificationStatus VerificationStatus,
    string Diagnostics)
{
    public bool CanGrantConsent => false;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        if (Authoritative)
            errors.Add("Companion vault consent events must be non-authoritative.");
        if (!Redacted)
            errors.Add("Companion vault consent events must be redacted.");
        if (VerificationStatus == BrowserVerificationStatus.Verified)
            errors.Add("Companion vault consent events cannot be verified.");
        if (BrowserCredentialRedactor.ContainsSecret(Diagnostics))
            errors.Add("Companion vault consent diagnostics contain unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public abstract record BrowserSecretVaultOperationRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserSecretReference SecretReference,
    BrowserProductiveVaultProviderKind ProviderKind,
    BrowserVaultConsentDecision? Consent,
    DateTimeOffset RequestedAtUtc,
    string Reason)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProfileId, nameof(ProfileId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);
        errors.AddRange(SecretReference.Validate().Errors);
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("Vault request reason contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSecretStorageRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserSecretReference SecretReference,
    BrowserProductiveVaultProviderKind ProviderKind,
    BrowserVaultConsentDecision? Consent,
    DateTimeOffset RequestedAtUtc,
    string Reason)
    : BrowserSecretVaultOperationRequest(RequestId, RunId, ActionId, CorrelationId, ProfileId, SessionId, SecretReference, ProviderKind, Consent, RequestedAtUtc, Reason);

public sealed record BrowserSecretRetrievalRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserSecretReference SecretReference,
    BrowserProductiveVaultProviderKind ProviderKind,
    BrowserVaultConsentDecision? Consent,
    DateTimeOffset RequestedAtUtc,
    string Reason)
    : BrowserSecretVaultOperationRequest(RequestId, RunId, ActionId, CorrelationId, ProfileId, SessionId, SecretReference, ProviderKind, Consent, RequestedAtUtc, Reason);

public sealed record BrowserSecretUseRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserSecretReference SecretReference,
    BrowserProductiveVaultProviderKind ProviderKind,
    BrowserVaultConsentDecision? Consent,
    DateTimeOffset RequestedAtUtc,
    string Reason,
    bool IsCredentialAutofill)
    : BrowserSecretVaultOperationRequest(RequestId, RunId, ActionId, CorrelationId, ProfileId, SessionId, SecretReference, ProviderKind, Consent, RequestedAtUtc, Reason);

public sealed record BrowserSecretRotationRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserSecretReference SecretReference,
    BrowserProductiveVaultProviderKind ProviderKind,
    BrowserVaultConsentDecision? Consent,
    DateTimeOffset RequestedAtUtc,
    string Reason)
    : BrowserSecretVaultOperationRequest(RequestId, RunId, ActionId, CorrelationId, ProfileId, SessionId, SecretReference, ProviderKind, Consent, RequestedAtUtc, Reason);

public sealed record BrowserSecretDeletionRequest(
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserSecretReference SecretReference,
    BrowserProductiveVaultProviderKind ProviderKind,
    BrowserVaultConsentDecision? Consent,
    DateTimeOffset RequestedAtUtc,
    string Reason)
    : BrowserSecretVaultOperationRequest(RequestId, RunId, ActionId, CorrelationId, ProfileId, SessionId, SecretReference, ProviderKind, Consent, RequestedAtUtc, Reason);

public sealed record BrowserSecretVaultDecision(
    BrowserProductiveVaultDecisionKind Decision,
    BrowserVaultOperationKind Operation,
    BrowserSecretReference? Reference,
    BrowserProductiveVaultProviderKind ProviderKind,
    string Message,
    BrowserSecretVaultAuditEvent AuditEvent,
    bool RedactionApplied)
{
    public bool AllowsOperation => Decision == BrowserProductiveVaultDecisionKind.Allowed;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (!RedactionApplied)
            errors.Add("Vault decision requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(Message))
            errors.Add("Vault decision contains unredacted secret-like content.");
        if (Reference is not null)
            errors.AddRange(Reference.Validate().Errors);
        errors.AddRange(AuditEvent.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSecretVaultAuditEvent(
    string EventId,
    string RequestId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    string? ConsentId,
    string? ConsentProofRef,
    BrowserVaultOperationKind Operation,
    BrowserProductiveVaultDecisionKind Decision,
    BrowserProductiveVaultProviderKind ProviderKind,
    string SecretId,
    BrowserSecretKind SecretKind,
    BrowserSecretScope SecretScope,
    DateTimeOffset CreatedAtUtc,
    string RedactedSummary,
    bool RedactionApplied)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProfileId, nameof(ProfileId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentProofRef, nameof(ConsentProofRef), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SecretId, nameof(SecretId), errors);
        if (!RedactionApplied)
            errors.Add("Vault audit event requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(RedactedSummary))
            errors.Add("Vault audit event contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed class BrowserSecretAuditTrail
{
    private readonly List<BrowserSecretVaultAuditEvent> _events = [];

    public IReadOnlyList<BrowserSecretVaultAuditEvent> Events => _events;

    public void Add(BrowserSecretVaultAuditEvent auditEvent)
    {
        var validation = auditEvent.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));
        _events.Add(auditEvent);
    }
}
