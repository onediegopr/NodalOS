using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("ExtraClassArgmax")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
[TestCategory("RecognizerDictionaryPairReplacement")]
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
[TestCategory("AlternativeRecognizer")]
[TestCategory("AlternativeLocalOcr")]
[TestCategory("OcrEngineStrategy")]
public sealed class NodalOsPaddleOcrSpaceTokenDecoderM271M273Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    // Synthetic charset: blank + digits 0-9 + space. Index 0 = blank, 1..10 = '0'..'9', 11 = ' '.
    private const int SyntheticClassCount = 12;
    private const int SyntheticSpaceIndex = 11;

    private static string[] SyntheticCharset() =>
        ["blank", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", " "];

    private static int DigitIndex(char digit) => (digit - '0') + 1;

    // One timestep per target index, near one-hot softmax rows.
    private static float[] NearOneHot(int[] argmaxSequence, int classCount, float top = 0.9f)
    {
        var rest = (1f - top) / (classCount - 1);
        var matrix = new float[argmaxSequence.Length * classCount];
        for (var t = 0; t < argmaxSequence.Length; t++)
        {
            for (var i = 0; i < classCount; i++)
                matrix[(t * classCount) + i] = i == argmaxSequence[t] ? top : rest;
        }

        return matrix;
    }

    [TestMethod]
    public void RootCause_ConfirmsSpaceToken_ForBothFamilies()
    {
        var rootCause = new NodalOsPaddleOcrSpaceTokenDecoderService().AuditSpaceTokenRootCause();

        Assert.AreEqual(
            NodalOsPaddleOcrSpaceTokenRootCauseStatus.SpaceTokenFromUseSpaceCharConfirmed,
            rootCause.Status);
        Assert.IsTrue(rootCause.AppliesToBothFamilies);
        Assert.IsTrue(rootCause.ExtraClassResolved);
        Assert.IsTrue(rootCause.OutputIsSoftmaxProbabilities);
        Assert.IsTrue(rootCause.OutputAxisOrder.Contains("[B,T,C]", StringComparison.Ordinal));

        // PP-OCRv4: 95 dictionary + blank + space = 97, space at index 96.
        Assert.AreEqual(97, rootCause.PpOcrV4Layout.TotalClassCount);
        Assert.AreEqual(96, rootCause.PpOcrV4Layout.SpaceIndex);
        Assert.AreEqual(0, rootCause.PpOcrV4Layout.BlankIndex);

        // PP-OCRv5: 436 dictionary + blank + space = 438, space at index 437.
        Assert.AreEqual(438, rootCause.PpOcrV5Layout.TotalClassCount);
        Assert.AreEqual(437, rootCause.PpOcrV5Layout.SpaceIndex);
        Assert.AreEqual(0, rootCause.PpOcrV5Layout.BlankIndex);
        Assert.IsTrue(rootCause.PpOcrV5Layout.UseSpaceChar);
        Assert.IsTrue(rootCause.NoAuthority);
    }

    [TestMethod]
    public void CharsetLayout_PlacesSpaceAtLastIndex_AfterDictionary()
    {
        var layout = new NodalOsPaddleOcrSpaceTokenDecoderService().BuildCharsetLayout("PP-OCRv5-en", 436);

        Assert.AreEqual(0, layout.BlankIndex);
        Assert.AreEqual(1, layout.DictionaryStartIndex);
        Assert.AreEqual(436, layout.DictionaryEndIndexInclusive);
        Assert.AreEqual(437, layout.SpaceIndex);
        Assert.AreEqual(438, layout.TotalClassCount);
    }

    [TestMethod]
    public void OfficialSpaceTokenPolicy_DecodesSpaces_OnSyntheticFixture()
    {
        // "12 34" -> '1','2',' ','3','4'
        var sequence = new[] { DigitIndex('1'), DigitIndex('2'), SyntheticSpaceIndex, DigitIndex('3'), DigitIndex('4') };
        var probabilities = NearOneHot(sequence, SyntheticClassCount);

        var experiment = new NodalOsPaddleOcrSpaceTokenDecoderService().DecodeWithPolicy(
            "synthetic-12_34",
            probabilities,
            SyntheticClassCount,
            NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken,
            SyntheticCharset());

        Assert.AreEqual("12 34", experiment.DecodedText);
        Assert.AreEqual(1, experiment.SpaceTokenEmissions);
        Assert.IsTrue(experiment.DecodeAttempted);
        Assert.IsTrue(experiment.EvidenceApproved);
        Assert.IsFalse(experiment.HypothesisOnly);
        Assert.IsFalse(experiment.ClassCountMismatch);

        // No-authority and never promotes anything.
        Assert.IsFalse(experiment.ProductiveOcr);
        Assert.IsFalse(experiment.ShadowMode);
        Assert.IsFalse(experiment.ReadinessPromoted);
        Assert.IsTrue(experiment.NoRawPersistence);
        Assert.IsTrue(experiment.NoSensitive);
        Assert.IsTrue(experiment.NoAuthority);
    }

    [TestMethod]
    public void IgnoreExtraClassPolicy_SilentlyLosesSpaces()
    {
        var sequence = new[] { DigitIndex('1'), DigitIndex('2'), SyntheticSpaceIndex, DigitIndex('3'), DigitIndex('4') };
        var probabilities = NearOneHot(sequence, SyntheticClassCount);

        var experiment = new NodalOsPaddleOcrSpaceTokenDecoderService().DecodeWithPolicy(
            "synthetic-12_34",
            probabilities,
            SyntheticClassCount,
            NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisIgnoreExtraClass,
            SyntheticCharset());

        // Dropping the extra class loses the real space - this is why ignoring it was unsafe.
        Assert.AreEqual("1234", experiment.DecodedText);
        Assert.AreEqual(0, experiment.SpaceTokenEmissions);
        Assert.IsTrue(experiment.HypothesisOnly);
        Assert.IsFalse(experiment.EvidenceApproved);
    }

    [TestMethod]
    public void UnknownTokenPolicy_EmitsReplacementCharacter()
    {
        var sequence = new[] { DigitIndex('1'), DigitIndex('2'), SyntheticSpaceIndex, DigitIndex('3'), DigitIndex('4') };
        var probabilities = NearOneHot(sequence, SyntheticClassCount);

        var experiment = new NodalOsPaddleOcrSpaceTokenDecoderService().DecodeWithPolicy(
            "synthetic-12_34",
            probabilities,
            SyntheticClassCount,
            NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisUnknownToken,
            SyntheticCharset());

        Assert.AreEqual("12�34", experiment.DecodedText);
        Assert.IsTrue(experiment.HypothesisOnly);
        Assert.IsFalse(experiment.EvidenceApproved);
    }

    [TestMethod]
    public void BlankOnlyPolicy_ReportsClassCountMismatch_AndDoesNotDecode()
    {
        var sequence = new[] { DigitIndex('1'), DigitIndex('2') };
        var probabilities = NearOneHot(sequence, SyntheticClassCount);

        var experiment = new NodalOsPaddleOcrSpaceTokenDecoderService().DecodeWithPolicy(
            "synthetic-12",
            probabilities,
            SyntheticClassCount,
            NodalOsPaddleOcrExtraClassDecodePolicyKind.BlankOnlyClassCountMismatch,
            SyntheticCharset());

        Assert.IsTrue(experiment.ClassCountMismatch);
        Assert.IsFalse(experiment.DecodeAttempted);
        Assert.AreEqual(string.Empty, experiment.DecodedText);
    }

    [TestMethod]
    public void ExtraClass_NeverWinsArgmax_ButIsRunnerUp_OnBlankDominantPadding()
    {
        // Reproduces the M262-M264 '12345' behaviour: blank wins every timestep, the space class is the
        // non-trivial runner-up (max probability ~0.28), and never decodes anything.
        const int timesteps = 3;
        var probabilities = new float[timesteps * SyntheticClassCount];
        for (var t = 0; t < timesteps; t++)
        {
            var offset = t * SyntheticClassCount;
            probabilities[offset + 0] = 0.55f;                 // blank dominates argmax
            probabilities[offset + SyntheticSpaceIndex] = 0.28f; // space is the runner-up
            var rest = (1f - 0.55f - 0.28f) / (SyntheticClassCount - 2);
            for (var i = 1; i < SyntheticSpaceIndex; i++)
                probabilities[offset + i] = rest;
        }

        var service = new NodalOsPaddleOcrSpaceTokenDecoderService();
        var experiment = service.DecodeWithPolicy(
            "synthetic-blank-dominant-padding",
            probabilities,
            SyntheticClassCount,
            NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken,
            SyntheticCharset());

        // Even though the space probability is non-trivial, it never wins -> nothing decodes.
        Assert.AreEqual(string.Empty, experiment.DecodedText);
        Assert.AreEqual(0, experiment.SpaceTokenEmissions);

        Assert.IsTrue(experiment.TopK.All(k => k.ArgmaxIndex == 0));
        Assert.IsTrue(experiment.TopK.All(k => k.SpaceIsTopTwo));
        Assert.IsTrue(experiment.TopK.All(k => k.RunnerUpIndex == SyntheticSpaceIndex));
        Assert.IsTrue(experiment.TopK.All(k => k.SpaceClassProbability > 0.27d && k.SpaceClassProbability < 0.29d));
    }

    [TestMethod]
    public void Decision_ApprovesSpaceTokenPolicy_WithoutEnablingProductiveOcr()
    {
        var service = new NodalOsPaddleOcrSpaceTokenDecoderService();
        var rootCause = service.AuditSpaceTokenRootCause();
        var sequence = new[] { DigitIndex('1'), SyntheticSpaceIndex, DigitIndex('2') };
        var probabilities = NearOneHot(sequence, SyntheticClassCount);

        var experiments = new[]
        {
            service.DecodeWithPolicy("synthetic-1_2", probabilities, SyntheticClassCount,
                NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken, SyntheticCharset()),
            service.DecodeWithPolicy("synthetic-1_2", probabilities, SyntheticClassCount,
                NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisIgnoreExtraClass, SyntheticCharset())
        };

        var decision = service.DecideSpaceTokenPolicy(rootCause, experiments);

        Assert.AreEqual(NodalOsPaddleOcrExtraClassDecision.ReadyForApprovedDecodePolicy, decision.Decision);
        Assert.IsTrue(decision.ExtraClassSemanticsResolved);
        Assert.IsTrue(decision.ApprovedPolicyIsSpaceToken);
        Assert.IsFalse(decision.DecodeSuccessClaimed);
        Assert.IsTrue(decision.ProductiveOcrBlocked);
        Assert.IsTrue(decision.ShadowModeBlocked);
        Assert.IsTrue(decision.NoRawPersistence);
        Assert.IsTrue(decision.NoFullScreen);
        Assert.IsTrue(decision.NoSensitive);
        Assert.IsTrue(decision.NoSaas);
        Assert.IsTrue(decision.NoAuthority);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM273()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-extra-class-root-cause-m273.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m273", "paddleocr-extra-class-root-cause-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-paddleocr-extra-class-root-cause-audit-m273.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-extra-class-root-cause-decision-m271-m273.md")));
    }
}
