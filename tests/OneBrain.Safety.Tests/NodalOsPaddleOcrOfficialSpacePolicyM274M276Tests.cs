using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrSpaceToken")]
[TestCategory("OfficialSpaceToken")]
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
public sealed class NodalOsPaddleOcrOfficialSpacePolicyM274M276Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void OfficialSpaceReadiness_ApprovesMappingOnly_AndBlocksProductiveOcr()
    {
        var report = new NodalOsPaddleOcrSpaceTokenDecoderService().CreateOfficialSpaceReadinessReport();

        Assert.AreEqual(
            NodalOsPaddleOcrOfficialSpaceReadinessDecision.ReadyForSyntheticOfficialSpaceDecodeFixtures,
            report.Decision);
        Assert.IsTrue(report.ClassSemanticsResolved);
        Assert.IsTrue(report.MappingPolicyApproved);
        Assert.IsFalse(report.IgnoreExtraClassApproved);
        Assert.IsFalse(report.DecodeSuccessClaimed);
        Assert.IsTrue(report.ProductiveOcrBlocked);
        Assert.IsTrue(report.ShadowModeBlocked);
        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.NoRawPersistence);
        Assert.IsTrue(report.NoFullScreen);
        Assert.IsTrue(report.NoSensitive);
        Assert.IsTrue(report.NoSaas);
    }

    [TestMethod]
    public void OfficialSpaceReadiness_PreservesPaddleOcrV4AndV5Formulas()
    {
        var rootCause = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateOfficialSpaceReadinessReport()
            .RootCause;

        Assert.AreEqual(0, rootCause.PpOcrV4Layout.BlankIndex);
        Assert.AreEqual(1, rootCause.PpOcrV4Layout.DictionaryStartIndex);
        Assert.AreEqual(95, rootCause.PpOcrV4Layout.DictionaryEndIndexInclusive);
        Assert.AreEqual(96, rootCause.PpOcrV4Layout.SpaceIndex);
        Assert.AreEqual(97, rootCause.PpOcrV4Layout.TotalClassCount);

        Assert.AreEqual(0, rootCause.PpOcrV5Layout.BlankIndex);
        Assert.AreEqual(1, rootCause.PpOcrV5Layout.DictionaryStartIndex);
        Assert.AreEqual(436, rootCause.PpOcrV5Layout.DictionaryEndIndexInclusive);
        Assert.AreEqual(437, rootCause.PpOcrV5Layout.SpaceIndex);
        Assert.AreEqual(438, rootCause.PpOcrV5Layout.TotalClassCount);
    }

    [TestMethod]
    public void OfficialSpaceReadiness_ConfirmsBtcSoftmaxOutput_WithoutDoubleSoftmax()
    {
        var report = new NodalOsPaddleOcrSpaceTokenDecoderService().CreateOfficialSpaceReadinessReport();

        Assert.IsTrue(report.OutputLayoutBatchTimeClass);
        Assert.IsTrue(report.OutputAlreadySoftmax);
        Assert.IsFalse(report.SoftmaxReapplied);
        Assert.IsTrue(report.RootCause.OutputAxisOrder.Contains("[B,T,C]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SyntheticProbabilityFixtures_DecodeSpaces_WithOfficialPolicyOnly()
    {
        var fixtures = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateOfficialSpaceReadinessReport()
            .SyntheticFixtures;

        Assert.IsTrue(fixtures.Any(f => f.FixtureId == "synthetic-probability-12-34"
                                        && f.DecodedText == "12 34"
                                        && f.SpaceTokenEmissions == 1));
        Assert.IsTrue(fixtures.Any(f => f.FixtureId == "synthetic-probability-pvc-wall"
                                        && f.DecodedText == "PVC WALL"
                                        && f.SpaceTokenEmissions == 1));
        Assert.IsTrue(fixtures.Any(f => f.FixtureId == "synthetic-probability-a-b-c"
                                        && f.DecodedText == "A B C"
                                        && f.SpaceTokenEmissions == 2));
        Assert.IsTrue(fixtures.All(f => f.PolicyKind == NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken));
        Assert.IsTrue(fixtures.All(f => f.NoAuthority && !f.ProductiveOcr && !f.ShadowMode));
    }

    [TestMethod]
    public void BlankDominantFixture_RecordsSpaceAsTopTwo_ButDoesNotDecode()
    {
        var fixture = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateOfficialSpaceReadinessReport()
            .SyntheticFixtures
            .Single(f => f.FixtureId == "synthetic-probability-blank-dominant-space-top2");

        Assert.AreEqual(string.Empty, fixture.DecodedText);
        Assert.AreEqual(0, fixture.SpaceTokenEmissions);
        Assert.IsTrue(fixture.TopK.All(k => k.ArgmaxIndex == 0));
        Assert.IsTrue(fixture.TopK.All(k => k.SpaceIsTopTwo));
    }

    [TestMethod]
    public void SpaceArgmaxFixture_EmitsSpace_WhenSpaceWinsATimestep()
    {
        var fixture = new NodalOsPaddleOcrSpaceTokenDecoderService()
            .CreateOfficialSpaceReadinessReport()
            .SyntheticFixtures
            .Single(f => f.FixtureId == "synthetic-probability-space-argmax");

        Assert.AreEqual("A B", fixture.DecodedText);
        Assert.AreEqual(1, fixture.SpaceTokenEmissions);
        Assert.IsTrue(fixture.TopK.Any(k => k.ArgmaxIndex == 37));
    }

    [TestMethod]
    public void M276_Report_Artifact_Audit_Adr_Exist()
    {
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "docs",
            "reports",
            "paddleocr-official-space-policy-integration-m276.md")));
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "artifacts",
            "ocr-vision-onnx",
            "m276",
            "paddleocr-official-space-policy-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "docs",
            "audits",
            "paddleocr-space-token-no-authority-readiness-audit-m276.md")));
        Assert.IsTrue(File.Exists(Path.Combine(
            RepoRoot,
            "docs",
            "adr",
            "paddleocr-official-space-token-policy-m274-m276.md")));
    }
}
