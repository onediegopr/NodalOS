using System.Diagnostics;
using System.Net.Sockets;
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
                if (clickResult.Status == BrowserVerificationStatus.Verified.ToString())
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
                await page.DisposeAsync().ConfigureAwait(false);
            if (session is not null)
                await session.DisposeAsync().ConfigureAwait(false);

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
                CleanupCompleted: CleanupLooksComplete(processId, port, profileDir));

            var cleanupStatus = cleanupHealth.CleanupCompleted ? BrowserRuntimeGateStatus.Passed : BrowserRuntimeGateStatus.Failed;
            var cleanupError = cleanupHealth.CleanupCompleted ? BrowserRuntimeErrorCode.None : BrowserRuntimeErrorCode.CleanupFailed;
            gates.Add(Result("Gate 10 - Cleanup", cleanupStatus, cleanupError, cleanupHealth.CleanupCompleted ? "No managed process, CDP port or temp profile remains." : "Managed process, CDP port or temp profile may remain.", cleanupHealth, DateTimeOffset.UtcNow));
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

    private static bool CleanupLooksComplete(int? processId, int? port, string profileDir)
    {
        if (processId is not null && IsProcessAlive(processId.Value))
            return false;
        if (port is not null && IsTcpPortOpen(port.Value))
            return false;
        if (!string.IsNullOrWhiteSpace(profileDir) && Directory.Exists(profileDir))
            return false;
        return true;
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
            return task.Wait(TimeSpan.FromMilliseconds(250)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

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
