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
    public bool SelfTest { get; init; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public static ChromeLabOptions Load(string[] args)
    {
        var host = ReadArg(args, "--host") ?? "127.0.0.1";
        var port = int.TryParse(ReadArg(args, "--port"), out var parsedPort) ? parsedPort : 8787;
        var model = ReadArg(args, "--model") ?? "gpt-4.1-mini";
        var selfTest = args.Any(arg => string.Equals(arg, "--self-test", StringComparison.OrdinalIgnoreCase));
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = TryReadLocalApiKey();

        return new ChromeLabOptions
        {
            Host = host,
            Port = port,
            Model = model,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
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
        var path = Path.Combine(AppContext.BaseDirectory, "config", "chrome-lab.local.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "config", "chrome-lab.local.json");
            if (!File.Exists(path))
                return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("openAiApiKey", out var key)
                ? key.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
