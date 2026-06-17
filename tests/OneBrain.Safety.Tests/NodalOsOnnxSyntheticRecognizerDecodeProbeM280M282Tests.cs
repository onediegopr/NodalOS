using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsOnnxSyntheticRecognizerDecodeProbeM280M282Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Artifact_CapturesPpOcrV5SyntheticRecognizerProbe_AndSafetyGates()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m282",
            "paddleocr-onnx-synthetic-recognizer-probe-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M280-M282", root.GetProperty("milestone").GetString());
        Assert.AreEqual("READY_FOR_SYNTHETIC_IMAGE_RECOGNIZER_CROP_FIXTURES", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("outOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("parentSurvived").GetBoolean());
        Assert.IsTrue(root.GetProperty("onnxProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("onnxProbeSucceeded").GetBoolean());
        Assert.IsTrue(root.GetProperty("productiveOcrBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("shadowModeBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("noAuthority").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRawPersistence").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawTensorPersisted").GetBoolean());
        Assert.IsFalse(root.GetProperty("realImageUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());

        var ppocrv5 = root.GetProperty("probes")[0];
        Assert.AreEqual("ppocrv5-en-rec-candidate", ppocrv5.GetProperty("modelId").GetString());
        CollectionAssert.AreEqual(new[] { 1, 40, 438 }, ppocrv5.GetProperty("outputShape").EnumerateArray().Select(e => e.GetInt32()).ToArray());
        Assert.AreEqual(438, ppocrv5.GetProperty("outputClassCount").GetInt32());
        Assert.AreEqual(438, ppocrv5.GetProperty("expectedClassCount").GetInt32());
        Assert.AreEqual(436, ppocrv5.GetProperty("dictionaryTokenCount").GetInt32());
        Assert.AreEqual(0, ppocrv5.GetProperty("blankIndex").GetInt32());
        Assert.AreEqual(437, ppocrv5.GetProperty("spaceIndex").GetInt32());
        Assert.IsTrue(ppocrv5.GetProperty("softmaxEvidence").GetProperty("looksLikeSoftmax").GetBoolean());
        Assert.IsFalse(root.GetProperty("softmaxReapplied").GetBoolean());
        Assert.IsFalse(ppocrv5.GetProperty("usefulOcrClaimed").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_UsesOutOfProcessGuard_WhenCandidateModelIsAvailable()
    {
        var model = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
        var dictionary = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
        var runner = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", "Debug", "net11.0", "OneBrain.Tools.OnnxOcrProbeRunner.dll");
        if (!File.Exists(model) || !File.Exists(dictionary) || !File.Exists(runner))
            Assert.Inconclusive("PP-OCRv5 recognizer candidate or built probe runner is not available locally.");

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };
        psi.ArgumentList.Add(runner);
        psi.ArgumentList.Add("--onnx-synthetic-recognizer-decode-probe");
        psi.ArgumentList.Add("--repo-root");
        psi.ArgumentList.Add(RepoRoot);
        psi.ArgumentList.Add("--timeout-ms");
        psi.ArgumentList.Add("60000");

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(70000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("OutOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
        Assert.IsTrue(root.GetProperty("OnnxProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("OnnxProbeSucceeded").GetBoolean());
        Assert.IsTrue(root.GetProperty("NoAuthority").GetBoolean());
        Assert.IsTrue(root.GetProperty("NoRawPersistence").GetBoolean());
        Assert.IsFalse(root.GetProperty("SoftmaxReapplied").GetBoolean());

        var result = root.GetProperty("Results").EnumerateArray()
            .First(e => e.GetProperty("ModelId").GetString() == "ppocrv5-en-rec-candidate");
        Assert.AreEqual("Passed", result.GetProperty("Status").GetString());
        Assert.AreEqual("NoCrash", result.GetProperty("CrashKind").GetString());
        Assert.IsTrue(result.GetProperty("ParentSurvived").GetBoolean());
        Assert.IsFalse(result.GetProperty("RawPersisted").GetBoolean());
        Assert.IsFalse(result.GetProperty("CallsSaas").GetBoolean());
        Assert.AreEqual(438, result.GetProperty("ChildSummary").GetProperty("OutputClassCount").GetInt32());
        CollectionAssert.AreEqual(
            new[] { 1, 40, 438 },
            result.GetProperty("ChildSummary").GetProperty("OutputShape").EnumerateArray().Select(e => e.GetInt32()).ToArray());
        Assert.IsTrue(result.GetProperty("ChildSummary").GetProperty("SoftmaxEvidence").GetProperty("LooksLikeSoftmax").GetBoolean());
        Assert.IsFalse(result.GetProperty("ChildSummary").GetProperty("UsefulOcrClaimed").GetBoolean());
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM282()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-onnx-synthetic-recognizer-decode-probe-m282.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m282", "paddleocr-onnx-synthetic-recognizer-probe-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-onnx-synthetic-recognizer-probe-audit-m282.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-onnx-synthetic-recognizer-probe-policy-m280-m282.md")));
    }
}
