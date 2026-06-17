using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerDictionaryPairReplacement")]
[TestCategory("RecognizerPairAcquisition")]
[TestCategory("PpOcrV5Recognizer")]
[TestCategory("DictionarySourceReconciliation")]
[TestCategory("RecognizerClassSemantics")]
[TestCategory("RecognizerTokenPolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DictionaryCompatibility")]
[TestCategory("DictionaryAcquisition")]
[TestCategory("DictionarySourceSelection")]
[TestCategory("PaddleOcrDictionary")]
[TestCategory("RecognizerRuntimeExperiment")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("FullOcrHandoff")]
[TestCategory("DetectorRuntimeCompatibility")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsPpOcrV5RecognizerPairAcquisitionM253M255Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Manifest_ContainsPpOcrV5RecognizerCandidate_AndDictionary()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-recognizer-pair-candidates-manifest.json")));
        var pair = doc.RootElement.GetProperty("pairs")[0];

        Assert.AreEqual("rapidocr-modelscope-ppocrv5-en-mobile-onnx", pair.GetProperty("pairId").GetString());
        Assert.AreEqual("en_PP-OCRv5_rec_mobile.onnx", pair.GetProperty("recognizerModel").GetProperty("fileName").GetString());
        Assert.AreEqual("ppocrv5_en_dict.txt", pair.GetProperty("dictionary").GetProperty("fileName").GetString());
        Assert.AreEqual(7872351, pair.GetProperty("recognizerModel").GetProperty("fileSizeBytes").GetInt32());
        Assert.AreEqual(1416, pair.GetProperty("dictionary").GetProperty("fileSizeBytes").GetInt32());
    }

    [TestMethod]
    public void DictionaryCount436PlusBlank_MapsToExpected437_ButRuntimeObserved438BlocksDecode()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-recognizer-pair-candidates-manifest.json")));
        var pair = doc.RootElement.GetProperty("pairs")[0];
        var dictionary = pair.GetProperty("dictionary");
        var recognizer = pair.GetProperty("recognizerModel");

        Assert.AreEqual(436, dictionary.GetProperty("tokenCount").GetInt32());
        Assert.AreEqual(0, dictionary.GetProperty("ctcBlankIndex").GetInt32());
        Assert.AreEqual(437, dictionary.GetProperty("expectedClassCount").GetInt32());
        Assert.AreEqual(437, recognizer.GetProperty("expectedOutputClassCount").GetInt32());
        Assert.AreEqual(438, recognizer.GetProperty("runtimeObservedOutputClassCount").GetInt32());
    }

    [TestMethod]
    public void AcquisitionScripts_Exist_AndRollbackDoesNotTargetCurrentModels()
    {
        var scriptDir = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx");

        Assert.IsTrue(File.Exists(Path.Combine(scriptDir, "download-recognizer-pair.ps1")));
        Assert.IsTrue(File.Exists(Path.Combine(scriptDir, "verify-recognizer-pair.ps1")));
        Assert.IsTrue(File.Exists(Path.Combine(scriptDir, "rollback-recognizer-pair.ps1")));

        var rollback = File.ReadAllText(Path.Combine(scriptDir, "rollback-recognizer-pair.ps1"));
        StringAssert.Contains(rollback, "ch_PP-OCRv4_det.onnx");
        StringAssert.Contains(rollback, "ch_PP-OCRv4_rec.onnx");
        StringAssert.Contains(rollback, "ch_ppocr_mobile_v2.0_cls.onnx");
        StringAssert.Contains(rollback, "Refusing to delete protected current model path");
    }

    [TestMethod]
    public void RuntimeClassMismatch_BlocksProductiveOcr_ShadowMode_AndDecode()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m255", "ppocrv5-recognizer-pair-acquisition-summary.json")));

        Assert.AreEqual("M253+M254+M255 PARCIAL", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("decode").GetProperty("attempted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("decode").GetProperty("allowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("productiveOcrBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("shadowModeBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("noAuthority").GetBoolean());
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM255()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "ppocrv5-recognizer-pair-acquisition-m255.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m255", "ppocrv5-recognizer-pair-acquisition-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-ppocrv5-recognizer-pair-acquisition-audit-m255.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "ppocrv5-recognizer-pair-acquisition-m253-m255.md")));
    }
}
