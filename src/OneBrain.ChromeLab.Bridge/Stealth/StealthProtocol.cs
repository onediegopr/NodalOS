using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge.Stealth;

public static class StealthProtocol
{
    public const string Version = "stealth-v1";

    public const string MessageTypeStealthHello = "stealth.hello";
    public const string MessageTypeStealthAck = "stealth.ack";
    public const string MessageTypeStealthTask = "stealth.task";
    public const string MessageTypeStealthResult = "stealth.result";
    public const string MessageTypeStealthPause = "stealth.pause";
    public const string MessageTypeStealthResume = "stealth.resume";
    public const string MessageTypeStealthStop = "stealth.stop";

    public const string MessageTypeFrictionSignal = "stealth.friction.signal";
    public const string MessageTypeFrictionDecision = "stealth.friction.decision";
    public const string MessageTypeFrictionSolved = "stealth.friction.solved";

    public const string MessageTypeHandoffActivate = "stealth.handoff.activate";
    public const string MessageTypeHandoffStatus = "stealth.handoff.status";
    public const string MessageTypeHandoffCompleted = "stealth.handoff.completed";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

public sealed record StealthFrictionSignalMessage(
    string Type,
    string TaskId,
    StealthFrictionSignalPayload Signal);

public sealed record StealthFrictionSignalPayload(
    string SignalId,
    string Kind,
    string Severity,
    string Source,
    string FrameId,
    string? ElementId,
    string? Sitekey,
    string? BlockHttpCode,
    string? BlockPattern,
    string RedactedEvidence,
    string Reason,
    string DetectedAtUtc,
    bool AutoSolvable,
    string? SolverRecommendation,
    string[]? EvidenceRefs,
    string[]? ProofRefs);

public sealed record StealthFrictionDecisionMessage(
    string Type,
    string TaskId,
    string SignalId,
    string Decision,
    string? SolverProvider,
    string? Sitekey,
    int RetryAttempt,
    int MaxRetries,
    int? CooldownMs,
    string Message,
    bool RotateProxy,
    bool RotateProfile);

public sealed record StealthFrictionSolvedMessage(
    string Type,
    string TaskId,
    string SignalId,
    bool Success,
    string? Token,
    string? Provider,
    int DurationMs,
    double Cost,
    string? Error);

public sealed record StealthHandoffActivateMessage(
    string Type,
    string TaskId,
    string HandoffId,
    string Reason,
    string Message,
    bool ScreenshotRequired);

public sealed record StealthHandoffStatusMessage(
    string Type,
    string TaskId,
    string HandoffId,
    string Status,
    string? Message);

public sealed record StealthHandoffCompletedMessage(
    string Type,
    string TaskId,
    string HandoffId,
    bool Success);

public sealed record StealthTaskMessage(
    string Type,
    string TaskId,
    string Instruction,
    string? Url,
    StealthTaskProfile? Profile,
    StealthTaskProxy? Proxy,
    int MaxRetries,
    bool SessionPersistence);

public sealed record StealthTaskProfile(
    string? ProfileId,
    string? Preset,
    string DeviceType,
    string Os,
    string? Country,
    int? ViewportWidth,
    int? ViewportHeight);

public sealed record StealthTaskProxy(
    string? Server,
    string? Username,
    string? Password,
    string? Type,
    string? Country);

public sealed class StealthTaskManager
{
    private readonly ConcurrentDictionary<string, StealthTask> _tasks = new(StringComparer.Ordinal);

    public StealthTask Start(string instruction)
    {
        var task = new StealthTask(
            TaskId: Guid.NewGuid().ToString("n"),
            Instruction: instruction,
            Status: "running",
            Message: "task created",
            RunnerId: null,
            CurrentRetryCount: 0,
            Log: ["task.created"]);
        _tasks[task.TaskId] = task;
        return task;
    }

    public StealthTask? Get(string taskId)
        => _tasks.TryGetValue(taskId, out var t) ? t : null;

    public StealthTask Pause(string taskId, string reason)
    {
        return Update(taskId, "paused", reason);
    }

    public StealthTask Resume(string taskId)
    {
        return Update(taskId, "running", "resumed");
    }

    public StealthTask Complete(string taskId, string message)
    {
        return Update(taskId, "completed", message);
    }

