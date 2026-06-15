using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserVaultMinimalM23Tests
{
    private const string SandboxUserValue = "sandbox-user";
    private const string SandboxPassValue = "synthetic-local-passphrase";

    [TestMethod]
    public async Task BrowserVaultRealMinimalStoresSandboxSecretWithoutLeakingValue()
    {
        var provider = new BrowserVaultMinimalSandboxProvider();
        var reference = Reference(BrowserSecretKind.Password);

        var result = await provider.StoreSandboxAsync(Store(reference), SandboxPassValue);

        Assert.IsTrue(result.Allowed);
        Assert.IsFalse(result.SecretValueReturned);
        Assert.IsFalse(result.ToString()!.Contains(SandboxPassValue, StringComparison.Ordinal));
        Assert.IsTrue(result.AuditEvent.Validate().IsValid);
    }

    [TestMethod]
    public async Task BrowserVaultRealMinimalRetrievesSecretOnlyThroughCoreBoundary()
    {
        var provider = new BrowserVaultMinimalSandboxProvider();
        var reference = Reference(BrowserSecretKind.Password);
        await provider.StoreSandboxAsync(Store(reference), SandboxPassValue);

        var result = await provider.RetrieveAsync(Retrieve(reference, Consent(), GateReport(SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.Allowed, result.Decision);
        Assert.IsTrue(result.AllowsCoreUse);
        Assert.IsFalse(result.SecretValueReturned);
        Assert.IsFalse(result.ToString()!.Contains(SandboxPassValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserVaultRetrieveFailsWithoutConsent()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, null, GateReport(SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultRetrieveFailsWithExpiredConsent()
    {
        var provider = await StoredProvider();
        var consent = Consent(ttl: TimeSpan.FromMilliseconds(-1));
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, consent, GateReport(SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultRetrieveFailsWithRevokedConsent()
    {
        var provider = await StoredProvider();
        var consentService = new BrowserConsentService();
        var request = ConsentRequest(TimeSpan.FromMinutes(5));
        var granted = consentService.Decide(request, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant!;
        var revoked = consentService.Revoke(granted, "core-test", DateTimeOffset.UtcNow).Grant;

        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, revoked, GateReport(SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultRetrieveFailsWithWrongScope()
    {
        var provider = await StoredProvider();
        var consent = Consent(scope: BrowserConsentScope.Session);

        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, consent, GateReport(SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultRetrieveFailsWhenGateFails()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, Consent(), GateReport(SafeState(vaultMinimal: true) with { CompanionAuthoritative = true })));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.FailClosed, result.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultRetrieveFailsForUnknownSecret()
    {
        var provider = new BrowserVaultMinimalSandboxProvider();
        var result = await provider.RetrieveAsync(Retrieve(Reference(BrowserSecretKind.Password), Consent(), GateReport(SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.UnknownSecret, result.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultRetrieveDoesNotExposeValueToCompanion()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, Consent(), GateReport(SafeState(vaultMinimal: true))));

        Assert.IsFalse(result.SecretValueReturned);
        Assert.IsFalse(result.ToString()!.Contains(SandboxPassValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserVaultAuditDoesNotContainSecretValue()
    {
        var provider = await StoredProvider();
        await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, Consent(), GateReport(SafeState(vaultMinimal: true))));

        var auditText = string.Join("\n", provider.Provider.AuditEvents.Select(e => e.ToString()));
        Assert.IsFalse(auditText.Contains(SandboxPassValue, StringComparison.Ordinal));
        Assert.IsFalse(provider.Provider.AuditEvents.Any(e => !e.Validate().IsValid));
    }

    [TestMethod]
    public async Task BrowserVaultExportDoesNotContainSecretValue()
    {
        var provider = await StoredProvider();
        await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, Consent(), GateReport(SafeState(vaultMinimal: true))));
        var export = string.Join("\n", provider.Provider.AuditEvents.Select(e => $"{e.EventId}|{e.Decision}|{e.Reference.ReferenceId}|{e.Reason}"));

        Assert.IsFalse(export.Contains(SandboxPassValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserVaultRevokeBlocksFutureRetrieval()
    {
        var provider = await StoredProvider();
        var revoked = await provider.Provider.RevokeAsync(new BrowserVaultRevocationRequest($"vault-request-{Guid.NewGuid():N}", "run-vault", "action-vault", "corr-vault", "profile-controlled", "session-controlled", provider.Reference, DateTimeOffset.UtcNow));
        var retrieve = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, Consent(), GateReport(SafeState(vaultMinimal: true))));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.Revoked, revoked.Decision);
        Assert.AreEqual(BrowserVaultAccessDecisionKind.Revoked, retrieve.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultRotationIsAuditedWithoutValue()
    {
        var provider = await StoredProvider();
        var rotated = await provider.Provider.RotateAsync(new BrowserVaultRotationRequest($"vault-request-{Guid.NewGuid():N}", "run-vault", "action-vault", "corr-vault", "profile-controlled", "session-controlled", provider.Reference, DateTimeOffset.UtcNow));

        Assert.AreEqual(BrowserVaultAccessDecisionKind.Allowed, rotated.Decision);
        Assert.IsFalse(rotated.ToString()!.Contains(SandboxPassValue, StringComparison.Ordinal));
        Assert.IsTrue(rotated.AuditEvent.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsVaultMinimalSandbox()
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path, SafeState(vaultMinimal: true));

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
        Assert.AreEqual(BrowserRuntimeVaultState.MinimalSandboxActive, report.ObservedState!.VaultState);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsForVaultReturningPublicValues() =>
        AssertGateFails(SafeState(vaultMinimal: true) with { VaultReturnsPublicValues = true }, "vault does not return public values");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsForUnknownVaultProvider() =>
        AssertGateFails(SafeState(vaultMinimal: true) with { VaultState = BrowserRuntimeVaultState.UnknownProvider, VaultProviderKnown = false }, "vault provider known");

    [TestMethod]
    public void BrowserRuntimePhaseGateReportsVaultMode()
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path, SafeState(vaultMinimal: true));

        Assert.AreEqual(BrowserRuntimeVaultState.MinimalSandboxActive, report.ObservedState!.VaultState);
        CollectionAssert.Contains(report.PassedChecks.ToList(), "minimal sandbox vault consent valid");
    }

    private static async Task<(BrowserVaultMinimalSandboxProvider Provider, BrowserVaultSecretReference Reference)> StoredProvider()
    {
        var provider = new BrowserVaultMinimalSandboxProvider();
        var reference = Reference(BrowserSecretKind.Password);
        await provider.StoreSandboxAsync(Store(reference), SandboxPassValue);
        return (provider, reference);
    }

    internal static BrowserVaultSecretReference Reference(BrowserSecretKind kind) =>
        new($"vault-ref-{Guid.NewGuid():N}", kind, BrowserSecretScope.Profile, "sandbox-owner", "local-fixture", BrowserVaultMinimalProviderKind.SandboxLocalEncrypted, DateTimeOffset.UtcNow, "sandbox item");

    internal static BrowserVaultStoreRequest Store(BrowserVaultSecretReference reference) =>
        new($"vault-request-{Guid.NewGuid():N}", "run-vault", "action-vault", "corr-vault", "profile-controlled", "session-controlled", reference, "sandbox item setup", DateTimeOffset.UtcNow);

    internal static BrowserVaultRetrieveRequest Retrieve(BrowserVaultSecretReference reference, BrowserConsentGrant? consent, BrowserRuntimePhaseCloseReport report) =>
        new($"vault-request-{Guid.NewGuid():N}", "run-vault", "action-vault", "corr-vault", "profile-controlled", "session-controlled", reference, consent, report, BrowserVaultAccessPolicy.SandboxRetrieval, "sandbox form fill", DateTimeOffset.UtcNow);

    internal static BrowserConsentGrant? Consent(TimeSpan? ttl = null, BrowserConsentScope scope = BrowserConsentScope.Profile)
    {
        var service = new BrowserConsentService();
        var request = ConsentRequest(ttl ?? TimeSpan.FromMinutes(5), scope);
        return service.Decide(request, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant;
    }

    private static BrowserConsentRequest ConsentRequest(TimeSpan ttl, BrowserConsentScope scope = BrowserConsentScope.Profile) =>
        new BrowserConsentService().CreateRequest(BrowserConsentCapability.SecretRetrieval, scope, "run-vault", "action-vault", "corr-vault", "core-test", "sandbox form fill", ttl);

    internal static BrowserRuntimeObservedState SafeState(bool vaultMinimal = false) =>
        new(
            CompanionAuthoritative: false,
            LegacyRunnerEnabled: false,
            RealProfileActive: false,
            RealVaultActive: vaultMinimal,
            LoginRealActive: false,
            NetworkCaptureMode: BrowserNetworkCaptureMode.MetadataOnly,
            RequestBodyCaptureSupported: false,
            ResponseBodyCaptureSupported: false,
            SensitiveHeaderValueCaptureSupported: false,
            ReplayExecutableEnabled: false,
            DownloadMode: "fixture-controlled",
            UploadMode: "fixture-controlled",
            TargetFrameManagerHealthy: true,
            AuditLedgerIntegrityProviderKind: "HMACSHA256",
            AuditLedgerHeadSealAvailable: true,
            AuditLedgerHeadSealValid: true,
            CdpLiveProofAvailable: true,
            Browser004xLegacyIsolated: true,
            Capabilities:
            [
                new BrowserRuntimeCapabilityState("vault-minimal-sandbox", vaultMinimal, "evidence:vault-minimal-sandbox"),
                new BrowserRuntimeCapabilityState("cdp-live-readonly", true, "evidence:m18-cdp-live")
            ],
            VaultState: vaultMinimal ? BrowserRuntimeVaultState.MinimalSandboxActive : BrowserRuntimeVaultState.DesignOnly,
            MinimalSandboxVaultConsentValid: vaultMinimal,
            VaultReturnsPublicValues: false,
            VaultCompanionExposure: false,
            VaultProviderKnown: true);

    internal static BrowserRuntimePhaseCloseReport GateReport(BrowserRuntimeObservedState state)
    {
        using var temp = TempDir();
        return PhaseReport(temp.Path, state);
    }

    private static void AssertGateFails(BrowserRuntimeObservedState state, string check)
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path, state);
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), check);
    }

    private static BrowserRuntimePhaseCloseReport PhaseReport(string tempPath, BrowserRuntimeObservedState state)
    {
        var ledger = new BrowserPersistentAuditLedger(new BrowserAuditLedgerPolicy(tempPath, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)));
        ledger.Append(BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.PhaseCloseGateEvaluated, "run-phase", "action-phase", "corr-phase", "profile-runtime", "session-phase", null, null, null, "Observed", "phase gate fixture observed"));
        var fixtureRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "browser-executor"));
        var download = new BrowserDownloadManager().CompleteFixtureDownload(new BrowserDownloadRequest("run-phase", "action-download", "corr-phase", "session-phase", "https://fixture.local/download", "sample-data.csv", "text/csv", null), new BrowserDownloadPolicy(tempPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, true, true, false), Path.Combine(fixtureRoot, "downloads", "sample-data.csv"));
        var uploadPath = Path.Combine(fixtureRoot, "upload", "sample-upload.txt");
        var upload = new BrowserUploadManager().PrepareFixtureUpload(new BrowserUploadRequest("run-phase", "action-upload", "corr-phase", "session-phase", uploadPath, "fixture upload target", true), new BrowserUploadPolicy(Path.GetDirectoryName(uploadPath)!, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, true, false));
        var network = new BrowserNetworkCapture().Capture(new BrowserNetworkCapturePolicy(BrowserNetworkCaptureMode.MetadataOnly, true, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST" }), [new BrowserNetworkCaptureEvent("request-phase", "corr-phase", "GET", "https://fixture.local/api/items", 200, "fetch", TimeSpan.FromMilliseconds(3), [new BrowserNetworkHeaderMetadata("content-type", true, true, "application/json", BrowserNetworkHeaderRedactionReason.None)], true, false, false, true)]);
        var manifest = new BrowserSessionReplayManifest("run-phase", "corr-phase", DateTimeOffset.UtcNow, [new BrowserSessionReplayStep("step-phase", "Verified", "Observe", "Verified", ["proof-phase"], true, false)], [BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.SessionExportCreated, "run-phase", "action-export", "corr-phase", "profile-runtime", "session-phase", null, null, null, "Completed", "session export created")], network.Events, [], [], "core-governed-browser-runtime", true, true);
        var package = new BrowserSessionExportService().CreatePackage(manifest);
        return new BrowserRuntimePhaseCloseGate().Evaluate(new StaticBrowserRuntimeSecurityProbe(state, ["evidence:m18-cdp-live"], ["audit:m17-hmac-head"]), ledger.ExportSafe(), download, upload, network, package);
    }

    internal static TempDirectory TempDir() => new();

    internal sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-m23-{Guid.NewGuid():N}");
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
