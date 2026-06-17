using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AlternativeRecognizer")]
[TestCategory("RecognizerFamilyReview")]
[TestCategory("AlternativeLocalOcr")]
[TestCategory("OcrEngineStrategy")]
[TestCategory("OcrFallbackStrategy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerDictionaryPairReplacement")]
[TestCategory("ExtraClassArgmax")]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("PpOcrV5Recognizer")]
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
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsAlternativeLocalOcrStrategyM268M270Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Strategy_RecommendsPpOcrV6Review_BeforeImplementation()
    {
        var strategy = new NodalOsOcrDictionaryCompatibilityService().CreateM270FinalLocalOcrEngineStrategy();

        Assert.AreEqual(NodalOsAlternativeLocalOcrFamilyDecision.ReadyForPpOcrV6Review, strategy.Decision);
        Assert.AreEqual("review-ppocrv6", strategy.RecommendedCandidate.CandidateId);
        Assert.IsTrue(strategy.CleanModelDictionaryPairPreferred);
        Assert.IsTrue(strategy.ProductiveOcrBlocked);
        Assert.IsTrue(strategy.DecodeBlocked);
        Assert.IsTrue(strategy.NoAuthority);
    }

    [TestMethod]
    public void Strategy_RanksCleanPairReview_OverHypothesisPolicy()
    {
        var candidates = new NodalOsOcrDictionaryCompatibilityService()
            .CreateM270FinalLocalOcrEngineStrategy()
            .Candidates;

        var manualPolicy = candidates.Single(candidate => candidate.CandidateId == "continue-paddleocr-manual-policy");
        var ppocrv6 = candidates.Single(candidate => candidate.CandidateId == "review-ppocrv6");

        Assert.AreEqual(NodalOsAlternativeLocalOcrFamilyCandidateStatus.RejectedHypothesisPolicy, manualPolicy.Status);
        Assert.IsTrue(manualPolicy.RequiresManualApproval);
        Assert.AreEqual(NodalOsAlternativeLocalOcrFamilyCandidateStatus.Recommended, ppocrv6.Status);
        Assert.IsFalse(ppocrv6.RequiresManualApproval);
    }

    [TestMethod]
    public void Tesseract_IsLocalFallbackOnly_NotPrimaryStrategy()
    {
        var strategy = new NodalOsOcrDictionaryCompatibilityService().CreateM270FinalLocalOcrEngineStrategy();
        var tesseract = strategy.Candidates.Single(candidate => candidate.CandidateId == "tesseract-full-fallback");

        Assert.AreEqual(NodalOsAlternativeLocalOcrFamilyCandidateStatus.FallbackOnly, tesseract.Status);
        Assert.IsTrue(strategy.FallbackStrategy.LocalOnly);
        Assert.IsTrue(strategy.FallbackStrategy.SaaSFree);
        Assert.IsTrue(strategy.FallbackStrategy.ProductiveOcrBlocked);
        Assert.IsTrue(strategy.FallbackStrategy.NoAuthority);
    }

    [TestMethod]
    public void AuditPack_IncludesCriticalEvidenceAndSecurityConstraints()
    {
        var prompt = File.ReadAllText(Path.Combine(RepoRoot, "docs", "audits", "claude-alternative-local-ocr-family-review-m268.md"));

        StringAssert.Contains(prompt, "PP-OCRv4");
        StringAssert.Contains(prompt, "Dictionary tokens `95`");
        StringAssert.Contains(prompt, "output classes `97`");
        StringAssert.Contains(prompt, "PP-OCRv5");
        StringAssert.Contains(prompt, "output classes `438`");
        StringAssert.Contains(prompt, "max probability `0.2835");
        StringAssert.Contains(prompt, "No OCR productivo");
        StringAssert.Contains(prompt, "no-authority");
    }

    [TestMethod]
    public void Reports_Artifacts_Adr_Exist_ForM268M270()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-alternative-local-ocr-family-review-m268.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "alternative-local-ocr-family-review-m268.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m268", "alternative-local-ocr-family-review-pack.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "final-local-ocr-engine-strategy-m270.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m270", "final-local-ocr-engine-strategy-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "final-local-ocr-engine-strategy-m268-m270.md")));
    }

    [TestMethod]
    public void M270Artifact_RecordsNoImplementationAndNoAuthority()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m270",
            "final-local-ocr-engine-strategy-summary.json")));

        Assert.AreEqual("M268+M269+M270 CERRADO / READY_FOR_PPOCRV6_REVIEW", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("execution").GetProperty("implementationExecuted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("execution").GetProperty("decodeAttempted").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("productiveOcrBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("noAuthority").GetBoolean());
    }
}
