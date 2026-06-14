using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge;

public sealed class OpenAiAgentClient
{
    public const string Provider = "OpenAI";

    public const string SystemPrompt = """
        Eres ONE BRAIN Chrome Lab Agent.
        Controlas una pestana de Chrome mediante tools declaradas.
        No puedes ejecutar JavaScript arbitrario.
        No puedes pedir ni escribir credenciales.
        Si detectas login, password, 2FA, captcha, clave fiscal, token, banco, pago o datos sensibles, debes pedir pausa humana.
        Debes usar pasos pequenos:
        1 observe
        2 decide
        3 act
        4 verify
        Si no estas seguro, observa de nuevo o pausa.
        STOP tiene prioridad absoluta.
        No hagas submit de formularios sensibles sin instruccion humana explicita posterior.
        Responde solo JSON compacto con esta forma:
        {"tool":"toolName","args":{},"reason":"short reason"}
        """;

    private readonly HttpClient _httpClient;
    private readonly ChromeLabOptions _options;

    public OpenAiAgentClient(HttpClient httpClient, ChromeLabOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public bool HasApiKey => _options.HasApiKey;

    public async Task<AgentToolDecision> CreateToolDecisionAsync(string instruction, JsonElement observation, CancellationToken cancellationToken)
    {
        if (!_options.HasApiKey)
            throw new InvalidOperationException("OpenAI API key missing. Set OPENAI_API_KEY or config/chrome-lab.local.json.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        var body = new
        {
            model = _options.Model,
            input = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        instruction,
                        allowedTools = new[]
                        {
                            "observePage",
                            "getCurrentTab",
                            "navigate",
                            "query",
                            "read",
                            "click",
                            "setValue",
                            "selectOption",
                            "scrollIntoView",
                            "waitForSelector",
                            "highlight",
                            "clearHighlight",
                            "pauseForHuman",
                            "stop"
                        },
                        observation
                    }, ChromeLabProtocol.JsonOptions)
                }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(body, ChromeLabProtocol.JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI request failed: {(int)response.StatusCode}");

        return ParseDecisionResponse(content);
    }

    public static AgentToolDecision ParseDecisionResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!TryExtractOutputText(doc.RootElement, out var outputText) || string.IsNullOrWhiteSpace(outputText))
            throw new InvalidOperationException("OpenAI response did not contain output_text.");

        return ParseDecisionText(outputText);
    }

    public static AgentToolDecision ParseDecisionText(string text)
    {
        var candidate = text.Trim();
        if (candidate.StartsWith("```", StringComparison.Ordinal))
        {
            candidate = candidate.Trim('`').Trim();
            if (candidate.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                candidate = candidate[4..].Trim();
        }

        using var doc = JsonDocument.Parse(candidate);
        var root = doc.RootElement;
        var tool = root.TryGetProperty("tool", out var toolProperty) ? toolProperty.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(tool))
            throw new InvalidOperationException("OpenAI decision missing tool.");

        var reason = root.TryGetProperty("reason", out var reasonProperty) ? reasonProperty.GetString() ?? "" : "";
        var args = root.TryGetProperty("args", out var argsProperty) && argsProperty.ValueKind == JsonValueKind.Object
            ? ToDictionary(argsProperty)
            : new Dictionary<string, object?>(StringComparer.Ordinal);

        return new AgentToolDecision(tool, args, reason);
    }

    private static bool TryExtractOutputText(JsonElement root, out string outputText)
    {
        if (root.TryGetProperty("output_text", out var directText) &&
            directText.ValueKind == JsonValueKind.String)
        {
            outputText = directText.GetString() ?? "";
            return true;
        }

        if (root.TryGetProperty("output", out var outputArray) &&
            outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputArray.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var contentArray) ||
                    contentArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var content in contentArray.EnumerateArray())
                {
                    if (content.TryGetProperty("type", out var typeProperty) &&
                        string.Equals(typeProperty.GetString(), "output_text", StringComparison.Ordinal) &&
                        content.TryGetProperty("text", out var textProperty) &&
                        textProperty.ValueKind == JsonValueKind.String)
                    {
                        outputText = textProperty.GetString() ?? "";
                        return true;
                    }
                }
            }
        }

        outputText = "";
        return false;
    }

    private static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            result[property.Name] = ToValue(property.Value);

        return result;
    }

    private static object? ToValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ToValue).ToArray(),
            _ => element.GetRawText()
        };
    }
}
