using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public enum BrowserRuntimeGateStatus { Passed, Failed, Skipped, Warning }

public enum BrowserRuntimeErrorCode
{
    None,
    LauncherFailed,
    CdpEndpointUnavailable,
    TargetDiscoveryFailed,
    TargetStale,
    TargetDetached,
    ActionRejected,
    ActionTimeout,
    VerificationFailed,
    VerificationUncertain,
    IdempotencyRejected,
    CleanupFailed,
    UnexpectedException,
    EnvironmentUnsupported
}

public sealed record BrowserRuntimeSmokeOptions(
    string? BrowserExecutablePath,
    Uri FixtureUri,
    bool Headless = true,
    TimeSpan? GateTimeout = null);

public sealed record BrowserRuntimeHealthSnapshot(
    string RuntimeKind,
    string BrowserExecutable,
    Uri? CdpEndpoint,
    int? Port,
    string ProfileMode,
    int? ProcessId,
    string? BrowserSessionId,
    string? TargetId,
    Uri? TargetUrl,
    string? TargetTitle,
    BrowserHeartbeatStatus LivenessStatus,
    bool UsesRealProfile,
    bool CleanupCompleted);

public sealed record BrowserRuntimeDiagnostic(
    BrowserRuntimeErrorCode ErrorCode,
    string Message,
    IReadOnlyList<string> EvidenceRefs,
    BrowserRuntimeHealthSnapshot? Health);

public sealed record BrowserRuntimeGateResult(
    string GateName,
    BrowserRuntimeGateStatus Status,
    BrowserRuntimeDiagnostic Diagnostic,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc)
{
    public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
}

public sealed record BrowserRuntimeSmokeReport(
    string ReportId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    IReadOnlyList<BrowserRuntimeGateResult> Gates,
    BrowserRuntimeHealthSnapshot FinalHealth,
    bool ContainsSecrets)
{
    public bool Passed => Gates.All(gate => gate.Status == BrowserRuntimeGateStatus.Passed);
    public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
}

public sealed record BrowserRuntimeSmokeCleanupDiagnostics(
    DateTimeOffset CleanupStartedAtUtc,
    DateTimeOffset CleanupCompletedAtUtc,
    int? BrowserProcessId,
    int? DebugPort,
    string RedactedProfilePath,
    bool CleanupCompleted,
    bool LeftoverProcessDetected,
    bool CdpPortOpen,
    bool ProfileDirectoryExists,
    string ProcessCleanupOutcome,
    string ProfileDeleteOutcome,
    string CdpPortCleanupOutcome,
    string WebSocketCloseOutcome,
    IReadOnlyList<string> CleanupWarnings)
{
    public string ToDiagnosticMessage() =>
        $"processOutcome={ProcessCleanupOutcome}; profileOutcome={ProfileDeleteOutcome}; " +
        $"cdpPortOutcome={CdpPortCleanupOutcome}; websocketOutcome={WebSocketCloseOutcome}; " +
        $"leftoverProcessDetected={LeftoverProcessDetected}; profileDirectoryExists={ProfileDirectoryExists}; " +
        $"cdpPortOpen={CdpPortOpen}; profile={RedactedProfilePath}; warnings={string.Join("|", CleanupWarnings)}";
}

