namespace OneBrain.BrowserExecutor.Contracts;

// M192 — Production-grade PaddleOCR local worker adapter contracts.
// Disabled-by-default; crop-only; redacted-only; no authority.

public enum NodalOsPaddleOcrWorkerExecutionMode
{
    Disabled,
    HealthCheckOnly,
    SyntheticOnly,
    RedactedCropShadow,
    BlockedForProduction,
    Error
}

public sealed record NodalOsPaddleOcrWorkerInvocationPolicy(
    NodalOsPaddleOcrWorkerExecutionMode AllowedMode,
    bool RequiresPixelRedactionV2,
    bool RequiresSafeForOcr,
    bool RequiresOriginalRawPersistedFalse,
    bool RequiresCropOnly,
    bool BlocksFullScreen,
    bool BlocksSensitive,
    bool BlocksCloud,
    bool BlocksProduction,
    int MaxImageWidth,
    int MaxImageHeight,
    int MaxPages,
    int TimeoutMs,
    int MaxStdoutBytes,
    int MaxStderrBytes,
    bool NoRawPersistence,
    bool NoAuthority,
    bool RequiresAuditRecord);

public sealed record NodalOsPaddleOcrWorkerRequest(
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

public sealed record NodalOsPaddleOcrWorkerResponse(
    string ResponseId,
    NodalOsPaddleOcrWorkerExecutionMode Mode,
    bool Success,
    string Status,
    IReadOnlyList<NodalOsPaddleOcrWorkerLine> Lines,
    double? Confidence,
    IReadOnlyList<string> Warnings,
    bool CallsRealOcr,
    bool CallsSaas,
    bool RawPersisted,
    bool NoAuthority,
    bool Redacted,
    TimeSpan Duration,
    NodalOsPaddleOcrWorkerAuditRecord AuditRecord);

public sealed record NodalOsPaddleOcrWorkerLine(
    string Text,
    double Confidence,
    IReadOnlyList<double>? BoundingBox);

public sealed record NodalOsPaddleOcrWorkerHealth(
    string HealthId,
    bool RuntimeAvailable,
    bool WorkerScriptPresent,
    bool InvocationPolicyValid,
    bool NoAuthority,
    DateTimeOffset CheckedAtUtc);

public sealed record NodalOsPaddleOcrWorkerAuditRecord(
    string AuditId,
    string RequestId,
    NodalOsPaddleOcrWorkerExecutionMode Mode,
    bool InvokedRealOcr,
    bool Allowed,
    string Reason,
    bool NoAuthority,
    bool Redacted);
