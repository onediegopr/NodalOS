using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RecognizerRuntimeExperiment")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("RecognizerCompatibility")]
[TestCategory("FullOcrHandoff")]
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
public sealed class NodalOsRecognizerRuntimeCompatibilityM229M231Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void ExperimentMatrix_RiskyRecognizerProbes_RunOnlyOutOfProcess_AndRecordOptions()
    {
        var matrix = new NodalOsRecognizerRuntimeExperimentBuilder().BuildMinimalMatrix();

        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsRecognizerRuntimeTensorKind.Zero));
        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsRecognizerRuntimeTensorKind.Ones));
        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsRecognizerRuntimeTensorKind.Gradient));
        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsRecognizerRuntimeTensorKind.Checker));
        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop));
        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsRecognizerRuntimeTensorKind.HighContrastManualCrop));
        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsRecognizerRuntimeTensorKind.DetectorDerivedCrop));
        Assert.IsTrue(matrix.Any(e => e.SessionOptions.OptionKind == NodalOsRecognizerRuntimeSessionOptionKind.GraphOptimizationDisabled));
        Assert.IsTrue(matrix.Any(e => e.SessionOptions.OptionKind == NodalOsRecognizerRuntimeSessionOptionKind.SequentialExecution));
        Assert.IsTrue(matrix.Any(e => e.SessionOptions.OptionKind == NodalOsRecognizerRuntimeSessionOptionKind.DeterministicMinimal));
        Assert.IsTrue(matrix.Any(e => e.Layout == NodalOsRecognizerRuntimeProbeLayout.Nhwc && !e.RequiresOutOfProcessGuard));
        Assert.IsTrue(matrix.Any(e => e.ShapeKind == NodalOsRecognizerRuntimeShapeKind.Invalid && !e.RequiresOutOfProcessGuard));
        Assert.IsTrue(matrix.Where(e => e.RequiresOutOfProcessGuard).All(e => !e.AllowInProcess));
        Assert.IsTrue(matrix.All(e => e.Synthetic && !e.RawPersisted && !e.FullScreen && !e.Sensitive && e.NoAuthority));
    }

    [TestMethod]
    public void Decision_AllCoreTensorCrash_MapsToOnnxRuntimeVersionExperiment()
    {
        var results = new[]
        {
            Result(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained),
            Result(NodalOsRecognizerRuntimeTensorKind.Ones, NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained),
            Result(NodalOsRecognizerRuntimeTensorKind.Gradient, NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained)
        };

        var finding = Decide(results);

        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForOnnxRuntimeVersionExperiment, finding.Decision);
        Assert.IsTrue(finding.AllCoreTensorsCrashed);
        Assert.IsTrue(finding.ShadowModeBlocked);
        Assert.IsTrue(finding.ProductiveOcrBlocked);
    }

    [TestMethod]
    public void Decision_OptionSuccess_MapsToSessionOptionsFix()
    {
        var results = new[]
        {
            Result(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained),
            Result(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured, NodalOsRecognizerRuntimeSessionOptionKind.SingleThreaded)
        };

        var finding = Decide(results);

        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForRecognizerSessionOptionsFix, finding.Decision);
        Assert.IsTrue(finding.AnySessionOptionAvoidedCrash);
    }

    [TestMethod]
    public void Decision_CropOnlyCrash_MapsToPreprocessingFix()
    {
        var results = new[]
        {
            Result(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured),
            Result(NodalOsRecognizerRuntimeTensorKind.Ones, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured),
            Result(NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop, NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained)
        };

        var finding = Decide(results);

        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForRecognizerPreprocessingFix, finding.Decision);
        Assert.IsTrue(finding.CropOnlyCrash);
    }

    [TestMethod]
    public void Decision_SuccessfulRunPlusDictionaryMismatch_MapsToDictionaryCompletion()
    {
        var results = new[]
        {
            Result(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured)
        };

        var finding = Decide(results);

        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForDictionaryCompletion, finding.Decision);
        Assert.IsTrue(finding.DictionaryMismatchDetected);
    }

    [TestMethod]
    public void Decision_InvalidShapeOnly_MapsToInputShapeBlock()
    {
        var results = new[]
        {
            Result(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape, shapeKind: NodalOsRecognizerRuntimeShapeKind.Invalid)
        };

        var finding = Decide(results);

        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.BlockedByRecognizerInputShape, finding.Decision);
        Assert.IsTrue(finding.InvalidShapeDetected);
    }

    [TestMethod]
    public void Decision_BlocksIfRecognizerModelParentCleanupRawOrAuthorityGateFails()
    {
        var result = Result(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured);
        var service = new NodalOsRecognizerRuntimeCompatibilityDecisionService();
        var dictionary = Dictionary();

        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.NotReady, service.Decide([result], false, true, true, true, true, dictionary).Decision);
        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, false, true, true, true, dictionary).Decision);
        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, true, false, true, true, dictionary).Decision);
        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, true, true, false, true, dictionary).Decision);
        Assert.AreEqual(NodalOsRecognizerRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, true, true, true, false, dictionary).Decision);
    }

    [TestMethod]
    public void TensorStats_RecordNaNInfinityValidationAndLayout()
    {
        var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(
            [0f, float.PositiveInfinity],
            [1, 3, 32, 1],
            "NCHW",
            "RGB");

        Assert.IsTrue(stats.HasInfinity);
        Assert.AreEqual("NCHW", stats.ChannelLayout);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM231()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "recognizer-runtime-compatibility-experiment-m231.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m231", "recognizer-runtime-compatibility-experiment-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-recognizer-runtime-compatibility-experiment-audit-m231.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "recognizer-runtime-compatibility-decision-m229-m231.md")));
    }

    private static NodalOsRecognizerModelCompatibilityFinding Decide(IReadOnlyList<NodalOsRecognizerRuntimeCompatibilityResult> results) =>
        new NodalOsRecognizerRuntimeCompatibilityDecisionService().Decide(
            results,
            recognizerModelVerified: true,
            parentSurvived: true,
            tempCleanup: true,
            noRawPersistence: true,
            noAuthority: true,
            Dictionary());

    private static NodalOsOcrDictionaryCompatibilityResult Dictionary()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        return service.Evaluate(service.CreateCurrentAsciiManifest(verified: true), 97);
    }

    private static NodalOsRecognizerRuntimeCompatibilityResult Result(
        NodalOsRecognizerRuntimeTensorKind tensor,
        NodalOsRecognizerRuntimeProbeStatus status,
        NodalOsRecognizerRuntimeSessionOptionKind option = NodalOsRecognizerRuntimeSessionOptionKind.Default,
        NodalOsRecognizerRuntimeShapeKind shapeKind = NodalOsRecognizerRuntimeShapeKind.CurrentPipelineFixed)
    {
        var shape = shapeKind == NodalOsRecognizerRuntimeShapeKind.Invalid ? new[] { 1, 1, 0, 0 } : new[] { 1, 3, 32, 320 };
        var experiment = new NodalOsRecognizerRuntimeExperiment(
            $"exp-{Guid.NewGuid():N}",
            tensor,
            NodalOsRecognizerRuntimeProbeLayout.Nchw,
            shapeKind,
            shape,
            NodalOsRecognizerRuntimeExperimentBuilder.Option(option),
            RequiresOutOfProcessGuard: shapeKind != NodalOsRecognizerRuntimeShapeKind.Invalid,
            AllowInProcess: false,
            Synthetic: true,
            FullScreen: false,
            Sensitive: false,
            RawPersisted: false,
            NoAuthority: true);

        return new NodalOsRecognizerRuntimeCompatibilityResult(
            $"result-{Guid.NewGuid():N}",
            experiment,
            status,
            "Microsoft.ML.OnnxRuntime 1.18.1",
            "onnxruntime.dll",
            "CPUExecutionProvider",
            "X64",
            Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx"),
            7653044,
            "e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318",
            ["x"],
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? ["softmax_2.tmp_0"] : ["softmax_2.tmp_0"],
            [[40, 1, 97]],
            new NodalOsOnnxTensorStats(shape, 0, 1, 0.5f, false, false, "NCHW", "RGB"),
            97,
            40,
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? -1073741676 : 0,
            status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained ? "0xC0000094" : "0x00000000",
            "RecognitionRun",
            SessionCreated: status != NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape,
            RunAttempted: status != NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape,
            ParentSurvived: true,
            TempFilesCleaned: true,
            OrphanProcessLeft: false,
            RawPersisted: false,
            CallsSaas: false,
            NoAuthority: true,
            "test");
    }
}