    public StealthTask Error(string taskId, string error)
    {
        return Update(taskId, "error", error);
    }

    public IReadOnlyList<StealthTask> Snapshot() =>
        _tasks.Values.OrderBy(t => t.TaskId, StringComparer.Ordinal).ToList();

    public StealthTask IncrementRetry(string taskId)
    {
        if (_tasks.TryGetValue(taskId, out var t))
        {
            var updated = t with { CurrentRetryCount = t.CurrentRetryCount + 1 };
            _tasks[taskId] = updated;
            return updated;
        }
        throw new InvalidOperationException("task not found");
    }

    private StealthTask Update(string taskId, string status, string message)
    {
        if (!_tasks.TryGetValue(taskId, out var current))
            current = new StealthTask(taskId, "", "error", "task not found", null, 0, []);

        var updated = current with
        {
            Status = status,
            Message = message,
            Log = [.. current.Log, $"task.{status}:{message}"]
        };
        _tasks[taskId] = updated;
        return updated;
    }
}

public sealed record StealthTask(
    string TaskId,
    string Instruction,
    string Status,
    string Message,
    string? RunnerId,
    int CurrentRetryCount,
    List<string> Log);

public sealed class StealthRunnerRegistry
{
    private readonly ConcurrentDictionary<string, StealthRunnerConnection> _runners = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sendLocks = new(StringComparer.Ordinal);

    public bool HasConnectedRunner =>
        _runners.Values.Any(r => r.Connected && r.Socket.State == WebSocketState.Open);

    public string Add(WebSocket socket, string runnerId)
    {
        _runners[runnerId] = new StealthRunnerConnection(
            RunnerId: runnerId,
            Socket: socket,
            Connected: false,
            ConnectedAt: DateTimeOffset.UtcNow,
            LastSeenAt: DateTimeOffset.UtcNow,
            CurrentTaskId: null,
            Capabilities: []);
        _sendLocks[runnerId] = new SemaphoreSlim(1, 1);
        return runnerId;
    }

    public void MarkConnected(string runnerId, string[] capabilities)
    {
        _runners.AddOrUpdate(runnerId,
            _ => throw new InvalidOperationException("runner not found"),
            (_, r) => r with { Connected = true, LastSeenAt = DateTimeOffset.UtcNow, Capabilities = capabilities });
    }

    public void MarkSeen(string runnerId)
    {
        _runners.AddOrUpdate(runnerId,
            _ => throw new InvalidOperationException("runner not found"),
            (_, r) => r with { LastSeenAt = DateTimeOffset.UtcNow });
    }

    public void AssignTask(string runnerId, string taskId)
    {
        _runners.AddOrUpdate(runnerId,
            _ => throw new InvalidOperationException("runner not found"),
            (_, r) => r with { CurrentTaskId = taskId });
    }

    public async Task BroadcastAsync(object message, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(message, StealthProtocol.JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        foreach (var (runnerId, runner) in _runners)
        {
            if (!runner.Connected || runner.Socket.State != WebSocketState.Open)
                continue;

            if (!_sendLocks.TryGetValue(runnerId, out var sem))
                continue;

            await sem.WaitAsync(ct);
            try
            {
                await runner.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
            }
            finally
            {
                sem.Release();
            }
        }
    }

    public async Task SendToRunnerAsync(string runnerId, object message, CancellationToken ct)
    {
        if (!_runners.TryGetValue(runnerId, out var runner)
            || runner.Socket.State != WebSocketState.Open)
            return;

        if (!_sendLocks.TryGetValue(runnerId, out var sem))
            return;

        var json = JsonSerializer.Serialize(message, StealthProtocol.JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await sem.WaitAsync(ct);
        try
        {
            await runner.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }
        finally
        {
            sem.Release();
        }
    }

    public void Remove(string runnerId)
    {
        _runners.TryRemove(runnerId, out _);
        if (_sendLocks.TryRemove(runnerId, out var sem))
            sem.Dispose();
    }
}

public sealed record StealthRunnerConnection(
    string RunnerId,
    WebSocket Socket,
    bool Connected,
    DateTimeOffset ConnectedAt,
    DateTimeOffset LastSeenAt,
    string? CurrentTaskId,
    string[] Capabilities);
