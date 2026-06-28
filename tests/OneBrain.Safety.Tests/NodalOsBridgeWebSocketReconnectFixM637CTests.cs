using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeWebSocketReconnect")]
[TestCategory("NodalOsBridgeWebSocketReconnect")]
[TestCategory("M637C")]
public sealed class NodalOsBridgeWebSocketReconnectFixM637CTests
{
    private const string RootCausePath = "artifacts/agent-operations/m637c/bridge-websocket-reconnect-root-cause.json";
    private const string FixSummaryPath = "artifacts/agent-operations/m637c/bridge-websocket-reconnect-fix-summary.json";
    private const string HandshakeRegressionPath = "artifacts/agent-operations/m637c/bridge-websocket-handshake-regression.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m637c/post-bridge-reconnect-fix-go-no-go.json";
    private const string ReportPath = "docs/reports/bridge-websocket-reconnect-fix-m637c.md";
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

    // --- Artifact existence ---

    [TestMethod]
    public void RootCauseArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(RootCausePath)), RootCausePath);
    }

    [TestMethod]
    public void FixSummaryArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(FixSummaryPath)), FixSummaryPath);
    }

    [TestMethod]
    public void HandshakeRegressionArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(HandshakeRegressionPath)), HandshakeRegressionPath);
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

    // --- Root cause artifact fields ---

    [TestMethod]
    public void RootCauseDecisionIsFixReady()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.AreEqual("BRIDGE_WEBSOCKET_RECONNECT_FIX_READY", doc.RootElement.GetProperty("decision").GetString());
    }

    [TestMethod]
    public void RootCauseCspWasNotRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsFalse(doc.RootElement.GetProperty("cspWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseTokenAuthWasRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsTrue(doc.RootElement.GetProperty("tokenAuthWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseHeartbeatWasNotRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsFalse(doc.RootElement.GetProperty("heartbeatWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseStaleStateWasNotRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsFalse(doc.RootElement.GetProperty("staleStateWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseProductFilesModifiedTrue()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsTrue(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    // --- Fix summary fields ---

    [TestMethod]
    public void FixSummaryServiceWorkerChangedTrue()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("serviceWorkerChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryBridgeChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryManifestChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("manifestChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryPermissionsChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryHostPermissionsChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryStorageKeysChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("storageKeysChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryProtocolVersionChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("protocolVersionChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryPortNamesChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("portNamesChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryAlarmNamesChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("alarmNamesChanged").GetBoolean());
    }

    [TestMethod]
    public void FixSummaryRuntimeArchitectureChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeArchitectureChanged").GetBoolean());
    }

    // --- Go/No-Go fields ---

    [TestMethod]
    public void GoNoGoReadyForManualReloadQaTrue()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsTrue(doc.RootElement.GetProperty("readyForManualReloadQaAfterFix").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoReadyForReleaseEvidenceGateFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
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

    // --- Service worker fix verification ---

    [TestMethod]
    public void ServiceWorkerUsesEffectiveTokenInHello()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        // M637E restructured the await into a try/catch; the invariant that the
        // effective token (not config.token) is sent in extension.hello still holds.
        AssertContains(source, "effectiveToken = await validateConnectionConfig(config)");
        AssertContains(source, "token: effectiveToken || ''");
    }

    [TestMethod]
    public void ServiceWorkerValidateConnectionConfigRequiresTokenBeforeWebSocket()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        AssertContains(source, "throw new Error('token_required');");
        AssertContains(source, "blockReconnect('tokenRequired', 'token_required')");
        AssertContains(source, "return token;");
    }

    [TestMethod]
    public void ServiceWorkerHasM637CBaseline()
    {
        Assert.AreEqual("B65E0385EC96F0E96DCB3493311372A3B307C53E732235C7B3093AFE2DC39859", Sha256Hex(ServiceWorkerPath));
    }

    // --- Product boundary: unchanged files ---

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("8BC99EF5AB4C37AE953D79F8A0D730BC624A7F3D193CEB31E1CD3F8744C55597", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("33472E7E0A6CEF54E954CC23E204E77A77A1FD96701F726BCE200D3D456424CD", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("41AB6093D2A6BFC4FC8C3E25CEBAB504163AEA4B0A267A4B62338B7D7DB10764", Sha256Hex(SidepanelJsPath));
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

    // --- Compat keys ---

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
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(serviceWorker, "PROTOCOL_VERSION = 'chrome-lab-v1'");
    }

    [TestMethod]
    public void SidepanelPortNameUnchanged()
    {
        var sidepanelJs = ReadRepoText(SidepanelJsPath);
        AssertContains(sidepanelJs, "onebrain-sidepanel");
    }

    // --- No autonomous execution / no capabilities opened ---

    [TestMethod]
    public void ServiceWorkerRunOwnerRemainsDisabled()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        AssertContains(source, "serviceWorkerRunOwner: false");
        AssertContains(source, "CORE_GOVERNED_MODE = true");
        AssertContains(source, "LEGACY_RUNNER_ENABLED = false");
    }
}
