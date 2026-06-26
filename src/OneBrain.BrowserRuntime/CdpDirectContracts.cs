namespace OneBrain.BrowserRuntime;

using System.Text.Json;
using System.Text.RegularExpressions;

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

public sealed record CdpPageMetadata(
    string Url,
    string Title,
    string ReadyState);

public sealed record CdpDomNodeSummary(
    string StableId,
    string Tag,
    string Role,
    string TextPreview,
    bool Visible);

public sealed record CdpInteractiveElementSummary(
    string StableId,
    string Tag,
    string Role,
    string Label,
    string? InputType,
    string? Href,
    bool Enabled,
    bool Visible,
    string SelectorHint,
    bool Required = false);

public sealed record CdpDomSnapshot(
    CdpPageMetadata PageMetadata,
    int NodeCount,
    string TextPreview,
    IReadOnlyList<CdpInteractiveElementSummary> InteractiveElements,
    int FormsCount,
    int LinksCount,
    int ButtonsCount,
    int InputsCount,
    bool ScreenshotsAvailable,
    string Source,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    int HeadingsCount = 0,
    int DisabledElementsCount = 0,
    int RequiredEmptyFieldsCount = 0,
    string PageStructureSummary = "")
{
    public bool StoresRawHtml => false;

    public bool StoresInputValues => false;

    public bool SecretsRedacted => true;
}

public sealed record CdpDomSnapshotEvidence(
    string Source,
    CdpPageMetadata PageMetadata,
    int NodeCount,
    int InteractiveElementCount,
    int FormsCount,
    int LinksCount,
    int ButtonsCount,
    int InputsCount,
    bool ScreenshotsAvailable,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool StoresRawHtml,
    bool StoresInputValues,
    bool SecretsRedacted,
    int HeadingsCount = 0,
    int DisabledElementsCount = 0,
    int RequiredEmptyFieldsCount = 0,
    string PageStructureSummary = "");

public enum CdpControlledActionKind
{
    ReadPageMetadata,
    CaptureDomSnapshot,
    ClickElementByStableId,
    TypeTextByStableId,
    ClearInputByStableId,
    CaptureScreenshot,
    WaitForDomReady,
    NavigateExternalUrl,
    SubmitForm,
    UploadFile,
    DownloadFile,
    SolveChallenge,
    RunArbitraryScript
}

public sealed record CdpControlledActionRequest(
    CdpControlledActionKind ActionKind,
    string? StableId = null,
    string? Text = null,
    string? Url = null,
    bool ReadOnly = true);

public sealed record CdpControlledActionEvidence(
    CdpControlledActionKind ActionKind,
    string Target,
    string Status,
    string Reason,
    bool CdpCommandsExecuted,
    bool ProductCommandsExecuted,
    bool FilesModified,
    bool ExtensionUsed,
    bool SystemBrowserUsed);

public sealed record CdpControlledActionResult(
    CdpControlledActionKind ActionKind,
    string Target,
    string Status,
    string Reason,
    CdpControlledActionEvidence Evidence);

public sealed class CdpDomSnapshotManager
{
    public const int DefaultMaxNodes = 120;
    public const int DefaultMaxInteractiveElements = 40;
    public const int DefaultMaxTextPreviewCharacters = 180;

