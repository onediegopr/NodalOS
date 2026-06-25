using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OneBrain.ChromeLab.Bridge;
using OneBrain.ChromeLab.Bridge.Sessions;
using OneBrain.ChromeLab.Bridge.Stealth;

var options = ChromeLabOptions.Load(args);
if (options.SelfTest)
    return await ChromeLabSelfTest.RunAsync(options);

var startedAt = DateTimeOffset.UtcNow;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(options);
builder.Services.AddSingleton(new BridgeRuntimeState(startedAt));
builder.Services.AddSingleton<ChromeLabRunManager>();
builder.Services.AddSingleton<ChromeLabClientRegistry>();
builder.Services.AddSingleton<PendingToolRequestRegistry>();
builder.Services.AddSingleton<ProtocolEventBuffer>();
builder.Services.AddHttpClient<OpenAiAgentClient>();

builder.Services.AddSingleton<UnifiedFrictionPolicyEngine>();
builder.Services.AddSingleton<StealthTaskManager>();
builder.Services.AddSingleton<StealthRunnerRegistry>();
builder.Services.AddSingleton<StealthHandoffGateway>();
builder.Services.AddSingleton<HandoffVerificationService>();
builder.Services.AddSingleton<ExtensionMessageHandler>();
builder.Services.AddCors(cors =>
{
    cors.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                origin.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                (options.AllowLan && (
                    origin.StartsWith("http://192.168.", StringComparison.OrdinalIgnoreCase) ||
                    origin.StartsWith("http://10.", StringComparison.OrdinalIgnoreCase))))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.WebHost.UseUrls($"http://{options.Host}:{options.Port}");

var app = builder.Build();
app.UseCors();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });

app.MapGet("/health", (
    ChromeLabClientRegistry clients,
    StealthRunnerRegistry stealthRunners,
    StealthTaskManager stealthTasks,
    ChromeLabOptions config) =>
{
    var runnersConnected = stealthRunners.HasConnectedRunner ? 1 : 0;
    return new
    {
        status = "healthy",
        service = ChromeLabProtocol.ServiceName,
        version = ChromeLabProtocol.EngineVersion,
        timestamp = DateTimeOffset.UtcNow,
        runnersConnected,
        stealthEnabled = config.StealthEnabled,
        activeTasks = stealthTasks.Snapshot().Count(t => t.Status == "running"),
    };
});

app.MapGet("/metrics", async (
    ChromeLabRunManager runs,
    StealthTaskManager stealthTasks,
    ChromeLabClientRegistry clients,
    StealthRunnerRegistry stealthRunners,
    ProtocolEventBuffer events) =>
{
    var sb = new StringBuilder();
    sb.AppendLine("# HELP nodalos_tasks_total Total stealth tasks created");
    sb.AppendLine("# TYPE nodalos_tasks_total counter");
    sb.AppendLine($"nodalos_tasks_total {stealthTasks.Snapshot().Count}");
    sb.AppendLine("# HELP nodalos_tasks_active Currently active stealth tasks");
    sb.AppendLine("# TYPE nodalos_tasks_active gauge");
    sb.AppendLine($"nodalos_tasks_active {stealthTasks.Snapshot().Count(t => t.Status == "running")}");
    sb.AppendLine("# HELP nodalos_runners_connected Connected stealth runners");
    sb.AppendLine("# TYPE nodalos_runners_connected gauge");
    sb.AppendLine($"nodalos_runners_connected {(stealthRunners.HasConnectedRunner ? 1 : 0)}");
    sb.AppendLine("# HELP nodalos_companion_runs_total Companion runs created");
    sb.AppendLine("# TYPE nodalos_companion_runs_total counter");
    sb.AppendLine($"nodalos_companion_runs_total {runs.Snapshot().Count}");
    return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
});

app.MapGet("/config/public", (ChromeLabOptions config) => new PublicConfigResponse(
    ChromeLabProtocol.ServiceName,
    ChromeLabProtocol.Version,
    "openai",
    config.Model,
    config.HasApiKey,
    config.RequiresToken));

app.MapGet("/pairing/local-token", (HttpContext context, ChromeLabOptions config) =>
{
    var remote = context.Connection.RemoteIpAddress;
    if (remote is null || !IPAddress.IsLoopback(remote))
        return Results.NotFound();

    return Results.Ok(new
    {
        ok = true,
        token = config.ConnectionToken,
        source = "loopback-local-pairing"
    });
});

