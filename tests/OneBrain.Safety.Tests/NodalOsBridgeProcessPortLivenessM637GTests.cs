using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeLiveness")]
[TestCategory("NodalOsBridgeProcessPortLiveness")]
[TestCategory("M637G")]
public sealed class NodalOsBridgeProcessPortLivenessM637GTests
{
    private const string DiagnosticPath = "artifacts/agent-operations/m637g/bridge-process-port-liveness-diagnostic.json";
    private const string EndpointMatrixPath = "artifacts/agent-operations/m637g/bridge-endpoint-liveness-matrix.json";
    private const string FixSummaryPath = "artifacts/agent-operations/m637g/bridge-startup-fix-summary.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m637g/post-m637g-go-no-go.json";
    private const string ReportPath = "docs/reports/bridge-process-port-liveness-m637g.md";
    private const string RunbookPath = "docs/runbooks/chrome-lab-bridge-startup-and-liveness.md";
    private const string LivenessScriptPath = "tools/scripts/bridge-liveness-check.ps1";
    private const string BridgeProgramPath = "src/OneBrain.ChromeLab.Bridge/Program.cs";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ServiceWorkerPath = "browser-extension/onebrain-chrome-lab/service_worker.js";
    private const string ContentScriptPath = "browser-extension/onebrain-chrome-lab/content_script.js";
    private const string RecipeCorePath = "browser-extension/onebrain-chrome-lab/recipe_core.js";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static string ReadRepoText(string relativePath) => File.ReadAllText(FullPath(relativePath));
    private static JsonDocument ReadJson(string relativePath) => JsonDocument.Parse(ReadRepoText(relativePath));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = File.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void AssertContains(string haystack, string needle) =>
        Assert.IsTrue(haystack.Contains(needle, StringComparison.Ordinal), $"Expected to find '{needle}'.");

    private static void AssertNotContains(string haystack, string needle) =>
        Assert.IsFalse(haystack.Contains(needle, StringComparison.OrdinalIgnoreCase), $"Did not expect to find '{needle}'.");

    // --- Artifact + report + deliverable existence ---

    [TestMethod]
    public void DiagnosticArtifactExists() => Assert.IsTrue(File.Exists(FullPath(DiagnosticPath)), DiagnosticPath);

    [TestMethod]
    public void EndpointMatrixArtifactExists() => Assert.IsTrue(File.Exists(FullPath(EndpointMatrixPath)), EndpointMatrixPath);

    [TestMethod]
    public void FixSummaryArtifactExists() => Assert.IsTrue(File.Exists(FullPath(FixSummaryPath)), FixSummaryPath);

    [TestMethod]
    public void GoNoGoArtifactExists() => Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void ReportMarkdownExists() => Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    [TestMethod]
    public void RunbookExists() => Assert.IsTrue(File.Exists(FullPath(RunbookPath)), RunbookPath);

    [TestMethod]
    public void LivenessScriptExists() => Assert.IsTrue(File.Exists(FullPath(LivenessScriptPath)), LivenessScriptPath);

    // --- Diagnostic fields ---

