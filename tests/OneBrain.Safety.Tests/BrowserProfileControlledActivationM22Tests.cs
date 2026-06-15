using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserProfileControlledActivationM22Tests
{
    [TestMethod]
    public async Task BrowserProfileControlledActivationRequiresValidConsent()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));

        try
        {
            Assert.IsTrue(result.IsActivated, result.Reason);
            Assert.IsNotNull(result.Profile);
            Assert.AreEqual(BrowserProfileKind.PersistentControlled, result.Profile.Kind);
            Assert.IsTrue(result.AuditEvent.Validate().IsValid);
        }
        finally
        {
            await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.CleanupRequested);
        }
    }

    [TestMethod]
    public void BrowserProfileControlledActivationFailsWithoutConsent()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, null, SafeGate(temp.Path));

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Blocked, result.State);
        StringAssert.Contains(result.Reason, "consent");
    }

    [TestMethod]
    public void BrowserProfileControlledActivationFailsWithExpiredConsent()
    {
        using var temp = TempDir();
        var service = new BrowserConsentService();
        var request = ConsentRequest(service, ttl: TimeSpan.FromMilliseconds(1));
        var consent = service.Decide(request, BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;

        var result = Activate(temp.Path, consent, SafeGate(temp.Path), now: DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Blocked, result.State);
    }

    [TestMethod]
    public void BrowserProfileControlledActivationFailsWithRevokedConsent()
    {
        using var temp = TempDir();
        var service = new BrowserConsentService();
        var grant = service.Decide(ConsentRequest(service), BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;
        var revoked = service.Revoke(grant, "operator", DateTimeOffset.UtcNow).Grant!;

        var result = Activate(temp.Path, revoked, SafeGate(temp.Path));

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Blocked, result.State);
    }

    [TestMethod]
    public void BrowserProfileControlledActivationFailsWithWrongScope()
    {
        using var temp = TempDir();
        var service = new BrowserConsentService();
        var request = service.CreateRequest(BrowserConsentCapability.ProfileControlledActivation, BrowserConsentScope.Session, "run-profile", "action-profile", "corr-profile", "core", "activate controlled profile", TimeSpan.FromMinutes(5));
        var consent = service.Decide(request, BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;

        var result = Activate(temp.Path, consent, SafeGate(temp.Path));

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Blocked, result.State);
    }

    [TestMethod]
    public void BrowserProfileControlledActivationFailsWhenGateFails()
    {
        using var temp = TempDir();
        var gate = Gate(temp.Path, SafeState() with { CompanionAuthoritative = true });

        var result = Activate(temp.Path, Consent(), gate);

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Blocked, result.State);
        StringAssert.Contains(result.Reason, "gate");
    }

    [TestMethod]
    public async Task BrowserProfileControlledActivationAuditsWithoutCookies()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));

        try
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(result.AuditEvent);
            Assert.IsTrue(result.AuditEvent.Validate().IsValid);
            Assert.IsFalse(serialized.Contains("cookie", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(serialized.Contains("set-cookie", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(serialized.Contains("authorization", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.CleanupRequested);
        }
    }

    [TestMethod]
    public async Task BrowserProfileControlledDoesNotExposeCookiesToCompanion()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));

        try
        {
            Assert.IsFalse(result.CookiesExposed);
            Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(result).Contains("sessionid=", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.CleanupRequested);
        }
    }

    [TestMethod]
    public async Task BrowserProfileControlledDoesNotPersistCookiesInAudit()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));

        try
        {
            var text = $"{result.AuditEvent.Reason} {string.Join(" ", result.AuditEvent.Metadata.Select(p => $"{p.Key}:{p.Value}"))}";
            Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(text));
            Assert.IsFalse(text.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.CleanupRequested);
        }
    }

    [TestMethod]
    public async Task BrowserProfileControlledDoesNotExposeSessionStorageValues()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));

        try
        {
            Assert.IsFalse(result.SessionStorageExposed);
            Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(result).Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.CleanupRequested);
        }
    }

    [TestMethod]
    public void BrowserProfileControlledDoesNotCaptureSensitiveHeaders()
    {
        using var temp = TempDir();
        var gate = SafeGate(temp.Path);

        Assert.IsNotNull(gate.ObservedState);
        Assert.IsFalse(gate.ObservedState.SensitiveHeaderValueCaptureSupported);
        Assert.IsFalse(gate.ObservedState.RequestBodyCaptureSupported);
        Assert.IsFalse(gate.ObservedState.ResponseBodyCaptureSupported);
    }

    [TestMethod]
    public async Task BrowserProfileControlledLifecycleIsAudited()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));

        try
        {
            Assert.AreEqual(BrowserControlledProfileLifecycleState.Activated, result.State);
            Assert.AreEqual("Activated", result.AuditEvent.Decision);
        }
        finally
        {
            await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.CleanupRequested);
        }
    }

    [TestMethod]
    public async Task BrowserProfileControlledCleanupRemovesTempArtifacts()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));
        Assert.IsNotNull(result.Profile);
        var dir = result.Profile.UserDataDir;
        Assert.IsTrue(Directory.Exists(dir));

        var cleaned = await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.CleanupRequested);

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Cleaned, cleaned.State);
        Assert.IsFalse(Directory.Exists(dir));
    }

    [TestMethod]
    public async Task BrowserProfileControlledRevocationTriggersCleanup()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));
        var dir = result.Profile!.UserDataDir;

        var cleaned = await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.Revoked);

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Cleaned, cleaned.State);
        Assert.IsFalse(Directory.Exists(dir));
    }

    [TestMethod]
    public async Task BrowserProfileControlledExpirationTriggersCleanup()
    {
        using var temp = TempDir();
        var result = Activate(temp.Path, Consent(), SafeGate(temp.Path));
        var dir = result.Profile!.UserDataDir;

        var cleaned = await Cleanup(temp.Path, result, BrowserControlledProfileLifecycleState.Expired);

        Assert.AreEqual(BrowserControlledProfileLifecycleState.Cleaned, cleaned.State);
        Assert.IsFalse(Directory.Exists(dir));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsControlledProfileWithValidConsent()
    {
        using var temp = TempDir();
        var report = Gate(temp.Path, SafeState() with { ProfileState = BrowserRuntimeProfileState.UserProfileControlledWithConsent, ControlledProfileConsentValid = true, RealProfileActive = true });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
        Assert.AreEqual(BrowserRuntimeProfileState.UserProfileControlledWithConsent, report.ObservedState!.ProfileState);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsForRawUserProfile()
    {
        using var temp = TempDir();
        var report = Gate(temp.Path, SafeState() with { ProfileState = BrowserRuntimeProfileState.RawUserProfileActive, RealProfileActive = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "no raw user profile");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsForControlledProfileWithoutConsent()
    {
        using var temp = TempDir();
        var report = Gate(temp.Path, SafeState() with { ProfileState = BrowserRuntimeProfileState.UserProfileControlledWithConsent, ControlledProfileConsentValid = false, RealProfileActive = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "controlled profile consent valid");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateReportsControlledProfileState()
    {
        using var temp = TempDir();
        var report = Gate(temp.Path, SafeState() with { ProfileState = BrowserRuntimeProfileState.UserProfileControlledWithConsent, ControlledProfileConsentValid = true, RealProfileActive = true });

        Assert.AreEqual(BrowserRuntimeProfileState.UserProfileControlledWithConsent, report.ObservedState!.ProfileState);
        Assert.IsTrue(report.ObservedState.ControlledProfileConsentValid);
    }

    private static BrowserControlledProfileActivationResult Activate(string root, BrowserConsentGrant? consent, BrowserRuntimePhaseCloseReport gate, DateTimeOffset? now = null) =>
        new BrowserControlledProfileActivationService(new BrowserProfileManager(root)).Activate(
            new BrowserControlledProfileActivationRequest("run-profile", "action-profile", "corr-profile", "tenant:acme/person:operator", BrowserControlledProfileMode.PersistentControlled, consent, gate, DateTimeOffset.UtcNow),
            new BrowserControlledProfileActivationPolicy(AllowPersistentControlled: true, AllowRawUserProfile: false, root, BrowserStorageScope.Tenant),
            now ?? DateTimeOffset.UtcNow);

    private static Task<BrowserControlledProfileActivationResult> Cleanup(string root, BrowserControlledProfileActivationResult result, BrowserControlledProfileLifecycleState state) =>
        new BrowserControlledProfileActivationService(new BrowserProfileManager(root)).CleanupAsync(result, state);

    private static BrowserConsentGrant Consent()
    {
        var service = new BrowserConsentService();
        var request = ConsentRequest(service);
        return service.Decide(request, BrowserConsentStatus.Granted, "core", "proof-consent", DateTimeOffset.UtcNow).Grant!;
    }

    private static BrowserConsentRequest ConsentRequest(BrowserConsentService service, TimeSpan? ttl = null) =>
        service.CreateRequest(BrowserConsentCapability.ProfileControlledActivation, BrowserConsentScope.Profile, "run-profile", "action-profile", "corr-profile", "core", "activate controlled profile", ttl ?? TimeSpan.FromMinutes(5));

    private static BrowserRuntimePhaseCloseReport SafeGate(string tempPath) => Gate(tempPath, SafeState());

    private static BrowserRuntimePhaseCloseReport Gate(string tempPath, BrowserRuntimeObservedState state)
    {
        var ledger = new BrowserPersistentAuditLedger(new BrowserAuditLedgerPolicy(tempPath, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)), BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("onebrain-m50-explicit-test-fixture-hmac-key"));
        ledger.Append(BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.PhaseCloseGateEvaluated, "run-profile", "action-gate", "corr-profile", "profile-runtime", "session-profile", null, null, null, "Observed", "profile gate observed"));
        var download = new BrowserDownloadResult(BrowserTransferDecisionKind.Completed, new BrowserDownloadArtifact("sample-data.csv", ".csv", "[SANDBOX]/sample-data.csv", "text/csv", 10, new string('a', 64), Quarantined: true, Redacted: true), "ok", ["download-proof"], BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.DownloadCompleted, "run-profile", "action-download", "corr-profile", "profile-runtime", "session-profile", null, null, null, "Completed", "download fixture completed"), Redacted: true);
        var upload = new BrowserUploadResult(BrowserTransferDecisionKind.Completed, new BrowserUploadArtifact("sample-upload.txt", ".txt", "[SANDBOX]/sample-upload.txt", 10, new string('b', 64), Redacted: true), "ok", ["upload-proof"], BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.UploadPrepared, "run-profile", "action-upload", "corr-profile", "profile-runtime", "session-profile", null, null, null, "Completed", "upload fixture prepared"), Redacted: true);
        var network = new BrowserNetworkCapture().Capture(new BrowserNetworkCapturePolicy(BrowserNetworkCaptureMode.MetadataOnly, true, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET" }), [new BrowserNetworkCaptureEvent("request-profile", "corr-profile", "GET", "https://fixture.local/items", 200, "fetch", TimeSpan.FromMilliseconds(1), [], ApiCandidate: false, RequestBodyCaptured: false, ResponseBodyCaptured: false, Redacted: true)]);
        var package = new BrowserSessionExportService().CreatePackage(new BrowserSessionReplayManifest("run-profile", "corr-profile", DateTimeOffset.UtcNow, [new BrowserSessionReplayStep("step-profile", "Verified", "Observe", "Verified", ["proof-profile"], true, false)], [ledger.Events.Single()], network.Events, [], [], "core-governed-browser-runtime", Redacted: true, DiagnosticOnly: true));
        return new BrowserRuntimePhaseCloseGate().Evaluate(new StaticBrowserRuntimeSecurityProbe(state), ledger.ExportSafe(), download, upload, network, package);
    }

    private static BrowserRuntimeObservedState SafeState() =>
        new(false, false, false, false, false, BrowserNetworkCaptureMode.MetadataOnly, false, false, false, false, "fixture-controlled", "fixture-controlled", true, "HMACSHA256", true, true, true, true, [], BrowserRuntimeProfileState.None, ControlledProfileConsentValid: false);

    private static TempDirectory TempDir() => new();

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-m22-{Guid.NewGuid():N}");
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
