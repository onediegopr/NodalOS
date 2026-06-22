using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SwVisibleStringsCleanup")]
[TestCategory("M635")]
public sealed class NodalOsSwVisibleStringsCleanupM635Tests
{
    private const string SummaryPath = "artifacts/agent-operations/m635/sw-visible-strings-cleanup-summary.json";
    private const string BeforeAfterPath = "artifacts/agent-operations/m635/sw-visible-strings-before-after.json";
    private const string ProtectedCompatPath = "artifacts/agent-operations/m635/protected-compat-keys-confirmation.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m635/post-sw-visible-strings-go-no-go.json";
    private const string ReportPath = "docs/reports/sw-visible-strings-cleanup-m635.md";
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

    private static void AssertDoesNotContain(string haystack, string needle) =>
        Assert.IsFalse(haystack.Contains(needle, StringComparison.Ordinal), $"Found unexpected '{needle}'.");

    [TestMethod]
    public void SummaryArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(SummaryPath)), SummaryPath);
    }

    [TestMethod]
    public void BeforeAfterArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(BeforeAfterPath)), BeforeAfterPath);
    }

    [TestMethod]
    public void ProtectedCompatKeysArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ProtectedCompatPath)), ProtectedCompatPath);
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
    public void SummaryDeclaresStringLiteralOnly()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("stringLiteralOnly").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresVisibleMessagesOnly()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("visibleMessagesOnly").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresStorageKeysUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("storageKeysChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresProtocolUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("protocolChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresMessageTypesUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("messageTypesChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresPortNamesUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("portNamesChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresAlarmNamesUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("alarmNamesChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresWebSocketUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("webSocketChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresRuntimeLogicUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeLogicChanged").GetBoolean());
    }

    [TestMethod]
    public void ProtectedCompatArtifactConfirmsNexaRecipesPreserved()
    {
        using var doc = ReadJson(ProtectedCompatPath);
        Assert.IsTrue(doc.RootElement.GetProperty("nexaRecipesPreserved").GetBoolean());
    }

    [TestMethod]
    public void ProtectedCompatArtifactConfirmsNexaLearningDraftPreserved()
    {
        using var doc = ReadJson(ProtectedCompatPath);
        Assert.IsTrue(doc.RootElement.GetProperty("nexaLearningDraftPreserved").GetBoolean());
    }

    [TestMethod]
    public void ProtectedCompatArtifactConfirmsNexaRuntimeStatePreserved()
    {
        using var doc = ReadJson(ProtectedCompatPath);
        Assert.IsTrue(doc.RootElement.GetProperty("nexaRuntimeStatePreserved").GetBoolean());
    }

    [TestMethod]
    public void ProtectedCompatArtifactConfirmsNexaKeepalivePreserved()
    {
        using var doc = ReadJson(ProtectedCompatPath);
        Assert.IsTrue(doc.RootElement.GetProperty("nexaKeepalivePreserved").GetBoolean());
    }

    [TestMethod]
    public void ProtectedCompatArtifactConfirmsNexaContentPingPreserved()
    {
        using var doc = ReadJson(ProtectedCompatPath);
        Assert.IsTrue(doc.RootElement.GetProperty("nexaContentPingPreserved").GetBoolean());
    }

    [TestMethod]
    public void ProtectedCompatArtifactConfirmsOnebrainSidepanelPortPreserved()
    {
        using var doc = ReadJson(ProtectedCompatPath);
        Assert.IsTrue(doc.RootElement.GetProperty("onebrainSidepanelPortPreserved").GetBoolean());
    }

    [TestMethod]
    public void ProtectedCompatArtifactConfirmsProtocolVersionPreserved()
    {
        using var doc = ReadJson(ProtectedCompatPath);
        Assert.IsTrue(doc.RootElement.GetProperty("protocolVersionPreserved").GetBoolean());
    }

    [TestMethod]
    public void ServiceWorkerNoLongerContainsNexaCanOperate()
    {
        AssertDoesNotContain(ReadRepoText(ServiceWorkerPath), "NEXA can operate");
    }

    [TestMethod]
    public void ServiceWorkerNoLongerContainsVisibleSpanishNexaPhrases()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        AssertDoesNotContain(serviceWorker, "NEXA llego");
        AssertDoesNotContain(serviceWorker, "NEXA se detuvo");
        AssertDoesNotContain(serviceWorker, "NEXA lo grabe");
        AssertDoesNotContain(serviceWorker, "NEXA vuelve");
        AssertDoesNotContain(serviceWorker, "NEXA sigue viendo");
        AssertDoesNotContain(serviceWorker, "NEXA espera");
        AssertDoesNotContain(serviceWorker, "NEXA reobservo");
        AssertDoesNotContain(serviceWorker, "NEXA sigue esperando");
    }

    [TestMethod]
    public void ServiceWorkerStillContainsProtectedCompatKeys()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        AssertContains(serviceWorker, "nexaRecipes");
        AssertContains(serviceWorker, "nexaLearningDraft");
        AssertContains(serviceWorker, "nexaRuntimeState");
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(serviceWorker, "nexa.content.ping");
        AssertContains(serviceWorker, "PROTOCOL_VERSION = 'chrome-lab-v1'");
        AssertContains(ReadRepoText(SidepanelJsPath), "onebrain-sidepanel");
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
        Assert.AreEqual("204D325980AE88619546B47F7D196FB66041C233B2DB040EBBD497AF337823D2", Sha256Hex(SidepanelJsPath));
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
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
    public void ServiceWorkerHasM635Baseline()
    {
        Assert.AreEqual("546AAF381024B5F784F28A94A57F05948AECA92F6BFF174F577D22B4120A655F", Sha256Hex(ServiceWorkerPath));
    }

    [TestMethod]
    public void ManifestPermissionsUnchanged()
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
    public void ProtocolStorageKeysPortAndAlarmUnchanged()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        AssertContains(serviceWorker, "PROTOCOL_VERSION = 'chrome-lab-v1'");
        AssertContains(serviceWorker, "nexaRecipes");
        AssertContains(serviceWorker, "nexaLearningDraft");
        AssertContains(serviceWorker, "nexaRuntimeState");
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(serviceWorker, "nexa.content.ping");
        AssertContains(ReadRepoText(SidepanelJsPath), "onebrain-sidepanel");
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
}
