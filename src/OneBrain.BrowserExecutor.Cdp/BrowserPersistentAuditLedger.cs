using System.Text.Json;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserPersistentAuditLedger
{
    private readonly BrowserAuditLedgerPolicy _policy;
    private readonly List<BrowserAuditLedgerEvent> _events = [];
    private readonly string _ledgerFile;
    private string _lastHash = "genesis";

    public BrowserPersistentAuditLedger(BrowserAuditLedgerPolicy policy)
    {
        var validation = policy.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));

        _policy = policy;
        _ledgerFile = Path.Combine(policy.LedgerDirectory, "browser-audit-ledger.jsonl");
        if (policy.AllowFilePersistence)
            Directory.CreateDirectory(policy.LedgerDirectory);
    }

    public IReadOnlyList<BrowserAuditLedgerEvent> Events => _events;
    public string LedgerFile => _ledgerFile;

    public BrowserAuditLedgerEvent Append(BrowserAuditLedgerEvent ledgerEvent)
    {
        var redacted = Redact(ledgerEvent);
        var withIntegrity = redacted.WithIntegrity(_events.Count + 1, _lastHash);
        var validation = withIntegrity.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));

        _events.Add(withIntegrity);
        _lastHash = withIntegrity.Integrity.EventHash;
        if (_policy.AllowFilePersistence)
            File.AppendAllText(_ledgerFile, JsonSerializer.Serialize(withIntegrity) + Environment.NewLine);
        ApplyRetention();
        return withIntegrity;
    }

    public BrowserAuditLedgerExport ExportSafe() =>
        new(DateTimeOffset.UtcNow, _events.Count, _events.ToArray(), Redacted: true);

    public static BrowserAuditLedgerEvent FromVaultDecision(BrowserSecretVaultDecision decision, BrowserAuditLedgerEventKind kind) =>
        Create(
            kind,
            decision.AuditEvent.RunId,
            decision.AuditEvent.ActionId,
            decision.AuditEvent.CorrelationId,
            decision.AuditEvent.ProfileId,
            decision.AuditEvent.SessionId,
            decision.AuditEvent.ConsentId,
            decision.AuditEvent.SecretId,
            decision.ProviderKind,
            decision.Decision.ToString(),
            decision.Message,
            new Dictionary<string, string>
            {
                ["operation"] = decision.Operation.ToString(),
                ["provider"] = decision.ProviderKind.ToString()
            });

    public static BrowserAuditLedgerEvent FromVaultConsent(BrowserVaultConsentDecision decision, BrowserAuditLedgerEventKind kind) =>
        Create(
            kind,
            decision.Request.RunId,
            decision.Request.ActionId,
            decision.Request.CorrelationId,
            decision.Request.ProfileId,
            decision.Request.SessionId,
            decision.Request.ConsentId,
            decision.Request.SecretReference?.SecretId,
            null,
            decision.Status.ToString(),
            decision.Message,
            new Dictionary<string, string>
            {
                ["consentType"] = decision.Request.ConsentType.ToString(),
                ["scope"] = decision.Request.Scope.ToString(),
                ["authority"] = decision.AuthorityKind.ToString()
            });

    public static BrowserAuditLedgerEvent Create(
        BrowserAuditLedgerEventKind kind,
        string runId,
        string actionId,
        string correlationId,
        string profileId,
        string sessionId,
        string? consentId,
        string? secretId,
        BrowserProductiveVaultProviderKind? providerKind,
        string decision,
        string reason,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            EventId: $"audit-ledger-{Guid.NewGuid():N}",
            Kind: kind,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RunId: BrowserCredentialRedactor.Redact(runId),
            ActionId: BrowserCredentialRedactor.Redact(actionId),
            CorrelationId: BrowserCredentialRedactor.Redact(correlationId),
            ProfileId: BrowserCredentialRedactor.Redact(profileId),
            SessionId: BrowserCredentialRedactor.Redact(sessionId),
            ConsentId: BrowserCredentialRedactor.Redact(consentId),
            SecretId: BrowserCredentialRedactor.Redact(secretId),
            ProviderKind: providerKind,
            Decision: BrowserCredentialRedactor.Redact(decision),
            Reason: BrowserCredentialRedactor.Redact(reason),
            Metadata: RedactMetadata(metadata ?? new Dictionary<string, string>()),
            Redacted: true,
            Integrity: new BrowserAuditLedgerIntegrityProof(0, "", ""));

    public static BrowserAuditLedgerEvent Redact(BrowserAuditLedgerEvent ledgerEvent) =>
        ledgerEvent with
        {
            RunId = BrowserCredentialRedactor.Redact(ledgerEvent.RunId),
            ActionId = BrowserCredentialRedactor.Redact(ledgerEvent.ActionId),
            CorrelationId = BrowserCredentialRedactor.Redact(ledgerEvent.CorrelationId),
            ProfileId = BrowserCredentialRedactor.Redact(ledgerEvent.ProfileId),
            SessionId = BrowserCredentialRedactor.Redact(ledgerEvent.SessionId),
            ConsentId = BrowserCredentialRedactor.Redact(ledgerEvent.ConsentId),
            SecretId = BrowserCredentialRedactor.Redact(ledgerEvent.SecretId),
            Decision = BrowserCredentialRedactor.Redact(ledgerEvent.Decision),
            Reason = BrowserCredentialRedactor.Redact(ledgerEvent.Reason),
            Metadata = RedactMetadata(ledgerEvent.Metadata),
            Redacted = true
        };

    public static IReadOnlyDictionary<string, string> RedactMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        var redacted = new Dictionary<string, string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var pair in metadata)
        {
            var key = BrowserCredentialRedactor.Redact(pair.Key);
            if (redacted.ContainsKey(key))
                key = $"{key}-{++index}";
            redacted[key] = BrowserCredentialRedactor.Redact(pair.Value);
        }

        return redacted;
    }

    private void ApplyRetention()
    {
        if (_policy.RetentionPolicy.MaxEvents is not { } maxEvents || _events.Count <= maxEvents)
            return;

        var removeCount = _events.Count - maxEvents;
        _events.RemoveRange(0, removeCount);
    }
}
