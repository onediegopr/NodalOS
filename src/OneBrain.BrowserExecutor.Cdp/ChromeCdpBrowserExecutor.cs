using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed record ChromeCdpOptions(
    string BrowserExecutablePath,
    int? RemoteDebuggingPort = null,
    bool Headless = true,
    TimeSpan? StartupTimeout = null);

public sealed record ChromeCdpTargetInfo(
    string Id,
    string Type,
    Uri Url,
    string Title,
    string WebSocketDebuggerUrl);

public sealed record ChromeCdpActionResult(
    string ActionId,
    bool Executed,
    string Status,
    BrowserEvidence Evidence,
    string? Error = null);

public sealed class ChromeCdpBrowserLauncher
{
    private readonly BrowserProfileManager _profileManager;

    public ChromeCdpBrowserLauncher()
        : this(new BrowserProfileManager())
    {
    }

    public ChromeCdpBrowserLauncher(BrowserProfileManager profileManager)
    {
        _profileManager = profileManager;
    }

    private static readonly string[] BrowserCandidates =
    [
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    ];

    public static string? FindBrowserExecutable() =>
        BrowserCandidates.FirstOrDefault(File.Exists);

    public async Task<ChromeCdpBrowserSession> LaunchAsync(ChromeCdpOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.BrowserExecutablePath) || !File.Exists(options.BrowserExecutablePath))
            throw new FileNotFoundException("Chrome/Edge executable was not found.", options.BrowserExecutablePath);

        var port = options.RemoteDebuggingPort ?? GetFreeTcpPort();
        var profile = _profileManager.CreateProfile(new BrowserProfilePolicy(
            Kind: BrowserProfileKind.Disposable,
            Scope: BrowserStorageScope.Temporary,
            CleanupPolicy: BrowserProfileCleanupPolicy.DeleteOnClose,
            ConsentPolicy: BrowserProfileConsentPolicy.NotRequired,
            AllowRealUserProfile: false,
            ControlledRootDirectory: _profileManager.ControlledRoot));
        var sessionPolicy = new BrowserSessionPolicy(
            Owner: "browser-executor-cdp",
            CorrelationId: Guid.NewGuid().ToString("N"),
            ExpiresAfter: null,
            CleanupPolicy: profile.CleanupPolicy);
        var sessionManager = new BrowserSessionManager();
        var managedSession = sessionManager.MarkState(sessionManager.CreateSession(profile, sessionPolicy).SessionId, BrowserSessionState.Launching);

        var arguments = new List<string>
        {
            $"--remote-debugging-address=127.0.0.1",
            $"--remote-debugging-port={port}",
            $"--user-data-dir=\"{profile.UserDataDir}\"",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-background-networking",
            "--disable-sync",
            "--disable-extensions",
            "--disable-component-update",
            "--disable-features=Translate,AutofillServerCommunication",
            "about:blank"
        };

        if (options.Headless)
            arguments.Insert(0, "--headless=new");

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = options.BrowserExecutablePath,
            Arguments = string.Join(" ", arguments),
            UseShellExecute = false,
            CreateNoWindow = options.Headless
        }) ?? throw new InvalidOperationException("Chrome process did not start.");

        sessionManager.MarkState(managedSession.SessionId, BrowserSessionState.Active);
        var session = new ChromeCdpBrowserSession(process, profile, sessionManager, managedSession.SessionId, port);
        try
        {
            await session.WaitUntilReadyAsync(options.StartupTimeout ?? TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}

public sealed class ChromeCdpBrowserSession : IAsyncDisposable
{
    private readonly Process _process;
    private readonly BrowserProfileDescriptor _profile;
    private readonly BrowserSessionManager _sessionManager;
    private readonly ManagedBrowserSessionId _managedSessionId;
    private readonly BrowserProfileManager _profileManager;
    private readonly HttpClient _http = new();
    private bool _disposed;

    internal ChromeCdpBrowserSession(Process process, BrowserProfileDescriptor profile, BrowserSessionManager sessionManager, ManagedBrowserSessionId managedSessionId, int port)
    {
        _process = process;
        _profile = profile;
        _sessionManager = sessionManager;
        _managedSessionId = managedSessionId;
        _profileManager = new BrowserProfileManager();
        Port = port;
        BrowserSessionId = managedSessionId.Value;
    }

    public int Port { get; }
    public string BrowserSessionId { get; }
    public string UserDataDir => _profile.UserDataDir;
    public BrowserProfileDescriptor Profile => _profile;
    public BrowserSessionDescriptor ManagedSession => _sessionManager.Get(_managedSessionId);
    public int ProcessId => _process.Id;
    public Uri VersionEndpoint => new($"http://127.0.0.1:{Port}/json/version");

    public bool IsProcessAlive => !_process.HasExited;

    public async Task WaitUntilReadyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var response = await _http.GetAsync(VersionEndpoint, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                last = ex;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"CDP endpoint did not become ready on localhost:{Port}.", last);
    }

    public async Task<JsonDocument> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var stream = await _http.GetStreamAsync(VersionEndpoint, cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChromeCdpTargetInfo>> ListTargetsAsync(CancellationToken cancellationToken = default)
    {
        var targets = await _http.GetFromJsonAsync<List<JsonElement>>($"http://127.0.0.1:{Port}/json/list", cancellationToken).ConfigureAwait(false) ?? [];
        return targets
            .Where(target => target.TryGetProperty("webSocketDebuggerUrl", out _))
            .Select(target => new ChromeCdpTargetInfo(
                target.GetProperty("id").GetString() ?? "",
                target.GetProperty("type").GetString() ?? "",
                new Uri(target.GetProperty("url").GetString() ?? "about:blank"),
                target.GetProperty("title").GetString() ?? "",
                target.GetProperty("webSocketDebuggerUrl").GetString() ?? ""))
            .ToList();
    }

    public async Task<ChromeCdpPageSession> CreatePageAsync(Uri url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"http://127.0.0.1:{Port}/json/new?{Uri.EscapeDataString(url.ToString())}");
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var target = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);
        var info = new ChromeCdpTargetInfo(
            target.GetProperty("id").GetString() ?? "",
            target.GetProperty("type").GetString() ?? "",
            new Uri(target.GetProperty("url").GetString() ?? url.ToString()),
            target.GetProperty("title").GetString() ?? "",
            target.GetProperty("webSocketDebuggerUrl").GetString() ?? "");
        var page = await ConnectToTargetAsync(info, cancellationToken).ConfigureAwait(false);
        await page.NavigateAsync(url, cancellationToken).ConfigureAwait(false);
        return page;
    }

    public async Task<ChromeCdpPageSession> ConnectToTargetAsync(ChromeCdpTargetInfo target, CancellationToken cancellationToken = default)
    {
        var page = new ChromeCdpPageSession(this, target);
        await page.ConnectAsync(cancellationToken).ConfigureAwait(false);
        return page;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _http.Dispose();

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // Cleanup is best effort; tests assert the process is gone when the environment allows it.
        }
        finally
        {
            _process.Dispose();
            _sessionManager.MarkState(_managedSessionId, BrowserSessionState.CleanupPending);
            await _profileManager.CleanupProfileAsync(_profile).ConfigureAwait(false);
            _sessionManager.MarkState(_managedSessionId, BrowserSessionState.Disposed);
        }
    }
}

