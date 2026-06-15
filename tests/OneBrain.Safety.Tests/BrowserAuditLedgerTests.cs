using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserAuditLedgerTests
{
    [TestMethod]
    public void BrowserAuditLedgerPersistsRedactedEvent()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        var appended = ledger.Append(Event(BrowserAuditLedgerEventKind.ConsentRequested, reason: "request without secret"));

        Assert.AreEqual(1, ledger.Events.Count);
        Assert.IsTrue(File.Exists(ledger.LedgerFile));
        Assert.IsTrue(appended.Validate().IsValid);
        Assert.IsFalse(File.ReadAllText(ledger.LedgerFile).Contains("password=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserAuditLedgerRedactsSecretLikeContentBeforePersisting()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);

        var appended = ledger.Append(Event(BrowserAuditLedgerEventKind.RedactionApplied, secretId: "token=raw-secret", reason: "client_secret=raw-value", metadata: new Dictionary<string, string>
        {
            ["authorization"] = "bearer raw-token",
            ["jwt"] = "header.payload.signature"
        }));
        var export = ledger.ExportSafe();
        var json = File.ReadAllText(ledger.LedgerFile);

        Assert.IsTrue(appended.Validate().IsValid);
        Assert.IsTrue(export.Validate().IsValid);
        Assert.IsFalse(json.Contains("raw-secret", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("raw-token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("header.payload.signature", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserAuditLedgerExportDoesNotContainSecrets()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        ledger.Append(Event(BrowserAuditLedgerEventKind.VaultOperationDenied, reason: BrowserCredentialRedactor.Redact("password=synthetic-value")));

        var export = ledger.ExportSafe();
        var serialized = System.Text.Json.JsonSerializer.Serialize(export);

        Assert.IsTrue(export.Redacted);
        Assert.IsTrue(export.Validate().IsValid);
        Assert.IsFalse(serialized.Contains("synthetic-value", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserAuditLedgerHashChangesWhenPayloadChanges()
    {
        var first = Event(BrowserAuditLedgerEventKind.PolicyBlocked, reason: "one").WithIntegrity(1, "genesis");
        var second = Event(BrowserAuditLedgerEventKind.PolicyBlocked, reason: "two").WithIntegrity(1, "genesis");

        Assert.AreNotEqual(first.Integrity.EventHash, second.Integrity.EventHash);
    }

    [TestMethod]
    public void BrowserAuditLedgerRecordsConsentLifecycle()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        var service = new BrowserVaultConsentService();
        var request = service.CreateRequest(BrowserVaultConsentType.SecretUseConsent, BrowserVaultConsentScope.Secret, Reference(), "run-1", "action-1", "corr-1", "profile-1", "session-1", "core", "synthetic use");
        var companionIntent = BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.ConsentUserApprovedIntent, request.RunId, request.ActionId, request.CorrelationId, request.ProfileId, request.SessionId, request.ConsentId, request.SecretReference?.SecretId, null, "Intent", "companion user approved intent");
        var granted = service.Decide(request, BrowserVaultConsentStatus.Granted, DateTimeOffset.UtcNow, BrowserVaultConsentAuthorityKind.TestHarness, "test", "unit", "proof-1", request.ConsentChallengeId);
        var denied = service.Decide(request, BrowserVaultConsentStatus.Denied, DateTimeOffset.UtcNow);
        var expired = service.Decide(request with { ExpiresAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(-1) }, BrowserVaultConsentStatus.Granted, DateTimeOffset.UtcNow, BrowserVaultConsentAuthorityKind.TestHarness, "test", "unit", "proof-2", request.ConsentChallengeId);
        var revoked = service.Decide(request, BrowserVaultConsentStatus.Revoked, DateTimeOffset.UtcNow, BrowserVaultConsentAuthorityKind.TestHarness, "test", "unit", "proof-3", request.ConsentChallengeId);

        ledger.Append(BrowserPersistentAuditLedger.FromVaultConsent(granted, BrowserAuditLedgerEventKind.ConsentGrantedByCore));
        ledger.Append(BrowserPersistentAuditLedger.FromVaultConsent(denied, BrowserAuditLedgerEventKind.ConsentDeniedByCore));
        ledger.Append(BrowserPersistentAuditLedger.FromVaultConsent(expired, BrowserAuditLedgerEventKind.ConsentExpired));
        ledger.Append(BrowserPersistentAuditLedger.FromVaultConsent(revoked, BrowserAuditLedgerEventKind.ConsentRevoked));
        ledger.Append(companionIntent);

        CollectionAssert.Contains(ledger.Events.Select(e => e.Kind).ToList(), BrowserAuditLedgerEventKind.ConsentUserApprovedIntent);
        CollectionAssert.Contains(ledger.Events.Select(e => e.Kind).ToList(), BrowserAuditLedgerEventKind.ConsentGrantedByCore);
        Assert.IsTrue(ledger.ExportSafe().Validate().IsValid);
    }

    [TestMethod]
    public async Task BrowserAuditLedgerRecordsVaultDeniedAndFailClosed()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        var provider = new InMemoryTestOnlyProductiveVaultProvider();
        var reference = Reference();
        var policy = new BrowserProductiveVaultPolicy(
            DenyByDefault: true,
            RequireConsent: true,
            AllowSyntheticTestOnly: true,
            AllowOsBackedStorage: false,
            AllowSecretExport: false,
            AllowedKinds: new HashSet<BrowserSecretKind> { BrowserSecretKind.ApiKey },
            AllowedScopes: new HashSet<BrowserSecretScope> { BrowserSecretScope.Temporary });
        var config = new BrowserProductiveVaultConfiguration(BrowserProductiveVaultProviderKind.InMemoryTestOnly, true, false, true, "config-1", "provider-1");

        var denied = await provider.StoreAsync(new BrowserSecretStorageRequest("request-1", "run-1", "action-1", "corr-1", "profile-1", "session-1", reference, BrowserProductiveVaultProviderKind.InMemoryTestOnly, null, DateTimeOffset.UtcNow, "synthetic"), policy, config);
        var failClosed = await provider.StoreAsync(new BrowserSecretStorageRequest("token=bad", "run-1", "action-1", "corr-1", "profile-1", "session-1", reference, BrowserProductiveVaultProviderKind.InMemoryTestOnly, null, DateTimeOffset.UtcNow, "synthetic"), policy, config);

        ledger.Append(BrowserPersistentAuditLedger.FromVaultDecision(denied, BrowserAuditLedgerEventKind.VaultOperationDenied));
        ledger.Append(BrowserPersistentAuditLedger.FromVaultDecision(failClosed, BrowserAuditLedgerEventKind.VaultOperationFailClosed));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.RequiresConsent, denied.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, failClosed.Decision);
        CollectionAssert.Contains(ledger.Events.Select(e => e.Kind).ToList(), BrowserAuditLedgerEventKind.VaultOperationDenied);
        CollectionAssert.Contains(ledger.Events.Select(e => e.Kind).ToList(), BrowserAuditLedgerEventKind.VaultOperationFailClosed);
    }

    private static BrowserPersistentAuditLedger Ledger(string path) =>
        new(new BrowserAuditLedgerPolicy(path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)));

    private static BrowserAuditLedgerEvent Event(BrowserAuditLedgerEventKind kind, string secretId = "synthetic-secret-1", string reason = "synthetic reason", IReadOnlyDictionary<string, string>? metadata = null) =>
        BrowserPersistentAuditLedger.Create(kind, "run-1", "action-1", "corr-1", "profile-1", "session-1", "consent-1", secretId, BrowserProductiveVaultProviderKind.InMemoryTestOnly, "Denied", reason, metadata);

    private static BrowserSecretReference Reference() =>
        new("synthetic-secret-1", BrowserSecretKind.ApiKey, BrowserSecretScope.Temporary, "owner", "fixture", DateTimeOffset.UtcNow, "ApiKey Temporary [REDACTED]");

    private static TempDirectory TempDir() => new();

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-audit-ledger-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
