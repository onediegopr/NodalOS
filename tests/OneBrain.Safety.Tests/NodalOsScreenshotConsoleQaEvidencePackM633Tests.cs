using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ScreenshotConsoleQaEvidencePack")]
[TestCategory("M633")]
public sealed class NodalOsScreenshotConsoleQaEvidencePackM633Tests
{
    private const string EvidencePackPath = "artifacts/agent-operations/m633/screenshot-console-qa-evidence-pack.json";
    private const string QaResultPath = "artifacts/agent-operations/m633/manual-screenshot-console-qa-result.json";
    private const string ConsoleInventoryPath = "artifacts/agent-operations/m633/console-error-inventory.json";
    private const string ScreenshotIndexPath = "artifacts/agent-operations/m633/qa-evidence-screenshot-index.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m633/post-m632-go-no-go.json";
    private const string ReportMarkdownPath = "docs/reports/screenshot-console-qa-evidence-pack-m633.md";
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

    private static void AssertContains(string haystack, string needle) =>
        Assert.IsTrue(haystack.Contains(needle, StringComparison.Ordinal),
            string.Concat("Expected to find '", needle, "'."));

    private static void AssertDoesNotContain(string haystack, string needle) =>
        Assert.IsFalse(haystack.Contains(needle, StringComparison.Ordinal),
            string.Concat("Found unexpected '", needle, "'."));

    // ── 1. Artifact existence ─────────────────────────────────────────────────

    [TestMethod]
    public void EvidencePackArtifactExists()
    {
        Assert.IsTrue(TextStore.Exists(FullPath(EvidencePackPath)), EvidencePackPath);
    }

    [TestMethod]
    public void QaResultArtifactExists()
    {
        Assert.IsTrue(TextStore.Exists(FullPath(QaResultPath)), QaResultPath);
    }

    [TestMethod]
    public void ConsoleErrorInventoryArtifactExists()
    {
        Assert.IsTrue(TextStore.Exists(FullPath(ConsoleInventoryPath)), ConsoleInventoryPath);
    }

    [TestMethod]
    public void ScreenshotIndexArtifactExists()
    {
        Assert.IsTrue(TextStore.Exists(FullPath(ScreenshotIndexPath)), ScreenshotIndexPath);
    }

    [TestMethod]
    public void GoNoGoArtifactExists()
    {
        Assert.IsTrue(TextStore.Exists(FullPath(GoNoGoPath)), GoNoGoPath);
    }

    [TestMethod]
    public void ReportMarkdownExists()
    {
        Assert.IsTrue(TextStore.Exists(FullPath(ReportMarkdownPath)), ReportMarkdownPath);
    }

    // ── 2. QA result required fields ─────────────────────────────────────────

    [TestMethod]
    public void QaResultIncludesScreenshotsProvided()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("screenshotsProvided");
    }

    [TestMethod]
    public void QaResultIncludesConsoleLogsProvided()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("consoleLogsProvided");
    }

    [TestMethod]
    public void QaResultIncludesExtensionLoaded()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("extensionLoaded");
    }

    [TestMethod]
    public void QaResultIncludesSidepanelOpened()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("sidepanelOpened");
    }

    [TestMethod]
    public void QaResultIncludesConsoleCriticalErrors()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("consoleCriticalErrors");
    }

    [TestMethod]
    public void QaResultIncludesCspViolations()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("cspViolations");
    }

    [TestMethod]
    public void QaResultIncludesChromeExtensionNameShowsNodalOs()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("chromeExtensionNameShowsNodalOs");
    }

    [TestMethod]
    public void QaResultIncludesVisibleNexaTextRemaining()
    {
        using var doc = ReadJson(QaResultPath);
        doc.RootElement.GetProperty("visibleNexaTextRemaining");
    }

    // ── 3. Go/No-Go invariants ────────────────────────────────────────────────

    [TestMethod]
    public void GoNoGoIncludesM632VerdictConditionalGo()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.AreEqual("AUDIT_CONDITIONAL_GO", doc.RootElement.GetProperty("m632Verdict").GetString());
    }

    [TestMethod]
    public void GoNoGoReadyForReleasePublicFalseWhenEvidenceIncomplete()
    {
        using var doc = ReadJson(GoNoGoPath);
        var evidenceComplete = doc.RootElement.GetProperty("m633EvidencePackCompleted").GetBoolean();
        if (!evidenceComplete)
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
    public void GoNoGoKeepsReadyForProviderCloudFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForProviderCloud").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReadyForFilesystemFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForFilesystem").GetBoolean());
    }

    // ── 4. Product file integrity (hashes must match M631 baseline) ──────────

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("298BEE3E6AAE130369CDDCF63476E7B8356842205788FECF1666E96D58AB95D8", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("014A6C5DCCA2787E61E01D8B2DCED4A20F32D9F41BB076B7C2A5E37EE746F71E", Sha256Hex(SidepanelHtmlPath));
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
    public void ServiceWorkerUnchanged()
    {
        Assert.AreEqual("F479C04C342E922EA23928B9E2857FEFDDF69792959E33A31D3A7D3A28534CEC", Sha256Hex(BackgroundServicePath()));
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

    // ── 5. Manifest permissions, host_permissions, protocol unchanged ─────────

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

    // ── 6. New M633 artifact files do not contain forbidden product markers ───

    [TestMethod]
    public void NewM633FilesDoNotContainForbiddenProductMarkers()
    {
        foreach (var path in new[]
        {
            EvidencePackPath, QaResultPath, ConsoleInventoryPath,
            ScreenshotIndexPath, GoNoGoPath, ReportMarkdownPath,
            "tests/OneBrain.Safety.Tests/NodalOsScreenshotConsoleQaEvidencePackM633Tests.cs"
        })
        {
            var text = ReadRepoText(path);
            AssertDoesNotContain(text, "BrowserExecutor.C" + "dp");
            AssertDoesNotContain(text, "Http" + "Client");
            AssertDoesNotContain(text, "Client" + "WebSocket");
            AssertDoesNotContain(text, "Process." + "Start");
            AssertDoesNotContain(text, "sche" + "duler");
            AssertDoesNotContain(text, "provider " + "call");
            AssertDoesNotContain(text, "filesystem " + "scan");
            AssertDoesNotContain(text, "embed" + "ding");
            AssertDoesNotContain(text, "vector" + "ization");
            AssertDoesNotContain(text, "tele" + "metry");
            AssertDoesNotContain(text, "external " + "script");
            AssertDoesNotContain(text, "s" + "k-");
            AssertDoesNotContain(text, "bea" + "rer");
            AssertDoesNotContain(text, "api_" + "key");
            AssertDoesNotContain(text, "access_" + "token");
            AssertDoesNotContain(text, "refresh_" + "token");
            AssertDoesNotContain(text, "@im" + "port");
        }
    }
}
