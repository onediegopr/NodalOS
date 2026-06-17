using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerDictionaryPairReplacement")]
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
public sealed class NodalOsRecognizerDictionaryPairReplacementM250M252Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void CandidateWithoutExplicitDictionary_IsRejected()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidate = service.CreateM250ReplacementCandidates()
            .Single(item => item.CandidateId == "paddleocr-huggingface-ppocrv4-mobile-rec");

        var audit = service.AuditReplacementCandidate(candidate);

        Assert.AreEqual(NodalOsRecognizerDictionaryPairCandidateDecision.RejectedNoOnnx, audit.Decision);
        Assert.IsFalse(audit.DecodeAttempted);
        Assert.IsTrue(audit.NoAuthority);
    }

    [TestMethod]
    public void CandidateWithoutOnnxAvailability_IsRejectedOrMetadataOnly()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var audit = service.CreateM251CompatibilityMatrix().CandidateAudits
            .Single(item => item.Candidate.CandidateId == "paddleocr-huggingface-ppocrv4-mobile-rec");

        Assert.AreEqual(NodalOsRecognizerDictionaryPairCandidateDecision.RejectedNoOnnx, audit.Decision);
        Assert.IsFalse(audit.Candidate.OnnxAvailable);
    }

    [TestMethod]
    public void CurrentCandidateWithDictionaryCountMismatch_IsRejected()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var audit = service.CreateM251CompatibilityMatrix().CandidateAudits
            .Single(item => item.Candidate.CandidateId == "rapidocr-modelscope-ppocrv4-en-current-onnx");

        Assert.AreEqual(NodalOsRecognizerDictionaryPairCandidateDecision.RejectedCountMismatch, audit.Decision);
        Assert.IsTrue(EqualityComparer<int?>.Default.Equals(97, audit.Candidate.ExpectedOutputClassCount));
        Assert.AreEqual(95, audit.Candidate.DictionaryTokenCount);
    }

    [TestMethod]
    public void CompatibilityMatrix_RanksExplicitEnglishPpocrV5Pair_ForAcquisition()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var matrix = service.CreateM251CompatibilityMatrix();

        Assert.AreEqual(NodalOsRecognizerDictionaryPairReplacementDecision.ReadyForRecognizerDictionaryPairAcquisition, matrix.Decision);
        Assert.IsNotNull(matrix.SelectedCandidate);
        Assert.AreEqual("rapidocr-modelscope-ppocrv5-en-mobile-onnx", matrix.SelectedCandidate.Candidate.CandidateId);
        Assert.AreEqual(436, matrix.SelectedCandidate.Candidate.DictionaryTokenCount);
        Assert.IsTrue(EqualityComparer<int?>.Default.Equals(437, matrix.SelectedCandidate.Candidate.ExpectedOutputClassCount));
        Assert.IsTrue(matrix.DecodeBlocked);
        Assert.IsTrue(matrix.NoAuthority);
    }

    [TestMethod]
    public void LatinCandidate_IsExplicitButManualReviewDueMigrationImpact()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var audit = service.CreateM251CompatibilityMatrix().CandidateAudits
            .Single(item => item.Candidate.CandidateId == "rapidocr-modelscope-ppocrv5-latin-mobile-onnx");

        Assert.AreEqual(NodalOsRecognizerDictionaryPairCandidateDecision.CandidateNeedsManualReview, audit.Decision);
        Assert.AreEqual(502, audit.Candidate.DictionaryTokenCount);
        Assert.IsTrue(EqualityComparer<int?>.Default.Equals(503, audit.Candidate.ExpectedOutputClassCount));
    }

    [TestMethod]
    public void UnpinnableCandidate_BlocksAcquisition()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidate = service.CreateM250ReplacementCandidates()
            .Single(item => item.CandidateId == "rapidocr-modelscope-ppocrv5-en-mobile-onnx") with
        {
            DictionarySha256 = null,
            DictionaryHashPinned = false
        };

        var audit = service.AuditReplacementCandidate(candidate);

        Assert.AreEqual(NodalOsRecognizerDictionaryPairCandidateDecision.RejectedUnpinnable, audit.Decision);
        Assert.IsFalse(audit.DecodeAttempted);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM252()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "recognizer-dictionary-pair-replacement-selection-m252.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m252", "recognizer-dictionary-pair-replacement-selection-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-recognizer-dictionary-pair-replacement-selection-audit-m252.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "recognizer-dictionary-pair-replacement-selection-m250-m252.md")));
    }
}
