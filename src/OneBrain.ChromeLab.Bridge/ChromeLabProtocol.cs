using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    bool HasApiKey);

public sealed record StartRunRequest(
    string Instruction,
    string? TabId = null,
    string Mode = "lab");

public sealed record RunResponse(
    string RunId,
    string Status,
    string Message);

public sealed record ToolRequest(
    string Type,
    string RunId,
    string RequestId,
    string Tool,
    Dictionary<string, object?> Args);

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

    public ChromeLabRun Stop(string runId, string reason = "userStop") =>
        Update(runId, "stopped", reason, stopRequested: true, pausedReason: null);

    public ChromeLabRun Pause(string runId, string reason = "humanInterventionRequired") =>
        Update(runId, "paused", reason, stopRequested: false, pausedReason: reason);

    public ChromeLabRun Resume(string runId) =>
        Update(runId, "running", "resumed", stopRequested: false, pausedReason: null);

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
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new(StringComparer.OrdinalIgnoreCase);

    public bool HasConnectedClients => !_clients.IsEmpty;

    public string Add(WebSocket socket)
    {
        var clientId = Guid.NewGuid().ToString("n");
        _clients[clientId] = socket;
        return clientId;
    }

    public void Remove(string clientId) => _clients.TryRemove(clientId, out _);

    public async Task BroadcastAsync(object message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(message, ChromeLabProtocol.JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);
        foreach (var client in _clients.ToArray())
        {
            if (client.Value.State != WebSocketState.Open)
            {
                Remove(client.Key);
                continue;
            }

            await client.Value.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }
    }
}
