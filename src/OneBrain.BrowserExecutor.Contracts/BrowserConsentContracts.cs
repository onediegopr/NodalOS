namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserConsentCapability
{
    ProfileControlledActivation,
    ProfileRealConsent,
    VaultConfiguration,
    SecretStorage,
    SecretRetrieval,
    SecretUse,
    CookieSessionAccess,
    HumanHandoffResume
}

public enum BrowserConsentScope
{
    Tenant,
    Company,
    Person,
    Worker,
    Portal,
    Profile,
    Session,
    Runtime,
    Temporary
}

public enum BrowserConsentStatus
{
    Requested,
    Granted,
    Denied,
    Revoked,
    Expired,
    Invalid
}

public sealed record BrowserConsentRequest(
    string ConsentId,
    BrowserConsentCapability Capability,
    BrowserConsentScope Scope,
    string RunId,
    string ActionId,
    string CorrelationId,
    string RequestedBy,
    string Purpose,
    string HumanReadableExplanation,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> WhatRemainsBlocked,
    string RevokeInstructions,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool Redacted)
{
    public bool IsExpired(DateTimeOffset now) => ExpiresAtUtc <= now;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        if (!Redacted)
            errors.Add("Consent request must be redacted.");
        if (string.IsNullOrWhiteSpace(Purpose))
            errors.Add("Consent purpose is required.");
        if (ExpiresAtUtc <= RequestedAtUtc)
            errors.Add("Consent expiration must be after request time.");
        if (BrowserCredentialRedactor.ContainsSecret(RequestedBy) ||
            BrowserCredentialRedactor.ContainsSecret(Purpose) ||
            BrowserCredentialRedactor.ContainsSecret(HumanReadableExplanation) ||
            Risks.Any(BrowserCredentialRedactor.ContainsSecret) ||
            WhatRemainsBlocked.Any(BrowserCredentialRedactor.ContainsSecret) ||
            BrowserCredentialRedactor.ContainsSecret(RevokeInstructions))
            errors.Add("Consent request contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserConsentUiModel(
    string ConsentId,
    string Title,
    string RequestedCapability,
    string Scope,
    string Purpose,
    TimeSpan Ttl,
    DateTimeOffset ExpiresAtUtc,
    string RequestedBy,
    string Explanation,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> WhatRemainsBlocked,
    IReadOnlyList<string> Options,
    string RevokeInstructions,
    bool Authoritative,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        if (Authoritative)
            errors.Add("Consent UI is not authoritative.");
        if (!Redacted)
            errors.Add("Consent UI must be redacted.");
        var text = string.Join(" ", Title, RequestedCapability, Scope, Purpose, RequestedBy, Explanation, RevokeInstructions, string.Join(" ", Risks), string.Join(" ", WhatRemainsBlocked), string.Join(" ", Options));
        if (BrowserCredentialRedactor.ContainsSecret(text))
            errors.Add("Consent UI contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserConsentAuditEvent(
    string EventId,
    string ConsentId,
    BrowserConsentCapability Capability,
    BrowserConsentScope Scope,
    BrowserConsentStatus Status,
    DateTimeOffset CreatedAtUtc,
    string DecisionBy,
    string RedactedSummary,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        if (!Redacted)
            errors.Add("Consent audit event must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(DecisionBy) || BrowserCredentialRedactor.ContainsSecret(RedactedSummary))
            errors.Add("Consent audit event contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserConsentGrant(
    BrowserConsentStatus Status,
    BrowserConsentRequest Request,
    string GrantedBy,
    string ProofRef,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    BrowserConsentAuditEvent AuditEvent)
{
    public bool IsActive(DateTimeOffset now) =>
        Status == BrowserConsentStatus.Granted &&
        RevokedAtUtc is null &&
        ExpiresAtUtc > now &&
        !string.IsNullOrWhiteSpace(ProofRef);

    public bool AllowsCapability(BrowserConsentCapability capability, BrowserConsentScope scope, DateTimeOffset now) =>
        IsActive(now) &&
        Request.Capability == capability &&
        Request.Scope == scope;

    public bool AuthorizesActionByItself => false;

    public BrowserConsentGrant Revoke(DateTimeOffset now, string actor) => this with
    {
        Status = BrowserConsentStatus.Revoked,
        RevokedAtUtc = now,
        AuditEvent = AuditEvent with
        {
            EventId = $"browser-consent-audit-{Guid.NewGuid():N}",
            Status = BrowserConsentStatus.Revoked,
            CreatedAtUtc = now,
            DecisionBy = BrowserCredentialRedactor.Redact(actor),
            RedactedSummary = BrowserCredentialRedactor.Redact($"consent revoked for {Request.Capability}")
        }
    };

    public ContractValidationResult Validate(DateTimeOffset now)
    {
        var errors = new List<string>();
        errors.AddRange(Request.Validate().Errors);
        errors.AddRange(AuditEvent.Validate().Errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProofRef, nameof(ProofRef), errors);
        if (Status == BrowserConsentStatus.Granted && !IsActive(now) && RevokedAtUtc is null)
            errors.Add("Granted consent is not active.");
        if (BrowserCredentialRedactor.ContainsSecret(GrantedBy))
            errors.Add("Consent grant contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserConsentDecision(
    BrowserConsentStatus Status,
    BrowserConsentRequest Request,
    BrowserConsentGrant? Grant,
    BrowserConsentAuditEvent AuditEvent,
    string Message)
{
    public bool AllowsPolicyEvaluation(DateTimeOffset now) =>
        Grant?.IsActive(now) == true;

    public bool MarksStepDone => false;
    public bool AuthorizesAction => false;
}