    public string BuildDomSnapshotExpression(
        int maxNodes = DefaultMaxNodes,
        int maxInteractiveElements = DefaultMaxInteractiveElements,
        int maxTextPreviewCharacters = DefaultMaxTextPreviewCharacters) =>
        $$"""
(() => {
  const maxNodes = {{maxNodes.ToString(System.Globalization.CultureInfo.InvariantCulture)}};
  const maxInteractiveElements = {{maxInteractiveElements.ToString(System.Globalization.CultureInfo.InvariantCulture)}};
  const maxTextPreviewCharacters = {{maxTextPreviewCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture)}};
  const ignoredTags = new Set(['SCRIPT', 'STYLE', 'NOSCRIPT', 'TEMPLATE']);
  const normalize = (value, limit = maxTextPreviewCharacters) => {
    const text = String(value || '')
      .replace(/\s+/g, ' ')
      .replace(/[A-Za-z0-9_-]{24,}\.[A-Za-z0-9_-]{12,}\.[A-Za-z0-9_-]{12,}/g, '[redacted-token]')
      .replace(/(api[_-]?key|token|secret|password)\s*[:=]\s*\S+/ig, '$1=[redacted]');
    return text.trim().slice(0, limit);
  };
  const isVisible = (element) => {
    if (!element || typeof element.getBoundingClientRect !== 'function') return false;
    const style = window.getComputedStyle(element);
    const rect = element.getBoundingClientRect();
    return style.display !== 'none'
      && style.visibility !== 'hidden'
      && Number(rect.width) > 0
      && Number(rect.height) > 0;
  };
  const roleOf = (element) => {
    const explicitRole = element.getAttribute('role');
    if (explicitRole) return normalize(explicitRole, 32);
    const tag = element.tagName.toLowerCase();
    if (tag === 'button') return 'button';
    if (tag === 'a') return 'link';
    if (tag === 'input' || tag === 'textarea' || tag === 'select') return 'input';
    if (element.isContentEditable) return 'textbox';
    return tag;
  };
  const labelOf = (element) => {
    const tag = element.tagName.toLowerCase();
    const type = String(element.getAttribute('type') || '').toLowerCase();
    if (tag === 'input' && (type === 'password' || type === 'hidden')) {
      return '[redacted input]';
    }
    return normalize(
      element.getAttribute('aria-label')
        || element.getAttribute('title')
        || element.getAttribute('placeholder')
        || element.innerText
        || element.textContent
        || element.getAttribute('name')
        || tag,
      96);
  };
  const selectorHintFor = (element, stableId) => {
    if (stableId) return `[data-nodal-cdp-stable-id="${stableId}"]`;
    const id = element.getAttribute('id');
    if (id && /^[A-Za-z][A-Za-z0-9_-]{0,63}$/.test(id)) return `#${id}`;
    return element.tagName.toLowerCase();
  };
  const safeHref = (element) => {
    const raw = element.getAttribute('href');
    if (!raw) return null;
    try {
      const parsed = new URL(raw, window.location.href);
      if (parsed.protocol === 'data:' || parsed.protocol === 'about:') return parsed.protocol;
      return 'external-url-redacted';
    } catch {
      return 'href-redacted';
    }
  };
  const allNodes = Array.from(document.querySelectorAll('*'))
    .filter((node) => !ignoredTags.has(node.tagName))
    .slice(0, maxNodes);
  const interactive = Array.from(document.querySelectorAll('button,input,textarea,select,a[href],[role="button"],[contenteditable="true"]'))
    .filter((node) => !ignoredTags.has(node.tagName))
    .slice(0, maxInteractiveElements)
    .map((element, index) => {
      const stableId = `cdp-el-${index + 1}`;
      element.setAttribute('data-nodal-cdp-stable-id', stableId);
      const tag = element.tagName.toLowerCase();
      const inputType = tag === 'input' ? String(element.getAttribute('type') || 'text').toLowerCase() : null;
      return {
        stableId,
        tag,
        role: roleOf(element),
        label: labelOf(element),
        inputType,
        href: tag === 'a' ? safeHref(element) : null,
        enabled: !element.disabled,
        visible: isVisible(element),
        selectorHint: selectorHintFor(element, stableId),
        required: element.hasAttribute('required')
      };
    });
  const headingsCount = document.querySelectorAll('h1,h2,h3,h4,h5,h6,[role="heading"]').length;
  const disabledElementsCount = document.querySelectorAll('button:disabled,input:disabled,textarea:disabled,select:disabled,[aria-disabled="true"]').length;
  const requiredEmptyFieldsCount = Array.from(document.querySelectorAll('input[required],textarea[required],select[required]'))
    .filter((element) => element.validity && element.validity.valueMissing)
    .length;
  const pageStructureSummary = [
    `headings:${headingsCount}`,
    `forms:${document.forms ? document.forms.length : 0}`,
    `links:${document.links ? document.links.length : 0}`,
    `buttons:${document.querySelectorAll('button,[role="button"]').length}`,
    `inputs:${document.querySelectorAll('input,textarea,select,[contenteditable="true"]').length}`
  ].join(' ');

  return {
    pageMetadata: {
      url: String(window.location.href || ''),
      title: String(document.title || ''),
      readyState: String(document.readyState || 'unknown')
    },
    nodeCount: allNodes.length,
    textPreview: normalize(document.body ? document.body.innerText : '', maxTextPreviewCharacters),
    interactiveElements: interactive,
    formsCount: document.forms ? document.forms.length : 0,
    linksCount: document.links ? document.links.length : 0,
    buttonsCount: document.querySelectorAll('button,[role="button"]').length,
    inputsCount: document.querySelectorAll('input,textarea,select,[contenteditable="true"]').length,
    headingsCount,
    disabledElementsCount,
    requiredEmptyFieldsCount,
    pageStructureSummary,
    screenshotsAvailable: true,
    source: 'cloakbrowser-cdp-direct',
    extensionUsed: false,
    systemBrowserUsed: false
  };
})();
""";