    [TestMethod]
    public void DiagnosticDecisionIsLivenessFixReady()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.AreEqual("BRIDGE_PROCESS_PORT_LIVENESS_FIX_READY", doc.RootElement.GetProperty("decision").GetString());
    }

    [TestMethod]
    public void DiagnosticRecordsErrConnectionRefusedExplanation()
    {
        using var doc = ReadJson(DiagnosticPath);
        var explained = doc.RootElement.GetProperty("errConnectionRefusedExplainedBy").GetString() ?? "";
        Assert.IsTrue(explained.Length > 0, "errConnectionRefusedExplainedBy must be populated");
    }

    [TestMethod]
    public void DiagnosticReportsWsEndpointAcceptsUpgrade()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsTrue(doc.RootElement.GetProperty("wsEndpointMapped").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("wsEndpointAcceptsUpgrade").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticReportsNoBindingOrConfigMismatch()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsFalse(doc.RootElement.GetProperty("bindingMismatchDetected").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("extensionConfigMismatchDetected").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticProductFilesModifiedFalse()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    // --- Endpoint matrix fields ---

    [TestMethod]
    public void EndpointMatrixIncludesAllProbes()
    {
        using var doc = ReadJson(EndpointMatrixPath);
        foreach (var key in new[] { "tcp1270018787", "httpRuntime", "httpDebug", "wsExtensionEndpoint" })
            Assert.IsTrue(doc.RootElement.TryGetProperty(key, out _), $"matrix missing {key}");
    }

    // --- Fix summary fields ---

    [TestMethod]
    public void FixSummaryBridgeCodeNotChanged()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeCodeChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryExtensionNotChanged()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("extensionChanged").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestChanged").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryDiagnosticScriptIsReadOnly()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("diagnosticScriptIsReadOnly").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("diagnosticScriptKillsProcesses").GetBoolean());
    }

    // --- Go/No-Go fields ---

    [TestMethod]
    public void GoNoGoReadyForManualReloadQaTrue()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsTrue(doc.RootElement.GetProperty("readyForManualReloadQaAfterBridgeLivenessFix").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoReadyForReleasePublicFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoReadyForJsChangesFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoReadyForRuntimeChangesFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoRecommendsM637H()
    {
        using var doc = ReadJson(GoNoGoPath);
        AssertContains(doc.RootElement.GetProperty("recommendedNextMilestone").GetString() ?? "", "M637H");
    }

    // --- Defaults remain aligned at 127.0.0.1:8787 ---

    [TestMethod]
    public void BridgeDefaultPortIs8787()
    {
        var options = new ChromeLabOptions();
        Assert.AreEqual(8787, options.Port);
        Assert.AreEqual("127.0.0.1", options.Host);
    }

    [TestMethod]
    public void ExtensionDefaultRemains127001Port8787()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        AssertContains(serviceWorker, "DEFAULT_CONFIG = { host: '127.0.0.1', port: '8787'");
    }

    [TestMethod]
    public void BridgeExpectedPortDocumentedAs8787()
    {
        AssertContains(ReadRepoText(RunbookPath), "8787");
        AssertContains(ReadRepoText(ReportPath), "127.0.0.1:8787");
    }

    [TestMethod]
    public void WsExtensionEndpointMappedInBridge()
    {
        var program = ReadRepoText(BridgeProgramPath);
        AssertContains(program, "app.Map(\"/ws/extension\"");
        AssertContains(program, "app.UseWebSockets(");
    }

    [TestMethod]
    public void RunbookDocumentsWsExtensionEndpoint()
    {
        AssertContains(ReadRepoText(RunbookPath), "/ws/extension");
    }

    // --- Diagnostic script is non-destructive ---

    [TestMethod]
    public void LivenessScriptDoesNotKillProcessesByDefault()
    {
        var script = ReadRepoText(LivenessScriptPath);
        AssertNotContains(script, "Stop-Process");
        AssertNotContains(script, "taskkill");
        AssertNotContains(script, "Stop-Service");
    }

    [TestMethod]
    public void LivenessScriptDeclaresReadOnly()
    {
        var script = ReadRepoText(LivenessScriptPath);
        AssertContains(script, "READ-ONLY");
    }

    [TestMethod]
    public void LivenessScriptProbesAllRequiredEndpoints()
    {
        var script = ReadRepoText(LivenessScriptPath);
        AssertContains(script, "8787");
        AssertContains(script, "/ws/extension");
        AssertContains(script, "health");
        AssertContains(script, "runtime");
        AssertContains(script, "debug");
    }

    // --- Product boundary: nothing in the extension changed ---

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("D2A14687DB6E201353A100A33B72AECB3C1858C1127114979945750AB5B717AC", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("FED938DE2C42EC56F9061E2587A57338DAD1A770BBFAD2B710937BBD97D9D329", Sha256Hex(SidepanelJsPath));
    }

    [TestMethod]
    public void ServiceWorkerUnchangedFromM637E()
    {
        Assert.AreEqual("B65E0385EC96F0E96DCB3493311372A3B307C53E732235C7B3093AFE2DC39859", Sha256Hex(ServiceWorkerPath));
    }

    [TestMethod]
    public void ContentScriptUnchanged()
    {
        Assert.AreEqual("E1042E37DC884BA8B088DC7CB4D805BC5BFC72820C78DB632D520B6AD4477186", Sha256Hex(ContentScriptPath));
    }

    [TestMethod]
    public void RecipeCoreUnchanged()
    {
        Assert.AreEqual("DEA70FD162CE2F94ED29D35CD2C919AD2D62DA1810D46F49DD0CEBF63399C5F8", Sha256Hex(RecipeCorePath));
    }

    [TestMethod]
    public void CspUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        var csp = doc.RootElement.GetProperty("content_security_policy").GetProperty("extension_pages").GetString() ?? "";
        AssertContains(csp, string.Concat("ws://12", "7.0.0.1:*"));
        Assert.IsFalse(csp.Contains("ws://*", StringComparison.Ordinal), "CSP must not be relaxed to a wildcard host.");
    }

    [TestMethod]
    public void PermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        CollectionAssert.AreEqual(
            new[] { "activeTab", "scripting", "storage", "tabs", "sidePanel", "alarms" },
            doc.RootElement.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
    }
}