public static class BrowserRuntimeSmokeCleanupProbe
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    public static async Task<BrowserRuntimeSmokeCleanupDiagnostics> ProbeAsync(
        int? browserProcessId,
        int? debugPort,
        string? profileDirectory,
        string webSocketCloseOutcome,
        IReadOnlyList<string>? cleanupWarnings = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var deadline = started + (timeout ?? DefaultTimeout);
        var warnings = new List<string>((cleanupWarnings ?? []).Select(Sanitize));
        var profileDeleteOutcome = string.IsNullOrWhiteSpace(profileDirectory)
            ? "not-created"
            : "pending";

        while (DateTimeOffset.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsOwnedTemporaryProfile(profileDirectory) && Directory.Exists(profileDirectory!))
                profileDeleteOutcome = TryDeleteProfile(profileDirectory!, warnings);
            else if (!string.IsNullOrWhiteSpace(profileDirectory))
                profileDeleteOutcome = Directory.Exists(profileDirectory) ? "not-owned-skipped" : "deleted";

            var processAlive = browserProcessId is not null && IsProcessAlive(browserProcessId.Value);
            var portOpen = debugPort is not null && IsTcpPortOpen(debugPort.Value);
            var profileExists = !string.IsNullOrWhiteSpace(profileDirectory) && Directory.Exists(profileDirectory);

            if (!processAlive && !portOpen && !profileExists)
            {
                return Create(
                    started,
                    browserProcessId,
                    debugPort,
                    profileDirectory,
                    cleanupCompleted: true,
                    processAlive,
                    portOpen,
                    profileExists,
                    ProcessOutcome(browserProcessId, processAlive),
                    string.IsNullOrWhiteSpace(profileDirectory) ? "not-created" : "deleted",
                    PortOutcome(debugPort, portOpen),
                    webSocketCloseOutcome,
                    warnings);
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        var leftoverProcess = browserProcessId is not null && IsProcessAlive(browserProcessId.Value);
        var cdpPortOpen = debugPort is not null && IsTcpPortOpen(debugPort.Value);
        var profileDirectoryExists = !string.IsNullOrWhiteSpace(profileDirectory) && Directory.Exists(profileDirectory);

        return Create(
            started,
            browserProcessId,
            debugPort,
            profileDirectory,
            cleanupCompleted: false,
            leftoverProcess,
            cdpPortOpen,
            profileDirectoryExists,
            ProcessOutcome(browserProcessId, leftoverProcess),
            profileDirectoryExists ? profileDeleteOutcome : string.IsNullOrWhiteSpace(profileDirectory) ? "not-created" : "deleted",
            PortOutcome(debugPort, cdpPortOpen),
            webSocketCloseOutcome,
            warnings);
    }

    private static BrowserRuntimeSmokeCleanupDiagnostics Create(
        DateTimeOffset started,
        int? browserProcessId,
        int? debugPort,
        string? profileDirectory,
        bool cleanupCompleted,
        bool processAlive,
        bool portOpen,
        bool profileExists,
        string processOutcome,
        string profileDeleteOutcome,
        string portOutcome,
        string webSocketCloseOutcome,
        IReadOnlyList<string> warnings) =>
        new(
            started,
            DateTimeOffset.UtcNow,
            browserProcessId,
            debugPort,
            RedactProfilePath(profileDirectory),
            cleanupCompleted,
            processAlive,
            portOpen,
            profileExists,
            processOutcome,
            profileDeleteOutcome,
            portOutcome,
            webSocketCloseOutcome,
            warnings);

    private static string ProcessOutcome(int? processId, bool processAlive) =>
        processId is null
            ? "not-started"
            : processAlive
                ? "owned-process-still-alive"
                : "owned-process-exited";

    private static string PortOutcome(int? port, bool portOpen) =>
        port is null
            ? "not-bound"
            : portOpen
                ? "cdp-port-still-open"
                : "cdp-port-closed";

    private static string TryDeleteProfile(string profileDirectory, List<string> warnings)
    {
        try
        {
            Directory.Delete(profileDirectory, recursive: true);
            return "deleted";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            warnings.Add($"{ex.GetType().Name}:{Sanitize(ex.Message)}");
            return "delete-retry-pending";
        }
    }

    private static bool IsOwnedTemporaryProfile(string? profileDirectory)
    {
        if (string.IsNullOrWhiteSpace(profileDirectory))
            return false;

        var leaf = Path.GetFileName(profileDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!leaf.StartsWith("onebrain-cdp-", StringComparison.Ordinal))
            return false;

        var full = Path.GetFullPath(profileDirectory);
        var temp = Path.GetFullPath(Path.GetTempPath());
        return full.StartsWith(temp, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTcpPortOpen(int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync("127.0.0.1", port);
            return task.Wait(TimeSpan.FromMilliseconds(150)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static string RedactProfilePath(string? profileDirectory)
    {
        if (string.IsNullOrWhiteSpace(profileDirectory))
            return "";

        var leaf = Path.GetFileName(profileDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(leaf) ? "" : leaf;
    }

    private static string Sanitize(string value) =>
        SecretRedactor.Redact(value).Replace(Environment.NewLine, " ", StringComparison.Ordinal);
}

public sealed class BrowserRuntimeSmokeRunner
{
    public async Task<BrowserRuntimeSmokeReport> RunAsync(BrowserRuntimeSmokeOptions options, CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var gates = new List<BrowserRuntimeGateResult>();
        var browserPath = options.BrowserExecutablePath ?? ChromeCdpBrowserLauncher.FindBrowserExecutable();

        if (browserPath is null || !File.Exists(browserPath))
        {
            var final = new BrowserRuntimeHealthSnapshot(
                "ChromeCdp",
                "",
                null,
                null,
                "Temporary",
                null,
                null,
                null,
                null,
                null,
                BrowserHeartbeatStatus.Unknown,
                UsesRealProfile: false,
                CleanupCompleted: true);
            gates.Add(Result("Gate 1 - Control Plane / Launcher", BrowserRuntimeGateStatus.Skipped, BrowserRuntimeErrorCode.EnvironmentUnsupported, "Chrome/Edge executable is not available.", final, started));
            return new BrowserRuntimeSmokeReport(Guid.NewGuid().ToString("N"), started, DateTimeOffset.UtcNow, gates, final, ContainsSecrets: false);
        }

        ChromeCdpBrowserSession? session = null;
        ChromeCdpPageSession? page = null;
        var processId = (int?)null;
        var port = (int?)null;
        var profileDir = "";
        var targetContext = (BrowserTargetContext?)null;
        var cleanupWarnings = new List<string>();
        var webSocketCloseOutcome = "not-opened";

        try
        {
            var launcher = new ChromeCdpBrowserLauncher();
            await RunGateAsync(gates, "Gate 1 - Control Plane / Launcher", async () =>
            {
                session = await launcher.LaunchAsync(new ChromeCdpOptions(browserPath, Headless: options.Headless, StartupTimeout: TimeSpan.FromSeconds(10)), cancellationToken).ConfigureAwait(false);
                processId = session.ProcessId;
                port = session.Port;
                profileDir = session.UserDataDir;
                using var version = await session.GetVersionAsync(cancellationToken).ConfigureAwait(false);
                if (session.VersionEndpoint.Host != "127.0.0.1")
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.CdpEndpointUnavailable, "CDP endpoint is not localhost.");
                if (!Directory.Exists(session.UserDataDir) || !Path.GetFileName(session.UserDataDir).StartsWith("onebrain-cdp-", StringComparison.Ordinal))
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.LauncherFailed, "Temporary profile was not created.");
                if (!session.IsProcessAlive)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.LauncherFailed, "Browser process is not alive after launch.");
                return Ok("Launcher started Chrome with CDP localhost and temporary profile.", Health(session, null, null, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false));
            }).ConfigureAwait(false);

            if (session is null)
                throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.LauncherFailed, "Session was not created.");

            await RunGateAsync(gates, "Gate 2 - Target Discovery", async () =>
            {
                page = await session.CreatePageAsync(options.FixtureUri, cancellationToken).ConfigureAwait(false);
                var targets = await session.ListTargetsAsync(cancellationToken).ConfigureAwait(false);
                var pageTarget = targets.FirstOrDefault(target => target.Id == page.TargetId);
                if (pageTarget is null || string.IsNullOrWhiteSpace(pageTarget.Id))
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetDiscoveryFailed, "Fixture page target was not discovered.");
                return Ok("Discovered fixture page target.", Health(session, page, null, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false));
            }).ConfigureAwait(false);

            if (page is null)
                throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetDiscoveryFailed, "Page was not created.");

            await RunGateAsync(gates, "Gate 3 - TargetContext", async () =>
            {
                targetContext = await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false);
                var validation = targetContext.Validate();
                if (!validation.IsValid)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetDiscoveryFailed, string.Join("; ", validation.Errors));
                if (targetContext.Generation < 1)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetStale, "Target generation was not advanced by initial navigation.");
                return Ok("TargetContext is valid. Active/user-facing is currently limited and reported as null.", Health(session, page, targetContext, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false));
            }).ConfigureAwait(false);

            BrowserObservation? observation = null;
            await RunGateAsync(gates, "Gate 4 - Observe", async () =>
            {
                observation = await page.ObserveAsync("smoke-run", cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!observation.Url.ToString().Contains("basic-form.html", StringComparison.OrdinalIgnoreCase))
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetDiscoveryFailed, "Observation URL does not point to fixture.");
                if (!observation.VisibleTextSummary.Contains("Fixture ready", StringComparison.OrdinalIgnoreCase))
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.VerificationFailed, "Fixture text was not observed.");
                if (observation.EvidenceRefs.Count == 0)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.VerificationUncertain, "Observation did not include evidence refs.");
                return Ok("Observed fixture URL, title, text and actionables without service worker.", Health(session, page, observation.TargetContext, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false), observation.EvidenceRefs);
            }).ConfigureAwait(false);

            await RunGateAsync(gates, "Gate 5 - Act", async () =>
            {
                var context = await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false);
                var type = CreateAction("smoke-run", "act-type", "action-type", "idem-type", context, BrowserActionType.TypeText, "#nameInput", "Gate", new BrowserExpectedOutcome("input contains Gate", null, null, "Gate"));
                var typeResult = await page.ExecuteActionAsync(type, cancellationToken).ConfigureAwait(false);
                if (!typeResult.Executed)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.ActionRejected, typeResult.Error ?? "TypeText did not execute.");

                var click = CreateAction("smoke-run", "act-click", "action-click", "idem-click", await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false), BrowserActionType.Click, "#applyButton", null, new BrowserExpectedOutcome("result contains Gate", null, "Result: Gate", null));
                var clickResult = await page.ExecuteActionAsync(click, cancellationToken).ConfigureAwait(false);
                if (!clickResult.Executed)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.ActionRejected, clickResult.Error ?? "Click did not execute.");
                if (ChromeCdpPageSession.ActionResultIsVerified(clickResult))
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.VerificationFailed, "Action result was incorrectly marked verified.");

                var wait = CreateAction("smoke-run", "act-wait", "action-wait", "", await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false), BrowserActionType.WaitFor, "#result", null, new BrowserExpectedOutcome("wait for result", null, "Result: Gate", null), BrowserRiskClass.ReadOnly);
                var waitResult = await page.ExecuteActionAsync(wait, cancellationToken).ConfigureAwait(false);
                if (!waitResult.Executed)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.ActionTimeout, waitResult.Error ?? "Wait did not execute.");

                var missingIdem = CreateAction("smoke-run", "act-reject", "action-reject", "", await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false), BrowserActionType.Click, "#applyButton");
                var rejected = await page.ExecuteActionAsync(missingIdem, cancellationToken).ConfigureAwait(false);
                if (rejected.Executed)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.ActionRejected, "Modifying action without idempotency executed.");

                return Ok("Click, TypeText and Wait executed; modifying action without idempotency rejected.", Health(session, page, context, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false), [typeResult.Evidence.EvidenceId, clickResult.Evidence.EvidenceId, waitResult.Evidence.EvidenceId]);
            }).ConfigureAwait(false);

            await RunGateAsync(gates, "Gate 6 - Verify", async () =>
            {
                var context = await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false);
                var verifiedAction = CreateAction("smoke-run", "verify-ok", "action-verify-ok", "", context, BrowserActionType.Read, "#result", null, new BrowserExpectedOutcome("result contains Gate", null, "Result: Gate", null), BrowserRiskClass.ReadOnly);
                var verified = await page.VerifyAsync(verifiedAction, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (verified.Status != BrowserVerificationStatus.Verified || !verified.AllowsStepDone())
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.VerificationFailed, "Expected fixture result was not verified.");

                var uncertainAction = CreateAction("smoke-run", "verify-uncertain", "action-verify-uncertain", "", context, BrowserActionType.Read, "#result", null, new BrowserExpectedOutcome("ambiguous", null, null, null), BrowserRiskClass.ReadOnly);
                var uncertain = await page.VerifyAsync(uncertainAction, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (uncertain.Status != BrowserVerificationStatus.Uncertain || uncertain.AllowsStepDone())
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.VerificationUncertain, "Uncertain verification was treated as done.");

                return Ok("Verified expected change and preserved Uncertain != Done.", Health(session, page, context, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false), verified.EvidenceRefs.Concat(uncertain.EvidenceRefs).ToList());
            }).ConfigureAwait(false);

            await RunGateAsync(gates, "Gate 7 - Liveness / Stale", async () =>
            {
                var liveContext = await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false);
                var alive = await page.ProbeLivenessAsync(liveContext, cancellationToken).ConfigureAwait(false);
                if (!alive.IsStrongAlive)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetDetached, "Target did not report strong alive.");

                await page.NavigateAsync(new Uri(options.FixtureUri + "#stale"), cancellationToken).ConfigureAwait(false);
                var staleHeartbeat = await page.ProbeLivenessAsync(liveContext, cancellationToken).ConfigureAwait(false);
                if (staleHeartbeat.Status != BrowserHeartbeatStatus.Stale)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetStale, "Generation mismatch did not produce stale liveness.");

                var staleAction = CreateAction("smoke-run", "stale", "action-stale", "idem-stale", liveContext, BrowserActionType.Click, "#applyButton");
                var staleResult = await page.ExecuteActionAsync(staleAction, cancellationToken).ConfigureAwait(false);
                if (staleResult.Executed)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.TargetStale, "Stale modifying action executed.");

                return Ok("Alive target detected; stale generation blocks modifying action.", Health(session, page, liveContext, staleHeartbeat.Status, cleanupCompleted: false), [staleResult.Evidence.EvidenceId]);
            }).ConfigureAwait(false);

            await RunGateAsync(gates, "Gate 8 - Timeout / Cancel", async () =>
            {
                var context = await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false);
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
                var wait = CreateAction("smoke-run", "timeout", "action-timeout", "", context, BrowserActionType.WaitFor, "#missing", null, new BrowserExpectedOutcome("missing text", null, "text-that-does-not-exist", null), BrowserRiskClass.ReadOnly);
                var result = await page.ExecuteActionAsync(wait, timeout.Token).ConfigureAwait(false);
                if (result.Executed)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.ActionTimeout, "Timed wait unexpectedly executed.");
                if (string.IsNullOrWhiteSpace(result.Error))
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.ActionTimeout, "Timed wait did not report an error.");
                return Ok("Controlled timeout returned classified action failure and did not hang.", Health(session, page, context, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false), [result.Evidence.EvidenceId]);
            }).ConfigureAwait(false);

            await RunGateAsync(gates, "Gate 9 - Idempotency / Replay Safety", async () =>
            {
                var context = await page.GetCurrentTargetContextAsync("smoke-run", cancellationToken).ConfigureAwait(false);
                var action = CreateAction("smoke-run", "idem", "action-idem", "idem-duplicate", context, BrowserActionType.Click, "#applyButton");
                var first = await page.ExecuteActionAsync(action, cancellationToken).ConfigureAwait(false);
                var duplicate = await page.ExecuteActionAsync(action, cancellationToken).ConfigureAwait(false);
                var different = await page.ExecuteActionAsync(action with { Target = new BrowserActionTarget("candidate-different", "#nameInput", "Name", null) }, cancellationToken).ConfigureAwait(false);
                var read = CreateAction("smoke-run", "idem-read", "action-read", "", context, BrowserActionType.Read, "#intro", null, new BrowserExpectedOutcome("read intro", null, "Fixture", null), BrowserRiskClass.ReadOnly);
                var read1 = await page.ExecuteActionAsync(read, cancellationToken).ConfigureAwait(false);
                var read2 = await page.ExecuteActionAsync(read, cancellationToken).ConfigureAwait(false);

                if (!first.Executed || duplicate.Executed || different.Executed || !read1.Executed || !read2.Executed)
                    throw new BrowserRuntimeSmokeException(BrowserRuntimeErrorCode.IdempotencyRejected, "Idempotency/replay result was not as expected.");

                return Ok("Duplicate modifying action rejected; different fingerprint blocked; read-only repeated safely.", Health(session, page, context, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false), [first.Evidence.EvidenceId, duplicate.Evidence.EvidenceId, different.Evidence.EvidenceId]);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            gates.Add(Result("Unexpected smoke failure", BrowserRuntimeGateStatus.Failed, BrowserRuntimeErrorCode.UnexpectedException, ex.Message, Health(session, page, targetContext, BrowserHeartbeatStatus.Unknown, cleanupCompleted: false), DateTimeOffset.UtcNow));
        }
        finally
        {
            if (page is not null)
            {
                try
                {
                    await page.DisposeAsync().ConfigureAwait(false);
                    webSocketCloseOutcome = "closed-or-already-closed";
                }
                catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or IOException or ObjectDisposedException)
                {
                    webSocketCloseOutcome = $"cleanup-exception:{ex.GetType().Name}";
                    cleanupWarnings.Add($"page-dispose:{ex.GetType().Name}:{SecretRedactor.Redact(ex.Message)}");
                }
            }

            if (session is not null)
            {
                try
                {
                    await session.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ObjectDisposedException)
                {
                    cleanupWarnings.Add($"session-dispose:{ex.GetType().Name}:{SecretRedactor.Redact(ex.Message)}");
                }
            }

            var cleanupDiagnostics = await BrowserRuntimeSmokeCleanupProbe.ProbeAsync(
                processId,
                port,
                profileDir,
                webSocketCloseOutcome,
                cleanupWarnings).ConfigureAwait(false);

            var cleanupHealth = new BrowserRuntimeHealthSnapshot(
                "ChromeCdp",
                RedactPath(browserPath),
                port is null ? null : new Uri($"http://127.0.0.1:{port}/json/version"),
                port,
                "Temporary",
                processId,
                session?.BrowserSessionId,
                targetContext?.TargetId,
                targetContext?.Url,
                targetContext?.Title,
                BrowserHeartbeatStatus.Disconnected,
                UsesRealProfile: false,
                CleanupCompleted: cleanupDiagnostics.CleanupCompleted);

            var cleanupStatus = cleanupHealth.CleanupCompleted ? BrowserRuntimeGateStatus.Passed : BrowserRuntimeGateStatus.Failed;
            var cleanupError = cleanupHealth.CleanupCompleted ? BrowserRuntimeErrorCode.None : BrowserRuntimeErrorCode.CleanupFailed;
            var cleanupMessage = cleanupHealth.CleanupCompleted
                ? $"No managed process, CDP port or temp profile remains. {cleanupDiagnostics.ToDiagnosticMessage()}"
                : $"Managed process, CDP port or temp profile may remain. {cleanupDiagnostics.ToDiagnosticMessage()}";
            gates.Add(Result("Gate 10 - Cleanup", cleanupStatus, cleanupError, cleanupMessage, cleanupHealth, cleanupDiagnostics.CleanupStartedAtUtc));
        }

        var finished = DateTimeOffset.UtcNow;
        var finalHealth = gates.Last().Diagnostic.Health ?? new BrowserRuntimeHealthSnapshot("ChromeCdp", RedactPath(browserPath), null, null, "Temporary", null, null, null, null, null, BrowserHeartbeatStatus.Unknown, false, false);
        var report = new BrowserRuntimeSmokeReport(Guid.NewGuid().ToString("N"), started, finished, gates, finalHealth, ContainsSecrets: ContainsSecret(gates));
        return report with { ContainsSecrets = ContainsSecret(report) };
    }

    private static async Task RunGateAsync(List<BrowserRuntimeGateResult> gates, string name, Func<Task<BrowserRuntimeGateResult>> gate)
    {
        try
        {
            var result = await gate().ConfigureAwait(false);
            gates.Add(string.IsNullOrWhiteSpace(result.GateName) ? result with { GateName = name } : result);
        }
        catch (BrowserRuntimeSmokeException ex)
        {
            gates.Add(Result(name, BrowserRuntimeGateStatus.Failed, ex.ErrorCode, ex.Message, null, DateTimeOffset.UtcNow));
        }
        catch (OperationCanceledException ex)
        {
            gates.Add(Result(name, BrowserRuntimeGateStatus.Failed, BrowserRuntimeErrorCode.ActionTimeout, ex.Message, null, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            gates.Add(Result(name, BrowserRuntimeGateStatus.Failed, BrowserRuntimeErrorCode.UnexpectedException, ex.Message, null, DateTimeOffset.UtcNow));
        }
    }

    private static BrowserRuntimeGateResult Ok(string message, BrowserRuntimeHealthSnapshot health, IReadOnlyList<string>? evidenceRefs = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrowserRuntimeGateResult(
            "",
            BrowserRuntimeGateStatus.Passed,
            new BrowserRuntimeDiagnostic(BrowserRuntimeErrorCode.None, message, evidenceRefs ?? [], health),
            now,
            now);
    }

    private static BrowserRuntimeGateResult Result(string name, BrowserRuntimeGateStatus status, BrowserRuntimeErrorCode error, string message, BrowserRuntimeHealthSnapshot? health, DateTimeOffset started)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrowserRuntimeGateResult(name, status, new BrowserRuntimeDiagnostic(error, message, [], health), started, now);
    }

    private static BrowserRuntimeHealthSnapshot Health(ChromeCdpBrowserSession? session, ChromeCdpPageSession? page, BrowserTargetContext? target, BrowserHeartbeatStatus liveness, bool cleanupCompleted) =>
        new(
            "ChromeCdp",
            session is null ? "" : "Chrome/Edge",
            session?.VersionEndpoint,
            session?.Port,
            "Temporary",
            session?.ProcessId,
            session?.BrowserSessionId,
            page?.TargetId ?? target?.TargetId,
            target?.Url,
            target?.Title,
            liveness,
            UsesRealProfile: false,
            cleanupCompleted);

    private static BrowserAction CreateAction(
        string runId,
        string stepId,
        string actionId,
        string idempotencyKey,
        BrowserTargetContext target,
        BrowserActionType type,
        string selector,
        string? input = null,
        BrowserExpectedOutcome? expected = null,
        BrowserRiskClass risk = BrowserRiskClass.Low) =>
        new(
            ActionId: actionId,
            IdempotencyKey: idempotencyKey,
            RunId: runId,
            StepId: stepId,
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: type,
            Target: new BrowserActionTarget(selector.TrimStart('#'), selector, selector, null),
            Input: input is null ? null : new BrowserActionInput(input, input, HasModifyingValue: true),
            ExpectedOutcome: expected ?? new BrowserExpectedOutcome("fixture text visible", null, "Browser Executor Fixture", null),
            RiskClass: risk,
            TimeoutMs: 8000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static string RedactPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        return Path.GetFileName(path);
    }

    private static bool ContainsSecret(BrowserRuntimeSmokeReport report) =>
        ContainsSecret(report.Gates);

    private static bool ContainsSecret(IEnumerable<BrowserRuntimeGateResult> gates) =>
        gates.Any(gate =>
            SecretRedactor.ContainsSecret(gate.Diagnostic.Message) ||
            gate.Diagnostic.EvidenceRefs.Any(SecretRedactor.ContainsSecret));
}

public sealed class BrowserRuntimeSmokeException : Exception
{
    public BrowserRuntimeSmokeException(BrowserRuntimeErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public BrowserRuntimeErrorCode ErrorCode { get; }
}
