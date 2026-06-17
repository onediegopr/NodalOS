using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M203 — ONNX synthetic redacted-crop OCR inference pipeline.
// Runs real ONNX Runtime detection + recognition on a synthetic/redacted crop only.
// Honest reporting: no boxes => NoTextDetected; low confidence => human review.
public sealed class NodalOsOnnxOcrSyntheticInferencePipeline
{
    private readonly NodalOsPaddleOcrOnnxModelCatalogService _catalog = new();
    private readonly NodalOsPaddleOcrOnnxModelVerifierService _verifier = new();
    private readonly NodalOsOnnxOcrImagePreProcessor _imagePreProcessor = new();
    private readonly NodalOsOnnxOcrDetectorPreProcessor _detectorPreProcessor = new();
    private readonly NodalOsOnnxOcrDetectorPostProcessor _detectorPostProcessor = new();
    private readonly NodalOsOnnxOcrRecognizerPreProcessor _recognizerPreProcessor = new();

    public NodalOsOnnxOcrSyntheticInferenceResult Run(
        NodalOsOnnxOcrSyntheticInferenceRequest request,
        string repositoryRoot)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();

        var authorization = Authorize(request);
        if (authorization.Status != NodalOsOnnxOcrInferenceStatus.Success)
        {
            stopwatch.Stop();
            return Failure(request, authorization.Status, authorization.Reason, [], null, stopwatch.Elapsed);
        }

