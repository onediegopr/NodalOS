using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public enum BrowserControlledProfileMode
{
    Disposable,
    PersistentControlled,
    UserProfileBlocked,
    UserProfileControlledWithConsent
}

public enum BrowserControlledProfileLifecycleState
{
    Created,
    Activated,
    InUse,
    Expired,
    Revoked,
    CleanupRequested,
    Cleaned,
    Blocked
}

public sealed record BrowserControlledProfileActivationPolicy(
    bool AllowPersistentControlled,
    bool AllowRawUserProfile,
    string ControlledRootDirectory,
    BrowserStorageScope RequiredScope)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ControlledRootDirectory))
            errors.Add("ControlledRootDirectory is required.");
        if (AllowRawUserProfile)
            errors.Add("Raw user profile activation is blocked in M22.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserControlledProfileActivationRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    string Owner,
    BrowserControlledProfileMode RequestedMode,
    BrowserConsentGrant? Consent,
    BrowserRuntimePhaseCloseReport GateReport,
    DateTimeOffset RequestedAtUtc);

public sealed record BrowserControlledProfileActivationResult(
    BrowserControlledProfileLifecycleState State,
    BrowserProfileDescriptor? Profile,
    BrowserAuditLedgerEvent AuditEvent,
    string Reason,
    bool CookiesExposed,
    bool SessionStorageExposed,
    bool Redacted)
{
    public bool IsActivated =>
        State == BrowserControlledProfileLifecycleState.Activated &&
        Profile is not null &&
        !CookiesExposed &&
        !SessionStorageExposed &&
        Redacted;
}

public sealed class BrowserConsentService
{
    public BrowserConsentRequest CreateRequest(
        BrowserConsentCapability capability,
        BrowserConsentScope scope,
        string runId,
        string actionId,
        string correlationId,
        string requestedBy,
        string purpose,
        TimeSpan ttl,
        IReadOnlyList<string>? risks = null,
        IReadOnlyList<string>? remainsBlocked = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrowserConsentRequest(
            ConsentId: $"browser-consent-{Guid.NewGuid():N}",
            Capability: capability,
            Scope: scope,
            RunId: BrowserCredentialRedactor.Redact(runId),
            ActionId: BrowserCredentialRedactor.Redact(actionId),
            CorrelationId: BrowserCredentialRedactor.Redact(correlationId),
            RequestedBy: BrowserCredentialRedactor.Redact(requestedBy),
            Purpose: BrowserCredentialRedactor.Redact(purpose),
            HumanReadableExplanation: ExplanationFor(capability),
            Risks: (risks ?? DefaultRisks(capability)).Select(BrowserCredentialRedactor.Redact).ToArray(),
            WhatRemainsBlocked: (remainsBlocked ?? DefaultBlocked()).Select(BrowserCredentialRedactor.Redact).ToArray(),
            RevokeInstructions: "Puede revocar este consentimiento desde la superficie de consentimiento. Revocar bloquea nuevas evaluaciones de policy.",
            RequestedAtUtc: now,
            ExpiresAtUtc: now + ttl,
            Redacted: true);
    }

    public BrowserConsentUiModel CreateUiModel(BrowserConsentRequest request)
    {
        var ttl = request.ExpiresAtUtc - request.RequestedAtUtc;
        return new BrowserConsentUiModel(
            ConsentId: request.ConsentId,
            Title: "NODAL OS necesita autorización",
            RequestedCapability: request.Capability.ToString(),
            Scope: request.Scope.ToString(),
            Purpose: request.Purpose,
            Ttl: ttl,
            ExpiresAtUtc: request.ExpiresAtUtc,
            RequestedBy: request.RequestedBy,
            Explanation: $"{request.HumanReadableExplanation} Esto no autoriza acciones sensibles automáticamente.",
            Risks: request.Risks,
            WhatRemainsBlocked: request.WhatRemainsBlocked,
            Options: ["Approve", "Deny", "Revoke", "CopyDiagnosticLog"],
            RevokeInstructions: request.RevokeInstructions,
            Authoritative: false,
            Redacted: true);
    }

    public BrowserConsentDecision Decide(BrowserConsentRequest request, BrowserConsentStatus status, string actor, string proofRef, DateTimeOffset now)
    {
        var finalStatus = FinalStatus(request, status, proofRef, now);
        var audit = Audit(request, finalStatus, actor, now);
        var grant = finalStatus == BrowserConsentStatus.Granted
            ? new BrowserConsentGrant(finalStatus, request, BrowserCredentialRedactor.Redact(actor), BrowserCredentialRedactor.Redact(proofRef), now, request.ExpiresAtUtc, null, audit)
            : null;
        return new BrowserConsentDecision(finalStatus, request, grant, audit, BrowserCredentialRedactor.Redact(MessageFor(finalStatus)));
    }