    public CdpDomSnapshot ParseRuntimeEvaluateResult(JsonElement runtimeEvaluateResult)
    {
        var value = runtimeEvaluateResult.TryGetProperty("result", out var result)
            && result.TryGetProperty("value", out var resultValue)
            ? resultValue
            : runtimeEvaluateResult;

        var page = value.GetProperty("pageMetadata");
        var interactive = new List<CdpInteractiveElementSummary>();
        if (value.TryGetProperty("interactiveElements", out var elements)
            && elements.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in elements.EnumerateArray())
            {
                interactive.Add(new CdpInteractiveElementSummary(
                    StableId: ReadString(element, "stableId"),
                    Tag: ReadString(element, "tag"),
                    Role: ReadString(element, "role"),
                    Label: ReadString(element, "label"),
                    InputType: ReadNullableString(element, "inputType"),
                    Href: ReadNullableString(element, "href"),
                    Enabled: ReadBoolean(element, "enabled"),
                    Visible: ReadBoolean(element, "visible"),
                    SelectorHint: ReadString(element, "selectorHint"),
                    Required: ReadBoolean(element, "required")));
            }
        }

        return new CdpDomSnapshot(
            PageMetadata: new CdpPageMetadata(
                Url: ReadString(page, "url"),
                Title: ReadString(page, "title"),
                ReadyState: ReadString(page, "readyState")),
            NodeCount: ReadInt32(value, "nodeCount"),
            TextPreview: ReadString(value, "textPreview"),
            InteractiveElements: interactive,
            FormsCount: ReadInt32(value, "formsCount"),
            LinksCount: ReadInt32(value, "linksCount"),
            ButtonsCount: ReadInt32(value, "buttonsCount"),
            InputsCount: ReadInt32(value, "inputsCount"),
            ScreenshotsAvailable: ReadBoolean(value, "screenshotsAvailable"),
            Source: ReadString(value, "source"),
            ExtensionUsed: ReadBoolean(value, "extensionUsed"),
            SystemBrowserUsed: ReadBoolean(value, "systemBrowserUsed"),
            HeadingsCount: ReadInt32(value, "headingsCount"),
            DisabledElementsCount: ReadInt32(value, "disabledElementsCount"),
            RequiredEmptyFieldsCount: ReadInt32(value, "requiredEmptyFieldsCount"),
            PageStructureSummary: ReadString(value, "pageStructureSummary"));
    }

    public CdpDomSnapshotEvidence CreateEvidence(CdpDomSnapshot snapshot) =>
        new(
            Source: snapshot.Source,
            PageMetadata: snapshot.PageMetadata,
            NodeCount: snapshot.NodeCount,
            InteractiveElementCount: snapshot.InteractiveElements.Count,
            FormsCount: snapshot.FormsCount,
            LinksCount: snapshot.LinksCount,
            ButtonsCount: snapshot.ButtonsCount,
            InputsCount: snapshot.InputsCount,
            ScreenshotsAvailable: snapshot.ScreenshotsAvailable,
            ExtensionUsed: snapshot.ExtensionUsed,
            SystemBrowserUsed: snapshot.SystemBrowserUsed,
            StoresRawHtml: snapshot.StoresRawHtml,
            StoresInputValues: snapshot.StoresInputValues,
            SecretsRedacted: snapshot.SecretsRedacted,
            HeadingsCount: snapshot.HeadingsCount,
            DisabledElementsCount: snapshot.DisabledElementsCount,
            RequiredEmptyFieldsCount: snapshot.RequiredEmptyFieldsCount,
            PageStructureSummary: snapshot.PageStructureSummary);

    public bool ContainsForbiddenRawDataReads(string script) =>
        script.Contains("outerHTML", StringComparison.Ordinal)
        || script.Contains("innerHTML", StringComparison.Ordinal)
        || script.Contains("document.cookie", StringComparison.Ordinal)
        || script.Contains("localStorage", StringComparison.Ordinal)
        || script.Contains("sessionStorage", StringComparison.Ordinal);

    private static string ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static string? ReadNullableString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int ReadInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : 0;

    private static bool ReadBoolean(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
        && (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
            && property.GetBoolean();
}

public sealed class CdpActionController
{
    private static readonly Regex StableIdPattern = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

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

    public CdpControlledActionRequest CreateClickElementByStableId(string stableId) =>
        new(CdpControlledActionKind.ClickElementByStableId, StableId: ValidateStableId(stableId));

    public CdpControlledActionRequest CreateTypeTextByStableId(string stableId, string text)
    {
        ValidateTextInput(text);
        return new(CdpControlledActionKind.TypeTextByStableId, StableId: ValidateStableId(stableId), Text: text);
    }

    public CdpControlledActionRequest CreateClearInputByStableId(string stableId) =>
        new(CdpControlledActionKind.ClearInputByStableId, StableId: ValidateStableId(stableId));

    public CdpControlledActionRequest CreateExternalNavigation(Uri uri)
    {
        if (!CdpTargetManager.IsAllowedHealthcheckNavigation(uri))
        {
            throw new InvalidOperationException("Controlled CDP actions do not allow external navigation in V1.");
        }

        return new(CdpControlledActionKind.NavigateExternalUrl, Url: uri.ToString());
    }

    public CdpControlledActionRequest CreateForbiddenFormSubmit(string stableId) =>
        throw new InvalidOperationException("Controlled CDP actions do not allow form submission in V1.");

    public CdpControlledActionResult RejectForbiddenAction(CdpControlledActionKind actionKind, string reason) =>
        new(
            ActionKind: actionKind,
            Target: "none",
            Status: "blocked",
            Reason: reason,
            Evidence: CreateEvidence(actionKind, "none", "blocked", reason));

    public CdpControlledActionResult RejectUnknownStableId(CdpControlledActionRequest request, CdpDomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (request.StableId is null
            || snapshot.InteractiveElements.All(element => !element.StableId.Equals(request.StableId, StringComparison.Ordinal)))
        {
            return new CdpControlledActionResult(
                request.ActionKind,
                request.StableId ?? "none",
                "blocked",
                "Unknown stable id.",
                CreateEvidence(request.ActionKind, request.StableId ?? "none", "blocked", "Unknown stable id."));
        }

        return new CdpControlledActionResult(
            request.ActionKind,
            request.StableId,
            "ready",
            "Stable id is present.",
            CreateEvidence(request.ActionKind, request.StableId, "ready", "Stable id is present."));
    }

    public string BuildControlledActionExpression(CdpControlledActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.ActionKind switch
        {
            CdpControlledActionKind.ClickElementByStableId => BuildClickExpression(ValidateStableId(request.StableId)),
            CdpControlledActionKind.TypeTextByStableId => BuildTypeExpression(ValidateStableId(request.StableId), ValidateTextInput(request.Text)),
            CdpControlledActionKind.ClearInputByStableId => BuildClearExpression(ValidateStableId(request.StableId)),
            CdpControlledActionKind.ReadPageMetadata => "window.__NODAL_OS_CDP_PAGE_METADATA__ && window.__NODAL_OS_CDP_PAGE_METADATA__()",
            CdpControlledActionKind.WaitForDomReady => "({ status: document.readyState === 'complete' || document.readyState === 'interactive' ? 'completed' : 'waiting', readyState: document.readyState })",
            CdpControlledActionKind.CaptureDomSnapshot => throw new InvalidOperationException("Use CdpDomSnapshotManager for DOM snapshots."),
            CdpControlledActionKind.CaptureScreenshot => throw new InvalidOperationException("Use Page.captureScreenshot for screenshots."),
            CdpControlledActionKind.NavigateExternalUrl => throw new InvalidOperationException("Controlled CDP actions do not allow external navigation in V1."),
            CdpControlledActionKind.SubmitForm => throw new InvalidOperationException("Controlled CDP actions do not allow form submission in V1."),
            CdpControlledActionKind.UploadFile => throw new InvalidOperationException("Controlled CDP actions do not allow file upload in V1."),
            CdpControlledActionKind.DownloadFile => throw new InvalidOperationException("Controlled CDP actions do not allow file download in V1."),
            CdpControlledActionKind.SolveChallenge => throw new InvalidOperationException("Controlled CDP actions do not allow challenge solving in V1."),
            CdpControlledActionKind.RunArbitraryScript => throw new InvalidOperationException("Controlled CDP actions do not allow arbitrary user script in V1."),
            _ => throw new InvalidOperationException("Unsupported controlled CDP action.")
        };
    }

    public CdpControlledActionResult ParseRuntimeEvaluateActionResult(
        CdpControlledActionKind actionKind,
        string target,
        JsonElement runtimeEvaluateResult)
    {
        var value = runtimeEvaluateResult.TryGetProperty("result", out var result)
            && result.TryGetProperty("value", out var resultValue)
            ? resultValue
            : runtimeEvaluateResult;
        var status = value.TryGetProperty("status", out var statusProperty)
            ? statusProperty.GetString() ?? "blocked"
            : "blocked";
        var reason = value.TryGetProperty("reason", out var reasonProperty)
            ? reasonProperty.GetString() ?? status
            : status;

        return new CdpControlledActionResult(
            actionKind,
            target,
            status,
            reason,
            CreateEvidence(actionKind, target, status, reason));
    }

    public CdpControlledActionEvidence CreateEvidence(
        CdpControlledActionKind actionKind,
        string target,
        string status,
        string reason) =>
        new(
            ActionKind: actionKind,
            Target: target,
            Status: status,
            Reason: reason,
            CdpCommandsExecuted: true,
            ProductCommandsExecuted: false,
            FilesModified: false,
            ExtensionUsed: false,
            SystemBrowserUsed: false);

    private static string BuildClickExpression(string stableId)
    {
        var stableIdJson = JsonSerializer.Serialize(stableId);
        return $$"""
(() => {
  const stableId = {{stableIdJson}};
  const element = document.querySelector(`[data-nodal-cdp-stable-id="${stableId}"]`);
  if (!element) return { status: 'blocked', reason: 'Unknown stable id.' };
  const style = window.getComputedStyle(element);
  const rect = element.getBoundingClientRect();
  const visible = style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
  if (!visible || element.disabled) return { status: 'blocked', reason: 'Element is not clickable.' };
  element.click();
  return { status: 'completed', reason: 'Controlled click completed.', target: stableId, clicked: true };
})();
""";
    }

    private static string BuildTypeExpression(string stableId, string text)
    {
        var stableIdJson = JsonSerializer.Serialize(stableId);
        var textJson = JsonSerializer.Serialize(text);
        return $$"""
(() => {
  const stableId = {{stableIdJson}};
  const text = {{textJson}};
  const element = document.querySelector(`[data-nodal-cdp-stable-id="${stableId}"]`);
  if (!element) return { status: 'blocked', reason: 'Unknown stable id.' };
  const tag = element.tagName.toLowerCase();
  const type = String(element.getAttribute('type') || 'text').toLowerCase();
  if (!['input', 'textarea'].includes(tag) || ['password', 'hidden', 'file'].includes(type)) {
    return { status: 'blocked', reason: 'Element does not accept controlled text.' };
  }
  element.focus();
  element.value = text;
  element.dispatchEvent(new Event('input', { bubbles: true }));
  element.dispatchEvent(new Event('change', { bubbles: true }));
  return { status: 'completed', reason: 'Controlled text entered.', target: stableId, textLength: text.length };
})();
""";
    }

    private static string BuildClearExpression(string stableId)
    {
        var stableIdJson = JsonSerializer.Serialize(stableId);
        return $$"""
(() => {
  const stableId = {{stableIdJson}};
  const element = document.querySelector(`[data-nodal-cdp-stable-id="${stableId}"]`);
  if (!element) return { status: 'blocked', reason: 'Unknown stable id.' };
  const tag = element.tagName.toLowerCase();
  const type = String(element.getAttribute('type') || 'text').toLowerCase();
  if (!['input', 'textarea'].includes(tag) || ['password', 'hidden', 'file'].includes(type)) {
    return { status: 'blocked', reason: 'Element cannot be cleared by controlled action.' };
  }
  element.value = '';
  element.dispatchEvent(new Event('input', { bubbles: true }));
  element.dispatchEvent(new Event('change', { bubbles: true }));
  return { status: 'completed', reason: 'Controlled input cleared.', target: stableId };
})();
""";
    }

    private static string ValidateStableId(string? stableId)
    {
        if (string.IsNullOrWhiteSpace(stableId) || !StableIdPattern.IsMatch(stableId))
        {
            throw new InvalidOperationException("Controlled CDP action stable id is invalid.");
        }

        return stableId;
    }

    private static string ValidateTextInput(string? text)
    {
        if (text is null)
        {
            throw new InvalidOperationException("Controlled CDP action text is required.");
        }

        if (text.Length > 256)
        {
            throw new InvalidOperationException("Controlled CDP action text is too long for V1.");
        }

        if (text.Contains("password", StringComparison.OrdinalIgnoreCase)
            || text.Contains("token", StringComparison.OrdinalIgnoreCase)
            || text.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || text.Contains("api_key", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Controlled CDP action text looks sensitive and was rejected.");
        }

        return text;
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
    bool StoresRawLogs = false,
    bool StoresRawHtml = false,
    bool StoresInputValues = false,
    bool SecretsRedacted = true,
    bool ExternalNavigationAttempted = false,
    bool ExternalNavigationBlocked = false,
    bool ProductFilesModified = false,
    CdpDomSnapshotEvidence? DomSnapshot = null,
    IReadOnlyList<CdpControlledActionEvidence>? ActionResults = null);

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
            StoresRawLogs: false,
            StoresRawHtml: result.RawHtmlStored,
            StoresInputValues: result.InputValuesStored,
            SecretsRedacted: result.SecretsRedacted,
            ExternalNavigationAttempted: result.ExternalNavigationAttempted,
            ExternalNavigationBlocked: result.ExternalNavigationBlocked,
            ProductFilesModified: result.FilesModified,
            DomSnapshot: result.DomSnapshotEvidence,
            ActionResults: result.ControlledActionEvidence);

    public CdpEvidence CreateDomActionEvidence(
        CdpDomSnapshot snapshot,
        IReadOnlyList<CdpControlledActionEvidence> actionResults,
        bool screenshotCaptured,
        bool externalNavigationAttempted,
        bool externalNavigationBlocked) =>
        new(
            Source: "cloakbrowser-cdp-direct",
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            BrowserVersion: null,
            TargetId: null,
            Url: snapshot.PageMetadata.Url,
            Title: snapshot.PageMetadata.Title,
            BootstrapInjected: true,
            ScreenshotCaptured: screenshotCaptured,
            StoresRawLogs: false,
            StoresRawHtml: snapshot.StoresRawHtml,
            StoresInputValues: snapshot.StoresInputValues,
            SecretsRedacted: snapshot.SecretsRedacted,
            ExternalNavigationAttempted: externalNavigationAttempted,
            ExternalNavigationBlocked: externalNavigationBlocked,
            ProductFilesModified: false,
            DomSnapshot: new CdpDomSnapshotManager().CreateEvidence(snapshot),
            ActionResults: actionResults);
}
