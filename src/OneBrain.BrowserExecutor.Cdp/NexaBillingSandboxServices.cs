using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPaymentProvider
{
    private readonly List<NexaBillingAuditEvent> _audit = [];

    public NexaBillingSandboxLedger Ledger => new(_audit, RealMoneyCharged: _audit.Any(audit => audit.RealChargeCreated), StoresPaymentCardData: _audit.Any(audit => audit.PaymentCardDataStored), EnablesSensitiveFeaturesByDefault: false);

    public NexaCheckoutSessionResult CreateCheckout(NexaPaymentProviderConfig config, NexaCheckoutSessionRequest request)
    {
        var reasons = Validate(config, request.ContainsPaymentCardData);
        if (reasons.Count > 0)
            return Checkout(request, config.ProviderKind, NexaPaymentDecisionKind.Blocked, reasons);
        return Checkout(request, config.ProviderKind, NexaPaymentDecisionKind.SandboxCreated, ["sandbox checkout session created"]);
    }

    public NexaPaymentIntentResult CreatePaymentIntent(NexaPaymentProviderConfig config, NexaPaymentIntentRequest request)
    {
        var reasons = Validate(config, request.ContainsPaymentCardData);
        if (reasons.Count > 0)
            return PaymentIntent(request, config.ProviderKind, NexaPaymentDecisionKind.Blocked, reasons);
        return PaymentIntent(request, config.ProviderKind, NexaPaymentDecisionKind.SandboxCreated, ["sandbox payment intent created"]);
    }

    public NexaBillingWebhookDecision HandleWebhook(NexaBillingWebhookEvent webhook, IReadOnlyList<NexaSubscriptionPlanBinding> bindings)
    {
        var reasons = new List<string>();
        if (!webhook.SandboxOnly)
            reasons.Add("billing webhook must be sandbox-only");
        if (webhook.ContainsPaymentCardData)
            reasons.Add("payment card data blocked");
        if (bindings.Any(binding => binding.EnablesSensitiveRealPilot || binding.EnablesProductiveVaultByDefault))
            reasons.Add("billing webhook cannot enable sensitive features by default");
        return new NexaBillingWebhookDecision(reasons.Count == 0 ? NexaPaymentDecisionKind.SandboxCreated : NexaPaymentDecisionKind.Blocked, reasons, EnablesSensitiveFeatures: false, Redacted: true);
    }

    private List<string> Validate(NexaPaymentProviderConfig config, bool containsPaymentCardData)
    {
        var reasons = new List<string>();
        if (config.ProviderKind is NexaPaymentProviderKind.Unknown or NexaPaymentProviderKind.Disabled)
            reasons.Add("payment provider unavailable");
        if (config.ProviderKind == NexaPaymentProviderKind.RealProviderFuture || config.RealBillingEnabled)
            reasons.Add("real billing blocked before future hito");
        if (config.ProviderKind == NexaPaymentProviderKind.SandboxProvider && (!config.PrivatePreviewLocal || !config.SandboxAllowed))
            reasons.Add("sandbox billing requires local private preview");
        if (containsPaymentCardData || config.StoresPaymentCardData)
            reasons.Add("payment card data blocked");
        return reasons;
    }

    private NexaCheckoutSessionResult Checkout(NexaCheckoutSessionRequest request, NexaPaymentProviderKind provider, NexaPaymentDecisionKind decision, IReadOnlyList<string> reasons)
    {
        _audit.Add(new NexaBillingAuditEvent($"billing-audit-{Guid.NewGuid():N}", request.AccountId, provider, "checkout", RealChargeCreated: false, PaymentCardDataStored: false, Redacted: true));
        return new NexaCheckoutSessionResult(decision, $"checkout-session-{Guid.NewGuid():N}", RealChargeCreated: false, Redacted: true, reasons);
    }

    private NexaPaymentIntentResult PaymentIntent(NexaPaymentIntentRequest request, NexaPaymentProviderKind provider, NexaPaymentDecisionKind decision, IReadOnlyList<string> reasons)
    {
        _audit.Add(new NexaBillingAuditEvent($"billing-audit-{Guid.NewGuid():N}", request.AccountId, provider, "payment-intent", RealChargeCreated: false, PaymentCardDataStored: false, Redacted: true));
        return new NexaPaymentIntentResult(decision, $"payment-intent-{Guid.NewGuid():N}", RealChargeCreated: false, PaymentCardDataStored: false, Redacted: true, reasons);
    }
}
