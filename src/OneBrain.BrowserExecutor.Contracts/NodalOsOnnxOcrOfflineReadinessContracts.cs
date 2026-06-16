namespace OneBrain.BrowserExecutor.Contracts;

// M199 — Offline synthetic ONNX OCR readiness gate.
// Decides whether M200–M202 can attempt a real ONNX OCR run on synthetic redacted crops.

public enum NodalOsOnnxOcrOfflineReadinessDecision
{
    ReadyForOnnxSyntheticRun,
    ModelMissing,
    ModelUnverified,
    UnsupportedModelShape,
    PreProcessingIncomplete,
    PostProcessingIncomplete,
    SessionLoadFailed,
    BlockedByRedaction,
    BlockedByPolicy,
    NotReady
}

public sealed record NodalOsOnnxOcrOfflineReadinessRequirement(
    string RequirementId,
    string Name,
    bool Satisfied,
    string Reason,
    bool BlocksReadiness);

public sealed record NodalOsOnnxOcrOfflineReadinessReport(
    string ReportId,
    NodalOsOnnxOcrOfflineReadinessDecision Decision,
    bool CanAttemptSyntheticRun,
    IReadOnlyList<NodalOsOnnxOcrOfflineReadinessRequirement> Requirements,
    bool OnnxRuntimePackageAvailable,
    bool DetectionModelVerified,
    bool RecognitionModelVerified,
    bool ModelShapesKnown,
    bool PreProcessorReady,
    bool DetectorPostProcessorReady,
    bool RecognizerPostProcessorReady,
    bool PixelRedactionV2Required,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    bool ProductionPublicOcrBlocked,
    IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAtUtc);
