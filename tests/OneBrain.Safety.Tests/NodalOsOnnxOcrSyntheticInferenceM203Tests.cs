using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxOcrSyntheticInference")]
[TestCategory("OnnxOcrEndToEnd")]
[TestCategory("OnnxOcrDetectionInference")]
[TestCategory("OnnxOcrRecognitionInference")]
[TestCategory("OnnxRuntimeDotNet")]
[TestCategory("PaddleOcrOnnxModelVerification")]
[TestCategory("PaddleOcrOnnxModelReadiness")]
[TestCategory("PixelRedaction")]
[TestCategory("ImagePixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxOcrSyntheticInferenceM203Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static NodalOsPaddleOcrOnnxModelManifest DefaultManifest()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        return catalog.LoadManifestFromFile(Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json"))
               ?? catalog.CreateDefaultManifest();
    }

    private static bool ModelsPresent()
    {
        var manifest = DefaultManifest();
        return manifest.Models.All(m => File.Exists(Path.Combine(RepoRoot, m.LocalRelativePath)));
    }

    private static byte[] SyntheticStripeCrop(int width, int height)
    {
        var bytes = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var dark = (y / 4) % 2 == 0;
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width + x) * 4;
                var value = dark ? (byte)0 : (byte)255;
                bytes[idx + 0] = value;
                bytes[idx + 1] = value;
                bytes[idx + 2] = value;
                bytes[idx + 3] = 255;
            }
        }
        return bytes;
    }

    private static NodalOsPixelRedactionResult CleanRedaction(byte[] imageBytes)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = Convert.ToHexStringLower(sha.ComputeHash(imageBytes));
        return new NodalOsPixelRedactionResult(
            $"redact-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            hash,
            hash,
            [],
            NodalOsPixelRedactionVerificationStatus.Verified,
            SafeForOcr: true,
            SafeForPersistence: false,
            OriginalRawPersisted: false,
            NoAuthority: true,
            [],
            [],
            new NodalOsPixelRedactionEvidence(
                $"ev-{Guid.NewGuid():N}",
                hash,
                hash,
                OriginalRawPersisted: false,
                [],
                "synthetic clean crop",
                Redacted: true),
            Redacted: true)
        {
            RedactedImageBytesForOcrHandoff = imageBytes
        };
    }

    private static NodalOsOnnxOcrSyntheticInferenceRequest Request(byte[] imageBytes, int width, int height, NodalOsOcrVisionSensitivity sensitivity = NodalOsOcrVisionSensitivity.Low)
    {
        return new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            imageBytes,
            width,
            height,
            CleanRedaction(imageBytes),
            sensitivity,
            AllowFullScreen: false,
            AllowRawPersistence: false,
            ProductionMode: false,
            Language: "en",
            ModelSetId: "paddleocr-onnx-v4-en");
    }

    [TestMethod]
    public void InferenceRequest_RequiresPixelRedactionV2()
    {
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(64, 32);
        var badRequest = new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            image,
            64,
            32,
            PixelRedactionResult: null!,
            NodalOsOcrVisionSensitivity.Low,
            AllowFullScreen: false,
            AllowRawPersistence: false,
            ProductionMode: false,
            Language: "en",
            ModelSetId: "paddleocr-onnx-v4-en");

        var result = pipeline.Run(badRequest, RepoRoot);

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.BlockedByRedaction, result.Status);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void MissingModels_Blocked_Honestly()
    {
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(64, 32);
        var request = Request(image, 64, 32);

        var result = pipeline.Run(request, Path.Combine(Path.GetTempPath(), $"nodal-os-no-models-{Guid.NewGuid():N}"));

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.ModelMissing, result.Status);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.CallsRealOcr);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void FullScreen_Blocked()
    {
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(64, 32);
        var request = Request(image, 64, 32) with { AllowFullScreen = true };

        var result = pipeline.Run(request, RepoRoot);

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.BlockedByFullScreen, result.Status);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void SensitiveSurface_Blocked()
    {
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(64, 32);
        var request = Request(image, 64, 32, NodalOsOcrVisionSensitivity.SensitiveSurface);

        var result = pipeline.Run(request, RepoRoot);

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.BlockedBySensitive, result.Status);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void RawPersistence_Blocked()
    {
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(64, 32);
        var request = Request(image, 64, 32) with { AllowRawPersistence = true };

        var result = pipeline.Run(request, RepoRoot);

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.BlockedByRawPersistence, result.Status);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void ProductionMode_Blocked()
    {
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(64, 32);
        var request = Request(image, 64, 32) with { ProductionMode = true };

        var result = pipeline.Run(request, RepoRoot);

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.BlockedByPolicy, result.Status);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void DetectionInference_UsesVerifiedModel()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping detection inference test.");
            return;
        }

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(128, 64);
        var request = Request(image, 128, 64);

        var result = pipeline.Run(request, RepoRoot);

        Assert.IsTrue(result.CallsRealOcr);
        Assert.IsNotNull(result.DetectionResult);
        Assert.IsTrue(result.DetectionResult.ModelId.Contains("det", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(result.NoAuthority);
        Assert.IsFalse(result.CallsSaas);
        Assert.IsFalse(result.RawPersisted);
    }

    [TestMethod]
    public void RecognitionInference_UsesVerifiedModel_WhenBoxesExist()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping recognition inference test.");
            return;
        }

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(128, 64);
        var request = Request(image, 128, 64);

        var result = pipeline.Run(request, RepoRoot);

        if (result.DetectionResult.TextBoxes.Count == 0)
        {
            Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.NoTextDetected, result.Status);
        }
        else
        {
            Assert.IsTrue(result.RecognitionResults.Count > 0);
            Assert.IsTrue(result.RecognitionResults.All(r => r.ModelId.Contains("rec", StringComparison.OrdinalIgnoreCase)));
        }
    }

    [TestMethod]
    public void Pipeline_HandlesNoBoxes_Honestly()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping no-boxes test.");
            return;
        }

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        // Uniform white image should produce no text regions.
        var image = Enumerable.Repeat((byte)255, 64 * 32 * 4).ToArray();
        var request = Request(image, 64, 32);

        var result = pipeline.Run(request, RepoRoot);

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.NoTextDetected, result.Status);
        Assert.AreEqual(0, result.DetectionResult.TextBoxes.Count);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void Pipeline_NoRawPersistence()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping test.");
            return;
        }

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(128, 64);
        var request = Request(image, 128, 64);

        var result = pipeline.Run(request, RepoRoot);

        Assert.IsFalse(result.RawPersisted);
        Assert.IsFalse(result.Evidence.OriginalRawPersisted);
    }

    [TestMethod]
    public void Pipeline_NoAuthority_Preserved()
    {
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = SyntheticStripeCrop(64, 32);
        var request = Request(image, 64, 32);

        var result = pipeline.Run(request, RepoRoot);

        Assert.IsTrue(result.NoAuthority);
        Assert.IsFalse(result.CallsSaas);
    }

}
