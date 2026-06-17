using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("DetectorRuntimeCompatibility")]
[TestCategory("DetectorCrashProbe")]
[TestCategory("OnnxRuntimeCrashIsolation")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("GuardedSyntheticTextOcr")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsDetectorRuntimeCompatibilityM223M225Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void ExperimentMatrix_RequiresOutOfProcess_RecordsTensorAndSessionOptions()
    {
        var matrix = new NodalOsDetectorRuntimeCompatibilityExperimentBuilder().BuildMinimalMatrix();

        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsDetectorRuntimeProbeTensorKind.Zero));
        Assert.IsTrue(matrix.Any(e => e.TensorKind == NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText));
        Assert.IsTrue(matrix.Any(e => e.SessionOptions.OptionKind == NodalOsDetectorRuntimeSessionOptionKind.GraphOptimizationDisabled));
        Assert.IsTrue(matrix.Any(e => e.SessionOptions.OptionKind == NodalOsDetectorRuntimeSessionOptionKind.SingleThreaded));
        Assert.IsTrue(matrix.Where(e => e.Layout == NodalOsDetectorRuntimeProbeLayout.Nchw).All(e => e.RequiresOutOfProcessGuard));
        Assert.IsTrue(matrix.All(e => !e.AllowInProcess));
        Assert.IsTrue(matrix.All(e => e.Synthetic && !e.RawPersisted && !e.FullScreen && !e.Sensitive && e.NoAuthority));
    }

    [TestMethod]
    public void Decision_MapsAllCrash_ToRuntimeVersionExperiment()
    {
        var results = new[] { Result(NodalOsDetectorRuntimeCompatibilityStatus.NativeRuntimeCrashContained, NodalOsDetectorRuntimeProbeTensorKind.Zero) };

        var finding = new NodalOsDetectorRuntimeCompatibilityDecisionService().Decide(results, true, true, true, true, true);

        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForOnnxRuntimeVersionExperiment, finding.Decision);
        Assert.IsTrue(finding.ShadowModeBlocked);
        Assert.IsTrue(finding.ProductiveOcrBlocked);
    }

    [TestMethod]
    public void Decision_MapsOptionSuccess_ToSessionOptionsFix()
    {
        var results = new[]
        {
            Result(NodalOsDetectorRuntimeCompatibilityStatus.NativeRuntimeCrashContained, NodalOsDetectorRuntimeProbeTensorKind.Zero, NodalOsDetectorRuntimeSessionOptionKind.Default),
            Result(NodalOsDetectorRuntimeCompatibilityStatus.RunSucceeded, NodalOsDetectorRuntimeProbeTensorKind.Zero, NodalOsDetectorRuntimeSessionOptionKind.SingleThreaded)
        };

        var finding = new NodalOsDetectorRuntimeCompatibilityDecisionService().Decide(results, true, true, true, true, true);

        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForSessionOptionsFix, finding.Decision);
        Assert.IsTrue(finding.AnySessionOptionAvoidedCrash);
    }

    [TestMethod]
    public void Decision_MapsTensorSpecificCrash_ToPreprocessingOrRendererFix()
    {
        var results = new[]
        {
            Result(NodalOsDetectorRuntimeCompatibilityStatus.RunSucceeded, NodalOsDetectorRuntimeProbeTensorKind.Zero),
            Result(NodalOsDetectorRuntimeCompatibilityStatus.NativeRuntimeCrashContained, NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText)
        };

        var finding = new NodalOsDetectorRuntimeCompatibilityDecisionService().Decide(results, true, true, true, true, true);

        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForRendererFix, finding.Decision);
        Assert.IsTrue(finding.TensorSpecificCrash);
    }

    [TestMethod]
    public void Decision_MapsMetadataMismatch_ToPostprocessingFix()
    {
        var result = Result(NodalOsDetectorRuntimeCompatibilityStatus.OutputMetadataCaptured, NodalOsDetectorRuntimeProbeTensorKind.Zero) with
        {
            OutputShapes = new[] { new[] { 1, 97 } }
        };

        var finding = new NodalOsDetectorRuntimeCompatibilityDecisionService().Decide([result], true, true, true, true, true);

        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForPostProcessingFix, finding.Decision);
        Assert.IsTrue(finding.MetadataMismatch);
    }

    [TestMethod]
    public void Decision_BlocksIfParentCleanupRawModelOrAuthorityGateFails()
    {
        var result = Result(NodalOsDetectorRuntimeCompatibilityStatus.RunSucceeded, NodalOsDetectorRuntimeProbeTensorKind.Zero);
        var service = new NodalOsDetectorRuntimeCompatibilityDecisionService();

        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.NotReady, service.Decide([result], false, true, true, true, true).Decision);
        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, false, true, true, true).Decision);
        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, true, false, true, true).Decision);
        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, true, true, false, true).Decision);
        Assert.AreEqual(NodalOsDetectorRuntimeCompatibilityDecision.NotReady, service.Decide([result], true, true, true, true, false).Decision);
    }

    [TestMethod]
    public void TensorStats_InvalidShapeBlocksBeforeRuntime()
    {
        var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(
            [0f, float.NaN],
            [1, 3, 1, 1],
            "NCHW",
            "RGB");

        Assert.IsTrue(stats.HasNaN);
        Assert.AreEqual("NCHW", stats.ChannelLayout);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM225()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "detector-runtime-compatibility-experiment-m225.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m225", "detector-runtime-compatibility-experiment-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-detector-runtime-compatibility-experiment-audit-m225.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "detector-runtime-compatibility-decision-m223-m225.md")));
    }

    private static NodalOsDetectorRuntimeCompatibilityResult Result(
        NodalOsDetectorRuntimeCompatibilityStatus status,
        NodalOsDetectorRuntimeProbeTensorKind tensorKind,
        NodalOsDetectorRuntimeSessionOptionKind option = NodalOsDetectorRuntimeSessionOptionKind.Default)
    {
        var experiment = new NodalOsDetectorRuntimeExperiment(
            $"exp-{Guid.NewGuid():N}",
            tensorKind,
            NodalOsDetectorRuntimeProbeLayout.Nchw,
            [1, 3, 640, 640],
            NodalOsDetectorRuntimeCompatibilityExperimentBuilder.Option(option),
            RequiresOutOfProcessGuard: true,
            AllowInProcess: false,
            Synthetic: true,
            FullScreen: false,
            Sensitive: false,
            RawPersisted: false,
            NoAuthority: true);

        return new NodalOsDetectorRuntimeCompatibilityResult(
            $"result-{Guid.NewGuid():N}",
            experiment,
            status,
            "Microsoft.ML.OnnxRuntime 1.18.1",
            "onnxruntime.dll",
            "CPUExecutionProvider",
            "X64",
            "ch_PP-OCRv4_det.onnx",
            17,
            ["x"],
            ["sigmoid_0.tmp_0"],
            [[1, 1, 640, 640]],
            new NodalOsOnnxTensorStats([1, 3, 640, 640], 0, 1, 0.5f, false, false, "NCHW", "RGB"),
            status == NodalOsDetectorRuntimeCompatibilityStatus.NativeRuntimeCrashContained ? -1073741676 : 0,
            status == NodalOsDetectorRuntimeCompatibilityStatus.NativeRuntimeCrashContained ? "0xC0000094" : "0x00000000",
            "DetectionRun",
            SessionCreated: true,
            RunAttempted: true,
            PostProcessingReached: status == NodalOsDetectorRuntimeCompatibilityStatus.OutputMetadataCaptured,
            ParentSurvived: true,
            TempFilesCleaned: true,
            OrphanProcessLeft: false,
            RawPersisted: false,
            CallsSaas: false,
            NoAuthority: true,
            "test");
    }
}
