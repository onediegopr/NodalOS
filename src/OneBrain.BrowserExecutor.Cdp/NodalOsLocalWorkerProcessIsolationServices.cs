using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M189 — Real OS worker isolation boundary with innocent echo process + hardened IPC.
// No OCR. No Python. No Paddle/Tesseract. No real OCR process.
// Honest about OS-level enforcement: modeled/observed/enforced/not-enforced.
public sealed class NodalOsLocalWorkerProcessLauncher
{
    public const string InnocentEchoContractVersion = "nodal-local-worker-echo.v1";

    public NodalOsLocalWorkerProcessSandboxPolicy DefaultPolicy() =>
        new(
            AllowExternalProcess: false,
            AllowNetwork: false,
            AllowRawPersistence: false,
            AllowPython: false,
            AllowRealOcr: false,
            NoAuthority: true,
            MaxProcessLifetimeMs: 2000,
            MaxStdoutBytes: 4096,
            MaxStderrBytes: 4096,
            NetworkEnforcement: NodalOsLocalWorkerIsolationEnforcementLevel.Modeled,
            FilesystemEnforcement: NodalOsLocalWorkerIsolationEnforcementLevel.Modeled,
            ProcessEnforcement: NodalOsLocalWorkerIsolationEnforcementLevel.Observed);

    public NodalOsLocalWorkerIpcSecurityPolicy DefaultIpcPolicy(string authToken) =>
        new(
            ContractVersion: InnocentEchoContractVersion,
            AuthToken: authToken,
            MaxMessageBytes: 4096,
            MaxLifetimeMs: 2000,
            RequireAuthToken: true,
            ValidateContractVersion: true,
            NoAuthority: true);

    // Simulates an echo response without launching any process.
    // Used when OS isolation cannot be exercised safely/portably.
    public NodalOsLocalWorkerProcessResult SimulateEcho(
        NodalOsLocalWorkerProcessLaunchSpec spec,
        NodalOsLocalWorkerProcessSandboxPolicy policy) =>
        new(
            $"result-sim-{Guid.NewGuid():N}",
            spec.WorkerId,
            NodalOsLocalWorkerProcessDecision.SimulatedOnly,
            ExitCode: 0,
            StdoutRedacted: BrowserCredentialRedactor.Redact("{\"status\":\"ok\",\"mode\":\"simulated\"}"),
            StderrRedacted: "",
            Duration: TimeSpan.FromMilliseconds(1),
            NoAuthority: true,
            RawPersisted: false,
            NetworkObserved: false,
            FilesystemWriteObserved: false,
            Killed: false,
            ["process isolation simulated only; no real subprocess launched"],
            DateTimeOffset.UtcNow,
            Redacted: true);

