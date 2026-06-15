using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserPersistentAuditLedger
{
    private readonly BrowserAuditLedgerPolicy _policy;
    private readonly IBrowserAuditLedgerIntegrityProvider _integrityProvider;
    private readonly List<BrowserAuditLedgerEvent> _events = [];
    private readonly string _ledgerFile;
    private readonly DateTimeOffset _createdAtUtc = DateTimeOffset.UtcNow;
    private readonly string _ledgerId = $"audit-ledger-{Guid.NewGuid():N}";
    private string _lastHash = "genesis";

    public BrowserPersistentAuditLedger(BrowserAuditLedgerPolicy policy, IBrowserAuditLedgerIntegrityProvider? integrityProvider = null)
    {
        var validation = policy.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));

        _policy = policy;
        _integrityProvider = integrityProvider ?? throw new InvalidOperationException("Audit ledger integrity provider is required. Default dev fixture fallback is disabled.");
        var health = _integrityProvider.HealthCheck();
        if (!health.Healthy || health.RawKeyExposed)
            throw new InvalidOperationException($"Audit ledger integrity provider is unavailable: {health.Reason}");
        _ledgerFile = Path.Combine(policy.LedgerDirectory, "browser-audit-ledger.jsonl");
        if (policy.AllowFilePersistence)
            Directory.CreateDirectory(policy.LedgerDirectory);
    }

    public IReadOnlyList<BrowserAuditLedgerEvent> Events => _events;
    public string LedgerFile => _ledgerFile;
    public BrowserAuditLedgerHeadSeal HeadSeal => _integrityProvider.ComputeHeadSeal(_ledgerId, _events, _createdAtUtc, DateTimeOffset.UtcNow);

    public BrowserAuditLedgerEvent Append(BrowserAuditLedgerEvent ledgerEvent)
    {
        var redacted = Redact(ledgerEvent);
        var withIntegrity = redacted with { Integrity = _integrityProvider.ComputeEventIntegrity(redacted, _events.Count + 1, _lastHash) };
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

    public bool VerifyIntegrity(BrowserAuditLedgerHeadSeal seal) =>
        _events.All(_integrityProvider.VerifyEventIntegrity) && _integrityProvider.VerifyHeadSeal(seal, _events);

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

public sealed class BrowserAuditLedgerHmacIntegrityProvider : IBrowserAuditLedgerIntegrityProvider
{
    private readonly byte[] _key;
    private readonly BrowserAuditIntegrityKeyReference _keyReference;

    public BrowserAuditLedgerHmacIntegrityProvider(byte[] key, BrowserAuditIntegrityKeyReference? keyReference = null)
    {
        if (key.Length < 16)
            throw new ArgumentException("HMAC key must be at least 16 bytes.", nameof(key));
        _key = key;
        _keyReference = keyReference ?? new BrowserAuditIntegrityKeyReference(BrowserAuditIntegrityKeyProviderKind.DevFixtureExplicit, "audit-key-dev-fixture", 1, "HMACSHA256", RawKeyExposed: false);
    }

    public BrowserAuditIntegrityKeyReference KeyReference => _keyReference;

    public static BrowserAuditLedgerHmacIntegrityProvider CreateDevFixtureProvider(string explicitFixtureKeyMaterial) =>
        new(
            Encoding.UTF8.GetBytes(explicitFixtureKeyMaterial),
            new BrowserAuditIntegrityKeyReference(BrowserAuditIntegrityKeyProviderKind.DevFixtureExplicit, "audit-key-dev-fixture-explicit", 1, "HMACSHA256", RawKeyExposed: false));

    public static BrowserAuditLedgerHmacIntegrityProvider CreateOsBackedDpapiCurrentUserProvider(string keyDirectory, string keyId = "audit-key-dpapi-current-user", int keyVersion = 1)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DPAPI CurrentUser audit integrity key provider is only available on Windows.");

        return CreateOsBackedDpapiCurrentUserProviderWindows(keyDirectory, keyId, keyVersion);
    }

    [SupportedOSPlatform("windows")]
    private static BrowserAuditLedgerHmacIntegrityProvider CreateOsBackedDpapiCurrentUserProviderWindows(string keyDirectory, string keyId, int keyVersion)
    {
        Directory.CreateDirectory(keyDirectory);
        var keyFile = Path.Combine(keyDirectory, $"{BrowserDownloadManager.SafeFileName(keyId)}.protected");
        byte[] key;
        if (File.Exists(keyFile))
        {
            var protectedKey = File.ReadAllBytes(keyFile);
            key = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
        }
        else
        {
            key = RandomNumberGenerator.GetBytes(32);
            var protectedKey = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(keyFile, protectedKey);
        }

        return new(
            key,
            new BrowserAuditIntegrityKeyReference(BrowserAuditIntegrityKeyProviderKind.OsBackedDpapiCurrentUser, keyId, keyVersion, "HMACSHA256", RawKeyExposed: false));
    }

    public BrowserAuditIntegrityKeyHealthCheck HealthCheck() =>
        new(_keyReference.ProviderKind, _keyReference.KeyId, _keyReference.KeyVersion, BrowserAuditIntegrityKeyStatus.Available, Healthy: _key.Length >= 16 && !_keyReference.RawKeyExposed, _keyReference.RawKeyExposed, "key available");

    public static BrowserAuditIntegrityKeyRotationPolicy PlanRotation(string previousKeyId, string newKeyId, bool requested = true) =>
        new(requested, previousKeyId, newKeyId, requested ? BrowserAuditIntegrityKeyStatus.RotationPlanned : BrowserAuditIntegrityKeyStatus.RotationBlocked, "audit integrity key rotation modeled without raw key", RawKeyExposed: false);

    public BrowserAuditLedgerIntegrityProof ComputeEventIntegrity(BrowserAuditLedgerEvent ledgerEvent, long sequenceNumber, string previousHash)
    {
        var proofWithoutHash = new BrowserAuditLedgerIntegrityProof(sequenceNumber, previousHash, "");
        var material = ledgerEvent with { Integrity = proofWithoutHash };
        return proofWithoutHash with { EventHash = Hmac(BrowserAuditLedgerEvent.ToCanonicalString(material)) };
    }

    public bool VerifyEventIntegrity(BrowserAuditLedgerEvent ledgerEvent)
    {
        var expected = ComputeEventIntegrity(
            ledgerEvent,
            ledgerEvent.Integrity.SequenceNumber,
            ledgerEvent.Integrity.PreviousHash);
        return FixedTimeEquals(expected.EventHash, ledgerEvent.Integrity.EventHash);
    }

    public BrowserAuditLedgerHeadSeal ComputeHeadSeal(string ledgerId, IReadOnlyList<BrowserAuditLedgerEvent> events, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        var last = events.LastOrDefault();
        var count = events.Count;
        var lastSequence = last?.Integrity.SequenceNumber ?? 0;
        var lastHash = last?.Integrity.EventHash ?? "genesis";
        var material = CanonicalHead(ledgerId, count, lastSequence, lastHash, createdAtUtc, updatedAtUtc);
        return new BrowserAuditLedgerHeadSeal(ledgerId, _keyReference.ProviderKind, _keyReference.KeyId, _keyReference.KeyVersion, _keyReference.Algorithm, count, lastSequence, lastHash, Hmac(material), createdAtUtc, updatedAtUtc);
    }

    public bool VerifyHeadSeal(BrowserAuditLedgerHeadSeal seal, IReadOnlyList<BrowserAuditLedgerEvent> events)
    {
        var last = events.LastOrDefault();
        if (seal.KeyProviderKind != _keyReference.ProviderKind)
            return false;
        if (!FixedTimeEquals(seal.KeyId, _keyReference.KeyId))
            return false;
        if (seal.KeyVersion != _keyReference.KeyVersion)
            return false;
        if (!FixedTimeEquals(seal.IntegrityAlgorithm, _keyReference.Algorithm))
            return false;
        if (!HealthCheck().Healthy)
            return false;
        if (seal.EventCount != events.Count)
            return false;
        if (seal.LastSequence != (last?.Integrity.SequenceNumber ?? 0))
            return false;
        if (!FixedTimeEquals(seal.LastEventHash, last?.Integrity.EventHash ?? "genesis"))
            return false;

        var expected = Hmac(CanonicalHead(seal.LedgerId, seal.EventCount, seal.LastSequence, seal.LastEventHash, seal.CreatedAtUtc, seal.UpdatedAtUtc));
        return FixedTimeEquals(expected, seal.HeadHmac);
    }

    private string Hmac(string material)
    {
        using var hmac = new HMACSHA256(_key);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }

    private static string CanonicalHead(string ledgerId, int count, long lastSequence, string lastHash, DateTimeOffset created, DateTimeOffset updated) =>
        string.Join("|",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(ledgerId)),
            count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            lastSequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            lastHash,
            created.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            updated.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture));

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
