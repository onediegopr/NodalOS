namespace OneBrain.BrowserRuntime;

public sealed record CdpConnection(string Host, int? Port, bool IsLive, string State, string Reason)
{
    public static CdpConnection BlockedRuntimeArtifactRequired(string host) =>
        new(host, null, false, "BLOCKED_RUNTIME_ARTIFACT_REQUIRED", BrowserRuntimeDefaults.RuntimeArtifactRequiredReason);
}

public static class CdpEndpointDiscovery
{
    public static Uri ValidateWebSocketDebuggerUrl(string? webSocketDebuggerUrl)
    {
        if (string.IsNullOrWhiteSpace(webSocketDebuggerUrl))
        {
            throw new InvalidOperationException("CDP version endpoint did not include webSocketDebuggerUrl.");
        }

        if (!Uri.TryCreate(webSocketDebuggerUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("CDP webSocketDebuggerUrl is not an absolute URI.");
        }

        if (uri.Scheme is not "ws" and not "wss")
        {
            throw new InvalidOperationException("CDP webSocketDebuggerUrl must use ws or wss.");
        }

        if (!uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("CDP webSocketDebuggerUrl must resolve to 127.0.0.1.");
        }

        return uri;
    }

    public static string CreateTimeoutMessage(Uri endpoint, Exception? lastError) =>
        "CDP /json/version did not become ready at "
        + endpoint
        + ". Last error: "
        + (lastError?.Message ?? "none");
}

public sealed class CdpConnectionLifecycle : IDisposable
{
    private int nextId;
    private bool disposed;

    public int NextCommandId()
    {
        ThrowIfDisposed();
        return Interlocked.Increment(ref nextId);
    }

    public TimeoutException CreateCommandTimeout(string method, TimeSpan timeout) =>
        new("CDP command timed out: " + method + " after " + timeout.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "s.");

    public void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CdpConnectionLifecycle), "CDP connection is already disposed.");
        }
    }

    public void Dispose()
    {
        disposed = true;
    }
}

public sealed record CdpSessionDescriptor(
    string SessionId,
    string TargetId,
    DateTimeOffset CreatedAt,
    string State = "created",
    DateTimeOffset? ClosedAt = null);

public sealed class CdpSessionRegistry
{
    private readonly Dictionary<string, CdpSessionDescriptor> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> sessionIdsByTarget = new(StringComparer.Ordinal);

    public CdpSessionDescriptor Register(string targetId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        if (sessionIdsByTarget.TryGetValue(targetId, out var existingSessionId)
            && sessions.TryGetValue(existingSessionId, out var existing)
            && existing.ClosedAt is null)
        {
            return existing;
        }

        var session = new CdpSessionDescriptor($"cdp-session-{sessions.Count + 1}", targetId, now);
        sessions[session.SessionId] = session;
        sessionIdsByTarget[targetId] = session.SessionId;
        return session;
    }

    public CdpSessionDescriptor Register(string targetId, string sessionId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (sessions.TryGetValue(sessionId, out var existing) && existing.ClosedAt is null)
        {
            return existing;
        }

        var session = new CdpSessionDescriptor(sessionId, targetId, now);
        sessions[session.SessionId] = session;
        sessionIdsByTarget[targetId] = session.SessionId;
        return session;
    }

    public CdpSessionDescriptor? Find(string sessionId) =>
        sessions.TryGetValue(sessionId, out var session) ? session : null;

    public CdpSessionDescriptor MarkState(string sessionId, string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        if (!sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException("CDP session was not found: " + sessionId);
        }

        var updated = session with { State = state };
        sessions[sessionId] = updated;
        return updated;
    }

    public CdpSessionDescriptor MarkClosed(string sessionId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException("CDP session was not found: " + sessionId);
        }

        var updated = session with { State = "closed", ClosedAt = session.ClosedAt ?? now };
        sessions[sessionId] = updated;
        return updated;
    }

    public IReadOnlyCollection<CdpSessionDescriptor> Snapshot() => sessions.Values.ToArray();
}

public sealed record CdpTargetDescriptor(
    string TargetId,
    string Url,
    string Title,
    string Kind,
    string State = "created",
    DateTimeOffset? ClosedAt = null);