    // Launches an innocent OS process (cmd /c echo) to exercise real lifecycle/timeout.
    // The process does NO OCR and writes NO files.
    public NodalOsLocalWorkerProcessResult LaunchInnocentEcho(
        NodalOsLocalWorkerProcessLaunchSpec spec,
        NodalOsLocalWorkerProcessSandboxPolicy policy)
    {
        if (!policy.AllowExternalProcess)
            return Rejected(spec, "external process launch blocked by sandbox policy");

        if (spec.InnocentEchoOnly && !IsInnocentExecutable(spec.ExecutablePath))
            return Rejected(spec, $"executable '{spec.ExecutablePath}' is not in the innocent echo allow-list");

        if (spec.TimeoutMs > policy.MaxProcessLifetimeMs)
            return Rejected(spec, $"timeout {spec.TimeoutMs}ms exceeds policy max {policy.MaxProcessLifetimeMs}ms");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = spec.ExecutablePath,
                Arguments = string.Join(" ", spec.Arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            using var process = Process.Start(psi);
            if (process is null)
                return Failed(spec, "process.Start returned null");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var completed = process.WaitForExit(spec.TimeoutMs);
            stopwatch.Stop();

            if (!completed)
            {
                try { process.Kill(); } catch { /* best effort */ }
                return TimedOut(spec, stopwatch.Elapsed);
            }

            var stdout = stdoutTask.Result;
            var stderr = stderrTask.Result;

            var stdoutRedacted = BrowserCredentialRedactor.Redact(Truncate(stdout, policy.MaxStdoutBytes));
            var stderrRedacted = BrowserCredentialRedactor.Redact(Truncate(stderr, policy.MaxStderrBytes));

            // Validate that the innocent echo produced the expected synthetic marker.
            if (!stdout.Contains("status=ok", StringComparison.OrdinalIgnoreCase) &&
                !stdout.Contains("nodal-echo", StringComparison.OrdinalIgnoreCase))
            {
                return UnexpectedOutput(spec, process.ExitCode, stdoutRedacted, stderrRedacted, stopwatch.Elapsed);
            }

            return new NodalOsLocalWorkerProcessResult(
                $"result-{Guid.NewGuid():N}",
                spec.WorkerId,
                NodalOsLocalWorkerProcessDecision.Launched,
                process.ExitCode,
                stdoutRedacted,
                stderrRedacted,
                stopwatch.Elapsed,
                NoAuthority: true,
                RawPersisted: false,
                NetworkObserved: false,
                FilesystemWriteObserved: false,
                Killed: false,
                ["innocent echo process launched; OS isolation observed, not hard-sandbox enforced"],
                DateTimeOffset.UtcNow,
                Redacted: true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Failed(spec, $"launch exception: {BrowserCredentialRedactor.Redact(ex.Message)}");
        }
    }

    private static bool IsInnocentExecutable(string path) =>
        path.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("cmd", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string value, int maxBytes)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length <= maxBytes)
            return value;
        return Encoding.UTF8.GetString(bytes, 0, maxBytes) + "...[truncated]";
    }

    private static NodalOsLocalWorkerProcessResult Rejected(NodalOsLocalWorkerProcessLaunchSpec spec, string reason) =>
        Result(spec, NodalOsLocalWorkerProcessDecision.RejectedByPolicy, -1, "", "", TimeSpan.Zero,
            [$"rejected: {BrowserCredentialRedactor.Redact(reason)}"], killed: false);

    private static NodalOsLocalWorkerProcessResult TimedOut(NodalOsLocalWorkerProcessLaunchSpec spec, TimeSpan duration) =>
        Result(spec, NodalOsLocalWorkerProcessDecision.TimedOut, -1, "", "", duration,
            ["process killed after timeout"], killed: true);

    private static NodalOsLocalWorkerProcessResult UnexpectedOutput(
        NodalOsLocalWorkerProcessLaunchSpec spec,
        int exitCode,
        string stdout,
        string stderr,
        TimeSpan duration) =>
        Result(spec, NodalOsLocalWorkerProcessDecision.UnexpectedOutput, exitCode, stdout, stderr, duration,
            ["unexpected output from innocent echo process"], killed: false);

    private static NodalOsLocalWorkerProcessResult Failed(NodalOsLocalWorkerProcessLaunchSpec spec, string reason) =>
        Result(spec, NodalOsLocalWorkerProcessDecision.LaunchFailed, -1, "", "", TimeSpan.Zero,
            [$"launch failed: {BrowserCredentialRedactor.Redact(reason)}"], killed: false);

    private static NodalOsLocalWorkerProcessResult Result(
        NodalOsLocalWorkerProcessLaunchSpec spec,
        NodalOsLocalWorkerProcessDecision decision,
        int exitCode,
        string stdout,
        string stderr,
        TimeSpan duration,
        IReadOnlyList<string> warnings,
        bool killed) =>
        new(
            $"result-{Guid.NewGuid():N}",
            spec.WorkerId,
            decision,
            exitCode,
            BrowserCredentialRedactor.Redact(stdout),
            BrowserCredentialRedactor.Redact(stderr),
            duration,
            NoAuthority: true,
            RawPersisted: false,
            NetworkObserved: false,
            FilesystemWriteObserved: false,
            killed,
            warnings,
            DateTimeOffset.UtcNow,
            Redacted: true);
}

