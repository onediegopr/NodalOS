using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabBridgeTests
{
    [TestMethod]
    public void HealthReturnsOk()
    {
        var health = new HealthResponse(true, ChromeLabProtocol.ServiceName, ChromeLabProtocol.EngineVersion);

        Assert.IsTrue(health.Ok);
        Assert.AreEqual("onebrain-chrome-lab-bridge", health.Service);
    }

    [TestMethod]
    public void ConfigPublicNeverExposesApiKey()
    {
        var config = new PublicConfigResponse(
            ChromeLabProtocol.ServiceName,
            ChromeLabProtocol.Version,
            "openai",
            "gpt-4.1-mini",
            HasApiKey: true);
        var json = JsonSerializer.Serialize(config, ChromeLabProtocol.JsonOptions);

        Assert.IsFalse(json.Contains("real-secret-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("OPENAI_API_KEY", StringComparison.Ordinal));
    }

    [TestMethod]
    public void MissingApiKeyDoesNotCrash()
    {
        var options = new ChromeLabOptions { ApiKey = null };

        Assert.IsFalse(options.HasApiKey);
    }

    [TestMethod]
    public void ToolRouterAllowsOnlyKnownTools()
    {
        Assert.IsTrue(ChromeLabToolPolicy.Validate("observePage", new Dictionary<string, object?>()).Allowed);
        Assert.IsFalse(ChromeLabToolPolicy.Validate("executeScript", new Dictionary<string, object?>()).Allowed);
    }

    [TestMethod]
    public void UrlValidatorRejectsUnsafeUrls()
    {
        Assert.IsTrue(UrlValidator.IsAllowedNavigationUrl("https://www.afip.gob.ar/"));
        Assert.IsFalse(UrlValidator.IsAllowedNavigationUrl("javascript:alert(1)"));
        Assert.IsFalse(UrlValidator.IsAllowedNavigationUrl("data:text/html,hello"));
        Assert.IsFalse(UrlValidator.IsAllowedNavigationUrl("file:///c:/temp/a.html"));
        Assert.IsFalse(UrlValidator.IsAllowedNavigationUrl("chrome://settings"));
    }

    [TestMethod]
    public void StopCancelsActiveRun()
    {
        var manager = new ChromeLabRunManager();
        var run = manager.Start("test");
        var stopped = manager.Stop(run.RunId);

        Assert.AreEqual("stopped", stopped.Status);
        Assert.IsTrue(stopped.StopRequested);
    }

    [TestMethod]
    public void PauseResumeStateTransitions()
    {
        var manager = new ChromeLabRunManager();
        var run = manager.Start("test");
        var paused = manager.Pause(run.RunId, "credentialRequired");
        var resumed = manager.Resume(run.RunId);

        Assert.AreEqual("paused", paused.Status);
        Assert.AreEqual("running", resumed.Status);
        Assert.IsNull(resumed.PausedReason);
    }

    [TestMethod]
    public void CredentialRequiredPausesRun()
    {
        var manager = new ChromeLabRunManager();
        var run = manager.Start("test");
        var paused = manager.CredentialRequired(run.RunId, "credentialRequired");

        Assert.AreEqual("paused", paused.Status);
        Assert.AreEqual("credentialRequired", paused.PausedReason);
    }

    [TestMethod]
    public void ProtocolMessagesSerializeDeserialize()
    {
        var request = new ToolRequest(
            "tool.request",
            "run-1",
            "request-1",
            "observePage",
            new Dictionary<string, object?>());
        var json = JsonSerializer.Serialize(request, ChromeLabProtocol.JsonOptions);
        var parsed = JsonSerializer.Deserialize<ToolRequest>(json, ChromeLabProtocol.JsonOptions);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("observePage", parsed!.Tool);
    }

    [TestMethod]
    public void PendingToolRequestRegistryTracksAndCompletesOnce()
    {
        var registry = new PendingToolRequestRegistry();

        registry.Track("request-1", "run-1", "navigate");
        var completed = registry.Complete("request-1");

        Assert.IsNotNull(completed);
        Assert.AreEqual("run-1", completed!.RunId);
        Assert.AreEqual("navigate", completed.Tool);
        Assert.IsNull(registry.Complete("request-1"));
    }

    [TestMethod]
    public void ExtensionSourceHasNoApiKeyOrRemoteCode()
    {
        var root = FindRepoRoot();
        var extensionDir = Path.Combine(root, "browser-extension", "onebrain-chrome-lab");
        var files = Directory.GetFiles(extensionDir, "*.*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".js" or ".html" or ".json" or ".css" or ".md")
            .ToList();
        var combined = string.Join("\n", files.Select(File.ReadAllText));
        var forbiddenPrefix = "s" + "k-";

        Assert.IsFalse(combined.Contains("OPENAI_API_KEY", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains(forbiddenPrefix, StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("eval(", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("new Function", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("executeScript(", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("https://cdn.", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(combined.Contains("<script src=\"http", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ManifestVersionIsThree()
    {
        var manifestPath = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab", "manifest.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));

        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32());
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return dir.FullName;
    }
}
