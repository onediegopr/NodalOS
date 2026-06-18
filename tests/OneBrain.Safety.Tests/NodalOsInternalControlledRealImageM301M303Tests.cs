using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsInternalControlledRealImageM301M303Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Provenance_AcceptsInternalControlledNonSensitiveFixture()
    {
        var result = new NodalOsInternalControlledRealImageFixtureProvenanceEvaluator().Evaluate(new(
            "qa-real-pvc-wall",
            "qa-real-pvc-wall.generated-rgba",
            NodalOsInternalControlledRealImageSourceCategory.InternalControlledRealImage,
            CreatedByInternalQa: true,
            ContainsRealPersonData: false,
            ContainsCustomerData: false,
            ContainsFinancialData: false,
            ContainsDocumentData: false,
            ContainsScreenCapture: false,
            Sensitive: false,
            ExpectedText: "PVC WALL",
            "internal QA non-sensitive fixture"));

        Assert.AreEqual(NodalOsInternalControlledRealImageFixtureDecision.AcceptedForOcrPipeline, result.Decision);
        Assert.IsTrue(result.AllowedForOcrPipeline);
        Assert.IsTrue(result.NoSensitive);
        Assert.IsTrue(result.NoScreenCapture);
        Assert.IsTrue(result.NoDocumentData);
    }

    [TestMethod]
    public void Provenance_RejectsUnknownSensitiveScreenAndDocumentFixtures()
    {
        var evaluator = new NodalOsInternalControlledRealImageFixtureProvenanceEvaluator();

        Assert.AreEqual(
            NodalOsInternalControlledRealImageFixtureDecision.RejectedUnknownProvenance,
            evaluator.Evaluate(BaseFixture() with
            {
                SourceCategory = NodalOsInternalControlledRealImageSourceCategory.RejectedUnknownProvenance
            }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledRealImageFixtureDecision.RejectedSensitive,
            evaluator.Evaluate(BaseFixture() with { Sensitive = true }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledRealImageFixtureDecision.RejectedScreenCapture,
            evaluator.Evaluate(BaseFixture() with { ContainsScreenCapture = true }).Decision);

        Assert.AreEqual(
            NodalOsInternalControlledRealImageFixtureDecision.RejectedDocumentFixture,
            evaluator.Evaluate(BaseFixture() with { ContainsDocumentData = true }).Decision);
    }

    [TestMethod]
    public void Artifact_CapturesInternalControlledRealImageReadiness()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m303",
            "paddleocr-internal-controlled-real-image-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M301-M303", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_INTERNAL_CONTROLLED_SCREEN_REGION_FIXTURES", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("internalControlledRealImagesUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("unknownProvenanceRejected").GetBoolean());
        Assert.IsTrue(root.GetProperty("sensitiveFixtureRejected").GetBoolean());
        Assert.IsFalse(root.GetProperty("realScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("detectorModelVerified").GetBoolean());
        Assert.AreEqual("RatioPreservingRightPad", root.GetProperty("recognizerResizeMode").GetString());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.AreEqual(3, root.GetProperty("fixturesAccepted").GetInt32());
        Assert.AreEqual(0, root.GetProperty("fixturesRejected").GetInt32());
        Assert.AreEqual(3, root.GetProperty("exactMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("totalEditDistance").GetInt32());
        Assert.IsTrue(root.GetProperty("successCriteriaMet").GetBoolean());
        Assert.IsFalse(root.GetProperty("modelsCommitted").GetBoolean());
        Assert.IsFalse(root.GetProperty("dictionariesCommitted").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_RunsInternalControlledRealImageProbe_WhenModelsAreAvailable()
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
        psi.ArgumentList.Add("--internal-controlled-real-image-probe");
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
        Assert.AreEqual("READY_FOR_INTERNAL_CONTROLLED_SCREEN_REGION_FIXTURES", root.GetProperty("ReadinessDecision").GetString());
        Assert.AreEqual(3, root.GetProperty("ExactMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("TotalEditDistance").GetInt32());
        Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
        Assert.IsFalse(root.GetProperty("RealScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("RealDocumentUsed").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels()
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
    public void Report_Artifact_Audit_Adr_Exist_ForM303()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-internal-controlled-real-image-fixtures-m303.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m303", "paddleocr-internal-controlled-real-image-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-internal-real-image-fixtures-audit-m303.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-internal-controlled-real-image-policy-m301-m303.md")));
    }

    private static NodalOsInternalControlledRealImageFixtureProvenance BaseFixture() => new(
        "qa-real-test",
        "qa-real-test.generated-rgba",
        NodalOsInternalControlledRealImageSourceCategory.InternalControlledRealImage,
        CreatedByInternalQa: true,
        ContainsRealPersonData: false,
        ContainsCustomerData: false,
        ContainsFinancialData: false,
        ContainsDocumentData: false,
        ContainsScreenCapture: false,
        Sensitive: false,
        ExpectedText: "ROMA",
        "internal QA test fixture");
}
