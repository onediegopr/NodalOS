namespace OneBrain.BrowserExecutor.Contracts;

// M203 — ONNX synthetic redacted-crop OCR inference contracts.
// Real ONNX Runtime inference end-to-end, but only on synthetic/redacted non-sensitive crops.

public enum NodalOsOnnxOcrInferenceStatus
{
    Success,
    BlockedByRedaction,
    BlockedBySensitive,
    BlockedByFullScreen,
    BlockedByRawPersistence,
    BlockedByPolicy,
    ModelMissing,
    ModelUnverified,
    SessionLoadFailed,
    PreProcessingFailed,
    DetectionFailed,
    RecognitionFailed,
    RecognitionEmpty,
    DictionaryMismatch,
    NoTextDetected,
    LowConfidence,
    RequiresHumanReview,
    BlockedByModelRuntime,
    Failed
}

public sealed record NodalOsOnnxOcrSyntheticInferenceRequest(
    string RequestId,
    byte[] RedactedImageBytes,
    int Width,
    int Height,
    NodalOsPixelRedactionResult PixelRedactionResult,
    NodalOsOcrVisionSensitivity Sensitivity,
    bool AllowFullScreen,
    bool AllowRawPersistence,
    bool ProductionMode,
    string Language,
    string ModelSetId);

public sealed record NodalOsOnnxOcrDetectionInferenceResult(
    string ResultId,
    NodalOsOnnxOcrInferenceStatus Status,
    IReadOnlyList<NodalOsOnnxOcrTextBox> TextBoxes,
    long? InferenceTimeMs,
    string ModelId,
    string ModelVersion,
    string Reason);

public sealed record NodalOsOnnxOcrRecognitionInferenceResult(
    string ResultId,
    NodalOsOnnxOcrInferenceStatus Status,
    IReadOnlyList<NodalOsOnnxOcrRecognitionCandidate> Candidates,
    long? InferenceTimeMs,
    string ModelId,
    string ModelVersion,
    string Reason);

public sealed record NodalOsOnnxOcrInferencePipeline(
    string PipelineId,
    string RepositoryRoot,
    NodalOsOnnxRuntimeSessionPolicy SessionPolicy,
    NodalOsOnnxOcrPreProcessingPolicy PreProcessingPolicy,
    double RecognitionConfidenceThreshold,
    double DetectorThreshold,
    int MaxBoxes,
    int MaxRecognizerWidth,
    bool NoAuthority);

public sealed record NodalOsOnnxOcrEndToEndEvidence(
    string EvidenceId,
    string RedactedCropHash,
    string? DetectionResultId,
    string? RecognitionResultId,
    IReadOnlyList<string> ModelManifestRefs,
    IReadOnlyList<string> EvidenceRefs,
    long? TotalInferenceTimeMs,
    int DetectionBoxCount,
    int RecognitionCandidateCount,
    bool OriginalRawPersisted,
    bool CallsSaas,
    bool ProductionEnabled,
    bool NoAuthority,
    string Summary);

public sealed record NodalOsOnnxOcrSyntheticInferenceResult(
    string ResultId,
    NodalOsOnnxOcrInferenceStatus Status,
    bool Success,
    NodalOsOnnxOcrDetectionInferenceResult DetectionResult,
    IReadOnlyList<NodalOsOnnxOcrRecognitionInferenceResult> RecognitionResults,
    NodalOsOnnxOcrEndToEndEvidence Evidence,
    bool CallsRealOcr,
    bool CallsSaas,
    bool RawPersisted,
    bool NoAuthority,
    bool RequiresHumanReview,
    TimeSpan Duration,
    IReadOnlyList<string> Warnings);

public enum NodalOsOnnxOcrSyntheticTextRunDecision
{
    PositiveTextDetected,
    DetectionBoxesFoundRecognitionLowConfidence,
    DetectionBoxesFoundRecognitionEmpty,
    DetectionBoxesFoundRecognitionBlockedByDictionary,
    NoTextDetected,
    BlockedByModelRuntime,
    BlockedByPreProcessing,
    BlockedByPostProcessing,
    Failed
}

public sealed record NodalOsOnnxOcrSyntheticTextFixtureResult(
    string ResultId,
    NodalOsSyntheticOcrTextFixture Fixture,
    NodalOsOnnxOcrSyntheticInferenceResult InferenceResult,
    NodalOsOnnxOcrSyntheticTextRunDecision Decision);

public sealed record NodalOsOnnxOcrSyntheticTextRunResult(
    string RunId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<NodalOsOnnxOcrSyntheticTextFixtureResult> FixtureResults,
    NodalOsOnnxOcrSyntheticTextRunDecision AggregateDecision,
    int FixturesRun,
    int BoxesDetected,
    int RecognitionAttempts,
    int RecognitionSuccesses,
    int RecognitionLowConfidence,
    int RecognitionEmpty,
    int DictionaryMismatches,
    bool CallsRealOcr,
    bool CallsSaas,
    bool RawPersisted,
    bool NoAuthority,
    string Reason);
