using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("DecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerDictionaryPairReplacement")]
[TestCategory("RecognizerPairAcquisition")]
[TestCategory("PpOcrV5Recognizer")]
[TestCategory("RecognizerClassSemantics")]
[TestCategory("RecognizerTokenPolicy")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DictionaryCompatibility")]
[TestCategory("RecognizerRuntimeExperiment")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("FullOcrHandoff")]
[TestCategory("DetectorRuntimeCompatibility")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOcrRecognizerReplacementStrategyM259M261Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void StrategyMatrix_RecommendsClaudeAudit_BeforeManualDecodeApproval()
    {
        var matrix = new NodalOsOcrDictionaryCompatibilityService().CreateRecognizerReplacementStrategyMatrix();

        Assert.AreEqual(NodalOsOcrRecognizerReplacementDecision.ReadyForClaudeExtraClassAudit, matrix.Decision);
        Assert.AreEqual("claude-extra-class-deep-audit", matrix.RecommendedStrategy.StrategyId);
        Assert.AreEqual(1, matrix.RecommendedStrategy.Rank);
        Assert.IsTrue(matrix.ExtraClassUnresolved);
        Assert.IsTrue(matrix.DecodeBlocked);
        Assert.IsTrue(matrix.ProductiveOcrBlocked);
        Assert.IsTrue(matrix.ShadowModeBlocked);
        Assert.IsTrue(matrix.NoAuthority);
    }

    [TestMethod]
    public void ManualPolicyStrategy_DoesNotAutoApproveDecode()
    {
        var manual = new NodalOsOcrDictionaryCompatibilityService()
            .CreateRecognizerReplacementStrategyMatrix()
            .Strategies
            .Single(strategy => strategy.StrategyId == "manual-extra-class-policy-approval");

        Assert.AreEqual(NodalOsOcrRecognizerReplacementStrategyStatus.ViableNeedsApproval, manual.Status);
        Assert.IsTrue(manual.RequiresManualApproval);
        Assert.IsFalse(manual.DecodeAutoApproved);
        Assert.IsTrue(manual.ProductiveOcrBlocked);
        Assert.IsTrue(manual.NoAuthority);
    }

    [TestMethod]
    public void ReplacementStrategy_RanksExplicitPairSearch_OverAlternativeFamilyAndTesseract()
    {
        var strategies = new NodalOsOcrDictionaryCompatibilityService()
            .CreateRecognizerReplacementStrategyMatrix()
            .Strategies;

        var replacement = strategies.Single(strategy => strategy.StrategyId == "recognizer-model-replacement-search");
        var alternative = strategies.Single(strategy => strategy.StrategyId == "alternative-local-ocr-family-review");
        var tesseract = strategies.Single(strategy => strategy.StrategyId == "tesseract-local-fallback");

        Assert.IsTrue(replacement.Rank < alternative.Rank);
        Assert.IsTrue(alternative.Rank < tesseract.Rank);
        Assert.IsTrue(replacement.Candidate.DictionaryExplicit);
        Assert.IsTrue(replacement.Candidate.ClassCountClear);
        Assert.IsTrue(replacement.Candidate.NoSaas);
    }

    [TestMethod]
    public void StrategyMatrix_PreservesNoAuthority_AndBlocksProductiveOcr()
    {
        var matrix = new NodalOsOcrDictionaryCompatibilityService().CreateRecognizerReplacementStrategyMatrix();

        Assert.IsTrue(matrix.Strategies.All(strategy => strategy.NoAuthority));
        Assert.IsTrue(matrix.Strategies.All(strategy => strategy.ProductiveOcrBlocked));
        Assert.IsTrue(matrix.Strategies.All(strategy => strategy.ShadowModeBlocked));
        Assert.IsTrue(matrix.Strategies.All(strategy => !strategy.DecodeAutoApproved));
    }

    [TestMethod]
    public void DeepAuditPack_IncludesV4V5Evidence_AndExplicitRecommendationQuestion()
    {
        var prompt = File.ReadAllText(Path.Combine(RepoRoot, "docs", "audits", "claude-paddleocr-extra-class-semantics-deep-audit-m259.md"));

        StringAssert.Contains(prompt, "PP-OCRv4");
        StringAssert.Contains(prompt, "PP-OCRv5");
        StringAssert.Contains(prompt, "dictionary tokens = 95");
        StringAssert.Contains(prompt, "dictionary tokens = 436");
        StringAssert.Contains(prompt, "Do not invent tokens");
        StringAssert.Contains(prompt, "Should NODAL OS approve a policy or replace the model pair?");
    }

    [TestMethod]
    public void Reports_Artifacts_Adr_Exist_ForM259M261()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-paddleocr-extra-class-semantics-deep-audit-m259.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m259", "paddleocr-extra-class-deep-audit-pack.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-extra-class-deep-audit-m259.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "ocr-recognizer-replacement-strategy-m261.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m261", "ocr-recognizer-replacement-strategy-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "ocr-recognizer-extra-class-and-replacement-decision-m259-m261.md")));
    }

    [TestMethod]
    public void M261Artifact_RecordsClaudeAuditDecision_AndBlocksDecode()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m261", "ocr-recognizer-replacement-strategy-summary.json")));

        Assert.AreEqual("M259+M260+M261 CERRADO / READY_FOR_CLAUDE_EXTRA_CLASS_AUDIT", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("productiveOcrBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("shadowModeBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("decodeBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("noAuthority").GetBoolean());
    }
}
