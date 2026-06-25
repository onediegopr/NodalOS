using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.BrowserRuntime;

public sealed record BrowserRuntimeLock
{
    [JsonPropertyName("provider")]
    public string Provider { get; init; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    [JsonPropertyName("extension_enabled")]
    public bool ExtensionEnabled { get; init; }

    [JsonPropertyName("runtime_source")]
    public string RuntimeSource { get; init; } = string.Empty;

    [JsonPropertyName("runtime_repo")]
    public string RuntimeRepo { get; init; } = string.Empty;

    [JsonPropertyName("runtime_channel")]
    public string RuntimeChannel { get; init; } = string.Empty;

    [JsonPropertyName("runtime_path_policy")]
    public string RuntimePathPolicy { get; init; } = string.Empty;

    [JsonPropertyName("runtime_version")]
    public string RuntimeVersion { get; init; } = string.Empty;

    [JsonPropertyName("runtime_commit")]
    public string RuntimeCommit { get; init; } = string.Empty;

    [JsonPropertyName("upstream_commit")]
    public string UpstreamCommit { get; init; } = string.Empty;

    [JsonPropertyName("binary_sha256")]
    public string BinarySha256 { get; init; } = string.Empty;

    [JsonPropertyName("cdp_host")]
    public string CdpHost { get; init; } = string.Empty;

    [JsonPropertyName("cdp_port_policy")]
    public string CdpPortPolicy { get; init; } = string.Empty;

    [JsonPropertyName("system_browser_allowed")]
    public bool SystemBrowserAllowed { get; init; }

    public static BrowserRuntimeLock Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BrowserRuntimeLock>(json, JsonOptions)
            ?? throw new InvalidOperationException("Browser runtime lockfile could not be parsed.");
    }

    public BrowserRuntimeValidationResult Validate()
    {
        var errors = new List<string>();

        if (!Provider.Equals("cloakbrowser", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("BrowserRuntimeProviderMustBeCloakBrowser");
        }

        if (!Mode.Equals("cdp-direct", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("BrowserRuntimeModeMustBeCdpDirect");
        }

        if (SystemBrowserAllowed)
        {
            errors.Add("SystemBrowserMustNotBeAllowed");
        }

        if (Provider.Equals("chrome", StringComparison.OrdinalIgnoreCase)
            || Provider.Equals("chromium", StringComparison.OrdinalIgnoreCase)
            || Provider.Equals("edge", StringComparison.OrdinalIgnoreCase)
            || Provider.Equals("msedge", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("SystemBrowserProviderRejected");
        }

        if (Mode.Equals("cdp-direct", StringComparison.OrdinalIgnoreCase) && ExtensionEnabled)
        {
            errors.Add("ExtensionMustNotBeDefaultRuntimeForCdpDirect");
        }

        if (!IsAllowedRuntimeSource(RuntimeSource))
        {
            errors.Add("RuntimeSourceMustBeForkReleaseOrPackage");
        }

        if (string.IsNullOrWhiteSpace(CdpHost))
        {
            errors.Add("CdpHostRequired");
        }

        return errors.Count == 0
            ? BrowserRuntimeValidationResult.Valid
            : BrowserRuntimeValidationResult.Invalid(errors);
    }

    public bool HasPinnedRuntimeArtifact =>
        !IsPending(RuntimeVersion)
        && !IsPending(RuntimeCommit)
        && !IsPending(BinarySha256);

    private static bool IsPending(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Equals("pending", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedRuntimeSource(string value) =>
        value.Equals("fork", StringComparison.OrdinalIgnoreCase)
        || value.Equals("release", StringComparison.OrdinalIgnoreCase)
        || value.Equals("package", StringComparison.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}

public sealed record BrowserRuntimeValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static BrowserRuntimeValidationResult Valid { get; } = new(true, Array.Empty<string>());

    public static BrowserRuntimeValidationResult Invalid(IReadOnlyList<string> errors) =>
        new(false, errors);
}
