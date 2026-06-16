namespace OneBrain.BrowserExecutor.Contracts;

// M202 — ONNX Runtime .NET session smoke test.
// Loads verified ONNX models and inspects metadata without executing OCR on real data.

public enum NodalOsOnnxModelSessionSmokeStatus
{
    Success,
    ModelMissing,
    ModelUnverified,
    SessionLoadFailed,
    UnsupportedModelShape,
    InputOutputMismatch,
    PolicyBlocked,
    NotAttempted
}

public sealed record NodalOsOnnxRuntimeSessionPolicy(
    string PolicyId,
    bool AllowRealModelLoad,
    bool AllowMetadataInspection,
    bool AllowDummyInference,
    bool AllowRealImageInference,
    long MaxModelSizeBytes,
    int TimeoutSeconds,
    bool NoAuthority);

public sealed record NodalOsOnnxModelSessionSmokeResult(
    string SmokeId,
    NodalOsOnnxModelSessionSmokeStatus Status,
    string ModelId,
    string ModelPath,
    bool SessionCreated,
    string? RuntimeVersion,
    string? Provider,
    IReadOnlyList<string> InputNames,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<int[]> InputShapes,
    IReadOnlyList<int[]> OutputShapes,
    bool DummyInferenceRun,
    bool RealImageInferenceRun,
    string Reason,
    bool NoAuthority,
    TimeSpan Duration);

public sealed record NodalOsOnnxModelSessionReadiness(
    string ReadinessId,
    bool Ready,
    NodalOsOnnxModelSessionSmokeStatus Status,
    string Reason,
    bool AllRequiredModelsVerified,
    bool AllSessionsLoaded,
    bool MetadataMatchesManifest,
    bool NoAuthority);