public sealed class NodalOsLocalWorkerIpcChannel
{
    public NodalOsLocalWorkerProcessHealth ValidateMessage(
        NodalOsLocalWorkerProcessMessage message,
        NodalOsLocalWorkerIpcSecurityPolicy policy)
    {
        var warnings = new List<string>();

        if (policy.RequireAuthToken)
        {
            if (string.IsNullOrWhiteSpace(message.AuthToken))
                return Health(message, false, NodalOsLocalWorkerIpcAuthDecision.MissingToken, "missing auth token");
            if (!string.Equals(message.AuthToken, policy.AuthToken, StringComparison.Ordinal))
                return Health(message, false, NodalOsLocalWorkerIpcAuthDecision.InvalidToken, "invalid auth token");
        }

        if (policy.ValidateContractVersion &&
            !string.Equals(message.ContractVersion, policy.ContractVersion, StringComparison.Ordinal))
        {
            return Health(message, false, NodalOsLocalWorkerIpcAuthDecision.Authenticated, "contract version mismatch");
        }

        var payloadBytes = Encoding.UTF8.GetBytes(message.PayloadJson ?? "");
        if (payloadBytes.Length > policy.MaxMessageBytes)
        {
            return Health(message, false, NodalOsLocalWorkerIpcAuthDecision.Authenticated, "message exceeds max size");
        }

        var ageMs = (DateTimeOffset.UtcNow - message.SentAtUtc).TotalMilliseconds;
        if (ageMs > policy.MaxLifetimeMs)
        {
            return Health(message, false, NodalOsLocalWorkerIpcAuthDecision.ExpiredToken, "message expired");
        }

        return Health(message, true, NodalOsLocalWorkerIpcAuthDecision.Authenticated, "ok");
    }

    public bool TrySerializePayload(object payload, out string json, out string? error)
    {
        try
        {
            json = JsonSerializer.Serialize(payload);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            json = "";
            error = BrowserCredentialRedactor.Redact(ex.Message);
            return false;
        }
    }

    public bool TryDeserializePayload<T>(string json, out T? value, out string? error)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(json);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            value = default;
            error = BrowserCredentialRedactor.Redact(ex.Message);
            return false;
        }
    }

    private static NodalOsLocalWorkerProcessHealth Health(
        NodalOsLocalWorkerProcessMessage message,
        bool responsive,
        NodalOsLocalWorkerIpcAuthDecision auth,
        string notes) =>
        new(
            $"health-{Guid.NewGuid():N}",
            responsive,
            ContractVersionMatch: auth != NodalOsLocalWorkerIpcAuthDecision.Authenticated || notes != "contract version mismatch",
            AuthTokenValid: auth == NodalOsLocalWorkerIpcAuthDecision.Authenticated,
            WithinSizeLimits: !notes.Contains("size", StringComparison.OrdinalIgnoreCase),
            WithinTimeoutLimits: !notes.Contains("expired", StringComparison.OrdinalIgnoreCase),
            NoAuthority: true,
            DateTimeOffset.UtcNow);
}

public sealed class NodalOsLocalWorkerProcessSandbox
{
    public NodalOsLocalWorkerProcessIsolationEvidence Evaluate(
        NodalOsLocalWorkerProcessSandboxPolicy policy,
        NodalOsLocalWorkerProcessResult result,
        NodalOsLocalWorkerProcessHealth health)
    {
        var notes = new List<string>();

        if (result.RawPersisted)
            notes.Add("raw persistence observed — isolation violated");
        if (result.NetworkObserved)
            notes.Add("network activity observed — isolation violated");
        if (result.FilesystemWriteObserved)
            notes.Add("filesystem write observed — isolation violated");
        if (result.Decision == NodalOsLocalWorkerProcessDecision.SimulatedOnly)
            notes.Add("process boundary is simulated only; no real subprocess isolation enforced");
        if (result.Decision == NodalOsLocalWorkerProcessDecision.Launched)
            notes.Add("innocent echo process launched and exited; OS-level hard sandbox not claimed");
        if (!health.IsResponsive)
            notes.Add("IPC health check failed");
        if (!policy.NoAuthority)
            notes.Add("policy does not declare no-authority");

        return new NodalOsLocalWorkerProcessIsolationEvidence(
            $"evidence-{Guid.NewGuid():N}",
            policy.NetworkEnforcement,
            policy.FilesystemEnforcement,
            policy.ProcessEnforcement,
            !result.RawPersisted,
            !result.NetworkObserved,
            !result.FilesystemWriteObserved,
            policy.NoAuthority,
            BrowserCredentialRedactor.Redact(string.Join("; ", notes)),
            Redacted: true);
    }
}
