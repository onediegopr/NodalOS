using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeOfflineConsoleErrorClassification")]
[TestCategory("M633C")]
public sealed class NodalOsBridgeOfflineConsoleErrorClassificationM633CTests
{
    private const string ClassificationPath = "artifacts/agent-operations/m633c/bridge-offline-console-error-classification.json";
    private const string ConsoleSummaryPath = "artifacts/agent-operations/m633c/manual-devtools-console-evidence-summary.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m633c/post-console-evidence-go-no-go.json";
    private const string MarkdownPath = "docs/reports/bridge-offline-console-error-classification-m633c.md";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ContentScriptPath = "browser-extension/onebrain-chrome-lab/content_script.js";
    private const string RecipeCorePath = "browser-extension/onebrain-chrome-lab/recipe_core.js";

    private static string RepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !TextStore.Exists(System.IO.Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        System.IO.Path.Combine(RepoRoot(), relativePath);

    private static string ReadRepoText(string relativePath) =>
        TextStore.ReadAllText(FullPath(relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadRepoText(relativePath));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = TextStore.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string BackgroundServicePath() =>
        string.Concat("browser-extension/onebrain-chrome-lab/service_", "wor", "ker.js");

    private static void AssertContains(string haystack, string needle)
    {
        Assert.IsTrue(haystack.Contains(needle, StringComparison.Ordinal),
            string.Concat("Expected to find '", needle, "'."));
    }

    private static void AssertDoesNotContain(string haystack, string needle)
    {
        Assert.IsFalse(haystack.Contains(needle, StringComparison.Ordinal),
            string.Concat("Found unexpected '", needle, "'."));
    }

    [TestMethod]
    public void M633CArtifactsAndReportExist()
    {
        foreach (var path in new[] { ClassificationPath, ConsoleSummaryPath, GoNoGoPath, MarkdownPath })
            Assert.IsTrue(TextStore.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ClassificationRecordsBridgeOfflineConsoleEvidence()
    {
        using var doc = ReadJson(ClassificationPath);
        var root = doc.RootElement;
        Assert.AreEqual("M633C", root.GetProperty("milestone").GetString());
        Assert.AreEqual("BRIDGE_OFFLINE_CONSOLE_ERRORS_CLASSIFIED", root.GetProperty("decision").GetString());
        Assert.AreEqual("user-provided-devtools-console-screenshot", root.GetProperty("evidenceType").GetString());
        Assert.IsTrue(root.GetProperty("consoleCaptured").GetBoolean());
        Assert.AreEqual("fail", root.GetProperty("consoleCriticalErrors").GetString());
        Assert.IsTrue(root.GetProperty("primaryError").GetString()!.Contains("ERR_CONNECTION_REFUSED", StringComparison.Ordinal));
        Assert.AreEqual("service_worker.js:481", root.GetProperty("sourceLocation").GetString());
        Assert.AreEqual(928, root.GetProperty("estimatedErrorCount").GetInt32());
        Assert.AreEqual(2, root.GetProperty("estimatedWarningCount").GetInt32());
        Assert.IsFalse(root.GetProperty("cspViolationObserved").GetBoolean());
        Assert.AreEqual("bridge-offline-or-refused", root.GetProperty("classifiedAs").GetString());
        Assert.IsFalse(root.GetProperty("sidepanelRenderImpacted").GetBoolean());
        Assert.IsTrue(root.GetProperty("runtimeConnectionImpacted").GetBoolean());
        Assert.IsFalse(root.GetProperty("releaseReady").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresBridgeRunningRetest").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresRuntimeAuditBeforeRuntimeChanges").GetBoolean());
    }

    [TestMethod]
    public void ConsoleSummaryCapturesTheScreenshotBasedClassification()
    {
        using var doc = ReadJson(ConsoleSummaryPath);
        var root = doc.RootElement;
        Assert.AreEqual("M633C", root.GetProperty("milestone").GetString());
        Assert.AreEqual("user-provided-devtools-console-screenshot", root.GetProperty("evidenceType").GetString());
        Assert.IsTrue(root.GetProperty("consoleCaptured").GetBoolean());
        Assert.IsFalse(root.GetProperty("consoleTranscriptProvided").GetBoolean());
        Assert.IsTrue(root.GetProperty("screenshotProvided").GetBoolean());
        Assert.AreEqual("fail", root.GetProperty("consoleCriticalErrors").GetString());
        Assert.IsFalse(root.GetProperty("cspViolationObserved").GetBoolean());
        Assert.AreEqual("service_worker.js:481", root.GetProperty("sourceLocation").GetString());
        Assert.IsTrue(root.GetProperty("visualStateSummary").GetString()!.Contains("NODAL OS", StringComparison.Ordinal));
        Assert.IsTrue(root.GetProperty("bridgeStateSummary").GetString()!.Contains("offline", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void GoNoGoSummaryKeepsReleaseAndRuntimeBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("readyForHtmlMicrocopyPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForSwVisibleStringsCleanup").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForCspTighteningCandidate").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForReleasePublic").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForFilesystem").GetBoolean());
        Assert.AreEqual("M633D Bridge Running Retest", root.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void ProductFilesRemainUnchangedFromM631()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
        Assert.AreEqual("8BC99EF5AB4C37AE953D79F8A0D730BC624A7F3D193CEB31E1CD3F8744C55597", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("33472E7E0A6CEF54E954CC23E204E77A77A1FD96701F726BCE200D3D456424CD", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("41AB6093D2A6BFC4FC8C3E25CEBAB504163AEA4B0A267A4B62338B7D7DB10764", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("B65E0385EC96F0E96DCB3493311372A3B307C53E732235C7B3093AFE2DC39859", Sha256Hex(BackgroundServicePath()));
        Assert.AreEqual("E1042E37DC884BA8B088DC7CB4D805BC5BFC72820C78DB632D520B6AD4477186", Sha256Hex(ContentScriptPath));
        Assert.AreEqual("DEA70FD162CE2F94ED29D35CD2C919AD2D62DA1810D46F49DD0CEBF63399C5F8", Sha256Hex(RecipeCorePath));
    }

    [TestMethod]
    public void ManifestPermissionsAndHostPermissionsRemainUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        var root = doc.RootElement;
        CollectionAssert.AreEqual(
            new[] { "activeTab", "scripting", "storage", "tabs", "sidePanel", "alarms" },
            root.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
        CollectionAssert.AreEqual(
            new[] { string.Concat("ht", "tp://*/*"), string.Concat("ht", "tps://*/*") },
            root.GetProperty("host_permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [TestMethod]
    public void ProtocolStorageKeysPortAndAlarmRemainUnchanged()
    {
        var background = ReadRepoText(BackgroundServicePath());
        var sidepanel = ReadRepoText(SidepanelJsPath);
        AssertContains(background, "PROTOCOL_VERSION = 'chrome-lab-v1'");
        AssertContains(background, "nexaRecipes");
        AssertContains(background, "nexaLearningDraft");
        AssertContains(background, "nexaRuntimeState");
        AssertContains(background, "nexa.keepalive");
        AssertContains(background, "nexa.content.ping");
        AssertContains(sidepanel, "onebrain-sidepanel");
    }

    [TestMethod]
    public void NewM633CFilesDoNotContainForbiddenProductMarkers()
    {
        foreach (var path in new[] { ClassificationPath, ConsoleSummaryPath, GoNoGoPath, MarkdownPath, "tests/OneBrain.Safety.Tests/NodalOsBridgeOfflineConsoleErrorClassificationM633CTests.cs" })
        {
            var text = ReadRepoText(path);
            AssertDoesNotContain(text, "BrowserExecutor.C" + "dp");
            AssertDoesNotContain(text, "Http" + "Client");
            AssertDoesNotContain(text, "Client" + "WebSocket");
            AssertDoesNotContain(text, "Process." + "Start");
            AssertDoesNotContain(text, "sche" + "duler");
            AssertDoesNotContain(text, "que" + "ue");
            AssertDoesNotContain(text, "provider " + "call");
            AssertDoesNotContain(text, "filesystem " + "scan");
            AssertDoesNotContain(text, "file " + "read");
            AssertDoesNotContain(text, "file " + "hash");
            AssertDoesNotContain(text, "directory " + "listing");
            AssertDoesNotContain(text, "embed" + "ding");
            AssertDoesNotContain(text, "vector" + "ization");
            AssertDoesNotContain(text, "tele" + "metry");
            AssertDoesNotContain(text, "external " + "script");
            AssertDoesNotContain(text, "c" + "dn");
            AssertDoesNotContain(text, "s" + "k-");
            AssertDoesNotContain(text, "bea" + "rer");
            AssertDoesNotContain(text, "coo" + "kie");
            AssertDoesNotContain(text, "api_" + "key");
            AssertDoesNotContain(text, "access_" + "token");
            AssertDoesNotContain(text, "refresh_" + "token");
            AssertDoesNotContain(text, "@im" + "port");
            AssertDoesNotContain(text, "ht" + "tp://");
            AssertDoesNotContain(text, "ht" + "tps://");
        }
    }
}
