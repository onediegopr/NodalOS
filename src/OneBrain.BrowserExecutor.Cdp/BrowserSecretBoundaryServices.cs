using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserSecretAccessPolicyEvaluator
{
    public BrowserSecretAccessDecision Decide(BrowserSecretAccessRequest request, BrowserSecretAccessPolicy policy, BrowserSecretAccessDecisionKind? forcedDecision = null)
    {
        var validation = request.Validate();
        var decision = forcedDecision ?? DecisionFor(request, policy, validation);
        var message = MessageFor(decision, request);
        var audit = Audit(request, decision, message);
        return new BrowserSecretAccessDecision(decision, BrowserCredentialRedactor.Redact(message), request, audit);
    }

    private static BrowserSecretAccessDecisionKind DecisionFor(BrowserSecretAccessRequest request, BrowserSecretAccessPolicy policy, ContractValidationResult validation)
    {
        if (!validation.IsValid || request.Secret.Kind == BrowserSecretKind.UnknownSensitiveSecret || request.Intent == BrowserSecretUsageIntent.Unknown)
            return BrowserSecretAccessDecisionKind.FailClosed;

        if (request.Secret.Kind is BrowserSecretKind.Cookie or BrowserSecretKind.SessionCookie && !policy.AllowCookieAccess)
            return BrowserSecretAccessDecisionKind.Denied;

        if (request.Secret.Kind is BrowserSecretKind.Password or BrowserSecretKind.ClaveFiscal or BrowserSecretKind.BankCredential && policy.RequiresHumanForPassword)
            return BrowserSecretAccessDecisionKind.RequiresHuman;

        if (request.Intent is BrowserSecretUsageIntent.SubmitCredential or BrowserSecretUsageIntent.FillCredential && !policy.AllowCredentialFill)
            return BrowserSecretAccessDecisionKind.RequiresApproval;

        if (!policy.AllowedKinds.Contains(request.Secret.Kind) || !policy.AllowedScopes.Contains(request.Secret.Scope))
            return policy.DenyByDefault ? BrowserSecretAccessDecisionKind.Denied : BrowserSecretAccessDecisionKind.RequiresApproval;

        return BrowserSecretAccessDecisionKind.Allowed;
    }

    private static string MessageFor(BrowserSecretAccessDecisionKind decision, BrowserSecretAccessRequest request) => decision switch
    {
        BrowserSecretAccessDecisionKind.Allowed => $"secret reference allowed for {request.Intent}",
        BrowserSecretAccessDecisionKind.RequiresHuman => "secret requires human handoff",
        BrowserSecretAccessDecisionKind.RequiresApproval => "secret access requires approval",
        BrowserSecretAccessDecisionKind.RequiresVault => "secret requires configured vault",
        BrowserSecretAccessDecisionKind.RequiresConsent => "secret access requires consent",
        BrowserSecretAccessDecisionKind.FailClosed => "secret access failed closed",
        _ => "secret access denied"
    };

    private static BrowserSecretAuditEvent Audit(BrowserSecretAccessRequest request, BrowserSecretAccessDecisionKind decision, string message) =>
        new(
            EventId: $"secret-audit-{Guid.NewGuid():N}",
            RequestId: request.RequestId,
            RunId: request.RunId,
            ActionId: request.ActionId,
            CorrelationId: request.CorrelationId,
            SecretId: request.Secret.SecretId,
            Kind: request.Secret.Kind,
            Scope: request.Secret.Scope,
            Intent: request.Intent,
            Decision: decision,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RedactedSummary: BrowserCredentialRedactor.Redact(message),
            RedactionApplied: true);
}

public sealed class NullBrowserSecretVault : IBrowserSecretVault
{
    private readonly List<BrowserSecretAuditEvent> _audit = [];
    private readonly BrowserSecretAccessPolicyEvaluator _evaluator = new();

    public IReadOnlyList<BrowserSecretAuditEvent> AuditEvents => _audit;

    public Task<BrowserSecretVaultResult> RequestAccessAsync(BrowserSecretAccessRequest request, BrowserSecretAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        var decision = _evaluator.Decide(request, policy, BrowserSecretAccessDecisionKind.Denied);
        _audit.Add(decision.AuditEvent);
        return Task.FromResult(new BrowserSecretVaultResult(decision, null, "null vault denies all secret access", true));
    }
}

public sealed class InMemoryTestSecretVault : IBrowserSecretVault
{
    private sealed record SyntheticSecret(BrowserSecretReference Reference, string Value);

    private readonly Dictionary<string, SyntheticSecret> _secrets = new(StringComparer.Ordinal);
    private readonly List<BrowserSecretAuditEvent> _audit = [];
    private readonly BrowserSecretAccessPolicyEvaluator _evaluator = new();

    public IReadOnlyList<BrowserSecretAuditEvent> AuditEvents => _audit;

