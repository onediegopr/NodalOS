using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OneBrain.ChromeLab.Bridge;

public static class ChromeLabProtocol
{
    public const string Version = "chrome-lab-v1";
    public const string ServiceName = "onebrain-chrome-lab-bridge";
    public const string EngineVersion = "0.1.0";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}

public sealed record HealthResponse(bool Ok, string Service, string Version);

public sealed record PublicConfigResponse(
    string Service,
    string ProtocolVersion,
    string AiProvider,
    string Model,
    bool HasApiKey,
    bool RequiresToken);

public sealed record ErrorResponse(
    bool Ok,
    string Error,
    string Message,
    string DebugUrl);

public sealed record StartRunRequest(
    string Instruction,
    string? TabId = null,
    string Mode = "lab");

public sealed record RunResponse(
    string RunId,
    string Status,
    string Message);

public sealed record RuntimeResponse(
    bool Ok,
    double UptimeSeconds,
    bool HasApiKey,
    string Provider,
    string Model,
    string Host,
    int Port,
    string? LastError);

public sealed record ClientDiagnosticsResponse(
    int ConnectedCount,
    IReadOnlyList<ClientDiagnostics> Clients);

public sealed record ClientDiagnostics(
    string ClientId,
    bool Connected,
    double LastSeenMs,
    string ProtocolVersion,
    string ExtensionVersion,
    string Browser,
    string Transport,
    string? CurrentRunId,
    string? PendingRequestId,
    string? LastError);

public sealed record ProtocolEvent(
    DateTimeOffset Timestamp,
    string EventType,
    string? RunId,
    string? RequestId,
    string? ClientId,
    string Summary);

public sealed record ToolRequest(
    string Type,
    string RunId,
    string RequestId,
    string Tool,
    Dictionary<string, object?> Args);

public sealed record AgentToolDecision(
    string Tool,
    Dictionary<string, object?> Args,
    string Reason);

public sealed record ToolResultEnvelope(
    string Type,
    string RunId,
    string RequestId,
    bool Success,
    JsonElement? Result,
    string? Error);

public sealed record StatusEnvelope(
    string Type,
    string RunId,
    string Status,
    string Message);

public sealed record ChromeLabRun(
    string RunId,
    string Instruction,
    string Status,
    string Message,
    bool StopRequested,
    string? PausedReason)
{
    public List<string> Log { get; init; } = [];
}

public sealed class ChromeLabRunManager
{
    private readonly ConcurrentDictionary<string, ChromeLabRun> _runs = new(StringComparer.OrdinalIgnoreCase);

    public ChromeLabRun Start(string instruction)
    {
        var run = new ChromeLabRun(
            Guid.NewGuid().ToString("n"),
            instruction,
            "running",
            "run created",
            StopRequested: false,
            PausedReason: null);
        run.Log.Add("run.created");
        _runs[run.RunId] = run;
        return run;
    }

    public ChromeLabRun? Get(string runId) => _runs.TryGetValue(runId, out var run) ? run : null;

    public IReadOnlyList<ChromeLabRun> Snapshot() => _runs.Values.OrderBy(run => run.RunId, StringComparer.Ordinal).ToArray();

    public ChromeLabRun Stop(string runId, string reason = "userStop") =>
        Update(runId, "stopped", reason, stopRequested: true, pausedReason: null);

    public ChromeLabRun Pause(string runId, string reason = "humanInterventionRequired") =>
        Update(runId, "paused", reason, stopRequested: false, pausedReason: reason);

