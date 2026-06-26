using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.WebSockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;
using OneBrain.ChromeLab.Bridge.Sessions;
using OneBrain.ChromeLab.Bridge.Stealth;

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
        Assert.IsFalse(json.Contains("ExtensionToken", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("nexa_", StringComparison.Ordinal));
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
        var originalToken = Environment.GetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN");

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "ApiKey.txt"), "test-local-key");
            Directory.SetCurrentDirectory(tempDir);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", "test-token");

            var options = ChromeLabOptions.Load([]);

            Assert.AreEqual("test-local-key", options.ApiKey);
            Assert.IsTrue(options.HasApiKey);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", originalToken);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void LoadPrefersChromeLabLocalJsonForApiKeyAndExtensionToken()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "onebrain-chromelab-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(tempDir, "config"));
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var originalToken = Environment.GetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN");

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "ApiKey.txt"), "fallback-key");
            File.WriteAllText(Path.Combine(tempDir, "config", "chrome-lab.local.json"), """
                {
                  "OpenAiApiKey": "json-key",
                  "ExtensionToken": "nexa_json_token",
                  "Host": "127.0.0.1",
                  "Port": 8787,
                  "AllowLan": false
                }
                """);
            Directory.SetCurrentDirectory(tempDir);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", null);

            var options = ChromeLabOptions.Load([]);

            Assert.AreEqual("json-key", options.ApiKey);
            Assert.AreEqual("nexa_json_token", options.ConnectionToken);
            Assert.IsFalse(options.ConnectionTokenGenerated);
            StringAssert.Contains(options.ConnectionTokenSource, "chrome-lab.local.json");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", originalToken);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void LoadGeneratesAndPersistsExtensionTokenWhenMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "onebrain-chromelab-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalToken = Environment.GetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN");

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", null);

            var options = ChromeLabOptions.Load([]);
            var configPath = Path.Combine(tempDir, "config", "chrome-lab.local.json");
            var configJson = File.ReadAllText(configPath);

            Assert.IsTrue(options.ConnectionTokenGenerated);
            StringAssert.StartsWith(options.ConnectionToken, "nexa_");
            Assert.IsTrue(configJson.Contains("\"ExtensionToken\"", StringComparison.Ordinal));
            Assert.IsTrue(configJson.Contains(options.ConnectionToken, StringComparison.Ordinal));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", originalToken);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void LoadReadsRootConfigWhenStartedFromNestedProjectDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "onebrain-chromelab-tests", Guid.NewGuid().ToString("n"));
        var nestedDir = Path.Combine(tempDir, "src", "OneBrain.ChromeLab.Bridge");
        Directory.CreateDirectory(Path.Combine(tempDir, "config"));
        Directory.CreateDirectory(nestedDir);
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var originalToken = Environment.GetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN");

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "OneBrain.slnx"), "");
            File.WriteAllText(Path.Combine(tempDir, "config", "chrome-lab.local.json"), """
                {
                  "OpenAiApiKey": "root-json-key",
                  "ExtensionToken": "nexa_root_token"
                }
                """);
            Directory.SetCurrentDirectory(nestedDir);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", null);

            var options = ChromeLabOptions.Load([]);

            Assert.AreEqual("root-json-key", options.ApiKey);
            Assert.AreEqual("nexa_root_token", options.ConnectionToken);
            Assert.IsFalse(options.ConnectionTokenGenerated);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("NEXA_CHROME_BRIDGE_TOKEN", originalToken);
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

        Assert.IsFalse(combined.Contains("OPENAI_API_KEY", StringComparison.Ordinal));
        Assert.IsFalse(Regex.IsMatch(combined, @"(?<![A-Za-z0-9_])sk-[A-Za-z0-9]{16,}", RegexOptions.CultureInvariant));
        Assert.IsFalse(combined.Contains("eval(", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("new Function", StringComparison.Ordinal));
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
        var handler = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.ChromeLab.Bridge", "Sessions", "ExtensionMessageHandler.cs"));

        Assert.IsTrue(program.Contains("MapGet(\"/clients\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("MapGet(\"/runtime\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("MapGet(\"/debug\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("MapGet(\"/last-events\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("MapGet(\"/pairing/local-token\"", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("IPAddress.IsLoopback", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("ReceiveTextMessageAsync", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("EndOfMessage", StringComparison.Ordinal));
        Assert.IsTrue(program.Contains("Results.Conflict", StringComparison.Ordinal));
        Assert.IsTrue(handler.Contains("invalid_token", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExtensionDefinesReconnectHeartbeatAlarmsAndSessionState()
    {
        var extensionDir = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab");
        var serviceWorker = File.ReadAllText(Path.Combine(extensionDir, "service_worker.js"));
        var manifest = File.ReadAllText(Path.Combine(extensionDir, "manifest.json"));
        var sidePanel = File.ReadAllText(Path.Combine(extensionDir, "sidepanel.js"));
        var sidePanelHtml = File.ReadAllText(Path.Combine(extensionDir, "sidepanel.html"));

        Assert.IsTrue(manifest.Contains("\"alarms\"", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("chrome.alarms.create('nexa.keepalive'", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("chrome.storage.session", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("extension.ping", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("engine.pong", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("scheduleReconnect", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("reconnectBlocked", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("token_required", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("protocol_version_mismatch", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("tryLocalPairing", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("/pairing/local-token", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("isLoopbackHost", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("outgoingQueue", StringComparison.Ordinal));
        Assert.IsTrue(sidePanel.Contains("runtimeDiagnostic", StringComparison.Ordinal));
        Assert.IsTrue(sidePanelHtml.Contains("Verificar conexion", StringComparison.Ordinal));
        Assert.IsTrue(sidePanelHtml.Contains("Reconectar extension", StringComparison.Ordinal));
        Assert.IsTrue(sidePanelHtml.Contains("Limpiar estado local", StringComparison.Ordinal));
        Assert.IsTrue(sidePanelHtml.Contains("Guardar y conectar", StringComparison.Ordinal));
        Assert.IsTrue(sidePanelHtml.Contains("Borrar token guardado", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExtensionDefinesLearningPauseResumeWithoutCapturingPausedEvents()
    {
        var extensionDir = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab");
        var serviceWorker = File.ReadAllText(Path.Combine(extensionDir, "service_worker.js"));
        var sidePanelHtml = File.ReadAllText(Path.Combine(extensionDir, "sidepanel.html"));
        var sidePanelJs = File.ReadAllText(Path.Combine(extensionDir, "sidepanel.js"));

        Assert.IsTrue(sidePanelHtml.Contains("pauseLearningBtn", StringComparison.Ordinal));
        Assert.IsTrue(sidePanelHtml.Contains("resumeLearningBtn", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("case 'learningPause'", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("case 'learningResume'", StringComparison.Ordinal));
        Assert.IsTrue(serviceWorker.Contains("learningSession.learningState === 'paused'", StringComparison.Ordinal));
        Assert.IsTrue(sidePanelJs.Contains("Aprendizaje pausado", StringComparison.Ordinal));
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
    [TestCategory("CompanionBridgeBugfix")]
    public async Task WebSocketSessionSendRawUsesClientSendLock()
    {
        using var socket = new SerializedTestWebSocket("""{"type":"extension.ping","seq":1}""");
        var registry = new ChromeLabClientRegistry();
        var clientId = registry.Add(socket);
        registry.RegisterHello(clientId, ChromeLabProtocol.Version, "test-extension", "fixture", null);
        var sendLock = registry.GetSendLock(clientId);
        Assert.IsNotNull(sendLock);

        var broadcast = registry.BroadcastAsync(new { type = "run.resume", runId = "run-1" }, CancellationToken.None);
        await socket.FirstSendEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var session = new WebSocketSession(
            clientId,
            new StaticMessageHandler("""{"type":"engine.pong","seq":1}"""),
            new ProtocolEventBuffer(),
            sendLock!);
        var run = session.RunAsync(socket, CancellationToken.None);

        await Task.Delay(50);
        Assert.AreEqual(1, socket.MaxConcurrentSends);
        Assert.AreEqual(1, socket.SendCount);

        socket.ReleaseFirstSend.SetResult();
        await Task.WhenAll(broadcast, run).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(1, socket.MaxConcurrentSends);
        Assert.AreEqual(2, socket.SendCount);
    }

    [TestMethod]
    [TestCategory("CompanionBridgeBugfix")]
    public async Task HandoffCompletionIsIdempotentByHandoffId()
    {
        var events = new ProtocolEventBuffer();
        var gateway = new StealthHandoffGateway(
            new StealthRunnerRegistry(),
            new HandoffVerificationService(),
            events);

        var first = await gateway.CompleteAsync("task-1", "handoff-1", success: true, CancellationToken.None);
        var duplicate = await gateway.CompleteAsync("task-1", "handoff-1", success: true, CancellationToken.None);
        var snapshot = events.Snapshot();

        Assert.IsTrue(first.FirstCompletion);
        Assert.IsTrue(first.Verified);
        Assert.IsFalse(duplicate.FirstCompletion);
        Assert.AreEqual(1, snapshot.Count(e => e.EventType == "handoff.completed"));
        Assert.AreEqual(1, snapshot.Count(e => e.EventType == "stealth.handoff.verifying"));
        Assert.AreEqual(1, snapshot.Count(e => e.EventType == "stealth.handoff.verified"));
    }

    [TestMethod]
    [TestCategory("CompanionBridgeBugfix")]
    public void ChromeLabRedactorRedactsStructuredSensitiveFields()
    {
        const string raw = """
            {
              "token": "tok-raw",
              "apiKey": "key-raw",
              "secret": "secret-raw",
              "password": "password-raw",
              "authorization": "Bearer authorization-raw",
              "cookie": "cookie-raw",
              "credential": "credential-raw",
              "accessToken": "access-raw",
              "refreshToken": "refresh-raw",
              "sessionId": "session-raw",
              "safeField": "safe-value"
            }
            """;

        var redacted = ChromeLabRedactor.Redact(raw);

        foreach (var leaked in new[]
        {
            "tok-raw",
            "key-raw",
            "secret-raw",
            "password-raw",
            "authorization-raw",
            "cookie-raw",
            "credential-raw",
            "access-raw",
            "refresh-raw",
            "session-raw"
        })
        {
            Assert.IsFalse(redacted.Contains(leaked, StringComparison.OrdinalIgnoreCase), leaked);
        }

        Assert.IsTrue(redacted.Contains("safe-value", StringComparison.Ordinal));
        Assert.IsTrue(redacted.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    [TestCategory("CompanionBridgeBugfix")]
    public void ClientDiagnosticsRedactsStructuredSensitiveErrors()
    {
        using var socket = new SerializedTestWebSocket("""{"type":"noop"}""");
        var registry = new ChromeLabClientRegistry();
        var clientId = registry.Add(socket);
        registry.MarkError(clientId, """{"apiKey":"key-raw","sessionId":"session-raw","safe":"visible"}""");

        var lastError = registry.Diagnostics().Clients.Single(c => c.ClientId == clientId).LastError;

        Assert.IsNotNull(lastError);
        Assert.IsFalse(lastError!.Contains("key-raw", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(lastError.Contains("session-raw", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(lastError.Contains("visible", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ManifestVersionIsThree()
    {
        var manifestPath = Path.Combine(FindRepoRoot(), "browser-extension", "onebrain-chrome-lab", "manifest.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));

        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32());
    }

    private sealed class StaticMessageHandler : IMessageHandler
    {
        private readonly string _response;

        public StaticMessageHandler(string response) => _response = response;

        public Task<string?> HandleAsync(string json, string clientId, CancellationToken ct) =>
            Task.FromResult<string?>(_response);
    }

    private sealed class SerializedTestWebSocket : WebSocket
    {
        private readonly byte[] _message;
        private int _receiveCount;
        private int _activeSends;
        private int _sendCount;
        private WebSocketState _state = WebSocketState.Open;

        public SerializedTestWebSocket(string message) => _message = Encoding.UTF8.GetBytes(message);

        public TaskCompletionSource FirstSendEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstSend { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SendCount => Volatile.Read(ref _sendCount);

        public int MaxConcurrentSends { get; private set; }

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => _state;

        public override string? SubProtocol => null;

        public override void Abort() => _state = WebSocketState.Aborted;

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Dispose() => _state = WebSocketState.Closed;

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var receive = Interlocked.Increment(ref _receiveCount);
            if (receive > 1)
                return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));

            Buffer.BlockCopy(_message, 0, buffer.Array!, buffer.Offset, _message.Length);
            return Task.FromResult(new WebSocketReceiveResult(_message.Length, WebSocketMessageType.Text, true));
        }

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var receive = Interlocked.Increment(ref _receiveCount);
            if (receive > 1)
                return ValueTask.FromResult(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));

            _message.CopyTo(buffer);
            return ValueTask.FromResult(new ValueWebSocketReceiveResult(_message.Length, WebSocketMessageType.Text, true));
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await SendCoreAsync(cancellationToken);
        }

        public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await SendCoreAsync(cancellationToken);
        }

        private async Task SendCoreAsync(CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _activeSends);
            MaxConcurrentSends = Math.Max(MaxConcurrentSends, active);
            var count = Interlocked.Increment(ref _sendCount);
            if (count == 1)
            {
                FirstSendEntered.SetResult();
                await ReleaseFirstSend.Task.WaitAsync(cancellationToken);
            }

            await Task.Delay(5, cancellationToken);
            Interlocked.Decrement(ref _activeSends);
        }
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