public sealed class ChromeCdpPageSession : IAsyncDisposable
{
    private readonly ChromeCdpBrowserSession _browser;
    private readonly ChromeCdpTargetInfo _target;
    private readonly ClientWebSocket _socket = new();
    private readonly BrowserIdempotencyLedger _idempotencyLedger = new();
    private int _nextMessageId;
    private long _generation;
    private bool _disposed;

    internal ChromeCdpPageSession(ChromeCdpBrowserSession browser, ChromeCdpTargetInfo target)
    {
        _browser = browser;
        _target = target;
    }

    public string TargetId => _target.Id;
    public long Generation => _generation;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _socket.ConnectAsync(new Uri(_target.WebSocketDebuggerUrl), cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("Runtime.enable", null, cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("Page.enable", null, cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("DOM.enable", null, cancellationToken).ConfigureAwait(false);
    }

    public async Task NavigateAsync(Uri url, CancellationToken cancellationToken = default)
    {
        _generation++;
        await SendCommandAsync("Page.navigate", new { url = url.ToString() }, cancellationToken).ConfigureAwait(false);
        await WaitForReadyStateAsync("complete", TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserObservation> ObserveAsync(string runId, bool payloadLimitApplied = false, CancellationToken cancellationToken = default)
    {
        var expression = """
(() => {
  const visible = (el) => {
    const style = getComputedStyle(el);
    const rect = el.getBoundingClientRect();
    return style.visibility !== 'hidden' && style.display !== 'none' && rect.width > 0 && rect.height > 0;
  };
  const text = (document.body?.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 4000);
  const actionables = Array.from(document.querySelectorAll('button,input,textarea,select,a,[role=button],[role=textbox]'))
    .filter(visible)
    .slice(0, 50)
    .map((el, index) => ({
      elementId: el.id || `candidate-${index + 1}`,
      role: el.getAttribute('role') || (el.tagName === 'A' ? 'link' : el.tagName.toLowerCase()),
      tagName: el.tagName.toLowerCase(),
      text: (el.innerText || el.value || el.getAttribute('aria-label') || '').slice(0, 200),
      accessibleName: (el.getAttribute('aria-label') || el.getAttribute('title') || '').slice(0, 200),
      label: (el.getAttribute('placeholder') || el.getAttribute('name') || '').slice(0, 200),
      selector: el.id ? `#${CSS.escape(el.id)}` : el.tagName.toLowerCase(),
      isEnabled: !el.disabled
    }));
  return { url: location.href, title: document.title, readyState: document.readyState, text, actionables, frameCount: window.frames.length + 1 };
})()
""";
        var value = await EvaluateReturnValueAsync(expression, cancellationToken).ConfigureAwait(false);
        var url = new Uri(value.GetProperty("url").GetString() ?? "about:blank");
        var title = value.GetProperty("title").GetString() ?? "";
        var readyState = value.GetProperty("readyState").GetString() ?? "";
        var summary = value.GetProperty("text").GetString() ?? "";
        var targetContext = CreateTargetContext(runId, url, title, readyState);
        var actionables = new List<ActionableElement>();

        foreach (var element in value.GetProperty("actionables").EnumerateArray())
        {
            actionables.Add(new ActionableElement(
                element.GetProperty("elementId").GetString() ?? "",
                "main",
                element.GetProperty("role").GetString() ?? "",
                element.GetProperty("tagName").GetString() ?? "",
                element.GetProperty("text").GetString() ?? "",
                element.GetProperty("accessibleName").GetString() ?? "",
                element.GetProperty("label").GetString() ?? "",
                [element.GetProperty("selector").GetString() ?? ""],
                null,
                IsVisible: true,
                IsEnabled: element.GetProperty("isEnabled").GetBoolean(),
                RiskHints: [],
                Confidence: 0.8));
        }

        return new BrowserObservation(
            ObservationId: Guid.NewGuid().ToString("N"),
            RunId: runId,
            TargetContext: targetContext,
            ObservedAtUtc: DateTimeOffset.UtcNow,
            Url: url,
            Title: title,
            ReadyState: readyState,
            FrameCount: value.GetProperty("frameCount").GetInt32(),
            MainFrameId: "main",
            VisibleTextSummary: summary,
            ActionableElements: actionables,
            Forms: [],
            Links: actionables.Where(a => a.TagName == "a").Select(a => new LinkSummary(a.ElementId, a.FrameId, null, a.Text)).ToList(),
            Warnings: [],
            PayloadLimitApplied: payloadLimitApplied,
            SensitivityRedactionApplied: false,
            EvidenceRefs: [Guid.NewGuid().ToString("N")]);
    }

    public async Task<ChromeCdpActionResult> ExecuteActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentTargetContextAsync(action.RunId, cancellationToken).ConfigureAwait(false);
        var validation = action.Validate(current);
        if (!validation.IsValid)
            return FailedAction(action, "invalid_action", string.Join("; ", validation.Errors));

        if (action.CanModifyState)
        {
            var decision = _idempotencyLedger.TryBegin(action, DateTimeOffset.UtcNow);
            if (!decision.Allowed)
                return FailedAction(action, decision.Status.ToString(), decision.Reason);
        }

        try
        {
            switch (action.ActionType)
            {
                case BrowserActionType.Click:
                    await DispatchClickAsync(action.Target.Selector ?? throw new InvalidOperationException("selector required"), cancellationToken).ConfigureAwait(false);
                    break;
                case BrowserActionType.TypeText:
                    await DispatchTypeAsync(action.Target.Selector ?? throw new InvalidOperationException("selector required"), action.Input?.Text ?? action.Input?.Value ?? "", cancellationToken).ConfigureAwait(false);
                    break;
                case BrowserActionType.WaitFor:
                    await WaitForTextAsync(action.ExpectedOutcome.TextContains ?? "", TimeSpan.FromMilliseconds(action.TimeoutMs), cancellationToken).ConfigureAwait(false);
                    break;
                case BrowserActionType.Read:
                case BrowserActionType.Extract:
                case BrowserActionType.NoOp:
                    break;
                case BrowserActionType.Navigate:
                    if (action.Target.Url is null)
                        throw new InvalidOperationException("url required");
                    await NavigateAsync(new Uri(action.Target.Url), cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException($"{action.ActionType} is not implemented in M2.");
            }

            if (action.CanModifyState)
                _idempotencyLedger.Complete(action.IdempotencyKey);

            return new ChromeCdpActionResult(
                action.ActionId,
                Executed: true,
                Status: "Executed",
                CreateEvidence(action, "Action executed. Verification is separate."));
        }
        catch (Exception ex)
        {
            return FailedAction(action, "execution_error", ex.Message);
        }
    }

    public async Task<BrowserVerification> VerifyAsync(BrowserAction action, string postObservationId = "", CancellationToken cancellationToken = default)
    {
        var observation = await ObserveAsync(action.RunId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var outcome = action.ExpectedOutcome;
        var matched = false;
        var reason = "";

        if (!string.IsNullOrWhiteSpace(outcome.UrlContains))
            matched = observation.Url.ToString().Contains(outcome.UrlContains, StringComparison.OrdinalIgnoreCase);
        else if (!string.IsNullOrWhiteSpace(outcome.TextContains))
            matched = observation.VisibleTextSummary.Contains(outcome.TextContains, StringComparison.OrdinalIgnoreCase);
        else if (!string.IsNullOrWhiteSpace(outcome.ElementCandidateId) && !string.IsNullOrWhiteSpace(action.Target.Selector))
            matched = await SelectorHasValueAsync(action.Target.Selector, outcome.ElementCandidateId, cancellationToken).ConfigureAwait(false);
        else
            reason = "No deterministic expectation was provided.";

        var status = matched
            ? BrowserVerificationStatus.Verified
            : string.IsNullOrWhiteSpace(reason) ? BrowserVerificationStatus.Failed : BrowserVerificationStatus.Uncertain;

        return new BrowserVerification(
            VerificationId: Guid.NewGuid().ToString("N"),
            RunId: action.RunId,
            StepId: action.StepId,
            ActionId: action.ActionId,
            TargetContext: observation.TargetContext,
            ExpectedOutcome: outcome,
            PreObservationId: null,
            PostObservationId: string.IsNullOrWhiteSpace(postObservationId) ? observation.ObservationId : postObservationId,
            Status: status,
            Confidence: matched ? 0.95 : 0.2,
            EvidenceRefs: [Guid.NewGuid().ToString("N")],
            FailureReason: matched ? null : string.IsNullOrWhiteSpace(reason) ? "Expected outcome was not observed." : reason,
            VerifiedAtUtc: DateTimeOffset.UtcNow,
            ProofRefs: matched ? [$"proof:{action.ActionId}:{observation.ObservationId}"] : []);
    }

    public static bool ActionResultIsVerified(ChromeCdpActionResult result) =>
        string.Equals(result.Status, "Verified", StringComparison.Ordinal);

    public async Task<BrowserHeartbeat> ProbeLivenessAsync(BrowserTargetContext expected, CancellationToken cancellationToken = default)
    {
        if (_socket.State != WebSocketState.Open)
        {
            return new BrowserHeartbeat(
                Guid.NewGuid().ToString("N"),
                _browser.BrowserSessionId,
                expected.TargetId,
                expected.FrameId,
                expected.Generation,
                DateTimeOffset.UtcNow,
                null,
                BrowserHeartbeatStatus.Disconnected,
                "CDP socket is not open.");
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            var current = await GetCurrentTargetContextAsync(expected.RunId, cancellationToken).ConfigureAwait(false);
            var elapsed = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            return BrowserHeartbeat.FromTargetComparison(Guid.NewGuid().ToString("N"), _browser.BrowserSessionId, expected, current, DateTimeOffset.UtcNow, elapsed);
        }
        catch (WebSocketException ex)
        {
            return new BrowserHeartbeat(Guid.NewGuid().ToString("N"), _browser.BrowserSessionId, expected.TargetId, expected.FrameId, expected.Generation, DateTimeOffset.UtcNow, null, BrowserHeartbeatStatus.Disconnected, ex.Message);
        }
    }

    public async Task<BrowserTargetContext> GetCurrentTargetContextAsync(string runId, CancellationToken cancellationToken = default)
    {
        var value = await EvaluateReturnValueAsync("({ url: location.href, title: document.title, readyState: document.readyState })", cancellationToken).ConfigureAwait(false);
        return CreateTargetContext(
            runId,
            new Uri(value.GetProperty("url").GetString() ?? "about:blank"),
            value.GetProperty("title").GetString() ?? "",
            value.GetProperty("readyState").GetString() ?? "");
    }

    public async Task CloseTargetAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandAsync("Target.closeTarget", new { targetId = _target.Id }, cancellationToken).ConfigureAwait(false);
        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
    }

    private BrowserTargetContext CreateTargetContext(string runId, Uri url, string title, string readyState)
    {
        return new BrowserTargetContext(
            RunId: runId,
            BrowserId: "chrome-cdp",
            BrowserSessionId: _browser.BrowserSessionId,
            BrowserContextId: null,
            WindowId: null,
            TargetId: _target.Id,
            PageId: _target.Id,
            TabId: null,
            FrameId: "main",
            ParentFrameId: null,
            Url: url,
            Title: title,
            Generation: _generation,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(_target.Id, "main", _generation),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: null,
            IsVisible: null,
            IsUserFacing: null,
            ReadyState: readyState,
            Source: BrowserTargetSource.Cdp);
    }

    private async Task WaitForReadyStateAsync(string readyState, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var value = await EvaluateReturnValueAsync("document.readyState", cancellationToken).ConfigureAwait(false);
            if (string.Equals(value.GetString(), readyState, StringComparison.OrdinalIgnoreCase))
                return;
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Page did not reach readyState '{readyState}'.");
    }

    private async Task WaitForTextAsync(string text, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var value = await EvaluateReturnValueAsync($"document.body.innerText.includes({JsonSerializer.Serialize(text)})", cancellationToken).ConfigureAwait(false);
            if (value.GetBoolean())
                return;
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Text was not observed: {text}");
    }

    private async Task DispatchClickAsync(string selector, CancellationToken cancellationToken)
    {
        var point = await GetElementCenterAsync(selector, cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("Input.dispatchMouseEvent", new { type = "mouseMoved", x = point.X, y = point.Y }, cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("Input.dispatchMouseEvent", new { type = "mousePressed", x = point.X, y = point.Y, button = "left", clickCount = 1 }, cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("Input.dispatchMouseEvent", new { type = "mouseReleased", x = point.X, y = point.Y, button = "left", clickCount = 1 }, cancellationToken).ConfigureAwait(false);
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchTypeAsync(string selector, string text, CancellationToken cancellationToken)
    {
        await DispatchClickAsync(selector, cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("Input.insertText", new { text }, cancellationToken).ConfigureAwait(false);
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(double X, double Y)> GetElementCenterAsync(string selector, CancellationToken cancellationToken)
    {
        var expression = $$"""
(() => {
  const el = document.querySelector({{JsonSerializer.Serialize(selector)}});
  if (!el) throw new Error('element_not_found');
  const rect = el.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) throw new Error('element_not_visible');
  return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
})()
""";
        var value = await EvaluateReturnValueAsync(expression, cancellationToken).ConfigureAwait(false);
        return (value.GetProperty("x").GetDouble(), value.GetProperty("y").GetDouble());
    }

    private async Task<bool> SelectorHasValueAsync(string selector, string expectedValue, CancellationToken cancellationToken)
    {
        var expression = $$"""
(() => {
  const el = document.querySelector({{JsonSerializer.Serialize(selector)}});
  return !!el && String(el.value || el.textContent || '').includes({{JsonSerializer.Serialize(expectedValue)}});
})()
""";
        var value = await EvaluateReturnValueAsync(expression, cancellationToken).ConfigureAwait(false);
        return value.GetBoolean();
    }

    private async Task<JsonElement> EvaluateReturnValueAsync(string expression, CancellationToken cancellationToken)
    {
        var response = await SendCommandAsync("Runtime.evaluate", new { expression, returnByValue = true, awaitPromise = true }, cancellationToken).ConfigureAwait(false);
        var result = response.GetProperty("result");
        if (result.TryGetProperty("exceptionDetails", out var exception))
            throw new InvalidOperationException(exception.ToString());
        return result.GetProperty("result").GetProperty("value");
    }

    private async Task<JsonElement> SendCommandAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextMessageId);
        var payload = JsonSerializer.Serialize(new { id, method, @params = parameters });
        var bytes = Encoding.UTF8.GetBytes(payload);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);

        while (true)
        {
            var json = await ReceiveJsonAsync(cancellationToken).ConfigureAwait(false);
            if (!json.RootElement.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id)
                continue;

            if (json.RootElement.TryGetProperty("error", out var error))
                throw new InvalidOperationException(error.ToString());

            return json.RootElement.Clone();
        }
    }

    private async Task<JsonDocument> ReceiveJsonAsync(CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[16 * 1024];
        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new WebSocketException("CDP socket closed.");
            memory.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        memory.Position = 0;
        return await JsonDocument.ParseAsync(memory, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static ChromeCdpActionResult FailedAction(BrowserAction action, string status, string error) =>
        new(action.ActionId, Executed: false, status, CreateEvidence(action, error), error);

    private static BrowserEvidence CreateEvidence(BrowserAction action, string summary) =>
        new(
            EvidenceId: Guid.NewGuid().ToString("N"),
            RunId: action.RunId,
            StepId: action.StepId,
            ActionId: action.ActionId,
            VerificationId: null,
            TargetContext: action.TargetContext,
            EvidenceType: BrowserEvidenceType.CdpEvent,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: summary,
            PayloadRef: null,
            InlinePayload: null,
            RedactionApplied: true,
            SensitivityLevel: BrowserSensitivityLevel.Low);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_socket.State == WebSocketState.Open)
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "dispose", CancellationToken.None).ConfigureAwait(false);
        _socket.Dispose();
    }
}
