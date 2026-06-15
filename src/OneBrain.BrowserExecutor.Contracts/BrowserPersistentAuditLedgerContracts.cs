using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserAuditLedgerEventKind
{
    VaultStorageRequested,
    VaultRetrievalRequested,
    VaultUseRequested,
    VaultDeletionRequested,
    VaultRotationRequested,
    VaultExportAttempted,
    VaultProviderSelected,
    VaultOperationDenied,
    VaultOperationFailClosed,
    ConsentRequested,
    ConsentUserApprovedIntent,
    ConsentUserDeniedIntent,
    ConsentGrantedByCore,
    ConsentDeniedByCore,
    ConsentExpired,
    ConsentRevoked,
    ProfileRealConsentRequested,
    ProfileRealConsentDenied,
    ProfileRealConsentGranted,
    HumanHandoffCreated,
    HumanHandoffUserCompletedIntent,
    HumanHandoffResumeVerified,
    HumanHandoffResumeRejected,
    PolicyBlocked,
    RedactionApplied
}

public sealed record BrowserAuditLedgerPolicy(
    string LedgerDirectory,
    bool AllowFilePersistence,
    bool RedactBeforePersist,
    BrowserAuditLedgerRetentionPolicy RetentionPolicy)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(LedgerDirectory))
            errors.Add("LedgerDirectory is required.");
        if (!RedactBeforePersist)
            errors.Add("Audit ledger must redact before persisting.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserAuditLedgerRetentionPolicy(
    TimeSpan? MaxAge,
    int? MaxEvents,
    bool DeleteOnCleanup);

public sealed record BrowserAuditLedgerIntegrityProof(
    long SequenceNumber,
    string PreviousHash,
    string EventHash);

public sealed record BrowserAuditLedgerEvent(
    string EventId,
    BrowserAuditLedgerEventKind Kind,
    DateTimeOffset CreatedAtUtc,
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    string? ConsentId,
    string? SecretId,
    BrowserProductiveVaultProviderKind? ProviderKind,
    string Decision,
    string Reason,
    IReadOnlyDictionary<string, string> Metadata,
    bool Redacted,
    BrowserAuditLedgerIntegrityProof Integrity)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RunId, nameof(RunId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActionId, nameof(ActionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CorrelationId, nameof(CorrelationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ProfileId, nameof(ProfileId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ConsentId, nameof(ConsentId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SecretId, nameof(SecretId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Integrity.EventHash, nameof(Integrity.EventHash), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Integrity.PreviousHash, nameof(Integrity.PreviousHash), errors);
        if (!Redacted)
            errors.Add("Audit ledger event must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Decision) ||
            BrowserCredentialRedactor.ContainsSecret(Reason) ||
            Metadata.Any(pair => BrowserCredentialRedactor.ContainsSecret(pair.Key) || BrowserCredentialRedactor.ContainsSecret(pair.Value)))
            errors.Add("Audit ledger event contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    public BrowserAuditLedgerEvent WithIntegrity(long sequenceNumber, string previousHash)
    {
        var proofWithoutHash = new BrowserAuditLedgerIntegrityProof(sequenceNumber, previousHash, "");
        var next = this with { Integrity = proofWithoutHash };
        return next with { Integrity = proofWithoutHash with { EventHash = ComputeHash(next) } };
    }

    public static string ComputeHash(BrowserAuditLedgerEvent ledgerEvent)
    {
        var payload = new
        {
            ledgerEvent.EventId,
            ledgerEvent.Kind,
            ledgerEvent.CreatedAtUtc,
            ledgerEvent.RunId,
            ledgerEvent.ActionId,
            ledgerEvent.CorrelationId,
            ledgerEvent.ProfileId,
            ledgerEvent.SessionId,
            ledgerEvent.ConsentId,
            ledgerEvent.SecretId,
            ledgerEvent.ProviderKind,
            ledgerEvent.Decision,
            ledgerEvent.Reason,
            Metadata = ledgerEvent.Metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray(),
            ledgerEvent.Redacted,
            ledgerEvent.Integrity.SequenceNumber,
            ledgerEvent.Integrity.PreviousHash
        };
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
}

public sealed record BrowserAuditLedgerExport(
    DateTimeOffset ExportedAtUtc,
    int EventCount,
    IReadOnlyList<BrowserAuditLedgerEvent> Events,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (!Redacted)
            errors.Add("Audit ledger export must be redacted.");
        foreach (var ledgerEvent in Events)
            errors.AddRange(ledgerEvent.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}
