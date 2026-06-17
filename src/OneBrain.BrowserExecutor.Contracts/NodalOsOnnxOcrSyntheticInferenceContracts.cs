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
    NoTextDetected,
    LowConfidence,
    RequiresHumanReview,
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
