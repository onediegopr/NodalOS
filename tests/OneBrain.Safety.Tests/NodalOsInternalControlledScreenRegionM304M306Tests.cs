using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InternalControlledScreenRegion")]
[TestCategory("ScreenRegionFixture")]
[TestCategory("RegionProvenance")]
[TestCategory("ScreenRegionCapture")]
[TestCategory("InternalControlledRealImage")]
[TestCategory("RealImageFixture")]
[TestCategory("FixtureProvenance")]
[TestCategory("PreprocessingAlignment")]
[TestCategory("RatioPreserving")]
[TestCategory("PipelineCalibrationAudit")]
[TestCategory("CropCalibration")]
[TestCategory("DetectorRecognizerCalibration")]
[TestCategory("DetectorModelRecovery")]
[TestCategory("SyntheticDetectorToRecognizer")]
[TestCategory("DetectorToRecognizer")]
[TestCategory("SyntheticImageRecognizer")]
[TestCategory("SyntheticCrop")]
[TestCategory("OnnxSyntheticRecognizer")]
[TestCategory("PaddleOcrSynthetic")]
[TestCategory("OfficialSpaceToken")]
[TestCategory("PaddleOcrSpaceToken")]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("ExtraClassArgmax")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerClassSemantics")]
[TestCategory("RecognizerTokenPolicy")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DictionaryCompatibility")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
public sealed class NodalOsInternalControlledScreenRegionM304M306Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void RegionProvenance_AcceptsBoundedInternalQaRegion()
    {
        var result = new NodalOsInternalControlledScreenRegionProvenanceEvaluator().Evaluate(BaseRegion());

        Assert.AreEqual(NodalOsInternalControlledScreenRegionFixtureDecision.AcceptedForOcrPipeline, result.Decision);
        Assert.IsTrue(result.AllowedForOcrPipeline);
        Assert.IsTrue(result.NoFullScreen);
        Assert.IsTrue(result.NoSensitive);
        Assert.IsTrue(result.BoundedRegion);
    }

    [TestMethod]
    public void RegionProvenance_RejectsUnsafeRegions()
    {
        var evaluator = new NodalOsInternalControlledScreenRegionProvenanceEvaluator();

        Assert.AreEqual(
            NodalOsInternalControlledScreenRegionFixtureDecision.RejectedUnknownWindow,
            evaluator.Evaluate(BaseRegion() with { SourceCategory = NodalOsInternalControlledScreenRegionSourceCategory.RejectedUnknownWindow }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledScreenRegionFixtureDecision.RejectedFullScreen,
            evaluator.Evaluate(BaseRegion() with { ContainsFullScreen = true }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledScreenRegionFixtureDecision.RejectedSensitiveWindow,
            evaluator.Evaluate(BaseRegion() with { Sensitive = true }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledScreenRegionFixtureDecision.RejectedDocumentRegion,
            evaluator.Evaluate(BaseRegion() with { ContainsDocumentData = true }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledScreenRegionFixtureDecision.RejectedCredentialData,
            evaluator.Evaluate(BaseRegion() with { ContainsCredentialOrPasswordData = true }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledScreenRegionFixtureDecision.RejectedUnboundedRegion,
            evaluator.Evaluate(BaseRegion() with { RegionBounds = new NodalOsScreenRegionBounds(0, 0, 0, 160) }).Decision);
    }

    [TestMethod]
    public void Artifact_CapturesInternalControlledScreenRegionReadiness()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m306",
            "paddleocr-internal-controlled-screen-region-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M304-M306", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_SCREEN_REGION_FIXTURE_SET_EXPANSION", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("internalControlledScreenRegionsUsed").GetBoolean());
        Assert.AreEqual("simulated-region", root.GetProperty("captureMode").GetString());
        Assert.IsTrue(root.GetProperty("unknownProvenanceRejected").GetBoolean());
        Assert.IsTrue(root.GetProperty("sensitiveRegionRejected").GetBoolean());
        Assert.IsTrue(root.GetProperty("fullScreenRejected").GetBoolean());
        Assert.IsTrue(root.GetProperty("documentRegionRejected").GetBoolean());
        Assert.IsFalse(root.GetProperty("fullScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.AreEqual("RatioPreservingRightPad", root.GetProperty("recognizerResizeMode").GetString());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.AreEqual(3, root.GetProperty("fixturesAccepted").GetInt32());
        Assert.AreEqual(0, root.GetProperty("fixturesRejected").GetInt32());
        Assert.AreEqual(3, root.GetProperty("exactMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("totalEditDistance").GetInt32());
        Assert.IsFalse(root.GetProperty("modelsCommitted").GetBoolean());
        Assert.IsFalse(root.GetProperty("dictionariesCommitted").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_RunsInternalControlledScreenRegionProbe_WhenModelsAreAvailable()
    {
        var detector = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
        var recognizer = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
        var dictionary = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
        var runner = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", "Debug", "net11.0", "OneBrain.Tools.OnnxOcrProbeRunner.dll");
        if (!File.Exists(detector) || !File.Exists(recognizer) || !File.Exists(dictionary) || !File.Exists(runner))
            Assert.Inconclusive("Detector, recognizer, dictionary, or built runner is not available locally.");

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };
        psi.ArgumentList.Add(runner);
        psi.ArgumentList.Add("--internal-controlled-screen-region-probe");
        psi.ArgumentList.Add("--repo-root");
        psi.ArgumentList.Add(RepoRoot);
        psi.ArgumentList.Add("--timeout-ms");
        psi.ArgumentList.Add("120000");

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(130000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.AreEqual("READY_FOR_SCREEN_REGION_FIXTURE_SET_EXPANSION", root.GetProperty("ReadinessDecision").GetString());
        Assert.AreEqual("simulated-region", root.GetProperty("CaptureMode").GetString());
        Assert.AreEqual(3, root.GetProperty("ExactMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("TotalEditDistance").GetInt32());
        Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
        Assert.IsFalse(root.GetProperty("FullScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("RealDocumentUsed").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_ForScreenRegionGate()
    {
        var psi = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };
        psi.ArgumentList.Add("ls-files");
        psi.ArgumentList.Add("*.onnx");

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(10000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);
        Assert.AreEqual(string.Empty, stdout.Trim());
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM306()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-internal-controlled-screen-region-fixtures-m306.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m306", "paddleocr-internal-controlled-screen-region-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-internal-screen-region-fixtures-audit-m306.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-internal-controlled-screen-region-policy-m304-m306.md")));
    }

    private static NodalOsInternalControlledScreenRegionFixtureProvenance BaseRegion() => new(
        "qa-screen-pvc-wall",
        NodalOsInternalControlledScreenRegionSourceCategory.InternalControlledScreenRegion,
        CreatedByInternalQa: true,
        WindowTitleOrSource: "NODAL OS OCR QA Simulated Region",
        ProcessOrSource: "onnx-ocr-probe-runner",
        RegionBounds: new NodalOsScreenRegionBounds(80, 64, 640, 160),
        BoundsSource: NodalOsInternalControlledScreenRegionBoundsSource.GeneratedQaFixture,
        ContainsRealPersonData: false,
        ContainsCustomerData: false,
        ContainsFinancialData: false,
        ContainsDocumentData: false,
        ContainsCredentialOrPasswordData: false,
        ContainsFullScreen: false,
        Sensitive: false,
        ExpectedText: "PVC WALL",
        Reason: "internal QA bounded simulated-region fixture");
}
