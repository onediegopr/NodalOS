namespace OneBrain.BrowserExecutor.Contracts;

// M198 — ONNX OCR post-processing contracts.
// Decodes detector/recognizer ONNX outputs into text boxes and candidates.
// No fake boxes. Low confidence => human review. No authority.

public enum NodalOsOnnxOcrPostProcessingStatus
{
    Success,
    Empty,
    RecognitionEmpty,
    DictionaryMismatch,
    UnsupportedModelShape,
    ThresholdTooHigh,
    BoxCountExceeded,
    InvalidOutput,
    RequiresHumanReview,
    Failed
}

public sealed record NodalOsOnnxOcrTextBox(
    string BoxId,
    IReadOnlyList<float> Polygon,
    double Confidence,
    int CropX,
    int CropY,
    int CropWidth,
    int CropHeight,
    bool Valid);

public sealed record NodalOsOnnxOcrRecognitionCandidate(
    string CandidateId,
    string Text,
    double Confidence,
    IReadOnlyList<int> CharacterPositions,
    bool LowConfidence,
    bool RequiresHumanReview);

public sealed record NodalOsOnnxOcrDetectorPostProcessingResult(
    string ResultId,
    NodalOsOnnxOcrPostProcessingStatus Status,
    IReadOnlyList<NodalOsOnnxOcrTextBox> TextBoxes,
    bool NoAuthority,
    bool Redacted,
    string Reason);

public sealed record NodalOsOnnxOcrRecognizerPostProcessingResult(
    string ResultId,
    NodalOsOnnxOcrPostProcessingStatus Status,
    IReadOnlyList<NodalOsOnnxOcrRecognitionCandidate> Candidates,
    bool NoAuthority,
    bool Redacted,
    string Reason);

public sealed record NodalOsOnnxOcrConfidenceAggregation(
    string AggregationId,
    double AverageConfidence,
    double MinimumConfidence,
    bool AllAboveThreshold,
    bool RequiresHumanReview,
    bool NoAuthority);
