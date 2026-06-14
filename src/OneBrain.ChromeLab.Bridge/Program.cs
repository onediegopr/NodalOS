using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OneBrain.ChromeLab.Bridge;

var options = ChromeLabOptions.Load(args);
if (options.SelfTest)
    return await ChromeLabSelfTest.RunAsync(options);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<ChromeLabRunManager>();
builder.Services.AddSingleton<ChromeLabClientRegistry>();
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

    if (!config.HasApiKey)
    {
        runs.Stop(run.RunId, "OpenAI API key missing");
        return Results.Ok(new RunResponse(run.RunId, "error", "OpenAI API key missing. Set OPENAI_API_KEY or config/chrome-lab.local.json."));
    }

    var observe = new ToolRequest(
        "tool.request",
        run.RunId,
        Guid.NewGuid().ToString("n"),
        "observePage",
        new Dictionary<string, object?>());
    await clients.BroadcastAsync(observe, cancellationToken);
    return Results.Ok(new RunResponse(run.RunId, "running", "Run started; observePage requested."));
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
    CancellationToken cancellationToken) =>
{
    var run = runs.Resume(runId);
    await clients.BroadcastAsync(new { type = "run.resume", runId }, cancellationToken);
    return Results.Ok(new RunResponse(run.RunId, run.Status, run.Message));
});

app.Map("/ws/extension", async (
    HttpContext context,
    ChromeLabClientRegistry clients,
    ChromeLabRunManager runs) =>
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
            await HandleExtensionMessage(json, socket, runs, context.RequestAborted);
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
        doc.RootElement.TryGetProperty("result", out var resultProperty))
    {
        var runId = runIdProperty.GetString() ?? "";
        if (resultProperty.TryGetProperty("hasCredentialLike", out var credentialLike) && credentialLike.GetBoolean())
        {
            runs.CredentialRequired(runId, "credentialRequired");
            await SendAsync(socket, new
            {
                type = "run.pause",
                runId,
                reason = "credentialRequired",
                message = "Credential, login, 2FA or captcha detected. Complete manually, then resume."
            }, cancellationToken);
        }
    }
}

static async Task SendAsync(WebSocket socket, object message, CancellationToken cancellationToken)
{
    var payload = JsonSerializer.Serialize(message, ChromeLabProtocol.JsonOptions);
    var bytes = Encoding.UTF8.GetBytes(payload);
    await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
}
