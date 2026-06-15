using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRuntimePhaseGateM19Tests
{
    [TestMethod]
    public void BrowserRuntimePhaseGateDerivesStateFromProbe()
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path, SafeState() with { CdpLiveProofAvailable = false });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        Assert.IsNotNull(report.ObservedState);
        Assert.IsFalse(report.ObservedState.CdpLiveProofAvailable);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "CDP live proof available");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateDoesNotTrustCallerFlags()
    {
        using var temp = TempDir();
        var unsafeState = SafeState() with { RealVaultActive = true };

        var report = PhaseReport(temp.Path, unsafeState);

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "no real vault");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenCompanionIsAuthoritative() =>
        AssertFails(SafeState() with { CompanionAuthoritative = true }, "companion non-authoritative");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenLegacyRunnerEnabled() =>
        AssertFails(SafeState() with { LegacyRunnerEnabled = true }, "legacy runner disabled");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenRealProfileActive() =>
        AssertFails(SafeState() with { RealProfileActive = true }, "no real profile");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenRealVaultActive() =>
        AssertFails(SafeState() with { RealVaultActive = true }, "no real vault");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenLoginRealActive() =>
        AssertFails(SafeState() with { LoginRealActive = true }, "no real login");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenBodiesSupported()
    {
        AssertFails(SafeState() with { RequestBodyCaptureSupported = true }, "request bodies unsupported");
        AssertFails(SafeState() with { ResponseBodyCaptureSupported = true }, "response bodies unsupported");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenSensitiveHeaderValuesSupported() =>
        AssertFails(SafeState() with { SensitiveHeaderValueCaptureSupported = true }, "sensitive header values unsupported");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenReplayExecutable() =>
        AssertFails(SafeState() with { ReplayExecutableEnabled = true }, "replay diagnostic-only");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWithoutHmacLedgerIntegrity() =>
        AssertFails(SafeState() with { AuditLedgerIntegrityProviderKind = "SHA256" }, "audit ledger HMAC integrity");

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWithoutHeadSeal()
    {
        AssertFails(SafeState() with { AuditLedgerHeadSealAvailable = false }, "audit ledger head seal available");
        AssertFails(SafeState() with { AuditLedgerHeadSealValid = false }, "audit ledger head seal valid");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWithoutCdpLiveProof() =>
        AssertFails(SafeState() with { CdpLiveProofAvailable = false }, "CDP live proof available");

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithObservedSafeState()
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path, SafeState());

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
        Assert.IsNotNull(report.ObservedState);
        Assert.IsFalse(report.ObservedState.RealVaultActive);
        Assert.AreEqual("Proceed to M21/M22 planning; do not enable real vault/profile/login yet.", report.RecommendedNextAction);
        CollectionAssert.Contains(report.EvidenceRefs!.ToList(), "evidence:m18-cdp-live");
        CollectionAssert.Contains(report.AuditRefs!.ToList(), "audit:m17-hmac-head");
    }

    private static void AssertFails(BrowserRuntimeObservedState state, string failedCheck)
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path, state);

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), failedCheck);
    }

    private static BrowserRuntimePhaseCloseReport PhaseReport(string tempPath, BrowserRuntimeObservedState state)
    {
        var ledger = new BrowserPersistentAuditLedger(new BrowserAuditLedgerPolicy(tempPath, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)));
        ledger.Append(BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.PhaseCloseGateEvaluated, "run-phase", "action-phase", "corr-phase", "profile-runtime", "session-phase", null, null, null, "Observed", "phase gate fixture observed"));
        var download = new BrowserDownloadManager().CompleteFixtureDownload(DownloadRequest("sample-data.csv"), DownloadPolicy(tempPath), Fixture("downloads", "sample-data.csv"));
        var uploadPath = Fixture("upload", "sample-upload.txt");
        var upload = new BrowserUploadManager().PrepareFixtureUpload(UploadRequest(uploadPath, hasApproval: true), UploadPolicy(Path.GetDirectoryName(uploadPath)!));
        var network = new BrowserNetworkCapture().Capture(NetworkPolicy(), [NetworkEvent()]);
        var package = new BrowserSessionExportService().CreatePackage(ReplayManifest(network.Events));
        var probe = new StaticBrowserRuntimeSecurityProbe(state, ["evidence:m18-cdp-live"], ["audit:m17-hmac-head"]);

        return new BrowserRuntimePhaseCloseGate().Evaluate(probe, ledger.ExportSafe(), download, upload, network, package);
    }

    private static BrowserRuntimeObservedState SafeState() =>
        new(
            CompanionAuthoritative: false,
            LegacyRunnerEnabled: false,
            RealProfileActive: false,
            RealVaultActive: false,
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
                new BrowserRuntimeCapabilityState("network", true, "evidence:network-metadata-only"),
                new BrowserRuntimeCapabilityState("audit-ledger", true, "audit:m17-hmac-head"),
                new BrowserRuntimeCapabilityState("cdp-live-readonly", true, "evidence:m18-cdp-live")
            ]);

    private static BrowserDownloadRequest DownloadRequest(string fileName) =>
        new("run-phase", "action-download", "corr-phase", "session-phase", "https://fixture.local/download", fileName, "text/csv", null);

    private static BrowserDownloadPolicy DownloadPolicy(string directory) =>
        new(directory, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, RequireHash: true, RequireEvidence: true, AllowAutoOpen: false);

    private static BrowserUploadRequest UploadRequest(string filePath, bool hasApproval) =>
        new("run-phase", "action-upload", "corr-phase", "session-phase", filePath, "fixture upload target", hasApproval);

    private static BrowserUploadPolicy UploadPolicy(string directory) =>
        new(directory, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, RequireApproval: true, AllowExternalTargets: false);

    private static BrowserNetworkCapturePolicy NetworkPolicy() =>
        new(BrowserNetworkCaptureMode.MetadataOnly, CaptureSensitiveHeaderPresenceOnly: true, AllowDirectHttpReplay: false, AllowedMethods: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST" });

    private static BrowserNetworkCaptureEvent NetworkEvent() =>
        new("request-phase", "corr-phase", "GET", "https://fixture.local/api/items", 200, "fetch", TimeSpan.FromMilliseconds(3), [new BrowserNetworkHeaderMetadata("content-type", true, true, "application/json", BrowserNetworkHeaderRedactionReason.None)], ApiCandidate: true, RequestBodyCaptured: false, ResponseBodyCaptured: false, Redacted: true);

    private static BrowserSessionReplayManifest ReplayManifest(IReadOnlyList<BrowserNetworkCaptureEvent> networkEvents) =>
        new(
            RunId: "run-phase",
            CorrelationId: "corr-phase",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Steps: [new BrowserSessionReplayStep("step-phase", "Verified", "Observe", "Verified", ["proof-phase"], DiagnosticOnly: true, WouldExecuteSensitiveAction: false)],
            AuditEvents: [BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.SessionExportCreated, "run-phase", "action-export", "corr-phase", "profile-runtime", "session-phase", null, null, null, "Completed", "session export created")],
            NetworkEvents: networkEvents,
            Downloads: [],
            Uploads: [],
            RuntimeKind: "core-governed-browser-runtime",
            Redacted: true,
            DiagnosticOnly: true);

    private static string FixtureRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "browser-executor"));

    private static string Fixture(params string[] parts) =>
        Path.Combine([FixtureRoot(), .. parts]);

    private static TempDirectory TempDir() => new();

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-m19-{Guid.NewGuid():N}");
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
