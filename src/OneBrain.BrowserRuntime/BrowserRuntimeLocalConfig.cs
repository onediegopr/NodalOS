using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.BrowserRuntime;

public sealed record BrowserRuntimeLocalConfig
{
    [JsonPropertyName("cloakbrowser_executable_path")]
    public string CloakBrowserExecutablePath { get; init; } = string.Empty;

    [JsonPropertyName("cdp_port")]
    public string CdpPort { get; init; } = "ephemeral-or-reserved";

    public bool HasExecutablePath => !string.IsNullOrWhiteSpace(CloakBrowserExecutablePath);

    public static BrowserRuntimeLocalConfig Empty { get; } = new();

    public static BrowserRuntimeLocalConfig Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return Empty;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BrowserRuntimeLocalConfig>(json, JsonOptions) ?? Empty;
    }

    public static BrowserRuntimeLocalConfig FromEnvironment(IDictionary<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return environment.TryGetValue("NODAL_CLOAKBROWSER_RUNTIME_PATH", out var path)
            && !string.IsNullOrWhiteSpace(path)
                ? new BrowserRuntimeLocalConfig { CloakBrowserExecutablePath = path }
                : Empty;
    }

    public static BrowserRuntimeLocalConfig Discover(string repositoryRoot, IDictionary<string, string?> environment)
    {
        var fromEnvironment = FromEnvironment(environment);
        if (fromEnvironment.HasExecutablePath)
        {
            return fromEnvironment;
        }

        return Load(Path.Combine(repositoryRoot, ".local", "browser-runtime.local.json"));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
