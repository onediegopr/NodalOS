using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsSyntheticImageRecognizerCropM283M285Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Artifact_CapturesSyntheticCropProbe_AndSafetyGates()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m285",
            "paddleocr-synthetic-image-recognizer-crop-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M283-M285", root.GetProperty("milestone").GetString());
        Assert.IsTrue(root.GetProperty("productiveOcrBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("shadowModeBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("noAuthority").GetBoolean());
        Assert.IsTrue(root.GetProperty("noSaaS").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRawPersistenceOfRealData").GetBoolean());
        Assert.IsTrue(root.GetProperty("syntheticImagesOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("realImageUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realScreenUsed").GetBoolean());
        Assert.IsFalse(root.GetProperty("realDocumentUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("officialSpacePolicy").GetBoolean());
        Assert.AreEqual(0, root.GetProperty("blankIndex").GetInt32());
        Assert.AreEqual(437, root.GetProperty("spaceIndex").GetInt32());
        Assert.AreEqual("[B,T,C]", root.GetProperty("outputLayout").GetString());
        Assert.IsFalse(root.GetProperty("softmaxReapplied").GetBoolean());
        Assert.IsTrue(root.GetProperty("outOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("parentSurvived").GetBoolean());
        Assert.IsTrue(root.GetProperty("onnxProbeAttempted").GetBoolean());
        Assert.AreEqual(438, root.GetProperty("ppOcrV5ExpectedClasses").GetInt32());
        Assert.AreEqual(7, root.GetProperty("syntheticCropFixturesCount").GetInt32());
        Assert.IsFalse(root.GetProperty("modelsCommitted").GetBoolean());
        Assert.IsFalse(root.GetProperty("dictionariesCommitted").GetBoolean());
    }

    [TestMethod]
    public void ProbeRunner_GeneratesSyntheticCrops_AndRunsOutOfProcess_WhenModelIsAvailable()
    {
        var model = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
        var dictionary = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
        var runner = Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "bin", "Debug", "net10.0", "OneBrain.Tools.OnnxOcrProbeRunner.dll");
        if (!File.Exists(model) || !File.Exists(dictionary) || !File.Exists(runner))
            Assert.Inconclusive("PP-OCRv5 recognizer candidate, dictionary, or built probe runner is not available locally.");

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };
        psi.ArgumentList.Add(runner);
        psi.ArgumentList.Add("--synthetic-image-recognizer-crop-probe");
        psi.ArgumentList.Add("--repo-root");
        psi.ArgumentList.Add(RepoRoot);
        psi.ArgumentList.Add("--timeout-ms");
        psi.ArgumentList.Add("90000");

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(100000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.AreEqual("M283-M285", root.GetProperty("Milestone").GetString());
        Assert.IsTrue(root.GetProperty("OutOfProcessGuardUsed").GetBoolean());
        Assert.IsTrue(root.GetProperty("ParentSurvived").GetBoolean());
        Assert.IsTrue(root.GetProperty("OnnxProbeAttempted").GetBoolean());
        Assert.IsTrue(root.GetProperty("OnnxProbeSucceeded").GetBoolean());
        Assert.IsTrue(root.GetProperty("SyntheticImagesOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("SoftmaxReapplied").GetBoolean());
        Assert.IsFalse(root.GetProperty("SyntheticCropsPersisted").GetBoolean());
        Assert.AreEqual(7, root.GetProperty("SyntheticCropFixturesCount").GetInt32());

        var fixtures = root.GetProperty("Fixtures").EnumerateArray().ToArray();
        Assert.AreEqual(7, fixtures.Length);
        foreach (var fixture in fixtures)
        {
            Assert.AreEqual("Passed", fixture.GetProperty("Status").GetString());
            Assert.AreEqual("NoCrash", fixture.GetProperty("CrashKind").GetString());
            Assert.IsTrue(fixture.GetProperty("ParentSurvived").GetBoolean());
            var child = fixture.GetProperty("ChildSummary");
            Assert.AreEqual("[B,T,C]", child.GetProperty("OutputLayout").GetString());
            Assert.AreEqual(438, child.GetProperty("OutputClassCount").GetInt32());
            Assert.IsTrue(child.GetProperty("OfficialSpacePolicy").GetBoolean());
            Assert.IsTrue(child.GetProperty("SoftmaxEvidence").GetProperty("LooksLikeSoftmax").GetBoolean());
            Assert.IsFalse(child.GetProperty("SoftmaxReapplied").GetBoolean());
            Assert.IsFalse(child.GetProperty("UsefulOcrClaimed").GetBoolean());
            Assert.IsFalse(child.GetProperty("RawTensorPersisted").GetBoolean());
            Assert.IsTrue(child.GetProperty("SyntheticImagesOnly").GetBoolean());
            Assert.IsFalse(child.GetProperty("RealImageUsed").GetBoolean());
        }
    }

    [TestMethod]
    public void Report_Artifact_Audit_Adr_Exist_ForM285()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-synthetic-image-recognizer-crop-fixtures-m285.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m285", "paddleocr-synthetic-image-recognizer-crop-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "paddleocr-synthetic-image-recognizer-crop-audit-m285.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-synthetic-image-recognizer-crop-policy-m283-m285.md")));
    }
}
