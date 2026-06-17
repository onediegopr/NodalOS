using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M210 — Out-of-process ONNX runtime guard.
// Launches an isolated child runner to attempt a probe that may crash the native ONNX Runtime.
// Any child crash / timeout / invalid output is contained as a controlled probe result; the parent
// process and test host always survive. Temp files are always cleaned; no orphan child is left.
public sealed class NodalOsOnnxOutOfProcessGuard
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Known native-fatal Windows/Unix exit codes mapped to native crash kinds.
    private static readonly Dictionary<int, NodalOsOnnxNativeRuntimeCrashKind> NativeFatalExitCodes = new()
    {
        [unchecked((int)0xC0000005)] = NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation, // STATUS_ACCESS_VIOLATION
        [unchecked((int)0xC00000FD)] = NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation, // STATUS_STACK_OVERFLOW
        [unchecked((int)0xC0000409)] = NodalOsOnnxNativeRuntimeCrashKind.NativeAbort,           // STATUS_STACK_BUFFER_OVERRUN / fail-fast
        [unchecked((int)0xC000001D)] = NodalOsOnnxNativeRuntimeCrashKind.NativeAbort,           // STATUS_ILLEGAL_INSTRUCTION
        [139] = NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation,                        // 128 + SIGSEGV
        [134] = NodalOsOnnxNativeRuntimeCrashKind.NativeAbort                                    // 128 + SIGABRT
    };

    public NodalOsOnnxOutOfProcessGuardResult Run(NodalOsOnnxOutOfProcessGuardRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        // Pre-launch safety gate: never launch a child for an unsafe fixture.
        if (!request.PassesPreLaunchSafetyGate())
        {
            return BlockedBeforeLaunch(request, "fixture blocked before launch (full-screen/sensitive/raw/non-synthetic)");
        }

        var workingDir = Path.Combine(Path.GetTempPath(), $"nodal-os-onnx-guard-{Guid.NewGuid():N}");
        var requestFile = Path.Combine(workingDir, "probe-request.json");
        var tempCleaned = false;
        Process? process = null;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var timedOut = false;
        var orphanLeft = false;

        try
        {
            Directory.CreateDirectory(workingDir);
            File.WriteAllText(requestFile, JsonSerializer.Serialize(request.ProbeRequest, JsonOptions));

            var args = new List<string>(request.RunnerArguments) { "--request", requestFile };
            var psi = new ProcessStartInfo
            {
                FileName = request.RunnerCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            process = new Process { StartInfo = psi };
            process.OutputDataReceived += (_, e) => AppendCapped(stdout, e.Data, request.MaxOutputBytes);
            process.ErrorDataReceived += (_, e) => AppendCapped(stderr, e.Data, request.MaxOutputBytes);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(request.TimeoutMs))
            {
                timedOut = true;
                TryKillTree(process);
            }

            // Ensure async readers flush.
            process.WaitForExit();

            var exitCode = SafeExitCode(process);
            orphanLeft = !process.HasExited;

            var (crashKind, statusOverride) = ClassifyOutcome(timedOut, exitCode, stdout.ToString(), out var report);

            var probeResult = BuildProbeResult(request, crashKind, statusOverride, report, exitCode, ranOutOfProcess: true);

            return new NodalOsOnnxOutOfProcessGuardResult(
                $"guard-result-{Guid.NewGuid():N}",
                request.GuardId,
                probeResult,
                exitCode,
                timedOut,
                ParentSurvived: true,
                ChildLaunched: true,
                BlockedBeforeLaunch: false,
                TempFilesCleaned: false, // set after cleanup below
                OrphanProcessLeft: orphanLeft,
                RawPersisted: report?.RawPersisted ?? false,
                CallsSaas: report?.CallsSaas ?? false,
                NoAuthority: report?.NoAuthority ?? true,
                StdErrSummary: BrowserCredentialRedactor.Redact(Truncate(stderr.ToString(), 512)),
                Reason: BrowserCredentialRedactor.Redact(probeResult.Reason)) with
            {
                TempFilesCleaned = (tempCleaned = CleanupTemp(workingDir))
            };
        }
        catch (Exception ex)
        {
            // Even a failure to launch is contained: the parent survives.
            tempCleaned = CleanupTemp(workingDir);
            var probeResult = BuildProbeResult(
                request,
                NodalOsOnnxNativeRuntimeCrashKind.ManagedException,
                statusOverride: NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime,
                report: null,
                exitCode: null,
                ranOutOfProcess: false);

            return new NodalOsOnnxOutOfProcessGuardResult(
                $"guard-result-{Guid.NewGuid():N}",
                request.GuardId,
                probeResult,
                ExitCode: null,
                TimedOut: timedOut,
                ParentSurvived: true,
                ChildLaunched: process is not null,
                BlockedBeforeLaunch: false,
                TempFilesCleaned: tempCleaned,
                OrphanProcessLeft: orphanLeft,
                RawPersisted: false,
                CallsSaas: false,
                NoAuthority: true,
                StdErrSummary: BrowserCredentialRedactor.Redact(Truncate(ex.Message, 512)),
                Reason: BrowserCredentialRedactor.Redact($"guard contained launch failure: {ex.GetType().Name}"));
        }
        finally
        {
            if (!tempCleaned) CleanupTemp(workingDir);
            process?.Dispose();
        }
    }

    private (NodalOsOnnxNativeRuntimeCrashKind CrashKind, NodalOsOnnxNativeRuntimeCrashProbeStatus? StatusOverride) ClassifyOutcome(
        bool timedOut,
        int? exitCode,
        string stdout,
        out NodalOsOnnxOutOfProcessRunnerReport? report)
    {
        report = null;

        if (timedOut)
            return (NodalOsOnnxNativeRuntimeCrashKind.TimedOut, null);

        if (exitCode is null)
            return (NodalOsOnnxNativeRuntimeCrashKind.Unknown, null);

        if (exitCode.Value != 0)
        {
            return NativeFatalExitCodes.TryGetValue(exitCode.Value, out var nativeKind)
                ? (nativeKind, null)
                : (NodalOsOnnxNativeRuntimeCrashKind.ProcessExitNonZero, null);
        }

        // Clean exit: require valid JSON report.
        report = TryParseReport(stdout);
        if (report is null)
            return (NodalOsOnnxNativeRuntimeCrashKind.InvalidOutput, null);

        var status = ParseStatus(report.Status);
        return (NodalOsOnnxNativeRuntimeCrashKind.NoCrash, status);
    }

    private static NodalOsOnnxOutOfProcessRunnerReport? TryParseReport(string stdout)
    {
        var json = ExtractJsonObject(stdout);
        if (json is null) return null;
        try
        {
            return JsonSerializer.Deserialize<NodalOsOnnxOutOfProcessRunnerReport>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout)) return null;
        var start = stdout.IndexOf('{');
        var end = stdout.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        return stdout.Substring(start, end - start + 1);
    }

    private static NodalOsOnnxNativeRuntimeCrashProbeStatus ParseStatus(string status)
    {
        return Enum.TryParse<NodalOsOnnxNativeRuntimeCrashProbeStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime;
    }

    private static NodalOsOnnxNativeRuntimeCrashProbeResult BuildProbeResult(
        NodalOsOnnxOutOfProcessGuardRequest request,
        NodalOsOnnxNativeRuntimeCrashKind crashKind,
        NodalOsOnnxNativeRuntimeCrashProbeStatus? statusOverride,
        NodalOsOnnxOutOfProcessRunnerReport? report,
        int? exitCode,
        bool ranOutOfProcess)
    {
        var status = statusOverride ?? NodalOsOnnxNativeRuntimeCrashProbeResult.MapCrashKind(crashKind);
        var reason = crashKind switch
        {
            NodalOsOnnxNativeRuntimeCrashKind.NoCrash => report?.Reason ?? "child returned controlled result",
            NodalOsOnnxNativeRuntimeCrashKind.TimedOut => $"child timed out after {request.TimeoutMs}ms; killed",
            NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation => $"child native access violation (exit {exitCode}); contained out-of-process",
            NodalOsOnnxNativeRuntimeCrashKind.NativeAbort => $"child native abort (exit {exitCode}); contained out-of-process",
            NodalOsOnnxNativeRuntimeCrashKind.ProcessExitNonZero => $"child exited non-zero ({exitCode})",
            NodalOsOnnxNativeRuntimeCrashKind.InvalidOutput => "child produced invalid/empty output",
            NodalOsOnnxNativeRuntimeCrashKind.ManagedException => "guard contained managed exception",
            _ => "unknown child outcome"
        };

        return new NodalOsOnnxNativeRuntimeCrashProbeResult(
            $"probe-result-{Guid.NewGuid():N}",
            request.ProbeRequest.ProbeId,
            request.ProbeRequest.FixtureKind,
            request.ProbeRequest.Stage,
            request.ProbeRequest.Width,
            request.ProbeRequest.Height,
            status,
            crashKind,
            RanInProcess: false,
            RanOutOfProcess: ranOutOfProcess,
            HostSurvived: true,
            BoxesDetected: report?.BoxesDetected,
            RecognitionAttempts: report?.RecognitionAttempts,
            NoAuthority: report?.NoAuthority ?? true,
            CallsSaas: report?.CallsSaas ?? false,
            RawPersisted: report?.RawPersisted ?? false,
            BrowserCredentialRedactor.Redact(reason));
    }

    private NodalOsOnnxOutOfProcessGuardResult BlockedBeforeLaunch(
        NodalOsOnnxOutOfProcessGuardRequest request,
        string reason)
    {
        var probeResult = new NodalOsOnnxNativeRuntimeCrashProbeResult(
            $"probe-result-{Guid.NewGuid():N}",
            request.ProbeRequest?.ProbeId ?? "unknown",
            request.ProbeRequest?.FixtureKind ?? NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe,
            request.ProbeRequest?.Stage ?? NodalOsOnnxNativeRuntimeCrashStage.SessionCreation,
            request.ProbeRequest?.Width ?? 0,
            request.ProbeRequest?.Height ?? 0,
            NodalOsOnnxNativeRuntimeCrashProbeStatus.UnsupportedFixture,
            NodalOsOnnxNativeRuntimeCrashKind.NoCrash,
            RanInProcess: false,
            RanOutOfProcess: false,
            HostSurvived: true,
            BoxesDetected: null,
            RecognitionAttempts: null,
            NoAuthority: true,
            CallsSaas: false,
            RawPersisted: false,
            BrowserCredentialRedactor.Redact(reason));

        return new NodalOsOnnxOutOfProcessGuardResult(
            $"guard-result-{Guid.NewGuid():N}",
            request.GuardId,
            probeResult,
            ExitCode: null,
            TimedOut: false,
            ParentSurvived: true,
            ChildLaunched: false,
            BlockedBeforeLaunch: true,
            TempFilesCleaned: true,
            OrphanProcessLeft: false,
            RawPersisted: false,
            CallsSaas: false,
            NoAuthority: true,
            StdErrSummary: string.Empty,
            BrowserCredentialRedactor.Redact(reason));
    }

    private static void AppendCapped(StringBuilder sb, string? data, long maxBytes)
    {
        if (data is null) return;
        if (sb.Length >= maxBytes) return;
        sb.AppendLine(data);
    }

    private static void TryKillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort; the guard never throws on cleanup.
        }
    }

    private static int? SafeExitCode(Process process)
    {
        try
        {
            return process.HasExited ? process.ExitCode : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool CleanupTemp(string workingDir)
    {
        try
        {
            if (Directory.Exists(workingDir))
                Directory.Delete(workingDir, recursive: true);
            return !Directory.Exists(workingDir);
        }
        catch
        {
            return false;
        }
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max];
    }
}
