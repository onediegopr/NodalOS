using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxSyntheticOcrReadiness")]
[TestCategory("OnnxRuntimeCrashIsolation")]
[TestCategory("OnnxNativeRuntimeCrash")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxNativeRuntimeCrashReadinessM211Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static NodalOsOnnxNativeRuntimeCrashProbeMatrix Matrix() =>
        new NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder().BuildDefaultMatrix();

    private static NodalOsOnnxOutOfProcessGuardResult GuardResult(
        NodalOsOnnxNativeRuntimeCrashProbeStatus status,
        NodalOsOnnxNativeRuntimeCrashKind crashKind,
        bool parentSurvived = true,
        bool orphan = false,
        bool tempCleaned = true,
        bool rawPersisted = false,
        bool callsSaas = false,
        bool noAuthority = true)
    {
        var probe = new NodalOsOnnxNativeRuntimeCrashProbeResult(
            $"pr-{Guid.NewGuid():N}", "probe-1",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.AntiAliasedPixelFontText,
            NodalOsOnnxNativeRuntimeCrashStage.DetectionRun, 256, 96,
            status, crashKind, RanInProcess: false, RanOutOfProcess: true, HostSurvived: parentSurvived,
            BoxesDetected: null, RecognitionAttempts: null,
            NoAuthority: noAuthority, CallsSaas: callsSaas, RawPersisted: rawPersisted, "test");

        return new NodalOsOnnxOutOfProcessGuardResult(
            $"gr-{Guid.NewGuid():N}", "guard-1", probe, ExitCode: -1073741819,
            TimedOut: false, ParentSurvived: parentSurvived, ChildLaunched: true,
            BlockedBeforeLaunch: false, TempFilesCleaned: tempCleaned, OrphanProcessLeft: orphan,
            RawPersisted: rawPersisted, CallsSaas: callsSaas, NoAuthority: noAuthority,
            StdErrSummary: "", "test");
    }

    private readonly NodalOsOnnxNativeRuntimeCrashReadinessReview _review = new();

    [TestMethod]
    public void Readiness_BlocksIf_NativeCrashIsInProcess()
    {
        var report = _review.Evaluate(new NodalOsOnnxNativeRuntimeCrashReadinessReview.Inputs(
            Matrix(),
            new[] { GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation) },
            InProcessCrashObserved: true,
            CleanupSucceeded: true,
            RawPersistenceDetected: false));

        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashReadinessDecision.BlockedByModelRuntime, report.Decision);
        Assert.IsFalse(report.CanRunGuardedSyntheticText);
    }

    [TestMethod]
    public void Readiness_AllowsGuardedMode_WhenCrashContainedOutOfProcess()
    {
        var report = _review.Evaluate(new NodalOsOnnxNativeRuntimeCrashReadinessReview.Inputs(
            Matrix(),
            new[] { GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation) },
            InProcessCrashObserved: false,
            CleanupSucceeded: true,
            RawPersistenceDetected: false));

        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForOutOfProcessOnly, report.Decision);
        Assert.IsTrue(report.CanRunGuardedSyntheticText);
        Assert.IsTrue(report.OutOfProcessGuardContainsCrash);
    }

    [TestMethod]
    public void Readiness_DoesNotAllowShadowMode()
    {
        var report = _review.Evaluate(new NodalOsOnnxNativeRuntimeCrashReadinessReview.Inputs(
            Matrix(),
            new[] { GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation) },
            InProcessCrashObserved: false,
            CleanupSucceeded: true,
            RawPersistenceDetected: false));

        Assert.IsFalse(report.CanAttemptRedactedCropShadow);
        Assert.IsTrue(report.ShadowModeBlocked);
    }

    [TestMethod]
    public void Readiness_BlocksIf_CleanupFails()
    {
        var report = _review.Evaluate(new NodalOsOnnxNativeRuntimeCrashReadinessReview.Inputs(
            Matrix(),
            new[] { GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation, tempCleaned: false) },
            InProcessCrashObserved: false,
            CleanupSucceeded: false,
            RawPersistenceDetected: false));

        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashReadinessDecision.BlockedByModelRuntime, report.Decision);
        Assert.IsFalse(report.TempFilesCleaned);
    }

    [TestMethod]
    public void Readiness_BlocksIf_RawPersistenceAppears()
    {
        var report = _review.Evaluate(new NodalOsOnnxNativeRuntimeCrashReadinessReview.Inputs(
            Matrix(),
            new[] { GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation, rawPersisted: true) },
            InProcessCrashObserved: false,
            CleanupSucceeded: true,
            RawPersistenceDetected: true));

        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashReadinessDecision.NotReady, report.Decision);
        Assert.IsFalse(report.NoRawPersistence);
    }

    [TestMethod]
    public void Readiness_PreservesNoAuthority_NoSaas()
    {
        var report = _review.Evaluate(new NodalOsOnnxNativeRuntimeCrashReadinessReview.Inputs(
            Matrix(),
            new[] { GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation) },
            InProcessCrashObserved: false,
            CleanupSucceeded: true,
            RawPersistenceDetected: false));

        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.NoSaas);
        Assert.IsTrue(report.ProductionPublicOcrBlocked);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Exist_ForM211()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "onnx-native-runtime-crash-isolation-m211.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m211", "onnx-native-runtime-crash-isolation-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-onnx-native-runtime-crash-isolation-audit-m211.md")));
    }
}
