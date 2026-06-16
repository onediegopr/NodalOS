namespace OneBrain.BrowserExecutor.Contracts;

// M191-M193 — Production-grade local PaddleOCR runtime.
// Real runtime readiness, installation state, health, rollback and operational status.
// Disabled-by-default; production public OCR remains blocked.

public enum NodalOsPaddleOcrInstallState
{
    NotInstalled,
    Installing,
    Installed,
    InstallFailed,
    BlockedByEnvironment,
    RollbackRequested
}

public enum NodalOsPaddleOcrRuntimeDecision
{
    NotReady,
    ReadyForHealthCheck,
    ReadyForSyntheticRedactedCrop,
    BlockedByPythonMissing,
    BlockedByPipMissing,
    BlockedByVenvUnavailable,
    BlockedByInstallFailure,
    BlockedByEnvironmentUnsupported,
    BlockedByPolicy,
    BlockedByNoAuthorityGate
}

public enum NodalOsPaddleOcrRuntimeHealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    NotInstalled
}

public sealed record NodalOsPaddleOcrRuntimeVersion(
    string? PaddleOcrVersion,
    string? PaddlePaddleVersion,
    string PythonVersion,
    string OsPlatform,
    string Architecture);

public sealed record NodalOsPaddleOcrRuntimeEnvironment(
    string EnvironmentId,
    bool PythonAvailable,
    string PythonPath,
    bool PipAvailable,
    bool VenvAvailable,
    bool PaddleOcrInstalled,
    bool PaddlePaddleInstalled,
    bool TesseractInstalled,
    string OsPlatform,
    string Architecture,
    string WorkingDirectory,
    bool HasSufficientPermissions,
    string Notes);

public sealed record NodalOsPaddleOcrRuntimeHealth(
    string HealthId,
    NodalOsPaddleOcrRuntimeHealthStatus Status,
    bool CanImportPaddleOcr,
    bool CanImportPaddlePaddle,
    bool VersionCheckPassed,
    bool NoNetworkRequiredForThisCheck,
    string? ErrorRedacted,
    DateTimeOffset CheckedAtUtc);

public sealed record NodalOsPaddleOcrRollbackPlan(
    string PlanId,
    string VenvPath,
    string[] PackagesToRemove,
    string[] DirectoriesToRemove,
    string[] FilesToRemove,
    bool RequiresConfirmation,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsPaddleOcrOperationalStatus(
    string StatusId,
    NodalOsPaddleOcrInstallState InstallState,
    NodalOsPaddleOcrRuntimeDecision RuntimeDecision,
    bool ProductionPublicEnabled,
    bool RealSaasEnabled,
    bool RealOcrProductiveEnabled,
    bool CropOnly,
    bool RedactedOnly,
    bool LocalOnly,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoAuthority,
    DateTimeOffset ReportedAtUtc);
