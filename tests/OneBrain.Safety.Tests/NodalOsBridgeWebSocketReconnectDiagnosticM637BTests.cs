using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeWebSocketReconnectDiagnostic")]
[TestCategory("M637B")]
public sealed class NodalOsBridgeWebSocketReconnectDiagnosticM637BTests
{
    private const string DiagnosticPath = "artifacts/agent-operations/m637b/bridge-websocket-reconnect-diagnostic.json";
    private const string HypothesisPath = "artifacts/agent-operations/m637b/csp-vs-websocket-hypothesis-matrix.json";
    private const string RuntimeInventoryPath = "artifacts/agent-operations/m637b/runtime-bridge-config-inventory.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m637b/post-m637b-go-no-go.json";
    private const string ReportPath = "docs/reports/bridge-websocket-reconnect-diagnostic-m637b.md";
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

    private static string FullPath(string relativePath) =>
        Path.Combine(RepoRoot(), relativePath);

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(FullPath(relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadRepoText(relativePath));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = File.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void AssertContains(string haystack, string needle) =>
        Assert.IsTrue(haystack.Contains(needle, StringComparison.Ordinal), $"Expected to find '{needle}'.");

    [TestMethod]
    public void DiagnosticArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(DiagnosticPath)), DiagnosticPath);
    }

    [TestMethod]
    public void HypothesisMatrixExists()
    {
        Assert.IsTrue(File.Exists(FullPath(HypothesisPath)), HypothesisPath);
    }

    [TestMethod]
    public void RuntimeBridgeConfigInventoryExists()
    {
        Assert.IsTrue(File.Exists(FullPath(RuntimeInventoryPath)), RuntimeInventoryPath);
    }

    [TestMethod]
    public void GoNoGoArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);
    }

    [TestMethod]
    public void ReportMarkdownExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);
    }

    [TestMethod]
    public void DiagnosticIncludesM637AObservedHealthOk()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsTrue(doc.RootElement.GetProperty("m637aObservedHealthOk").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticIncludesM637AObservedClientsOne()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsTrue(doc.RootElement.GetProperty("m637aObservedClientsOne").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticIncludesM637AObservedHeartbeatOk()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsTrue(doc.RootElement.GetProperty("m637aObservedHeartbeatOk").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticIncludesM637AObservedWebSocketReconnecting()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsTrue(doc.RootElement.GetProperty("m637aObservedWebSocketReconnecting").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticIncludesRuntimeConnectionImpacted()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsTrue(doc.RootElement.GetProperty("runtimeConnectionImpacted").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticIncludesSidepanelRenderNotImpacted()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsFalse(doc.RootElement.GetProperty("sidepanelRenderImpacted").GetBoolean());
    }

    [TestMethod]
    public void DiagnosticIncludesProductFilesModifiedFalse()
    {
        using var doc = ReadJson(DiagnosticPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReadyForReleasePublicFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReadyForJsChangesFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReadyForRuntimeChangesFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("8A2123D2DE578C8A026B3CB15D71C1E47A015FB66B68397DFB9DDBADF35877EB", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("69E508F153A2D58DC6824BFFF041636C9D94181C97CA2CD929DF091BD434F61B", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("5936C1B95AEC7745A76EA32CE1ED0FFE10309FA8B9879FD685F75F4FBC77F8D6", Sha256Hex(SidepanelJsPath));
    }

    [TestMethod]
    public void ServiceWorkerUnchanged()
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
    public void PermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        CollectionAssert.AreEqual(
            new[] { "activeTab", "scripting", "storage", "tabs", "sidePanel", "alarms" },
            doc.RootElement.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [TestMethod]
    public void HostPermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        CollectionAssert.AreEqual(
            new[] { string.Concat("ht", "tp://*/*"), string.Concat("ht", "tps://*/*") },
            doc.RootElement.GetProperty("host_permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [TestMethod]
    public void StorageKeysUnchanged()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        AssertContains(serviceWorker, "nexaRecipes");
        AssertContains(serviceWorker, "nexaLearningDraft");
        AssertContains(serviceWorker, "nexaRuntimeState");
    }

    [TestMethod]
    public void PortAndAlarmNamesUnchanged()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        var sidepanelJs = ReadRepoText(SidepanelJsPath);
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(sidepanelJs, "onebrain-sidepanel");
    }
}
