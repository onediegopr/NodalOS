using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PrePaddleOcrReadiness")]
[TestCategory("PixelRedaction")]
[TestCategory("ImagePixelRedaction")]
[TestCategory("RealImageRedaction")]
[TestCategory("LocalWorkerProcessIsolation")]
[TestCategory("LocalWorkerProcessBoundary")]
[TestCategory("LocalWorkerIpcSecurity")]
[TestCategory("LocalOcrWorkerOutOfProcess")]
[TestCategory("LocalOcrWorkerIpcContract")]
[TestCategory("LocalOcrWorkerRuntimeIsolation")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsPrePaddleOcrReadinessM190Tests
{
    private const int W = 32;
    private const int H = 24;

    private static byte[] SolidImage()
    {
        var bytes = new byte[W * H * 4];
        for (var i = 0; i < W * H; i++)
        {
            bytes[(i * 4) + 0] = 255;
            bytes[(i * 4) + 1] = 255;
            bytes[(i * 4) + 2] = 255;
            bytes[(i * 4) + 3] = 255;
        }
        return bytes;
    }

    private static NodalOsPixelRedactionResult ValidPixelRedaction()
    {
        var redactor = new NodalOsPixelImageRedactor();
        var request = new NodalOsPixelRedactionRequest(
            $"req-{Guid.NewGuid():N}",
            SolidImage(),
            NodalOsPixelRedactionImageFormat.RawRgba32,
            W,
            H,
            new NodalOsOcrBoundingBox(0, 0, W, H),
            default,
            NodalOsOcrVisionSensitivity.High,
            [new NodalOsPixelRedactionRegion(2, 2, 6, 4, NodalOsPixelRedactionRegionKind.Password, 0.95)],
            AllowRawPersistence: false,
            AllowFullScreen: false);

        var result = redactor.Redact(request);
        Assert.AreEqual(NodalOsPixelRedactionDecision.RedactedPixels, result.Decision);
        return result;
    }

    private static NodalOsLocalWorkerProcessHealth HealthyIpc() =>
        new(
            $"health-{Guid.NewGuid():N}",
            IsResponsive: true,
            ContractVersionMatch: true,
            AuthTokenValid: true,
            WithinSizeLimits: true,
            WithinTimeoutLimits: true,
            NoAuthority: true,
            DateTimeOffset.UtcNow);

    private static NodalOsLocalWorkerProcessIsolationEvidence Isolation(
        NodalOsLocalWorkerIsolationEnforcementLevel network = NodalOsLocalWorkerIsolationEnforcementLevel.Modeled,
        NodalOsLocalWorkerIsolationEnforcementLevel filesystem = NodalOsLocalWorkerIsolationEnforcementLevel.Modeled,
        NodalOsLocalWorkerIsolationEnforcementLevel process = NodalOsLocalWorkerIsolationEnforcementLevel.Observed,
        bool noRawPersistence = true,
        bool noNetwork = true,
        bool noFilesystemWrite = true,
        bool noAuthority = true) =>
        new(
            $"evidence-{Guid.NewGuid():N}",
            network,
            filesystem,
            process,
            noRawPersistence,
            noNetwork,
            noFilesystemWrite,
            noAuthority,
            "redacted isolation evidence; no hard OS sandbox claimed unless enforced",
            Redacted: true);

    private static NodalOsPrePaddleOcrReadinessReview Review(
        NodalOsPixelRedactionResult? pixel = null,
        NodalOsLocalWorkerProcessHealth? health = null,
        NodalOsLocalWorkerProcessIsolationEvidence? isolation = null,
        bool activationBlocks = true,
        bool messageSizeLimitConfigured = true,
        bool timeoutKillBehaviorVerified = true) =>
        new(
            $"review-{Guid.NewGuid():N}",
            pixel ?? ValidPixelRedaction(),
            health ?? HealthyIpc(),
            isolation ?? Isolation(),
            FullScreenBlocked: true,
            ActivationGateStillBlocksRealOcr: activationBlocks,
            SaaSStillDisabled: true,
            HumanEscalationConfigured: true,
            RollbackPauseConfigured: true,
            EvaluationHarnessPassing: true,
            messageSizeLimitConfigured,
            timeoutKillBehaviorVerified,
            ProcessBoundaryEvaluated: true,
            NoAuthority: true,
            Redacted: true);

    [TestMethod]
    public void ReadinessBlocksIfPixelRedactionMissing()
    {
        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(Review(pixel: null) with { PixelRedactionResult = null });

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.BlockedByRedaction, report.Decision);
        Assert.IsFalse(report.PixelRedactionV2Verified);
    }

    [TestMethod]
    public void ReadinessBlocksIfRawPersistenceRisk()
    {
        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(
            Review(isolation: Isolation(noRawPersistence: false)));

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.BlockedByRawPersistenceRisk, report.Decision);
        Assert.IsFalse(report.RawNotPersisted);
    }

    [TestMethod]
    public void ReadinessBlocksIfIpcMissingAuth()
    {
        var badHealth = HealthyIpc() with { IsResponsive = false, AuthTokenValid = false };
        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(Review(health: badHealth));

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.BlockedByIpcSecurity, report.Decision);
        Assert.IsFalse(report.IpcAuthEnforcedOrModeled);
    }

    [TestMethod]
    public void ReadinessBlocksIfOversizeAcceptedOrSizeLimitMissing()
    {
        var badHealth = HealthyIpc() with { WithinSizeLimits = false };
        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(
            Review(health: badHealth, messageSizeLimitConfigured: false));

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.BlockedByIpcSecurity, report.Decision);
        Assert.IsFalse(report.MessageSizeLimitEnforced);
    }

    [TestMethod]
    public void ReadinessBlocksIfActivationGateEnablesRealOcr()
    {
        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(Review(activationBlocks: false));

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.BlockedByAuthorityRisk, report.Decision);
        Assert.IsFalse(report.ActivationGateStillBlocksRealOcr);
    }

    [TestMethod]
    public void ReadinessReportsModeledAndObservedIsolationHonestly()
    {
        var isolation = Isolation(
            network: NodalOsLocalWorkerIsolationEnforcementLevel.Modeled,
            filesystem: NodalOsLocalWorkerIsolationEnforcementLevel.NotEnforced,
            process: NodalOsLocalWorkerIsolationEnforcementLevel.Observed);

        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(Review(isolation: isolation));

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.ReadyForPaddleOcrDesignOnly, report.Decision);
        Assert.AreEqual(NodalOsLocalWorkerIsolationEnforcementLevel.Modeled, report.NetworkIsolationStatus);
        Assert.AreEqual(NodalOsLocalWorkerIsolationEnforcementLevel.NotEnforced, report.FilesystemIsolationStatus);
        Assert.AreEqual(NodalOsLocalWorkerIsolationEnforcementLevel.Observed, report.ProcessIsolationStatus);
        Assert.IsTrue(report.Warnings.Any(w => w.Contains("not claimed", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ReadinessWithEnforcedIsolationCanReachSyntheticInstallPlanOnly()
    {
        var enforced = Isolation(
            network: NodalOsLocalWorkerIsolationEnforcementLevel.Enforced,
            filesystem: NodalOsLocalWorkerIsolationEnforcementLevel.Enforced,
            process: NodalOsLocalWorkerIsolationEnforcementLevel.Enforced);

        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(Review(isolation: enforced));

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.ReadyForPaddleOcrSyntheticInstallPlan, report.Decision);
        Assert.IsTrue(report.ActivationGateStillBlocksRealOcr);
        Assert.IsTrue(report.SaaSStillDisabled);
        Assert.IsTrue(report.NoAuthority);
    }

    [TestMethod]
    public void ReadinessBlocksIfNetworkActivityObserved()
    {
        var report = new NodalOsPrePaddleOcrReadinessReviewer().Review(
            Review(isolation: Isolation(noNetwork: false)));

        Assert.AreEqual(NodalOsPrePaddleOcrReadinessDecision.BlockedByNetworkRisk, report.Decision);
    }

    [TestMethod]
    public void DocsForClaudeAuditExist()
    {
        var root = FindRepoRoot();
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "adr", "pre-paddleocr-redaction-isolation-readiness-m188-m190.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "reports", "pre-paddleocr-readiness-m190.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "audits", "claude-pre-paddleocr-readiness-audit-m190.md")));
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        Assert.Fail("Repository root with OneBrain.slnx was not found.");
        return Directory.GetCurrentDirectory();
    }
}
