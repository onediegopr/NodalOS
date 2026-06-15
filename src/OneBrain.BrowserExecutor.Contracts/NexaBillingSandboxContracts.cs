namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPaymentProviderKind
{
    Disabled,
    MockOnly,
    SandboxProvider,
    RealProviderFuture,
    Unknown
}

public enum NexaPaymentDecisionKind
{
    SandboxCreated,
    Blocked,
    FailClosed,
    DesignOnly
}

public enum NexaSubscriptionLifecycleEventKind
{
    SubscriptionCreatedSandbox,
    SubscriptionCanceledSandbox,
    InvoicePaidSandbox,
    InvoiceFailedSandbox
}

public sealed record NexaPaymentProviderConfig(
    NexaPaymentProviderKind ProviderKind,
    bool PrivatePreviewLocal,
    bool RealBillingEnabled,
    bool SandboxAllowed,
    bool StoresPaymentCardData);

public sealed record NexaSubscriptionPlanBinding(NexaPlanKind Plan, string SandboxPriceId, bool EnablesSensitiveRealPilot, bool EnablesProductiveVaultByDefault);

public sealed record NexaCheckoutSessionRequest(string RequestId, string AccountId, NexaPlanKind Plan, decimal Amount, string Currency, bool ContainsPaymentCardData);
public sealed record NexaCheckoutSessionResult(NexaPaymentDecisionKind Decision, string SessionId, bool RealChargeCreated, bool Redacted, IReadOnlyList<string> ReasonCodes);

public sealed record NexaPaymentIntentRequest(string RequestId, string AccountId, decimal Amount, string Currency, bool ContainsPaymentCardData);
public sealed record NexaPaymentIntentResult(NexaPaymentDecisionKind Decision, string IntentId, bool RealChargeCreated, bool PaymentCardDataStored, bool Redacted, IReadOnlyList<string> ReasonCodes);

public sealed record NexaSubscriptionLifecycleEvent(string EventId, NexaSubscriptionLifecycleEventKind Kind, string AccountId, NexaPlanKind Plan, bool Redacted);

public sealed record NexaBillingWebhookEvent(string EventId, string EventType, string AccountId, bool SandboxOnly, bool ContainsPaymentCardData);
public sealed record NexaBillingWebhookDecision(NexaPaymentDecisionKind Decision, IReadOnlyList<string> ReasonCodes, bool EnablesSensitiveFeatures, bool Redacted);

public sealed record NexaBillingAuditEvent(string EventId, string AccountId, NexaPaymentProviderKind ProviderKind, string Action, bool RealChargeCreated, bool PaymentCardDataStored, bool Redacted);

public sealed record NexaBillingSandboxLedger(IReadOnlyList<NexaBillingAuditEvent> Events, bool RealMoneyCharged, bool StoresPaymentCardData, bool EnablesSensitiveFeaturesByDefault);
