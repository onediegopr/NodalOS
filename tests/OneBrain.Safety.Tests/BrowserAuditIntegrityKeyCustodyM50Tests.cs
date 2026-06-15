using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserAuditIntegrityKeyCustodyM50Tests
{
    [TestMethod]
    public void BrowserAuditIntegrityFailsClosedWithoutKeyProvider()
    {
        using var temp = new TempDirectory();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new BrowserPersistentAuditLedger(Policy(temp.Path)));
    }

    [TestMethod]
    public void BrowserAuditIntegrityDoesNotUseDevFixtureByDefault()
    {
        using var temp = new TempDirectory();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            new BrowserPersistentAuditLedger(Policy(temp.Path)));
        Assert.IsTrue(ex.Message.Contains("provider is required", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserAuditIntegrityAllowsDevFixtureOnlyWhenExplicit()
    {
        using var temp = new TempDirectory();
        var ledger = new BrowserPersistentAuditLedger(Policy(temp.Path), BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider(DevFixtureKey));
        ledger.Append(AuditEvent("explicit fixture"));

        Assert.AreEqual(BrowserAuditIntegrityKeyProviderKind.DevFixtureExplicit, ledger.HeadSeal.KeyProviderKind);
        Assert.IsTrue(ledger.VerifyIntegrity(ledger.HeadSeal));
    }

    [TestMethod]
    public void BrowserAuditIntegrityBlocksDevFixtureInProductionLocked()
    {
        var health = new BrowserAuditIntegrityKeyCustodyService().EvaluateForProfile(NexaConfigurationProfileKind.ProductionLocked, BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider(DevFixtureKey));

        Assert.IsFalse(health.Healthy);
    }

    [TestMethod]
    public void BrowserAuditIntegrityOsBackedDpapiProviderHealthOk()
    {
        using var temp = new TempDirectory();
        var provider = OsBacked(temp.Path);

        var health = provider.HealthCheck();

        Assert.AreEqual(BrowserAuditIntegrityKeyProviderKind.OsBackedDpapiCurrentUser, health.ProviderKind);
        Assert.IsTrue(health.Healthy, health.Reason);
    }

    [TestMethod]
    public void BrowserAuditIntegrityOsBackedDoesNotExposeRawKey()
    {
        using var temp = new TempDirectory();
        var provider = OsBacked(temp.Path);

        Assert.IsFalse(provider.KeyReference.RawKeyExposed);
        Assert.IsFalse(provider.HealthCheck().RawKeyExposed);
        Assert.IsFalse(provider.ToString()!.Contains("synthetic-os-backed", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserAuditLedgerHeadSealIncludesKeyId()
    {
        using var temp = new TempDirectory();
        var ledger = Ledger(temp.Path, OsBacked(temp.Path));
        ledger.Append(AuditEvent("one"));

        var seal = ledger.HeadSeal;

        Assert.AreEqual("audit-key-m50", seal.KeyId);
        Assert.AreEqual(7, seal.KeyVersion);
        Assert.AreEqual("HMACSHA256", seal.IntegrityAlgorithm);
    }

    [TestMethod]
    public void BrowserAuditLedgerVerificationFailsWithWrongKeyId()
    {
        using var temp = new TempDirectory();
        var ledger = Ledger(temp.Path, OsBacked(temp.Path));
        ledger.Append(AuditEvent("one"));

        Assert.IsFalse(ledger.VerifyIntegrity(ledger.HeadSeal with { KeyId = "wrong-key-id" }));
    }

    [TestMethod]
    public void BrowserAuditLedgerVerificationFailsWhenKeyUnavailable()
    {
        using var temp = new TempDirectory();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new BrowserPersistentAuditLedger(Policy(temp.Path), new UnavailableIntegrityProvider()));
    }

    [TestMethod]
    public void BrowserAuditLedgerDetectsTamperWithOsBackedKey()
    {
        using var temp = new TempDirectory();
        var provider = OsBacked(temp.Path);
        var ledger = Ledger(temp.Path, provider);
        var original = ledger.Append(AuditEvent("one"));
        var tampered = original with { Reason = "tampered" };

        Assert.IsFalse(provider.VerifyEventIntegrity(tampered));
    }

    [TestMethod]
    public void BrowserAuditLedgerDetectsTruncationWithOsBackedKey()
    {
        using var temp = new TempDirectory();
        var provider = OsBacked(temp.Path);
        var ledger = Ledger(temp.Path, provider);
        ledger.Append(AuditEvent("one"));
        ledger.Append(AuditEvent("two"));
        var seal = ledger.HeadSeal;

        Assert.IsFalse(provider.VerifyHeadSeal(seal, ledger.Events.Take(1).ToArray()));
    }

    [TestMethod]
    public void BrowserAuditIntegrityRotationIsModeledWithoutExposingKey()
    {
        var policy = BrowserAuditLedgerHmacIntegrityProvider.PlanRotation("audit-key-old", "audit-key-new");

        Assert.AreEqual(BrowserAuditIntegrityKeyStatus.RotationPlanned, policy.Status);
        Assert.IsFalse(policy.RawKeyExposed);
    }

    [TestMethod]
    public void ProductionLockedBlocksDevFixtureAuditKey()
    {
        var health = new BrowserAuditIntegrityKeyCustodyService().EvaluateForProfile(NexaConfigurationProfileKind.ProductionLocked, BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider(DevFixtureKey));

        Assert.IsFalse(health.Healthy);
    }

    [TestMethod]
    public void EnterpriseControlledRequiresOsBackedOrExternalAuditKey()
    {
        var health = new BrowserAuditIntegrityKeyCustodyService().EvaluateForProfile(NexaConfigurationProfileKind.EnterpriseControlled, BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider(DevFixtureKey));

        Assert.IsFalse(health.Healthy);
    }

    [TestMethod]
    public void DevelopmentAllowsExplicitDevFixtureAuditKey()
    {
        var health = new BrowserAuditIntegrityKeyCustodyService().EvaluateForProfile(NexaConfigurationProfileKind.Development, BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider(DevFixtureKey));

        Assert.IsTrue(health.Healthy);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWithoutAuditKeyProvider()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(NexaLocalProductShellM48Tests.SafeState() with { AuditIntegrityKeyProviderConfigured = false });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "audit integrity key custody safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithOsBackedAuditKeyProvider()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(NexaLocalProductShellM48Tests.SafeState() with
        {
            AuditIntegrityKeyProviderConfigured = true,
            AuditIntegrityKeyProviderOsBacked = true,
            AuditIntegrityDefaultFailClosed = true,
            AuditIntegrityDevFixtureExplicitOnly = true,
            AuditIntegrityKeyHealthOk = true,
            AuditLedgerHeadSealIncludesKeyId = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
    }

    private static BrowserAuditLedgerHmacIntegrityProvider OsBacked(string path) =>
        BrowserAuditLedgerHmacIntegrityProvider.CreateOsBackedDpapiCurrentUserProvider(System.IO.Path.Combine(path, "keys"), "audit-key-m50", 7);

    private const string DevFixtureKey = "onebrain-m50-explicit-test-fixture-hmac-key";

    private static BrowserPersistentAuditLedger Ledger(string path, IBrowserAuditLedgerIntegrityProvider provider) =>
        new(Policy(path), provider);

    private static BrowserAuditLedgerPolicy Policy(string path) =>
        new(path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true));

    private static BrowserAuditLedgerEvent AuditEvent(string reason) =>
        BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.PolicyBlocked, "run-m50", "action-m50", "corr-m50", "profile-m50", "session-m50", null, null, null, "Blocked", reason);

    private sealed class UnavailableIntegrityProvider : IBrowserAuditLedgerIntegrityProvider
    {
        public BrowserAuditIntegrityKeyReference KeyReference { get; } =
            new(BrowserAuditIntegrityKeyProviderKind.Disabled, "missing", 0, "HMACSHA256", RawKeyExposed: false);

        public BrowserAuditIntegrityKeyHealthCheck HealthCheck() =>
            new(BrowserAuditIntegrityKeyProviderKind.Disabled, "missing", 0, BrowserAuditIntegrityKeyStatus.Unavailable, Healthy: false, RawKeyExposed: false, "unavailable");

        public BrowserAuditLedgerIntegrityProof ComputeEventIntegrity(BrowserAuditLedgerEvent ledgerEvent, long sequenceNumber, string previousHash) =>
            throw new InvalidOperationException("unavailable");

        public bool VerifyEventIntegrity(BrowserAuditLedgerEvent ledgerEvent) => false;

        public BrowserAuditLedgerHeadSeal ComputeHeadSeal(string ledgerId, IReadOnlyList<BrowserAuditLedgerEvent> events, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc) =>
            throw new InvalidOperationException("unavailable");

        public bool VerifyHeadSeal(BrowserAuditLedgerHeadSeal seal, IReadOnlyList<BrowserAuditLedgerEvent> events) => false;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-m50-{Guid.NewGuid():N}");
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