app.MapGet("/runtime", (ChromeLabOptions config, ProtocolEventBuffer events, BridgeRuntimeState runtime) => new RuntimeResponse(
    true,
    (DateTimeOffset.UtcNow - runtime.StartedAt).TotalSeconds,
    config.HasApiKey,
    "OpenAI",
    config.Model,
    config.Host,
    config.Port,
    events.LastError));

app.MapGet("/clients", (ChromeLabClientRegistry clients) => clients.Diagnostics());

app.MapGet("/last-events", (ProtocolEventBuffer events) => events.Snapshot());

app.MapGet("/debug", (
    ChromeLabOptions config,
    ChromeLabClientRegistry clients,
    ChromeLabRunManager runs,
    PendingToolRequestRegistry pending,
    ProtocolEventBuffer events,
    BridgeRuntimeState runtime) => new
{
    runtime = new RuntimeResponse(
        true,
        (DateTimeOffset.UtcNow - runtime.StartedAt).TotalSeconds,
        config.HasApiKey,
        "OpenAI",
        config.Model,
        config.Host,
        config.Port,
        events.LastError),
    clients = clients.Diagnostics(),
    runs = runs.Snapshot().Select(run => new
    {
        run.RunId,
        run.Status,
        run.Message,
        run.StopRequested,
        run.PausedReason
    }),
    pending = pending.Snapshot().Select(item => new
    {
        requestId = item.Key,
        item.Value.RunId,
        item.Value.Tool
    }),
    events = events.Snapshot()
});

app.MapPost("/api/runs", async (
    StartRunRequest request,
    ChromeLabRunManager runs,
    ChromeLabClientRegistry clients,
    PendingToolRequestRegistry pending,
    OpenAiAgentClient agent,
    ChromeLabOptions config,
    StealthTaskManager stealthRuns,
    StealthRunnerRegistry stealthRunners,
    ProtocolEventBuffer events,
    CancellationToken cancellationToken) =>
{
    if (string.Equals(request.Mode, "stealth", StringComparison.OrdinalIgnoreCase))
    {
        return await HandleStealthRunStart(
            request, stealthRuns, stealthRunners, agent, config, events, cancellationToken);
    }

    if (string.IsNullOrWhiteSpace(request.Instruction))
        return Results.BadRequest(new RunResponse("", "error", "instruction is required"));

    if (!clients.HasConnectedClients)
    {
        return Results.Conflict(new ErrorResponse(
            false,
            "no_extension_client",
            "No extension client connected. Open NODAL OS side panel or connect the extension.",
            "/debug"));
    }

    var run = runs.Start(request.Instruction);
    if (!agent.HasApiKey)
    {
        runs.Stop(run.RunId, "OpenAI API key missing");
        return Results.Ok(new RunResponse(run.RunId, "error", "OpenAI API key missing. Set OPENAI_API_KEY, config/chrome-lab.local.json or ApiKey.txt."));
    }

    var firstUrl = ExtractFirstHttpUrl(request.Instruction);
    var firstTool = firstUrl == null ? "observePage" : "navigate";
    var firstArgs = firstUrl == null
        ? new Dictionary<string, object?>()
        : new Dictionary<string, object?> { ["url"] = firstUrl };
    var firstRequestId = Guid.NewGuid().ToString("n");
    var observe = new ToolRequest(
        "tool.request",
        run.RunId,
        firstRequestId,
        firstTool,
        firstArgs);
    pending.Track(firstRequestId, run.RunId, firstTool);
    clients.Diagnostics().Clients
        .Where(client => client.Connected)
        .ToList()
        .ForEach(client => clients.MarkRun(client.ClientId, run.RunId, firstRequestId));
    await clients.BroadcastAsync(observe, cancellationToken);
    return Results.Ok(new RunResponse(run.RunId, "running", $"Run started; {firstTool} requested."));
});

