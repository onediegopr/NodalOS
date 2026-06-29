using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeWebSocketReconnect")]
[TestCategory("NodalOsBridgeWebSocketReconnect")]
[TestCategory("M637E")]
public sealed class NodalOsBridgeWebSocketReconnectFollowupFixM637ETests
{
    private const string RootCausePath = "artifacts/agent-operations/m637e/bridge-websocket-reconnect-followup-root-cause.json";
    private const string FixSummaryPath = "artifacts/agent-operations/m637e/bridge-websocket-reconnect-followup-fix-summary.json";
    private const string CloseReasonPath = "artifacts/agent-operations/m637e/bridge-websocket-close-reason-evidence.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m637e/post-m637e-go-no-go.json";
    private const string ReportPath = "docs/reports/bridge-websocket-reconnect-followup-fix-m637e.md";
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

    // --- Artifact + report existence ---

    [TestMethod]
    public void RootCauseArtifactExists() => Assert.IsTrue(File.Exists(FullPath(RootCausePath)), RootCausePath);

    [TestMethod]
    public void FixSummaryArtifactExists() => Assert.IsTrue(File.Exists(FullPath(FixSummaryPath)), FixSummaryPath);

    [TestMethod]
    public void CloseReasonEvidenceArtifactExists() => Assert.IsTrue(File.Exists(FullPath(CloseReasonPath)), CloseReasonPath);

    [TestMethod]
    public void GoNoGoArtifactExists() => Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void ReportMarkdownExists() => Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    // --- Root cause fields ---

    [TestMethod]
    public void RootCauseDecisionIsFixReady()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.AreEqual("BRIDGE_WEBSOCKET_RECONNECT_FIX_READY", doc.RootElement.GetProperty("decision").GetString());
    }

    [TestMethod]
    public void RootCauseRaceConditionWasRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsTrue(doc.RootElement.GetProperty("raceConditionWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseCspWasNotRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsFalse(doc.RootElement.GetProperty("cspWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseBridgeWsHandlingWasNotRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeWsHandlingWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseTokenAuthWasNotRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsFalse(doc.RootElement.GetProperty("tokenAuthWasRootCause").GetBoolean());
    }

    [TestMethod]
    public void RootCauseHeartbeatWasNotRootCause()
    {
        using var doc = ReadJson(RootCausePath);
        Assert.IsFalse(doc.RootElement.GetProperty("heartbeatWasRootCause").GetBoolean());
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
    public void FixSummaryCspChangedFalse()
    {
        using var doc = ReadJson(FixSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("cspChanged").GetBoolean());
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

    // --- Close reason evidence fields ---

    [TestMethod]
    public void CloseReasonEvidenceConfirmsBridgeNotCloseSource()
    {
        using var doc = ReadJson(CloseReasonPath);
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeCloseBehavior")
            .GetProperty("everyBridgeInitiatedCloseIsPrecededByProtocolErrorMessage").GetBoolean());
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

    [TestMethod]
    public void GoNoGoRecommendsM637F()
    {
        using var doc = ReadJson(GoNoGoPath);
        AssertContains(doc.RootElement.GetProperty("recommendedNextMilestone").GetString() ?? "", "M637F");
    }

    // --- Regression: the actual fix is present in service_worker.js ---

    [TestMethod]
    public void ServiceWorkerDeclaresConnectInFlightGuard()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        AssertContains(source, "let connectInFlight = false;");
        AssertContains(source, "if (connectInFlight) {");
        AssertContains(source, "connectInFlight = true;");
    }

    [TestMethod]
    public void ServiceWorkerGuardsValidateAwaitAgainstReentry()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        // connectInFlight must be set before the validateConnectionConfig await
        var guardIndex = source.IndexOf("connectInFlight = true;", StringComparison.Ordinal);
        var awaitIndex = source.IndexOf("await validateConnectionConfig(config)", StringComparison.Ordinal);
        Assert.IsTrue(guardIndex >= 0, "connectInFlight = true; not found");
        Assert.IsTrue(awaitIndex >= 0, "await validateConnectionConfig not found");
        Assert.IsTrue(guardIndex < awaitIndex, "connectInFlight must be set before the validateConnectionConfig await");
    }

    [TestMethod]
    public void ServiceWorkerUsesActiveSocketGuard()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        AssertContains(source, "const activeSocket = new WebSocket(url);");
        AssertContains(source, "socket = activeSocket;");
        AssertContains(source, "if (socket !== activeSocket) {");
    }

    [TestMethod]
    public void ServiceWorkerResetsConnectInFlightInTeardown()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        // Must reset the flag when the socket settles or the connection is torn down,
        // otherwise the guard could deadlock future reconnects.
        var resets = source.Split("connectInFlight = false;").Length - 1;
        Assert.IsTrue(resets >= 4, $"Expected connectInFlight reset in open/close/error/teardown paths, found {resets}");
    }

    [TestMethod]
    public void ServiceWorkerStillUsesEffectiveTokenFromM637C()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        AssertContains(source, "effectiveToken = await validateConnectionConfig(config)");
        AssertContains(source, "token: effectiveToken || ''");
    }

    [TestMethod]
    public void ServiceWorkerHasM637EBaseline()
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
    public void ContentScriptUnchanged()
    {
        Assert.AreEqual("E1042E37DC884BA8B088DC7CB4D805BC5BFC72820C78DB632D520B6AD4477186", Sha256Hex(ContentScriptPath));
    }

    [TestMethod]
    public void RecipeCoreUnchanged()
    {
        Assert.AreEqual("DEA70FD162CE2F94ED29D35CD2C919AD2D62DA1810D46F49DD0CEBF63399C5F8", Sha256Hex(RecipeCorePath));
    }

    // --- Compat keys + CSP ---

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
    public void CspLoopbackUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        var csp = doc.RootElement.GetProperty("content_security_policy").GetProperty("extension_pages").GetString() ?? "";
        AssertContains(csp, string.Concat("ws://12", "7.0.0.1:*"));
        Assert.IsFalse(csp.Contains("ws://*", StringComparison.Ordinal), "CSP must not be relaxed to a wildcard host.");
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
    public void PortAlarmAndProtocolNamesUnchanged()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        var sidepanelJs = ReadRepoText(SidepanelJsPath);
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(serviceWorker, "nexa.content.ping");
        AssertContains(serviceWorker, "PROTOCOL_VERSION = 'chrome-lab-v1'");
        AssertContains(sidepanelJs, "onebrain-sidepanel");
    }

    [TestMethod]
    public void NoAutonomousExecutionOpened()
    {
        var source = ReadRepoText(ServiceWorkerPath);
        AssertContains(source, "serviceWorkerRunOwner: false");
        AssertContains(source, "CORE_GOVERNED_MODE = true");
        AssertContains(source, "LEGACY_RUNNER_ENABLED = false");
        AssertContains(source, "contentScriptAuthoritative: false");
    }
}
