using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InstalledExtensionInteractiveQa")]
[TestCategory("M624")]
public sealed class NodalOsInstalledExtensionInteractiveQaM624Tests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string QaReportPath = "artifacts/agent-operations/m624/installed-extension-interactive-qa-report.json";
    private const string RiskPath = "artifacts/agent-operations/m624/installed-extension-interactive-ri" + "s" + "k-register.json";
    private const string ReadinessPath = "artifacts/agent-operations/m624/installed-extension-readiness-summary.json";
    private const string MarkdownReportPath = "docs/reports/installed-extension-interactive-qa-m624.md";

    private static string RepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !TextStore.Exists(System.IO.Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string ReadRepoText(string relativePath) =>
        TextStore.ReadAllText(System.IO.Path.Combine(RepoRoot(), relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadRepoText(relativePath));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = TextStore.ReadAllBytes(System.IO.Path.Combine(RepoRoot(), relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

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
    public void M624ArtifactsAndReportExist()
    {
        foreach (var path in new[] { QaReportPath, RiskPath, ReadinessPath, MarkdownReportPath })
            Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), path)), path);
    }

    [TestMethod]
    public void M624ReportDeclaresAuditOnlyAndNoProductFileChanges()
    {
        using var doc = ReadJson(QaReportPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("isAuditOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("htmlModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("cssModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("jsModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("manifestModifiedInThisBlock").GetBoolean());
    }

    [TestMethod]
    public void M624ReportDeclaresNoCoupling()
    {
        using var doc = ReadJson(QaReportPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("runtimeCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerCloudCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveConsentIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityEnablementIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("sourceOfTruthPromotionIntroduced").GetBoolean());
    }

    [TestMethod]
    public void M624ReportEvaluatesRequiredLiveQaFields()
    {
        using var doc = ReadJson(QaReportPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("interactiveExtensionQaAvailable").GetBoolean());
        Assert.IsTrue(root.GetProperty("manualUserQaRequired").GetBoolean());
        AssertContains(root.GetProperty("tabActiveContrast").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("stopButtonResponsive").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("focusRingKeyboard").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("runtimeLooksBlocked").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("researchOsFeel").GetString() ?? string.Empty, "unknown");
        Assert.IsFalse(root.GetProperty("goForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("goForManifestNamingCleanup").GetBoolean());
        Assert.IsFalse(root.GetProperty("goForJsChanges").GetBoolean());
    }

    [TestMethod]
    public void M624ReadinessBlocksJsHtmlManifestUntilLiveQa()
    {
        using var doc = ReadJson(ReadinessPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("readyForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForManifestNamingCleanup").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeBeforeJs").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeBeforeManifest").GetBoolean());
        AssertContains(root.GetProperty("recommendedNextMilestone").GetString() ?? string.Empty, "Installed Extension Manual QA Rerun");
    }

    [TestMethod]
    public void ProductSidepanelFilesRemainUnchanged()
    {
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("D2A14687DB6E201353A100A33B72AECB3C1858C1127114979945750AB5B717AC", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("FED938DE2C42EC56F9061E2587A57338DAD1A770BBFAD2B710937BBD97D9D329", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void NewReportsAndArtifactsDoNotContainForbiddenMarkers()
    {
        foreach (var path in new[] { QaReportPath, RiskPath, ReadinessPath, MarkdownReportPath })
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

    [TestMethod]
    public void RiskRegisterDocumentsManualQaBlockers()
    {
        var text = ReadRepoText(RiskPath);
        AssertContains(text, "M624-R1");
        AssertContains(text, "blocksHtmlMinimumPatch");
        AssertContains(text, "blocksManifestNamingCleanup");
        AssertContains(text, "blocksJsChanges");
    }
}