app.MapPost("/api/runs/{runId}/stop", async (
    string runId,
    ChromeLabRunManager runs,
    ChromeLabClientRegistry clients,
    CancellationToken cancellationToken) =>
{
    var run = runs.Stop(runId);
    await clients.BroadcastAsync(new { type = "run.stop", runId, reason = "userStop" }, cancellationToken);
    return Results.Ok(new RunResponse(run.RunId, run.Status, run.Message));
});

app.MapPost("/api/runs/{runId}/pause", async (
    string runId,
    ChromeLabRunManager runs,
    ChromeLabClientRegistry clients,
    CancellationToken cancellationToken) =>
{
    var run = runs.Pause(runId);
    await clients.BroadcastAsync(new { type = "run.pause", runId, reason = "humanInterventionRequired", message = "Paused by bridge." }, cancellationToken);
    return Results.Ok(new RunResponse(run.RunId, run.Status, run.Message));
});

app.MapPost("/api/runs/{runId}/resume", async (
    string runId,
    ChromeLabRunManager runs,
    ChromeLabClientRegistry clients,
    PendingToolRequestRegistry pending,
    CancellationToken cancellationToken) =>
{
    var run = runs.Resume(runId);
    await clients.BroadcastAsync(new { type = "run.resume", runId }, cancellationToken);
    if (clients.HasConnectedClients)
        await BroadcastObserveRequestAsync(clients, pending, run.RunId, cancellationToken);
    return Results.Ok(new RunResponse(run.RunId, run.Status, run.Message));
});

app.Map("/ws/extension", async (
    HttpContext context,
    ChromeLabClientRegistry clients,
    ExtensionMessageHandler handler,
    ProtocolEventBuffer events) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = clients.Add(socket);
    var session = new WebSocketSession(clientId, handler, events);
    await session.RunAsync(socket, context.RequestAborted);
    clients.Remove(clientId);
});

app.Map("/ws/stealth", async (
    HttpContext context,
    StealthRunnerRegistry runners,
    StealthTaskManager tasks,
    IUnifiedFrictionPolicyEngine policyEngine,
    StealthHandoffGateway handoffGateway,
    OpenAiAgentClient agent,
    ChromeLabOptions config,
    ProtocolEventBuffer events) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var runnerId = Guid.NewGuid().ToString("n");
    runners.Add(socket, runnerId);
    events.Add("stealth.ws.accepted", "Stealth WebSocket accepted", clientId: runnerId);

    try
    {
        while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
        {
            var received = await ReceiveTextMessageAsync(socket, context.RequestAborted);
            if (received == null)
                break;

            try
            {
                await HandleStealthMessage(received, socket, runnerId,
                    runners, tasks, policyEngine, handoffGateway, agent, config, events,
                    context.RequestAborted);
            }
            catch (JsonException)
            {
                events.Add("stealth.protocol.error", "Malformed JSON ignored", clientId: runnerId);
                await SendAsync(socket, new
                {
                    type = "protocol.error",
                    error = "malformed_json",
                    message = "Malformed JSON ignored."
                }, context.RequestAborted);
            }
            catch (Exception ex)
            {
                events.Add("stealth.protocol.error", ex.Message, clientId: runnerId);
                await SendAsync(socket, new
                {
                    type = "protocol.error",
                    error = "message_failed",
                    message = ex.Message
                }, context.RequestAborted);
            }
        }
    }
    finally
    {
        runners.Remove(runnerId);
        events.Add("stealth.ws.closed", "Stealth WebSocket closed", clientId: runnerId);
    }
});

Console.WriteLine($"{ChromeLabProtocol.ServiceName} listening on http://{options.Host}:{options.Port}");
if (options.StealthEnabled)
    Console.WriteLine($"Stealth Engine endpoint: ws://{options.Host}:{options.Port}/ws/stealth");
foreach (var ip in options.GetLocalIpAddresses())
    Console.WriteLine($"LAN candidate: http://{ip}:{options.Port}");
