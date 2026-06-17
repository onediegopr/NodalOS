using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrOnnx")]
[TestCategory("PaddleOcrOnnxModelManifest")]
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
public sealed class NodalOsPaddleOcrOnnxModelVerificationM197Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static string CreateTempRepoRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"onebrain-onnx-model-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "tools", "ocr-worker", "models", "onnx"));
        return root;
    }

    private static void DeleteTempRepoRoot(string root)
    {
        if (root.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase) && Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [TestMethod]
    public void ManifestFile_Exists_And_Loads()
    {
        var path = Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json");
        Assert.IsTrue(File.Exists(path));

        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.LoadManifestFromFile(path);

        Assert.IsNotNull(manifest);
        Assert.IsTrue(manifest.Models.Count >= 2);
    }

    [TestMethod]
    public void Manifest_HasDetectionAndRecognitionEntries()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest();

        var kinds = manifest.Models.Select(m => m.Kind).ToList();
        Assert.IsTrue(kinds.Contains(NodalOsPaddleOcrOnnxModelKind.TextDetection));
        Assert.IsTrue(kinds.Contains(NodalOsPaddleOcrOnnxModelKind.TextRecognition));
    }

    [TestMethod]
    public void Catalog_SeparatesRequiredAndOptionalModels()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var cat = catalog.CreateDefaultCatalog();

        Assert.IsTrue(cat.RequiredModels.Count >= 2);
        Assert.IsTrue(cat.OptionalModels.Count >= 1);
        Assert.IsTrue(cat.RequiredModels.All(m => m.Kind is NodalOsPaddleOcrOnnxModelKind.TextDetection or NodalOsPaddleOcrOnnxModelKind.TextRecognition));
    }

    [TestMethod]
    public void MissingModel_Readiness_IsMissing()
    {
        var service = new NodalOsPaddleOcrOnnxModelReadinessService();
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var tempRoot = CreateTempRepoRoot();

        try
        {
            var readiness = service.Evaluate(manifest, tempRoot, licenseAccepted: true);

            Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.Missing, readiness.Status);
            Assert.IsFalse(readiness.CanRunOcr);
            Assert.IsTrue(readiness.NoAuthority);
        }
        finally
        {
            DeleteTempRepoRoot(tempRoot);
        }
    }

    [TestMethod]
    public void DownloadedWithoutChecksum_IsInvalid()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var tempRoot = CreateTempRepoRoot();
        var modelDir = Path.Combine(tempRoot, "tools", "ocr-worker", "models", "onnx");
        var path = Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx");
        File.WriteAllText(path, "fake-model-no-checksum");
        try
        {
            var verifier = new NodalOsPaddleOcrOnnxModelVerifierService();
            var result = verifier.Verify(manifest.Models[0], tempRoot, licenseAccepted: true);

            Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.Invalid, result.Status);
            Assert.IsFalse(result.ChecksumMatches);
        }
        finally
        {
            DeleteTempRepoRoot(tempRoot);
        }
    }

    [TestMethod]
    public void ChecksumMismatch_IsInvalid()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var tempRoot = CreateTempRepoRoot();
        var modelDir = Path.Combine(tempRoot, "tools", "ocr-worker", "models", "onnx");
        var path = Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx");
        File.WriteAllText(path, "fake-model-wrong-checksum");
        try
        {
            var manifestWithChecksum = catalog.UpdateModelStatus(
                manifest,
                "paddleocr-det-onnx",
                NodalOsPaddleOcrOnnxModelStatus.Downloaded,
                fileSizeBytes: new FileInfo(path).Length,
                checksum: "0000000000000000000000000000000000000000000000000000000000000000");

            var verifier = new NodalOsPaddleOcrOnnxModelVerifierService();
            var result = verifier.Verify(manifestWithChecksum.Models[0], tempRoot, licenseAccepted: true);

            Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.Invalid, result.Status);
            Assert.IsFalse(result.ChecksumMatches);
        }
        finally
        {
            DeleteTempRepoRoot(tempRoot);
        }
    }

    [TestMethod]
    public void OversizedModel_IsBlockedBySize()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true, TotalMaxSizeBytes = 1 };
        var tempRoot = CreateTempRepoRoot();
        var modelDir = Path.Combine(tempRoot, "tools", "ocr-worker", "models", "onnx");
        var detPath = Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx");
        var recPath = Path.Combine(modelDir, "ch_PP-OCRv4_rec.onnx");
        File.WriteAllText(detPath, "det");
        File.WriteAllText(recPath, "rec");
        try
        {
            var detSize = new FileInfo(detPath).Length;
            var recSize = new FileInfo(recPath).Length;
            var detHash = NodalOsPaddleOcrOnnxModelCatalogService.ComputeChecksum(detPath);
            var recHash = NodalOsPaddleOcrOnnxModelCatalogService.ComputeChecksum(recPath);

            var verified = catalog.UpdateModelStatus(manifest, "paddleocr-det-onnx", NodalOsPaddleOcrOnnxModelStatus.Downloaded, detSize, detHash);
            verified = catalog.UpdateModelStatus(verified, "paddleocr-rec-onnx", NodalOsPaddleOcrOnnxModelStatus.Downloaded, recSize, recHash);

            var service = new NodalOsPaddleOcrOnnxModelReadinessService();
            var readiness = service.Evaluate(verified, tempRoot, licenseAccepted: true);

            Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.BlockedBySize, readiness.Status);
            Assert.IsFalse(readiness.CanRunOcr);
        }
        finally
        {
            DeleteTempRepoRoot(tempRoot);
        }
    }

    [TestMethod]
    public void LicenseNotAccepted_IsBlockedByLicense()
    {
        var service = new NodalOsPaddleOcrOnnxModelReadinessService();
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest();

        var readiness = service.Evaluate(manifest, RepoRoot, licenseAccepted: false);

        Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.BlockedByLicense, readiness.Status);
        Assert.IsFalse(readiness.CanRunOcr);
    }

    [TestMethod]
    public void VerifiedModel_RequiresFileAndMatchingHash()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var tempRoot = CreateTempRepoRoot();
        var modelDir = Path.Combine(tempRoot, "tools", "ocr-worker", "models", "onnx");
        var path = Path.Combine(modelDir, "ch_PP-OCRv4_det.onnx");
        File.WriteAllText(path, "verified-model-content");
        try
        {
            var fileSize = new FileInfo(path).Length;
            var checksum = NodalOsPaddleOcrOnnxModelCatalogService.ComputeChecksum(path);
            var manifestWithChecksum = catalog.UpdateModelStatus(
                manifest,
                "paddleocr-det-onnx",
                NodalOsPaddleOcrOnnxModelStatus.Downloaded,
                fileSize,
                checksum);

            var verifier = new NodalOsPaddleOcrOnnxModelVerifierService();
            var result = verifier.Verify(manifestWithChecksum.Models[0], tempRoot, licenseAccepted: true);

            Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.Verified, result.Status);
            Assert.IsTrue(result.FileExists);
            Assert.IsTrue(result.SizeMatches);
            Assert.IsTrue(result.ChecksumMatches);
        }
        finally
        {
            DeleteTempRepoRoot(tempRoot);
        }
    }

    [TestMethod]
    public void NoOcrExecution_InM197()
    {
        var service = new NodalOsPaddleOcrOnnxModelReadinessService();
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var tempRoot = CreateTempRepoRoot();

        try
        {
            var readiness = service.Evaluate(manifest, tempRoot, licenseAccepted: true);

            Assert.IsFalse(readiness.CanRunOcr);
        }
        finally
        {
            DeleteTempRepoRoot(tempRoot);
        }
    }

    [TestMethod]
    public void NoAuthority_Preserved()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var cat = catalog.CreateDefaultCatalog();
        var service = new NodalOsPaddleOcrOnnxModelReadinessService();
        var manifest = catalog.CreateDefaultManifest();

        var readiness = service.Evaluate(manifest, RepoRoot, licenseAccepted: false);

        Assert.IsTrue(cat.NoAuthority);
        Assert.IsTrue(readiness.NoAuthority);
    }

    [TestMethod]
    public void VerificationReport_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "reports", "paddleocr-onnx-model-verification-m197.md");
        Assert.IsTrue(File.Exists(path));
    }
}
