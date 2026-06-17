namespace OneBrain.BrowserExecutor.Contracts;

// M209 — ONNX native runtime crash reproduction matrix contracts.
// Models a controlled matrix of fixture x dimension x stage probes so the native ONNX Runtime
// crash can be located without killing the main test host. Probes that may crash are never
// allowed to run in-process in the normal suite; they are routed through the M210 guard.

public enum NodalOsOnnxNativeRuntimeCrashStage
{
    ModelFileVerification,
    SessionCreation,
    InputTensorPreparation,
    DetectionRun,
    DetectorPostProcessing,
    CropExtraction,
    RecognitionTensorPreparation,
    RecognitionRun,
    RecognitionPostProcessing
}

public enum NodalOsOnnxNativeRuntimeCrashKind
{
    NoCrash,
    NativeAccessViolation,
    NativeAbort,
    ManagedException,
    ProcessExitNonZero,
    TimedOut,
    InvalidOutput,
    Unknown
}

public enum NodalOsOnnxNativeRuntimeCrashProbeStatus
{
    Passed,
    NoTextDetected,
    LowConfidence,
    BlockedByModelRuntime,
    NativeRuntimeCrash,
    ProcessCrashed,
    TimedOut,
    InvalidTensorShape,
    UnsupportedFixture,
    SkippedQuarantined
}

public enum NodalOsOnnxNativeRuntimeCrashFixtureKind
{
    StripeSafe,
    SmallRectangle,
    LargeRectangle,
    LargeCircle,
    AntiAliasedPixelFontText,
    PixelFontText,
    ThickHorizontalBars,
    NumericText,
    AlphanumericText
}

// How a fixture is allowed to be executed against the native runtime.
public enum NodalOsOnnxNativeRuntimeCrashFixtureSafety
{
    // Known safe shapes proven not to crash the native host (stripe / rectangle / circle).
    SafeInProcess,
    // Text-like fixtures that have crashed the native host; may only be attempted through the M210 guard.
    QuarantinedOutOfProcessOnly,
    // Unsafe requests (full-screen / sensitive / raw-persisted / non-synthetic) rejected before any runtime.
    BlockedBeforeRuntime
}

public sealed record NodalOsOnnxNativeRuntimeCrashProbeRequest(
    string ProbeId,
    NodalOsOnnxNativeRuntimeCrashFixtureKind FixtureKind,
    NodalOsSyntheticOcrTextRenderMode RenderMode,
    int Width,
    int Height,
    NodalOsOnnxNativeRuntimeCrashStage Stage,
    NodalOsOcrVisionSensitivity Sensitivity,
    bool FullScreen,
    bool Sensitive,
    bool OriginalRawPersisted,
    bool Synthetic,
    bool NoAuthority,
    bool RunOutOfProcess)
{
    // Validate the fixture metadata is well formed and safe enough to even be modelled.
    // This never executes the runtime; it only checks declared metadata.
    public NodalOsOnnxNativeRuntimeCrashFixtureSafety ClassifySafety()
    {
        if (FullScreen || Sensitive || OriginalRawPersisted || !Synthetic ||
            Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface)
        {
            return NodalOsOnnxNativeRuntimeCrashFixtureSafety.BlockedBeforeRuntime;
        }

        return FixtureKind switch
        {
            NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe => NodalOsOnnxNativeRuntimeCrashFixtureSafety.SafeInProcess,
            NodalOsOnnxNativeRuntimeCrashFixtureKind.SmallRectangle => NodalOsOnnxNativeRuntimeCrashFixtureSafety.SafeInProcess,
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeRectangle => NodalOsOnnxNativeRuntimeCrashFixtureSafety.SafeInProcess,
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCircle => NodalOsOnnxNativeRuntimeCrashFixtureSafety.SafeInProcess,
            _ => NodalOsOnnxNativeRuntimeCrashFixtureSafety.QuarantinedOutOfProcessOnly
        };
    }

    public bool MetadataValid =>
        !string.IsNullOrWhiteSpace(ProbeId) &&
        Width > 0 && Height > 0 &&
        Width <= 4096 && Height <= 4096 &&
        Synthetic &&
        NoAuthority;
}

public sealed record NodalOsOnnxNativeRuntimeCrashProbeResult(
    string ResultId,
    string ProbeId,
    NodalOsOnnxNativeRuntimeCrashFixtureKind FixtureKind,
    NodalOsOnnxNativeRuntimeCrashStage Stage,
    int Width,
    int Height,
    NodalOsOnnxNativeRuntimeCrashProbeStatus Status,
    NodalOsOnnxNativeRuntimeCrashKind CrashKind,
    bool RanInProcess,
    bool RanOutOfProcess,
    bool HostSurvived,
    int? BoxesDetected,
    int? RecognitionAttempts,
    bool NoAuthority,
    bool CallsSaas,
    bool RawPersisted,
    string Reason)
{
    // Maps an out-of-process child outcome to a controlled probe status. A native crash that
    // would have killed an in-process host is reported as NativeRuntimeCrash, never as a host death.
    public static NodalOsOnnxNativeRuntimeCrashProbeStatus MapCrashKind(NodalOsOnnxNativeRuntimeCrashKind crashKind)
    {
        return crashKind switch
        {
            NodalOsOnnxNativeRuntimeCrashKind.NoCrash => NodalOsOnnxNativeRuntimeCrashProbeStatus.Passed,
            NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation => NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash,
            NodalOsOnnxNativeRuntimeCrashKind.NativeAbort => NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash,
            NodalOsOnnxNativeRuntimeCrashKind.ProcessExitNonZero => NodalOsOnnxNativeRuntimeCrashProbeStatus.ProcessCrashed,
            NodalOsOnnxNativeRuntimeCrashKind.TimedOut => NodalOsOnnxNativeRuntimeCrashProbeStatus.TimedOut,
            NodalOsOnnxNativeRuntimeCrashKind.InvalidOutput => NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime,
            NodalOsOnnxNativeRuntimeCrashKind.ManagedException => NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime,
            _ => NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime
        };
    }
}

public sealed record NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry(
    string EntryId,
    NodalOsOnnxNativeRuntimeCrashProbeRequest Request,
    NodalOsOnnxNativeRuntimeCrashFixtureSafety FixtureSafety,
    NodalOsOnnxNativeRuntimeCrashProbeStatus ExpectedStatus,
    bool AllowedInProcess,
    bool RequiresGuard,
    bool BlockedBeforeRuntime,
    string Reason);

public sealed record NodalOsOnnxNativeRuntimeCrashProbeMatrix(
    string MatrixId,
    IReadOnlyList<NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry> Entries,
    int SafeFixtureCount,
    int QuarantinedFixtureCount,
    int BlockedBeforeRuntimeCount,
    bool NoSaas,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoAuthority,
    DateTimeOffset CreatedAtUtc);