Console.WriteLine(options.HasApiKey ? "OpenAI key loaded: yes" : "OpenAI key loaded: no");
if (options.ConnectionTokenGenerated)
{
    Console.WriteLine("NODAL OS bridge started.");
    Console.WriteLine("Extension token generated and saved in config/chrome-lab.local.json.");
    Console.WriteLine("Open NODAL OS Runtime and paste this token once:");
    Console.WriteLine();
    Console.WriteLine(options.ConnectionToken);
    Console.WriteLine();
    Console.WriteLine("The extension will save it locally and will not ask again.");
}
else
{
    Console.WriteLine($"NODAL OS Extension Token: loaded from {TokenSourceLabel(options.ConnectionTokenSource)} ({MaskToken(options.ConnectionToken)})");
}
if (!options.AllowLan && !string.Equals(options.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
    Console.WriteLine("LAN disabled; use --allow-lan explicitly to bind outside loopback.");

await app.RunAsync();
return 0;

static async Task SendAsync(WebSocket socket, object message, CancellationToken cancellationToken)
{
    var payload = JsonSerializer.Serialize(message, ChromeLabProtocol.JsonOptions);
    var bytes = Encoding.UTF8.GetBytes(payload);

    var key = socket.GetHashCode().ToString("x", CultureInfo.InvariantCulture);
    var sem = SendLockPool.GetOrCreate(key);

    await sem.WaitAsync(cancellationToken);
    try { await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken); }
    finally { sem.Release(); }
}

static async Task<string?> ReceiveTextMessageAsync(WebSocket socket, CancellationToken cancellationToken)
{
    var buffer = new byte[16 * 1024];
    using var stream = new MemoryStream();
    while (true)
    {
        var result = await socket.ReceiveAsync(buffer, cancellationToken);
        if (result.MessageType == WebSocketMessageType.Close) return null;
        if (result.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("Only text WebSocket messages are supported.");
        stream.Write(buffer, 0, result.Count);
        if (result.EndOfMessage) break;
    }
    return Encoding.UTF8.GetString(stream.ToArray());
}

static string? ExtractFirstHttpUrl(string text)
{
    var match = Regex.Match(text ?? "", @"https?://[^\s""'<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    if (!match.Success) return null;
    var url = match.Value.TrimEnd('.', ',', ';', ')', ']');
    return UrlValidator.IsAllowedNavigationUrl(url) ? url : null;
}

static async Task BroadcastObserveRequestAsync(ChromeLabClientRegistry clients, PendingToolRequestRegistry pending, string runId, CancellationToken ct)
{
    var id = Guid.NewGuid().ToString("n");
    pending.Track(id, runId, "observePage");
    await clients.BroadcastAsync(new ToolRequest("tool.request", runId, id, "observePage", new Dictionary<string, object?>()), ct);
}

static string MaskToken(string token) =>
    string.IsNullOrWhiteSpace(token) ? "<missing>" : token.Length <= 8 ? "********" : $"{token[..4]}...{token[^4..]}";

static string TokenSourceLabel(string source)
{
    if (string.IsNullOrWhiteSpace(source)) return "runtime configuration";
    if (source.Contains("chrome-lab.local.json", StringComparison.OrdinalIgnoreCase)) return "config/chrome-lab.local.json";
    if (string.Equals(source, "environment", StringComparison.OrdinalIgnoreCase)) return ChromeLabOptions.CurrentConnectionTokenEnvironmentVariable;
    return source;
}

static async Task<IResult> HandleStealthRunStart(
    StartRunRequest request,
    StealthTaskManager tasks,
    StealthRunnerRegistry runners,
    OpenAiAgentClient agent,
    ChromeLabOptions config,
    ProtocolEventBuffer events,
    CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(request.Instruction))
        return Results.BadRequest(new RunResponse("", "error", "instruction is required"));

    if (!runners.HasConnectedRunner)
        return Results.Conflict(new ErrorResponse(false, "no_stealth_runner",
            "No stealth runner connected. Start the Stealth Engine first.", "/debug"));

    if (!agent.HasApiKey)
        return Results.Problem("OpenAI API key missing for stealth mode.");

    var task = tasks.Start(request.Instruction);
    var firstUrl = ExtractFirstHttpUrl(request.Instruction);

    events.Add("stealth.task.created", $"Stealth task {task.TaskId} created", runId: task.TaskId);

    var stealthProfile = new StealthTaskProfile(
        null, config.StealthFingerprintProfile, "desktop", "Windows", null, null, null);

    await runners.BroadcastAsync(new StealthTaskMessage(
        StealthProtocol.MessageTypeStealthTask,
        task.TaskId,
        request.Instruction,
        firstUrl,
        stealthProfile,
        null,
        config.StealthMaxRetries,
        true), ct);

    return Results.Ok(new RunResponse(task.TaskId, "running", $"Stealth task started."));
}

static async Task HandleStealthMessage(
    string json,
    WebSocket socket,
    string runnerId,
    StealthRunnerRegistry runners,
    StealthTaskManager tasks,
    IUnifiedFrictionPolicyEngine policyEngine,
    StealthHandoffGateway handoffGateway,
    OpenAiAgentClient agent,
    ChromeLabOptions config,
    ProtocolEventBuffer events,
    CancellationToken ct)
{
    using var doc = JsonDocument.Parse(json);
    var type = doc.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

    runners.MarkSeen(runnerId);

    if (type == StealthProtocol.MessageTypeStealthHello)
    {
        var caps = doc.RootElement.TryGetProperty("capabilities", out var capsProp)
            && capsProp.ValueKind == JsonValueKind.Array
            ? capsProp.EnumerateArray().Select(e => e.GetString() ?? "").ToArray()
            : [];

        runners.MarkConnected(runnerId, caps);
        events.Add("stealth.hello", $"Stealth runner {runnerId} connected", clientId: runnerId);

        await SendAsync(socket, new
        {
            type = StealthProtocol.MessageTypeStealthAck,
            runnerId,
            serverTime = DateTimeOffset.UtcNow
        }, ct);
        return;
    }

    if (type == StealthProtocol.MessageTypeFrictionSignal)
    {
        if (!doc.RootElement.TryGetProperty("taskId", out var taskIdProp))
            return;

        var taskId = taskIdProp.GetString() ?? "";
        var signalJson = doc.RootElement.TryGetProperty("signal", out var sigProp)
            && sigProp.ValueKind == JsonValueKind.Object
            ? sigProp
            : default;

        var task = tasks.Get(taskId);
        var currentRetry = task?.CurrentRetryCount ?? 0;

        var signal = FrictionSignalRouter.FromStealthMessage(taskId, signalJson);
        events.Add("friction.signal", $"Stealth friction: {signal.Kind} severity={signal.Severity}",
            runId: taskId);

        var decision = await policyEngine.EvaluateAsync(signal, "stealth", currentRetry, ct);
        events.Add("policy.decision", $"Policy decision: {decision.Decision} for {signal.Kind}",
            runId: taskId);

        if (decision.RequiresHuman)
        {
            await handoffGateway.ActivateAsync(decision, taskId, ct);
            tasks.Pause(taskId, "handoff");
            return;
        }

        if (decision.BlocksAutomation)
        {
            tasks.Error(taskId, decision.Message);
            await SendAsync(socket, new
            {
                type = "run.stop",
                runId = taskId,
                reason = decision.Message
            }, ct);
            return;
        }

        if (decision.RequiresStealthAction)
        {
            tasks.IncrementRetry(taskId);
        }

        var decisionMsg = new StealthFrictionDecisionMessage(
            Type: StealthProtocol.MessageTypeFrictionDecision,
            TaskId: taskId,
            SignalId: signal.SignalId,
            Decision: decision.Decision.ToString(),
            SolverProvider: decision.SolverProvider,
            Sitekey: signal.Sitekey,
            RetryAttempt: decision.RetryAttempt ?? 0,
            MaxRetries: decision.MaxRetries ?? config.StealthMaxRetries,
            CooldownMs: decision.CooldownMs,
            Message: decision.Message,
            RotateProxy: decision.RotateProxy,
            RotateProfile: decision.RotateProfile);

        await SendAsync(socket, decisionMsg, ct);
        return;
    }

    if (type == StealthProtocol.MessageTypeFrictionSolved)
    {
        var taskId = doc.RootElement.TryGetProperty("taskId", out var tidProp) ? tidProp.GetString() ?? "" : "";
        var success = doc.RootElement.TryGetProperty("success", out var succProp)
            && succProp.ValueKind == JsonValueKind.True;

        if (success)
        {
            events.Add("captcha.solved", "CAPTCHA solved successfully", runId: taskId);
            var obsReqId = Guid.NewGuid().ToString("n");
            await runners.BroadcastAsync(new ToolRequest(
                "tool.request", taskId, obsReqId, "observePage",
                new Dictionary<string, object?>()), ct);
        }
        else
        {
            events.Add("captcha.solved.failed", "CAPTCHA solve failed", runId: taskId);

            var task = tasks.Get(taskId);
            var currentRetry = task?.CurrentRetryCount ?? 0;

            var signalJson = doc.RootElement;
            var signal = FrictionSignalRouter.FromStealthMessage(taskId, signalJson);
            var decision = await policyEngine.EvaluateAsync(signal, "stealth", currentRetry + 1, ct);

            if (decision.RequiresHuman)
            {
                await handoffGateway.ActivateAsync(decision, taskId, ct);
                tasks.Pause(taskId, "handoff");
            }
            else
            {
                await SendAsync(socket, new StealthFrictionDecisionMessage(
                    StealthProtocol.MessageTypeFrictionDecision, taskId, signal.SignalId,
                    decision.Decision.ToString(), decision.SolverProvider, signal.Sitekey,
                    decision.RetryAttempt ?? 0, decision.MaxRetries ?? config.StealthMaxRetries,
                    decision.CooldownMs, decision.Message, decision.RotateProxy, decision.RotateProfile), ct);
            }
        }
        return;
    }

    if (type == StealthProtocol.MessageTypeHandoffCompleted)
    {
        var taskId = doc.RootElement.TryGetProperty("taskId", out var hcProp) ? hcProp.GetString() ?? "" : "";
        var hSuccess = doc.RootElement.TryGetProperty("success", out var hsuccProp)
            && hsuccProp.ValueKind == JsonValueKind.True;

        events.Add("handoff.completed", $"Handoff completed: success={hSuccess}", runId: taskId);

        if (hSuccess)
        {
            var verified = await handoffGateway.VerifyAndResumeAsync(taskId, ct);
            events.Add("handoff.verified", $"Handoff verified: {verified}", runId: taskId);
        }

        tasks.Resume(taskId);
        var obsReqId2 = Guid.NewGuid().ToString("n");
        await runners.BroadcastAsync(new ToolRequest(
            "tool.request", taskId, obsReqId2, "observePage",
            new Dictionary<string, object?>()), ct);
        return;
    }

    if (type == "stealth.result")
    {
        var taskId = doc.RootElement.TryGetProperty("taskId", out var srProp) ? srProp.GetString() ?? "" : "";
        var toolSuccess = doc.RootElement.TryGetProperty("success", out var tsProp)
            && tsProp.ValueKind == JsonValueKind.True;
        var resultProp = doc.RootElement.TryGetProperty("result", out var resProp) ? resProp : default;

        events.Add("stealth.result", $"Stealth result: success={toolSuccess}", runId: taskId);

        if (toolSuccess && resultProp.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var decision = await agent.CreateToolDecisionAsync(
                    "Stealth task: " + (tasks.Get(taskId)?.Instruction ?? ""),
                    resultProp, ct);

                if (string.Equals(decision.Tool, "stop", StringComparison.Ordinal))
                {
                    tasks.Complete(taskId, decision.Reason);
                    await SendAsync(socket, new { type = "run.stop", runId = taskId, reason = decision.Reason }, ct);
                    return;
                }

                if (string.Equals(decision.Tool, "pauseForHuman", StringComparison.Ordinal))
                {
                    tasks.Pause(taskId, decision.Reason);
                    await SendAsync(socket, new { type = "run.pause", runId = taskId, reason = "agentPause", message = decision.Reason }, ct);
                    return;
                }

                var toolRequest = new ToolRequest("tool.request", taskId, Guid.NewGuid().ToString("n"), decision.Tool, decision.Args);
                await SendAsync(socket, toolRequest, ct);
                await SendAsync(socket, new
                {
                    type = "run.status", runId = taskId, status = "running",
                    message = $"Dispatching {decision.Tool}: {decision.Reason}"
                }, ct);
            }
            catch (Exception ex)
            {
                tasks.Error(taskId, ex.Message);
                await SendAsync(socket, new { type = "run.status", runId = taskId, status = "error", message = ex.Message }, ct);
            }
        }
        return;
    }
}

public sealed record BridgeRuntimeState(DateTimeOffset StartedAt);

static class SendLockPool
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    public static SemaphoreSlim GetOrCreate(string key) => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
}
