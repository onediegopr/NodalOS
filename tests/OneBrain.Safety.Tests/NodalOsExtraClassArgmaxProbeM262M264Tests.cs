using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ExtraClassArgmax")]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerDictionaryPairReplacement")]
[TestCategory("RecognizerPairAcquisition")]
[TestCategory("PpOcrV5Recognizer")]
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
public sealed class NodalOsExtraClassArgmaxProbeM262M264Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void NeverArgmaxAndNegligibleProbability_MapsToIgnoredExtraClassCandidate_ButDoesNotApproveDecode()
    {
        var result = Result("normal", "TEST", NodalOsExtraClassArgmaxProbeStatus.ExtraClassNeverArgmax, 438, 0, 0.0001, 0.00001);
        var report = new NodalOsOcrDictionaryCompatibilityService()
            .DecideExtraClassDecodePolicyReadiness([result], negligibleProbabilityThreshold: 0.001);

        Assert.AreEqual(NodalOsExtraClassRiskClassification.IgnoredExtraClassCandidate, report.PolicyCandidate.RiskClassification);
        Assert.AreEqual(NodalOsExtraClassDecodePolicyReadiness.ReadyForManualIgnoredExtraClassPolicyApproval, report.Decision);
        Assert.IsFalse(report.PolicyCandidate.DecodeApproved);
        Assert.IsFalse(report.DecodeAttempted);
        Assert.IsTrue(report.ProductiveOcrBlocked);
        Assert.IsTrue(report.ShadowModeBlocked);
        Assert.IsTrue(report.NoAuthority);
    }

    [TestMethod]
    public void AnyExtraClassArgmax_BlocksPolicy()
    {
        var result = Result("extreme", "Checkerboard", NodalOsExtraClassArgmaxProbeStatus.ExtraClassArgmaxObserved, 438, 1, 0.91, 0.02);
        var report = new NodalOsOcrDictionaryCompatibilityService()
            .DecideExtraClassDecodePolicyReadiness([result], negligibleProbabilityThreshold: 0.001);

        Assert.AreEqual(NodalOsExtraClassRiskClassification.BlockedByExtraClassArgmaxObserved, report.PolicyCandidate.RiskClassification);
        Assert.AreEqual(NodalOsExtraClassDecodePolicyReadiness.BlockedByExtraClassArgmaxObserved, report.Decision);
        Assert.IsFalse(report.PolicyCandidate.DecodeApproved);
    }

    [TestMethod]
    public void NonTrivialProbabilityWithoutArgmax_BlocksForManualReview()
    {
        var result = Result("normal", "NODAL", NodalOsExtraClassArgmaxProbeStatus.ExtraClassProbabilityNonTrivial, 438, 0, 0.04, 0.003);
        var report = new NodalOsOcrDictionaryCompatibilityService()
            .DecideExtraClassDecodePolicyReadiness([result], negligibleProbabilityThreshold: 0.001);

        Assert.AreEqual(NodalOsExtraClassRiskClassification.ManualReviewRequired, report.PolicyCandidate.RiskClassification);
        Assert.AreEqual(NodalOsExtraClassDecodePolicyReadiness.BlockedByExtraClassNontrivialProbability, report.Decision);
        Assert.IsFalse(report.DecodeAttempted);
    }

    [TestMethod]
    public void UnexpectedClassCount_BlocksAndRecommendsReplacementRoute()
    {
        var result = Result("normal", "TEST", NodalOsExtraClassArgmaxProbeStatus.ProbeRuntimeFailed, 437, 0, 0, 0);
        var report = new NodalOsOcrDictionaryCompatibilityService()
            .DecideExtraClassDecodePolicyReadiness([result], negligibleProbabilityThreshold: 0.001);

        Assert.AreEqual(NodalOsExtraClassRiskClassification.BlockedByUnexpectedClassCount, report.PolicyCandidate.RiskClassification);
        Assert.AreEqual(NodalOsExtraClassDecodePolicyReadiness.ReadyForRecognizerModelReplacement, report.Decision);
    }

    [TestMethod]
    public void Runner_ExposesExtraClassArgmaxProbe_AndBlocksInvalidCropBeforeRuntime()
    {
        var runner = File.ReadAllText(Path.Combine(RepoRoot, "tools", "onnx-ocr-probe-runner", "Program.cs"));

        StringAssert.Contains(runner, "--extra-class-argmax-probe");
        StringAssert.Contains(runner, "--extra-class-argmax-child");
        StringAssert.Contains(runner, "ExtraClassIndex = 437");
        StringAssert.Contains(runner, "InvalidEmptyCrop");
        StringAssert.Contains(runner, "ProbeBlockedByInvalidInput");
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM264()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "extra-class-argmax-frequency-probe-m264.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m264", "extra-class-argmax-frequency-probe-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-extra-class-argmax-frequency-probe-audit-m264.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "extra-class-argmax-frequency-probe-decision-m262-m264.md")));
    }

    [TestMethod]
    public void Artifact_BlocksDecodeAndRecordsProbeEvidence()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m264", "extra-class-argmax-frequency-probe-summary.json")));

        Assert.IsFalse(doc.RootElement.GetProperty("decode").GetProperty("attempted").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("productiveOcrBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("shadowModeBlocked").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("safety").GetProperty("noAuthority").GetBoolean());
        Assert.AreEqual(437, doc.RootElement.GetProperty("probe").GetProperty("extraClassIndex").GetInt32());
        Assert.AreEqual(438, doc.RootElement.GetProperty("probe").GetProperty("outputClassCount").GetInt32());
    }

    private static NodalOsExtraClassArgmaxProbeResult Result(
        string group,
        string fixture,
        NodalOsExtraClassArgmaxProbeStatus status,
        int classCount,
        int argmaxCount,
        double maxProbability,
        double avgProbability)
    {
        return new NodalOsExtraClassArgmaxProbeResult(
            $"unit-{fixture}",
            fixture,
            group,
            status,
            [1, 40, classCount],
            new NodalOsExtraClassProbabilitySummary(
                ExtraClassIndex: 437,
                OutputClassCount: classCount,
                Timesteps: 40,
                ExtraClassArgmaxCount: argmaxCount,
                ExtraClassMaxProbability: maxProbability,
                ExtraClassAverageProbability: avgProbability,
                BlankIndex: 0,
                BlankArgmaxCount: 39,
                DominantClassIndexes: [0, 1]),
            RanOutOfProcess: true,
            ParentSurvived: true,
            TempCleanup: true,
            RawPersisted: false,
            Sensitive: false,
            FullScreen: false,
            CallsSaas: false,
            NoAuthority: true,
            "unit synthetic evidence only");
    }
}