public sealed class CdpTargetManager
{
    private readonly Dictionary<string, CdpTargetDescriptor> targets = new(StringComparer.Ordinal);

    public CdpTargetDescriptor DescribePageTarget(string targetId, string url, string title) =>
        new(targetId, url, title, "page");

    public CdpTargetDescriptor TrackPageTarget(string targetId, string url, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var descriptor = new CdpTargetDescriptor(targetId, url, title, "page");
        targets[targetId] = descriptor;
        return descriptor;
    }

    public CdpTargetDescriptor MarkNavigated(string targetId, string url, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var current = targets.TryGetValue(targetId, out var existing)
            ? existing
            : new CdpTargetDescriptor(targetId, url, title, "page");

        var updated = current with { Url = url, Title = title, State = "navigated" };
        targets[targetId] = updated;
        return updated;
    }

    public CdpTargetDescriptor Close(string targetId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var current = targets.TryGetValue(targetId, out var existing)
            ? existing
            : new CdpTargetDescriptor(targetId, string.Empty, string.Empty, "page");

        var updated = current with { State = "closed", ClosedAt = current.ClosedAt ?? now };
        targets[targetId] = updated;
        return updated;
    }

    public static bool IsAllowedHealthcheckNavigation(Uri uri) =>
        uri.Scheme.Equals("data", StringComparison.OrdinalIgnoreCase)
        || uri.ToString().Equals("about:blank", StringComparison.OrdinalIgnoreCase);
}

public sealed record CdpActionRequest(string Kind, string Url, bool ReadOnly);

public sealed class CdpActionController
{
    public CdpActionRequest CreateReadOnlyNavigation(Uri uri) =>
        new("navigate", uri.ToString(), ReadOnly: true);

    public CdpActionRequest CreateHealthcheckNavigation(Uri uri)
    {
        if (!CdpTargetManager.IsAllowedHealthcheckNavigation(uri))
        {
            throw new InvalidOperationException("CDP healthcheck navigation only allows controlled data URLs or about:blank.");
        }

        return CreateReadOnlyNavigation(uri);
    }
}

public sealed record CdpEvidence(
    string Source,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    string? BrowserVersion,
    string? TargetId,
    string? Url,
    string? Title,
    bool BootstrapInjected,
    bool ScreenshotCaptured,
    bool TargetCreated = false,
    bool TargetClosed = false,
    bool SessionCreated = false,
    bool SessionClosed = false,
    bool RuntimeShutdown = false,
    bool OrphanProcessDetected = false,
    bool StoresRawLogs = false);

public sealed class CdpEvidenceAdapter
{
    public CdpEvidence CreateBlockedEvidence(string? url = null, string? title = null) =>
        new(
            Source: "cloakbrowser-cdp-direct",
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            BrowserVersion: null,
            TargetId: null,
            Url: url,
            Title: title,
            BootstrapInjected: false,
            ScreenshotCaptured: false);

    public CdpEvidence CreateLiveEvidence(
        string browserVersion,
        string targetId,
        string url,
        string title,
        bool bootstrapInjected,
        bool screenshotCaptured) =>
        new(
            Source: "cloakbrowser-cdp-direct",
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            BrowserVersion: browserVersion,
            TargetId: targetId,
            Url: url,
            Title: title,
            BootstrapInjected: bootstrapInjected,
            ScreenshotCaptured: screenshotCaptured);

    public CdpEvidence CreateLifecycleEvidence(CloakBrowserCdpHealthcheckResult result) =>
        new(
            Source: "cloakbrowser-cdp-direct",
            ExtensionUsed: result.ExtensionUsed,
            SystemBrowserUsed: result.SystemBrowserUsed,
            BrowserVersion: result.BrowserVersion,
            TargetId: result.TargetId,
            Url: result.Url,
            Title: result.Title,
            BootstrapInjected: result.BootstrapInjected,
            ScreenshotCaptured: result.ScreenshotCaptured,
            TargetCreated: result.TargetCreated,
            TargetClosed: result.TargetClosed,
            SessionCreated: result.SessionCreated,
            SessionClosed: result.SessionClosed,
            RuntimeShutdown: result.RuntimeShutdown,
            OrphanProcessDetected: result.OrphanProcessDetected,
            StoresRawLogs: false);
}
