using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge;

public sealed class ChromeLabOptions
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 8787;
    public string Model { get; init; } = "gpt-4.1-mini";
    public string? ApiKey { get; init; }
    public string ConnectionToken { get; init; } = "";
    public bool AllowLan { get; init; }
    public bool SelfTest { get; init; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
    public bool RequiresToken => !string.IsNullOrWhiteSpace(ConnectionToken);

    public static ChromeLabOptions Load(string[] args)
    {
        var host = ReadArg(args, "--host") ?? "127.0.0.1";
        var port = int.TryParse(ReadArg(args, "--port"), out var parsedPort) ? parsedPort : 8787;
        var model = ReadArg(args, "--model") ?? "gpt-4.1-mini";
        var selfTest = args.Any(arg => string.Equals(arg, "--self-test", StringComparison.OrdinalIgnoreCase));
        var allowLan = args.Any(arg => string.Equals(arg, "--allow-lan", StringComparison.OrdinalIgnoreCase));
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var token = Environment.GetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN");

        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = TryReadLocalApiKey();
        if (string.IsNullOrWhiteSpace(token))
            token = TryReadLocalConnectionToken();
        if (string.IsNullOrWhiteSpace(token))
            token = Guid.NewGuid().ToString("n");

        if (!allowLan && !IsLoopbackHost(host))
            host = "127.0.0.1";

        return new ChromeLabOptions
        {
            Host = host,
            Port = port,
            Model = model,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            ConnectionToken = token,
            AllowLan = allowLan,
            SelfTest = selfTest
        };
    }

    public IEnumerable<string> GetLocalIpAddresses()
    {
        foreach (var address in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
                     .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                     .Select(address => address.Address)
                     .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)))
        {
            yield return address.ToString();
        }
    }

    private static string? ReadArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static string? TryReadLocalApiKey()
    {
        foreach (var path in GetCandidateApiKeyPaths())
        {
            var apiKey = TryReadApiKeyFile(path);
            if (!string.IsNullOrWhiteSpace(apiKey))
                return apiKey;
        }

        return null;
    }

    private static string? TryReadLocalConnectionToken()
    {
        foreach (var path in GetCandidateApiKeyPaths())
        {
            var token = TryReadConnectionTokenFile(path);
            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateApiKeyPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "config", "chrome-lab.local.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "config", "chrome-lab.local.json");
        yield return Path.Combine(AppContext.BaseDirectory, "ApiKey.txt");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "ApiKey.txt");
    }

    private static string? TryReadApiKeyFile(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                return doc.RootElement.TryGetProperty("openAiApiKey", out var key)
                    ? key.GetString()
                    : null;
            }

            return File.ReadAllText(path).Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadConnectionTokenFile(string path)
    {
        if (!File.Exists(path) || !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("connectionToken", out var token)
                ? token.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsLoopbackHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}
