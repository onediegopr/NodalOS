using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public interface IBrowserProductiveVaultProvider
{
    BrowserProductiveVaultProviderKind Kind { get; }
    IReadOnlySet<BrowserProductiveVaultCapability> Capabilities { get; }
    IReadOnlyList<BrowserSecretVaultAuditEvent> AuditEvents { get; }
    Task<BrowserSecretVaultDecision> StoreAsync(BrowserSecretStorageRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default);
    Task<BrowserSecretVaultDecision> RetrieveAsync(BrowserSecretRetrievalRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default);
    Task<BrowserSecretVaultDecision> UseAsync(BrowserSecretUseRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default);
    Task<BrowserSecretVaultDecision> RotateAsync(BrowserSecretRotationRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default);
    Task<BrowserSecretVaultDecision> DeleteAsync(BrowserSecretDeletionRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default);
    Task<BrowserSecretVaultDecision> ExportAsync(BrowserSecretRetrievalRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default);
}

public abstract class BrowserProductiveVaultProviderBase : IBrowserProductiveVaultProvider
{
    private readonly List<BrowserSecretVaultAuditEvent> _audit = [];

    protected BrowserProductiveVaultProviderBase(BrowserProductiveVaultProviderKind kind, IReadOnlySet<BrowserProductiveVaultCapability> capabilities)
    {
        Kind = kind;
        Capabilities = capabilities;
    }

    public BrowserProductiveVaultProviderKind Kind { get; }
    public IReadOnlySet<BrowserProductiveVaultCapability> Capabilities { get; }
    public IReadOnlyList<BrowserSecretVaultAuditEvent> AuditEvents => _audit;

