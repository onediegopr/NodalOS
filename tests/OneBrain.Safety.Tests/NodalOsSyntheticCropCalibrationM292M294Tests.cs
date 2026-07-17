using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsSyntheticCropCalibrationM292M294Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Artifact_CapturesSyntheticCropCalibration_ReadinessAndSafety()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m294",
            "paddleocr-synthetic-detector-recognizer-crop-calibration-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M292-M294", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_SYNTHETIC_PIPELINE_CALIBRATION_AUDIT", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("detectorModelAvailable").GetBoolean());
        Assert.IsTrue(root.GetProperty("detectorModelVerified").GetBoolean());
        Assert.IsTrue(root.GetProperty("recognizerModelAvailable").GetBoolean());
        Assert.IsTrue(root.GetProperty("dictionaryAvailable").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.AreEqual(0, root.GetProperty("blankIndex").GetInt32());
        Assert.AreEqual(437, root.GetProperty("spaceIndex").GetInt32());
        Assert.AreEqual("[B,T,C]", root.GetProperty("recognizerOutputLayout").GetString());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.IsTrue(root.GetProperty("outOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("parentSurvived").GetBoolean());
        Assert.AreEqual(45, root.GetProperty("calibrationAttempts").GetInt32());
        Assert.AreEqual("percent-expanded-box", root.GetProperty("bestCropStrategy").GetString());
        Assert.AreEqual("10%", root.GetProperty("bestMarginPolicy").GetString());
        Assert.AreEqual("1.5", root.GetProperty("bestUnclipPolicy").GetString());
        Assert.AreEqual(0, root.GetProperty("baselineExactMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("baselineNormalizedMatches").GetInt32());
        Assert.AreEqual(0, root.GetProperty("calibratedExactMatches").GetInt32());
        Assert.AreEqual(3, root.GetProperty("calibratedNormalizedMatches").GetInt32());
        Assert.AreEqual(1, root.GetProperty("distinctMatchedFixtures").GetInt32());
        Assert.IsTrue(root.GetProperty("improvedOverBaseline").GetBoolean());
        Assert.IsFalse(root.GetProperty("publicProductReady").GetBoolean());
        Assert.IsFalse(root.GetProperty("realScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawPersistenceOfRealData").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_RunsCalibrationMatrix_WhenModelsAreAvailable()
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
        psi.ArgumentList.Add("--synthetic-detector-recognizer-crop-calibration-probe");
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
        Assert.AreEqual("READY_FOR_SYNTHETIC_PIPELINE_CALIBRATION_AUDIT", root.GetProperty("ReadinessDecision").GetString());
        Assert.AreEqual(45, root.GetProperty("CalibrationAttempts").GetInt32());
        Assert.AreEqual(3, root.GetProperty("CalibratedNormalizedMatches").GetInt32());
        Assert.AreEqual(1, root.GetProperty("DistinctMatchedFixtures").GetInt32());
        Assert.IsTrue(root.GetProperty("ImprovedOverBaseline").GetBoolean());
        Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
        Assert.IsFalse(root.GetProperty("PublicProductReady").GetBoolean());
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM294()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-synthetic-detector-recognizer-crop-calibration-m294.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m294", "paddleocr-synthetic-detector-recognizer-crop-calibration-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-synthetic-crop-calibration-audit-m294.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-detector-recognizer-crop-calibration-policy-m292-m294.md")));
    }
}
