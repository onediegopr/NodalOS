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
    Checker,
    SyntheticTextCrop,
    HighContrastManualCrop,
    DetectorDerivedCrop
}

public enum NodalOsRecognizerRuntimeProbeLayout
{
    Nchw,
    Nhwc
}

public enum NodalOsRecognizerRuntimeShapeKind
{
    CurrentPipelineFixed,
    PaddleOcrCandidate320,
    PaddleOcrCandidate640,
    Invalid
}

public enum NodalOsRecognizerRuntimeSessionOptionKind
{
    Default,
    GraphOptimizationDisabled,
    GraphOptimizationBasic,
    SingleThreaded,
    MemoryPatternDisabled,
    CpuArenaDisabled,
    SequentialExecution,
    DeterministicMinimal
}

public enum NodalOsRecognizerRuntimeProbeStatus
{
    SessionCreated,
    RunSucceeded,
    OutputMetadataCaptured,
    NativeRuntimeCrashContained,
    InvalidTensorShape,
    UnsupportedLayout,
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

public enum NodalOsRecognizerRuntimeCompatibilityDecision
{
    ReadyForOnnxRuntimeVersionExperiment,
    ReadyForRecognizerModelReplacement,
    ReadyForRecognizerSessionOptionsFix,
    ReadyForRecognizerPreprocessingFix,
    ReadyForDictionaryCompletion,
    BlockedByRecognizerModelRuntime,
    BlockedByRecognizerModelCompatibility,
    BlockedByRecognizerInputShape,
    BlockedByDictionary,
    NotReady
}

public enum NodalOsOnnxRuntimeVersionExperimentStatus
{
    NotRun,
    RestoreSucceeded,
    RuntimeVersionRestoreFailed,
    BuildSucceeded,
    BuildFailed,
    DetectorSanitySucceeded,
    DetectorSanityFailed,
    RecognizerRunSucceeded,
    RecognizerNativeRuntimeCrashContained,
    ProbeFailed,
    OutputMetadataCaptured
}

public enum NodalOsOnnxRuntimeVersionDecision
{
    ReadyForOnnxRuntimeUpgrade,
    ReadyForDictionaryCompletion,
    ReadyForRecognizerModelReplacement,
    BlockedByRecognizerModelRuntime,
    BlockedByRuntimeRestore,
    NotReady
}

public sealed record NodalOsRecognizerRuntimeSessionOptionsMetadata(
    NodalOsRecognizerRuntimeSessionOptionKind OptionKind,
    string Description,
    bool CpuExecutionProviderOnly,
    bool GraphOptimizationDisabled,
    bool GraphOptimizationBasic,
    int? IntraOpThreads,
    int? InterOpThreads,
    bool MemoryPatternDisabled,
    bool CpuArenaDisabled,
    bool SequentialExecution,
    bool DeterministicMinimal);

public sealed record NodalOsRecognizerRuntimeExperiment(
    string ExperimentId,
    NodalOsRecognizerRuntimeTensorKind TensorKind,
    NodalOsRecognizerRuntimeProbeLayout Layout,
    NodalOsRecognizerRuntimeShapeKind ShapeKind,
    int[] InputShape,
    NodalOsRecognizerRuntimeSessionOptionsMetadata SessionOptions,
    bool RequiresOutOfProcessGuard,
    bool AllowInProcess,
    bool Synthetic,
    bool FullScreen,
    bool Sensitive,
    bool RawPersisted,
    bool NoAuthority);

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

public sealed record NodalOsRecognizerRuntimeCompatibilityResult(
    string ResultId,
    NodalOsRecognizerRuntimeExperiment Experiment,
    NodalOsRecognizerRuntimeProbeStatus Status,
    string RuntimePackageVersion,
    string RuntimeNativeLibrary,
    string Provider,
    string OsArchitecture,
    string ModelPath,
    long ModelSizeBytes,
    string ModelSha256,
    IReadOnlyList<string> InputNames,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<int[]> OutputShapes,
    NodalOsOnnxTensorStats TensorStats,
    int? OutputClassCount,
    int? OutputTimeSteps,
    int? ExitCode,
    string? ExitCodeHex,
    string CrashStage,
    bool SessionCreated,
    bool RunAttempted,
    bool ParentSurvived,
    bool TempFilesCleaned,
    bool OrphanProcessLeft,
    bool RawPersisted,
    bool CallsSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsRecognizerModelCompatibilityFinding(
    string FindingId,
    NodalOsRecognizerRuntimeCompatibilityDecision Decision,
    bool RecognizerModelVerified,
    bool AnyRunSucceeded,
    bool AnySessionOptionAvoidedCrash,
    bool AllCoreTensorsCrashed,
    bool CropOnlyCrash,
    bool InvalidShapeDetected,
    bool DictionaryMismatchDetected,
    bool ShadowModeBlocked,
    bool ProductiveOcrBlocked,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOnnxRuntimeVersionExperimentPlan(
    string BaselineVersion,
    IReadOnlyList<string> CandidateVersions,
    string PackageReferenceProject,
    bool Reversible,
    bool CpuProviderOnly,
    bool ProductiveOcrBlocked,
    bool NoAuthority);

public sealed record NodalOsOnnxRuntimeVersionExperimentResult(
    string RuntimeVersion,
    string RequestedPackageVersion,
    string RestoredPackageVersion,
    string NativeRuntimeObserved,
    bool RestoreSucceeded,
    bool BuildSucceeded,
    bool DetectorSanitySucceeded,
    bool RecognizerZeroSucceeded,
    bool RecognizerOnesSucceeded,
    bool RecognizerGradientSucceeded,
    bool RecognizerCropSucceeded,
    bool AnyRecognizerRunSucceeded,
    bool AnyRecognizerCrashContained,
    int? ExitCode,
    string? ExitCodeHex,
    string CrashStage,
    bool ParentSurvived,
    bool TempFilesCleaned,
    bool RawPersisted,
    bool CallsSaas,
    bool NoAuthority,
    NodalOsOnnxRuntimeVersionExperimentStatus Status,
    string Reason);

public sealed record NodalOsOnnxRuntimeVersionDecisionReport(
    string ReportId,
    NodalOsOnnxRuntimeVersionDecision Decision,
    string BaselineVersion,
    string FinalPackageVersion,
    bool BranchLeftAtBaseline,
    bool AnyVersionAvoidedCrash,
    bool DetectorSanityRequired,
    bool DictionaryMismatchStillBlocksDecode,
    bool ShadowModeBlocked,
    bool ProductiveOcrBlocked,
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
