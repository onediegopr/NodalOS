using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.BrowserRuntime;

public sealed record CloakBrowserCdpHealthcheckOptions(
    string RepositoryRoot,
    string LockfilePath,
    string? RuntimeArtifactPath = null,
    TimeSpan? Timeout = null)
{
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(30);
}

public sealed record CloakBrowserCdpHealthcheckResult(
    string Status,
    string Decision,
    string Reason,
    string RuntimeProvider,
    string CdpMode,
    string RuntimeVersion,
    string BinarySha256,
    string? BrowserVersion,
    string? ProtocolVersion,
    string? TargetId,
    string? Url,
    string? Title,
    bool TargetCreated,
    bool TargetClosed,
    bool SessionCreated,
    bool SessionClosed,
    bool NavigationOk,
    bool TitleRead,
    bool BootstrapInjected,
    bool DoubleInjectionPrevented,
    bool ScreenshotCaptured,
    bool ProcessStarted,
    IReadOnlyList<string> LaunchArgsRedacted,
    bool LaunchTimeout,
    string? CdpEndpointHost,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool ForcedKillUsed,
    bool OrphanProcessDetected,
    bool SystemBrowserUsed,
    bool ExtensionUsed,
    bool CommandsExecuted,
    bool CdpCommandsExecuted,
    bool FilesModified,
    string? EvidencePath,
    string? ScreenshotPath,
    int? ProcessId,
    bool DomSnapshotCaptured = false,
    int InteractiveElementCount = 0,
    int FormsCount = 0,
    int LinksCount = 0,
    int ButtonsCount = 0,
    int InputsCount = 0,
    bool ControlledClickOk = false,
    bool ControlledTypeOk = false,
    bool ExternalNavigationAttempted = false,
    bool ExternalNavigationBlocked = false,
    bool SecretsRedacted = true,
    bool RawHtmlStored = false,
    bool InputValuesStored = false,
    CdpDomSnapshotEvidence? DomSnapshotEvidence = null,
    IReadOnlyList<CdpControlledActionEvidence>? ControlledActionEvidence = null,
    string? DomActionEvidencePath = null);

public sealed class CloakBrowserCdpHealthcheckRunner
{
    private const string SuccessDecision = "NODAL_OS_CLOAKBROWSER_CDP_DOM_SNAPSHOT_ACTION_CONTROLLER_READY";
    private const string BlockedDecision = "NODAL_OS_CLOAKBROWSER_CDP_LIVE_BLOCKED_WITH_CAUSE";
    private const string PageTitle = "NODAL OS CDP Controlled Action Test";

    public async Task<CloakBrowserCdpHealthcheckResult> RunAsync(
        CloakBrowserCdpHealthcheckOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.LockfilePath);

        var timeout = options.EffectiveTimeout;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        BrowserRuntimeLock runtimeLock;
        BrowserRuntimeLocalConfig localConfig;
        try
        {
            runtimeLock = BrowserRuntimeLock.Load(options.LockfilePath);
            localConfig = BrowserRuntimeLocalConfig.Discover(
                options.RepositoryRoot,
                Environment.GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .ToDictionary(entry => (string)entry.Key, entry => entry.Value?.ToString()));
        }
        catch (Exception ex)
        {
            return await WriteBlockedEvidenceAsync(options.RepositoryRoot, null, null, "Runtime config load failed: " + ex.Message, cancellationToken)
                .ConfigureAwait(false);
        }

        var runtimeArtifactPath = options.RuntimeArtifactPath;
        if (string.IsNullOrWhiteSpace(runtimeArtifactPath) && localConfig.HasExecutablePath)
        {
            runtimeArtifactPath = localConfig.CloakBrowserExecutablePath;
        }

        var status = new CloakBrowserRuntimeProvider().GetStatus(runtimeLock, runtimeArtifactPath);
        if (!status.LaunchAllowed)
        {
            return await WriteBlockedEvidenceAsync(options.RepositoryRoot, runtimeLock, runtimeArtifactPath, status.Reason, cancellationToken)
                .ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(runtimeArtifactPath) || !File.Exists(runtimeArtifactPath))
        {
            return await WriteBlockedEvidenceAsync(options.RepositoryRoot, runtimeLock, runtimeArtifactPath, BrowserRuntimeDefaults.RuntimeArtifactRequiredReason, cancellationToken)
                .ConfigureAwait(false);
        }