    public virtual Task<BrowserSecretVaultDecision> StoreAsync(BrowserSecretStorageRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(Decide(request, BrowserVaultOperationKind.Storage, BrowserVaultConsentType.SecretStorageConsent, policy, configuration));

    public virtual Task<BrowserSecretVaultDecision> RetrieveAsync(BrowserSecretRetrievalRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(Decide(request, BrowserVaultOperationKind.Retrieval, BrowserVaultConsentType.SecretRetrievalConsent, policy, configuration));

    public virtual Task<BrowserSecretVaultDecision> UseAsync(BrowserSecretUseRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(Decide(request, BrowserVaultOperationKind.Use, request.IsCredentialAutofill ? BrowserVaultConsentType.CredentialAutofillConsent : BrowserVaultConsentType.SecretUseConsent, policy, configuration));

    public virtual Task<BrowserSecretVaultDecision> RotateAsync(BrowserSecretRotationRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(Decide(request, BrowserVaultOperationKind.Rotation, BrowserVaultConsentType.SecretRotationConsent, policy, configuration));

    public virtual Task<BrowserSecretVaultDecision> DeleteAsync(BrowserSecretDeletionRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(Decide(request, BrowserVaultOperationKind.Deletion, BrowserVaultConsentType.SecretDeletionConsent, policy, configuration));

    public virtual Task<BrowserSecretVaultDecision> ExportAsync(BrowserSecretRetrievalRequest request, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(Decision(request, BrowserVaultOperationKind.Export, BrowserProductiveVaultDecisionKind.Denied, "secret export is prohibited by default"));

    protected BrowserSecretVaultDecision Decide(BrowserSecretVaultOperationRequest request, BrowserVaultOperationKind operation, BrowserVaultConsentType requiredConsent, BrowserProductiveVaultPolicy policy, BrowserProductiveVaultConfiguration configuration)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
            return Decision(request, operation, BrowserProductiveVaultDecisionKind.FailClosed, "vault request failed closed");

        var configValidation = configuration.Validate();
        if (!configValidation.IsValid || !configuration.IsConfigured)
            return Decision(request, operation, configuration.ProviderKind == BrowserProductiveVaultProviderKind.Unsupported ? BrowserProductiveVaultDecisionKind.Unsupported : BrowserProductiveVaultDecisionKind.Unconfigured, "vault provider is not configured");

        if (Kind != configuration.ProviderKind)
            return Decision(request, operation, BrowserProductiveVaultDecisionKind.FailClosed, "vault provider mismatch");

        if (Kind is BrowserProductiveVaultProviderKind.Unsupported or BrowserProductiveVaultProviderKind.ExternalVaultFuture)
            return Decision(request, operation, BrowserProductiveVaultDecisionKind.Unsupported, "vault provider unsupported");

        if (operation == BrowserVaultOperationKind.Export && !policy.AllowSecretExport)
            return Decision(request, operation, BrowserProductiveVaultDecisionKind.Denied, "secret export is prohibited");

        if (!policy.AllowedKinds.Contains(request.SecretReference.Kind) || !policy.AllowedScopes.Contains(request.SecretReference.Scope))
            return Decision(request, operation, policy.DenyByDefault ? BrowserProductiveVaultDecisionKind.Denied : BrowserProductiveVaultDecisionKind.RequiresApproval, "secret kind or scope is not allowed");

        if (policy.RequireConsent && (request.Consent is null || !request.Consent.Allows(DateTimeOffset.UtcNow, requiredConsent, request.SecretReference)))
            return Decision(request, operation, BrowserProductiveVaultDecisionKind.RequiresConsent, "vault operation requires scoped consent");

        return Allow(request, operation);
    }

    protected virtual BrowserSecretVaultDecision Allow(BrowserSecretVaultOperationRequest request, BrowserVaultOperationKind operation) =>
        Decision(request, operation, BrowserProductiveVaultDecisionKind.Allowed, "vault operation allowed by provider policy", request.SecretReference);

    protected BrowserSecretVaultDecision Decision(
        BrowserSecretVaultOperationRequest request,
        BrowserVaultOperationKind operation,
        BrowserProductiveVaultDecisionKind decision,
        string message,
        BrowserSecretReference? reference = null)
    {
        var audit = new BrowserSecretVaultAuditEvent(
            EventId: $"vault-audit-{Guid.NewGuid():N}",
            RequestId: request.RequestId,
            RunId: request.RunId,
            ActionId: request.ActionId,
            CorrelationId: request.CorrelationId,
            ProfileId: request.ProfileId,
            SessionId: request.SessionId,
            ConsentId: request.Consent?.Request.ConsentId,
            ConsentProofRef: request.Consent?.ConsentProofRef,
            Operation: operation,
            Decision: decision,
            ProviderKind: Kind,
            SecretId: BrowserCredentialRedactor.Redact(request.SecretReference.SecretId),
            SecretKind: request.SecretReference.Kind,
            SecretScope: request.SecretReference.Scope,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RedactedSummary: BrowserCredentialRedactor.Redact(message),
            RedactionApplied: true);
        _audit.Add(audit);
        return new BrowserSecretVaultDecision(decision, operation, reference, Kind, BrowserCredentialRedactor.Redact(message), audit, RedactionApplied: true);
    }
}

public sealed class NullBrowserProductiveVaultProvider : BrowserProductiveVaultProviderBase
{
    public NullBrowserProductiveVaultProvider()
        : base(BrowserProductiveVaultProviderKind.Null, new HashSet<BrowserProductiveVaultCapability>())
    {
    }

    protected override BrowserSecretVaultDecision Allow(BrowserSecretVaultOperationRequest request, BrowserVaultOperationKind operation) =>
        Decision(request, operation, BrowserProductiveVaultDecisionKind.Denied, "null productive vault denies all operations");
}

public sealed class UnsupportedBrowserProductiveVaultProvider : BrowserProductiveVaultProviderBase
{
    public UnsupportedBrowserProductiveVaultProvider()
        : base(BrowserProductiveVaultProviderKind.Unsupported, new HashSet<BrowserProductiveVaultCapability>())
    {
    }
}

public sealed class WindowsDpapiSecretVaultProvider : BrowserProductiveVaultProviderBase
{
    public WindowsDpapiSecretVaultProvider()
        : base(BrowserProductiveVaultProviderKind.WindowsDpapi, new HashSet<BrowserProductiveVaultCapability> { BrowserProductiveVaultCapability.OsBackedStorage })
    {
    }

    protected override BrowserSecretVaultDecision Allow(BrowserSecretVaultOperationRequest request, BrowserVaultOperationKind operation) =>
        Decision(request, operation, BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI vault provider is design-only in M13/M14");
}

public sealed class WindowsCredentialManagerSecretVaultProvider : BrowserProductiveVaultProviderBase
{
    public WindowsCredentialManagerSecretVaultProvider()
        : base(BrowserProductiveVaultProviderKind.WindowsCredentialManager, new HashSet<BrowserProductiveVaultCapability> { BrowserProductiveVaultCapability.OsBackedStorage })
    {
    }

    protected override BrowserSecretVaultDecision Allow(BrowserSecretVaultOperationRequest request, BrowserVaultOperationKind operation) =>
        Decision(request, operation, BrowserProductiveVaultDecisionKind.FailClosed, "Windows Credential Manager vault provider is design-only in M13/M14");
}

public sealed class InMemoryTestOnlyProductiveVaultProvider : BrowserProductiveVaultProviderBase
{
    private readonly Dictionary<string, BrowserSecretReference> _references = new(StringComparer.Ordinal);

    public InMemoryTestOnlyProductiveVaultProvider()
        : base(BrowserProductiveVaultProviderKind.InMemoryTestOnly, new HashSet<BrowserProductiveVaultCapability>
        {
            BrowserProductiveVaultCapability.StoreReference,
            BrowserProductiveVaultCapability.RetrieveReference,
            BrowserProductiveVaultCapability.UseReference,
            BrowserProductiveVaultCapability.RotateReference,
            BrowserProductiveVaultCapability.DeleteReference,
            BrowserProductiveVaultCapability.SyntheticTestOnly
        })
    {
    }

    protected override BrowserSecretVaultDecision Allow(BrowserSecretVaultOperationRequest request, BrowserVaultOperationKind operation)
    {
        if (!IsSyntheticReference(request.SecretReference))
            return Decision(request, operation, BrowserProductiveVaultDecisionKind.FailClosed, "test-only productive vault accepts only synthetic references");

        return operation switch
        {
            BrowserVaultOperationKind.Storage => StoreReference(request),
            BrowserVaultOperationKind.Retrieval or BrowserVaultOperationKind.Use or BrowserVaultOperationKind.Rotation => ReferenceRequired(request, operation),
            BrowserVaultOperationKind.Deletion => DeleteReference(request),
            _ => Decision(request, operation, BrowserProductiveVaultDecisionKind.Denied, "operation not supported by test-only productive vault")
        };
    }

    private BrowserSecretVaultDecision StoreReference(BrowserSecretVaultOperationRequest request)
    {
        _references[request.SecretReference.SecretId] = request.SecretReference;
        return Decision(request, BrowserVaultOperationKind.Storage, BrowserProductiveVaultDecisionKind.Allowed, "synthetic reference stored", request.SecretReference);
    }

    private BrowserSecretVaultDecision ReferenceRequired(BrowserSecretVaultOperationRequest request, BrowserVaultOperationKind operation)
    {
        if (!_references.TryGetValue(request.SecretReference.SecretId, out var canonical) || !ReferenceMatches(request.SecretReference, canonical))
            return Decision(request, operation, BrowserProductiveVaultDecisionKind.Denied, "synthetic reference is not stored or canonical");

        return Decision(request, operation, BrowserProductiveVaultDecisionKind.Allowed, "synthetic reference operation allowed", canonical);
    }

    private BrowserSecretVaultDecision DeleteReference(BrowserSecretVaultOperationRequest request)
    {
        if (!_references.TryGetValue(request.SecretReference.SecretId, out var canonical) || !ReferenceMatches(request.SecretReference, canonical))
            return Decision(request, BrowserVaultOperationKind.Deletion, BrowserProductiveVaultDecisionKind.Denied, "synthetic reference is not stored or canonical");

        _references.Remove(request.SecretReference.SecretId);
        return Decision(request, BrowserVaultOperationKind.Deletion, BrowserProductiveVaultDecisionKind.Allowed, "synthetic reference deleted", canonical);
    }

    private static bool IsSyntheticReference(BrowserSecretReference reference) =>
        reference.SecretId.StartsWith("synthetic-secret-", StringComparison.Ordinal) &&
        reference.Scope == BrowserSecretScope.Temporary;

    private static bool ReferenceMatches(BrowserSecretReference request, BrowserSecretReference stored) =>
        string.Equals(request.SecretId, stored.SecretId, StringComparison.Ordinal) &&
        request.Kind == stored.Kind &&
        request.Scope == stored.Scope &&
        string.Equals(request.Owner, stored.Owner, StringComparison.Ordinal) &&
        string.Equals(request.Portal, stored.Portal, StringComparison.Ordinal) &&
        string.Equals(request.RedactedLabel, stored.RedactedLabel, StringComparison.Ordinal);
}

public sealed class BrowserVaultConsentService
{
    public const string RuntimeKind = "core-governed-browser-runtime";
    public const string CompanionSource = "chrome-companion";
    public const string CoreSource = "core-browser-runtime";

    public BrowserVaultConsentRequest CreateRequest(
        BrowserVaultConsentType consentType,
        BrowserVaultConsentScope scope,
        BrowserSecretReference? secretReference,
        string runId,
        string actionId,
        string correlationId,
        string profileId,
        string sessionId,
        string requestingActor,
        string purpose,
        TimeSpan? ttl = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrowserVaultConsentRequest(
            ConsentId: $"vault-consent-{Guid.NewGuid():N}",
            ConsentType: consentType,
            Scope: scope,
            SecretReference: secretReference,
            RunId: BrowserCredentialRedactor.Redact(runId),
            ActionId: BrowserCredentialRedactor.Redact(actionId),
            CorrelationId: BrowserCredentialRedactor.Redact(correlationId),
            ProfileId: BrowserCredentialRedactor.Redact(profileId),
            SessionId: BrowserCredentialRedactor.Redact(sessionId),
            RequestingActor: BrowserCredentialRedactor.Redact(requestingActor),
            ConsentChallengeId: $"vault-consent-challenge-{Guid.NewGuid():N}",
            Purpose: BrowserCredentialRedactor.Redact(purpose),
            RequestedAtUtc: now,
            ExpiresAtUtc: now + (ttl ?? TimeSpan.FromMinutes(15)),
            RedactionApplied: true);
    }

    public BrowserVaultConsentDecision Decide(
        BrowserVaultConsentRequest request,
        BrowserVaultConsentStatus status,
        DateTimeOffset now,
        BrowserVaultConsentAuthorityKind authorityKind = BrowserVaultConsentAuthorityKind.Unknown,
        string approvingActor = "",
        string approvalSource = "",
        string consentProofRef = "",
        string consentChallengeId = "",
        bool companionAuthoritative = false)
    {
        var finalStatus = FinalStatus(request, status, now, authorityKind, approvingActor, approvalSource, consentProofRef, consentChallengeId, companionAuthoritative);
        var audit = new BrowserVaultConsentAuditEvent(
            EventId: $"vault-consent-audit-{Guid.NewGuid():N}",
            ConsentId: request.ConsentId,
            ConsentType: request.ConsentType,
            Scope: request.Scope,
            Status: finalStatus,
            AuthorityKind: authorityKind,
            CreatedAtUtc: now,
            RedactedSummary: BrowserCredentialRedactor.Redact($"vault consent {finalStatus} for {request.ConsentType}"),
            RedactionApplied: true);
        return new BrowserVaultConsentDecision(
            Status: finalStatus,
            Request: request,
            AuditEvent: audit,
            AuthorityKind: authorityKind,
            ApprovingActor: BrowserCredentialRedactor.Redact(approvingActor),
            ApprovalSource: BrowserCredentialRedactor.Redact(approvalSource),
            ConsentProofRef: BrowserCredentialRedactor.Redact(consentProofRef),
            ConsentChallengeId: BrowserCredentialRedactor.Redact(consentChallengeId),
            CompanionAuthoritative: companionAuthoritative,
            IssuedAtUtc: now,
            RevokedAtUtc: finalStatus == BrowserVaultConsentStatus.Revoked ? now : null,
            Message: BrowserCredentialRedactor.Redact(MessageFor(finalStatus)));
    }

    public BrowserVaultConsentPresentation CreatePresentation(BrowserVaultConsentRequest request) =>
        new(
            ConsentId: request.ConsentId,
            ConsentType: request.ConsentType,
            Status: BrowserVaultConsentStatus.Requested,
            Scope: request.Scope,
            SafeTitle: TitleFor(request.ConsentType),
            Instruction: InstructionFor(request.ConsentType),
            AllowedOptions: ["ApproveIntent", "Deny", "CopyDiagnosticLog"],
            BlockedOptions: BlockedOptionsFor(request.ConsentType),
            ExpiresAtUtc: request.ExpiresAtUtc,
            Authoritative: false,
            Redacted: true,
            Source: CompanionSource,
            RuntimeKind: RuntimeKind);

    public BrowserVaultUiEvent CompanionIntent(string type, BrowserVaultConsentRequest request, string diagnostics = "") =>
        new(
            Type: type,
            ConsentId: request.ConsentId,
            RunId: request.RunId,
            ActionId: request.ActionId,
            CorrelationId: request.CorrelationId,
            Source: CompanionSource,
            RuntimeKind: RuntimeKind,
            Authoritative: false,
            Redacted: true,
            VerificationStatus: BrowserVerificationStatus.Uncertain,
            Diagnostics: BrowserCredentialRedactor.Redact(diagnostics));

    private static BrowserVaultConsentStatus FinalStatus(
        BrowserVaultConsentRequest request,
        BrowserVaultConsentStatus status,
        DateTimeOffset now,
        BrowserVaultConsentAuthorityKind authorityKind,
        string approvingActor,
        string approvalSource,
        string consentProofRef,
        string consentChallengeId,
        bool companionAuthoritative)
    {
        if (!request.Validate().IsValid)
            return BrowserVaultConsentStatus.Invalid;
        if (request.IsExpired(now) && status == BrowserVaultConsentStatus.Granted)
            return BrowserVaultConsentStatus.Expired;
        if (status != BrowserVaultConsentStatus.Granted)
            return status;
        if (companionAuthoritative ||
            authorityKind is BrowserVaultConsentAuthorityKind.Unknown or BrowserVaultConsentAuthorityKind.UserViaCompanionIntent ||
            string.IsNullOrWhiteSpace(approvingActor) ||
            string.IsNullOrWhiteSpace(approvalSource) ||
            string.IsNullOrWhiteSpace(consentProofRef) ||
            string.IsNullOrWhiteSpace(consentChallengeId) ||
            !string.Equals(consentChallengeId, request.ConsentChallengeId, StringComparison.Ordinal))
            return BrowserVaultConsentStatus.Invalid;
        return BrowserVaultConsentStatus.Granted;
    }

    private static string MessageFor(BrowserVaultConsentStatus status) => status switch
    {
        BrowserVaultConsentStatus.Granted => "vault consent granted by core authority",
        BrowserVaultConsentStatus.Denied => "vault consent denied",
        BrowserVaultConsentStatus.Expired => "vault consent expired",
        BrowserVaultConsentStatus.Revoked => "vault consent revoked",
        BrowserVaultConsentStatus.Invalid => "vault consent invalid",
        _ => "vault consent not granted"
    };

    private static string TitleFor(BrowserVaultConsentType type) => type switch
    {
            BrowserVaultConsentType.ProfileRealConsent => "NODAL OS necesita autorizacion para perfil real",
            BrowserVaultConsentType.SecretStorageConsent => "NODAL OS necesita autorizacion para guardar una referencia secreta",
            BrowserVaultConsentType.SecretRetrievalConsent => "NODAL OS necesita autorizacion para recuperar una referencia secreta",
            BrowserVaultConsentType.SecretUseConsent => "NODAL OS necesita autorizacion para usar una referencia secreta",
            BrowserVaultConsentType.CookieAccessConsent => "NODAL OS necesita autorizacion para acceder a cookie o sesion sensible",
            _ => "NODAL OS necesita autorizacion"
    };

    private static string InstructionFor(BrowserVaultConsentType type) => type switch
    {
        BrowserVaultConsentType.SecretStorageConsent => "El sistema solicita guardar una referencia a un secreto futuro. No se mostrara ni transportara el valor secreto.",
        BrowserVaultConsentType.SecretRetrievalConsent => "El sistema solicita recuperar una referencia a un secreto. El valor no sera mostrado al Companion.",
        BrowserVaultConsentType.SecretUseConsent => "El sistema solicita usar una referencia secreta bajo politica. Esto no autoriza exportacion ni autofill.",
        BrowserVaultConsentType.CredentialAutofillConsent => "El sistema solicita autofill futuro. Esta accion requiere autorizacion separada y verificacion posterior.",
        BrowserVaultConsentType.CookieAccessConsent => "El sistema solicita acceso a cookie o sesion sensible. Esto no autoriza password ni token.",
        BrowserVaultConsentType.ProfileRealConsent => "El sistema solicita usar perfil real en el futuro. Esto no autoriza acceso a secretos.",
        _ => "El sistema solicita autorizacion scoped. Companion solo puede registrar intencion; Core decide."
    };

    private static IReadOnlyList<string> BlockedOptionsFor(BrowserVaultConsentType type) => type switch
    {
        BrowserVaultConsentType.SecretExportConsent => ["export secret value", "mark consent granted from companion"],
        BrowserVaultConsentType.CredentialAutofillConsent => ["autofill without core proof", "show password in companion"],
        BrowserVaultConsentType.CookieAccessConsent => ["read password", "read token", "export cookie"],
        _ => ["mark consent granted from companion", "show secret value"]
    };
}
