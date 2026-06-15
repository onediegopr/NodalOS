namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaOnboardingStatus
{
    Requested,
    Approved,
    Activated,
    Denied,
    Revoked
}

public enum NexaOnboardingStep
{
    RequestReceived,
    EmailValidated,
    LicenseCreated,
    BillingPreviewCreated,
    EmailDraftQueued,
    Activated
}

public enum NexaBillingProviderKind
{
    MockOnly,
    StripeReal,
    RedsysReal,
    PayPalReal,
    BankReal,
    ManualRealCharge
}

public enum NexaBillingSubscriptionStatus
{
    None,
    MockPreview,
    MockActive,
    Revoked
}

public enum NexaEmailDeliveryMode
{
    MockOutboxOnly,
    RealSmtp,
    RealApi
}

public enum NexaEmailTemplate
{
    FreeLicenseRequested,
    TrialCreated,
    LicenseExpiring,
    PlanUpgradeInterest
}

public sealed record NexaOnboardingRequest(
    string RequestId,
    string Email,
    string AccountId,
    NexaPlanKind RequestedPlan,
    bool AdminApproved,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? PreviousFreeIssuedAtUtc,
    IReadOnlySet<NexaFeatureFlag>? TrialFeatures = null,
    IReadOnlyList<NexaUsageLimit>? TrialLimits = null);

public sealed record NexaOnboardingAuditEvent(
    string EventId,
    string RequestId,
    string AccountId,
    NexaPlanKind Plan,
    NexaOnboardingStatus Status,
    string Reason,
    DateTimeOffset TimestampUtc,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(AccountId, nameof(AccountId), errors);
        if (!Redacted)
            errors.Add("Onboarding audit must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("Onboarding audit contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaBillingCheckoutRequest(string CheckoutId, string AccountId, NexaPlanKind Plan, decimal Amount, string Currency);

public sealed record NexaBillingInvoicePreview(string InvoiceId, string AccountId, NexaPlanKind Plan, decimal Amount, string Currency, bool MockOnly, bool Redacted);

public sealed record NexaBillingCheckoutResult(
    NexaBillingProviderKind ProviderKind,
    NexaBillingSubscriptionStatus Status,
    NexaBillingInvoicePreview? InvoicePreview,
    bool RealChargeCreated,
    string Reason,
    bool Redacted);

public sealed record NexaEmailMessageDraft(
    string MessageId,
    string To,
    NexaEmailTemplate Template,
    string Subject,
    string Body,
    NexaEmailDeliveryMode DeliveryMode,
    bool Sent,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(MessageId, nameof(MessageId), errors);
        if (DeliveryMode != NexaEmailDeliveryMode.MockOutboxOnly)
            errors.Add("Email delivery must be mock outbox only.");
        if (Sent)
            errors.Add("Mock email draft cannot be sent.");
        if (!Redacted || BrowserCredentialRedactor.ContainsSecret(Subject) || BrowserCredentialRedactor.ContainsSecret(Body))
            errors.Add("Email draft must be redacted.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaOnboardingDecision(NexaOnboardingStatus Status, string Reason, bool Redacted)
{
    public bool Allowed => Status is NexaOnboardingStatus.Approved or NexaOnboardingStatus.Activated;
}

public sealed record NexaOnboardingResult(
    NexaOnboardingDecision Decision,
    NexaLicense? License,
    IReadOnlyList<NexaOnboardingStep> Steps,
    NexaBillingCheckoutResult? Billing,
    NexaEmailMessageDraft? EmailDraft,
    NexaOnboardingAuditEvent AuditEvent,
    bool Redacted)
{
    public bool Succeeded => Decision.Allowed && Redacted && AuditEvent.Validate().IsValid && EmailDraft?.Validate().IsValid != false && Billing?.RealChargeCreated != true;
}
