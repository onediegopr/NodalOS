using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OneBrain.ChromeLab.Bridge.Sessions;
using OneBrain.ChromeLab.Bridge.Stealth;

namespace OneBrain.ChromeLab.Bridge.Sessions;

public sealed class ExtensionMessageHandler : IMessageHandler
{
    private readonly ChromeLabRunManager _runs;
    private readonly ChromeLabClientRegistry _clients;
    private readonly PendingToolRequestRegistry _pending;
    private readonly OpenAiAgentClient _agent;
    private readonly ChromeLabOptions _config;
    private readonly ProtocolEventBuffer _events;
    private readonly IUnifiedFrictionPolicyEngine _policyEngine;

    public ExtensionMessageHandler(
        ChromeLabRunManager runs,
        ChromeLabClientRegistry clients,
        PendingToolRequestRegistry pending,
        OpenAiAgentClient agent,
        ChromeLabOptions config,
        ProtocolEventBuffer events,
        IUnifiedFrictionPolicyEngine policyEngine)
    {
        _runs = runs;
        _clients = clients;
        _pending = pending;
        _agent = agent;
        _config = config;
        _events = events;
        _policyEngine = policyEngine;
    }

    public async Task<string?> HandleAsync(string json, string clientId, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

        _clients.MarkSeen(clientId);

        switch (type)
        {
            case "extension.hello":
                return await HandleHello(doc, clientId, ct);

            case "extension.ping":
                _clients.MarkPing(clientId);
                var seq = doc.RootElement.TryGetProperty("seq", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetInt32() : 0;
                _clients.MarkPong(clientId);
                return JsonSerializer.Serialize(new { type = "engine.pong", seq, serverTime = DateTimeOffset.UtcNow }, ChromeLabProtocol.JsonOptions);

            case "tool.result":
                return await HandleToolResult(doc, clientId, ct);

            default:
                return null;
        }
    }

    private async Task<string?> HandleHello(JsonDocument doc, string clientId, CancellationToken ct)
    {
        var token = doc.RootElement.TryGetProperty("token", out var tp) ? tp.GetString() ?? "" : "";

        if (!FixedTimeTokenEquals(token, _config.ConnectionToken))
        {
            _clients.MarkError(clientId, "Invalid connection token");
            _events.Add("auth.rejected",
                $"Invalid token; expectedLen={_config.ConnectionToken.Length} receivedLen={token.Length}",
                clientId: clientId);

            return JsonSerializer.Serialize(new
            {
                type = "protocol.error",
                error = "invalid_token",
                message = "Invalid connection token."
            }, ChromeLabProtocol.JsonOptions);
        }

        var protocolVersion = doc.RootElement.TryGetProperty("protocolVersion", out var pp) ? pp.GetString() ?? "" : "";
        if (!string.Equals(protocolVersion, ChromeLabProtocol.Version, StringComparison.Ordinal))
        {
            _clients.MarkError(clientId, "Protocol version mismatch");
            _events.Add("protocol.rejected", "Protocol version mismatch", clientId: clientId);
            return JsonSerializer.Serialize(new
            {
                type = "protocol.error",
                error = "protocol_version_mismatch",
                message = "Protocol version mismatch."
            }, ChromeLabProtocol.JsonOptions);
        }

        var extClientId = doc.RootElement.TryGetProperty("clientId", out var cp) ? cp.GetString() ?? "" : "";
        var extVersion = doc.RootElement.TryGetProperty("extensionVersion", out var vp) ? vp.GetString() ?? "" : "";
        var browser = doc.RootElement.TryGetProperty("browser", out var bp) ? bp.GetString() ?? "" : "";
        var resumeRunId = doc.RootElement.TryGetProperty("resumeRunId", out var rp) ? rp.GetString() : null;

        _clients.RegisterHello(clientId, protocolVersion, extVersion, browser, resumeRunId);
        _events.Add("extension.hello", $"Extension hello from {browser} {extVersion}", clientId: clientId);

        return JsonSerializer.Serialize(new
        {
            type = "engine.hello",
            protocolVersion = ChromeLabProtocol.Version,
            engineVersion = ChromeLabProtocol.EngineVersion,
            serverTime = DateTimeOffset.UtcNow,
            resync = new { run = resumeRunId == null ? null : _runs.Get(resumeRunId), pendingRequest = (object?)null }
        }, ChromeLabProtocol.JsonOptions);
    }

    private async Task<string?> HandleToolResult(JsonDocument doc, string clientId, CancellationToken ct)
    {
        if (!doc.RootElement.TryGetProperty("runId", out var ridP) ||
            !doc.RootElement.TryGetProperty("requestId", out var reqP) ||
            !doc.RootElement.TryGetProperty("success", out var succP) ||
            !doc.RootElement.TryGetProperty("result", out var resP))
            return null;

        var runId = ridP.GetString() ?? "";
        var requestId = reqP.GetString() ?? "";
        var success = succP.ValueKind is JsonValueKind.True or JsonValueKind.False && succP.GetBoolean();

        _events.Add("tool.result", $"Tool result success={success}", runId, requestId, clientId);
        _clients.MarkRun(clientId, runId, requestId);

        var completed = _pending.Complete(requestId);
        var run = _runs.Get(runId);
        if (run == null) return null;

        if (!success)
        {
            var error = doc.RootElement.TryGetProperty("error", out var ep) ? ep.GetString() ?? "tool failed" : "tool failed";
            _runs.Stop(runId, error);
            return JsonSerializer.Serialize(new { type = "run.status", runId, status = "error", message = error }, ChromeLabProtocol.JsonOptions);
        }

        if (completed?.Tool == "navigate")
        {
            var obsId = Guid.NewGuid().ToString("n");
            _pending.Track(obsId, runId, "observePage");
            return JsonSerializer.Serialize(new ToolRequest("tool.request", runId, obsId, "observePage", new Dictionary<string, object?>()), ChromeLabProtocol.JsonOptions);
        }

        if (resP.ValueKind == JsonValueKind.Object && ShouldPauseForCredentialEntry(resP))
        {
            _runs.CredentialRequired(runId, "credentialRequired");
            return JsonSerializer.Serialize(new { type = "run.pause", runId, reason = "credentialRequired", message = "Credential, login, 2FA or captcha detected." }, ChromeLabProtocol.JsonOptions);
        }

        if (!string.Equals(completed?.Tool, "observePage", StringComparison.Ordinal))
        {
            var obsId2 = Guid.NewGuid().ToString("n");
            _pending.Track(obsId2, runId, "observePage");
            return JsonSerializer.Serialize(new ToolRequest("tool.request", runId, obsId2, "observePage", new Dictionary<string, object?>()), ChromeLabProtocol.JsonOptions);
        }

        if (run.StopRequested || string.Equals(run.Status, "paused", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var decision = await _agent.CreateToolDecisionAsync(run.Instruction, resP, ct);
            if (string.Equals(decision.Tool, "stop", StringComparison.Ordinal))
            {
                _runs.Stop(runId, decision.Reason);
                return JsonSerializer.Serialize(new { type = "run.stop", runId, reason = decision.Reason }, ChromeLabProtocol.JsonOptions);
            }

            if (string.Equals(decision.Tool, "pauseForHuman", StringComparison.Ordinal))
            {
                _runs.Pause(runId, decision.Reason);
                return JsonSerializer.Serialize(new { type = "run.pause", runId, reason = "humanInterventionRequired", message = decision.Reason }, ChromeLabProtocol.JsonOptions);
            }

            var nextId = Guid.NewGuid().ToString("n");
            _pending.Track(nextId, runId, decision.Tool);
            return JsonSerializer.Serialize(new ToolRequest("tool.request", runId, nextId, decision.Tool, decision.Args), ChromeLabProtocol.JsonOptions);
        }
        catch (Exception)
        {
            _runs.Stop(runId, "internal error");
            return JsonSerializer.Serialize(new { type = "run.status", runId, status = "error", message = "An internal error occurred. Check server logs." }, ChromeLabProtocol.JsonOptions);
        }
    }

    private static bool ShouldPauseForCredentialEntry(JsonElement result)
    {
        return ReadBool(result, "hasCredentialEntry") ||
               ReadBool(result, "hasPasswordField") ||
               ReadBool(result, "hasCaptchaLike") ||
               ReadBool(result, "hasTwoFactorLike");
    }

    private static bool ReadBool(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False && p.GetBoolean();

    private static bool FixedTimeTokenEquals(string? received, string? expected)
    {
        if (received is null || expected is null) return false;
        var r = Encoding.UTF8.GetBytes(received);
        var e = Encoding.UTF8.GetBytes(expected);
        if (r.Length != e.Length) return false;
        return CryptographicOperations.FixedTimeEquals(r, e);
    }
}
