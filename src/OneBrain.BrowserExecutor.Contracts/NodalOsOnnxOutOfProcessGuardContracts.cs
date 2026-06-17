namespace OneBrain.BrowserExecutor.Contracts;

// M210 — Out-of-process ONNX runtime guard contracts.
// The guard launches an isolated child runner to attempt probes that may crash the native ONNX
// Runtime. A child crash, timeout, or invalid output must be contained as a controlled result;
// the parent (and the test host) must always survive.

public sealed record NodalOsOnnxOutOfProcessGuardRequest(
    string GuardId,
    NodalOsOnnxNativeRuntimeCrashProbeRequest ProbeRequest,
    string RunnerCommand,
    IReadOnlyList<string> RunnerArguments,
    int TimeoutMs,
    long MaxOutputBytes,
    bool AllowRawPersistence)
{
    // Fixtures must be synthetic, redacted, non-sensitive, non-full-screen before the child launches.
    public bool PassesPreLaunchSafetyGate()
    {
        if (ProbeRequest is null) return false;
        if (ProbeRequest.FullScreen || ProbeRequest.Sensitive) return false;
        if (ProbeRequest.OriginalRawPersisted) return false;
        if (!ProbeRequest.Synthetic) return false;
        if (!ProbeRequest.NoAuthority) return false;
        if (AllowRawPersistence) return false;
        if (ProbeRequest.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface) return false;
        return true;
    }
}

// Child runner self-report DTO. The runner serializes this to stdout as JSON.
public sealed record NodalOsOnnxOutOfProcessRunnerReport(
    string ProbeId,
    string Stage,
    string Status,
    int? BoxesDetected,
    int? RecognitionAttempts,
    bool CallsSaas,
    bool RawPersisted,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOnnxOutOfProcessGuardResult(
    string ResultId,
    string GuardId,
    NodalOsOnnxNativeRuntimeCrashProbeResult ProbeResult,
    int? ExitCode,
    bool TimedOut,
    bool ParentSurvived,
    bool ChildLaunched,
    bool BlockedBeforeLaunch,
    bool TempFilesCleaned,
    bool OrphanProcessLeft,
    bool RawPersisted,
    bool CallsSaas,
    bool NoAuthority,
    string StdErrSummary,
    string Reason);
