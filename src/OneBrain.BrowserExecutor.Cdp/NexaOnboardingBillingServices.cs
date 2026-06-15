using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public interface NexaBillingProvider
{
    NexaBillingProviderKind Kind { get; }
    NexaBillingCheckoutResult CreateCheckout(NexaBillingCheckoutRequest request);
}

public sealed class NexaBillingMockProvider : NexaBillingProvider
{
    public NexaBillingProviderKind Kind => NexaBillingProviderKind.MockOnly;

    public NexaBillingCheckoutResult CreateCheckout(NexaBillingCheckoutRequest request)
    {
        var preview = new NexaBillingInvoicePreview($"invoice-preview-{Guid.NewGuid():N}", request.AccountId, request.Plan, request.Amount, request.Currency, MockOnly: true, Redacted: true);
        return new NexaBillingCheckoutResult(Kind, NexaBillingSubscriptionStatus.MockPreview, preview, RealChargeCreated: false, "mock checkout preview created; no real charge", Redacted: true);
    }
}

public sealed class NexaEmailOutboxMock
{
    private readonly List<NexaEmailMessageDraft> _drafts = [];

    public IReadOnlyList<NexaEmailMessageDraft> Drafts => _drafts;

    public NexaEmailMessageDraft Queue(string to, NexaEmailTemplate template, string subject, string body)
    {
        var draft = new NexaEmailMessageDraft($"email-draft-{Guid.NewGuid():N}", BrowserCredentialRedactor.Redact(to), template, BrowserCredentialRedactor.Redact(subject), BrowserCredentialRedactor.Redact(body), NexaEmailDeliveryMode.MockOutboxOnly, Sent: false, Redacted: true);
        var validation = draft.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));
        _drafts.Add(draft);
        return draft;
    }
}

public sealed class NexaOnboardingService
{
    private readonly NexaBillingProvider _billing;
    private readonly NexaEmailOutboxMock _email;

    public NexaOnboardingService(NexaBillingProvider? billing = null, NexaEmailOutboxMock? email = null)
    {
        _billing = billing ?? new NexaBillingMockProvider();
        _email = email ?? new NexaEmailOutboxMock();
    }

    public NexaEmailOutboxMock EmailOutbox => _email;

    public NexaOnboardingResult Start(NexaOnboardingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || BrowserCredentialRedactor.ContainsSecret(request.Email))
            return Result(request, NexaOnboardingStatus.Denied, "valid email required", null, [], null, null);

        return request.RequestedPlan switch
        {
            NexaPlanKind.Free => Free(request),
            NexaPlanKind.Trial => Trial(request),
            NexaPlanKind.Pro => MockCheckout(request, NexaPlan.Pro(), NexaEmailTemplate.PlanUpgradeInterest),
            NexaPlanKind.Enterprise => MockCheckout(request, NexaPlan.Enterprise(), NexaEmailTemplate.PlanUpgradeInterest),
            _ => Result(request, NexaOnboardingStatus.Denied, "unsupported plan", null, [], null, null)
        };
    }

    public NexaOnboardingResult RevokeTrial(NexaOnboardingResult existing)
    {
        if (existing.License is null)
            return existing;
        var revoked = existing.License with { Status = NexaLicenseStatus.Revoked };
        var audit = new NexaOnboardingAuditEvent($"onboarding-audit-{Guid.NewGuid():N}", existing.AuditEvent.RequestId, existing.AuditEvent.AccountId, existing.AuditEvent.Plan, NexaOnboardingStatus.Revoked, "trial revoked", DateTimeOffset.UtcNow, Redacted: true);
        return existing with { Decision = new NexaOnboardingDecision(NexaOnboardingStatus.Revoked, "trial revoked", Redacted: true), License = revoked, AuditEvent = audit };
    }

    private NexaOnboardingResult Free(NexaOnboardingRequest request)
    {
        var free = new NexaFreeLicenseRequest(request.Email, request.RequestedAtUtc, request.PreviousFreeIssuedAtUtc, TimeSpan.FromDays(30));
        if (!free.CanGenerate)
            return Result(request, NexaOnboardingStatus.Denied, "free plan already issued within window", null, [NexaOnboardingStep.RequestReceived, NexaOnboardingStep.EmailValidated], null, null);
        var license = free.Generate(request.AccountId);
        var email = _email.Queue(request.Email, NexaEmailTemplate.FreeLicenseRequested, "Free license requested", "Free license draft queued");
        return Result(request, NexaOnboardingStatus.Activated, "free plan activated in mock onboarding", license, [NexaOnboardingStep.RequestReceived, NexaOnboardingStep.EmailValidated, NexaOnboardingStep.LicenseCreated, NexaOnboardingStep.EmailDraftQueued, NexaOnboardingStep.Activated], null, email);
    }

    private NexaOnboardingResult Trial(NexaOnboardingRequest request)
    {
        if (!request.AdminApproved)
            return Result(request, NexaOnboardingStatus.Denied, "trial requires admin approval", null, [NexaOnboardingStep.RequestReceived], null, null);
        var now = request.RequestedAtUtc;
        var plan = NexaPlan.Trial(request.TrialFeatures, request.TrialLimits, TimeSpan.FromDays(14));
        var license = new NexaLicense($"license-trial-{Guid.NewGuid():N}", request.AccountId, plan, NexaLicenseStatus.Active, now, now.Add(plan.DefaultDuration ?? TimeSpan.FromDays(14)), [], ManualAdminOverride: false);
        var email = _email.Queue(request.Email, NexaEmailTemplate.TrialCreated, "Trial created", "Trial license draft queued");
        return Result(request, NexaOnboardingStatus.Activated, "trial activated in mock onboarding", license, [NexaOnboardingStep.RequestReceived, NexaOnboardingStep.EmailValidated, NexaOnboardingStep.LicenseCreated, NexaOnboardingStep.EmailDraftQueued, NexaOnboardingStep.Activated], null, email);
    }

    private NexaOnboardingResult MockCheckout(NexaOnboardingRequest request, NexaPlan plan, NexaEmailTemplate template)
    {
        var billing = _billing.CreateCheckout(new NexaBillingCheckoutRequest($"checkout-{Guid.NewGuid():N}", request.AccountId, plan.Kind, 0m, "USD"));
        var email = _email.Queue(request.Email, template, "Plan interest received", "Mock checkout preview queued");
        return Result(request, NexaOnboardingStatus.Approved, "mock checkout preview created", null, [NexaOnboardingStep.RequestReceived, NexaOnboardingStep.BillingPreviewCreated, NexaOnboardingStep.EmailDraftQueued], billing, email);
    }

    private static NexaOnboardingResult Result(NexaOnboardingRequest request, NexaOnboardingStatus status, string reason, NexaLicense? license, IReadOnlyList<NexaOnboardingStep> steps, NexaBillingCheckoutResult? billing, NexaEmailMessageDraft? email)
    {
        var audit = new NexaOnboardingAuditEvent($"onboarding-audit-{Guid.NewGuid():N}", request.RequestId, request.AccountId, request.RequestedPlan, status, BrowserCredentialRedactor.Redact(reason), DateTimeOffset.UtcNow, Redacted: true);
        return new NexaOnboardingResult(new NexaOnboardingDecision(status, BrowserCredentialRedactor.Redact(reason), Redacted: true), license, steps, billing, email, audit, Redacted: true);
    }
}
