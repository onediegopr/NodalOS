namespace OneBrain.BrowserExecutor.Contracts;

// M223-M225 - detector-only ONNX Runtime compatibility experiment.
// Risky detector probes are represented as metadata and must execute out-of-process.

public enum NodalOsDetectorRuntimeProbeTensorKind
{
    Zero,
    Ones,
    Gradient,
    SyntheticText,
    CurrentPreprocessedSyntheticText,
    SafeRectangle,
    SafeCircle
}

public enum NodalOsDetectorRuntimeProbeLayout
{
    Nchw,
    Nhwc
}

public enum NodalOsDetectorRuntimeSessionOptionKind
{
    Default,
    GraphOptimizationDisabled,
    GraphOptimizationBasic,
    SingleThreaded,
    MemoryPatternDisabled,
    CpuArenaDisabled
}

public enum NodalOsDetectorRuntimeCompatibilityStatus
{
    SessionCreated,
    RunSucceeded,
    OutputMetadataCaptured,
    NativeRuntimeCrashContained,
    InvalidTensorShape,
    UnsupportedLayout,
    TimedOut,
    BlockedByModelRuntime,
    Skipped
}

public enum NodalOsDetectorRuntimeCompatibilityDecision
{
    ReadyForOnnxRuntimeVersionExperiment,
    ReadyForDetectorModelReplacement,
    ReadyForSessionOptionsFix,
    ReadyForPreProcessingFix,
    ReadyForRendererFix,
    ReadyForPostProcessingFix,
    BlockedByModelRuntime,
    NotReady
}

public sealed record NodalOsDetectorRuntimeSessionOptionsMetadata(
    NodalOsDetectorRuntimeSessionOptionKind OptionKind,
    string Description,
    bool CpuExecutionProviderOnly,
    bool GraphOptimizationDisabled,
    bool GraphOptimizationBasic,
    int? IntraOpThreads,
    int? InterOpThreads,
    bool MemoryPatternDisabled,
    bool CpuArenaDisabled);

public sealed record NodalOsDetectorRuntimeExperiment(
    string ExperimentId,
    NodalOsDetectorRuntimeProbeTensorKind TensorKind,
    NodalOsDetectorRuntimeProbeLayout Layout,
    int[] InputShape,
    NodalOsDetectorRuntimeSessionOptionsMetadata SessionOptions,
    bool RequiresOutOfProcessGuard,
    bool AllowInProcess,
    bool Synthetic,
    bool FullScreen,
    bool Sensitive,
    bool RawPersisted,
    bool NoAuthority);

public sealed record NodalOsDetectorRuntimeCompatibilityResult(
    string ResultId,
    NodalOsDetectorRuntimeExperiment Experiment,
    NodalOsDetectorRuntimeCompatibilityStatus Status,
    string RuntimePackageVersion,
    string RuntimeNativeLibrary,
    string Provider,
    string OsArchitecture,
    string ModelPath,
    int ModelOpset,
    IReadOnlyList<string> InputNames,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<int[]> OutputShapes,
    NodalOsOnnxTensorStats TensorStats,
    int? ExitCode,
    string? ExitCodeHex,
    string CrashStage,
    bool SessionCreated,
    bool RunAttempted,
    bool PostProcessingReached,
    bool ParentSurvived,
    bool TempFilesCleaned,
    bool OrphanProcessLeft,
    bool RawPersisted,
    bool CallsSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsDetectorModelCompatibilityFinding(
    string FindingId,
    NodalOsDetectorRuntimeCompatibilityDecision Decision,
    bool ModelVerified,
    bool AnyRunSucceeded,
    bool AnySessionOptionAvoidedCrash,
    bool AllRunAttemptsCrashed,
    bool TensorSpecificCrash,
    bool MetadataMismatch,
    bool ShadowModeBlocked,
    bool ProductiveOcrBlocked,
    bool NoAuthority,
    string Reason);
