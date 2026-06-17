using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M207 — Positive ONNX OCR synthetic text run service.
// Runs the M203 inference pipeline over M206 fixtures and reports honest aggregate results.
public sealed class NodalOsOnnxOcrSyntheticTextRunService
{
    private readonly NodalOsOnnxOcrSyntheticInferencePipeline _pipeline = new();
    private readonly NodalOsSyntheticOcrTextFixtureGenerator _fixtureGenerator = new();

    public NodalOsOnnxOcrSyntheticTextRunResult Run(
        string runId,
        NodalOsSyntheticOcrTextFixtureCatalog catalog,
        string repositoryRoot)
    {
        var results = new List<NodalOsOnnxOcrSyntheticTextFixtureResult>();
        var boxesDetected = 0;
        var recognitionAttempts = 0;
        var recognitionSuccesses = 0;
        var recognitionLowConfidence = 0;
        var recognitionEmpty = 0;
        var dictionaryMismatches = 0;

        foreach (var fixture in catalog.Fixtures.Where(f => f.Status == NodalOsSyntheticOcrTextFixtureStatus.Ready))
        {
            var request = new NodalOsOnnxOcrSyntheticInferenceRequest(
                $"req-{Guid.NewGuid():N}",
                fixture.ImageBytes,
                fixture.Width,
                fixture.Height,
                fixture.RedactionResult,
                NodalOsOcrVisionSensitivity.Low,
                AllowFullScreen: false,
                AllowRawPersistence: false,
                ProductionMode: false,
                Language: "en",
                ModelSetId: "paddleocr-onnx-v4-en");

            var inferenceResult = _pipeline.Run(request, repositoryRoot);
            var decision = ClassifyFixtureDecision(inferenceResult);

            if (inferenceResult.DetectionResult.TextBoxes.Count > 0)
                boxesDetected += inferenceResult.DetectionResult.TextBoxes.Count;

            recognitionAttempts += inferenceResult.RecognitionResults.Count;
            recognitionSuccesses += inferenceResult.RecognitionResults.Count(r => r.Status == NodalOsOnnxOcrInferenceStatus.Success);
            recognitionLowConfidence += inferenceResult.RecognitionResults.Count(r => r.Status == NodalOsOnnxOcrInferenceStatus.LowConfidence);
            recognitionEmpty += inferenceResult.RecognitionResults.Count(r => r.Status == NodalOsOnnxOcrInferenceStatus.RecognitionEmpty);
            dictionaryMismatches += inferenceResult.RecognitionResults.Count(r => r.Status == NodalOsOnnxOcrInferenceStatus.DictionaryMismatch);

            results.Add(new NodalOsOnnxOcrSyntheticTextFixtureResult(
                $"fixture-result-{Guid.NewGuid():N}",
                fixture,
                inferenceResult,
                decision));
        }

        var aggregate = ClassifyAggregateDecision(results);

        return new NodalOsOnnxOcrSyntheticTextRunResult(
            runId,
            DateTimeOffset.UtcNow,
            results,
            aggregate,
            results.Count,
            boxesDetected,
            recognitionAttempts,
            recognitionSuccesses,
            recognitionLowConfidence,
            recognitionEmpty,
            dictionaryMismatches,
            CallsRealOcr: results.Any(r => r.InferenceResult.CallsRealOcr),
            CallsSaas: false,
            RawPersisted: results.Any(r => r.InferenceResult.RawPersisted),
            NoAuthority: true,
            BuildReason(aggregate, results));
    }

    public NodalOsOnnxOcrSyntheticTextRunResult RunDefaultCatalog(
        string runId,
        string repositoryRoot)
    {
        var catalog = _fixtureGenerator.GenerateCatalog();
        return Run(runId, catalog, repositoryRoot);
    }

    private static NodalOsOnnxOcrSyntheticTextRunDecision ClassifyFixtureDecision(NodalOsOnnxOcrSyntheticInferenceResult inference)
    {
        return inference.Status switch
        {
            NodalOsOnnxOcrInferenceStatus.ModelMissing => NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime,
            NodalOsOnnxOcrInferenceStatus.ModelUnverified => NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime,
            NodalOsOnnxOcrInferenceStatus.SessionLoadFailed => NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime,
            NodalOsOnnxOcrInferenceStatus.BlockedByModelRuntime => NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime,
            NodalOsOnnxOcrInferenceStatus.PreProcessingFailed => NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByPreProcessing,
            NodalOsOnnxOcrInferenceStatus.DetectionFailed => NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByPostProcessing,
            NodalOsOnnxOcrInferenceStatus.RecognitionFailed => NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByPostProcessing,
            NodalOsOnnxOcrInferenceStatus.NoTextDetected => NodalOsOnnxOcrSyntheticTextRunDecision.NoTextDetected,
            NodalOsOnnxOcrInferenceStatus.Success => NodalOsOnnxOcrSyntheticTextRunDecision.PositiveTextDetected,
            NodalOsOnnxOcrInferenceStatus.LowConfidence => NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionLowConfidence,
            NodalOsOnnxOcrInferenceStatus.RecognitionEmpty => NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionEmpty,
            NodalOsOnnxOcrInferenceStatus.DictionaryMismatch => NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionBlockedByDictionary,
            _ => NodalOsOnnxOcrSyntheticTextRunDecision.Failed
        };
    }

    private static NodalOsOnnxOcrSyntheticTextRunDecision ClassifyAggregateDecision(
        IReadOnlyList<NodalOsOnnxOcrSyntheticTextFixtureResult> results)
    {
        if (results.Count == 0)
            return NodalOsOnnxOcrSyntheticTextRunDecision.NoTextDetected;

        if (results.Any(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime))
            return NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByModelRuntime;

        if (results.Any(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByPreProcessing))
            return NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByPreProcessing;

        if (results.Any(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByPostProcessing))
            return NodalOsOnnxOcrSyntheticTextRunDecision.BlockedByPostProcessing;

        if (results.Any(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.PositiveTextDetected))
            return NodalOsOnnxOcrSyntheticTextRunDecision.PositiveTextDetected;

        if (results.Any(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionLowConfidence))
            return NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionLowConfidence;

        if (results.Any(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionEmpty))
            return NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionEmpty;

        if (results.Any(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionBlockedByDictionary))
            return NodalOsOnnxOcrSyntheticTextRunDecision.DetectionBoxesFoundRecognitionBlockedByDictionary;

        if (results.All(r => r.Decision == NodalOsOnnxOcrSyntheticTextRunDecision.NoTextDetected))
            return NodalOsOnnxOcrSyntheticTextRunDecision.NoTextDetected;

        return NodalOsOnnxOcrSyntheticTextRunDecision.Failed;
    }

    private static string BuildReason(
        NodalOsOnnxOcrSyntheticTextRunDecision aggregate,
        IReadOnlyList<NodalOsOnnxOcrSyntheticTextFixtureResult> results)
    {
        var summary = $"aggregate: {aggregate}; fixtures={results.Count}; " +
                      $"boxes={results.Sum(r => r.InferenceResult.DetectionResult.TextBoxes.Count)}; " +
                      $"recAttempts={results.Sum(r => r.InferenceResult.RecognitionResults.Count)}";
        return BrowserCredentialRedactor.Redact(summary);
    }
}
