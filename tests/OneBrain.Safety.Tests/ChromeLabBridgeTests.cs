using System.Text.Json;
using System.Net.WebSockets;
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
            HasApiKey: true,
            RequiresToken: true);
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
    public void ChromeLabOptionsRequireConnectionToken()
    {
        var options = new ChromeLabOptions { ConnectionToken = "local-token" };

        Assert.IsTrue(options.RequiresToken);
    }

    [TestMethod]
    public void LoadReadsApiKeyFromApiKeyTxtWhenEnvIsMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "onebrain-chromelab-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "ApiKey.txt"), "test-local-key");
            Directory.SetCurrentDirectory(tempDir);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

            var options = ChromeLabOptions.Load([]);

            Assert.AreEqual("test-local-key", options.ApiKey);
            Assert.IsTrue(options.HasApiKey);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void ToolRouterAllowsOnlyKnownTools()
    {
        Assert.IsTrue(ChromeLabToolPolicy.Validate("observePage", new Dictionary<string, object?>()).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("getElementCatalog", new Dictionary<string, object?>()).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("resolveTarget", new Dictionary<string, object?> { ["targetText"] = "iniciar sesion" }).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("clickElement", new Dictionary<string, object?> { ["elementId"] = "nexa-el-1" }).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("readElement", new Dictionary<string, object?> { ["elementId"] = "nexa-el-1" }).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("setElementValue", new Dictionary<string, object?> { ["elementId"] = "nexa-el-1" }).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("focusElement", new Dictionary<string, object?> { ["elementId"] = "nexa-el-1" }).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("highlightElement", new Dictionary<string, object?> { ["elementId"] = "nexa-el-1" }).Allowed);
        Assert.IsTrue(ChromeLabToolPolicy.Validate("scrollElementIntoView", new Dictionary<string, object?> { ["elementId"] = "nexa-el-1" }).Allowed);
        Assert.IsFalse(ChromeLabToolPolicy.Validate("executeScript", new Dictionary<string, object?>()).Allowed);
    }

    [TestMethod]
    public void ResolveTargetRequiresTargetText()
    {
        var result = ChromeLabToolPolicy.Validate("resolveTarget", new Dictionary<string, object?>());

        Assert.IsFalse(result.Allowed);
        Assert.AreEqual("TargetTextRejected", result.Reason);
    }

    [TestMethod]
    public void ClickAllowsElementIdWithoutSelector()
    {
        var result = ChromeLabToolPolicy.Validate("click", new Dictionary<string, object?> { ["elementId"] = "nexa-el-1" });

        Assert.IsTrue(result.Allowed);
    }

    [TestMethod]
    public void OpenAiPromptRequiresResolveTargetAndElementIdActions()
    {
        Assert.IsTrue(OpenAiAgentClient.SystemPrompt.Contains("preferir resolveTarget", StringComparison.Ordinal));
        Assert.IsTrue(OpenAiAgentClient.SystemPrompt.Contains("usa elementId", StringComparison.Ordinal));
        Assert.IsTrue(OpenAiAgentClient.SystemPrompt.Contains("highlightElement", StringComparison.Ordinal));
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
    public void ClientRegistryDiagnosticsShowsDisconnectedWhenNoHello()
    {
        using var socket = WebSocket.CreateFromStream(new MemoryStream(), true, null, TimeSpan.FromSeconds(1));
        var registry = new ChromeLabClientRegistry();
        var clientId = registry.Add(socket);
        var diagnostics = registry.Diagnostics();

        Assert.AreEqual(0, diagnostics.ConnectedCount);
        Assert.AreEqual(clientId, diagnostics.Clients[0].ClientId);
        Assert.IsFalse(diagnostics.Clients[0].Connected);
    }

    [TestMethod]
    public void ProtocolEventBufferRedactsSensitiveSummaries()
    {
        var events = new ProtocolEventBuffer();

        events.Add("debug", "password clave token raw value");
        var snapshot = events.Snapshot();

        Assert.IsFalse(snapshot[0].Summary.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshot[0].Summary.Contains("clave", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshot[0].Summary.Contains("token raw", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void OpenAiDecisionParserReadsOutputTextDecision()
    {
        const string response = """
            {
              "output": [
                {
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"tool\":\"click\",\"args\":{\"selector\":\"a[href='/login']\"},\"reason\":\"Open login\"}"
                    }
                  ]
                }
              ]
            }
            """;

        var decision = OpenAiAgentClient.ParseDecisionResponse(response);

        Assert.AreEqual("click", decision.Tool);
        Assert.AreEqual("Open login", decision.Reason);
        Assert.AreEqual("a[href='/login']", decision.Args["selector"]);
    }

    [TestMethod]
    public void OpenAiDecisionParserRejectsMissingTool()
    {
        const string response = """
            {
              "output": [
                {
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"args\":{},\"reason\":\"missing tool\"}"
                    }
                  ]
                }
              ]
            }
            """;

        Assert.ThrowsExactly<InvalidOperationException>(() => OpenAiAgentClient.ParseDecisionResponse(response));
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
    public void SidePanelDefinesModeTabsAndGlobalState()
    {
        var extensionDir = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab");
        var html = File.ReadAllText(Path.Combine(extensionDir, "sidepanel.html"));
        var js = File.ReadAllText(Path.Combine(extensionDir, "sidepanel.js"));

        Assert.IsTrue(html.Contains("Operar", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("Aprender", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("Recetas", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("Runtime", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("globalStopBtn", StringComparison.Ordinal));
        Assert.IsTrue(js.Contains("const state = {", StringComparison.Ordinal));
        Assert.IsTrue(js.Contains("learning:", StringComparison.Ordinal));
        Assert.IsTrue(js.Contains("recipes:", StringComparison.Ordinal));
        Assert.IsTrue(js.Contains("runtime:", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExtensionDefinesLearningAndRecipeStorageV0()
    {
        var extensionDir = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab");
        var serviceWorker = File.ReadAllText(Path.Combine(extensionDir, "service_worker.js"));
        var contentScript = File.ReadAllText(Path.Combine(extensionDir, "content_script.js"));

        Assert.IsTrue(serviceWorker.Contains("nexaRecipes", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("nexaLearningDraft", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("learning.event", StringComparison.Ordinal));
        Assert.IsTrue(contentScript.Contains("learning.start", StringComparison.Ordinal));
        Assert.IsTrue(contentScript.Contains("valueRedacted", StringComparison.Ordinal));
        Assert.IsTrue(contentScript.Contains("isPassword", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExtensionDefinesRecipeSchemaV1AndDeterministicRunner()
    {
        var extensionDir = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab");
        var recipeCore = File.ReadAllText(Path.Combine(extensionDir, "recipe_core.js"));
        var serviceWorker = File.ReadAllText(Path.Combine(extensionDir, "service_worker.js"));

        Assert.IsTrue(recipeCore.Contains("NEXA_RECIPE_SCHEMA_VERSION = 1", StringComparison.Ordinal));
        Assert.IsTrue(recipeCore.Contains("createRecipeV1", StringComparison.Ordinal));
        Assert.IsTrue(recipeCore.Contains("validateRecipeV1", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("startRecipeRun", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("executeRecipeStep", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("stepResults", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("recipeRunState", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BridgeDefinesConnectionReliabilityEndpointsAndFramedWebSocketRead()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.ChromeLab.Bridge", "Program.cs"));

        Assert.IsTrue(program.Contains("MapGet(\"/clients\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("MapGet(\"/runtime\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("MapGet(\"/debug\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("MapGet(\"/last-events\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("ReceiveTextMessageAsync", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("EndOfMessage", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("Results.Conflict", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("invalid_token", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExtensionDefinesReconnectHeartbeatAlarmsAndSessionState()
    {
        var extensionDir = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab");
        var serviceWorker = File.ReadAllText(Path.Combine(extensionDir, "service_worker.js"));
        var manifest = File.ReadAllText(Path.Combine(extensionDir, "manifest.json"));
        var sidePanel = File.ReadAllText(Path.Combine(extensionDir, "sidepanel.js"));

        Assert.IsTrue(manifest.Contains("\"alarms\"", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("chrome.alarms.create('nexa.keepalive'", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("chrome.storage.session", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("extension.ping", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("engine.pong", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("scheduleReconnect", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("outgoingQueue", StringComparison.Ordinal));
        Assert.IsTrue(sidePanel.Contains("runtimeDiagnostic", StringComparison.Ordinal));
        Assert.IsTrue(sidePanel.Contains("refreshDebug", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExtensionIncludesJsFixtureTests()
    {
        var testPath = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab", "tests", "fixture_tests.js");
        var tests = File.ReadAllText(testPath);

        Assert.IsTrue(tests.Contains("testBasicButtons", StringComparison.Ordinal));
        Assert.IsTrue(tests.Contains("testFormRedaction", StringComparison.Ordinal));
        Assert.IsTrue(tests.Contains("testStableSelectors", StringComparison.Ordinal));
        Assert.IsTrue(tests.Contains("testAmbiguousTargets", StringComparison.Ordinal));
        Assert.IsTrue(tests.Contains("testRecipeRunnerFixture", StringComparison.Ordinal));
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
