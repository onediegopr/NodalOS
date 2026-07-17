using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsSyntheticDetectorToRecognizerPipelineM286M288Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Artifact_CapturesAvailabilityBlock_AndSafetyGates()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m288",
            "paddleocr-synthetic-detector-to-recognizer-pipeline-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M286-M288", root.GetProperty("milestone").GetString());
        Assert.AreEqual("BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("internalDevelopmentOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("publicProductReady").GetBoolean());
        Assert.IsTrue(root.GetProperty("noSaaS").GetBoolean());
        Assert.IsTrue(root.GetProperty("noApiKeys").GetBoolean());
        Assert.IsTrue(root.GetProperty("noSensitive").GetBoolean());
        Assert.IsFalse(root.GetProperty("realScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("syntheticImagesOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawPersistenceOfRealData").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.AreEqual(0, root.GetProperty("blankIndex").GetInt32());
        Assert.AreEqual(437, root.GetProperty("spaceIndex").GetInt32());
        Assert.AreEqual("[B,T,C]", root.GetProperty("recognizerOutputLayout").GetString());
        Assert.IsFalse(root.GetProperty("recognizerSoftmaxReapplied").GetBoolean());
        Assert.IsFalse(root.GetProperty("detectorProbeAttempted").GetBoolean());
        Assert.IsFalse(root.GetProperty("detectorProbeSucceeded").GetBoolean());
        Assert.IsFalse(root.GetProperty("recognizerProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("parentSurvived").GetBoolean());
        Assert.AreEqual(5, root.GetProperty("syntheticFullImageFixturesCount").GetInt32());
        Assert.AreEqual(0, root.GetProperty("detectedBoxesTotal").GetInt32());
        Assert.IsFalse(root.GetProperty("modelsCommitted").GetBoolean());
        Assert.IsFalse(root.GetProperty("dictionariesCommitted").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_BlocksBeforeRuntime_WhenDetectorModelIsMissing()
    {
        var runner = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", "Debug", "net10.0", "OneBrain.Tools.OnnxOcrProbeRunner.dll");
        if (!File.Exists(runner))
            Assert.Inconclusive("Built probe runner is not available locally.");

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
        Assert.AreEqual("M286-M288", root.GetProperty("Milestone").GetString());
        if (!root.GetProperty("DetectorAvailable").GetBoolean())
        {
            Assert.AreEqual("BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY", root.GetProperty("ReadinessDecision").GetString());
            Assert.IsFalse(root.GetProperty("DetectorProbeAttempted").GetBoolean());
            Assert.IsFalse(root.GetProperty("OutOfProcessGuardUsed").GetBoolean());
            Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
            Assert.AreEqual(0, root.GetProperty("DetectedBoxesTotal").GetInt32());
        }
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM288()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-synthetic-detector-to-recognizer-pipeline-m288.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m288", "paddleocr-synthetic-detector-to-recognizer-pipeline-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-synthetic-detector-recognizer-pipeline-audit-m288.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-synthetic-detector-recognizer-pipeline-policy-m286-m288.md")));
    }
}
