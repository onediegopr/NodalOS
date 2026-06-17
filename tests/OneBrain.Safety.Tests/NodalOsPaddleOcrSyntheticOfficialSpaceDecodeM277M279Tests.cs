using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrSynthetic")]
[TestCategory("OfficialSpaceToken")]
[TestCategory("PaddleOcrSpaceToken")]
[TestCategory("PaddleOcrExtraClass")]
[TestCategory("ExtraClassArgmax")]
[TestCategory("DecodePolicy")]
[TestCategory("CtcDecodePolicy")]
[TestCategory("RecognizerDictionaryPair")]
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
public sealed class NodalOsPaddleOcrSyntheticOfficialSpaceDecodeM277M279Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void SyntheticDecodeReadiness_AdvancesToOnnxRecognizerProbeGateOnly()
    {
        var report = new NodalOsPaddleOcrSpaceTokenDecoderService().CreateSyntheticDecodeReadinessReport();

        Assert.AreEqual(
            NodalOsPaddleOcrSyntheticDecodeReadinessDecision.ReadyForOnnxSyntheticRecognizerDecodeProbe,
            report.Decision);
        Assert.IsTrue(report.OfficialSpacePolicy);
        Assert.AreEqual(0, report.BlankIndex);
        Assert.AreEqual("1..N", report.DictionaryIndexRange);
        Assert.AreEqual("N+1", report.SpaceIndexFormula);
        Assert.AreEqual("[B,T,C]", report.OutputLayout);
        Assert.IsTrue(report.OutputAlreadySoftmax);
        Assert.IsFalse(report.SoftmaxReapplied);
        Assert.IsFalse(report.OnnxProbeAttempted);
        Assert.IsFalse(report.OnnxProbeSucceeded);
        Assert.IsTrue(report.ProductiveOcrBlocked);
        Assert.IsTrue(report.ShadowModeBlocked);
        Assert.IsTrue(report.NoAuthority);
    }

    [TestMethod]
    public void RequiredSyntheticFixtures_DecodeWithOfficialSpacePolicy()
    {
        var fixtures = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateSyntheticDecodeReadinessReport()
            .SyntheticFixtures;

        AssertFixture(fixtures, "synthetic-probability-12-34", "12 34", 1);
        AssertFixture(fixtures, "synthetic-probability-pvc-wall", "PVC WALL", 1);
        AssertFixture(fixtures, "synthetic-probability-a-b-c", "A B C", 2);
        AssertFixture(fixtures, "synthetic-probability-marmoles-pvc", "MARMOLES PVC", 1);
        Assert.IsTrue(fixtures.All(f => f.PolicyKind == NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken));
        Assert.IsTrue(fixtures.All(f => f.TopK.Count == f.Timesteps));
    }

    [TestMethod]
    public void CtcRepeatCollapse_EmitsRepeatedCharacters_WhenSeparatedByBlank()
    {
        var fixtures = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateSyntheticDecodeReadinessReport()
            .SyntheticFixtures;

        AssertFixture(fixtures, "synthetic-probability-repeat-ll", "LL", 0);
        AssertFixture(fixtures, "synthetic-probability-repeat-oo", "OO", 0);
        AssertFixture(fixtures, "synthetic-probability-repeat-11", "11", 0);

        foreach (var fixture in fixtures.Where(f => f.FixtureId.Contains("repeat", StringComparison.Ordinal)))
            Assert.IsTrue(fixture.TopK.Any(k => k.ArgmaxIndex == 0), fixture.FixtureId);
    }

    [TestMethod]
    public void IntermediateBlanks_CollapseWithoutTextLoss()
    {
        var fixture = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateSyntheticDecodeReadinessReport()
            .SyntheticFixtures
            .Single(f => f.FixtureId == "synthetic-probability-p-blank-v-blank-c");

        Assert.AreEqual("PVC", fixture.DecodedText);
        Assert.AreEqual(0, fixture.SpaceTokenEmissions);
        Assert.IsTrue(fixture.TopK.Count(k => k.ArgmaxIndex == 0) >= 2);
    }

    [TestMethod]
    public void MultipleAndBoundarySpaces_ArePreservedByExplicitPolicy()
    {
        var fixtures = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateSyntheticDecodeReadinessReport()
            .SyntheticFixtures;

        AssertFixture(fixtures, "synthetic-probability-multiple-spaces-a-two-spaces-b", "A  B", 2);
        AssertFixture(fixtures, "synthetic-probability-leading-trailing-space", " A ", 2);
    }

    [TestMethod]
    public void IgnoreExtraClass_RemainsUnsafe_BecauseItDropsRealSpace()
    {
        var service = new NodalOsPaddleOcrSpaceTokenDecoderService();
        var charset = service.CreateSyntheticOfficialSpaceCharset();
        var classCount = charset.Count;
        var spaceIndex = classCount - 1;
        var probabilities = Row(classCount, 12) // B
            .Concat(Row(classCount, spaceIndex))
            .Concat(Row(classCount, 13)) // C
            .ToArray();

        var official = service.DecodeWithPolicy(
            "synthetic-ignore-extra-class-risk",
            probabilities,
            classCount,
            NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken,
            charset);
        var ignored = service.DecodeWithPolicy(
            "synthetic-ignore-extra-class-risk",
            probabilities,
            classCount,
            NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisIgnoreExtraClass,
            charset);

        Assert.AreEqual("B C", official.DecodedText);
        Assert.AreEqual("BC", ignored.DecodedText);
        Assert.IsTrue(ignored.HypothesisOnly);
        Assert.IsFalse(ignored.EvidenceApproved);
    }

    [TestMethod]
    public void SyntheticDecodeFixtures_DoNotClaimProductiveDecodeOrShadowMode()
    {
        var report = new NodalOsPaddleOcrSpaceTokenDecoderService().CreateSyntheticDecodeReadinessReport();

        Assert.IsTrue(report.SyntheticFixtures.All(f => f.DecodeAttempted));
        Assert.IsTrue(report.SyntheticFixtures.All(f => !f.ProductiveOcr));
        Assert.IsTrue(report.SyntheticFixtures.All(f => !f.ShadowMode));
        Assert.IsTrue(report.SyntheticFixtures.All(f => !f.ReadinessPromoted));
        Assert.IsTrue(report.SyntheticFixtures.All(f => f.NoRawPersistence));
        Assert.IsTrue(report.SyntheticFixtures.All(f => f.NoSensitive));
        Assert.IsTrue(report.SyntheticFixtures.All(f => f.NoAuthority));
    }

    [TestMethod]
    public void M279_Report_Artifact_Audit_Adr_Exist()
    {
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "docs",
            "reports",
            "paddleocr-synthetic-official-space-decode-fixtures-m279.md")));
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m279",
            "paddleocr-synthetic-official-space-decode-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "docs",
            "audits",
            "paddleocr-synthetic-decode-fixtures-audit-m279.md")));
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "docs",
            "adr",
            "paddleocr-synthetic-decode-no-authority-policy-m277-m279.md")));
    }

    private static void AssertFixture(
        IReadOnlyList<NodalOsPaddleOcrDecodePolicyExperiment> fixtures,
        string fixtureId,
        string expectedText,
        int expectedSpaceEmissions)
    {
        var fixture = fixtures.Single(f => f.FixtureId == fixtureId);
        Assert.AreEqual(expectedText, fixture.DecodedText);
        Assert.AreEqual(expectedSpaceEmissions, fixture.SpaceTokenEmissions);
        Assert.IsTrue(fixture.EvidenceApproved);
        Assert.IsFalse(fixture.HypothesisOnly);
        Assert.IsFalse(fixture.ClassCountMismatch);
    }

    private static float[] Row(int classCount, int argmax)
    {
        var top = 0.92f;
        var rest = (1f - top) / (classCount - 1);
        var row = new float[classCount];
        for (var i = 0; i < classCount; i++)
            row[i] = i == argmax ? top : rest;

        return row;
    }
}
