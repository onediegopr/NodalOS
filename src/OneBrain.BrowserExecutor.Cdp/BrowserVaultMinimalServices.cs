using System.Security.Cryptography;
using System.Text;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserVaultMinimalSandboxProvider
{
    private readonly byte[] _key;
    private readonly Dictionary<string, StoredVaultItem> _items = new(StringComparer.Ordinal);
    private readonly List<BrowserVaultAuditEvent> _audit = [];

    public BrowserVaultMinimalSandboxProvider()
    {
        _key = RandomNumberGenerator.GetBytes(32);
    }

    public BrowserVaultMinimalProviderKind Kind => BrowserVaultMinimalProviderKind.SandboxLocalEncrypted;
    public IReadOnlyList<BrowserVaultAuditEvent> AuditEvents => _audit;

    public Task<BrowserVaultStoreResult> StoreSandboxAsync(BrowserVaultStoreRequest request, string sandboxValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validation = request.Validate();
        if (!validation.IsValid || string.IsNullOrWhiteSpace(sandboxValue))
            return Task.FromResult(StoreResult(request, BrowserVaultAccessDecisionKind.FailClosed, "vault item store failed closed", null));
        if (!request.Reference.ProviderKind.Equals(Kind))
            return Task.FromResult(StoreResult(request, BrowserVaultAccessDecisionKind.FailClosed, "vault provider mismatch", null));

        var encrypted = Encrypt(sandboxValue);
        _items[request.Reference.ReferenceId] = new StoredVaultItem(request.Reference, encrypted, BrowserVaultSecretLifecycleState.Active);
        return Task.FromResult(StoreResult(request, BrowserVaultAccessDecisionKind.Allowed, "vault item stored for sandbox", request.Reference));
    }

    public Task<BrowserVaultRetrieveResult> RetrieveAsync(BrowserVaultRetrieveRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var decision = EvaluateRetrieve(request, DateTimeOffset.UtcNow);
        return Task.FromResult(RetrieveResult(request, decision.Decision, decision.Reason, decision.Reference));
    }

    internal Task<BrowserVaultCoreSecretHandle?> RetrieveForCoreBoundaryAsync(BrowserVaultRetrieveRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var decision = EvaluateRetrieve(request, DateTimeOffset.UtcNow);
        var result = RetrieveResult(request, decision.Decision, decision.Reason, decision.Reference);
        if (!result.AllowsCoreUse || !_items.TryGetValue(request.Reference.ReferenceId, out var stored))
            return Task.FromResult<BrowserVaultCoreSecretHandle?>(null);

        return Task.FromResult<BrowserVaultCoreSecretHandle?>(new BrowserVaultCoreSecretHandle(stored.Reference, Decrypt(stored.EncryptedValue), result.AuditEvent));
    }

    public Task<BrowserVaultMutationResult> RevokeAsync(BrowserVaultRevocationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_items.TryGetValue(request.Reference.ReferenceId, out var stored))
            return Task.FromResult(MutationResult(request, BrowserVaultMinimalOperationKind.Revoke, BrowserVaultAccessDecisionKind.UnknownSecret, "vault item unknown", null));

        var revoked = stored with { State = BrowserVaultSecretLifecycleState.Revoked };
        _items[request.Reference.ReferenceId] = revoked;
        return Task.FromResult(MutationResult(request, BrowserVaultMinimalOperationKind.Revoke, BrowserVaultAccessDecisionKind.Revoked, "vault item revoked", revoked.Reference));
    }

    public Task<BrowserVaultMutationResult> RotateAsync(BrowserVaultRotationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_items.TryGetValue(request.Reference.ReferenceId, out var stored))
            return Task.FromResult(MutationResult(request, BrowserVaultMinimalOperationKind.Rotate, BrowserVaultAccessDecisionKind.UnknownSecret, "vault item unknown", null));

        _items[request.Reference.ReferenceId] = stored with { State = BrowserVaultSecretLifecycleState.Rotated };
        return Task.FromResult(MutationResult(request, BrowserVaultMinimalOperationKind.Rotate, BrowserVaultAccessDecisionKind.Allowed, "vault item rotation audited", stored.Reference));
    }

    private RetrieveDecision EvaluateRetrieve(BrowserVaultRetrieveRequest request, DateTimeOffset now)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
            return new(BrowserVaultAccessDecisionKind.FailClosed, "vault item retrieval failed closed", null);
        if (!request.Policy.AllowSandboxProvider || request.Policy.RequiredProviderKind != Kind || request.Reference.ProviderKind != Kind)
            return new(BrowserVaultAccessDecisionKind.Disabled, "vault provider disabled", null);
        if (request.Policy.AllowCompanionExposure || request.Policy.AllowPublicValueReturn)
            return new(BrowserVaultAccessDecisionKind.FailClosed, "vault policy is unsafe", null);
        if (request.Policy.RequirePhaseGatePassed && request.PhaseGateReport?.Passed != true)
            return new(BrowserVaultAccessDecisionKind.FailClosed, "phase gate failed", null);
        if (request.PhaseGateReport?.ObservedState is { VaultModeAllowed: false } or { CompanionAuthoritative: true })
            return new(BrowserVaultAccessDecisionKind.FailClosed, "runtime state blocks vault use", null);
        if (request.Policy.RequireConsent &&
            (request.Consent is null || !request.Consent.AllowsCapability(request.Policy.RequiredCapability, request.Policy.RequiredConsentScope, now)))
            return new(BrowserVaultAccessDecisionKind.RequiresConsent, "scoped consent required", null);
        if (!_items.TryGetValue(request.Reference.ReferenceId, out var stored) || !ReferenceMatches(request.Reference, stored.Reference))
            return new(BrowserVaultAccessDecisionKind.UnknownSecret, "vault item unknown", null);
        if (stored.State == BrowserVaultSecretLifecycleState.Revoked)
            return new(BrowserVaultAccessDecisionKind.Revoked, "vault item revoked", stored.Reference);
        return new(BrowserVaultAccessDecisionKind.Allowed, "vault item available to core boundary", stored.Reference);
    }

    private BrowserVaultStoreResult StoreResult(BrowserVaultStoreRequest request, BrowserVaultAccessDecisionKind decision, string reason, BrowserVaultSecretReference? reference)
    {
        var audit = Audit(BrowserVaultMinimalOperationKind.Store, decision, request.Reference, request.RunId, request.ActionId, request.CorrelationId, request.ProfileId, request.SessionId, reason);
        return new BrowserVaultStoreResult(decision, reference, audit, Evidence(decision, request.CorrelationId), Redacted: true);
    }

    private BrowserVaultRetrieveResult RetrieveResult(BrowserVaultRetrieveRequest request, BrowserVaultAccessDecisionKind decision, string reason, BrowserVaultSecretReference? reference)
    {
        var audit = Audit(BrowserVaultMinimalOperationKind.Retrieve, decision, request.Reference, request.RunId, request.ActionId, request.CorrelationId, request.ProfileId, request.SessionId, reason);
        return new BrowserVaultRetrieveResult(decision, reference, audit, Evidence(decision, request.CorrelationId), Redacted: true);
    }

    private BrowserVaultMutationResult MutationResult(BrowserVaultRevocationRequest request, BrowserVaultMinimalOperationKind operation, BrowserVaultAccessDecisionKind decision, string reason, BrowserVaultSecretReference? reference)
    {
        var audit = Audit(operation, decision, request.Reference, request.RunId, request.ActionId, request.CorrelationId, request.ProfileId, request.SessionId, reason);
        return new BrowserVaultMutationResult(decision, reference, audit, Evidence(decision, request.CorrelationId), Redacted: true);
    }

    private BrowserVaultMutationResult MutationResult(BrowserVaultRotationRequest request, BrowserVaultMinimalOperationKind operation, BrowserVaultAccessDecisionKind decision, string reason, BrowserVaultSecretReference? reference)
    {
        var audit = Audit(operation, decision, request.Reference, request.RunId, request.ActionId, request.CorrelationId, request.ProfileId, request.SessionId, reason);
        return new BrowserVaultMutationResult(decision, reference, audit, Evidence(decision, request.CorrelationId), Redacted: true);
    }

    private BrowserVaultAuditEvent Audit(BrowserVaultMinimalOperationKind operation, BrowserVaultAccessDecisionKind decision, BrowserVaultSecretReference reference, string runId, string actionId, string correlationId, string profileId, string sessionId, string reason)
    {
        var audit = new BrowserVaultAuditEvent(
            EventId: $"vault-audit-{Guid.NewGuid():N}",
            operation,
            decision,
            reference,
            runId,
            actionId,
            correlationId,
            profileId,
            sessionId,
            BrowserCredentialRedactor.Redact(reason),
            DateTimeOffset.UtcNow,
            RedactionApplied: true);
        _audit.Add(audit);
        return audit;
    }

    private static IReadOnlyList<string> Evidence(BrowserVaultAccessDecisionKind decision, string correlationId) =>
        [$"vault:{decision}:{correlationId}"];

    private byte[] Encrypt(string value)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plain = Encoding.UTF8.GetBytes(value);
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        return [.. aes.IV, .. cipher];
    }

    private string Decrypt(byte[] encrypted)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = encrypted.Take(aes.BlockSize / 8).ToArray();
        using var decryptor = aes.CreateDecryptor();
        var cipher = encrypted.Skip(aes.IV.Length).ToArray();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }

    private static bool ReferenceMatches(BrowserVaultSecretReference request, BrowserVaultSecretReference stored) =>
        request.ReferenceId == stored.ReferenceId &&
        request.Kind == stored.Kind &&
        request.Scope == stored.Scope &&
        request.Owner == stored.Owner &&
        request.Portal == stored.Portal &&
        request.ProviderKind == stored.ProviderKind;

    private sealed record StoredVaultItem(BrowserVaultSecretReference Reference, byte[] EncryptedValue, BrowserVaultSecretLifecycleState State);
    private sealed record RetrieveDecision(BrowserVaultAccessDecisionKind Decision, string Reason, BrowserVaultSecretReference? Reference);
}

internal sealed class BrowserVaultCoreSecretHandle : IDisposable
{
    private string? _value;

    internal BrowserVaultCoreSecretHandle(BrowserVaultSecretReference reference, string value, BrowserVaultAuditEvent auditEvent)
    {
        Reference = reference;
        _value = value;
        AuditEvent = auditEvent;
    }

    public BrowserVaultSecretReference Reference { get; }
    public BrowserVaultAuditEvent AuditEvent { get; }

    internal string RequireValue() => _value ?? throw new ObjectDisposedException(nameof(BrowserVaultCoreSecretHandle));

    public void Dispose() => _value = null;
}
