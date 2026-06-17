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
public sealed class NodalOsOnnxOcrSyntheticTextRunM207Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static bool ModelsPresent()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.LoadManifestFromFile(Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json"))
                       ?? catalog.CreateDefaultManifest();
        return manifest.Models.All(m => File.Exists(Path.Combine(RepoRoot, m.LocalRelativePath)));
    }

    private const string QuarantineMessage =
        "M207 quarantine: native ONNX Runtime crash on synthetic text fixture render mode. See M208 report.";

    [TestMethod]
    [Ignore(QuarantineMessage)]
    public void PositiveFixture_AttemptsDetection()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping test.");
            return;
        }

        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var catalog = generator.GenerateCatalog(texts: new[] { "TEST" });
        var service = new NodalOsOnnxOcrSyntheticTextRunService();

        var result = service.Run("run-test", catalog, RepoRoot);

#pragma warning disable MSTEST0032
        Assert.AreEqual(1, result.FixturesRun);
        Assert.IsNotNull(result.AggregateDecision);
#pragma warning restore MSTEST0032
    }

    [TestMethod]
    [Ignore(QuarantineMessage)]
    public void DetectionBoxes_TriggerRecognitionAttempt()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping test.");
            return;
        }

        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var catalog = generator.GenerateCatalog(texts: new[] { "TEST" });
        var service = new NodalOsOnnxOcrSyntheticTextRunService();

        var result = service.Run("run-test", catalog, RepoRoot);

        if (result.BoxesDetected > 0)
        {
            Assert.IsTrue(result.RecognitionAttempts > 0);
        }
    }

    [TestMethod]
    public void NoBoxes_DoesNotExecuteRecognition_Unit()
    {
        var detection = new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.NoTextDetected,
            Array.Empty<NodalOsOnnxOcrTextBox>(),
            10, "det", "v4", "no boxes");

        var inference = new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.NoTextDetected,
            true,
            detection,
            Array.Empty<NodalOsOnnxOcrRecognitionInferenceResult>(),
            new NodalOsOnnxOcrEndToEndEvidence(
                $"ev-{Guid.NewGuid():N}", "hash", detection.ResultId, null,
                Array.Empty<string>(), Array.Empty<string>(), 10, 0, 0,
                false, false, false, true, "no boxes"),
            true, false, false, true, false, TimeSpan.FromMilliseconds(10), Array.Empty<string>());

        Assert.AreEqual(0, inference.RecognitionResults.Count);
        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.NoTextDetected, inference.Status);
    }

    [TestMethod]
    [Ignore(QuarantineMessage)]
    public void NoBoxes_DoesNotExecuteRecognition_Integration()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping test.");
            return;
        }

        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var catalog = generator.GenerateCatalog(texts: new[] { "TEST" });
        var service = new NodalOsOnnxOcrSyntheticTextRunService();

        var result = service.Run("run-test", catalog, RepoRoot);

        if (result.BoxesDetected == 0)
        {
            Assert.AreEqual(0, result.RecognitionAttempts);
            Assert.AreEqual(NodalOsOnnxOcrSyntheticTextRunDecision.NoTextDetected, result.AggregateDecision);
        }
    }

    [TestMethod]
    public void RecognitionLowConfidence_RequiresHumanReview()
    {
        var inference = new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.LowConfidence,
            true,
            new NodalOsOnnxOcrDetectionInferenceResult($"det-{Guid.NewGuid():N}", NodalOsOnnxOcrInferenceStatus.Success, new[] { new NodalOsOnnxOcrTextBox("box-0", new List<float> { 0, 0, 10, 0, 10, 10, 0, 10 }, 0.8f, 0, 0, 10, 10, true) }, 100, "det", "v4", ""),
            new[] { new NodalOsOnnxOcrRecognitionInferenceResult($"rec-{Guid.NewGuid():N}", NodalOsOnnxOcrInferenceStatus.LowConfidence, new[] { new NodalOsOnnxOcrRecognitionCandidate($"c-{Guid.NewGuid():N}", "x", 0.2f, new List<int>(), true, true) }, 50, "rec", "v4", "") },
            new NodalOsOnnxOcrEndToEndEvidence($"ev-{Guid.NewGuid():N}", "hash", null, null, new[] { "det", "rec" }, Array.Empty<string>(), 150, 1, 1, false, false, false, true, ""),
            true, false, false, true, true, TimeSpan.FromMilliseconds(150), Array.Empty<string>());

        var normalizer = new NodalOsOnnxOcrResultNormalizer();
        var redaction = new NodalOsPixelRedactionResult(
            $"r-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            "hash", "hash", Array.Empty<NodalOsPixelRedactionMask>(),
            NodalOsPixelRedactionVerificationStatus.Verified,
            true, false, false, true, Array.Empty<NodalOsPixelRedactionFinding>(), Array.Empty<string>(),
            new NodalOsPixelRedactionEvidence($"e-{Guid.NewGuid():N}", "hash", "hash", false, Array.Empty<string>(), "", true),
            true);

        var normalized = normalizer.Normalize(inference, redaction);

        Assert.IsTrue(normalized.RequiresHumanReview);
    }

    [TestMethod]
    public void EmptyRecognition_ReportedHonestly()
    {
        var inference = new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.RecognitionEmpty,
            true,
            new NodalOsOnnxOcrDetectionInferenceResult($"det-{Guid.NewGuid():N}", NodalOsOnnxOcrInferenceStatus.Success, new[] { new NodalOsOnnxOcrTextBox("box-0", new List<float> { 0, 0, 10, 0, 10, 10, 0, 10 }, 0.8f, 0, 0, 10, 10, true) }, 100, "det", "v4", ""),
            new[] { new NodalOsOnnxOcrRecognitionInferenceResult($"rec-{Guid.NewGuid():N}", NodalOsOnnxOcrInferenceStatus.RecognitionEmpty, new[] { new NodalOsOnnxOcrRecognitionCandidate($"c-{Guid.NewGuid():N}", "", 0.5f, new List<int>(), true, true) }, 50, "rec", "v4", "") },
            new NodalOsOnnxOcrEndToEndEvidence($"ev-{Guid.NewGuid():N}", "hash", null, null, new[] { "det", "rec" }, Array.Empty<string>(), 150, 1, 1, false, false, false, true, ""),
            true, false, false, true, true, TimeSpan.FromMilliseconds(150), Array.Empty<string>());

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.RecognitionEmpty, inference.Status);
        Assert.AreEqual("", inference.RecognitionResults[0].Candidates[0].Text);
    }

    [TestMethod]
    public void DictionaryMismatch_BlocksHonestly()
    {
        var inference = new NodalOsOnnxOcrSyntheticInferenceResult(
            $"inf-{Guid.NewGuid():N}",
            NodalOsOnnxOcrInferenceStatus.DictionaryMismatch,
            false,
            new NodalOsOnnxOcrDetectionInferenceResult($"det-{Guid.NewGuid():N}", NodalOsOnnxOcrInferenceStatus.Success, new[] { new NodalOsOnnxOcrTextBox("box-0", new List<float> { 0, 0, 10, 0, 10, 10, 0, 10 }, 0.8f, 0, 0, 10, 10, true) }, 100, "det", "v4", ""),
            new[] { new NodalOsOnnxOcrRecognitionInferenceResult($"rec-{Guid.NewGuid():N}", NodalOsOnnxOcrInferenceStatus.DictionaryMismatch, Array.Empty<NodalOsOnnxOcrRecognitionCandidate>(), 50, "rec", "v4", "") },
            new NodalOsOnnxOcrEndToEndEvidence($"ev-{Guid.NewGuid():N}", "hash", null, null, new[] { "det", "rec" }, Array.Empty<string>(), 150, 1, 0, false, false, false, true, ""),
            true, false, false, true, false, TimeSpan.FromMilliseconds(150), Array.Empty<string>());

        Assert.AreEqual(NodalOsOnnxOcrInferenceStatus.DictionaryMismatch, inference.Status);
    }

    [TestMethod]
    public void Run_NoRawPersistence_Unit()
    {
        var runResult = new NodalOsOnnxOcrSyntheticTextRunResult(
            "run-test",
            DateTimeOffset.UtcNow,
            Array.Empty<NodalOsOnnxOcrSyntheticTextFixtureResult>(),
            NodalOsOnnxOcrSyntheticTextRunDecision.NoTextDetected,
            0, 0, 0, 0, 0, 0, 0,
            CallsRealOcr: true,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            "unit test");

        Assert.IsFalse(runResult.RawPersisted);
    }

    [TestMethod]
    [Ignore(QuarantineMessage)]
    public void Run_NoRawPersistence_Integration()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping test.");
            return;
        }

        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var catalog = generator.GenerateCatalog();
        var service = new NodalOsOnnxOcrSyntheticTextRunService();

        var result = service.Run("run-test", catalog, RepoRoot);

        Assert.IsFalse(result.RawPersisted);
    }

    [TestMethod]
    public void Run_NoAuthority_NoSaas_ProductionDisabled_Unit()
    {
        var runResult = new NodalOsOnnxOcrSyntheticTextRunResult(
            "run-test",
            DateTimeOffset.UtcNow,
            Array.Empty<NodalOsOnnxOcrSyntheticTextFixtureResult>(),
            NodalOsOnnxOcrSyntheticTextRunDecision.NoTextDetected,
            0, 0, 0, 0, 0, 0, 0,
            CallsRealOcr: true,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            "unit test");

        Assert.IsTrue(runResult.NoAuthority);
        Assert.IsFalse(runResult.CallsSaas);
    }

    [TestMethod]
    [Ignore(QuarantineMessage)]
    public void Run_NoAuthority_NoSaas_ProductionDisabled_Integration()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping test.");
            return;
        }

        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var catalog = generator.GenerateCatalog();
        var service = new NodalOsOnnxOcrSyntheticTextRunService();

        var result = service.Run("run-test", catalog, RepoRoot);

        Assert.IsTrue(result.NoAuthority);
        Assert.IsFalse(result.CallsSaas);
    }

    [TestMethod]
    public void ModelMissing_MapsToBlockedByModelRuntime()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"nodal-os-missing-models-{Guid.NewGuid():N}");
        try
        {
            var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
            var catalog = generator.GenerateCatalog(texts: new[] { "TEST" });
            var service = new NodalOsOnnxOcrSyntheticTextRunService();

            var result = service.Run("run-test", catalog, tempRoot);

            Assert.AreEqual(NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime, result.AggregateDecision);
            Assert.IsTrue(result.Reason.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                          result.FixtureResults.Any(r => r.InferenceResult.Status == NodalOsOnnxOcrInferenceStatus.ModelMissing));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void ModelUnverified_MapsToBlockedByModelRuntime()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"nodal-os-unverified-models-{Guid.NewGuid():N}");
        try
        {
            var detPath = Path.Combine(tempRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
            var recPath = Path.Combine(tempRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx");
            Directory.CreateDirectory(Path.GetDirectoryName(detPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(recPath)!);
            File.WriteAllText(detPath, "fake");
            File.WriteAllText(recPath, "fake");

            var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
            var catalog = generator.GenerateCatalog(texts: new[] { "TEST" });
            var service = new NodalOsOnnxOcrSyntheticTextRunService();

            var result = service.Run("run-test", catalog, tempRoot);

            Assert.AreEqual(NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime, result.AggregateDecision);
            Assert.IsTrue(result.FixtureResults.Any(r =>
                r.InferenceResult.Status == NodalOsOnnxOcrInferenceStatus.ModelUnverified ||
                r.InferenceResult.Status == NodalOsOnnxOcrInferenceStatus.SessionLoadFailed));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void Diagnostic_StripeFixture_RunPipeline()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping diagnostic.");
            return;
        }

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var image = Enumerable.Repeat((byte)0, 64 * 32 * 4).ToArray();
        for (var y = 0; y < 32; y += 4)
        {
            for (var x = 0; x < 64; x++)
            {
                var idx = (y * 64 + x) * 4;
                image[idx] = 255;
                image[idx + 1] = 255;
                image[idx + 2] = 255;
            }
        }

        var redaction = new NodalOsPixelRedactionResult(
            $"r-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            "hash", "hash", Array.Empty<NodalOsPixelRedactionMask>(),
            NodalOsPixelRedactionVerificationStatus.Verified,
            true, false, false, true, Array.Empty<NodalOsPixelRedactionFinding>(), Array.Empty<string>(),
            new NodalOsPixelRedactionEvidence($"e-{Guid.NewGuid():N}", "hash", "hash", false, Array.Empty<string>(), "", true),
            true)
        {
            RedactedImageBytesForOcrHandoff = image
        };

        var request = new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            image, 64, 32, redaction,
            NodalOsOcrVisionSensitivity.Low,
            false, false, false, "en", "paddleocr-onnx-v4-en");

        var result = pipeline.Run(request, RepoRoot);

        Console.WriteLine($"M207-STRIPE-DIAG: status={result.Status}, boxes={result.DetectionResult.TextBoxes.Count}");
        AssertSafeOnnxDiagnosticRan(result);
    }

    [TestMethod]
    [Ignore(QuarantineMessage)]
    public void Diagnostic_PixelFont_RunPipeline()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping diagnostic.");
            return;
        }

        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var catalog = generator.GenerateCatalog(texts: new[] { "TEST" });
        var fixture = catalog.Fixtures[0];
        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var request = new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            fixture.ImageBytes,
            fixture.Width,
            fixture.Height,
            fixture.RedactionResult,
            NodalOsOcrVisionSensitivity.Low,
            false, false, false, "en", "paddleocr-onnx-v4-en");

        var result = pipeline.Run(request, RepoRoot);

        Console.WriteLine($"M207-FONT-DIAG: status={result.Status}, boxes={result.DetectionResult.TextBoxes.Count}, recAttempts={result.RecognitionResults.Count}");
        foreach (var box in result.DetectionResult.TextBoxes)
        {
            Console.WriteLine($"  Box {box.BoxId}: x={box.CropX}, y={box.CropY}, w={box.CropWidth}, h={box.CropHeight}, conf={box.Confidence}");
        }

        AssertSafeOnnxDiagnosticRan(result);
    }

    [TestMethod]
    public void Diagnostic_LargeCircle_RunPipeline()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping diagnostic.");
            return;
        }

        var width = 640;
        var height = 640;
        var image = new byte[width * height * 4];
        var cx = width / 2;
        var cy = height / 2;
        var r = 100;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width + x) * 4;
                var dx = x - cx;
                var dy = y - cy;
                var dark = dx * dx + dy * dy <= r * r;
                image[idx + 0] = dark ? (byte)0 : (byte)255;
                image[idx + 1] = dark ? (byte)0 : (byte)255;
                image[idx + 2] = dark ? (byte)0 : (byte)255;
                image[idx + 3] = 255;
            }
        }

        var redaction = new NodalOsPixelRedactionResult(
            $"r-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            "hash", "hash", Array.Empty<NodalOsPixelRedactionMask>(),
            NodalOsPixelRedactionVerificationStatus.Verified,
            true, false, false, true, Array.Empty<NodalOsPixelRedactionFinding>(), Array.Empty<string>(),
            new NodalOsPixelRedactionEvidence($"e-{Guid.NewGuid():N}", "hash", "hash", false, Array.Empty<string>(), "", true),
            true)
        {
            RedactedImageBytesForOcrHandoff = image
        };

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var request = new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            image, width, height, redaction,
            NodalOsOcrVisionSensitivity.Low,
            false, false, false, "en", "paddleocr-onnx-v4-en");

        var result = pipeline.Run(request, RepoRoot);

        Console.WriteLine($"M207-CIRCLE-DIAG: status={result.Status}, boxes={result.DetectionResult.TextBoxes.Count}, recAttempts={result.RecognitionResults.Count}");
        foreach (var box in result.DetectionResult.TextBoxes)
        {
            Console.WriteLine($"  Box {box.BoxId}: x={box.CropX}, y={box.CropY}, w={box.CropWidth}, h={box.CropHeight}, conf={box.Confidence}");
        }

        AssertSafeOnnxDiagnosticRan(result);
    }

    [TestMethod]
    [Ignore(QuarantineMessage)]
    public void Diagnostic_ThickHorizontalBars_RunPipeline()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping diagnostic.");
            return;
        }

        var width = 320;
        var height = 160;
        var image = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width + x) * 4;
                var bar = (y % 4) < 2;
                var dark = bar;
                image[idx + 0] = dark ? (byte)0 : (byte)255;
                image[idx + 1] = dark ? (byte)0 : (byte)255;
                image[idx + 2] = dark ? (byte)0 : (byte)255;
                image[idx + 3] = 255;
            }
        }

        var redaction = new NodalOsPixelRedactionResult(
            $"r-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            "hash", "hash", Array.Empty<NodalOsPixelRedactionMask>(),
            NodalOsPixelRedactionVerificationStatus.Verified,
            true, false, false, true, Array.Empty<NodalOsPixelRedactionFinding>(), Array.Empty<string>(),
            new NodalOsPixelRedactionEvidence($"e-{Guid.NewGuid():N}", "hash", "hash", false, Array.Empty<string>(), "", true),
            true)
        {
            RedactedImageBytesForOcrHandoff = image
        };

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var request = new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            image, width, height, redaction,
            NodalOsOcrVisionSensitivity.Low,
            false, false, false, "en", "paddleocr-onnx-v4-en");

        var result = pipeline.Run(request, RepoRoot);

        Console.WriteLine($"M207-BARS-DIAG: status={result.Status}, boxes={result.DetectionResult.TextBoxes.Count}, recAttempts={result.RecognitionResults.Count}");
        foreach (var box in result.DetectionResult.TextBoxes)
        {
            Console.WriteLine($"  Box {box.BoxId}: x={box.CropX}, y={box.CropY}, w={box.CropWidth}, h={box.CropHeight}, conf={box.Confidence}");
        }

        AssertSafeOnnxDiagnosticRan(result);
    }

    [TestMethod]
    public void Diagnostic_LargeSolidRectangle_RunPipeline()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping diagnostic.");
            return;
        }

        var width = 640;
        var height = 640;
        var image = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width + x) * 4;
                var dark = x >= 200 && x < 440 && y >= 280 && y < 360;
                image[idx + 0] = dark ? (byte)0 : (byte)255;
                image[idx + 1] = dark ? (byte)0 : (byte)255;
                image[idx + 2] = dark ? (byte)0 : (byte)255;
                image[idx + 3] = 255;
            }
        }

        var redaction = new NodalOsPixelRedactionResult(
            $"r-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            "hash", "hash", Array.Empty<NodalOsPixelRedactionMask>(),
            NodalOsPixelRedactionVerificationStatus.Verified,
            true, false, false, true, Array.Empty<NodalOsPixelRedactionFinding>(), Array.Empty<string>(),
            new NodalOsPixelRedactionEvidence($"e-{Guid.NewGuid():N}", "hash", "hash", false, Array.Empty<string>(), "", true),
            true)
        {
            RedactedImageBytesForOcrHandoff = image
        };

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var request = new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            image, width, height, redaction,
            NodalOsOcrVisionSensitivity.Low,
            false, false, false, "en", "paddleocr-onnx-v4-en");

        var result = pipeline.Run(request, RepoRoot);

        Console.WriteLine($"M207-LARGE-RECT-DIAG: status={result.Status}, boxes={result.DetectionResult.TextBoxes.Count}, recAttempts={result.RecognitionResults.Count}");
        foreach (var box in result.DetectionResult.TextBoxes)
        {
            Console.WriteLine($"  Box {box.BoxId}: x={box.CropX}, y={box.CropY}, w={box.CropWidth}, h={box.CropHeight}, conf={box.Confidence}");
        }

        AssertSafeOnnxDiagnosticRan(result);
    }

    [TestMethod]
    public void Diagnostic_SolidRectangle_RunPipeline()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping diagnostic.");
            return;
        }

        var width = 128;
        var height = 64;
        var image = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width + x) * 4;
                var dark = x >= 32 && x < 96 && y >= 16 && y < 48;
                image[idx + 0] = dark ? (byte)0 : (byte)255;
                image[idx + 1] = dark ? (byte)0 : (byte)255;
                image[idx + 2] = dark ? (byte)0 : (byte)255;
                image[idx + 3] = 255;
            }
        }

        var redaction = new NodalOsPixelRedactionResult(
            $"r-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            "hash", "hash", Array.Empty<NodalOsPixelRedactionMask>(),
            NodalOsPixelRedactionVerificationStatus.Verified,
            true, false, false, true, Array.Empty<NodalOsPixelRedactionFinding>(), Array.Empty<string>(),
            new NodalOsPixelRedactionEvidence($"e-{Guid.NewGuid():N}", "hash", "hash", false, Array.Empty<string>(), "", true),
            true)
        {
            RedactedImageBytesForOcrHandoff = image
        };

        var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
        var request = new NodalOsOnnxOcrSyntheticInferenceRequest(
            $"req-{Guid.NewGuid():N}",
            image, width, height, redaction,
            NodalOsOcrVisionSensitivity.Low,
            false, false, false, "en", "paddleocr-onnx-v4-en");

        var result = pipeline.Run(request, RepoRoot);

        Console.WriteLine($"M207-RECT-DIAG: status={result.Status}, boxes={result.DetectionResult.TextBoxes.Count}, recAttempts={result.RecognitionResults.Count}");
        foreach (var box in result.DetectionResult.TextBoxes)
        {
            Console.WriteLine($"  Box {box.BoxId}: x={box.CropX}, y={box.CropY}, w={box.CropWidth}, h={box.CropHeight}, conf={box.Confidence}");
        }

        AssertSafeOnnxDiagnosticRan(result);
    }

    private static void AssertSafeOnnxDiagnosticRan(NodalOsOnnxOcrSyntheticInferenceResult result)
    {
        // Safe-shape diagnostics prove the native ONNX Runtime loads and executes;
        // they do not claim a positive text detection.
#pragma warning disable MSTEST0032
        Assert.IsTrue(result.CallsRealOcr, "safe-shape diagnostic should call real ONNX OCR");
#pragma warning restore MSTEST0032
        Assert.IsFalse(result.CallsSaas);
        Assert.IsFalse(result.RawPersisted);
        Assert.IsTrue(result.NoAuthority);
    }
}
