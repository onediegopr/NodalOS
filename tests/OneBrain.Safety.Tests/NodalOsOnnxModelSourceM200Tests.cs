using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrOnnxModelSource")]
[TestCategory("PaddleOcrOnnxModelAcquisition")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxModelSourceM200Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Adr_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "adr", "paddleocr-onnx-model-source-decision-m200-m202.md");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void SourceDecision_Primary_IsVerifiedOnnxDownload()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.AreEqual(NodalOsOnnxModelAcquisitionDecision.VerifiedOnnxDownload, decision.Primary);
    }

    [TestMethod]
    public void SourceDecision_OnnxRuntimeDotNet_RemainsPrimary()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(decision.OnnxRuntimeDotNetRemainsPrimary);
    }

    [TestMethod]
    public void SourceDecision_Python_NotPrimary()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(decision.PythonNotPrimary);
        Assert.AreNotEqual(NodalOsOnnxModelAcquisitionDecision.PythonPaddleOcrLegacy, decision.Primary);
        Assert.AreNotEqual(NodalOsOnnxModelAcquisitionDecision.PythonPaddleOcrLegacy, decision.Secondary);
    }

    [TestMethod]
    public void SourceDecision_Saas_Disabled()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(decision.SaasDisabled);
    }

    [TestMethod]
    public void SourceDecision_Production_Blocked()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(decision.ProductionPublicOcrBlocked);
    }

    [TestMethod]
    public void SourceDecision_RecommendedSource_IsPinnedAndHasChecksum()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(decision.RecommendedSource.PinnedVersion);
        Assert.IsTrue(decision.RecommendedSource.ChecksumPublished);
        Assert.IsFalse(string.IsNullOrWhiteSpace(decision.RecommendedSource.Url));
        Assert.IsTrue(decision.RecommendedSource.NoAuthority);
    }

    [TestMethod]
    public void SourceDecision_ConversionPlan_Exists()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsFalse(string.IsNullOrWhiteSpace(decision.ConversionPlan.PlanId));
        Assert.IsTrue(decision.ConversionPlan.Steps.Count > 0);
    }

    [TestMethod]
    public void SourceDecision_RollbackPlan_ConstrainedToOnnx()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(decision.RollbackPlan.DeletesOnlyOnnx);
        Assert.IsTrue(decision.RollbackPlan.FilesToRemove.All(f => f.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void SourceDecisionReport_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "reports", "paddleocr-onnx-model-source-decision-m200.md");
        Assert.IsTrue(File.Exists(path));
    }
}
