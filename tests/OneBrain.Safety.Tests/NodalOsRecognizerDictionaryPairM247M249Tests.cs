using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RecognizerDictionaryPair")]
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
public sealed class NodalOsRecognizerDictionaryPairM247M249Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Parser_CanPreserveSpaceToken_AndDoesNotSilentlyTrimIt()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var analysis = service.CreateM247SourceReconciliationAnalyses()
            .Single(item => item.SourceId == "paddleocr-release-2.8-en-dict");

        Assert.AreEqual(190L, analysis.RawByteSize);
        Assert.AreEqual("5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3", analysis.Sha256);
        Assert.AreEqual(1, analysis.SpaceOnlyLineCount);
        Assert.IsTrue(analysis.HasSignificantSpaceToken);
        Assert.AreEqual(" ", analysis.LastSignificantToken);
        Assert.AreEqual(95, analysis.PaddleOcrParserTokenCount);
    }

    [TestMethod]
    public void Parser_ReportsRawCount_AndNormalizedCountSeparately()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var analysis = service.CreateM247SourceReconciliationAnalyses()
            .Single(item => item.SourceId == "rapidocr-modelscope-v3.8.0-en-dict");

        Assert.AreEqual(96, analysis.RawLineSegmentCount);
        Assert.AreEqual(96, analysis.TokenCountPreservingEmptyLines);
        Assert.AreEqual(95, analysis.TokenCountPreservingSpaceDroppingTerminalEmpty);
        Assert.AreEqual(94, analysis.TokenCountTrimmingEmptyLines);
        Assert.AreEqual(1, analysis.EmptyLineCount);
        Assert.IsTrue(analysis.HasFinalNewline);
    }

    [TestMethod]
    public void SourceWith95PaddleTokens_RemainsBlocked_WhenNoSignificantExtraTokenExplains97()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var compatibility = service.EvaluateRecognizerDictionaryPair(service.CreateM247SourceReconciliationAnalyses());

        Assert.AreEqual(NodalOsRecognizerDictionaryPairDecision.ReadyForRecognizerModelDictionaryPairReplacement, compatibility.Decision);
        Assert.IsFalse(compatibility.FoundVerified96TokenSource);
        Assert.IsFalse(compatibility.DecodeAllowed);
        Assert.IsTrue(compatibility.OnnxMetadataMatchesDictionary);
        Assert.IsTrue(compatibility.ProductiveOcrBlocked);
        Assert.IsTrue(compatibility.ShadowModeBlocked);
        Assert.IsTrue(compatibility.NoAuthority);
    }

    [TestMethod]
    public void Verified96TokenSource_WouldMapToDictionaryPinning()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var raw = service.CreateOfficialEnglishDictionaryRawText() + "x";
        var analysis = service.AnalyzeDictionaryRawLines(
            "controlled-96-token-source",
            "local-test://controlled-96-token-source",
            "test",
            "test",
            raw);
        var compatibility = service.EvaluateRecognizerDictionaryPair(
        [
            analysis with
            {
                SourceId = "rapidocr-modelscope-v3.8.0-en-dict",
                CountBecomes96UnderDocumentedParser = true
            }
        ]);

        Assert.AreEqual(NodalOsRecognizerDictionaryPairDecision.ReadyForDictionaryPinning, compatibility.Decision);
        Assert.IsTrue(compatibility.FoundVerified96TokenSource);
        Assert.IsFalse(compatibility.DecodeAllowed);
    }

    [TestMethod]
    public void HypothesisOnlyExtraToken_StillBlocksDecode_AndInventsNoText()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidate = service.CreateCtcDecodePolicyCandidates()
            .Single(item => item.CandidateId == "hypothesis-blank-start-unknown");
        var result = service.EvaluateCtcDecodePolicyCandidate(candidate);

        Assert.AreEqual(NodalOsCtcDecodePolicyExperimentStatus.HypothesisOnlyDecodeBlocked, result.Status);
        Assert.IsFalse(result.DecodeAttempted);
        Assert.IsFalse(result.DecodeAllowed);
        Assert.IsNull(result.DecodedText);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void ModelDictionaryMismatch_MapsToPairReplacement()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var compatibility = service.EvaluateRecognizerDictionaryPair(service.CreateM247SourceReconciliationAnalyses());

        Assert.AreEqual(97, compatibility.Pair.RecognizerOutputClassCount);
        Assert.AreEqual(95, compatibility.Pair.DictionaryTokenCountUnderPaddlePolicy);
        Assert.AreEqual(0, compatibility.Pair.CtcBlankIndex);
        Assert.AreEqual(NodalOsDictionaryParserPolicy.PaddleOcrReadLinesStripNewline, compatibility.Pair.ParserPolicy);
        Assert.AreEqual(NodalOsRecognizerDictionaryPairDecision.ReadyForRecognizerModelDictionaryPairReplacement, compatibility.Decision);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM249()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "recognizer-dictionary-pair-reconciliation-m249.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m249", "recognizer-dictionary-pair-reconciliation-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-recognizer-dictionary-pair-reconciliation-audit-m249.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "recognizer-dictionary-pair-reconciliation-m247-m249.md")));
    }
}
