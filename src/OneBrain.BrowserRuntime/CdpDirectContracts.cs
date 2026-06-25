namespace OneBrain.BrowserRuntime;

public sealed record CdpConnection(string Host, int? Port, bool IsLive, string State, string Reason)
{
    public static CdpConnection BlockedRuntimeArtifactRequired(string host) =>
        new(host, null, false, "BLOCKED_RUNTIME_ARTIFACT_REQUIRED", BrowserRuntimeDefaults.RuntimeArtifactRequiredReason);
}

public sealed record CdpSessionDescriptor(string SessionId, string TargetId, DateTimeOffset CreatedAt);

public sealed class CdpSessionRegistry
{
    private readonly Dictionary<string, CdpSessionDescriptor> sessions = new(StringComparer.Ordinal);

    public CdpSessionDescriptor Register(string targetId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var session = new CdpSessionDescriptor($"cdp-session-{sessions.Count + 1}", targetId, now);
        sessions[session.SessionId] = session;
        return session;
    }

    public CdpSessionDescriptor? Find(string sessionId) =>
        sessions.TryGetValue(sessionId, out var session) ? session : null;
}

public sealed record CdpTargetDescriptor(string TargetId, string Url, string Title, string Kind);

public sealed class CdpTargetManager
{
    public CdpTargetDescriptor DescribePageTarget(string targetId, string url, string title) =>
        new(targetId, url, title, "page");
}

public sealed record CdpActionRequest(string Kind, string Url, bool ReadOnly);

public sealed class CdpActionController
{
    public CdpActionRequest CreateReadOnlyNavigation(Uri uri) =>
        new("navigate", uri.ToString(), ReadOnly: true);
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
    bool ScreenshotCaptured);

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
}
