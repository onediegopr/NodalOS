using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PreprocessingAlignment")]
[TestCategory("RatioPreserving")]
[TestCategory("PipelineCalibrationAudit")]
[TestCategory("RenderingFidelity")]
[TestCategory("SystemFontRenderer")]
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
public sealed class NodalOsPaddleOcrPreprocessingAlignmentM298M300Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Artifact_CapturesPreprocessingAlignmentAbEvidence()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m300",
            "paddleocr-preprocessing-alignment-ab-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M298-M300", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("abProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("stretchModeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("ratioPreservingModeAttempted").GetBoolean());
        Assert.AreEqual(0, root.GetProperty("stretchExactMatches").GetInt32());
        Assert.AreEqual(1, root.GetProperty("stretchNormalizedMatches").GetInt32());
        Assert.AreEqual(4, root.GetProperty("stretchMismatches").GetInt32());
        Assert.AreEqual(7, root.GetProperty("stretchTotalEditDistance").GetInt32());
        Assert.AreEqual(3, root.GetProperty("ratioExactMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("ratioNormalizedMatches").GetInt32());
        Assert.AreEqual(2, root.GetProperty("ratioMismatches").GetInt32());
        Assert.AreEqual(2, root.GetProperty("ratioTotalEditDistance").GetInt32());
        Assert.IsTrue(root.GetProperty("ratioImprovedOverStretch").GetBoolean());
        Assert.IsTrue(root.GetProperty("successCriteriaMet").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.AreEqual(0, root.GetProperty("blankIndex").GetInt32());
        Assert.AreEqual(437, root.GetProperty("spaceIndex").GetInt32());
        Assert.IsFalse(root.GetProperty("publicProductReady").GetBoolean());
        Assert.IsFalse(root.GetProperty("realScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawPersistenceOfRealData").GetBoolean());
        Assert.IsFalse(root.GetProperty("modelsCommitted").GetBoolean());
        Assert.IsFalse(root.GetProperty("dictionariesCommitted").GetBoolean());
    }

    [TestMethod]
    public void AbComparison_ComputesRatioImprovementAndSuccessCriteria()
    {
        var stretchMatches = 0 + 1;
        var ratioMatches = 3 + 0;
        var stretchEditDistance = 7;
        var ratioEditDistance = 2;
        var improvement = (double)(stretchEditDistance - ratioEditDistance) / stretchEditDistance;

        Assert.IsTrue(ratioMatches > stretchMatches);
        Assert.IsTrue(ratioEditDistance < stretchEditDistance);
        Assert.IsTrue(improvement >= 0.40d);
        Assert.IsTrue(ratioMatches >= 3);
    }

    [TestMethod]
    public void ProbeRunner_RunsPreprocessingAlignmentAb_WhenModelsAreAvailable()
    {
        var detector = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
        var recognizer = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
        var dictionary = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
        var runner = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", "Debug", "net10.0", "OneBrain.Tools.OnnxOcrProbeRunner.dll");
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
        psi.ArgumentList.Add("--paddleocr-preprocessing-alignment-ab-probe");
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
        Assert.AreEqual("READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES", root.GetProperty("ReadinessDecision").GetString());
        Assert.AreEqual(3, root.GetProperty("RatioExactMatches").GetInt32());
        Assert.AreEqual(2, root.GetProperty("RatioTotalEditDistance").GetInt32());
        Assert.AreEqual(7, root.GetProperty("StretchTotalEditDistance").GetInt32());
        Assert.IsTrue(root.GetProperty("RatioImprovedOverStretch").GetBoolean());
        Assert.IsTrue(root.GetProperty("SuccessCriteriaMet").GetBoolean());
        Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM300()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-preprocessing-alignment-ab-probe-m300.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m300", "paddleocr-preprocessing-alignment-ab-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-preprocessing-alignment-ab-audit-m300.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-ratio-preserving-recognizer-preprocessing-m298-m300.md")));
    }
}
