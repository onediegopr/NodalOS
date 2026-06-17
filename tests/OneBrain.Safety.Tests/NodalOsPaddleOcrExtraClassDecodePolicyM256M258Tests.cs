using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerDictionaryPairReplacement")]
[TestCategory("RecognizerPairAcquisition")]
[TestCategory("PpOcrV5Recognizer")]
[TestCategory("DictionarySourceReconciliation")]
[TestCategory("RecognizerClassSemantics")]
[TestCategory("RecognizerTokenPolicy")]
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
public sealed class NodalOsPaddleOcrExtraClassDecodePolicyM256M258Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void OfficialBlankOnlyPolicy_RemainsInsufficient_ForPpOcrV4AndPpOcrV5()
    {
        var semantics = new NodalOsOcrDictionaryCompatibilityService().AuditPaddleOcrExtraClassSemantics();
        var official = semantics.Candidates.Single(c => c.CandidateId == "official-ctc-blank-only");

        Assert.AreEqual(95, semantics.PpOcrV4DictionaryTokenCount);
        Assert.AreEqual(97, semantics.PpOcrV4ObservedClassCount);
        Assert.AreEqual(436, semantics.PpOcrV5DictionaryTokenCount);
        Assert.AreEqual(438, semantics.PpOcrV5ObservedClassCount);
        Assert.IsTrue(official.EvidenceApproved);
        Assert.IsFalse(official.SupportsPpOcrV4Pattern);
        Assert.IsFalse(official.SupportsPpOcrV5Pattern);
        Assert.IsTrue(semantics.OfficialBlankOnlyPolicyInsufficient);
        Assert.IsFalse(semantics.DecodeAllowed);
    }

    [TestMethod]
    public void ExtraClassHypothesisWithoutEvidence_BlocksDecode()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var semantics = service.AuditPaddleOcrExtraClassSemantics();
        var ignored = semantics.Candidates.Single(c => c.CandidateId == "hypothesis-ignored-extra-class");
        var approval = service.EvaluatePaddleOcrDecodeClassPolicy(
            service.CreatePaddleOcrDecodeClassPolicies().Single(p => p.PolicyId == "ppocrv5-hypothesis-ignore-extra"));

        Assert.IsTrue(ignored.SupportsPpOcrV4Pattern);
        Assert.IsTrue(ignored.SupportsPpOcrV5Pattern);
        Assert.IsFalse(ignored.EvidenceApproved);
        Assert.IsFalse(ignored.DecodeAllowed);
        Assert.AreEqual(NodalOsPaddleOcrDecodePolicyExperimentStatus.HypothesisOnly, approval.Status);
        Assert.IsFalse(approval.DecodeAttempted);
        Assert.IsFalse(approval.DecodeAllowed);
        Assert.IsNull(approval.DecodedText);
    }

    [TestMethod]
    public void UnknownAndPaddingPolicies_RequireEvidence()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var approvals = service.CreatePaddleOcrDecodeClassPolicies()
            .Where(policy => policy.PolicyId is "ppocrv5-hypothesis-unknown" or "ppocrv5-hypothesis-padding")
            .Select(service.EvaluatePaddleOcrDecodeClassPolicy)
            .ToArray();

        Assert.AreEqual(2, approvals.Length);
        Assert.IsTrue(approvals.All(a => a.Policy.HypothesisOnly));
        Assert.IsTrue(approvals.All(a => !a.Policy.EvidenceApproved));
        Assert.IsTrue(approvals.All(a => a.Status == NodalOsPaddleOcrDecodePolicyExperimentStatus.HypothesisOnly));
        Assert.IsTrue(approvals.All(a => !a.DecodeAllowed));
        Assert.IsTrue(approvals.All(a => a.NoAuthority));
    }

    [TestMethod]
    public void DecodePolicyDecision_MapsUnresolvedExtraClass_ToBlockedByExtraClassSemantics()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var semantics = service.AuditPaddleOcrExtraClassSemantics();
        var approvals = service.CreatePaddleOcrDecodeClassPolicies()
            .Select(service.EvaluatePaddleOcrDecodeClassPolicy)
            .ToArray();

        var decision = service.DecidePaddleOcrDecodePolicy(semantics, approvals);

        Assert.AreEqual(NodalOsPaddleOcrExtraClassDecision.BlockedByExtraClassSemantics, decision.Decision);
        Assert.IsTrue(decision.ProductiveOcrBlocked);
        Assert.IsTrue(decision.ShadowModeBlocked);
        Assert.IsFalse(decision.DecodeSuccessClaimed);
        Assert.IsTrue(decision.NoRawPersistence);
        Assert.IsTrue(decision.NoFullScreen);
        Assert.IsTrue(decision.NoSensitive);
        Assert.IsTrue(decision.NoSaas);
        Assert.IsTrue(decision.NoAuthority);
    }

    [TestMethod]
    public void ApprovedPolicyRequiresEvidence_BeforeDecodeSuccessCanBeClaimed()
    {
        var policy = new NodalOsPaddleOcrDecodeClassPolicy(
            "unit-approved-policy",
            "unit approved policy",
            DictionaryTokenCount: 436,
            BlankIndex: 0,
            ModelClassCount: 437,
            ExpectedClassCount: 437,
            ExtraClassIndex: null,
            EvidenceApproved: true,
            HypothesisOnly: false,
            EvidenceSource: "unit test evidence",
            AllowsDecode: true,
            Reason: "unit test");

        var approval = new NodalOsOcrDictionaryCompatibilityService().EvaluatePaddleOcrDecodeClassPolicy(policy);

        Assert.AreEqual(NodalOsPaddleOcrDecodePolicyExperimentStatus.Approved, approval.Status);
        Assert.IsTrue(approval.DecodeAllowed);
        Assert.IsFalse(approval.DecodeAttempted);
        Assert.IsTrue(approval.RequiresHumanReview);
        Assert.IsTrue(approval.NoAuthority);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM258()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-extra-class-decode-policy-m258.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m258", "paddleocr-extra-class-decode-policy-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-paddleocr-extra-class-decode-policy-audit-m258.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-extra-class-decode-policy-m256-m258.md")));
    }
}
