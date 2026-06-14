using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OneBrain.ChromeLab.Bridge;

var options = ChromeLabOptions.Load(args);
if (options.SelfTest)
    return await ChromeLabSelfTest.RunAsync(options);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<ChromeLabRunManager>();
builder.Services.AddSingleton<ChromeLabClientRegistry>();
builder.Services.AddSingleton<PendingToolRequestRegistry>();
builder.Services.AddHttpClient<OpenAiAgentClient>();
builder.Services.AddCors(cors =>
{
    cors.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                origin.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("http://192.168.", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("http://10.", StringComparison.OrdinalIgnoreCase))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.WebHost.UseUrls($"http://{options.Host}:{options.Port}");

var app = builder.Build();
app.UseCors();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });

app.MapGet("/health", () => new HealthResponse(true, ChromeLabProtocol.ServiceName, ChromeLabProtocol.EngineVersion));

app.MapGet("/config/public", (ChromeLabOptions config) => new PublicConfigResponse(
    ChromeLabProtocol.ServiceName,
    ChromeLabProtocol.Version,
    "openai",
    config.Model,
    config.HasApiKey));

app.MapPost("/api/runs", async (
    StartRunRequest request,
    ChromeLabRunManager runs,
    ChromeLabClientRegistry clients,
    PendingToolRequestRegistry pending,
    OpenAiAgentClient agent,
    ChromeLabOptions config,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Instruction))
        return Results.BadRequest(new RunResponse("", "error", "instruction is required"));

    var run = runs.Start(request.Instruction);
    if (!clients.HasConnectedClients)
    {
        runs.Stop(run.RunId, "no extension connected");
        return Results.Ok(new RunResponse(run.RunId, "error", "No extension client connected."));
    }

    if (!agent.HasApiKey)
    {
        runs.Stop(run.RunId, "OpenAI API key missing");
        return Results.Ok(new RunResponse(run.RunId, "error", "OpenAI API key missing. Set OPENAI_API_KEY or config/chrome-lab.local.json."));
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
    ChromeLabRunManager runs,
    PendingToolRequestRegistry pending,
    OpenAiAgentClient agent) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = clients.Add(socket);
    await clients.BroadcastAsync(new
    {
        type = "engine.hello",
        protocolVersion = ChromeLabProtocol.Version,
        engineVersion = ChromeLabProtocol.EngineVersion
    }, context.RequestAborted);

    var buffer = new byte[64 * 1024];
    try
    {
        while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, context.RequestAborted);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await HandleExtensionMessage(json, socket, runs, clients, pending, agent, context.RequestAborted);
        }
    }
    finally
    {
        clients.Remove(clientId);
    }
});

Console.WriteLine($"{ChromeLabProtocol.ServiceName} listening on http://{options.Host}:{options.Port}");
foreach (var ip in options.GetLocalIpAddresses())
    Console.WriteLine($"LAN candidate: http://{ip}:{options.Port}");
Console.WriteLine(options.HasApiKey ? "OpenAI key loaded: yes" : "OpenAI key loaded: no");

await app.RunAsync();
return 0;

