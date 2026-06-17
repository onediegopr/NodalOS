using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AlternativeRecognizer")]
[TestCategory("RecognizerFamilyReview")]
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
public sealed class NodalOsAlternativeRecognizerFamilyReviewM265M267Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void PpOcrV5English_IsRejectedBecauseExtraClassProbabilityWasNonTrivial()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidate = service.CreateM265AlternativeRecognizerCandidates()
            .Single(c => c.CandidateId == "rapidocr-modelscope-ppocrv5-en-mobile-onnx");

        var audit = service.AuditAlternativeRecognizerCandidate(candidate);

        Assert.AreEqual(438, candidate.OutputClassCount);
        Assert.AreEqual(436, candidate.DictionaryTokenCount);
        Assert.IsTrue(candidate.ExtraClassUnresolved);
        Assert.AreEqual(NodalOsAlternativeRecognizerCandidateDecision.RejectedExtraClassUnresolved, audit.Decision);
        Assert.IsFalse(audit.DecodeAttempted);
        Assert.IsFalse(audit.DownloadExecuted);
        Assert.IsTrue(audit.NoAuthority);
    }

    [TestMethod]
    public void CandidateWithoutExplicitDictionary_IsRejected()
    {
        var candidate = new NodalOsAlternativeRecognizerCandidate(
            "unit-no-dictionary",
            "unit",
            "https://example.invalid/model.onnx",
            DictionaryUrlOrRef: null,
            ConfigUrlOrRef: "https://example.invalid/config.yaml",
            License: "unit-test",
            Provenance: "unit-test",
            Official: true,
            OnnxAvailable: true,
            DictionaryExplicit: false,
            OutputClassCount: 10,
            DictionaryTokenCount: null,
            BlankOrSpecialTokenPolicy: "unknown",
            HashSizePinnable: true,
            LocalOffline: true,
            ExpectedRuntimeRisk: "unit",
            ImplementationImpact: "unit",
            PrivacySecurityRisk: "unit",
            ExtraClassUnresolved: false,
            NoSaas: true,
            NoAuthority: true);

        var audit = new NodalOsOcrDictionaryCompatibilityService().AuditAlternativeRecognizerCandidate(candidate);

        Assert.AreEqual(NodalOsAlternativeRecognizerCandidateDecision.RejectedNoExplicitDictionary, audit.Decision);
        Assert.IsFalse(audit.DecodeAttempted);
        Assert.IsTrue(audit.ProductiveOcrBlocked);
    }

    [TestMethod]
    public void CandidateWithoutOnnx_IsRejectedForCleanPairAcquisition()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidate = service.CreateM265AlternativeRecognizerCandidates()
            .Single(c => c.CandidateId == "paddleocr-ppocrv6-official-inference-family");

        var audit = service.AuditAlternativeRecognizerCandidate(candidate);

        Assert.IsFalse(candidate.OnnxAvailable);
        Assert.AreEqual(NodalOsAlternativeRecognizerCandidateDecision.RejectedNoOnnx, audit.Decision);
        Assert.IsFalse(audit.DownloadExecuted);
        Assert.IsTrue(audit.ShadowModeBlocked);
    }

    [TestMethod]
    public void CleanPairMatrix_DoesNotAutoSelectPpOcrV5IgnoredExtraClass()
    {
        var matrix = new NodalOsOcrDictionaryCompatibilityService().CreateM266CleanRecognizerCompatibilityMatrix();

        Assert.AreEqual(NodalOsAlternativeRecognizerFamilyDecision.ReadyForAlternativeLocalOcrFamilyReview, matrix.Decision);
        Assert.IsNull(matrix.SelectedCandidate);
        Assert.IsFalse(matrix.PpOcrV5IgnoredExtraClassAutoSelected);
        Assert.IsTrue(matrix.DecodeBlocked);
        Assert.IsFalse(matrix.DownloadExecuted);
        Assert.IsTrue(matrix.NoAuthority);
    }

    [TestMethod]
    public void CleanPairMatrix_RanksRuntimeProbeAlternatives_OverTesseractFallback()
    {
        var audits = new NodalOsOcrDictionaryCompatibilityService()
            .CreateM266CleanRecognizerCompatibilityMatrix()
            .CandidateAudits;

        var latin = audits.Single(a => a.Candidate.CandidateId == "rapidocr-modelscope-ppocrv5-latin-mobile-onnx");
        var tesseract = audits.Single(a => a.Candidate.CandidateId == "tesseract-local-fallback");

        Assert.AreEqual(NodalOsAlternativeRecognizerCandidateDecision.CandidateNeedsRuntimeProbe, latin.Decision);
        Assert.AreEqual(NodalOsAlternativeRecognizerCandidateDecision.CandidateNeedsManualReview, tesseract.Decision);
        Assert.IsTrue(latin.Candidate.OnnxAvailable);
        Assert.IsFalse(tesseract.Candidate.OnnxAvailable);
    }

    [TestMethod]
    public void Reports_Artifacts_ClaudePrompt_Adr_Exist_ForM267()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "alternative-recognizer-family-review-m267.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m267", "alternative-recognizer-family-review-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-alternative-recognizer-family-review-audit-m267.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "alternative-recognizer-family-selection-m265-m267.md")));
    }

    [TestMethod]
    public void M267Artifact_RecordsNoDownloadNoDecode_AndNoAuthority()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m267",
            "alternative-recognizer-family-review-summary.json")));

        Assert.AreEqual("M265+M266+M267 CERRADO / READY_FOR_ALTERNATIVE_LOCAL_OCR_FAMILY_REVIEW", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("execution").GetProperty("modelDictionaryDownloadExecuted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("execution").GetProperty("decodeAttempted").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("productiveOcrBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("noAuthority").GetBoolean());
    }
}