    public BrowserConsentDecision Revoke(BrowserConsentGrant grant, string actor, DateTimeOffset now)
    {
        var revoked = grant.Revoke(now, actor);
        return new BrowserConsentDecision(BrowserConsentStatus.Revoked, grant.Request, revoked, revoked.AuditEvent, "consent revoked");
    }

    public BrowserConsentDecision Evaluate(BrowserConsentGrant? grant, DateTimeOffset now)
    {
        if (grant is null)
            throw new InvalidOperationException("Consent grant is required for evaluation.");
        if (grant.RevokedAtUtc is not null)
            return new BrowserConsentDecision(BrowserConsentStatus.Revoked, grant.Request, grant, grant.AuditEvent, "consent revoked");
        if (grant.ExpiresAtUtc <= now)
            return new BrowserConsentDecision(BrowserConsentStatus.Expired, grant.Request, grant, grant.AuditEvent with { Status = BrowserConsentStatus.Expired, CreatedAtUtc = now, RedactedSummary = "consent expired" }, "consent expired");
        return new BrowserConsentDecision(grant.Status, grant.Request, grant, grant.AuditEvent, "consent active for policy evaluation");
    }

    private static BrowserConsentStatus FinalStatus(BrowserConsentRequest request, BrowserConsentStatus status, string proofRef, DateTimeOffset now)
    {
        if (!request.Validate().IsValid)
            return BrowserConsentStatus.Invalid;
        if (request.IsExpired(now) && status == BrowserConsentStatus.Granted)
            return BrowserConsentStatus.Expired;
        if (status == BrowserConsentStatus.Granted && string.IsNullOrWhiteSpace(proofRef))
            return BrowserConsentStatus.Invalid;
        return status;
    }

    private static BrowserConsentAuditEvent Audit(BrowserConsentRequest request, BrowserConsentStatus status, string actor, DateTimeOffset now) =>
        new(
            EventId: $"browser-consent-audit-{Guid.NewGuid():N}",
            ConsentId: request.ConsentId,
            Capability: request.Capability,
            Scope: request.Scope,
            Status: status,
            CreatedAtUtc: now,
            DecisionBy: BrowserCredentialRedactor.Redact(actor),
            RedactedSummary: BrowserCredentialRedactor.Redact($"consent {status} for {request.Capability}"),
            Redacted: true);

    private static string ExplanationFor(BrowserConsentCapability capability) => capability switch
    {
        BrowserConsentCapability.ProfileControlledActivation => "Se solicita habilitar un perfil de navegador controlado por ONE BRAIN dentro de una carpeta administrada.",
        BrowserConsentCapability.ProfileRealConsent => "Se solicita consentimiento para evaluar un perfil real, que sigue bloqueado por policy hasta autorización posterior.",
        BrowserConsentCapability.CookieSessionAccess => "Se solicita evaluar acceso a sesión/cookies, sin exponer valores al companion, logs ni audit.",
        _ => "Se solicita consentimiento para evaluar una capability sensible bajo policy de Core."
    };

    private static IReadOnlyList<string> DefaultRisks(BrowserConsentCapability capability) => capability switch
    {
        BrowserConsentCapability.ProfileControlledActivation => ["Puede conservar estado de navegador controlado dentro del scope permitido.", "No habilita login ni lectura de cookies por sí mismo."],
        BrowserConsentCapability.CookieSessionAccess => ["Puede existir material de sesión sensible.", "Los valores de cookies siguen bloqueados para UI/protocol/audit."],
        _ => ["Capability sensible requiere evaluación posterior de policy."]
    };

    private static IReadOnlyList<string> DefaultBlocked() =>
        ["No autoriza login real.", "No autoriza vault real.", "No autoriza acciones sensibles automáticamente.", "No permite que Companion sea autoridad."];

    private static string MessageFor(BrowserConsentStatus status) => status switch
    {
        BrowserConsentStatus.Granted => "consent enables later core policy evaluation only",
        BrowserConsentStatus.Denied => "consent denied",
        BrowserConsentStatus.Revoked => "consent revoked",
        BrowserConsentStatus.Expired => "consent expired",
        _ => "consent invalid"
    };
}

public sealed class BrowserControlledProfileActivationService
{
    private readonly BrowserProfileManager _profileManager;