    public BrowserSecretReference StoreSyntheticSecret(BrowserSecretKind kind, BrowserSecretScope scope, string owner, string portal, string syntheticValue)
    {
        if (!syntheticValue.StartsWith("synthetic://", StringComparison.Ordinal))
            throw new InvalidOperationException("InMemoryTestSecretVault only accepts synthetic test secrets.");

        var reference = new BrowserSecretReference(
            SecretId: $"synthetic-secret-{Guid.NewGuid():N}",
            Kind: kind,
            Scope: scope,
            Owner: BrowserCredentialRedactor.Redact(owner),
            Portal: BrowserCredentialRedactor.Redact(portal),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RedactedLabel: $"{kind} {scope} [REDACTED]");
        _secrets[reference.SecretId] = new SyntheticSecret(reference, syntheticValue);
        return reference;
    }

    public Task<BrowserSecretVaultResult> RequestAccessAsync(BrowserSecretAccessRequest request, BrowserSecretAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        BrowserSecretAccessDecisionKind? forced = policy.AllowSyntheticTestSecretsOnly && _secrets.ContainsKey(request.Secret.SecretId)
            ? null
            : BrowserSecretAccessDecisionKind.Denied;
        var decision = _evaluator.Decide(request, policy, forced);
        _audit.Add(decision.AuditEvent);
        var reference = decision.AllowsAccess ? request.Secret : null;
        return Task.FromResult(new BrowserSecretVaultResult(decision, reference, "synthetic test vault returned a reference only", true));
    }
}

public sealed class BrowserProfileConsentManager
{
    private readonly Dictionary<string, BrowserProfileConsentDecision> _decisions = new(StringComparer.Ordinal);
    private readonly List<BrowserProfileConsentAuditEvent> _audit = [];

    public IReadOnlyList<BrowserProfileConsentAuditEvent> AuditEvents => _audit;

    public BrowserProfileConsentRequest CreateRequest(string profileId, string sessionId, string correlationId, BrowserProfileConsentScope scope, string owner, string purpose, TimeSpan? ttl = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrowserProfileConsentRequest(
            RequestId: $"profile-consent-{Guid.NewGuid():N}",
            ProfileId: BrowserCredentialRedactor.Redact(profileId),
            SessionId: BrowserCredentialRedactor.Redact(sessionId),
            CorrelationId: BrowserCredentialRedactor.Redact(correlationId),
            Scope: scope,
            Owner: BrowserCredentialRedactor.Redact(owner),
            Purpose: BrowserCredentialRedactor.Redact(purpose),
            RequestedAtUtc: now,
            ExpiresAtUtc: now + (ttl ?? TimeSpan.FromMinutes(15)),
            RedactionApplied: true);
    }

    public BrowserProfileConsentDecision Decide(BrowserProfileConsentRequest request, BrowserProfileConsentStatus status, DateTimeOffset now)
    {
        var finalStatus = request.IsExpired(now) && status == BrowserProfileConsentStatus.Granted
            ? BrowserProfileConsentStatus.Expired
            : status;
        var audit = Audit(request, finalStatus);
        var decision = new BrowserProfileConsentDecision(finalStatus, request, audit, MessageFor(finalStatus));
        _decisions[request.RequestId] = decision;
        _audit.Add(audit);
        return decision;
    }

    public BrowserProfileConsentDecision Revoke(BrowserProfileConsentRequest request) =>
        Decide(request, BrowserProfileConsentStatus.Revoked, DateTimeOffset.UtcNow);

    public BrowserProfileConsentDecision Evaluate(BrowserProfileConsentRequest request, DateTimeOffset now)
    {
        if (!_decisions.TryGetValue(request.RequestId, out var decision))
            return Decide(request, BrowserProfileConsentStatus.NotRequested, now);
        if (decision.Status == BrowserProfileConsentStatus.Granted && request.IsExpired(now))
            return Decide(request, BrowserProfileConsentStatus.Expired, now);
        return decision;
    }

    private static BrowserProfileConsentAuditEvent Audit(BrowserProfileConsentRequest request, BrowserProfileConsentStatus status) =>
        new(
            EventId: $"profile-consent-audit-{Guid.NewGuid():N}",
            RequestId: request.RequestId,
            ProfileId: request.ProfileId,
            Scope: request.Scope,
            Status: status,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RedactedSummary: BrowserCredentialRedactor.Redact($"profile consent {status} for {request.Scope}"),
            RedactionApplied: true);

    private static string MessageFor(BrowserProfileConsentStatus status) => status switch
    {
        BrowserProfileConsentStatus.Granted => "real profile consent granted for scoped request",
        BrowserProfileConsentStatus.Denied => "real profile consent denied",
        BrowserProfileConsentStatus.Expired => "real profile consent expired",
        BrowserProfileConsentStatus.Revoked => "real profile consent revoked",
        _ => "real profile consent not granted"
    };
}
