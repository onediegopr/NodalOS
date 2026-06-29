using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("HtmlMicrocopyPatch")]
[TestCategory("M634")]
public sealed class NodalOsHtmlMicrocopyPatchM634Tests
{
    private const string SummaryPath = "artifacts/agent-operations/m634/html-microcopy-patch-summary.json";
    private const string BeforeAfterPath = "artifacts/agent-operations/m634/html-microcopy-before-after.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m634/post-html-microcopy-go-no-go.json";
    private const string ReportPath = "docs/reports/html-microcopy-patch-m634.md";
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
    public void SummaryDeclaresHtmlOnly()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("htmlOnly").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresTextOnly()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("textOnly").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresDomStructureUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("domStructureChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresIdsUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("idsChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresClassesUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("classesChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresJsUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("jsChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresCssUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("cssChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresManifestUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("manifestChanged").GetBoolean());
    }

    [TestMethod]
    public void SidepanelHtmlContainsSpanishTargetResolution()
    {
        AssertContains(ReadRepoText(SidepanelHtmlPath), "Resolución del objetivo");
    }

    [TestMethod]
    public void SidepanelHtmlContainsSpanishVerification()
    {
        AssertContains(ReadRepoText(SidepanelHtmlPath), "Verificación");
    }

    [TestMethod]
    public void SidepanelHtmlDoesNotContainTargetResolution()
    {
        AssertDoesNotContain(ReadRepoText(SidepanelHtmlPath), "Target Resolution");
    }

    [TestMethod]
    public void SidepanelHtmlDoesNotContainVerificationHeading()
    {
        AssertDoesNotContain(ReadRepoText(SidepanelHtmlPath), "Verification");
    }

    [TestMethod]
    public void SidepanelHtmlDoesNotContainGuardara()
    {
        AssertDoesNotContain(ReadRepoText(SidepanelHtmlPath), "guardara");
    }

    [TestMethod]
    public void SidepanelHtmlDoesNotContainVolvera()
    {
        AssertDoesNotContain(ReadRepoText(SidepanelHtmlPath), "volvera");
    }

    [TestMethod]
    public void SidepanelHtmlDoesNotContainInvalido()
    {
        AssertDoesNotContain(ReadRepoText(SidepanelHtmlPath), "invalido");
    }

    [TestMethod]
    public void SidepanelHtmlDoesNotContainReemplazalo()
    {
        AssertDoesNotContain(ReadRepoText(SidepanelHtmlPath), "reemplazalo");
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("0141931FA94B0004A8F2631C9E6985E1CF9243B0B9CBF787AFB2449858B6CED9", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("E5DAE393D670E903FA0A8413D7DC2F4F33C46754AEE27C24CD98A0C4ED875869", Sha256Hex(SidepanelJsPath));
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
        var sidepanelJs = ReadRepoText(SidepanelJsPath);
        AssertContains(serviceWorker, "PROTOCOL_VERSION = 'chrome-lab-v1'");
        AssertContains(serviceWorker, "nexaRecipes");
        AssertContains(serviceWorker, "nexaLearningDraft");
        AssertContains(serviceWorker, "nexaRuntimeState");
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(serviceWorker, "nexa.content.ping");
        AssertContains(sidepanelJs, "onebrain-sidepanel");
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
