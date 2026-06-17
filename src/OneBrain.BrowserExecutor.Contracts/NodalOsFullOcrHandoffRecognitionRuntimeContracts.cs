namespace OneBrain.BrowserExecutor.Contracts;

// M226-M228 - full OCR handoff and recognizer runtime isolation.
// Risky OCR remains out-of-process only. Results are diagnostic evidence, never authority.

public enum NodalOsFullOcrHandoffStage
{
    DetectorSessionCreation,
    DetectorRun,
    DetectorOutputMetadata,
    DetectorPostProcessing,
    BoxValidation,
    BoxCoordinateNormalization,
    CropBoundsCalculation,
    CropExtraction,
    RecognizerCropResize,
    RecognizerTensorPreparation,
    RecognizerSessionCreation,
    RecognizerRun,
    RecognizerOutputMetadata,
    RecognitionPostProcessing,
    CtcDictionaryCompatibility
}

public enum NodalOsFullOcrHandoffBoxKind
{
    DetectorProduced,
    ManualSafe,
    Degenerate,
    OutOfBounds,
    EmptyCrop,
    TooSmall
}

public enum NodalOsFullOcrHandoffProbeStatus
{
    StageSucceeded,
    BlockedByInvalidBox,
    BlockedByOutOfBoundsCrop,
    BlockedByEmptyCrop,
    BlockedByRecognizerTensorShape,
    RecognizerSessionCreated,
    RecognizerRunSucceeded,
    NativeRuntimeCrashContained,
    TimedOut,
    BlockedByModelRuntime,
    BlockedByPostProcessing
}

public enum NodalOsRecognizerRuntimeTensorKind
{
    Zero,
    Ones,
    Gradient,
    SyntheticTextCrop,
    HighContrastManualCrop,
    DetectorDerivedCrop
}

public enum NodalOsRecognizerRuntimeProbeStatus
{
    SessionCreated,
    RunSucceeded,
    OutputMetadataCaptured,
    NativeRuntimeCrashContained,
    InvalidTensorShape,
    TimedOut,
    BlockedByModelRuntime,
    BlockedByDictionaryClassCountMismatch,
    Skipped
}

public enum NodalOsFullOcrRuntimeDecision
{
    ReadyForDictionaryCompletion,
    ReadyForRecognizerRuntimeExperiment,
    ReadyForRecognizerModelReplacement,
    ReadyForHandoffFix,
    ReadyForPostProcessingFix,
    BlockedByModelRuntime,
    BlockedByDictionary,
    NotReady
}

public sealed record NodalOsFullOcrHandoffProbeResult(
    string ResultId,
    NodalOsFullOcrHandoffBoxKind BoxKind,
    NodalOsFullOcrHandoffProbeStatus Status,
    NodalOsFullOcrHandoffStage LastSuccessfulStage,
    int? BoxX,
    int? BoxY,
    int? BoxWidth,
    int? BoxHeight,
    int[] RecognizerInputShape,
    bool DetectorRunSucceeded,
    bool DetectorPostProcessingReached,
    bool CropExtractionSucceeded,
    bool RecognizerTensorPrepared,
    bool RecognizerSessionCreated,
    bool RecognizerRunAttempted,
    int? ExitCode,
    string? ExitCodeHex,
    bool RanOutOfProcess,
    bool ParentSurvived,
    bool TempFilesCleaned,
    bool RawPersisted,
    bool CallsSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsRecognizerRuntimeProbeResult(
    string ResultId,
    NodalOsRecognizerRuntimeTensorKind TensorKind,
    NodalOsRecognizerRuntimeProbeStatus Status,
    string RuntimePackageVersion,
    string Provider,
    string ModelPath,
    IReadOnlyList<string> InputNames,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<int[]> OutputShapes,
    int[] InputShape,
    NodalOsOnnxTensorStats TensorStats,
    int? OutputClassCount,
    int? OutputTimeSteps,
    int? ExitCode,
    string? ExitCodeHex,
    string CrashStage,
    bool SessionCreated,
    bool RunAttempted,
    bool RanOutOfProcess,
    bool ParentSurvived,
    bool TempFilesCleaned,
    bool RawPersisted,
    bool CallsSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsFullOcrRuntimeDecisionInput(
    bool ModelsVerified,
    bool RiskyOcrNeverRanInProcess,
    bool ParentSurvived,
    bool TempCleanup,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    IReadOnlyList<NodalOsFullOcrHandoffProbeResult> HandoffResults,
    IReadOnlyList<NodalOsRecognizerRuntimeProbeResult> RecognizerResults,
    NodalOsOcrDictionaryCompatibilityResult DictionaryCompatibility);

public sealed record NodalOsFullOcrRuntimeDecisionReport(
    string ReportId,
    NodalOsFullOcrRuntimeDecision Decision,
    bool ShadowModeBlocked,
    bool ProductiveOcrBlocked,
    bool DetectorIsolatedRunSucceeded,
    bool DetectorPostProcessingProducedBoxes,
    bool HandoffFailureDetected,
    bool RecognizerRuntimeCrashDetected,
    bool RecognizerRuntimeSucceeded,
    bool DictionaryMismatchDetected,
    bool NoAuthority,
    string Reason);
