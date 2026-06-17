using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("FullOcrHandoff")]
[TestCategory("RecognizerRuntimeProbe")]
[TestCategory("RecognizerCompatibility")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DetectorRuntimeCompatibility")]
[TestCategory("DetectorCrashProbe")]
[TestCategory("OnnxRuntimeCrashIsolation")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("GuardedSyntheticTextOcr")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsFullOcrHandoffRecognitionRuntimeM226M228Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Handoff_InvalidBox_OutOfBounds_EmptyCrop_BlockBeforeRuntime()
    {
        var invalid = Handoff(NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox, NodalOsFullOcrHandoffBoxKind.Degenerate);
        var oob = Handoff(NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop, NodalOsFullOcrHandoffBoxKind.OutOfBounds);
        var empty = Handoff(NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop, NodalOsFullOcrHandoffBoxKind.EmptyCrop);

        Assert.IsFalse(invalid.RecognizerRunAttempted);
        Assert.IsFalse(oob.RecognizerRunAttempted);
        Assert.IsFalse(empty.RecognizerRunAttempted);
        Assert.IsFalse(invalid.RawPersisted);
        Assert.IsTrue(invalid.RanOutOfProcess);
        Assert.IsTrue(invalid.ParentSurvived);
        Assert.IsTrue(invalid.NoAuthority);
    }

    [TestMethod]
    public void Handoff_RecordsLastSuccessfulStage_AndRecognizerTensorShape()
    {
        var result = Handoff(NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded, NodalOsFullOcrHandoffBoxKind.ManualSafe);

        Assert.AreEqual(NodalOsFullOcrHandoffStage.RecognizerOutputMetadata, result.LastSuccessfulStage);
        CollectionAssert.AreEqual(new[] { 1, 3, 32, 320 }, result.RecognizerInputShape);
        Assert.IsTrue(result.CropExtractionSucceeded);
        Assert.IsTrue(result.RecognizerTensorPrepared);
        Assert.IsTrue(result.RecognizerSessionCreated);
        Assert.IsTrue(result.RecognizerRunAttempted);
    }

    [TestMethod]
    public void RecognizerRuntime_RecordsZeroOnesGradientSyntheticCrop_AndClassCount()
    {
        var results = new[]
        {
            Recognizer(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured),
            Recognizer(NodalOsRecognizerRuntimeTensorKind.Ones, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured),
            Recognizer(NodalOsRecognizerRuntimeTensorKind.Gradient, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured),
            Recognizer(NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured),
        };

        CollectionAssert.AreEquivalent(
            new[]
            {
                NodalOsRecognizerRuntimeTensorKind.Zero,
                NodalOsRecognizerRuntimeTensorKind.Ones,
                NodalOsRecognizerRuntimeTensorKind.Gradient,
                NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop
            },
            results.Select(r => r.TensorKind).ToArray());
        Assert.IsTrue(results.All(r => r.OutputClassCount == 97));
        Assert.IsTrue(results.All(r => r.RunAttempted));
        Assert.IsTrue(results.All(r => !r.RawPersisted && !r.CallsSaas && r.NoAuthority));
    }

    [TestMethod]
    public void RecognizerRuntime_CrashMapsToContainedCrash()
    {
        var result = Recognizer(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained);

        Assert.AreEqual(-1073741676, result.ExitCode);
        Assert.AreEqual("0xC0000094", result.ExitCodeHex);
        Assert.IsTrue(result.ParentSurvived);
        Assert.IsTrue(result.TempFilesCleaned);
    }

    [TestMethod]
    public void Dictionary_ClassCountMismatch_BlocksDecode_ForRecognizer97()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var dictionary = service.Evaluate(service.CreateCurrentAsciiManifest(verified: true), 97);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, dictionary.Status);
        Assert.AreEqual(97, dictionary.CtcDecoderCompatibility.RecognizerOutputClassCount);
        Assert.AreEqual(87, dictionary.CtcDecoderCompatibility.DictionaryClassCountIncludingBlank);
        Assert.IsFalse(dictionary.CtcDecoderCompatibility.DecodeAllowed);
        Assert.IsFalse(dictionary.RecognitionSuccessAllowed);
    }

    [TestMethod]
    public void Decision_MapsRecognizerRuntimeSuccess_WithDictionaryMismatch_ToDictionaryCompletion()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var input = Input(
            [Handoff(NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded, NodalOsFullOcrHandoffBoxKind.ManualSafe)],
            [Recognizer(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured)],
            service.Evaluate(service.CreateCurrentAsciiManifest(verified: true), 97));

        var report = new NodalOsFullOcrRuntimeDecisionService().Decide(input);

        Assert.AreEqual(NodalOsFullOcrRuntimeDecision.ReadyForDictionaryCompletion, report.Decision);
        Assert.IsTrue(report.DictionaryMismatchDetected);
        Assert.IsTrue(report.ShadowModeBlocked);
        Assert.IsTrue(report.ProductiveOcrBlocked);
    }

    [TestMethod]
    public void Decision_MapsRecognizerRuntimeCrash_ToRecognizerRuntimeExperiment()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);
        var input = Input(
            [Handoff(NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained, NodalOsFullOcrHandoffBoxKind.ManualSafe)],
            [Recognizer(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained)],
            dictionary);

        var report = new NodalOsFullOcrRuntimeDecisionService().Decide(input);

        Assert.AreEqual(NodalOsFullOcrRuntimeDecision.ReadyForRecognizerRuntimeExperiment, report.Decision);
        Assert.IsTrue(report.RecognizerRuntimeCrashDetected);
    }

    [TestMethod]
    public void Decision_MapsHandoffFailure_ToHandoffFix()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);
        var input = Input(
            [Handoff(NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop, NodalOsFullOcrHandoffBoxKind.OutOfBounds)],
            [],
            dictionary);

        var report = new NodalOsFullOcrRuntimeDecisionService().Decide(input);

        Assert.AreEqual(NodalOsFullOcrRuntimeDecision.ReadyForHandoffFix, report.Decision);
        Assert.IsTrue(report.HandoffFailureDetected);
    }

    [TestMethod]
    public void Decision_BlocksIfSafetyGateFails()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);
        var input = Input([], [], dictionary) with { RiskyOcrNeverRanInProcess = false };

        var report = new NodalOsFullOcrRuntimeDecisionService().Decide(input);

        Assert.AreEqual(NodalOsFullOcrRuntimeDecision.NotReady, report.Decision);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM228()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "full-ocr-handoff-recognition-runtime-isolation-m228.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m228", "full-ocr-handoff-recognition-runtime-isolation-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-full-ocr-handoff-recognition-runtime-isolation-audit-m228.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "full-ocr-handoff-recognition-runtime-decision-m226-m228.md")));
    }

    private static NodalOsFullOcrRuntimeDecisionInput Input(
        IReadOnlyList<NodalOsFullOcrHandoffProbeResult> handoff,
        IReadOnlyList<NodalOsRecognizerRuntimeProbeResult> recognizer,
        NodalOsOcrDictionaryCompatibilityResult dictionary) =>
        new(
            ModelsVerified: true,
            RiskyOcrNeverRanInProcess: true,
            ParentSurvived: true,
            TempCleanup: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: true,
            handoff,
            recognizer,
            dictionary);

    private static NodalOsFullOcrHandoffProbeResult Handoff(
        NodalOsFullOcrHandoffProbeStatus status,
        NodalOsFullOcrHandoffBoxKind boxKind) =>
        new(
            $"handoff-{Guid.NewGuid():N}",
            boxKind,
            status,
            status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded
                ? NodalOsFullOcrHandoffStage.RecognizerOutputMetadata
                : NodalOsFullOcrHandoffStage.CropBoundsCalculation,
            32,
            24,
            status == NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox ? 0 : 220,
            status == NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop ? 0 : 52,
            status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded ? [1, 3, 32, 320] : [],
            DetectorRunSucceeded: true,
            DetectorPostProcessingReached: true,
            CropExtractionSucceeded: status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded,
            RecognizerTensorPrepared: status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded,
            RecognizerSessionCreated: status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded,
            RecognizerRunAttempted: status is NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded or NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained,
            ExitCode: status == NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained ? -1073741676 : 0,
            ExitCodeHex: status == NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained ? "0xC0000094" : "0x00000000",
            RanOutOfProcess: true,
            ParentSurvived: true,
            TempFilesCleaned: true,
            RawPersisted: false,
            CallsSaas: false,
            NoAuthority: true,
            "test");

    private static NodalOsRecognizerRuntimeProbeResult Recognizer(
        NodalOsRecognizerRuntimeTensorKind tensorKind,
        NodalOsRecognizerRuntimeProbeStatus status) =>
        new(
            $"rec-{Guid.NewGuid():N}",
            tensorKind,
            status,
            "Microsoft.ML.OnnxRuntime 1.18.1",
            "CPUExecutionProvider",
            Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx"),
            ["x"],
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? [] : ["softmax_2.tmp_0"],
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? [] : [[40, 1, 97]],
            [1, 3, 32, 320],
            new NodalOsOnnxTensorStats([1, 3, 32, 320], 0, 1, 0.5f, false, false, "NCHW", "RGB"),
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? null : 97,
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? null : 40,
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? -1073741676 : 0,
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? "0xC0000094" : "0x00000000",
            "RecognitionRun",
            SessionCreated: true,
            RunAttempted: true,
            RanOutOfProcess: true,
            ParentSurvived: true,
            TempFilesCleaned: true,
            RawPersisted: false,
            CallsSaas: false,
            NoAuthority: true,
            "test");
}
