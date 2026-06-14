using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge;

public sealed class OpenAiAgentClient
{
    public const string Provider = "OpenAI";

    public const string SystemPrompt = """
        Eres ONE BRAIN Chrome Lab Agent.
        Controlas una pestaña de Chrome mediante tools declaradas.
        No puedes ejecutar JavaScript arbitrario.
        No puedes pedir ni escribir credenciales.
        Si detectas login, password, 2FA, captcha, clave fiscal, token, banco, pago o datos sensibles, debes pedir pausa humana.
        Debes usar pasos pequeños:
        1 observe
        2 decide
        3 act
        4 verify
        Si no estás seguro, observa de nuevo o pausa.
        STOP tiene prioridad absoluta.
        No hagas submit de formularios sensibles sin instrucción humana explícita posterior.
        Devuelve siempre tool calls JSON válidos.
        """;

    private readonly HttpClient _httpClient;
    private readonly ChromeLabOptions _options;

    public OpenAiAgentClient(HttpClient httpClient, ChromeLabOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public bool HasApiKey => _options.HasApiKey;

    public async Task<string> CreateToolDecisionAsync(string instruction, object observation, CancellationToken cancellationToken)
    {
        if (!_options.HasApiKey)
            return """{"type":"error","reason":"OpenAI API key missing. Set OPENAI_API_KEY or config/chrome-lab.local.json."}""";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        var body = new
        {
            model = _options.Model,
            input = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = JsonSerializer.Serialize(new { instruction, observation }) }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { type = "error", reason = "OpenAI request failed", status = (int)response.StatusCode });

        return content;
    }
}