        var actualHash = await ComputeSha256Async(runtimeArtifactPath, timeoutSource.Token).ConfigureAwait(false);
        if (!actualHash.Equals(runtimeLock.BinarySha256, StringComparison.OrdinalIgnoreCase))
        {
            return await WriteBlockedEvidenceAsync(
                    options.RepositoryRoot,
                    runtimeLock,
                    runtimeArtifactPath,
                    "Pinned CloakBrowser artifact hash mismatch.",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var guard = BrowserRuntimeGuard.ValidateLaunch(
            new BrowserRuntimeLaunchRequest(ExecutablePath: runtimeArtifactPath),
            runtimeLock);
        if (!guard.IsAllowed)
        {
            return await WriteBlockedEvidenceAsync(options.RepositoryRoot, runtimeLock, runtimeArtifactPath, guard.Reason, cancellationToken)
                .ConfigureAwait(false);
        }

        var artifactsRoot = Path.Combine(options.RepositoryRoot, "artifacts", "local-verification");
        Directory.CreateDirectory(artifactsRoot);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ");
        var profileDirectory = Path.Combine(artifactsRoot, "cloakbrowser-profile-" + timestamp);
        Directory.CreateDirectory(profileDirectory);
        var screenshotPath = Path.Combine(artifactsRoot, "cloakbrowser-cdp-healthcheck-" + timestamp + ".png");

        var port = ReserveTcpPort();
        var launchArgs = BuildLaunchArguments(port, profileDirectory);
        ValidateSafeLaunchArguments(launchArgs);
        Process? process = null;
        string? browserVersion = null;
        string? protocolVersion = null;
        string? webSocketDebuggerUrl = null;
        string? cdpEndpointHost = null;
        string? targetId = null;
        string? sessionId = null;
        string? url = null;
        string? title = null;
        var processStarted = false;
        var targetCreated = false;
        var targetClosed = false;
        var sessionCreated = false;
        var sessionClosed = false;
        var navigationOk = false;
        var titleRead = false;
        var bootstrapInjected = false;
        var doubleInjectionPrevented = false;
        var domSnapshotCaptured = false;
        var interactiveElementCount = 0;
        var formsCount = 0;
        var linksCount = 0;
        var buttonsCount = 0;
        var inputsCount = 0;
        var controlledClickOk = false;
        var controlledTypeOk = false;
        var externalNavigationAttempted = false;
        var externalNavigationBlocked = false;
        CdpDomSnapshot? domSnapshot = null;
        CdpDomSnapshotEvidence? domSnapshotEvidence = null;
        var controlledActionEvidence = new List<CdpControlledActionEvidence>();
        var screenshotCaptured = false;
        var runtimeShutdown = false;
        var processExited = false;
        var forcedKillUsed = false;
        var launchTimeout = false;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var sessionRegistry = new CdpSessionRegistry();
        var targetManager = new CdpTargetManager();

        try
        {
            process = StartCloakBrowser(runtimeArtifactPath, launchArgs, stdout, stderr);
            processStarted = true;
            var versionInfo = await WaitForCdpVersionAsync(port, timeoutSource.Token).ConfigureAwait(false);
            browserVersion = versionInfo.BrowserVersion;
            protocolVersion = versionInfo.ProtocolVersion;
            webSocketDebuggerUrl = versionInfo.WebSocketDebuggerUrl;
            cdpEndpointHost = new Uri(webSocketDebuggerUrl).Host;

            await using var liveCdp = await CdpWebSocketClient.ConnectAsync(webSocketDebuggerUrl, timeoutSource.Token).ConfigureAwait(false);

            var browserVersionResult = await liveCdp.SendAsync("Browser.getVersion", null, null, timeoutSource.Token).ConfigureAwait(false);
            if (browserVersionResult.TryGetProperty("product", out var product))
            {
                browserVersion = product.GetString() ?? browserVersion;
            }

            var dataUrl = BuildHealthcheckDataUrl();
            new CdpActionController().CreateHealthcheckNavigation(new Uri(dataUrl));
            var createTarget = await liveCdp.SendAsync(
                    "Target.createTarget",
                    new Dictionary<string, object?> { ["url"] = "about:blank" },
                    null,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            targetId = createTarget.GetProperty("targetId").GetString();
            targetCreated = !string.IsNullOrWhiteSpace(targetId);
            if (targetCreated && targetId is not null)
            {
                targetManager.TrackPageTarget(targetId, "about:blank", string.Empty);
            }

            var attachTarget = await liveCdp.SendAsync(
                    "Target.attachToTarget",
                    new Dictionary<string, object?> { ["targetId"] = targetId, ["flatten"] = true },
                    null,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            sessionId = attachTarget.GetProperty("sessionId").GetString();
            sessionCreated = !string.IsNullOrWhiteSpace(sessionId);
            if (sessionCreated && targetId is not null && sessionId is not null)
            {
                sessionRegistry.Register(targetId, sessionId, DateTimeOffset.UtcNow);
                sessionRegistry.MarkState(sessionId, "attached");
            }

            await liveCdp.SendAsync("Page.enable", null, sessionId, timeoutSource.Token).ConfigureAwait(false);
            await liveCdp.SendAsync("Runtime.enable", null, sessionId, timeoutSource.Token).ConfigureAwait(false);
            await liveCdp.SendAsync(
                    "Page.navigate",
                    new Dictionary<string, object?> { ["url"] = dataUrl },
                    sessionId,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            await WaitForDocumentReadyAsync(liveCdp, sessionId, timeoutSource.Token).ConfigureAwait(false);

            url = await EvaluateStringAsync(liveCdp, sessionId, "String(window.location.href)", timeoutSource.Token).ConfigureAwait(false);
            title = await EvaluateStringAsync(liveCdp, sessionId, "String(document.title)", timeoutSource.Token).ConfigureAwait(false);
            navigationOk = url?.StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase) == true;
            titleRead = string.Equals(title, PageTitle, StringComparison.Ordinal);
            if (sessionId is not null)
            {
                sessionRegistry.MarkState(sessionId, "navigated");
            }

            if (targetId is not null && url is not null && title is not null)
            {
                targetManager.MarkNavigated(targetId, url, title);
            }

            var injectionManager = new CdpInjectionManager();
            injectionManager.EnsureReadySession(sessionId);
            var firstInjection = await liveCdp.SendAsync(
                    "Runtime.evaluate",
                    new Dictionary<string, object?>
                    {
                        ["expression"] = injectionManager.BuildBootstrapScript(),
                        ["returnByValue"] = true,
                        ["awaitPromise"] = true
                    },
                    sessionId,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            bootstrapInjected = ReadRuntimeObjectBoolean(firstInjection, "injected");
            if (bootstrapInjected && sessionId is not null)
            {
                sessionRegistry.MarkState(sessionId, "injected");
            }

            var secondInjection = await liveCdp.SendAsync(
                    "Runtime.evaluate",
                    new Dictionary<string, object?>
                    {
                        ["expression"] = injectionManager.BuildBootstrapScript(),
                        ["returnByValue"] = true,
                        ["awaitPromise"] = true
                    },
                    sessionId,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            doubleInjectionPrevented = ReadRuntimeObjectBoolean(secondInjection, "alreadyInjected");

            var domSnapshotManager = new CdpDomSnapshotManager();
            var domSnapshotResult = await liveCdp.SendAsync(
                    "Runtime.evaluate",
                    new Dictionary<string, object?>
                    {
                        ["expression"] = domSnapshotManager.BuildDomSnapshotExpression(),
                        ["returnByValue"] = true,
                        ["awaitPromise"] = true
                    },
                    sessionId,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            domSnapshot = domSnapshotManager.ParseRuntimeEvaluateResult(domSnapshotResult);
            domSnapshotEvidence = domSnapshotManager.CreateEvidence(domSnapshot);
            domSnapshotCaptured = domSnapshot.NodeCount > 0 && domSnapshot.InteractiveElements.Count > 0;
            interactiveElementCount = domSnapshot.InteractiveElements.Count;
            formsCount = domSnapshot.FormsCount;
            linksCount = domSnapshot.LinksCount;
            buttonsCount = domSnapshot.ButtonsCount;
            inputsCount = domSnapshot.InputsCount;

            var actionController = new CdpActionController();
            var buttonStableId = FindStableId(domSnapshot, "button", "button");
            if (!string.IsNullOrWhiteSpace(buttonStableId))
            {
                var clickRequest = actionController.CreateClickElementByStableId(buttonStableId);
                var clickResult = await liveCdp.SendAsync(
                        "Runtime.evaluate",
                        new Dictionary<string, object?>
                        {
                            ["expression"] = actionController.BuildControlledActionExpression(clickRequest),
                            ["returnByValue"] = true,
                            ["awaitPromise"] = true
                        },
                        sessionId,
                        timeoutSource.Token)
                    .ConfigureAwait(false);
                var parsedClick = actionController.ParseRuntimeEvaluateActionResult(clickRequest.ActionKind, buttonStableId, clickResult);
                controlledClickOk = parsedClick.Status.Equals("completed", StringComparison.OrdinalIgnoreCase);
                controlledActionEvidence.Add(parsedClick.Evidence);
            }

            var inputStableId = FindStableId(domSnapshot, "input", "input");
            if (!string.IsNullOrWhiteSpace(inputStableId))
            {
                var typeRequest = actionController.CreateTypeTextByStableId(inputStableId, "NODAL OS controlled text");
                var typeResult = await liveCdp.SendAsync(
                        "Runtime.evaluate",
                        new Dictionary<string, object?>
                        {
                            ["expression"] = actionController.BuildControlledActionExpression(typeRequest),
                            ["returnByValue"] = true,
                            ["awaitPromise"] = true
                        },
                        sessionId,
                        timeoutSource.Token)
                    .ConfigureAwait(false);
                var parsedType = actionController.ParseRuntimeEvaluateActionResult(typeRequest.ActionKind, inputStableId, typeResult);
                controlledTypeOk = parsedType.Status.Equals("completed", StringComparison.OrdinalIgnoreCase);
                controlledActionEvidence.Add(parsedType.Evidence);
            }

            externalNavigationAttempted = true;
            try
            {
                actionController.CreateExternalNavigation(new Uri("https://example.invalid/blocked"));
            }
            catch (InvalidOperationException)
            {
                externalNavigationBlocked = true;
                controlledActionEvidence.Add(actionController.CreateEvidence(
                    CdpControlledActionKind.NavigateExternalUrl,
                    "https://example.invalid/blocked",
                    "blocked",
                    "Controlled CDP actions do not allow external navigation in V1."));
            }

            var screenshot = await liveCdp.SendAsync(
                    "Page.captureScreenshot",
                    new Dictionary<string, object?> { ["format"] = "png", ["fromSurface"] = true },
                    sessionId,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            var screenshotBase64 = screenshot.GetProperty("data").GetString();
            if (!string.IsNullOrWhiteSpace(screenshotBase64))
            {
                await File.WriteAllBytesAsync(screenshotPath, Convert.FromBase64String(screenshotBase64), timeoutSource.Token)
                    .ConfigureAwait(false);
                screenshotCaptured = true;
            }

            if (!string.IsNullOrWhiteSpace(targetId))
            {
                await liveCdp.SendAsync(
                        "Target.closeTarget",
                        new Dictionary<string, object?> { ["targetId"] = targetId },
                        null,
                        timeoutSource.Token)
                    .ConfigureAwait(false);
                targetClosed = true;
                targetManager.Close(targetId, DateTimeOffset.UtcNow);
                if (sessionId is not null)
                {
                    sessionRegistry.MarkClosed(sessionId, DateTimeOffset.UtcNow);
                    sessionClosed = true;
                }
            }

            try
            {
                await liveCdp.SendAsync("Browser.close", null, null, timeoutSource.Token).ConfigureAwait(false);
                runtimeShutdown = true;
            }
            catch
            {
                // Browser.close can race with process exit; shutdown is checked below.
            }

            processExited = await WaitForExitAsync(process, TimeSpan.FromSeconds(8), CancellationToken.None).ConfigureAwait(false);
            if (!processExited && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                forcedKillUsed = true;
                processExited = await WaitForExitAsync(process, TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
            }

            runtimeShutdown = runtimeShutdown || processExited;

            var success = targetCreated
                && navigationOk
                && titleRead
                && bootstrapInjected
                && doubleInjectionPrevented
                && domSnapshotCaptured
                && controlledClickOk
                && controlledTypeOk
                && externalNavigationBlocked
                && screenshotCaptured
                && targetClosed
                && sessionCreated
                && sessionClosed
                && processExited;

            var result = new CloakBrowserCdpHealthcheckResult(
                Status: success ? "PASS" : "BLOCKED",
                Decision: success ? SuccessDecision : BlockedDecision,
                Reason: success ? "CloakBrowser CDP live healthcheck completed." : "CloakBrowser CDP live healthcheck did not complete all checks.",
                RuntimeProvider: "cloakbrowser",
                CdpMode: "cdp-direct",
                RuntimeVersion: runtimeLock.RuntimeVersion,
                BinarySha256: runtimeLock.BinarySha256,
                BrowserVersion: browserVersion,
                ProtocolVersion: protocolVersion,
                TargetId: targetId,
                Url: url,
                Title: title,
                TargetCreated: targetCreated,
                TargetClosed: targetClosed,
                SessionCreated: sessionCreated,
                SessionClosed: sessionClosed,
                NavigationOk: navigationOk,
                TitleRead: titleRead,
                BootstrapInjected: bootstrapInjected,
                DoubleInjectionPrevented: doubleInjectionPrevented,
                ScreenshotCaptured: screenshotCaptured,
                ProcessStarted: processStarted,
                LaunchArgsRedacted: RedactLaunchArguments(launchArgs),
                LaunchTimeout: launchTimeout,
                CdpEndpointHost: cdpEndpointHost,
                RuntimeShutdown: runtimeShutdown,
                ProcessExited: processExited,
                ForcedKillUsed: forcedKillUsed,
                OrphanProcessDetected: !processExited,
                SystemBrowserUsed: false,
                ExtensionUsed: false,
                CommandsExecuted: false,
                CdpCommandsExecuted: true,
                FilesModified: false,
                EvidencePath: null,
                ScreenshotPath: screenshotCaptured ? screenshotPath : null,
                ProcessId: process.Id,
                DomSnapshotCaptured: domSnapshotCaptured,
                InteractiveElementCount: interactiveElementCount,
                FormsCount: formsCount,
                LinksCount: linksCount,
                ButtonsCount: buttonsCount,
                InputsCount: inputsCount,
                ControlledClickOk: controlledClickOk,
                ControlledTypeOk: controlledTypeOk,
                ExternalNavigationAttempted: externalNavigationAttempted,
                ExternalNavigationBlocked: externalNavigationBlocked,
                SecretsRedacted: domSnapshot?.SecretsRedacted ?? true,
                RawHtmlStored: domSnapshot?.StoresRawHtml ?? false,
                InputValuesStored: domSnapshot?.StoresInputValues ?? false,
                DomSnapshotEvidence: domSnapshotEvidence,
                ControlledActionEvidence: controlledActionEvidence);

            return await WriteEvidenceAsync(options.RepositoryRoot, result, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            launchTimeout = true;
            if (process is { HasExited: false })
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    forcedKillUsed = true;
                    await WaitForExitAsync(process, TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Best effort cleanup. The evidence records the blocked launch.
                }
            }

            return await WriteBlockedEvidenceAsync(
                    options.RepositoryRoot,
                    runtimeLock,
                    runtimeArtifactPath,
                    "CloakBrowser CDP live healthcheck timed out before lifecycle completion.",
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (process is { HasExited: false })
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    forcedKillUsed = true;
                    await WaitForExitAsync(process, TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Best effort cleanup. The evidence records the blocked launch.
                }
            }

            return await WriteBlockedEvidenceAsync(
                    options.RepositoryRoot,
                    runtimeLock,
                    runtimeArtifactPath,
                    "CloakBrowser CDP live healthcheck failed: " + RedactProcessText(ex.Message + " " + stderr),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            process?.Dispose();
            TryDeleteDirectory(profileDirectory);
        }
    }

    private static Process StartCloakBrowser(
        string runtimeArtifactPath,
        IReadOnlyList<string> launchArguments,
        StringBuilder stdout,
        StringBuilder stderr)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = runtimeArtifactPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in launchArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stdout.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stderr.AppendLine(args.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("CloakBrowser process could not be started.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static IReadOnlyList<string> BuildLaunchArguments(int port, string profileDirectory) =>
    [
        "--headless=new",
        "--remote-debugging-address=127.0.0.1",
        "--remote-debugging-port=" + port.ToString(System.Globalization.CultureInfo.InvariantCulture),
        "--user-data-dir=" + profileDirectory,
        "--no-first-run",
        "--disable-default-apps",
        "about:blank"
    ];

    private static void ValidateSafeLaunchArguments(IReadOnlyList<string> launchArguments)
    {
        foreach (var argument in launchArguments)
        {
            if (argument.Contains("channel=chrome", StringComparison.OrdinalIgnoreCase)
                || argument.Contains("channel=msedge", StringComparison.OrdinalIgnoreCase)
                || argument.Contains("msedge", StringComparison.OrdinalIgnoreCase)
                || argument.Contains("playwright", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unsafe CloakBrowser launch argument rejected.");
            }
        }
    }

    private static IReadOnlyList<string> RedactLaunchArguments(IReadOnlyList<string> launchArguments) =>
        launchArguments
            .Select(argument => argument.StartsWith("--user-data-dir=", StringComparison.OrdinalIgnoreCase)
                ? "--user-data-dir=<local-verification-profile>"
                : argument)
            .ToArray();

    private static async Task<CdpVersionInfo> WaitForCdpVersionAsync(int port, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var endpoint = new Uri($"http://127.0.0.1:{port.ToString(System.Globalization.CultureInfo.InvariantCulture)}/json/version");
        Exception? lastError = null;

        for (var attempt = 0; attempt < 80; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var response = await http.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var root = json.RootElement;
                    var webSocketDebuggerUrl = root.GetProperty("webSocketDebuggerUrl").GetString()
                        ?? throw new InvalidOperationException("CDP version endpoint did not include webSocketDebuggerUrl.");
                    var webSocketUri = CdpEndpointDiscovery.ValidateWebSocketDebuggerUrl(webSocketDebuggerUrl);

                    return new CdpVersionInfo(
                        BrowserVersion: root.TryGetProperty("Browser", out var browser) ? browser.GetString() : null,
                        ProtocolVersion: root.TryGetProperty("Protocol-Version", out var protocol) ? protocol.GetString() : null,
                        WebSocketDebuggerUrl: webSocketUri.ToString());
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
            {
                lastError = ex;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(CdpEndpointDiscovery.CreateTimeoutMessage(endpoint, lastError));
    }

    private static async Task WaitForDocumentReadyAsync(CdpWebSocketClient cdp, string? sessionId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var readyState = await EvaluateStringAsync(cdp, sessionId, "String(document.readyState)", cancellationToken).ConfigureAwait(false);
            if (readyState is "interactive" or "complete")
            {
                return;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<string?> EvaluateStringAsync(
        CdpWebSocketClient cdp,
        string? sessionId,
        string expression,
        CancellationToken cancellationToken)
    {
        var result = await cdp.SendAsync(
                "Runtime.evaluate",
                new Dictionary<string, object?>
                {
                    ["expression"] = expression,
                    ["returnByValue"] = true,
                    ["awaitPromise"] = true
                },
                sessionId,
                cancellationToken)
            .ConfigureAwait(false);

        return result.GetProperty("result").TryGetProperty("value", out var value)
            ? value.GetString()
            : null;
    }

    private static bool ReadRuntimeObjectBoolean(JsonElement runtimeEvaluateResult, string propertyName)
    {
        if (!runtimeEvaluateResult.TryGetProperty("result", out var result)
            || !result.TryGetProperty("value", out var value)
            || value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty(propertyName, out var property)
            || property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            return false;
        }

        return property.GetBoolean();
    }

    private static string BuildHealthcheckDataUrl()
    {
        const string html = """
<!doctype html>
<html>
  <head><meta charset="utf-8"><title>NODAL OS CDP Controlled Action Test</title></head>
  <body>
    <main>
      <h1>NODAL OS CDP Controlled Action Test</h1>
      <p>Controlled local data URL.</p>
      <button id="controlled-button" type="button" onclick="document.body.setAttribute('data-clicked', 'true')">Record controlled click</button>
      <label for="controlled-input">Controlled input</label>
      <input id="controlled-input" name="controlledInput" type="text" placeholder="safe text" />
      <input id="redacted-password" name="password" type="password" value="never-store-this" />
      <a id="external-link" href="https://example.invalid/blocked?token=do-not-store">External link blocked by controller</a>
      <form id="blocked-form" action="https://example.invalid/post" onsubmit="document.body.setAttribute('data-submit-attempted', 'true'); return false;">
        <button type="submit">Submit should stay blocked</button>
      </form>
    </main>
  </body>
</html>
""";

        return "data:text/html;charset=utf-8," + Uri.EscapeDataString(html);
    }

    private static string? FindStableId(CdpDomSnapshot snapshot, string tag, string role) =>
        snapshot.InteractiveElements
            .FirstOrDefault(element =>
                element.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)
                && element.Role.Equals(role, StringComparison.OrdinalIgnoreCase)
                && element.Visible
                && element.Enabled)
            ?.StableId;

    private static int ReserveTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<CloakBrowserCdpHealthcheckResult> WriteBlockedEvidenceAsync(
        string repositoryRoot,
        BrowserRuntimeLock? runtimeLock,
        string? runtimeArtifactPath,
        string reason,
        CancellationToken cancellationToken)
    {
        var result = new CloakBrowserCdpHealthcheckResult(
            Status: "BLOCKED",
            Decision: BlockedDecision,
            Reason: reason,
            RuntimeProvider: "cloakbrowser",
            CdpMode: "cdp-direct",
            RuntimeVersion: runtimeLock?.RuntimeVersion ?? "unknown",
            BinarySha256: runtimeLock?.BinarySha256 ?? "unknown",
            BrowserVersion: null,
            ProtocolVersion: null,
            TargetId: null,
            Url: null,
            Title: null,
            TargetCreated: false,
            TargetClosed: false,
            SessionCreated: false,
            SessionClosed: false,
            NavigationOk: false,
            TitleRead: false,
            BootstrapInjected: false,
            DoubleInjectionPrevented: false,
            ScreenshotCaptured: false,
            ProcessStarted: false,
            LaunchArgsRedacted: [],
            LaunchTimeout: false,
            CdpEndpointHost: null,
            RuntimeShutdown: false,
            ProcessExited: false,
            ForcedKillUsed: false,
            OrphanProcessDetected: false,
            SystemBrowserUsed: false,
            ExtensionUsed: false,
            CommandsExecuted: false,
            CdpCommandsExecuted: false,
            FilesModified: false,
            EvidencePath: null,
            ScreenshotPath: null,
            ProcessId: null);

        return await WriteEvidenceAsync(repositoryRoot, result, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<CloakBrowserCdpHealthcheckResult> WriteEvidenceAsync(
        string repositoryRoot,
        CloakBrowserCdpHealthcheckResult result,
        CancellationToken cancellationToken)
    {
        var artifactsRoot = Path.Combine(repositoryRoot, "artifacts", "local-verification");
        Directory.CreateDirectory(artifactsRoot);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ");
        var evidencePath = Path.Combine(artifactsRoot, "cloakbrowser-cdp-healthcheck-" + timestamp + ".redacted.json");

        var evidence = new
        {
            status = result.Status,
            decision = result.Decision,
            reason = result.Reason,
            runtimeProvider = result.RuntimeProvider,
            cdpMode = result.CdpMode,
            runtimeVersion = result.RuntimeVersion,
            binarySha256 = result.BinarySha256,
            browserVersion = result.BrowserVersion,
            protocolVersion = result.ProtocolVersion,
            targetId = result.TargetId,
            url = result.Url,
            title = result.Title,
            processStarted = result.ProcessStarted,
            processId = result.ProcessId,
            launchArgsRedacted = result.LaunchArgsRedacted,
            launchTimeout = result.LaunchTimeout,
            cdpEndpointHost = result.CdpEndpointHost,
            targetCreated = result.TargetCreated,
            targetClosed = result.TargetClosed,
            sessionCreated = result.SessionCreated,
            sessionClosed = result.SessionClosed,
            navigationOk = result.NavigationOk,
            titleRead = result.TitleRead,
            bootstrapInjected = result.BootstrapInjected,
            doubleInjectionPrevented = result.DoubleInjectionPrevented,
            screenshotCaptured = result.ScreenshotCaptured,
            screenshotArtifact = result.ScreenshotPath is null ? null : Path.GetFileName(result.ScreenshotPath),
            runtimeShutdown = result.RuntimeShutdown,
            processExited = result.ProcessExited,
            forcedKillUsed = result.ForcedKillUsed,
            orphanProcessDetected = result.OrphanProcessDetected,
            systemBrowserUsed = result.SystemBrowserUsed,
            extensionUsed = result.ExtensionUsed,
            commandsExecuted = result.CommandsExecuted,
            cdpCommandsExecuted = result.CdpCommandsExecuted,
            filesModified = result.FilesModified,
            domSnapshotCaptured = result.DomSnapshotCaptured,
            interactiveElementCount = result.InteractiveElementCount,
            formsCount = result.FormsCount,
            linksCount = result.LinksCount,
            buttonsCount = result.ButtonsCount,
            inputsCount = result.InputsCount,
            controlledClickOk = result.ControlledClickOk,
            controlledTypeOk = result.ControlledTypeOk,
            externalNavigationAttempted = result.ExternalNavigationAttempted,
            externalNavigationBlocked = result.ExternalNavigationBlocked,
            secretsRedacted = result.SecretsRedacted,
            rawHtmlStored = result.RawHtmlStored,
            inputValuesStored = result.InputValuesStored,
            timestamp = DateTimeOffset.UtcNow
        };

        await File.WriteAllTextAsync(
                evidencePath,
                JsonSerializer.Serialize(evidence, JsonOptions),
                cancellationToken)
            .ConfigureAwait(false);

        string? domActionEvidencePath = null;
        if (result.DomSnapshotCaptured
            || result.ControlledClickOk
            || result.ControlledTypeOk
            || result.ExternalNavigationBlocked)
        {
            domActionEvidencePath = Path.Combine(artifactsRoot, "cloakbrowser-cdp-dom-action-" + timestamp + ".redacted.json");
            var domActionEvidence = new
            {
                status = result.Status,
                decision = result.Decision,
                runtimeProvider = result.RuntimeProvider,
                cdpMode = result.CdpMode,
                source = "cloakbrowser-cdp-direct",
                runtimeVersion = result.RuntimeVersion,
                binarySha256 = result.BinarySha256,
                browserVersion = result.BrowserVersion,
                protocolVersion = result.ProtocolVersion,
                targetId = result.TargetId,
                url = result.Url,
                title = result.Title,
                pageMetadata = result.DomSnapshotEvidence?.PageMetadata,
                domSnapshotSummary = result.DomSnapshotEvidence is null
                    ? null
                    : new
                    {
                        nodeCount = result.DomSnapshotEvidence.NodeCount,
                        interactiveElementCount = result.DomSnapshotEvidence.InteractiveElementCount,
                        formsCount = result.DomSnapshotEvidence.FormsCount,
                        linksCount = result.DomSnapshotEvidence.LinksCount,
                        buttonsCount = result.DomSnapshotEvidence.ButtonsCount,
                        inputsCount = result.DomSnapshotEvidence.InputsCount,
                        screenshotsAvailable = result.DomSnapshotEvidence.ScreenshotsAvailable,
                        storesRawHtml = result.DomSnapshotEvidence.StoresRawHtml,
                        storesInputValues = result.DomSnapshotEvidence.StoresInputValues,
                        secretsRedacted = result.DomSnapshotEvidence.SecretsRedacted
                    },
                actionResults = result.ControlledActionEvidence,
                screenshotCaptured = result.ScreenshotCaptured,
                extensionUsed = result.ExtensionUsed,
                systemBrowserUsed = result.SystemBrowserUsed,
                externalNavigationAttempted = result.ExternalNavigationAttempted,
                externalNavigationBlocked = result.ExternalNavigationBlocked,
                productFilesModified = result.FilesModified,
                secretsRedacted = result.SecretsRedacted,
                cdpCommandsExecuted = result.CdpCommandsExecuted,
                productCommandsExecuted = result.CommandsExecuted,
                timestamp = DateTimeOffset.UtcNow
            };

            await File.WriteAllTextAsync(
                    domActionEvidencePath,
                    JsonSerializer.Serialize(domActionEvidence, JsonOptions),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return result with { EvidencePath = evidencePath, DomActionEvidencePath = domActionEvidencePath };
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return process.HasExited;
        }
    }

    private static string RedactProcessText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text
            .Replace(Environment.UserName, "<user>", StringComparison.OrdinalIgnoreCase)
            .Replace(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "<profile>", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Local verification artifacts are best-effort cleanup only.
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record CdpVersionInfo(string? BrowserVersion, string? ProtocolVersion, string WebSocketDebuggerUrl);
}

internal sealed class CdpWebSocketClient : IAsyncDisposable
{
    private readonly ClientWebSocket socket;
    private readonly CdpConnectionLifecycle lifecycle = new();

    private CdpWebSocketClient(ClientWebSocket socket)
    {
        this.socket = socket;
    }

    public static async Task<CdpWebSocketClient> ConnectAsync(string webSocketUrl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webSocketUrl);

        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(webSocketUrl), cancellationToken).ConfigureAwait(false);
        return new CdpWebSocketClient(socket);
    }

    public async Task<JsonElement> SendAsync(
        string method,
        IReadOnlyDictionary<string, object?>? parameters,
        string? sessionId,
        CancellationToken cancellationToken)
    {
        lifecycle.ThrowIfDisposed();
        var id = lifecycle.NextCommandId();
        var message = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["method"] = method
        };

        if (parameters is not null)
        {
            message["params"] = parameters;
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            message["sessionId"] = sessionId;
        }

        var json = JsonSerializer.Serialize(message);
        try
        {
            await socket.SendAsync(
                    Encoding.UTF8.GetBytes(json),
                    WebSocketMessageType.Text,
                    WebSocketMessageFlags.EndOfMessage,
                    cancellationToken)
                .ConfigureAwait(false);

            while (true)
            {
                var responseJson = await ReceiveTextMessageAsync(cancellationToken).ConfigureAwait(false);
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;

                if (!root.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id)
                {
                    continue;
                }

                if (root.TryGetProperty("error", out var error))
                {
                    throw new InvalidOperationException("CDP command failed for " + method + ": " + error.ToString());
                }

                return root.GetProperty("result").Clone();
            }
        }
        catch (OperationCanceledException ex)
        {
            var timeout = lifecycle.CreateCommandTimeout(method, TimeSpan.FromSeconds(30));
            throw new TimeoutException(timeout.Message, ex);
        }
    }

    private async Task<string> ReceiveTextMessageAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        await using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("CDP WebSocket closed.");
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (socket.State is WebSocketState.Closed or WebSocketState.Aborted or WebSocketState.None)
        {
            lifecycle.Dispose();
            socket.Dispose();
            return;
        }

        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", timeout.Token).ConfigureAwait(false);
            }
            catch
            {
                // Ignore dispose races after Browser.close.
            }
        }

        lifecycle.Dispose();
        socket.Dispose();
    }
}
