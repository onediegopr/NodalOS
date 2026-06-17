using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsDetectorModelRecoveryPipelineM289M291Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Artifact_CapturesDetectorRecovery_AndDecodeEvidenceBlock()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m291",
            "paddleocr-detector-model-recovery-pipeline-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M289-M291", root.GetProperty("milestone").GetString());
        Assert.AreEqual("BLOCKED_BY_SYNTHETIC_PIPELINE_DECODE_EVIDENCE", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("detectorModelRecovered").GetBoolean());
        Assert.IsTrue(root.GetProperty("detectorModelVerified").GetBoolean());
        Assert.IsFalse(root.GetProperty("detectorModelCommitted").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerModelCommitted").GetBoolean());
        Assert.IsFalse(root.GetProperty("dictionaryCommitted").GetBoolean());
        Assert.IsTrue(root.GetProperty("detectorProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("detectorProbeSucceeded").GetBoolean());
        Assert.AreEqual(5, root.GetProperty("detectedBoxesTotal").GetInt32());
        Assert.IsTrue(root.GetProperty("recognizerProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("recognizerProbeSucceeded").GetBoolean());
        Assert.AreEqual(5, root.GetProperty("recognizedCropsTotal").GetInt32());
        Assert.AreEqual(0, root.GetProperty("exactMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("normalizedMatches").GetInt32());
        Assert.AreEqual(5, root.GetProperty("mismatches").GetInt32());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.AreEqual(0, root.GetProperty("blankIndex").GetInt32());
        Assert.AreEqual(437, root.GetProperty("spaceIndex").GetInt32());
        Assert.AreEqual("[B,T,C]", root.GetProperty("recognizerOutputLayout").GetString());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.IsTrue(root.GetProperty("outOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("parentSurvived").GetBoolean());
        Assert.IsFalse(root.GetProperty("publicProductReady").GetBoolean());
        Assert.IsTrue(root.GetProperty("syntheticImagesOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("realScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_RunsDetectorAndRecognizer_WhenRecoveredModelsAreAvailable()
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
        psi.ArgumentList.Add("--synthetic-detector-to-recognizer-pipeline-probe");
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
        Assert.IsTrue(root.GetProperty("DetectorAvailable").GetBoolean());
        Assert.IsTrue(root.GetProperty("DetectorProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("DetectorProbeSucceeded").GetBoolean());
        Assert.AreEqual(5, root.GetProperty("DetectedBoxesTotal").GetInt32());
        Assert.IsTrue(root.GetProperty("RecognizerProbeSucceeded").GetBoolean());
        Assert.AreEqual("BLOCKED_BY_SYNTHETIC_PIPELINE_DECODE_EVIDENCE", root.GetProperty("ReadinessDecision").GetString());
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM291()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-detector-model-recovery-and-pipeline-retry-m291.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m291", "paddleocr-detector-model-recovery-pipeline-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-detector-model-recovery-pipeline-audit-m291.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-detector-model-recovery-policy-m289-m291.md")));
    }
}
