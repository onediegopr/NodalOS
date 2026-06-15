namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPrivateTrialLifecycleState
{
    Requested,
    AdminApproved,
    LicenseCreated,
    BillingPreviewCreated,
    EmailDraftQueued,
    Active,
    Expiring,
    Expired,
    Revoked,
    Blocked
}

public sealed record NexaPrivateTrialSimulationRequest(
    string RequestId,
    string AccountId,
    string Email,
    NexaRole ApproverRole,
    bool AdminApproved,
    IReadOnlySet<NexaFeatureFlag> RequestedFeatures,
    IReadOnlyList<NexaUsageLimit> Limits,
    NexaPaymentProviderConfig BillingProvider,
    NexaEmailProviderConfig EmailProvider,
    DateTimeOffset RequestedAtUtc);

public sealed record NexaPrivateTrialBillingSimulation(
    bool InvoicePreviewCreated,
    bool RealChargeCreated,
    bool PaymentCardDataStored,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record NexaPrivateTrialEmailSimulation(
    bool EmailDraftQueued,
    bool RealEmailSent,
    string OutboxRef,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record NexaPrivateTrialAuditEvent(
    string EventId,
    string RequestId,
    string AccountId,
    NexaPrivateTrialLifecycleState State,
    string Reason,
    bool Redacted);

public sealed record NexaPrivateTrialSimulationResult(
    IReadOnlyList<NexaPrivateTrialLifecycleState> States,
    NexaLicense? License,
    NexaPrivateTrialBillingSimulation Billing,
    NexaPrivateTrialEmailSimulation Email,
    NexaPrivateTrialAuditEvent Audit,
    IReadOnlyList<string> Violations,
    bool Redacted)
{
    public bool Succeeded => Violations.Count == 0 && States.Contains(NexaPrivateTrialLifecycleState.Active) && Redacted;
}
