using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("GuardedSyntheticTextOcr")]
[TestCategory("GuardedSyntheticTextProbe")]
[TestCategory("SyntheticTextOcrDiagnosis")]
[TestCategory("DetectorCompatibilityDiagnosis")]
[TestCategory("RecognizerCompatibilityDiagnosis")]
[TestCategory("OnnxNativeRuntimeCrash")]
[TestCategory("OnnxNativeCrashProbe")]
[TestCategory("OnnxRuntimeCrashIsolation")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxOcrSyntheticTextRun")]
[TestCategory("SyntheticOcrTextFixture")]
[TestCategory("OnnxOcrSyntheticInference")]
[TestCategory("OnnxOcrDetectionInference")]
[TestCategory("OnnxOcrRecognitionInference")]
[TestCategory("OnnxSyntheticOcrReadiness")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsGuardedSyntheticTextOcrM212M214Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static NodalOsGuardedSyntheticTextOcrProbeMatrix Matrix() =>
        new NodalOsGuardedSyntheticTextOcrProbeMatrixBuilder().BuildDefaultMatrix();

    private static string Configuration =>
        AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release") ? "Release" : "Debug";

    private static string? RunnerExePath()
    {
        var exe = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", Configuration, "net11.0",
            "OneBrain.Tools.OnnxOcrProbeRunner.exe");
        return File.Exists(exe) ? exe : null;
    }

    private static NodalOsOnnxOutOfProcessGuardResult GuardResult(
        NodalOsOnnxNativeRuntimeCrashProbeStatus status,
        NodalOsOnnxNativeRuntimeCrashKind crashKind = NodalOsOnnxNativeRuntimeCrashKind.NoCrash,
        int? boxes = null)
    {
        var probe = new NodalOsOnnxNativeRuntimeCrashProbeResult(
            $"pr-{Guid.NewGuid():N}",
            "probe-1",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
            NodalOsOnnxNativeRuntimeCrashStage.DetectionRun,
            256,
            96,
            status,
            crashKind,
            RanInProcess: false,
            RanOutOfProcess: true,
            HostSurvived: true,
            BoxesDetected: boxes,
            RecognitionAttempts: boxes > 0 ? 1 : 0,
            NoAuthority: true,
            CallsSaas: false,
            RawPersisted: false,
            "test");

        return new NodalOsOnnxOutOfProcessGuardResult(
            $"gr-{Guid.NewGuid():N}",
            "guard-1",
            probe,
            ExitCode: crashKind == NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation ? -1073741819 : 0,
            TimedOut: status == NodalOsOnnxNativeRuntimeCrashProbeStatus.TimedOut,
            ParentSurvived: true,
            ChildLaunched: true,
            BlockedBeforeLaunch: false,
            TempFilesCleaned: true,
            OrphanProcessLeft: false,
            RawPersisted: false,
            CallsSaas: false,
            NoAuthority: true,
            StdErrSummary: "",
            "test");
    }

    [TestMethod]
    public void ProbeMatrix_RecordsFixtureRenderPreprocessingMetadata()
    {
        var matrix = Matrix();

        Assert.AreEqual(13, matrix.FixtureVariantCount);
        Assert.AreEqual(6, matrix.DimensionCount);
        Assert.AreEqual(7, matrix.PreProcessingVariantCount);
        Assert.IsTrue(matrix.Requests.Any(r => r.Fixture.Width == 256 && r.Fixture.Height == 96));
        Assert.IsTrue(matrix.Requests.Any(r => r.Fixture.RenderMode == NodalOsSyntheticOcrTextRenderMode.PixelFont));
        Assert.IsTrue(matrix.Requests.Any(r => r.Fixture.RenderMode == NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont));
        Assert.IsTrue(matrix.Requests.Any(r => r.PreProcessingVariant == NodalOsGuardedSyntheticTextOcrPreProcessingVariant.ChannelLayoutValidation));
        Assert.IsTrue(matrix.NoRawPersistence);
        Assert.IsTrue(matrix.NoFullScreen);
        Assert.IsTrue(matrix.NoSensitive);
        Assert.IsTrue(matrix.NoSaas);
        Assert.IsTrue(matrix.NoAuthority);
    }

    [TestMethod]
    public void ProbeMatrix_RiskyTextProbesAreRejectedInProcess()
    {
        var matrix = Matrix();

        Assert.IsTrue(matrix.RiskyProbesRequireGuard);
        Assert.IsTrue(matrix.RejectsRiskyInProcess);
        Assert.IsTrue(matrix.Requests.Where(r => r.Fixture.RiskyTextFixture && !r.Fixture.UsesSystemFont).All(r => r.RequiresOutOfProcessGuard));
        Assert.IsTrue(matrix.Requests.All(r => !r.AllowInProcess));
    }

    [TestMethod]
    public void Diagnostic_CapturesInputTensorStats()
    {
        var request = Matrix().Requests.First(r => !r.Fixture.UsesSystemFont);
        var tensor = Enumerable.Range(0, 1 * 3 * 32 * 32).Select(i => (float)(i % 255) / 255).ToArray();

        var diagnosis = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseDetection(
            request,
            tensor,
            [1, 3, 32, 32],
            GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NoTextDetected, boxes: 0),
            RepoRoot);

        Assert.AreEqual("NCHW", diagnosis.InputTensorStats.ChannelLayout);
        Assert.AreEqual("RGB", diagnosis.InputTensorStats.ColorOrder);
        Assert.IsFalse(diagnosis.InputTensorStats.HasNaN);
        Assert.IsFalse(diagnosis.InputTensorStats.HasInfinity);
        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, diagnosis.Status);
    }

    [TestMethod]
    public void Diagnostic_BlocksNaNInfinityBeforeRuntime()
    {
        var request = Matrix().Requests.First(r => !r.Fixture.UsesSystemFont);
        var tensor = new[] { 0f, float.NaN, float.PositiveInfinity };

        var diagnosis = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseDetection(
            request,
            tensor,
            [1, 3, 1, 1],
            null,
            RepoRoot);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByPreProcessing, diagnosis.Status);
        Assert.IsTrue(diagnosis.InputTensorStats.HasNaN);
        Assert.IsTrue(diagnosis.InputTensorStats.HasInfinity);
    }

    [TestMethod]
    public void Diagnostic_MapsDetectionRunCrash_ToContainedNativeCrash()
    {
        var request = Matrix().Requests.First(r => !r.Fixture.UsesSystemFont);
        var tensor = new float[1 * 3 * 32 * 32];

        var diagnosis = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseDetection(
            request,
            tensor,
            [1, 3, 32, 32],
            GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation),
            RepoRoot);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained, diagnosis.Status);
        Assert.IsTrue(diagnosis.SessionRunCrashed);
    }

    [TestMethod]
    public void Diagnostic_MapsDictionaryMissingMismatch()
    {
        var recognition = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseRecognition(
            GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.Passed, boxes: 1));

        Assert.IsTrue(recognition.Reachable);
        Assert.IsTrue(recognition.CtcDecodingAttempted);
        Assert.IsFalse(recognition.DictionaryCompatible);
        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByDictionary, recognition.Status);
    }

    [TestMethod]
    public void Readiness_BlocksIfRiskyTextRanInProcess()
    {
        var matrix = Matrix();
        var request = matrix.Requests.First(r => !r.Fixture.UsesSystemFont);
        var detection = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseDetection(
            request, new float[1 * 3 * 32 * 32], [1, 3, 32, 32],
            GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation),
            RepoRoot);
        var recognition = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseRecognition(null);
        var probeResult = new NodalOsGuardedSyntheticTextOcrProbeResult(
            "r", request.ProbeId, NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained,
            RanInProcess: true, RanOutOfProcess: false, ParentSurvived: true, ChildLaunched: false,
            TempFilesCleaned: true, RawPersisted: false, CallsSaas: false, NoAuthority: true,
            BoxesDetected: null, RecognitionAttempts: null, "bad in-process run");

        var report = new NodalOsGuardedSyntheticTextOcrReadinessReview().Evaluate(
            new NodalOsGuardedSyntheticTextOcrReadinessReview.Inputs(matrix, [probeResult], detection, recognition, true, true, true, true));

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady, report.Decision);
        Assert.IsFalse(report.RiskyTextNeverRanInProcess);
    }

    [TestMethod]
    public void Readiness_MapsContainedNativeCrash_ToRuntimeVersionExperiment()
    {
        var matrix = Matrix();
        var request = matrix.Requests.First(r => !r.Fixture.UsesSystemFont);
        var detection = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseDetection(
            request, new float[1 * 3 * 32 * 32], [1, 3, 32, 32],
            GuardResult(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash, NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation),
            RepoRoot);
        var recognition = new NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder().DiagnoseRecognition(null);
        var probeResult = new NodalOsGuardedSyntheticTextOcrProbeResult(
            "r", request.ProbeId, NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained,
            RanInProcess: false, RanOutOfProcess: true, ParentSurvived: true, ChildLaunched: true,
            TempFilesCleaned: true, RawPersisted: false, CallsSaas: false, NoAuthority: true,
            BoxesDetected: null, RecognitionAttempts: null, "contained");

        var report = new NodalOsGuardedSyntheticTextOcrReadinessReview().Evaluate(
            new NodalOsGuardedSyntheticTextOcrReadinessReview.Inputs(matrix, [probeResult], detection, recognition, true, true, true, true));

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForOnnxRuntimeVersionExperiment, report.Decision);
        Assert.IsTrue(report.ShadowModeBlocked);
        Assert.IsTrue(report.ProductionPublicOcrBlocked);
    }

    [TestMethod]
    public void RiskySyntheticTextProbe_RunsOnlyThroughGuard_ParentSurvives()
    {
        var runner = RunnerExePath();
        if (runner is null) { Assert.Inconclusive("Probe runner not built; skipping."); return; }

        var modelDir = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx");
        if (!File.Exists(Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx")))
        {
            Assert.Inconclusive("Real ONNX models not present; skipping risky child probe.");
            return;
        }

        var probe = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
            $"probe-{Guid.NewGuid():N}",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.NumericText,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            256,
            96,
            NodalOsOnnxNativeRuntimeCrashStage.DetectionRun,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: true);

        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashFixtureSafety.QuarantinedOutOfProcessOnly, probe.ClassifySafety());

        var result = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
            $"guard-{Guid.NewGuid():N}",
            probe,
            runner,
            ["--probe", "--repo-root", RepoRoot],
            TimeoutMs: 60000,
            MaxOutputBytes: 64 * 1024,
            AllowRawPersistence: false));

        Assert.IsTrue(result.ParentSurvived);
        Assert.IsFalse(result.OrphanProcessLeft);
        Assert.IsTrue(result.TempFilesCleaned);
        Assert.IsFalse(result.RawPersisted);
        Assert.IsFalse(result.CallsSaas);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsTrue(result.ProbeResult.RanOutOfProcess || result.ProbeResult.Status == NodalOsOnnxNativeRuntimeCrashProbeStatus.UnsupportedFixture);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM214()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "guarded-synthetic-text-ocr-diagnosis-m214.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m214", "guarded-synthetic-text-ocr-diagnosis-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-guarded-synthetic-text-ocr-diagnosis-audit-m214.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "guarded-synthetic-text-ocr-runtime-decision-m212-m214.md")));
    }
}
