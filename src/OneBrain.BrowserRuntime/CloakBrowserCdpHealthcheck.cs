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
    bool NavigationOk,
    bool TitleRead,
    bool BootstrapInjected,
    bool DoubleInjectionPrevented,
    bool ScreenshotCaptured,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool OrphanProcessDetected,
    bool SystemBrowserUsed,
    bool ExtensionUsed,
    bool CommandsExecuted,
    bool CdpCommandsExecuted,
    bool FilesModified,
    string? EvidencePath,
    string? ScreenshotPath,
    int? ProcessId);

public sealed class CloakBrowserCdpHealthcheckRunner
{
    private const string SuccessDecision = "NODAL_OS_CLOAKBROWSER_CDP_LIVE_HEALTHCHECK_READY";
    private const string BlockedDecision = "NODAL_OS_CLOAKBROWSER_CDP_LIVE_BLOCKED_WITH_CAUSE";
    private const string PageTitle = "NODAL OS CloakBrowser CDP Healthcheck";

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
        Process? process = null;
        string? browserVersion = null;
        string? protocolVersion = null;
        string? webSocketDebuggerUrl = null;
        string? targetId = null;
        string? sessionId = null;
        string? url = null;
        string? title = null;
        var targetCreated = false;
        var navigationOk = false;
        var titleRead = false;
        var bootstrapInjected = false;
        var doubleInjectionPrevented = false;
        var screenshotCaptured = false;
        var runtimeShutdown = false;
        var processExited = false;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            process = StartCloakBrowser(runtimeArtifactPath, port, profileDirectory, stdout, stderr);
            var versionInfo = await WaitForCdpVersionAsync(port, timeoutSource.Token).ConfigureAwait(false);
            browserVersion = versionInfo.BrowserVersion;
            protocolVersion = versionInfo.ProtocolVersion;
            webSocketDebuggerUrl = versionInfo.WebSocketDebuggerUrl;

            await using var liveCdp = await CdpWebSocketClient.ConnectAsync(webSocketDebuggerUrl, timeoutSource.Token).ConfigureAwait(false);

            var browserVersionResult = await liveCdp.SendAsync("Browser.getVersion", null, null, timeoutSource.Token).ConfigureAwait(false);
            if (browserVersionResult.TryGetProperty("product", out var product))
            {
                browserVersion = product.GetString() ?? browserVersion;
            }

            var dataUrl = BuildHealthcheckDataUrl();
            var createTarget = await liveCdp.SendAsync(
                    "Target.createTarget",
                    new Dictionary<string, object?> { ["url"] = "about:blank" },
                    null,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            targetId = createTarget.GetProperty("targetId").GetString();
            targetCreated = !string.IsNullOrWhiteSpace(targetId);

            var attachTarget = await liveCdp.SendAsync(
                    "Target.attachToTarget",
                    new Dictionary<string, object?> { ["targetId"] = targetId, ["flatten"] = true },
                    null,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            sessionId = attachTarget.GetProperty("sessionId").GetString();

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

            var injectionManager = new CdpInjectionManager();
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
                processExited = await WaitForExitAsync(process, TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
            }

            runtimeShutdown = runtimeShutdown || processExited;

            var success = targetCreated
                && navigationOk
                && titleRead
                && bootstrapInjected
                && doubleInjectionPrevented
                && screenshotCaptured
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
                NavigationOk: navigationOk,
                TitleRead: titleRead,
                BootstrapInjected: bootstrapInjected,
                DoubleInjectionPrevented: doubleInjectionPrevented,
                ScreenshotCaptured: screenshotCaptured,
                RuntimeShutdown: runtimeShutdown,
                ProcessExited: processExited,
                OrphanProcessDetected: !processExited,
                SystemBrowserUsed: false,
                ExtensionUsed: false,
                CommandsExecuted: false,
                CdpCommandsExecuted: true,
                FilesModified: false,
                EvidencePath: null,
                ScreenshotPath: screenshotCaptured ? screenshotPath : null,
                ProcessId: process.Id);

            return await WriteEvidenceAsync(options.RepositoryRoot, result, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (process is { HasExited: false })
            {
                try
                {
                    process.Kill(entireProcessTree: true);
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
        int port,
        string profileDirectory,
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

        startInfo.ArgumentList.Add("--headless=new");
        startInfo.ArgumentList.Add("--remote-debugging-address=127.0.0.1");
        startInfo.ArgumentList.Add("--remote-debugging-port=" + port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--user-data-dir=" + profileDirectory);
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--disable-default-apps");
        startInfo.ArgumentList.Add("about:blank");

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
                    return new CdpVersionInfo(
                        BrowserVersion: root.TryGetProperty("Browser", out var browser) ? browser.GetString() : null,
                        ProtocolVersion: root.TryGetProperty("Protocol-Version", out var protocol) ? protocol.GetString() : null,
                        WebSocketDebuggerUrl: root.GetProperty("webSocketDebuggerUrl").GetString()
                            ?? throw new InvalidOperationException("CDP version endpoint did not include webSocketDebuggerUrl."));
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
            {
                lastError = ex;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("CDP /json/version did not become ready. Last error: " + lastError?.Message);
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
  <head><meta charset="utf-8"><title>NODAL OS CloakBrowser CDP Healthcheck</title></head>
  <body><main><h1>NODAL OS CloakBrowser CDP Healthcheck</h1><p>Controlled local data URL.</p></main></body>
</html>
""";

        return "data:text/html;charset=utf-8," + Uri.EscapeDataString(html);
    }

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
            NavigationOk: false,
            TitleRead: false,
            BootstrapInjected: false,
            DoubleInjectionPrevented: false,
            ScreenshotCaptured: false,
            RuntimeShutdown: false,
            ProcessExited: false,
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
            targetCreated = result.TargetCreated,
            navigationOk = result.NavigationOk,
            titleRead = result.TitleRead,
            bootstrapInjected = result.BootstrapInjected,
            doubleInjectionPrevented = result.DoubleInjectionPrevented,
            screenshotCaptured = result.ScreenshotCaptured,
            screenshotArtifact = result.ScreenshotPath is null ? null : Path.GetFileName(result.ScreenshotPath),
            runtimeShutdown = result.RuntimeShutdown,
            processExited = result.ProcessExited,
            orphanProcessDetected = result.OrphanProcessDetected,
            systemBrowserUsed = result.SystemBrowserUsed,
            extensionUsed = result.ExtensionUsed,
            commandsExecuted = result.CommandsExecuted,
            cdpCommandsExecuted = result.CdpCommandsExecuted,
            filesModified = result.FilesModified,
            processId = result.ProcessId,
            timestamp = DateTimeOffset.UtcNow
        };

        await File.WriteAllTextAsync(
                evidencePath,
                JsonSerializer.Serialize(evidence, JsonOptions),
                cancellationToken)
            .ConfigureAwait(false);

        return result with { EvidencePath = evidencePath };
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
    private int nextId;

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
        var id = Interlocked.Increment(ref nextId);
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
                throw new InvalidOperationException("CDP command failed: " + error.ToString());
            }

            return root.GetProperty("result").Clone();
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

        socket.Dispose();
    }
}
