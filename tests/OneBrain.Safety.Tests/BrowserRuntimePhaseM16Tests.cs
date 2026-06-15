using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRuntimePhaseM16Tests
{
    [TestMethod]
    public void BrowserDownloadAllowedGeneratesRedactedEvidence()
    {
        using var temp = TempDir();
        var manager = new BrowserDownloadManager();
        var result = manager.CompleteFixtureDownload(DownloadRequest("sample-data.csv"), DownloadPolicy(temp.Path), Fixture("downloads", "sample-data.csv"));

        Assert.IsTrue(result.IsSuccess, result.Reason);
        Assert.IsNotNull(result.Artifact);
        Assert.AreEqual(".csv", result.Artifact.Extension);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Artifact.Sha256));
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(result.AuditEvent.Reason));
        Assert.IsTrue(result.AuditEvent.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserDownloadBlockedByPolicyDoesNotExecute()
    {
        using var temp = TempDir();
        var manager = new BrowserDownloadManager();
        var policy = DownloadPolicy(temp.Path) with { AllowedExtensions = new HashSet<string> { ".pdf" } };

        var result = manager.CompleteFixtureDownload(DownloadRequest("sample-data.csv"), policy, Fixture("downloads", "sample-data.csv"));

        Assert.AreEqual(BrowserTransferDecisionKind.Blocked, result.Status);
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Artifact);
        Assert.AreEqual(BrowserAuditLedgerEventKind.DownloadBlocked, result.AuditEvent.Kind);
    }

    [TestMethod]
    public void BrowserUploadAllowedRequiresPolicyAndApproval()
    {
        var manager = new BrowserUploadManager();
        var uploadPath = Fixture("upload", "sample-upload.txt");
        var result = manager.PrepareFixtureUpload(UploadRequest(uploadPath, hasApproval: true), UploadPolicy(Path.GetDirectoryName(uploadPath)!));

        Assert.IsTrue(result.IsSuccess, result.Reason);
        Assert.IsNotNull(result.Artifact);
        Assert.AreEqual(".txt", result.Artifact.Extension);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Artifact.Sha256));
        Assert.IsTrue(result.AuditEvent.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserUploadBlockedDoesNotExposeSensitivePath()
    {
        var manager = new BrowserUploadManager();
        var uploadPath = Path.Combine(Path.GetTempPath(), "password=raw.txt");
        var result = manager.PrepareFixtureUpload(UploadRequest(uploadPath, hasApproval: false), UploadPolicy(FixtureRoot()));

        Assert.AreEqual(BrowserTransferDecisionKind.Blocked, result.Status);
        Assert.IsFalse(result.IsSuccess);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(result.Reason));
        Assert.IsTrue(result.AuditEvent.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserNetworkCaptureMetadataOnlyRedactsHeadersAndTokens()
    {
        var capture = new BrowserNetworkCapture();
        var raw = new BrowserNetworkCaptureEvent(
            RequestId: "request-1",
            CorrelationId: "corr-1",
            Method: "GET",
            RedactedUrl: "https://fixture.local/api/data?access_token=synthetic&x=1",
            StatusCode: 200,
            ResourceType: "fetch",
            Duration: TimeSpan.FromMilliseconds(12),
            ResponseHeaders:
            [
                new BrowserNetworkHeaderMetadata("authorization", true, true, "bearer synthetic-token", BrowserNetworkHeaderRedactionReason.None),
                new BrowserNetworkHeaderMetadata("content-type", true, true, "application/json", BrowserNetworkHeaderRedactionReason.None),
                new BrowserNetworkHeaderMetadata("x-debug", true, true, SyntheticJwt(), BrowserNetworkHeaderRedactionReason.None)
            ],
            ApiCandidate: true,
            RequestBodyCaptured: true,
            ResponseBodyCaptured: true,
            Redacted: false);

        var summary = capture.Capture(NetworkPolicy(), [raw]);
        var serialized = System.Text.Json.JsonSerializer.Serialize(summary);

        Assert.IsTrue(summary.IsSafe);
        Assert.IsTrue(summary.MetadataOnly);
        Assert.IsFalse(serialized.Contains("synthetic-token", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("header.payload.signature", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("access_token=synthetic", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(summary.Events.Single().ApiCandidate);
    }

    [TestMethod]
    public void BrowserNetworkApiCandidateDoesNotAuthorizeDirectHttpReplay()
    {
        var policy = NetworkPolicy();

        Assert.IsFalse(policy.AllowDirectHttpReplay);
    }

    [TestMethod]
    public void BrowserDiagnosticReplayPackageIsDiagnosticOnly()
    {
        var service = new BrowserSessionExportService();
        var package = service.CreatePackage(ReplayManifest());

        Assert.IsTrue(package.Validate().IsValid);
        Assert.IsTrue(package.DiagnosticOnly);
        Assert.IsTrue(package.Manifest.Steps.All(step => step.DiagnosticOnly));
        Assert.IsFalse(package.Manifest.Steps.Any(step => step.WouldExecuteSensitiveAction));
    }

    [TestMethod]
    public void BrowserDiagnosticReplayBlocksSensitiveExecution()
    {
        var manifest = ReplayManifest() with
        {
            Steps =
            [
                new BrowserSessionReplayStep("step-1", "Blocked", "SensitiveSubmit", "Uncertain", [], DiagnosticOnly: false, WouldExecuteSensitiveAction: true)
            ]
        };

        var package = new BrowserSessionExportService().CreatePackage(manifest);

        Assert.IsFalse(package.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserRuntimePhaseCloseFailsForUnsafeCapability()
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path, companionAuthoritative: true);

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "companion non-authoritative");
    }

    [TestMethod]
    public void BrowserRuntimePhaseClosePassesWithLocalFixtures()
    {
        using var temp = TempDir();
        var report = PhaseReport(temp.Path);

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
        Assert.IsTrue(report.AuditEvent.Validate().IsValid);
        Assert.IsTrue(report.AuditLedgerOk);
        Assert.IsTrue(report.DownloadOk);
        Assert.IsTrue(report.UploadOk);
        Assert.IsTrue(report.NetworkMetadataOnlyOk);
        Assert.IsTrue(report.ReplayDiagnosticOnlyOk);
        Assert.IsTrue(report.CompanionNonAuthoritative);
        Assert.IsTrue(report.ServiceWorkerNotBrain);
        Assert.IsTrue(report.NoRealProfile);
        Assert.IsTrue(report.NoRealVault);
        Assert.IsTrue(report.NoLoginReal);
    }

    private static BrowserRuntimePhaseCloseReport PhaseReport(string tempPath, bool companionAuthoritative = false)
    {
        var ledger = new BrowserPersistentAuditLedger(new BrowserAuditLedgerPolicy(tempPath, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)), BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("onebrain-m50-explicit-test-fixture-hmac-key"));
        ledger.Append(BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.ConsentRequested, "run-1", "action-1", "corr-1", "profile-1", "session-1", "consent-1", null, null, "Requested", "phase close consent requested"));

        var download = new BrowserDownloadManager().CompleteFixtureDownload(DownloadRequest("sample-data.csv"), DownloadPolicy(tempPath), Fixture("downloads", "sample-data.csv"));
        var uploadPath = Fixture("upload", "sample-upload.txt");
        var upload = new BrowserUploadManager().PrepareFixtureUpload(UploadRequest(uploadPath, hasApproval: true), UploadPolicy(Path.GetDirectoryName(uploadPath)!));
        var network = new BrowserNetworkCapture().Capture(NetworkPolicy(), [NetworkEvent()]);
        var package = new BrowserSessionExportService().CreatePackage(ReplayManifest(network.Events));

        var observedState = new BrowserRuntimeObservedState(
            CompanionAuthoritative: companionAuthoritative,
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
            Capabilities: []);
        return new BrowserRuntimePhaseCloseGate().Evaluate(new StaticBrowserRuntimeSecurityProbe(observedState), ledger.ExportSafe(), download, upload, network, package);
    }

    private static BrowserDownloadRequest DownloadRequest(string fileName) =>
        new("run-1", "action-1", "corr-1", "session-1", "https://fixture.local/download", fileName, "text/csv", null);

    private static BrowserDownloadPolicy DownloadPolicy(string directory) =>
        new(directory, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, RequireHash: true, RequireEvidence: true, AllowAutoOpen: false);

    private static BrowserUploadRequest UploadRequest(string filePath, bool hasApproval) =>
        new("run-1", "action-upload", "corr-1", "session-1", filePath, "fixture upload target", hasApproval);

    private static BrowserUploadPolicy UploadPolicy(string directory) =>
        new(directory, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".json", ".pdf", ".xlsx" }, 1024 * 1024, RequireApproval: true, AllowExternalTargets: false);

    private static BrowserNetworkCapturePolicy NetworkPolicy() =>
        new(BrowserNetworkCaptureMode.MetadataOnly, CaptureSensitiveHeaderPresenceOnly: true, AllowDirectHttpReplay: false, AllowedMethods: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST" });

    private static BrowserNetworkCaptureEvent NetworkEvent() =>
        new("request-1", "corr-1", "GET", "https://fixture.local/api/items?token=synthetic", 200, "fetch", TimeSpan.FromMilliseconds(3), [new BrowserNetworkHeaderMetadata("content-type", true, true, "application/json", BrowserNetworkHeaderRedactionReason.None)], ApiCandidate: true, RequestBodyCaptured: false, ResponseBodyCaptured: false, Redacted: true);

    private static string SyntheticJwt() =>
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzeW50aGV0aWMifQ.c2lnbmF0dXJlMTIzNDU2";

    private static BrowserSessionReplayManifest ReplayManifest(IReadOnlyList<BrowserNetworkCaptureEvent>? networkEvents = null) =>
        new(
            RunId: "run-1",
            CorrelationId: "corr-1",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Steps:
            [
                new BrowserSessionReplayStep("step-1", "Verified", "Observe", "Verified", ["proof-1"], DiagnosticOnly: true, WouldExecuteSensitiveAction: false)
            ],
            AuditEvents:
            [
                BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.SessionExportCreated, "run-1", "action-1", "corr-1", "profile-1", "session-1", null, null, null, "Completed", "session export created")
            ],
            NetworkEvents: networkEvents ?? [NetworkEvent() with { RedactedUrl = BrowserNetworkCapture.RedactUrl(NetworkEvent().RedactedUrl), Redacted = true }],
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-m16-{Guid.NewGuid():N}");
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
