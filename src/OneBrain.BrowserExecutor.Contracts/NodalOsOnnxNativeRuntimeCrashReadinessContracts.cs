namespace OneBrain.BrowserExecutor.Contracts;

// M211 — ONNX native runtime crash isolation readiness contracts.
// Honest decision after M209 (matrix) + M210 (out-of-process guard). Shadow mode stays blocked.

public enum NodalOsOnnxNativeRuntimeCrashReadinessDecision
{
    ReadyForGuardedSyntheticTextRun,
    ReadyForMoreSyntheticFixtures,
    ReadyForOutOfProcessOnly,
    BlockedByModelRuntime,
    BlockedByInputTensorShape,
    BlockedByOnnxRuntimeVersion,
    BlockedByModelCompatibility,
    BlockedByRenderer,
    BlockedByPostProcessing,
    NotReady
}

public sealed record NodalOsOnnxNativeRuntimeCrashReadinessReport(
    string ReportId,
    NodalOsOnnxNativeRuntimeCrashReadinessDecision Decision,
    bool CanAttemptRedactedCropShadow,
    bool CanRunGuardedSyntheticText,
    bool CanContinueWithMoreFixtures,
    IReadOnlyList<NodalOsOnnxSyntheticOcrRequirement> Requirements,
    bool InProcessCrashContained,
    bool OutOfProcessGuardContainsCrash,
    bool ParentProcessSurvived,
    bool NoOrphanProcesses,
    bool TempFilesCleaned,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    bool ShadowModeBlocked,
    bool ProductionPublicOcrBlocked,
    IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAtUtc);
