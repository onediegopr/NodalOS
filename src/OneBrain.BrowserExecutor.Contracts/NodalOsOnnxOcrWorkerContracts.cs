namespace OneBrain.BrowserExecutor.Contracts;

// M196 — ONNX Runtime .NET OCR worker skeleton and readiness gate.
// Production-grade structure; real inference blocked until verified ONNX models exist.

public enum NodalOsOnnxOcrWorkerExecutionMode
{
    Disabled,
    HealthCheckOnly,
    ModelMissing,
    ModelVerified,
    SyntheticRedactedCropOnly,
    ShadowRedactedCropOnly,
    BlockedForProduction,
    Error
}

public sealed record NodalOsOnnxOcrModelSet(
    string ModelSetId,
    string? DetectionModelPath,
    string? RecognitionModelPath,
    string? ClassificationModelPath,
    bool AllRequiredPresent,
    bool AllVerified,
    bool NoAuthority);

public sealed record NodalOsOnnxOcrWorkerRequest(
    string RequestId,
    string ContractVersion,
    string AuthToken,
    byte[] RedactedImageBytes,
    int Width,
    int Height,
    string Language,
    NodalOsPixelRedactionResult PixelRedactionResult,
    NodalOsOcrVisionSensitivity Sensitivity,
    bool AllowFullScreen,
    bool AllowRawPersistence,
    bool ProductionMode,
    DateTimeOffset CreatedAtUtc);

public sealed record NodalOsOnnxOcrWorkerResponse(
    string ResponseId,
    NodalOsOnnxOcrWorkerExecutionMode Mode,
    bool Success,
    string Status,
    IReadOnlyList<NodalOsOnnxOcrLine> Lines,
    double? Confidence,
    IReadOnlyList<string> Warnings,
    bool CallsRealOcr,
    bool CallsSaas,
    bool RawPersisted,
    bool NoAuthority,
    bool Redacted,
    TimeSpan Duration,
    NodalOsOnnxOcrWorkerAuditRecord AuditRecord);

public sealed record NodalOsOnnxOcrLine(
    string Text,
    double Confidence,
    IReadOnlyList<float>? BoundingBox);

public sealed record NodalOsOnnxOcrWorkerHealth(
    string HealthId,
    bool OnnxRuntimeAvailable,
    bool ModelSetPresent,
    bool ModelSetVerified,
    bool InvocationPolicyValid,
    bool NoAuthority,
    DateTimeOffset CheckedAtUtc);

public sealed record NodalOsOnnxOcrWorkerAuditRecord(
    string AuditId,
    string RequestId,
    NodalOsOnnxOcrWorkerExecutionMode Mode,
    bool InvokedRealOcr,
    bool Allowed,
    string Reason,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsOnnxOcrReadinessReport(
    string ReportId,
    bool Ready,
    NodalOsOnnxOcrWorkerExecutionMode Mode,
    bool OnnxRuntimePackageAvailable,
    bool ModelSetPresent,
    bool ModelSetVerified,
    bool PixelRedactionV2Required,
    bool CropOnly,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoRawPersistence,
    bool NoSaas,
    bool NoPython,
    bool NoAuthority,
    bool ProductionPublicOcrBlocked,
    IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAtUtc);
