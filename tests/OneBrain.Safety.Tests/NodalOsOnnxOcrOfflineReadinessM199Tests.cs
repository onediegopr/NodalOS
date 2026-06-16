using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxOcrOfflineReadiness")]
[TestCategory("OnnxOcrModelMissing")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxOcrOfflineReadinessM199Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static NodalOsPaddleOcrOnnxModelManifest DefaultManifest()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        return catalog.CreateDefaultManifest();
    }

    private static (NodalOsPaddleOcrOnnxModelManifest Manifest, List<string> FilesToClean) CreateVerifiedManifest()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var modelDir = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx");
        Directory.CreateDirectory(modelDir);
        var detPath = Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx");
        var recPath = Path.Combine(modelDir, "ch_PP-OCRv4_rec.onnx");
        File.WriteAllText(detPath, "det-content");
        File.WriteAllText(recPath, "rec-content");

        var detSize = new FileInfo(detPath).Length;
        var recSize = new FileInfo(recPath).Length;
        var detHash = NodalOsPaddleOcrOnnxModelCatalogService.ComputeChecksum(detPath);
        var recHash = NodalOsPaddleOcrOnnxModelCatalogService.ComputeChecksum(recPath);

        var verified = catalog.UpdateModelStatus(manifest, "paddleocr-det-onnx", NodalOsPaddleOcrOnnxModelStatus.Downloaded, detSize, detHash);
        verified = catalog.UpdateModelStatus(verified, "paddleocr-rec-onnx", NodalOsPaddleOcrOnnxModelStatus.Downloaded, recSize, recHash);

        return (verified, new List<string> { detPath, recPath });
    }

    [TestMethod]
    public void MissingModel_Decision_IsModelMissing()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var manifest = DefaultManifest() with { LicenseReviewed = true };

        var report = gate.Evaluate(manifest, RepoRoot, licenseAccepted: true, true, true, true, NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En);

        Assert.AreEqual(NodalOsOnnxOcrOfflineReadinessDecision.ModelMissing, report.Decision);
        Assert.IsFalse(report.CanAttemptSyntheticRun);
    }

    [TestMethod]
    public void UnverifiedModel_Decision_IsModelUnverified()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var modelDir = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx");
        Directory.CreateDirectory(modelDir);
        var detPath = Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx");
        var recPath = Path.Combine(modelDir, "ch_PP-OCRv4_rec.onnx");
        File.WriteAllText(detPath, "det-bad");
        File.WriteAllText(recPath, "rec-good");
        try
        {
            var recSize = new FileInfo(recPath).Length;
            var recHash = NodalOsPaddleOcrOnnxModelCatalogService.ComputeChecksum(recPath);

            // Detection model exists but checksum is wrong => Invalid/Unverified.
            var updated = catalog.UpdateModelStatus(manifest, "paddleocr-det-onnx", NodalOsPaddleOcrOnnxModelStatus.Downloaded, 100, "abc");
            updated = catalog.UpdateModelStatus(updated, "paddleocr-rec-onnx", NodalOsPaddleOcrOnnxModelStatus.Downloaded, recSize, recHash);

            var report = gate.Evaluate(updated, RepoRoot, licenseAccepted: true, true, true, true, NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En);

            Assert.AreEqual(NodalOsOnnxOcrOfflineReadinessDecision.ModelUnverified, report.Decision);
        }
        finally
        {
            File.Delete(detPath);
            File.Delete(recPath);
        }
    }

    [TestMethod]
    public void UnknownShape_Decision_IsUnsupportedModelShape()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var (manifest, files) = CreateVerifiedManifest();
        try
        {
            var report = gate.Evaluate(manifest, RepoRoot, licenseAccepted: true, true, true, true, NodalOsOnnxOcrSyntheticFixtureSet.UnknownShapes);
            Assert.AreEqual(NodalOsOnnxOcrOfflineReadinessDecision.UnsupportedModelShape, report.Decision);
        }
        finally
        {
            foreach (var f in files) File.Delete(f);
        }
    }

    [TestMethod]
    public void PreProcessorMissing_Decision_IsPreProcessingIncomplete()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var (manifest, files) = CreateVerifiedManifest();
        try
        {
            var report = gate.Evaluate(manifest, RepoRoot, licenseAccepted: true, false, true, true, NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En);
            Assert.AreEqual(NodalOsOnnxOcrOfflineReadinessDecision.PreProcessingIncomplete, report.Decision);
        }
        finally
        {
            foreach (var f in files) File.Delete(f);
        }
    }

    [TestMethod]
    public void PostProcessorMissing_Decision_IsPostProcessingIncomplete()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var (manifest, files) = CreateVerifiedManifest();
        try
        {
            var report = gate.Evaluate(manifest, RepoRoot, licenseAccepted: true, true, false, true, NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En);
            Assert.AreEqual(NodalOsOnnxOcrOfflineReadinessDecision.PostProcessingIncomplete, report.Decision);
        }
        finally
        {
            foreach (var f in files) File.Delete(f);
        }
    }

    [TestMethod]
    public void ValidSyntheticFixture_WithVerifiedModels_IsReady()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var (manifest, files) = CreateVerifiedManifest();
        try
        {
            var report = gate.Evaluate(manifest, RepoRoot, licenseAccepted: true, true, true, true, NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En);

            Assert.AreEqual(NodalOsOnnxOcrOfflineReadinessDecision.ReadyForOnnxSyntheticRun, report.Decision);
            Assert.IsTrue(report.CanAttemptSyntheticRun);
            Assert.IsTrue(report.DetectionModelVerified);
            Assert.IsTrue(report.RecognitionModelVerified);
            Assert.IsTrue(report.ModelShapesKnown);
        }
        finally
        {
            foreach (var f in files) File.Delete(f);
        }
    }

    [TestMethod]
    public void NoAuthority_Preserved()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var manifest = DefaultManifest() with { LicenseReviewed = true };

        var report = gate.Evaluate(manifest, RepoRoot, licenseAccepted: true, true, true, true, NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En);

        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.ProductionPublicOcrBlocked);
    }

    [TestMethod]
    public void NoSaas_NoRaw_NoFullScreen_NoSensitive_Preserved()
    {
        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var manifest = DefaultManifest() with { LicenseReviewed = true };

        var report = gate.Evaluate(manifest, RepoRoot, licenseAccepted: true, true, true, true, NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En);

        Assert.IsTrue(report.NoSaas);
        Assert.IsTrue(report.NoRawPersistence);
        Assert.IsTrue(report.NoFullScreen);
        Assert.IsTrue(report.NoSensitive);
    }

    [TestMethod]
    public void OfflineReadinessReport_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "reports", "onnx-ocr-offline-readiness-m199.md");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void OfflineReadinessArtifact_Exists()
    {
        var path = Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m199", "onnx-ocr-offline-readiness-summary.json");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void OfflineReadinessClaudePrompt_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "audits", "claude-onnx-ocr-offline-readiness-audit-m199.md");
        Assert.IsTrue(File.Exists(path));
    }
}