        var manifest = _catalog.LoadManifestFromFile(Path.Combine(repositoryRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json"))
                       ?? _catalog.CreateDefaultManifest();
        manifest = manifest with { LicenseReviewed = true };

        var verifiedManifest = _verifier.VerifyManifest(manifest, repositoryRoot, licenseAccepted: true);
        var catalog = _catalog.BuildCatalog(verifiedManifest);

        if (!catalog.AllRequiredPresent)
        {
            stopwatch.Stop();
            return Failure(request, NodalOsOnnxOcrInferenceStatus.ModelMissing, "required detection/recognition models missing", [], null, stopwatch.Elapsed);
        }

        if (!catalog.AllRequiredVerified)
        {
            stopwatch.Stop();
            var first = catalog.RequiredModels.First(m => m.Status != NodalOsPaddleOcrOnnxModelStatus.Verified);
            return Failure(request, NodalOsOnnxOcrInferenceStatus.ModelUnverified, $"model {first.ModelId} not verified: {first.Status}", [], null, stopwatch.Elapsed);
        }

        var detectionModel = verifiedManifest.Models.First(m => m.Kind == NodalOsPaddleOcrOnnxModelKind.TextDetection);
        var recognitionModel = verifiedManifest.Models.First(m => m.Kind == NodalOsPaddleOcrOnnxModelKind.TextRecognition);

        var imagePrep = _imagePreProcessor.Prepare(
            request.RedactedImageBytes,
            request.Width,
            request.Height,
            request.PixelRedactionResult,
            request.Sensitivity,
            request.AllowFullScreen,
            request.AllowRawPersistence);

        if (imagePrep.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
        {
            stopwatch.Stop();
            return Failure(request, MapPreProcessingStatus(imagePrep.Status), imagePrep.Reason, [], null, stopwatch.Elapsed);
        }

        // Detector: use model expected size if static, otherwise round image to 32.
        var detTarget = ComputeDetectorTargetSize(detectionModel, imagePrep.Width, imagePrep.Height);
        var detPrep = _detectorPreProcessor.Prepare(imagePrep, detTarget.Width, detTarget.Height);
        if (detPrep.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
        {
            stopwatch.Stop();
            return Failure(request, MapPreProcessingStatus(detPrep.Status), detPrep.Reason, [], null, stopwatch.Elapsed);
        }

        var detResult = RunDetection(detectionModel, detPrep, request.Width, request.Height, repositoryRoot);
        if (detResult.Status is NodalOsOnnxOcrInferenceStatus.SessionLoadFailed or NodalOsOnnxOcrInferenceStatus.DetectionFailed)
        {
            stopwatch.Stop();
            return Failure(request, detResult.Status, detResult.Reason, [], detResult, stopwatch.Elapsed);
        }

        var recResults = new List<NodalOsOnnxOcrRecognitionInferenceResult>();
        if (detResult.TextBoxes.Count == 0)
        {
            warnings.Add("detector found no text regions above threshold on synthetic crop");
        }
        else
        {
            foreach (var box in detResult.TextBoxes.Where(b => b.Valid))
            {
                var recResult = RunRecognitionForBox(recognitionModel, imagePrep, box, repositoryRoot);
                recResults.Add(recResult);
            }
        }

        stopwatch.Stop();

        var status = DetermineStatus(detResult, recResults);
        var requiresHumanReview = status is NodalOsOnnxOcrInferenceStatus.LowConfidence or NodalOsOnnxOcrInferenceStatus.RequiresHumanReview;
        var success = status is NodalOsOnnxOcrInferenceStatus.Success or NodalOsOnnxOcrInferenceStatus.NoTextDetected or NodalOsOnnxOcrInferenceStatus.LowConfidence or NodalOsOnnxOcrInferenceStatus.RequiresHumanReview;

        var evidence = BuildEvidence(request, detResult, recResults, stopwatch.ElapsedMilliseconds);

        return new NodalOsOnnxOcrSyntheticInferenceResult(
            $"ocr-inference-{Guid.NewGuid():N}",
            status,
            success,
            detResult,
            recResults,
            evidence,
            CallsRealOcr: true,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            requiresHumanReview,
            stopwatch.Elapsed,
            warnings);
    }

    private static (NodalOsOnnxOcrInferenceStatus Status, string Reason) Authorize(NodalOsOnnxOcrSyntheticInferenceRequest request)
    {
        if (request.ProductionMode)
            return (NodalOsOnnxOcrInferenceStatus.BlockedByPolicy, "production OCR blocked");

        if (request.AllowFullScreen)
            return (NodalOsOnnxOcrInferenceStatus.BlockedByFullScreen, "full-screen OCR blocked");

        if (request.AllowRawPersistence)
            return (NodalOsOnnxOcrInferenceStatus.BlockedByRawPersistence, "raw persistence blocked");

        if (request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface)
            return (NodalOsOnnxOcrInferenceStatus.BlockedBySensitive, "sensitive surface blocked");

        if (request.PixelRedactionResult is null)
            return (NodalOsOnnxOcrInferenceStatus.BlockedByRedaction, "pixel redaction V2 result required");

        if (request.PixelRedactionResult.OriginalRawPersisted)
            return (NodalOsOnnxOcrInferenceStatus.BlockedByRawPersistence, "original raw image persisted");

        if (!request.PixelRedactionResult.SafeForOcr ||
            request.PixelRedactionResult.Decision is NodalOsPixelRedactionDecision.RedactionFailed or NodalOsPixelRedactionDecision.BlockedSensitive)
        {
            return (NodalOsOnnxOcrInferenceStatus.BlockedByRedaction, "pixel redaction decision does not allow OCR");
        }

        return (NodalOsOnnxOcrInferenceStatus.Success, "authorized for synthetic redacted crop inference");
    }

    private static NodalOsOnnxOcrInferenceStatus MapPreProcessingStatus(NodalOsOnnxOcrPreProcessingStatus status)
    {
        return status switch
        {
            NodalOsOnnxOcrPreProcessingStatus.MissingRedaction => NodalOsOnnxOcrInferenceStatus.BlockedByRedaction,
            NodalOsOnnxOcrPreProcessingStatus.BlockedSensitive => NodalOsOnnxOcrInferenceStatus.BlockedBySensitive,
            NodalOsOnnxOcrPreProcessingStatus.BlockedFullScreen => NodalOsOnnxOcrInferenceStatus.BlockedByFullScreen,
            NodalOsOnnxOcrPreProcessingStatus.BlockedRawPersistence => NodalOsOnnxOcrInferenceStatus.BlockedByRawPersistence,
            NodalOsOnnxOcrPreProcessingStatus.UnsupportedFormat => NodalOsOnnxOcrInferenceStatus.PreProcessingFailed,
            NodalOsOnnxOcrPreProcessingStatus.InvalidImage => NodalOsOnnxOcrInferenceStatus.PreProcessingFailed,
            _ => NodalOsOnnxOcrInferenceStatus.PreProcessingFailed
        };
    }

    private static (int Width, int Height) ComputeDetectorTargetSize(NodalOsPaddleOcrOnnxModelRef model, int imageWidth, int imageHeight)
    {
        var expected = model.ExpectedInputShape?.ToArray();
        if (expected is { Length: 4 } && expected[2] > 0 && expected[3] > 0)
        {
            // Use expected static size if image is smaller; otherwise scale up preserving aspect.
            var targetHeight = Math.Max(expected[2], RoundTo32(imageHeight));
            var targetWidth = Math.Max(expected[3], RoundTo32(imageWidth));
            return (targetWidth, targetHeight);
        }

        return (RoundTo32(imageWidth), RoundTo32(imageHeight));
    }

    private static int RoundTo32(int value)
    {
        return (int)(Math.Ceiling((double)value / 32) * 32);
    }

    private NodalOsOnnxOcrDetectionInferenceResult RunDetection(
        NodalOsPaddleOcrOnnxModelRef model,
        NodalOsOnnxOcrDetectorPreProcessingResult prep,
        int cropWidth,
        int cropHeight,
        string repositoryRoot)
    {
        var sw = Stopwatch.StartNew();
        var modelPath = Path.GetFullPath(Path.Combine(repositoryRoot, model.LocalRelativePath));

        try
        {
            using var session = new InferenceSession(modelPath);
            var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? model.InputNames.FirstOrDefault() ?? "x";
            var inputTensor = new DenseTensor<float>(prep.InputTensor, prep.InputShape);
            var input = NamedOnnxValue.CreateFromTensor(inputName, inputTensor);

            using var outputs = session.Run(new[] { input });
            var output = outputs.First();
            var outputTensor = output.AsTensor<float>();
            if (outputTensor is null)
            {
                sw.Stop();
                return DetectionResult(NodalOsOnnxOcrInferenceStatus.DetectionFailed, [], sw.ElapsedMilliseconds, model, "detector output is not a float tensor");
            }

            var outputShape = outputTensor.Dimensions.ToArray();
            var outputData = outputTensor.ToArray();

            var decoded = _detectorPostProcessor.Decode(outputData, outputShape, cropWidth, cropHeight, threshold: 0.3f);

            sw.Stop();

            return decoded.Status switch
            {
                NodalOsOnnxOcrPostProcessingStatus.Success => DetectionResult(NodalOsOnnxOcrInferenceStatus.Success, decoded.TextBoxes, sw.ElapsedMilliseconds, model, "detection completed"),
                NodalOsOnnxOcrPostProcessingStatus.Empty => DetectionResult(NodalOsOnnxOcrInferenceStatus.NoTextDetected, decoded.TextBoxes, sw.ElapsedMilliseconds, model, "no text regions above threshold"),
                NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape => DetectionResult(NodalOsOnnxOcrInferenceStatus.DetectionFailed, [], sw.ElapsedMilliseconds, model, $"unsupported detector output shape [{string.Join(",", outputShape)}]"),
                _ => DetectionResult(NodalOsOnnxOcrInferenceStatus.DetectionFailed, [], sw.ElapsedMilliseconds, model, decoded.Reason)
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return DetectionResult(NodalOsOnnxOcrInferenceStatus.SessionLoadFailed, [], sw.ElapsedMilliseconds, model, $"detection inference failed: {ex.Message}");
        }
    }

    private NodalOsOnnxOcrRecognitionInferenceResult RunRecognitionForBox(
        NodalOsPaddleOcrOnnxModelRef model,
        NodalOsOnnxOcrImagePreProcessingResult fullImage,
        NodalOsOnnxOcrTextBox box,
        string repositoryRoot)
    {
        var sw = Stopwatch.StartNew();
        var modelPath = Path.GetFullPath(Path.Combine(repositoryRoot, model.LocalRelativePath));

        try
        {
            var crop = ExtractCrop(fullImage, box);
            if (crop is null)
            {
                sw.Stop();
                return RecognitionResult(NodalOsOnnxOcrInferenceStatus.RecognitionFailed, [], sw.ElapsedMilliseconds, model, "box crop extraction failed");
            }

            var recPrep = _recognizerPreProcessor.Prepare(crop, maxWidth: 320);
            if (recPrep.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
            {
                sw.Stop();
                return RecognitionResult(NodalOsOnnxOcrInferenceStatus.RecognitionFailed, [], sw.ElapsedMilliseconds, model, recPrep.Reason);
            }

            using var session = new InferenceSession(modelPath);
            var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? model.InputNames.FirstOrDefault() ?? "x";
            var inputTensor = new DenseTensor<float>(recPrep.InputTensor, recPrep.InputShape);
            var input = NamedOnnxValue.CreateFromTensor(inputName, inputTensor);

            using var outputs = session.Run(new[] { input });
            var output = outputs.First();
            var outputTensor = output.AsTensor<float>();
            if (outputTensor is null)
            {
                sw.Stop();
                return RecognitionResult(NodalOsOnnxOcrInferenceStatus.RecognitionFailed, [], sw.ElapsedMilliseconds, model, "recognizer output is not a float tensor");
            }

            var outputShape = outputTensor.Dimensions.ToArray();
            var outputData = outputTensor.ToArray();

            var dictionary = new NodalOsOnnxOcrCharacterDictionary().Load("en-ascii", "en");
            var decoder = new NodalOsOnnxOcrRecognizerPostProcessor(dictionary);
            var decoded = decoder.Decode(outputData, outputShape, confidenceThreshold: 0.6);

            sw.Stop();

            return decoded.Status switch
            {
                NodalOsOnnxOcrPostProcessingStatus.Success => RecognitionResult(NodalOsOnnxOcrInferenceStatus.Success, decoded.Candidates, sw.ElapsedMilliseconds, model, "recognition completed"),
                NodalOsOnnxOcrPostProcessingStatus.RequiresHumanReview => RecognitionResult(NodalOsOnnxOcrInferenceStatus.LowConfidence, decoded.Candidates, sw.ElapsedMilliseconds, model, "low confidence; requires human review"),
                NodalOsOnnxOcrPostProcessingStatus.RecognitionEmpty => RecognitionResult(NodalOsOnnxOcrInferenceStatus.RecognitionEmpty, decoded.Candidates, sw.ElapsedMilliseconds, model, "recognizer decoded empty text"),
                NodalOsOnnxOcrPostProcessingStatus.DictionaryMismatch => RecognitionResult(NodalOsOnnxOcrInferenceStatus.DictionaryMismatch, decoded.Candidates, sw.ElapsedMilliseconds, model, "recognizer dictionary mismatch"),
                NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape => RecognitionResult(NodalOsOnnxOcrInferenceStatus.RecognitionFailed, [], sw.ElapsedMilliseconds, model, $"unsupported recognizer output shape [{string.Join(",", outputShape)}]"),
                _ => RecognitionResult(NodalOsOnnxOcrInferenceStatus.RecognitionFailed, [], sw.ElapsedMilliseconds, model, decoded.Reason)
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return RecognitionResult(NodalOsOnnxOcrInferenceStatus.RecognitionFailed, [], sw.ElapsedMilliseconds, model, $"recognition inference failed: {ex.Message}");
        }
    }

    private static NodalOsOnnxOcrImagePreProcessingResult? ExtractCrop(NodalOsOnnxOcrImagePreProcessingResult image, NodalOsOnnxOcrTextBox box)
    {
        var x = Math.Clamp(box.CropX, 0, image.Width - 1);
        var y = Math.Clamp(box.CropY, 0, image.Height - 1);
        var w = Math.Min(box.CropWidth, image.Width - x);
        var h = Math.Min(box.CropHeight, image.Height - y);

        if (w <= 0 || h <= 0)
            return null;

        var cropData = new float[w * h * 4];
        for (var row = 0; row < h; row++)
        {
            for (var col = 0; col < w; col++)
            {
                var srcIdx = ((y + row) * image.Width + (x + col)) * 4;
                var dstIdx = (row * w + col) * 4;
                cropData[dstIdx + 0] = image.NormalizedData[srcIdx + 0];
                cropData[dstIdx + 1] = image.NormalizedData[srcIdx + 1];
                cropData[dstIdx + 2] = image.NormalizedData[srcIdx + 2];
                cropData[dstIdx + 3] = image.NormalizedData[srcIdx + 3];
            }
        }

        return new NodalOsOnnxOcrImagePreProcessingResult(
            $"crop-{Guid.NewGuid():N}",
            NodalOsOnnxOcrPreProcessingStatus.Success,
            cropData,
            1,
            4,
            h,
            w,
            ScaleX: 1.0f,
            ScaleY: 1.0f,
            PadLeft: 0,
            PadTop: 0,
            image.SourceFormat,
            w,
            h,
            NoAuthority: true,
            Redacted: true,
            "box crop extracted from redacted image");
    }

    private static NodalOsOnnxOcrInferenceStatus DetermineStatus(
        NodalOsOnnxOcrDetectionInferenceResult detection,
        IReadOnlyList<NodalOsOnnxOcrRecognitionInferenceResult> recognitions)
    {
        if (detection.Status == NodalOsOnnxOcrInferenceStatus.NoTextDetected)
            return NodalOsOnnxOcrInferenceStatus.NoTextDetected;

        if (detection.Status != NodalOsOnnxOcrInferenceStatus.Success)
            return detection.Status;

        if (recognitions.Count == 0)
            return NodalOsOnnxOcrInferenceStatus.NoTextDetected;

        if (recognitions.All(r => r.Status == NodalOsOnnxOcrInferenceStatus.RecognitionFailed))
            return NodalOsOnnxOcrInferenceStatus.RecognitionFailed;

        if (recognitions.Any(r => r.Status == NodalOsOnnxOcrInferenceStatus.LowConfidence))
            return NodalOsOnnxOcrInferenceStatus.LowConfidence;

        if (recognitions.Any(r => r.Status == NodalOsOnnxOcrInferenceStatus.RequiresHumanReview))
            return NodalOsOnnxOcrInferenceStatus.RequiresHumanReview;

        return NodalOsOnnxOcrInferenceStatus.Success;
    }

    private static NodalOsOnnxOcrEndToEndEvidence BuildEvidence(
        NodalOsOnnxOcrSyntheticInferenceRequest request,
        NodalOsOnnxOcrDetectionInferenceResult detection,
        IReadOnlyList<NodalOsOnnxOcrRecognitionInferenceResult> recognitions,
        long totalTimeMs)
    {
        var recResultId = recognitions.Count > 0 ? recognitions[0].ResultId : null;
        return new NodalOsOnnxOcrEndToEndEvidence(
            $"ocr-evidence-{Guid.NewGuid():N}",
            request.PixelRedactionResult.RedactedImageHash,
            detection.ResultId,
            recResultId,
            new[] { "paddleocr-det-onnx", "paddleocr-rec-onnx" },
            new[] { request.PixelRedactionResult.Evidence.EvidenceId },
            totalTimeMs,
            detection.TextBoxes.Count,
            recognitions.Sum(r => r.Candidates.Count),
            OriginalRawPersisted: false,
            CallsSaas: false,
            ProductionEnabled: false,
            NoAuthority: true,
            $"synthetic redacted crop inference: {detection.TextBoxes.Count} boxes, {recognitions.Count} recognition runs");
    }

    private static NodalOsOnnxOcrSyntheticInferenceResult Failure(
        NodalOsOnnxOcrSyntheticInferenceRequest request,
        NodalOsOnnxOcrInferenceStatus status,
        string reason,
        IReadOnlyList<NodalOsOnnxOcrRecognitionInferenceResult> recognitions,
        NodalOsOnnxOcrDetectionInferenceResult? detection,
        TimeSpan duration)
    {
        var det = detection ?? new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-failure-{Guid.NewGuid():N}",
            status,
            Array.Empty<NodalOsOnnxOcrTextBox>(),
            null,
            "paddleocr-det-onnx",
            "v4",
            BrowserCredentialRedactor.Redact(reason));

        var evidence = new NodalOsOnnxOcrEndToEndEvidence(
            $"ocr-evidence-{Guid.NewGuid():N}",
            request.PixelRedactionResult?.RedactedImageHash ?? "",
            det.ResultId,
            null,
            new[] { "paddleocr-det-onnx", "paddleocr-rec-onnx" },
            Array.Empty<string>(),
            (long?)duration.TotalMilliseconds,
            0,
            0,
            OriginalRawPersisted: false,
            CallsSaas: false,
            ProductionEnabled: false,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(reason));

        return new NodalOsOnnxOcrSyntheticInferenceResult(
            $"ocr-inference-{Guid.NewGuid():N}",
            status,
            Success: false,
            det,
            recognitions,
            evidence,
            CallsRealOcr: !(status == NodalOsOnnxOcrInferenceStatus.ModelMissing || status == NodalOsOnnxOcrInferenceStatus.ModelUnverified),
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            RequiresHumanReview: false,
            duration,
            new[] { BrowserCredentialRedactor.Redact(reason) });
    }

    private static NodalOsOnnxOcrDetectionInferenceResult DetectionResult(
        NodalOsOnnxOcrInferenceStatus status,
        IReadOnlyList<NodalOsOnnxOcrTextBox> boxes,
        long? timeMs,
        NodalOsPaddleOcrOnnxModelRef model,
        string reason)
    {
        return new NodalOsOnnxOcrDetectionInferenceResult(
            $"det-{Guid.NewGuid():N}",
            status,
            boxes,
            timeMs,
            model.ModelId,
            model.Version,
            BrowserCredentialRedactor.Redact(reason));
    }

    private static NodalOsOnnxOcrRecognitionInferenceResult RecognitionResult(
        NodalOsOnnxOcrInferenceStatus status,
        IReadOnlyList<NodalOsOnnxOcrRecognitionCandidate> candidates,
        long? timeMs,
        NodalOsPaddleOcrOnnxModelRef model,
        string reason)
    {
        return new NodalOsOnnxOcrRecognitionInferenceResult(
            $"rec-{Guid.NewGuid():N}",
            status,
            candidates,
            timeMs,
            model.ModelId,
            model.Version,
            BrowserCredentialRedactor.Redact(reason));
    }
}