static async Task HandleExtensionMessage(
    string json,
    WebSocket socket,
    ChromeLabRunManager runs,
    ChromeLabClientRegistry clients,
    PendingToolRequestRegistry pending,
    OpenAiAgentClient agent,
    CancellationToken cancellationToken)
{
    using var doc = JsonDocument.Parse(json);
    var type = doc.RootElement.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() ?? "" : "";
    if (type == "extension.hello")
    {
        await SendAsync(socket, new
        {
            type = "engine.hello",
            protocolVersion = ChromeLabProtocol.Version,
            engineVersion = ChromeLabProtocol.EngineVersion
        }, cancellationToken);
        return;
    }

    if (type == "tool.result" &&
        doc.RootElement.TryGetProperty("runId", out var runIdProperty) &&
        doc.RootElement.TryGetProperty("requestId", out var requestIdProperty) &&
        doc.RootElement.TryGetProperty("success", out var successProperty) &&
        doc.RootElement.TryGetProperty("result", out var resultProperty))
    {
        var runId = runIdProperty.GetString() ?? "";
        var requestId = requestIdProperty.GetString() ?? "";
        var success = successProperty.ValueKind is JsonValueKind.True or JsonValueKind.False && successProperty.GetBoolean();
        var completed = pending.Complete(requestId);
        var run = runs.Get(runId);
        if (run == null)
            return;

        if (!success)
        {
            var error = doc.RootElement.TryGetProperty("error", out var errorProperty) ? errorProperty.GetString() ?? "tool failed" : "tool failed";
            runs.Stop(runId, error);
            await SendAsync(socket, new
            {
                type = "run.status",
                runId,
                status = "error",
                message = error
            }, cancellationToken);
            return;
        }

        if (completed?.Tool == "navigate")
        {
            await BroadcastObserveRequestAsync(clients, pending, runId, cancellationToken);
            return;
        }

        if (resultProperty.ValueKind == JsonValueKind.Object &&
            ShouldPauseForCredentialEntry(resultProperty))
        {
            runs.CredentialRequired(runId, "credentialRequired");
            await SendAsync(socket, new
            {
                type = "run.pause",
                runId,
                reason = "credentialRequired",
                message = "Credential, login, 2FA or captcha detected. Complete manually, then resume."
            }, cancellationToken);
            return;
        }

        if (!string.Equals(completed?.Tool, "observePage", StringComparison.Ordinal))
        {
            await BroadcastObserveRequestAsync(clients, pending, runId, cancellationToken);
            return;
        }

        if (run.StopRequested || string.Equals(run.Status, "paused", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var decision = TryCreateDeterministicDecision(run.Instruction, resultProperty) ??
                           await agent.CreateToolDecisionAsync(run.Instruction, resultProperty, cancellationToken);
            var validation = ChromeLabToolPolicy.Validate(decision.Tool, decision.Args);
            if (!validation.Allowed)
                throw new InvalidOperationException($"Tool decision rejected: {validation.Reason}");

            if (string.Equals(decision.Tool, "pauseForHuman", StringComparison.Ordinal))
            {
                runs.Pause(runId, decision.Reason.Length == 0 ? "humanInterventionRequired" : decision.Reason);
                await SendAsync(socket, new
                {
                    type = "run.pause",
                    runId,
                    reason = "humanInterventionRequired",
                    message = string.IsNullOrWhiteSpace(decision.Reason) ? "Manual intervention required." : decision.Reason
                }, cancellationToken);
                return;
            }

            if (string.Equals(decision.Tool, "stop", StringComparison.Ordinal))
            {
                runs.Stop(runId, decision.Reason.Length == 0 ? "agentStop" : decision.Reason);
                await SendAsync(socket, new
                {
                    type = "run.stop",
                    runId,
                    reason = string.IsNullOrWhiteSpace(decision.Reason) ? "agentStop" : decision.Reason
                }, cancellationToken);
                return;
            }

            var nextRequestId = Guid.NewGuid().ToString("n");
            pending.Track(nextRequestId, runId, decision.Tool);
            await clients.BroadcastAsync(new ToolRequest(
                "tool.request",
                runId,
                nextRequestId,
                decision.Tool,
                decision.Args), cancellationToken);
            await SendAsync(socket, new
            {
                type = "run.status",
                runId,
                status = "running",
                message = string.IsNullOrWhiteSpace(decision.Reason)
                    ? $"Dispatching {decision.Tool}."
                    : $"{decision.Tool}: {decision.Reason}"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            runs.Stop(runId, ex.Message);
            await SendAsync(socket, new
            {
                type = "run.status",
                runId,
                status = "error",
                message = ex.Message
            }, cancellationToken);
        }
    }
}

static AgentToolDecision? TryCreateDeterministicDecision(string instruction, JsonElement observation)
{
    var normalizedInstruction = NormalizeForMatch(instruction);
    if (!normalizedInstruction.Contains("iniciar sesion", StringComparison.Ordinal) &&
        !normalizedInstruction.Contains("login", StringComparison.Ordinal) &&
        !normalizedInstruction.Contains("clave fiscal", StringComparison.Ordinal) &&
        !normalizedInstruction.Contains("acceso", StringComparison.Ordinal) &&
        !normalizedInstruction.Contains("ingresar", StringComparison.Ordinal) &&
        !normalizedInstruction.Contains("autenticar", StringComparison.Ordinal) &&
        !normalizedInstruction.Contains("entrar", StringComparison.Ordinal) &&
        !normalizedInstruction.Contains("identificarse", StringComparison.Ordinal))
    {
        return null;
    }

    if (ShouldPauseForCredentialEntry(observation))
        return null;

    var candidate = FindClickableCandidate(observation, "buttons") ?? FindClickableCandidate(observation, "links");
    if (candidate == null)
        return null;

    return new AgentToolDecision(
        "click",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["selector"] = candidate.Value.selector
        },
        $"Open login entry: {candidate.Value.text}");
}

static bool ShouldPauseForCredentialEntry(JsonElement result)
{
    return ReadBool(result, "hasCredentialEntry") ||
           ReadBool(result, "hasPasswordField") ||
           ReadBool(result, "hasCaptchaLike") ||
           ReadBool(result, "hasTwoFactorLike");
}

static bool ReadBool(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var property) &&
           property.ValueKind is JsonValueKind.True or JsonValueKind.False &&
           property.GetBoolean();
}

static (string selector, string text)? FindClickableCandidate(JsonElement observation, string propertyName)
{
    if (!observation.TryGetProperty(propertyName, out var items) || items.ValueKind != JsonValueKind.Array)
        return null;

    foreach (var item in items.EnumerateArray())
    {
        var text = item.TryGetProperty("text", out var textProperty) ? textProperty.GetString() ?? "" : "";
        var selector = item.TryGetProperty("selector", out var selectorProperty) ? selectorProperty.GetString() ?? "" : "";
        var normalized = NormalizeForMatch(text);
        if (string.IsNullOrWhiteSpace(selector))
            continue;

        if (normalized.Contains("iniciar sesion", StringComparison.Ordinal) ||
            normalized.Contains("login", StringComparison.Ordinal) ||
            normalized.Contains("ingresar", StringComparison.Ordinal) ||
            normalized.Contains("acceso", StringComparison.Ordinal) ||
            normalized.Contains("entrar", StringComparison.Ordinal) ||
            normalized.Contains("autenticar", StringComparison.Ordinal) ||
            normalized.Contains("identificarse", StringComparison.Ordinal) ||
            normalized.Contains("clave fiscal", StringComparison.Ordinal) ||
            normalized.Contains("mi afip", StringComparison.Ordinal))
        {
            return (selector, text);
        }
    }

    return null;
}

static string NormalizeForMatch(string value)
{
    return value
        .ToLowerInvariant()
        .Normalize(NormalizationForm.FormD)
        .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
        .Aggregate(new StringBuilder(), (builder, ch) => builder.Append(ch))
        .ToString();
}

static async Task BroadcastObserveRequestAsync(
    ChromeLabClientRegistry clients,
    PendingToolRequestRegistry pending,
    string runId,
    CancellationToken cancellationToken)
{
    var observeRequestId = Guid.NewGuid().ToString("n");
    pending.Track(observeRequestId, runId, "observePage");
    await clients.BroadcastAsync(new ToolRequest(
        "tool.request",
        runId,
        observeRequestId,
        "observePage",
        new Dictionary<string, object?>()), cancellationToken);
}

static string? ExtractFirstHttpUrl(string text)
{
    var match = Regex.Match(text ?? "", @"https?://[^\s""'<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    if (!match.Success)
        return null;

    var url = match.Value.TrimEnd('.', ',', ';', ')', ']');
    return UrlValidator.IsAllowedNavigationUrl(url) ? url : null;
}

static async Task SendAsync(WebSocket socket, object message, CancellationToken cancellationToken)
{
    var payload = JsonSerializer.Serialize(message, ChromeLabProtocol.JsonOptions);
    var bytes = Encoding.UTF8.GetBytes(payload);
    await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
}
