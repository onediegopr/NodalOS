using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("GuardedSyntheticTextOcr")]
[TestCategory("GuardedSyntheticTextProbe")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("OnnxOcrSyntheticInference")]
[TestCategory("OnnxOcrDetectionInference")]
[TestCategory("OnnxOcrRecognitionInference")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("OnnxSyntheticOcrReadiness")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsGuardedSyntheticTextOcrM220M222Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static NodalOsGuardedSyntheticTextOcrProbeMatrix Matrix() =>
        new NodalOsGuardedSyntheticTextOcrProbeMatrixBuilder().BuildDefaultMatrix();

    private static NodalOsGuardedSyntheticTextOcrProbeResult Probe(
        NodalOsGuardedSyntheticTextOcrProbeStatus status,
        bool ranInProcess = false,
        int? boxes = null,
        bool rawPersisted = false,
        bool callsSaas = false,
        bool noAuthority = true) =>
        new(
            $"probe-result-{Guid.NewGuid():N}",
            "probe-m220",
            status,
            ranInProcess,
            RanOutOfProcess: !ranInProcess,
            ParentSurvived: true,
            ChildLaunched: !ranInProcess,
            TempFilesCleaned: true,
            rawPersisted,
            callsSaas,
            noAuthority,
            boxes,
            RecognitionAttempts: boxes > 0 ? 1 : 0,
            "test");

    private static NodalOsDetectorCompatibilityDiagnosis Detection(NodalOsGuardedSyntheticTextOcrProbeStatus status, int boxes = 0) =>
        new(
            $"det-{Guid.NewGuid():N}",
            SessionCreationReached: status != NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained,
            RuntimeVersion: "Microsoft.ML.OnnxRuntime test",
            Provider: "CPUExecutionProvider",
            ModelPath: Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx"),
            ModelOpset: "17",
            InputShape: [1, 3, 640, 640],
            new NodalOsOnnxTensorStats([1, 3, 640, 640], 0f, 1f, 0.5f, false, false, "NCHW", "RGB"),
            OutputNames: status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained ? [] : ["sigmoid_0.tmp_0"],
            OutputShapes: status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained ? [] : [[1, 1, 640, 640]],
            SessionRunCrashed: status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained,
            PostProcessingCrashed: status == NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByPostProcessing,
            boxes,
            status,
            "test");

    private static NodalOsRecognizerCompatibilityDiagnosis Recognition(NodalOsGuardedSyntheticTextOcrProbeStatus status, bool reachable = false) =>
        new(
            $"rec-{Guid.NewGuid():N}",
            reachable,
            reachable ? [1, 3, 32, 320] : [],
            CropExtractionSucceeded: reachable,
            OutputNames: reachable ? ["softmax_2.tmp_0"] : [],
            OutputShapes: reachable ? [[40, 1, 97]] : [],
            CtcDecodingAttempted: reachable,
            DictionaryCompatible: status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveRecognition,
            DictionaryId: "en-ascii",
            Confidence: status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveRecognition ? 0.91 : null,
            status,
            "test");

    private static NodalOsGuardedSyntheticTextOcrReadinessReport Review(
        NodalOsGuardedSyntheticTextOcrProbeStatus detectionStatus,
        NodalOsOcrDictionaryCompatibilityResult dictionary,
        IReadOnlyList<NodalOsGuardedSyntheticTextOcrProbeResult>? probes = null,
        bool modelsVerified = true,
        bool onnxModelsTracked = false,
        bool parentSurvived = true,
        bool cleanup = true,
        bool noFullScreen = true,
        bool noSensitive = true)
    {
        var boxes = detectionStatus == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveDetection ? 1 : 0;
        return new NodalOsGuardedSyntheticTextOcrRetryReadinessReview().Evaluate(
            new NodalOsGuardedSyntheticTextOcrRetryReadinessInput(
                DetectionModelVerified: modelsVerified,
                RecognitionModelVerified: modelsVerified,
                ClassificationModelVerified: modelsVerified,
                onnxModelsTracked,
                probes ?? [Probe(detectionStatus, boxes: boxes)],
                Detection(detectionStatus, boxes),
                Recognition(boxes > 0 ? NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByDictionary : NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, reachable: boxes > 0),
                dictionary,
                GuardExists: true,
                parentSurvived,
                ChildCleanupWorks: cleanup,
                TempCleanupWorks: cleanup,
                noFullScreen,
                noSensitive));
    }

    [TestMethod]
    public void Dictionary_ClassCountMismatch_BlocksDecode()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var ascii = service.CreateCurrentAsciiManifest(verified: true);
        var result = service.Evaluate(ascii, recognizerOutputClassCount: 97);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, result.Status);
        Assert.IsFalse(result.CtcDecoderCompatibility.DecodeAllowed);
        Assert.IsFalse(result.RecognitionSuccessAllowed);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void Dictionary_Missing_BlocksRecognitionSuccess()
    {
        var result = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, recognizerOutputClassCount: 97);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary, result.Status);
        Assert.IsFalse(result.RecognitionSuccessAllowed);
        Assert.IsFalse(result.CtcDecoderCompatibility.DecodeAllowed);
    }

    [TestMethod]
    public void Dictionary_AsciiSubset_NotUsed_WhenModelRequiresLargerCharset()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var ascii = service.CreateCurrentAsciiManifest(verified: true);

        var result = service.Evaluate(ascii, recognizerOutputClassCount: 97);

        Assert.AreNotEqual(97, result.CtcDecoderCompatibility.DictionaryClassCountIncludingBlank);
        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, result.Status);
    }

    [TestMethod]
    public void CtcDecode_PreservesNoAuthority_AndLowConfidenceRequiresHumanReview()
    {
        var characters = Enumerable.Range(0, 96).Select(i => ((char)('!' + (i % 60))).ToString()).ToArray();
        var dictionary = new NodalOsOcrDictionaryManifest("paddleocr-en-test", "en", characters.Length, 1, "test", "sha256-test", Verified: true, NoAuthority: true);
        var result = new NodalOsOcrDictionaryCompatibilityService().Evaluate(dictionary, recognizerOutputClassCount: 97);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.Compatible, result.Status);
        Assert.IsTrue(result.RecognitionSuccessAllowed);
        Assert.IsTrue(result.RequiresHumanReview);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void RecognitionEmpty_ReportedHonestly()
    {
        var dictionary = new NodalOsOnnxOcrCharacterDictionary().Load("digits", "digits");
        var post = new NodalOsOnnxOcrRecognizerPostProcessor(dictionary);
        var output = Enumerable.Repeat(0f, 2 * 11).ToArray();
        output[dictionary.BlankIndex] = 1f;
        output[11 + dictionary.BlankIndex] = 1f;

        var decoded = post.Decode(output, [2, 11]);

        Assert.AreEqual(NodalOsOnnxOcrPostProcessingStatus.RecognitionEmpty, decoded.Status);
        Assert.IsTrue(decoded.NoAuthority);
        Assert.IsTrue(decoded.Redacted);
    }

    [TestMethod]
    public void RetryReadiness_BlocksIfDetRecNotVerified()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);
        var report = Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, dictionary, modelsVerified: false);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady, report.Decision);
        Assert.IsTrue(report.ShadowModeBlocked);
    }

    [TestMethod]
    public void RetryReadiness_BlocksIfRiskyTextRanInProcess()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);
        var report = Review(
            NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained,
            dictionary,
            [Probe(NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained, ranInProcess: true)]);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady, report.Decision);
        Assert.IsFalse(report.RiskyTextNeverRanInProcess);
    }

    [TestMethod]
    public void RetryReadiness_MapsContainedNativeCrash_ToBlockedByModelRuntime()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);
        var report = Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained, dictionary);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.BlockedByModelRuntime, report.Decision);
        Assert.IsTrue(report.ParentSurvivedCrash);
        Assert.IsTrue(report.ShadowModeBlocked);
        Assert.IsTrue(report.ProductionPublicOcrBlocked);
    }

    [TestMethod]
    public void RetryReadiness_MapsDictionaryMismatch_AfterPositiveDetection()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var dictionary = service.Evaluate(service.CreateCurrentAsciiManifest(verified: true), 97);
        var report = Review(NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveDetection, dictionary);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForDictionaryCompletion, report.Decision);
        Assert.IsTrue(report.DictionaryStatusDocumented);
    }

    [TestMethod]
    public void RetryReadiness_MapsPositiveDetectionWithoutRecognition_Honestly()
    {
        var characters = Enumerable.Range(0, 96).Select(i => i.ToString()).ToArray();
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(
            new NodalOsOcrDictionaryManifest("compatible-test", "en", characters.Length, 1, "test", "sha256-test", Verified: true, NoAuthority: true),
            97);

        var report = Review(NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveDetection, dictionary);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForMoreSyntheticFixtures, report.Decision);
        Assert.IsTrue(report.ShadowModeBlocked);
    }

    [TestMethod]
    public void RetryReadiness_BlocksCleanupRawSensitiveFullScreenNoAuthorityViolations()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady,
            Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, dictionary, [Probe(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, rawPersisted: true)]).Decision);
        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady,
            Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, dictionary, [Probe(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, noAuthority: false)]).Decision);
        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady,
            Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, dictionary, cleanup: false).Decision);
        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady,
            Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, dictionary, noFullScreen: false).Decision);
        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady,
            Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, dictionary, noSensitive: false).Decision);
    }

    [TestMethod]
    public void RetryReadiness_BlocksUnexpectedTrackedOnnxModel()
    {
        var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, 97);
        var report = Review(NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected, dictionary, onnxModelsTracked: true);

        Assert.AreEqual(NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady, report.Decision);
    }

    [TestMethod]
    public void ProbeMatrix_CoversM220RequiredFixtures_WithoutInProcessRisk()
    {
        var matrix = Matrix();

        CollectionAssert.IsSubsetOf(
            new[] { "TEST", "NODAL", "ABC123", "12345", "SAFE TEXT" },
            matrix.Requests.Select(r => r.Fixture.ExpectedText).Distinct().ToArray());
        Assert.IsTrue(matrix.Requests.Any(r => r.Fixture.Width == 320 && r.Fixture.Height == 128));
        Assert.IsTrue(matrix.Requests.Any(r => r.Fixture.Width == 640 && r.Fixture.Height == 160));
        Assert.IsTrue(matrix.Requests.Any(r => r.Fixture.Width == 640 && r.Fixture.Height == 320));
        Assert.IsTrue(matrix.Requests.Any(r => r.Fixture.Width == 640 && r.Fixture.Height == 640));
        Assert.IsTrue(matrix.Requests.All(r => !r.AllowInProcess));
        Assert.IsTrue(matrix.NoRawPersistence);
        Assert.IsTrue(matrix.NoFullScreen);
        Assert.IsTrue(matrix.NoSensitive);
        Assert.IsTrue(matrix.NoSaas);
        Assert.IsTrue(matrix.NoAuthority);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM222()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "guarded-synthetic-text-ocr-retry-m222.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m222", "guarded-synthetic-text-ocr-retry-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-guarded-synthetic-text-ocr-retry-audit-m222.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "guarded-synthetic-text-ocr-retry-decision-m220-m222.md")));
    }
}
