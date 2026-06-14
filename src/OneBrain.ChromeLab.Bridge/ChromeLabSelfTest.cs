using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge;

public static class ChromeLabSelfTest
{
    public static async Task<int> RunAsync(ChromeLabOptions options)
    {
        var runManager = new ChromeLabRunManager();
        var run = runManager.Start("self-test");
        runManager.Pause(run.RunId, "humanInterventionRequired");
        runManager.Resume(run.RunId);
        var stopped = runManager.Stop(run.RunId);

        var checks = new Dictionary<string, bool>
        {
            ["health"] = true,
            ["configPublicDoesNotExposeApiKey"] = !JsonSerializer.Serialize(new PublicConfigResponse(
                ChromeLabProtocol.ServiceName,
                ChromeLabProtocol.Version,
                "openai",
                options.Model,
                options.HasApiKey,
                options.RequiresToken)).Contains(options.ApiKey ?? ChromeLabSecrets.ApiKeyPlaceholder, StringComparison.Ordinal),
            ["toolRouterAllowsObserve"] = ChromeLabToolPolicy.Validate("observePage", new Dictionary<string, object?>()).Allowed,
            ["toolRouterRejectsUnknown"] = !ChromeLabToolPolicy.Validate("executeScript", new Dictionary<string, object?>()).Allowed,
            ["urlRejectsScript"] = !UrlValidator.IsAllowedNavigationUrl("javascript:alert(1)"),
            ["stopCancelsRun"] = stopped.StopRequested && stopped.Status == "stopped",
            ["webSocketProtocolVersion"] = ChromeLabProtocol.Version == "chrome-lab-v1"
        };

        var ok = checks.Values.All(value => value);
        var result = new
        {
            ok,
            service = ChromeLabProtocol.ServiceName,
            protocolVersion = ChromeLabProtocol.Version,
            hasApiKey = options.HasApiKey,
            checks
        };
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(result, ChromeLabProtocol.JsonOptions));
        return ok ? 0 : 1;
    }
}
