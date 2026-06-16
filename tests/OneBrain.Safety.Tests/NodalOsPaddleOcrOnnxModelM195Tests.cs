using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrOnnx")]
[TestCategory("PaddleOcrOnnxModelManifest")]
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
public sealed class NodalOsPaddleOcrOnnxModelM195Tests
{
    [TestMethod]
    public void PlaceholderManifest_HasRequiredModelKinds()
    {
        var svc = new NodalOsPaddleOcrOnnxModelService();
        var manifest = svc.CreatePlaceholderManifest();

        var kinds = manifest.Models.Select(m => m.Kind).ToList();
        Assert.IsTrue(kinds.Contains(NodalOsPaddleOcrOnnxModelKind.TextDetection));
        Assert.IsTrue(kinds.Contains(NodalOsPaddleOcrOnnxModelKind.TextRecognition));
        Assert.IsTrue(kinds.Contains(NodalOsPaddleOcrOnnxModelKind.TextDirectionClassification));
    }

    [TestMethod]
    public void PlaceholderManifest_ModelsAreMissing()
    {
        var svc = new NodalOsPaddleOcrOnnxModelService();
        var manifest = svc.CreatePlaceholderManifest();

        Assert.IsTrue(manifest.Models.All(m => m.Status == NodalOsPaddleOcrOnnxModelStatus.Missing));
        Assert.IsFalse(manifest.AllRequiredPresent);
        Assert.IsFalse(manifest.AllVerified);
    }

    [TestMethod]
    public void Readiness_BlockedByLicense_WhenNotReviewed()
    {
        var svc = new NodalOsPaddleOcrOnnxModelService();
        var manifest = svc.CreatePlaceholderManifest();
        var readiness = svc.Evaluate(manifest);

        Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.BlockedByLicense, readiness.Status);
        Assert.IsFalse(readiness.CanRunOcr);
    }

    [TestMethod]
    public void Readiness_Missing_WhenLicenseReviewedButModelsMissing()
    {
        var svc = new NodalOsPaddleOcrOnnxModelService();
        var manifest = svc.CreatePlaceholderManifest() with { LicenseReviewed = true };
        var readiness = svc.Evaluate(manifest);

        Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.Missing, readiness.Status);
        Assert.IsFalse(readiness.CanRunOcr);
    }

    [TestMethod]
    public void Readiness_BlockedBySize_WhenModelsTooLarge()
    {
        var svc = new NodalOsPaddleOcrOnnxModelService();
        var manifest = svc.CreatePlaceholderManifest();
        var verifiedModels = manifest.Models.Select(m => m with
        {
            Status = NodalOsPaddleOcrOnnxModelStatus.Verified,
            Integrity = m.Integrity with { FileSizeBytes = 200 * 1024 * 1024 }
        }).ToList();
        manifest = manifest with
        {
            LicenseReviewed = true,
            TotalMaxSizeBytes = 1,
            Models = verifiedModels
        };
        var readiness = svc.Evaluate(manifest);

        Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.BlockedBySize, readiness.Status);
        Assert.IsFalse(readiness.CanRunOcr);
    }

    [TestMethod]
    public void VerifyChecksum_RequiresExpectedChecksum()
    {
        var svc = new NodalOsPaddleOcrOnnxModelService();
        var result = svc.VerifyChecksum("nonexistent.onnx", "");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void AcquisitionPlan_Exists()
    {
        var svc = new NodalOsPaddleOcrOnnxModelService();
        var plan = svc.AcquisitionPlan();

        Assert.IsFalse(string.IsNullOrWhiteSpace(plan.PlanId));
        Assert.IsTrue(plan.Steps.Count > 0);
        Assert.IsTrue(plan.RequiresLicenseReview);
        Assert.IsTrue(plan.RequiresManualDownload);
        Assert.IsTrue(plan.NoAuthority);
    }

    [TestMethod]
    public void ManifestPlaceholderFile_Exists()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(root, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json")));
    }

    [TestMethod]
    public void AcquisitionPlanReport_Exists()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "reports", "paddleocr-onnx-model-acquisition-plan-m195.md")));
    }
}
