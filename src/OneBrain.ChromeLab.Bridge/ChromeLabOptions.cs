using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneBrain.ChromeLab.Bridge;

public sealed class ChromeLabOptions
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 8787;
    public string Model { get; init; } = "gpt-4.1-mini";
    public string? ApiKey { get; init; }
    public string ConnectionToken { get; init; } = "";
    public string ConnectionTokenSource { get; init; } = "";
    public bool ConnectionTokenGenerated { get; init; }
    public bool AllowLan { get; init; }
    public bool SelfTest { get; init; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
    public bool RequiresToken => !string.IsNullOrWhiteSpace(ConnectionToken);

    public static ChromeLabOptions Load(string[] args)
    {
        var configFile = TryReadLocalConfig();
        var host = ReadArg(args, "--host") ?? TryReadString(configFile, "Host") ?? "127.0.0.1";
        var port = int.TryParse(ReadArg(args, "--port"), out var parsedPort)
            ? parsedPort
            : TryReadInt(configFile, "Port") ?? 8787;
        var model = ReadArg(args, "--model") ?? "gpt-4.1-mini";
        var selfTest = args.Any(arg => string.Equals(arg, "--self-test", StringComparison.OrdinalIgnoreCase));
        var allowLan = args.Any(arg => string.Equals(arg, "--allow-lan", StringComparison.OrdinalIgnoreCase)) ||
                       TryReadBool(configFile, "AllowLan") == true;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var token = Environment.GetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN");
        var tokenSource = string.IsNullOrWhiteSpace(token) ? "" : "environment";
        var tokenGenerated = false;

        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = TryReadLocalApiKey();
        if (string.IsNullOrWhiteSpace(token))
        {
            var tokenRead = TryReadLocalConnectionToken();
            token = tokenRead.Token;
            tokenSource = tokenRead.Source;
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            token = GenerateExtensionToken();
            SaveGeneratedConnectionToken(token);
            tokenSource = "config/chrome-lab.local.json";
            tokenGenerated = true;
        }

        if (!allowLan && !IsLoopbackHost(host))
            host = "127.0.0.1";

        return new ChromeLabOptions
        {
            Host = host,
            Port = port,
            Model = model,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            ConnectionToken = token,
            ConnectionTokenSource = tokenSource,
            ConnectionTokenGenerated = tokenGenerated,
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

    private static (string? Token, string Source) TryReadLocalConnectionToken()
    {
        foreach (var path in GetCandidateApiKeyPaths())
        {
            var token = TryReadConnectionTokenFile(path);
            if (!string.IsNullOrWhiteSpace(token))
                return (token, path);
        }

        return (null, "");
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
                if (doc.RootElement.TryGetProperty("OpenAiApiKey", out var pascalKey))
                    return pascalKey.GetString();
                if (doc.RootElement.TryGetProperty("openAiApiKey", out var camelKey))
                    return camelKey.GetString();
                return null;
            }

            return File.ReadAllText(path).Trim();
        }
        catch
        {
            return null;
        }
    }

    private static JsonDocument? TryReadLocalConfig()
    {
        foreach (var path in GetCandidateApiKeyPaths().Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            if (!File.Exists(path))
                continue;
            try
            {
                return JsonDocument.Parse(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static string? TryReadString(JsonDocument? doc, string propertyName)
    {
        return doc != null && doc.RootElement.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? TryReadInt(JsonDocument? doc, string propertyName)
    {
        return doc != null && doc.RootElement.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static bool? TryReadBool(JsonDocument? doc, string propertyName)
    {
        return doc != null && doc.RootElement.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : null;
    }

    private static string? TryReadConnectionTokenFile(string path)
    {
        if (!File.Exists(path) || !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ExtensionToken", out var pascalToken))
                return pascalToken.GetString();
            if (doc.RootElement.TryGetProperty("connectionToken", out var camelToken))
                return camelToken.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateExtensionToken()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return $"nexa_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    private static void SaveGeneratedConnectionToken(string token)
    {
        var path = Path.Combine(FindRepoOrCurrentDirectory(), "config", "chrome-lab.local.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        JsonObject root;
        if (File.Exists(path))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
            }
            catch
            {
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }

        if (!root.ContainsKey("OpenAiApiKey"))
            root["OpenAiApiKey"] = "";
        root["ExtensionToken"] = token;
        if (!root.ContainsKey("Host"))
            root["Host"] = "127.0.0.1";
        if (!root.ContainsKey("Port"))
            root["Port"] = 8787;
        if (!root.ContainsKey("AllowLan"))
            root["AllowLan"] = false;

        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static string FindRepoOrCurrentDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static bool IsLoopbackHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}
