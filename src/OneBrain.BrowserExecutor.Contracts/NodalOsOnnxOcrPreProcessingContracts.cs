namespace OneBrain.BrowserExecutor.Contracts;

// M198 — ONNX OCR pre-processing contracts.
// Prepares redacted pixel crops for ONNX Runtime inference without persisting raw data.

public enum NodalOsOnnxOcrImageFormat
{
    RawRgba32,
    Png,
    Jpeg,
    Bmp,
    Unknown
}

public enum NodalOsOnnxOcrPreProcessingStatus
{
    Success,
    MissingRedaction,
    BlockedSensitive,
    BlockedFullScreen,
    BlockedRawPersistence,
    InvalidImage,
    UnsupportedFormat,
    Failed
}

public sealed record NodalOsOnnxOcrImagePreProcessingResult(
    string ResultId,
    NodalOsOnnxOcrPreProcessingStatus Status,
    float[] NormalizedData,
    int BatchSize,
    int Channels,
    int Height,
    int Width,
    float ScaleX,
    float ScaleY,
    float PadLeft,
    float PadTop,
    NodalOsOnnxOcrImageFormat SourceFormat,
    int SourceWidth,
    int SourceHeight,
    bool NoAuthority,
    bool Redacted,
    string Reason);

public sealed record NodalOsOnnxOcrDetectorPreProcessingResult(
    string ResultId,
    NodalOsOnnxOcrPreProcessingStatus Status,
    float[] InputTensor,
    int[] InputShape,
    float Scale,
    int PaddedWidth,
    int PaddedHeight,
    bool NoAuthority,
    bool Redacted,
    string Reason);

public sealed record NodalOsOnnxOcrRecognizerPreProcessingResult(
    string ResultId,
    NodalOsOnnxOcrPreProcessingStatus Status,
    float[] InputTensor,
    int[] InputShape,
    int MaxWidth,
    bool NoAuthority,
    bool Redacted,
    string Reason);

public sealed record NodalOsOnnxOcrPreProcessingPolicy(
    bool RequirePixelRedactionV2,
    bool BlockSensitive,
    bool BlockFullScreen,
    bool BlockRawPersistence,
    bool CropOnly,
    bool NoAuthority);
