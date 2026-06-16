using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrOnnxModelAcquisition")]
[TestCategory("PaddleOcrOnnxModelVerification")]
[TestCategory("PaddleOcrOnnxModelReadiness")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxModelAcquisitionM201Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void DownloadScript_Exists()
    {
        var path = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "download-models.ps1");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void VerifyScript_Exists()
    {
        var path = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "verify-models.ps1");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void RollbackScript_Exists()
    {
        var path = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "rollback-models.ps1");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void ConversionPlan_Exists()
    {
        var path = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "convert-models-plan.md");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void Readme_Exists()
    {
        var path = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "README.md");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void AcquisitionService_ValidatesPinnedSource()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(service.ValidateSource(decision.RecommendedSource));
    }

    [TestMethod]
    public void AcquisitionService_Blocks_UnpinnedSource()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var badSource = new NodalOsOnnxModelAcquisitionSource(
            "bad",
            "unpinned",
            "https://example.com/model.onnx",
            "Apache-2.0",
            "https://example.com/license",
            RequiresAttribution: false,
            NodalOsOnnxModelSupplyChainRisk.CommunityUnverified,
            PinnedVersion: false,
            ChecksumPublished: true,
            NoAuthority: true);

        Assert.IsFalse(service.ValidateSource(badSource));
    }

    [TestMethod]
    public void Manifest_HasSourceUrlsAndHashes()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.LoadManifestFromFile(Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json"));
        Assert.IsNotNull(manifest);

        foreach (var model in manifest.Models)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(model.Source.Url), $"{model.ModelId} missing source URL");
            Assert.IsFalse(string.IsNullOrWhiteSpace(model.Integrity.Checksum), $"{model.ModelId} missing checksum");
            Assert.AreEqual("SHA-256", model.Integrity.Algorithm);
        }
    }

    [TestMethod]
    public void RollbackPlan_DoesNotDeleteCode()
    {
        var service = new NodalOsOnnxModelAcquisitionService();
        var decision = service.DecideSource();

        Assert.IsTrue(decision.RollbackPlan.FilesToRemove.All(f => f.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(decision.RollbackPlan.FilesToRemove.Any(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(decision.RollbackPlan.FilesToRemove.Any(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(decision.RollbackPlan.FilesToRemove.Any(f => f.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)));
    }
}
