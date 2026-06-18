using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserAuditLedgerEventKind
{
    OcrEvidenceAuxiliaryRecorded,
    OcrEvidenceDiagnosticRejectedRecorded,
    OcrEvidenceDiagnosticUncertainRecorded,
    OcrEvidencePolicyViolationRejected,
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
    RedactionApplied,
    DownloadRequested,
    DownloadBlocked,
    DownloadCompleted,
    UploadRequested,
    UploadBlocked,
    UploadPrepared,
    NetworkCaptureRecorded,
    SessionExportCreated,
    DiagnosticReplayCreated,
    PhaseCloseGateEvaluated
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

public enum BrowserAuditIntegrityKeyProviderKind
{
    Disabled,
    DevFixtureExplicit,
    OsBackedDpapiCurrentUser,
    ExternalFuture
}

public enum BrowserAuditIntegrityKeyStatus
{
    Unavailable,
    Available,
    RotationRequested,
    RotationPlanned,
    RotationBlocked,
    RotationCompleted
}

public sealed record BrowserAuditIntegrityKeyReference(
    BrowserAuditIntegrityKeyProviderKind ProviderKind,
    string KeyId,
    int KeyVersion,
    string Algorithm,
    bool RawKeyExposed);

public sealed record BrowserAuditIntegrityKeyHealthCheck(
    BrowserAuditIntegrityKeyProviderKind ProviderKind,
    string KeyId,
    int KeyVersion,
    BrowserAuditIntegrityKeyStatus Status,
    bool Healthy,
    bool RawKeyExposed,
    string Reason);

public sealed record BrowserAuditIntegrityKeyRotationPolicy(
    bool RotationRequested,
    string? PreviousKeyId,
    string? NewKeyId,
    BrowserAuditIntegrityKeyStatus Status,
    string AuditReason,
    bool RawKeyExposed);

public sealed record BrowserAuditLedgerHeadSeal(
    string LedgerId,
    BrowserAuditIntegrityKeyProviderKind KeyProviderKind,
    string KeyId,
    int KeyVersion,
    string IntegrityAlgorithm,
    int EventCount,
    long LastSequence,
    string LastEventHash,
    string HeadHmac,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public interface IBrowserAuditLedgerIntegrityProvider
{
    BrowserAuditIntegrityKeyReference KeyReference { get; }
    BrowserAuditIntegrityKeyHealthCheck HealthCheck();
    BrowserAuditLedgerIntegrityProof ComputeEventIntegrity(BrowserAuditLedgerEvent ledgerEvent, long sequenceNumber, string previousHash);
    bool VerifyEventIntegrity(BrowserAuditLedgerEvent ledgerEvent);
    BrowserAuditLedgerHeadSeal ComputeHeadSeal(string ledgerId, IReadOnlyList<BrowserAuditLedgerEvent> events, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc);
    bool VerifyHeadSeal(BrowserAuditLedgerHeadSeal seal, IReadOnlyList<BrowserAuditLedgerEvent> events);
}

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
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ToCanonicalString(ledgerEvent)))).ToLowerInvariant();
    }

    public static string ToCanonicalString(BrowserAuditLedgerEvent ledgerEvent)
    {
        var metadata = string.Join("&", ledgerEvent.Metadata
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{Escape(pair.Key)}={Escape(pair.Value)}"));
        return string.Join("|",
            Escape(ledgerEvent.EventId),
            ledgerEvent.Kind.ToString(),
            ledgerEvent.CreatedAtUtc.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            Escape(ledgerEvent.RunId),
            Escape(ledgerEvent.ActionId),
            Escape(ledgerEvent.CorrelationId),
            Escape(ledgerEvent.ProfileId),
            Escape(ledgerEvent.SessionId),
            Escape(ledgerEvent.ConsentId ?? ""),
            Escape(ledgerEvent.SecretId ?? ""),
            ledgerEvent.ProviderKind?.ToString() ?? "",
            Escape(ledgerEvent.Decision),
            Escape(ledgerEvent.Reason),
            metadata,
            ledgerEvent.Redacted ? "true" : "false",
            ledgerEvent.Integrity.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Escape(ledgerEvent.Integrity.PreviousHash));
    }

    private static string Escape(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Normalize(NormalizationForm.FormC)));
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
