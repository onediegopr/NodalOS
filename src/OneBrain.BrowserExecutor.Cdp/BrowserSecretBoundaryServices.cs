using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserSecretAccessPolicyEvaluator
{
    public BrowserSecretAccessDecision Decide(BrowserSecretAccessRequest request, BrowserSecretAccessPolicy policy)
    {
        var validation = request.Validate();
        var decision = DecisionFor(request, policy, validation);
        return CreateDecision(request, decision);
    }

    public BrowserSecretAccessDecision Deny(BrowserSecretAccessRequest request)
    {
        var validation = request.Validate();
        var decision = validation.IsValid ? BrowserSecretAccessDecisionKind.Denied : BrowserSecretAccessDecisionKind.FailClosed;
        return CreateDecision(request, decision);
    }

    private static BrowserSecretAccessDecision CreateDecision(BrowserSecretAccessRequest request, BrowserSecretAccessDecisionKind decision)
    {
        var message = MessageFor(decision);
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

    private static string MessageFor(BrowserSecretAccessDecisionKind decision) => decision switch
    {
        BrowserSecretAccessDecisionKind.Allowed => "secret reference allowed by policy",
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
            SecretId: BrowserCredentialRedactor.Redact(request.Secret.SecretId),
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
        var decision = _evaluator.Deny(request);
        _audit.Add(decision.AuditEvent);
        return Task.FromResult(new BrowserSecretVaultResult(decision, null, "null vault denies all secret access", true));
    }
}

/// <summary>
/// Test-only vault for synthetic fixture secrets. It is not a productive secret store.
/// </summary>
public sealed class InMemoryTestOnlySecretVault : IBrowserSecretVault
{
    private sealed record SyntheticSecret(BrowserSecretReference Reference, string Value);

    private readonly Dictionary<string, SyntheticSecret> _secrets = new(StringComparer.Ordinal);
    private readonly List<BrowserSecretAuditEvent> _audit = [];
    private readonly BrowserSecretAccessPolicyEvaluator _evaluator = new();

    public IReadOnlyList<BrowserSecretAuditEvent> AuditEvents => _audit;

    public BrowserSecretReference StoreSyntheticSecret(BrowserSecretKind kind, BrowserSecretScope scope, string owner, string portal, string syntheticValue)
    {
        if (!syntheticValue.StartsWith("synthetic://", StringComparison.Ordinal))
            throw new InvalidOperationException("InMemoryTestOnlySecretVault only accepts synthetic test secrets.");

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
        if (!policy.AllowSyntheticTestSecretsOnly ||
            !_secrets.TryGetValue(request.Secret.SecretId, out var stored) ||
            !ReferenceMatches(request.Secret, stored.Reference))
        {
            var denied = _evaluator.Deny(request);
            _audit.Add(denied.AuditEvent);
            return Task.FromResult(new BrowserSecretVaultResult(denied, null, "synthetic test vault denied non-canonical request", true));
        }

        var canonicalRequest = request with { Secret = stored.Reference };
        var decision = _evaluator.Decide(canonicalRequest, policy);
        _audit.Add(decision.AuditEvent);
        var reference = decision.AllowsAccess ? stored.Reference : null;
        return Task.FromResult(new BrowserSecretVaultResult(decision, reference, "synthetic test vault returned a reference only", true));
    }

    private static bool ReferenceMatches(BrowserSecretReference request, BrowserSecretReference stored) =>
        string.Equals(request.SecretId, stored.SecretId, StringComparison.Ordinal) &&
        request.Kind == stored.Kind &&
        request.Scope == stored.Scope &&
        string.Equals(request.Owner, stored.Owner, StringComparison.Ordinal) &&
        string.Equals(request.Portal, stored.Portal, StringComparison.Ordinal) &&
        string.Equals(request.RedactedLabel, stored.RedactedLabel, StringComparison.Ordinal);
}

public sealed class BrowserProfileConsentManager
{
    private readonly Dictionary<string, BrowserProfileConsentDecision> _decisions = new(StringComparer.Ordinal);
    private readonly List<BrowserProfileConsentAuditEvent> _audit = [];

    public IReadOnlyList<BrowserProfileConsentAuditEvent> AuditEvents => _audit;

    public BrowserProfileConsentRequest CreateRequest(string profileId, string sessionId, string correlationId, BrowserProfileConsentScope scope, string owner, string purpose, TimeSpan? ttl = null, string requestingActor = "core-browser-runtime")
    {
        var now = DateTimeOffset.UtcNow;
        return new BrowserProfileConsentRequest(
            RequestId: $"profile-consent-{Guid.NewGuid():N}",
            ProfileId: BrowserCredentialRedactor.Redact(profileId),
            SessionId: BrowserCredentialRedactor.Redact(sessionId),
            CorrelationId: BrowserCredentialRedactor.Redact(correlationId),
            Scope: scope,
            RequestingActor: BrowserCredentialRedactor.Redact(requestingActor),
            ConsentChallengeId: $"profile-consent-challenge-{Guid.NewGuid():N}",
            Owner: BrowserCredentialRedactor.Redact(owner),
            Purpose: BrowserCredentialRedactor.Redact(purpose),
            RequestedAtUtc: now,
            ExpiresAtUtc: now + (ttl ?? TimeSpan.FromMinutes(15)),
            RedactionApplied: true);
    }

    public BrowserProfileConsentDecision Decide(
        BrowserProfileConsentRequest request,
        BrowserProfileConsentStatus status,
        DateTimeOffset now,
        BrowserProfileConsentAuthorityKind authorityKind = BrowserProfileConsentAuthorityKind.Unknown,
        string approvingActor = "",
        string approvalSource = "",
        string consentProofRef = "",
        string consentChallengeId = "",
        bool companionAuthoritative = false)
    {
        var finalStatus = FinalStatus(request, status, now, authorityKind, approvingActor, approvalSource, consentProofRef, consentChallengeId, companionAuthoritative);
        var audit = Audit(request, finalStatus, authorityKind);
        var decision = new BrowserProfileConsentDecision(
            finalStatus,
            request,
            audit,
            MessageFor(finalStatus),
            BrowserCredentialRedactor.Redact(approvingActor),
            BrowserCredentialRedactor.Redact(approvalSource),
            authorityKind,
            BrowserCredentialRedactor.Redact(consentProofRef),
            BrowserCredentialRedactor.Redact(consentChallengeId),
            companionAuthoritative,
            now,
            finalStatus == BrowserProfileConsentStatus.Revoked ? now : null);
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

    private static BrowserProfileConsentStatus FinalStatus(
        BrowserProfileConsentRequest request,
        BrowserProfileConsentStatus status,
        DateTimeOffset now,
        BrowserProfileConsentAuthorityKind authorityKind,
        string approvingActor,
        string approvalSource,
        string consentProofRef,
        string consentChallengeId,
        bool companionAuthoritative)
    {
        if (!request.Validate().IsValid)
            return BrowserProfileConsentStatus.Invalid;
        if (request.IsExpired(now) && status == BrowserProfileConsentStatus.Granted)
            return BrowserProfileConsentStatus.Expired;
        if (status != BrowserProfileConsentStatus.Granted)
            return status;
        if (companionAuthoritative ||
            authorityKind is BrowserProfileConsentAuthorityKind.Unknown or BrowserProfileConsentAuthorityKind.UserViaCompanionIntent ||
            string.IsNullOrWhiteSpace(approvingActor) ||
            string.IsNullOrWhiteSpace(approvalSource) ||
            string.IsNullOrWhiteSpace(consentProofRef) ||
            string.IsNullOrWhiteSpace(consentChallengeId) ||
            !string.Equals(consentChallengeId, request.ConsentChallengeId, StringComparison.Ordinal))
            return BrowserProfileConsentStatus.Invalid;
        return BrowserProfileConsentStatus.Granted;
    }

    private static BrowserProfileConsentAuditEvent Audit(BrowserProfileConsentRequest request, BrowserProfileConsentStatus status, BrowserProfileConsentAuthorityKind authorityKind) =>
        new(
            EventId: $"profile-consent-audit-{Guid.NewGuid():N}",
            RequestId: request.RequestId,
            ProfileId: request.ProfileId,
            Scope: request.Scope,
            Status: status,
            AuthorityKind: authorityKind,
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
