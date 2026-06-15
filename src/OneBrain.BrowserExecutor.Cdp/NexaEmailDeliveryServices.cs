using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaEmailDeliveryProvider
{
    private readonly List<NexaEmailDeliveryResult> _outbox = [];

    public IReadOnlyList<NexaEmailDeliveryResult> Outbox => _outbox;

    public NexaEmailDeliveryResult Deliver(NexaEmailProviderConfig config, NexaEmailDeliveryRequest request)
    {
        if (config.ProviderKind is NexaEmailProviderKind.Unknown or NexaEmailProviderKind.Disabled)
            return Result(config.ProviderKind, request, NexaEmailDeliveryDecision.FailClosed, null, false, "email provider unavailable");
        if (config.ProviderKind == NexaEmailProviderKind.RealProviderFuture || config.RealEmailDeliveryEnabled)
            return Result(config.ProviderKind, request, NexaEmailDeliveryDecision.DesignOnly, null, false, "real email delivery blocked before future hito");
        if (config.ProviderKind == NexaEmailProviderKind.SandboxProvider && (!config.PrivatePreviewLocal || !config.SandboxAllowed))
            return Result(config.ProviderKind, request, NexaEmailDeliveryDecision.Blocked, null, false, "sandbox email requires local private preview");
        if (request.ContainsSecret || request.ContainsCookie || request.Parameters.Values.Any(BrowserCredentialRedactor.ContainsSecret))
            return Result(config.ProviderKind, request, NexaEmailDeliveryDecision.Blocked, null, false, "email request contains secret-like content");

        var rendered = Render(request.Template, request.Parameters);
        if (rendered.ContainsSecret || rendered.ContainsCookie)
            return Result(config.ProviderKind, request, NexaEmailDeliveryDecision.Blocked, rendered, false, "email template render contains sensitive content");
        return Result(config.ProviderKind, request, NexaEmailDeliveryDecision.QueuedSandbox, rendered, false, "email queued in sandbox/outbox only");
    }

    public NexaEmailTemplateRenderResult Render(NexaEmailTemplateKind template, IReadOnlyDictionary<string, string> parameters)
    {
        var account = BrowserCredentialRedactor.Redact(parameters.TryGetValue("account", out var value) ? value : "local-account");
        var subject = template switch
        {
            NexaEmailTemplateKind.FreeLicenseRequested => "Free license requested",
            NexaEmailTemplateKind.TrialCreated => "Trial created",
            NexaEmailTemplateKind.LicenseExpiring => "License expiring",
            NexaEmailTemplateKind.PlanUpgradeInterest => "Plan upgrade interest",
            NexaEmailTemplateKind.SupportBundleReady => "Support bundle ready",
            NexaEmailTemplateKind.PrivatePreviewFeedbackReceived => "Private preview feedback received",
            _ => "NEXA notification"
        };
        var body = BrowserCredentialRedactor.Redact($"Template {template} for {account}. Metadata only.");
        return new NexaEmailTemplateRenderResult(template, subject, body, Redacted: true, BrowserCredentialRedactor.ContainsSecret(subject) || BrowserCredentialRedactor.ContainsSecret(body), body.Contains("cookie", StringComparison.OrdinalIgnoreCase));
    }

    public NexaEmailSandboxOutbox Snapshot() => new(_outbox, SendsRealEmail: false);

    private NexaEmailDeliveryResult Result(NexaEmailProviderKind kind, NexaEmailDeliveryRequest request, NexaEmailDeliveryDecision decision, NexaEmailTemplateRenderResult? rendered, bool realSent, string reason)
    {
        var audit = new NexaEmailDeliveryAuditEvent($"email-audit-{Guid.NewGuid():N}", request.RequestId, kind, decision, BrowserCredentialRedactor.Redact(reason), Redacted: true);
        var result = new NexaEmailDeliveryResult(decision, kind, rendered, realSent, BrowserCredentialRedactor.Redact(reason), audit, Redacted: true);
        if (decision == NexaEmailDeliveryDecision.QueuedSandbox)
            _outbox.Add(result);
        return result;
    }
}
