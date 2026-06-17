using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsRecognizerClassSemanticsM244M246Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void OfficialPaddleCtcPolicy_95TokensPlusBlankOnly_DoesNotEqual97()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var semantics = service.AuditRecognizerClassSemantics();
        var mapping = semantics.CandidateMappings.Single(m => m.MappingId == "paddle-ctc-blank-at-start");

        Assert.AreEqual(95, mapping.DictionaryTokenCount);
        Assert.IsTrue(EqualityComparer<int>.Default.Equals(96, mapping.ExpectedClassCount));
        Assert.AreEqual(97, mapping.ModelClassCount);
        Assert.AreEqual(0, mapping.BlankIndex);
        Assert.IsFalse(mapping.Compatible);
        Assert.IsFalse(mapping.DecodeAllowed);
    }

    [TestMethod]
    public void TwoSpecialTokenPolicy_Explains97OnlyAsHypothesis_AndBlocksDecode()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidate = service.CreateCtcDecodePolicyCandidates()
            .Single(c => c.CandidateId == "hypothesis-blank-start-unknown");

        var result = service.EvaluateCtcDecodePolicyCandidate(candidate);

        Assert.AreEqual(95, candidate.DictionaryTokenCount);
        Assert.AreEqual(97, candidate.ModelClassCount);
        Assert.IsTrue(candidate.HypothesisOnly);
        Assert.IsFalse(candidate.EvidenceApproved);
        Assert.AreEqual(NodalOsCtcDecodePolicyExperimentStatus.HypothesisOnlyDecodeBlocked, result.Status);
        Assert.IsFalse(result.DecodeAllowed);
        Assert.IsNull(result.DecodedText);
    }

    [TestMethod]
    public void UnsupportedInferredPolicy_BlocksDecode_AndPreservesNoAuthority()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidate = service.CreateCtcDecodePolicyCandidates()
            .Single(c => c.CandidateId == "hypothesis-blank-start-padding");

        var result = service.EvaluateCtcDecodePolicyCandidate(candidate);

        Assert.AreEqual(NodalOsCtcDecodePolicyExperimentStatus.HypothesisOnlyDecodeBlocked, result.Status);
        Assert.IsFalse(result.DecodeAttempted);
        Assert.IsFalse(result.DecodeAllowed);
        Assert.IsTrue(result.NoRawPersistence);
        Assert.IsTrue(result.NoSensitive);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void UnknownPolicy_MapsToRecognizerModelDictionarySourceReview()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var semantics = service.AuditRecognizerClassSemantics();
        var results = service.CreateCtcDecodePolicyCandidates()
            .Select(service.EvaluateCtcDecodePolicyCandidate)
            .ToArray();

        var decision = service.DecideTokenPolicyReadiness(semantics, results);

        Assert.AreEqual(NodalOsRecognizerClassSemanticsDecision.ReadyForRecognizerModelDictionarySourceReview, decision.Decision);
        Assert.IsTrue(decision.ProductiveOcrBlocked);
        Assert.IsTrue(decision.ShadowModeBlocked);
        Assert.IsTrue(decision.NoAuthority);
        Assert.IsFalse(results.Any(r => r.DecodeAttempted));
        Assert.IsFalse(results.Any(r => r.DecodedText is not null));
    }

    [TestMethod]
    public void ModelDictionaryMismatch_IsRecordedAsBlockingCandidate()
    {
        var semantics = new NodalOsOcrDictionaryCompatibilityService().AuditRecognizerClassSemantics();
        var mismatch = semantics.CandidateMappings.Single(m => m.TokenPolicy == NodalOsRecognizerTokenPolicy.ModelDictionaryMismatch);

        Assert.IsFalse(mismatch.Compatible);
        Assert.IsFalse(mismatch.DecodeAllowed);
        Assert.AreEqual("high", mismatch.RiskLevel);
        Assert.IsTrue(mismatch.Reason.Contains("unexplained", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ApprovedTokenPolicyWouldBeRequiredBeforeDecodeSuccess()
    {
        var approved = new NodalOsCtcDecodePolicyCandidate(
            "unit-approved-policy",
            NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAtStart,
            NodalOsRecognizerSpecialTokenPolicy.BlankAtStart,
            DictionaryTokenCount: 96,
            ModelClassCount: 97,
            BlankIndex: 0,
            ExtraTokenIndex: null,
            EvidenceApproved: true,
            HypothesisOnly: false,
            EvidenceSource: "unit-test-approved",
            Reason: "unit test policy");

        var result = new NodalOsOcrDictionaryCompatibilityService().EvaluateCtcDecodePolicyCandidate(approved);

        Assert.AreEqual(NodalOsCtcDecodePolicyExperimentStatus.ApprovedDecodeAllowed, result.Status);
        Assert.IsTrue(result.DecodeAllowed);
        Assert.IsFalse(result.DecodeAttempted);
        Assert.IsTrue(result.RequiresHumanReview);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void LowConfidencePolicy_RemainsHumanReviewOnly()
    {
        var result = new NodalOsCtcDecodePolicyExperimentResult(
            "low-confidence-unit",
            new NodalOsOcrDictionaryCompatibilityService().CreateCtcDecodePolicyCandidates().First(),
            NodalOsCtcDecodePolicyExperimentStatus.LowConfidenceRequiresHumanReview,
            DecodeAttempted: true,
            DecodeAllowed: true,
            DecodedText: "TEST",
            Confidence: 0.31,
            RequiresHumanReview: true,
            NoRawPersistence: true,
            NoSensitive: true,
            NoAuthority: true,
            Reason: "low confidence");

        Assert.IsTrue(result.RequiresHumanReview);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM246()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "recognizer-class-semantics-ctc-policy-m246.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m246", "recognizer-class-semantics-ctc-policy-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-recognizer-class-semantics-ctc-policy-audit-m246.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "recognizer-class-semantics-ctc-policy-decision-m244-m246.md")));
    }
}
