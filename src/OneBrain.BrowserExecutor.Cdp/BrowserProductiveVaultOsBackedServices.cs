using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserProductiveVaultDpapiCurrentUserProvider
{
    private readonly Dictionary<string, StoredSecret> _items = new(StringComparer.Ordinal);
    private readonly List<BrowserProductiveVaultAuditEvent> _audit = [];

    public BrowserProductiveVaultProviderKind Kind => BrowserProductiveVaultProviderKind.OsBackedDpapiCurrentUser;
    public IReadOnlyList<BrowserProductiveVaultAuditEvent> AuditEvents => _audit;

    public BrowserProductiveVaultHealthCheck HealthCheck()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return new BrowserProductiveVaultHealthCheck(Kind, ProviderAvailable: false, OsBacked: true, SyntheticOnly: true, "DPAPI CurrentUser requires Windows", Redacted: true);
            var protectedBytes = ProtectCurrentUser(Encoding.UTF8.GetBytes("synthetic-health-check"));
            _ = UnprotectCurrentUser(protectedBytes);
            return new BrowserProductiveVaultHealthCheck(Kind, ProviderAvailable: true, OsBacked: true, SyntheticOnly: true, "DPAPI CurrentUser available", Redacted: true);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or CryptographicException)
        {
            return new BrowserProductiveVaultHealthCheck(Kind, ProviderAvailable: false, OsBacked: true, SyntheticOnly: true, "DPAPI CurrentUser unavailable", Redacted: true);
        }
    }

    public Task<BrowserProductiveVaultStoreResult> StoreSyntheticAsync(BrowserProductiveVaultStoreRequest request, string syntheticSecretValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var access = EvaluateAccess(request, BrowserVaultConsentType.SecretStorageConsent, BrowserVaultOperationKind.Storage, requireCoreBoundary: false);
        if (access.Decision != BrowserProductiveVaultDecisionKind.Allowed)
            return Task.FromResult(StoreResult(request, access.Decision, access.Reason, null));
        if (!IsSyntheticValue(syntheticSecretValue))
            return Task.FromResult(StoreResult(request, BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI provider accepts only synthetic test secrets", null));
        if (HealthCheck().ProviderAvailable == false)
            return Task.FromResult(StoreResult(request, BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI provider unavailable", null));

        if (!OperatingSystem.IsWindows())
            return Task.FromResult(StoreResult(request, BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI provider requires Windows", null));

        var protectedBytes = ProtectCurrentUser(Encoding.UTF8.GetBytes(syntheticSecretValue));
        var stored = request.Reference with { LifecycleState = BrowserProductiveVaultSecretLifecycleState.Active };
        _items[stored.ReferenceId] = new StoredSecret(stored, protectedBytes);
        return Task.FromResult(StoreResult(request, BrowserProductiveVaultDecisionKind.Allowed, "synthetic secret stored using DPAPI CurrentUser", stored));
    }

    public Task<BrowserProductiveVaultRetrieveResult> RetrieveAsync(BrowserProductiveVaultRetrieveRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var access = EvaluateAccess(request, BrowserVaultConsentType.SecretRetrievalConsent, BrowserVaultOperationKind.Retrieval, requireCoreBoundary: true);
        if (access.Decision != BrowserProductiveVaultDecisionKind.Allowed)
            return Task.FromResult(RetrieveResult(request, access.Decision, access.Reason, null));
        if (!_items.TryGetValue(request.Reference.ReferenceId, out var stored) || !ReferenceMatches(request.Reference, stored.Reference))
            return Task.FromResult(RetrieveResult(request, BrowserProductiveVaultDecisionKind.Denied, "DPAPI secret reference unknown", null));
        if (stored.Reference.IsRevoked)
            return Task.FromResult(RetrieveResult(request, BrowserProductiveVaultDecisionKind.Denied, "DPAPI secret reference revoked", stored.Reference));

        var accessed = stored.Reference with { LastAccessedAtUtc = request.AccessContext.NowUtc };
        _items[accessed.ReferenceId] = stored with { Reference = accessed };
        return Task.FromResult(RetrieveResult(request, BrowserProductiveVaultDecisionKind.Allowed, "synthetic secret available to core boundary", accessed));
    }

    internal Task<BrowserProductiveVaultCoreSecretHandle?> RetrieveForCoreBoundaryAsync(BrowserProductiveVaultRetrieveRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = RetrieveAsync(request, cancellationToken).GetAwaiter().GetResult();
        if (!result.AllowsCoreBoundaryUse || !_items.TryGetValue(request.Reference.ReferenceId, out var stored))
            return Task.FromResult<BrowserProductiveVaultCoreSecretHandle?>(null);

        if (!OperatingSystem.IsWindows())
            return Task.FromResult<BrowserProductiveVaultCoreSecretHandle?>(null);

        var unprotected = UnprotectCurrentUser(stored.ProtectedValue);
        return Task.FromResult<BrowserProductiveVaultCoreSecretHandle?>(new BrowserProductiveVaultCoreSecretHandle(stored.Reference, Encoding.UTF8.GetString(unprotected), result.AuditEvent));
    }

    public Task<BrowserProductiveVaultDeleteResult> DeleteAsync(BrowserProductiveVaultDeleteRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var retrieveRequest = new BrowserProductiveVaultRetrieveRequest(request.RequestId, request.RunId, request.ActionId, request.CorrelationId, request.ProfileId, request.SessionId, request.Reference, request.AccessContext, request.Purpose);
        var access = EvaluateAccess(retrieveRequest, BrowserVaultConsentType.SecretDeletionConsent, BrowserVaultOperationKind.Deletion, requireCoreBoundary: true);
        if (access.Decision != BrowserProductiveVaultDecisionKind.Allowed)
            return Task.FromResult(DeleteResult(request, access.Decision, access.Reason, null));
        if (!_items.TryGetValue(request.Reference.ReferenceId, out var stored))
            return Task.FromResult(DeleteResult(request, BrowserProductiveVaultDecisionKind.Denied, "DPAPI secret reference unknown", null));

        var deleted = stored.Reference with { LifecycleState = BrowserProductiveVaultSecretLifecycleState.Deleted };
        _items[deleted.ReferenceId] = stored with { Reference = deleted, ProtectedValue = [] };
        return Task.FromResult(DeleteResult(request, BrowserProductiveVaultDecisionKind.Allowed, "synthetic secret deleted", deleted));
    }

    private AccessDecision EvaluateAccess(BrowserProductiveVaultStoreRequest request, BrowserVaultConsentType consentType, BrowserVaultOperationKind operation, bool requireCoreBoundary)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
            return new(BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI vault request failed validation");
        return EvaluateAccess(request.AccessContext, request.Reference, consentType, operation, requireCoreBoundary);
    }

    private AccessDecision EvaluateAccess(BrowserProductiveVaultRetrieveRequest request, BrowserVaultConsentType consentType, BrowserVaultOperationKind operation, bool requireCoreBoundary)
    {
        var validation = request.Validate();
        if (!validation.IsValid)
            return new(BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI vault request failed validation");
        return EvaluateAccess(request.AccessContext, request.Reference, consentType, operation, requireCoreBoundary);
    }

    private AccessDecision EvaluateAccess(BrowserProductiveVaultAccessContext context, BrowserProductiveVaultSecretReference reference, BrowserVaultConsentType consentType, BrowserVaultOperationKind operation, bool requireCoreBoundary)
    {
        if (reference.ProviderKind != Kind)
            return new(BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI vault provider mismatch");
        if (!context.PolicyAllowed)
            return new(BrowserProductiveVaultDecisionKind.Denied, "DPAPI vault policy denied operation");
        if (context.GateReport?.Passed != true)
            return new(BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI vault requires passing phase gate");
        if (context.LicenseDecision?.Allowed != true || context.LicenseDecision.Feature != NexaFeatureFlag.ProductiveVault)
            return new(BrowserProductiveVaultDecisionKind.Denied, "DPAPI vault requires ProductiveVault license decision");
        if (context.Consent is null || !context.Consent.Allows(context.NowUtc, consentType, ToSecretReference(reference)))
            return new(BrowserProductiveVaultDecisionKind.RequiresConsent, "DPAPI vault requires scoped consent");
        if (requireCoreBoundary && !context.CoreBoundary)
            return new(BrowserProductiveVaultDecisionKind.FailClosed, "DPAPI vault retrieval requires core-only boundary");
        if (operation is BrowserVaultOperationKind.Export)
            return new(BrowserProductiveVaultDecisionKind.Denied, "DPAPI vault export is prohibited");
        return new(BrowserProductiveVaultDecisionKind.Allowed, "DPAPI vault access allowed");
    }

    private BrowserProductiveVaultStoreResult StoreResult(BrowserProductiveVaultStoreRequest request, BrowserProductiveVaultDecisionKind decision, string reason, BrowserProductiveVaultSecretReference? reference)
    {
        var audit = Audit(request.RequestId, request.RunId, request.ActionId, request.CorrelationId, BrowserVaultOperationKind.Storage, decision, request.Reference.ReferenceId, reason);
        return new BrowserProductiveVaultStoreResult(decision, reference, Kind, audit, [audit.EventId], BrowserCredentialRedactor.Redact(reason), Redacted: true);
    }

    private BrowserProductiveVaultRetrieveResult RetrieveResult(BrowserProductiveVaultRetrieveRequest request, BrowserProductiveVaultDecisionKind decision, string reason, BrowserProductiveVaultSecretReference? reference)
    {
        var audit = Audit(request.RequestId, request.RunId, request.ActionId, request.CorrelationId, BrowserVaultOperationKind.Retrieval, decision, request.Reference.ReferenceId, reason);
        return new BrowserProductiveVaultRetrieveResult(decision, reference, Kind, audit, [audit.EventId], BrowserCredentialRedactor.Redact(reason), Redacted: true);
    }

    private BrowserProductiveVaultDeleteResult DeleteResult(BrowserProductiveVaultDeleteRequest request, BrowserProductiveVaultDecisionKind decision, string reason, BrowserProductiveVaultSecretReference? reference)
    {
        var audit = Audit(request.RequestId, request.RunId, request.ActionId, request.CorrelationId, BrowserVaultOperationKind.Deletion, decision, request.Reference.ReferenceId, reason);
        return new BrowserProductiveVaultDeleteResult(decision, reference, Kind, audit, [audit.EventId], BrowserCredentialRedactor.Redact(reason), Redacted: true);
    }

    private BrowserProductiveVaultAuditEvent Audit(string requestId, string runId, string actionId, string correlationId, BrowserVaultOperationKind operation, BrowserProductiveVaultDecisionKind decision, string referenceId, string reason)
    {
        var audit = new BrowserProductiveVaultAuditEvent(
            $"productive-vault-audit-{Guid.NewGuid():N}",
            requestId,
            runId,
            actionId,
            correlationId,
            operation,
            decision,
            Kind,
            BrowserCredentialRedactor.Redact(referenceId),
            DateTimeOffset.UtcNow,
            BrowserCredentialRedactor.Redact(reason),
            Redacted: true);
        _audit.Add(audit);
        return audit;
    }

    private static BrowserSecretReference ToSecretReference(BrowserProductiveVaultSecretReference reference) =>
        new(reference.ReferenceId, reference.Kind, reference.Scope, reference.Owner, reference.Portal, DateTimeOffset.UtcNow, reference.RedactedLabel);

    private static bool IsSyntheticValue(string value) =>
        value is "synthetic-os-backed-password" or "synthetic-api-token-not-real" or "synthetic-login-secret" ||
        value.StartsWith("synthetic-", StringComparison.Ordinal);

    private static bool ReferenceMatches(BrowserProductiveVaultSecretReference request, BrowserProductiveVaultSecretReference stored) =>
        request.ReferenceId == stored.ReferenceId &&
        request.Kind == stored.Kind &&
        request.Scope == stored.Scope &&
        request.ProviderKind == stored.ProviderKind &&
        request.Owner == stored.Owner &&
        request.Portal == stored.Portal;

    [SupportedOSPlatform("windows")]
    private static byte[] ProtectCurrentUser(byte[] value) =>
        ProtectedData.Protect(value, null, DataProtectionScope.CurrentUser);

    [SupportedOSPlatform("windows")]
    private static byte[] UnprotectCurrentUser(byte[] value) =>
        ProtectedData.Unprotect(value, null, DataProtectionScope.CurrentUser);

    private sealed record StoredSecret(BrowserProductiveVaultSecretReference Reference, byte[] ProtectedValue);
    private sealed record AccessDecision(BrowserProductiveVaultDecisionKind Decision, string Reason);
}

internal sealed class BrowserProductiveVaultCoreSecretHandle : IDisposable
{
    private string? _value;

    internal BrowserProductiveVaultCoreSecretHandle(BrowserProductiveVaultSecretReference reference, string value, BrowserProductiveVaultAuditEvent auditEvent)
    {
        Reference = reference;
        _value = value;
        AuditEvent = auditEvent;
    }

    public BrowserProductiveVaultSecretReference Reference { get; }
    public BrowserProductiveVaultAuditEvent AuditEvent { get; }

    internal string RequireValue() => _value ?? throw new ObjectDisposedException(nameof(BrowserProductiveVaultCoreSecretHandle));

    public void Dispose() => _value = null;
}