    public BrowserControlledProfileActivationService(BrowserProfileManager? profileManager = null)
    {
        _profileManager = profileManager ?? new BrowserProfileManager();
    }

    public BrowserControlledProfileActivationResult Activate(BrowserControlledProfileActivationRequest request, BrowserControlledProfileActivationPolicy policy, DateTimeOffset now)
    {
        var policyValidation = policy.Validate();
        if (!policyValidation.IsValid)
            return Block(request, string.Join("; ", policyValidation.Errors));

        if (!request.GateReport.Passed)
            return Block(request, "phase gate failed");
        if (request.GateReport.ObservedState is { CompanionAuthoritative: true } or { RequestBodyCaptureSupported: true } or { ResponseBodyCaptureSupported: true } or { SensitiveHeaderValueCaptureSupported: true } or { RealVaultActive: true })
            return Block(request, "phase gate observed unsafe runtime state");
        if (request.RequestedMode == BrowserControlledProfileMode.UserProfileBlocked)
            return Block(request, "raw user profile remains blocked");
        if (request.RequestedMode == BrowserControlledProfileMode.UserProfileControlledWithConsent)
            return Block(request, "raw user profile launch remains disabled; use persistent controlled profile only in M22");
        if (request.RequestedMode != BrowserControlledProfileMode.PersistentControlled || !policy.AllowPersistentControlled)
            return Block(request, "requested profile mode is not allowed");
        if (request.Consent is null || !request.Consent.AllowsCapability(BrowserConsentCapability.ProfileControlledActivation, BrowserConsentScope.Profile, now))
            return Block(request, "valid scoped consent is required");
        if (policy.RequiredScope is not BrowserStorageScope.Tenant and not BrowserStorageScope.Person and not BrowserStorageScope.Portal)
            return Block(request, "profile activation policy scope is not compatible");

        var profilePolicy = new BrowserProfilePolicy(
            BrowserProfileKind.PersistentControlled,
            policy.RequiredScope,
            BrowserProfileCleanupPolicy.KeepControlled,
            BrowserProfileConsentPolicy.Granted,
            AllowRealUserProfile: false,
            ControlledRootDirectory: policy.ControlledRootDirectory);
        var profile = _profileManager.CreateProfile(profilePolicy);
        var audit = Audit(request, BrowserControlledProfileLifecycleState.Activated, "controlled profile activated");
        return new BrowserControlledProfileActivationResult(BrowserControlledProfileLifecycleState.Activated, profile, audit, "controlled profile activated", CookiesExposed: false, SessionStorageExposed: false, Redacted: true);
    }

    public async Task<BrowserControlledProfileActivationResult> CleanupAsync(BrowserControlledProfileActivationResult activation, BrowserControlledProfileLifecycleState requestedState)
    {
        if (activation.Profile is not null)
        {
            var cleanupProfile = activation.Profile with { CleanupPolicy = BrowserProfileCleanupPolicy.DeleteOnClose };
            await _profileManager.CleanupProfileAsync(cleanupProfile).ConfigureAwait(false);
        }

        var audit = activation.AuditEvent with
        {
            EventId = $"audit-ledger-{Guid.NewGuid():N}",
            Decision = requestedState.ToString(),
            Reason = BrowserCredentialRedactor.Redact($"controlled profile cleanup after {requestedState}")
        };
        return activation with { State = BrowserControlledProfileLifecycleState.Cleaned, AuditEvent = audit, Reason = "controlled profile cleaned" };
    }

    private static BrowserControlledProfileActivationResult Block(BrowserControlledProfileActivationRequest request, string reason)
    {
        var audit = Audit(request, BrowserControlledProfileLifecycleState.Blocked, reason);
        return new BrowserControlledProfileActivationResult(BrowserControlledProfileLifecycleState.Blocked, null, audit, BrowserCredentialRedactor.Redact(reason), CookiesExposed: false, SessionStorageExposed: false, Redacted: true);
    }

    private static BrowserAuditLedgerEvent Audit(BrowserControlledProfileActivationRequest request, BrowserControlledProfileLifecycleState state, string reason) =>
        BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.ProfileRealConsentRequested,
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            "profile-controlled",
            "session-controlled",
            request.Consent?.Request.ConsentId,
            null,
            null,
            state.ToString(),
            reason,
            new Dictionary<string, string>
            {
                ["profileMode"] = request.RequestedMode.ToString(),
                ["browserStorageBoundary"] = "sealed",
                ["sensitiveMaterialExposed"] = "false"
            });
}