    public ChromeLabRun Resume(string runId)
    {
        if (_runs.TryGetValue(runId, out var current))
        {
            if (string.Equals(current.Status, "running", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(current.Status, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(current.Status, "stopped", StringComparison.OrdinalIgnoreCase))
            {
                current.Log.Add($"run.{current.Status}:resume-noop");
                return current;
            }
        }
        return Update(runId, "running", "resumed", stopRequested: false, pausedReason: null);
    }

    public ChromeLabRun CredentialRequired(string runId, string reason) =>
        Update(runId, "paused", reason, stopRequested: false, pausedReason: "credentialRequired");

    private ChromeLabRun Update(string runId, string status, string message, bool stopRequested, string? pausedReason)
    {
        if (!_runs.TryGetValue(runId, out var current))
        {
            current = new ChromeLabRun(runId, "", "error", "run not found", false, null);
        }

        var updated = current with
        {
            Status = status,
            Message = message,
            StopRequested = stopRequested,
            PausedReason = pausedReason,
            Log = current.Log
        };
        updated.Log.Add($"run.{status}:{message}");
        _runs[runId] = updated;
        return updated;
    }
}

public sealed class ChromeLabClientRegistry
{
    private readonly ConcurrentDictionary<string, ClientConnection> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sendLocks = new(StringComparer.OrdinalIgnoreCase);

    public bool HasConnectedClients => _clients.Values.Any(client => client.Connected && client.Socket.State == WebSocketState.Open);

    public string Add(WebSocket socket)
    {
        var clientId = Guid.NewGuid().ToString("n");
        var now = DateTimeOffset.UtcNow;
        _clients[clientId] = new ClientConnection(
            clientId,
            socket,
            Connected: false,
            ConnectedAt: now,
            LastSeenAt: now,
            LastPingAt: null,
            LastPongAt: null,
            ProtocolVersion: "",
            ExtensionVersion: "",
            Browser: "",
            Transport: "websocket",
            CurrentRunId: null,
            PendingRequestId: null,
            LastError: "Awaiting hello");
        _sendLocks[clientId] = new SemaphoreSlim(1, 1);
        return clientId;
    }

    public SemaphoreSlim? GetSendLock(string clientId)
    {
        _sendLocks.TryGetValue(clientId, out var sem);
        return sem;
    }

    public void RegisterHello(
        string clientId,
        string protocolVersion,
        string extensionVersion,
        string browser,
        string? resumeRunId)
    {
        _clients.AddOrUpdate(
            clientId,
            _ => throw new InvalidOperationException("client not found"),
            (_, current) => current with
            {
                Connected = true,
                LastSeenAt = DateTimeOffset.UtcNow,
                ProtocolVersion = protocolVersion,
                ExtensionVersion = extensionVersion,
                Browser = browser,
                CurrentRunId = resumeRunId,
                LastError = null
            });
    }

    public void MarkSeen(string clientId)
    {
        Update(clientId, current => current with { LastSeenAt = DateTimeOffset.UtcNow });
    }

    public void MarkPing(string clientId)
    {
        var now = DateTimeOffset.UtcNow;
        Update(clientId, current => current with { LastSeenAt = now, LastPingAt = now });
    }

    public void MarkPong(string clientId)
    {
        Update(clientId, current => current with { LastSeenAt = DateTimeOffset.UtcNow, LastPongAt = DateTimeOffset.UtcNow });
    }

    public void MarkRun(string clientId, string? runId, string? requestId)
    {
        Update(clientId, current => current with { CurrentRunId = runId, PendingRequestId = requestId, LastSeenAt = DateTimeOffset.UtcNow });
    }

    public void MarkError(string clientId, string error)
    {
        Update(clientId, current => current with { LastError = Redact(error), LastSeenAt = DateTimeOffset.UtcNow });
    }

    public void Remove(string clientId)
    {
        Update(clientId, current => current with { Connected = false, LastError = "Disconnected", LastSeenAt = DateTimeOffset.UtcNow });
        if (_sendLocks.TryRemove(clientId, out var sem))
            sem.Dispose();
    }

    public async Task SendAsync(string clientId, object message, CancellationToken ct)
    {
        if (!_clients.TryGetValue(clientId, out var client)
            || client.Socket.State != WebSocketState.Open)
            return;

        if (!_sendLocks.TryGetValue(clientId, out var sem))
            return;

        var payload = JsonSerializer.Serialize(message, ChromeLabProtocol.JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);

        await sem.WaitAsync(ct);
        try
        {
            await client.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task BroadcastAsync(object message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(message, ChromeLabProtocol.JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);
        foreach (var (clientId, client) in _clients.ToArray())
        {
            if (!client.Connected || client.Socket.State != WebSocketState.Open)
            {
                Remove(clientId);
                continue;
            }

            if (!_sendLocks.TryGetValue(clientId, out var sem))
                continue;

            await sem.WaitAsync(cancellationToken);
            try
            {
                await client.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
            }
            finally
            {
                sem.Release();
            }
        }
    }

    public ClientDiagnosticsResponse Diagnostics()
    {
        var now = DateTimeOffset.UtcNow;
        var clients = _clients.Values
            .OrderByDescending(client => client.LastSeenAt)
            .Select(client => new ClientDiagnostics(
                client.ClientId,
                client.Connected && client.Socket.State == WebSocketState.Open,
                Math.Max(0, (now - client.LastSeenAt).TotalMilliseconds),
                client.ProtocolVersion,
                client.ExtensionVersion,
                client.Browser,
                client.Transport,
                client.CurrentRunId,
                client.PendingRequestId,
                client.LastError))
            .ToArray();

        return new ClientDiagnosticsResponse(clients.Count(client => client.Connected), clients);
    }

    private void Update(string clientId, Func<ClientConnection, ClientConnection> update)
    {
        _clients.AddOrUpdate(
            clientId,
            _ => throw new InvalidOperationException("client not found"),
            (_, current) => update(current));
    }

    private static string Redact(string value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Length > 240 ? string.Concat(value.AsSpan(0, 240), "...") : value;
}

public sealed record ClientConnection(
    string ClientId,
    WebSocket Socket,
    bool Connected,
    DateTimeOffset ConnectedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? LastPingAt,
    DateTimeOffset? LastPongAt,
    string ProtocolVersion,
    string ExtensionVersion,
    string Browser,
    string Transport,
    string? CurrentRunId,
    string? PendingRequestId,
    string? LastError);

public sealed class ProtocolEventBuffer
{
    private readonly ConcurrentQueue<ProtocolEvent> _events = new();
    private const int MaxEvents = 200;

    public string? LastError { get; private set; }

    public void Add(string eventType, string summary, string? runId = null, string? requestId = null, string? clientId = null)
    {
        var safeSummary = Redact(summary);
        _events.Enqueue(new ProtocolEvent(DateTimeOffset.UtcNow, eventType, runId, requestId, clientId, safeSummary));
        if (eventType.Contains("error", StringComparison.OrdinalIgnoreCase))
            LastError = safeSummary;

        while (_events.Count > MaxEvents && _events.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<ProtocolEvent> Snapshot() => _events.ToArray();

    private static readonly Regex _jwtRedact = new(@"\b[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex _bearerRedact = new(@"\bBearer\s+[A-Za-z0-9._\-]{8,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _apiKeyRedact = new(@"\b(sk-[A-Za-z0-9]{32,}|[A-Za-z0-9_-]{32,})\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex _secretRedact = new(@"(password|passwd|secret|token|access_token|refresh_token|id_token|api[_-]?key|cookie|set-cookie|authorization|bearer|otp|code|clave(?:\s+fiscal)?|sessionid|csrf|xsrf|jwt)\s*[:=]\s*[^\s;,}\""']+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _privateIpRedact = new(@"\b(10\.\d{1,3}|172\.(1[6-9]|2\d|3[01])|192\.168)\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex _emailRedact = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var redacted = _jwtRedact.Replace(value, "[REDACTED-JWT]");
        redacted = _bearerRedact.Replace(redacted, "[REDACTED-TOKEN]");
        redacted = _apiKeyRedact.Replace(redacted, "[REDACTED-KEY]");
        redacted = _secretRedact.Replace(redacted, m => $"{m.Groups[1].Value}[REDACTED]");
        redacted = _privateIpRedact.Replace(redacted, "[REDACTED-IP]");
        redacted = _emailRedact.Replace(redacted, "[REDACTED-EMAIL]");

        return redacted.Length > 2000 ? string.Concat(redacted.AsSpan(0, 2000), "...") : redacted;
    }
}

public sealed record PendingToolRequest(string RunId, string Tool);

public sealed class PendingToolRequestRegistry
{
    private readonly ConcurrentDictionary<string, PendingToolRequest> _pending = new(StringComparer.OrdinalIgnoreCase);

    public void Track(string requestId, string runId, string tool)
    {
        _pending[requestId] = new PendingToolRequest(runId, tool);
    }

    public PendingToolRequest? Complete(string requestId)
    {
        return _pending.TryRemove(requestId, out var pending) ? pending : null;
    }

    public IReadOnlyDictionary<string, PendingToolRequest> Snapshot() => _pending.ToDictionary();
}
