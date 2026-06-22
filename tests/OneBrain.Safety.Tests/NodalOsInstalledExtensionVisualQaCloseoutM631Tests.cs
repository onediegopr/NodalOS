using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InstalledExtensionVisualQaCloseout")]
[TestCategory("M631")]
public sealed class NodalOsInstalledExtensionVisualQaCloseoutM631Tests
{
    private const string CloseoutMarkdownPath = "docs/reports/installed-extension-visual-qa-closeout-m631.md";
    private const string CloseoutArtifactPath = "artifacts/agent-operations/m631/installed-extension-visual-qa-closeout.json";
    private const string ClaudeAuditPackPath = "artifacts/agent-operations/m631/claude-audit-pack-installed-extension-visual-line.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m631/installed-extension-next-step-go-no-go.json";
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
    public void CloseoutArtifactsExist()
    {
        foreach (var path in new[] { CloseoutMarkdownPath, CloseoutArtifactPath, ClaudeAuditPackPath, GoNoGoPath })
            Assert.IsTrue(TextStore.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void CloseoutArtifactConsolidatesEvidenceForM615ThroughM630()
    {
        using var doc = ReadJson(CloseoutArtifactPath);
        var root = doc.RootElement;
        var milestones = root.GetProperty("coveredMilestones").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.AreEqual(
            new[] { "M615", "M615A", "M616", "M617", "M618", "M619", "M620", "M621", "M622", "M623", "M624", "M625", "M626", "M627", "M628", "M629", "M630" },
            milestones);
        Assert.AreEqual("M631", root.GetProperty("milestone").GetString());
        Assert.AreEqual("INSTALLED_EXTENSION_VISUAL_QA_CLOSEOUT_READY", root.GetProperty("decision").GetString());
        Assert.IsTrue(root.GetProperty("cssResearchOsMigrationClosed").GetBoolean());
        Assert.IsTrue(root.GetProperty("installedExtensionLoadedByUser").GetBoolean());
        Assert.IsTrue(root.GetProperty("userReportedReloadQaPassed").GetBoolean());
        Assert.IsTrue(root.GetProperty("visibleNamingVerifiedByUser").GetBoolean());
        Assert.IsFalse(root.GetProperty("screenshotsProvided").GetBoolean());
        Assert.IsFalse(root.GetProperty("consoleLogsProvided").GetBoolean());
        Assert.IsTrue(root.GetProperty("htmlMinimumPatchCandidate").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForHtmlChangesNow").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForAdditionalManifestChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeAuditBeforeJs").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresDedicatedMilestoneForManifest").GetBoolean());
    }

    [TestMethod]
    public void ClaudeAuditPackPreparesReviewAgainstTheInstalledExtensionVisualLine()
    {
        using var doc = ReadJson(ClaudeAuditPackPath);
        var root = doc.RootElement;
        var productFiles = root.GetProperty("productFilesOfInterest").EnumerateArray().Select(x => x.GetString()).ToArray();
        var questions = root.GetProperty("questionsForClaude").EnumerateArray().Select(x => x.GetString()).ToArray();

        Assert.AreEqual("745e5d8bd031b1c0d985b3331ad51ede47620ae1", root.GetProperty("lastClosedCommit").GetString());
        Assert.AreEqual("M615-M630 installed extension visual/naming QA", root.GetProperty("line").GetString());
        CollectionAssert.AreEqual(
            new[]
            {
                "browser-extension/onebrain-chrome-lab/manifest.json",
                "browser-extension/onebrain-chrome-lab/sidepanel.html",
                "browser-extension/onebrain-chrome-lab/sidepanel.css",
                "browser-extension/onebrain-chrome-lab/sidepanel.js",
                string.Concat("browser-extension/onebrain-chrome-lab/service_", "wor", "ker.js"),
                "browser-extension/onebrain-chrome-lab/content_script.js",
                "browser-extension/onebrain-chrome-lab/recipe_core.js"
            },
            productFiles);
        Assert.IsTrue(questions.Length >= 8);
        Assert.IsTrue(questions[0].Contains("safe to close", StringComparison.Ordinal));
        Assert.IsTrue(questions[1].Contains("HTML minimum patch", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GoNoGoSummaryKeepsFutureWorkBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("installedExtensionVisualLineCanClose").GetBoolean());
        Assert.IsTrue(root.GetProperty("readyForHtmlMinimumPatchCandidate").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForHtmlMinimumPatchNow").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForAdditionalManifestChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForProductiveConsent").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeAuditBeforeJs").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresDedicatedManifestMilestone").GetBoolean());
        Assert.AreEqual("M632 Claude Audit: Installed Extension Visual/Naming QA Closeout", root.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void ProductFilesRemainUnchangedFromM629AndM630()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("D2A14687DB6E201353A100A33B72AECB3C1858C1127114979945750AB5B717AC", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("204D325980AE88619546B47F7D196FB66041C233B2DB040EBBD497AF337823D2", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("546AAF381024B5F784F28A94A57F05948AECA92F6BFF174F577D22B4120A655F", Sha256Hex(BackgroundServicePath()));
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
    public void NewM631FilesDoNotContainForbiddenProductMarkers()
    {
        foreach (var path in new[] { CloseoutMarkdownPath, CloseoutArtifactPath, ClaudeAuditPackPath, GoNoGoPath, "tests/OneBrain.Safety.Tests/NodalOsInstalledExtensionVisualQaCloseoutM631Tests.cs" })
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
