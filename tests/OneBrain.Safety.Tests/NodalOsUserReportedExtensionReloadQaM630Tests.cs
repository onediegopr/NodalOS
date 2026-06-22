using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("UserReportedExtensionReloadQa")]
[TestCategory("M630")]
public sealed class NodalOsUserReportedExtensionReloadQaM630Tests
{
    private const string ResultPath = "artifacts/agent-operations/m630/user-reported-extension-reload-qa-result.json";
    private const string ReloadReportPath = "artifacts/agent-operations/m630/manual-reload-qa-after-naming-report.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m630/html-manifest-js-go-no-go-after-reload.json";
    private const string MarkdownReportPath = "docs/reports/user-reported-extension-reload-qa-m630.md";
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
    public void M630ArtifactsAndReportExist()
    {
        foreach (var path in new[] { ResultPath, ReloadReportPath, GoNoGoPath, MarkdownReportPath })
            Assert.IsTrue(TextStore.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void UserReportedReloadQaResultRecordsPositiveEvidenceWithoutInventingAttachments()
    {
        using var doc = ReadJson(ResultPath);
        var root = doc.RootElement;
        Assert.AreEqual("M630", root.GetProperty("milestone").GetString());
        Assert.AreEqual("USER_REPORTED_EXTENSION_RELOAD_QA_PASSED", root.GetProperty("decision").GetString());
        Assert.AreEqual("user-reported-manual-qa", root.GetProperty("evidenceType").GetString());
        Assert.AreEqual("probé la extensión y está perfecta", root.GetProperty("userStatement").GetString());
        Assert.IsTrue(root.GetProperty("manualReloadQaCompleted").GetBoolean());
        Assert.AreEqual("pass", root.GetProperty("extensionLoaded").GetString());
        Assert.AreEqual("pass", root.GetProperty("extensionReloaded").GetString());
        Assert.AreEqual("pass", root.GetProperty("sidepanelOpened").GetString());
        Assert.AreEqual("pass", root.GetProperty("chromeExtensionNameShowsNodalOs").GetString());
        Assert.AreEqual("fail", root.GetProperty("chromeExtensionNameShowsNexa").GetString());
        Assert.AreEqual("fail", root.GetProperty("visibleNexaTextRemaining").GetString());
        Assert.AreEqual("unknown", root.GetProperty("consoleCriticalErrors").GetString());
        Assert.IsFalse(root.GetProperty("screenshotsProvided").GetBoolean());
        Assert.IsFalse(root.GetProperty("consoleLogsProvided").GetBoolean());
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
        Assert.IsTrue(root.GetProperty("readyForHtmlMinimumPatchCandidate").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForAdditionalManifestChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
    }

    [TestMethod]
    public void ManualReloadReportSummarizesUserReportedEvidenceOnly()
    {
        using var doc = ReadJson(ReloadReportPath);
        var root = doc.RootElement;
        var checks = root.GetProperty("checks");
        Assert.AreEqual("M630", root.GetProperty("milestone").GetString());
        Assert.AreEqual("user-reported-manual-qa", root.GetProperty("evidenceType").GetString());
        Assert.AreEqual("probé la extensión y está perfecta", root.GetProperty("userStatement").GetString());
        Assert.IsFalse(root.GetProperty("screenshotsProvided").GetBoolean());
        Assert.IsFalse(root.GetProperty("consoleLogsProvided").GetBoolean());
        Assert.IsTrue(root.GetProperty("manualReloadQaCompleted").GetBoolean());
        Assert.AreEqual("pass", checks.GetProperty("extensionLoaded").GetString());
        Assert.AreEqual("pass", checks.GetProperty("extensionReloaded").GetString());
        Assert.AreEqual("pass", checks.GetProperty("sidepanelOpened").GetString());
        Assert.AreEqual("pass", checks.GetProperty("chromeExtensionNameShowsNodalOs").GetString());
        Assert.AreEqual("fail", checks.GetProperty("chromeExtensionNameShowsNexa").GetString());
        Assert.AreEqual("fail", checks.GetProperty("visibleNexaTextRemaining").GetString());
        Assert.AreEqual("unknown", checks.GetProperty("consoleCriticalErrors").GetString());
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsOnlyHtmlMinimumPatchAsFutureCandidate()
    {
        using var doc = ReadJson(GoNoGoPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("manifestMinimumNamingCleanupVerified").GetBoolean());
        Assert.IsTrue(root.GetProperty("readyForHtmlMinimumPatchCandidate").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForAdditionalManifestChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeBeforeJs").GetBoolean());
        Assert.AreEqual("M631 Installed Extension Visual QA Closeout / Claude Audit Prep", root.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void ProductFilesRemainUnchangedFromM629()
    {
        Assert.AreEqual("298BEE3E6AAE130369CDDCF63476E7B8356842205788FECF1666E96D58AB95D8", Sha256Hex(ManifestPath));
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("D2A14687DB6E201353A100A33B72AECB3C1858C1127114979945750AB5B717AC", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("204D325980AE88619546B47F7D196FB66041C233B2DB040EBBD497AF337823D2", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("F479C04C342E922EA23928B9E2857FEFDDF69792959E33A31D3A7D3A28534CEC", Sha256Hex(BackgroundServicePath()));
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
    public void NewM630FilesDoNotContainForbiddenProductMarkers()
    {
        foreach (var path in new[] { ResultPath, ReloadReportPath, GoNoGoPath, MarkdownReportPath, "tests/OneBrain.Safety.Tests/NodalOsUserReportedExtensionReloadQaM630Tests.cs" })
        {
            var text = ReadRepoText(path);
            AssertDoesNotContain(text, "BrowserExecutor.C" + "dp");
            AssertDoesNotContain(text, "Http" + "Client");
            AssertDoesNotContain(text, "Client" + "WebSocket");
            AssertDoesNotContain(text, "Process." + "Start");
            AssertDoesNotContain(text, "sche" + "duler");
            AssertDoesNotContain(text, "wor" + "ker");
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
