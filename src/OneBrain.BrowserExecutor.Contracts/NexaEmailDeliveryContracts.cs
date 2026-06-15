namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaEmailProviderKind
{
    Disabled,
    MockOutboxOnly,
    SandboxProvider,
    RealProviderFuture,
    Unknown
}

public enum NexaEmailDeliveryDecision
{
    QueuedSandbox,
    Blocked,
    FailClosed,
    DesignOnly
}

public enum NexaEmailTemplateKind
{
    FreeLicenseRequested,
    TrialCreated,
    LicenseExpiring,
    PlanUpgradeInterest,
    SupportBundleReady,
    PrivatePreviewFeedbackReceived
}

public sealed record NexaEmailProviderConfig(
    NexaEmailProviderKind ProviderKind,
    bool PrivatePreviewLocal,
    bool RealEmailDeliveryEnabled,
    bool SandboxAllowed);

public sealed record NexaEmailTemplateDefinition(NexaEmailTemplateKind Template, string SubjectTemplate, string BodyTemplate, bool Redacted);

public sealed record NexaEmailTemplateRenderResult(NexaEmailTemplateKind Template, string Subject, string Body, bool Redacted, bool ContainsSecret, bool ContainsCookie);

public sealed record NexaEmailDeliveryRequest(
    string RequestId,
    string To,
    NexaEmailTemplateKind Template,
    IReadOnlyDictionary<string, string> Parameters,
    bool ContainsSecret,
    bool ContainsCookie);

public sealed record NexaEmailDeliveryAuditEvent(string EventId, string RequestId, NexaEmailProviderKind ProviderKind, NexaEmailDeliveryDecision Decision, string Reason, bool Redacted);

public sealed record NexaEmailDeliveryResult(
    NexaEmailDeliveryDecision Decision,
    NexaEmailProviderKind ProviderKind,
    NexaEmailTemplateRenderResult? Rendered,
    bool RealEmailSent,
    string Reason,
    NexaEmailDeliveryAuditEvent AuditEvent,
    bool Redacted);

public sealed record NexaEmailSandboxOutbox(IReadOnlyList<NexaEmailDeliveryResult> Messages, bool SendsRealEmail);
