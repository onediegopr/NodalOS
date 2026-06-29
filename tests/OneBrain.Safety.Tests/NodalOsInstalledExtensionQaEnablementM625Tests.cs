using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InstalledExtensionQaEnablement")]
[TestCategory("M625")]
public sealed class NodalOsInstalledExtensionQaEnablementM625Tests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string EnablementReportPath = "artifacts/agent-operations/m625/installed-extension-qa-enablement-report.json";
    private const string RunbookPath = "artifacts/agent-operations/m625/manual-extension-qa-runbook.json";
    private const string ChecklistPath = "artifacts/agent-operations/m625/manual-screenshot-checklist.json";
    private const string ReadinessPath = "artifacts/agent-operations/m625/installed-extension-qa-readiness-summary.json";
    private const string MarkdownReportPath = "docs/reports/installed-extension-qa-enablement-m625.md";

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
    public void M625ArtifactsAndReportExist()
    {
        foreach (var path in new[] { EnablementReportPath, RunbookPath, ChecklistPath, ReadinessPath, MarkdownReportPath })
            Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), path)), path);
    }

    [TestMethod]
    public void EnablementReportDeclaresAuditOnlyAndNoProductFileChanges()
    {
        using var doc = ReadJson(EnablementReportPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("isAuditOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(root.GetProperty("htmlModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("cssModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("jsModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("manifestModifiedInThisBlock").GetBoolean());
        Assert.IsTrue(root.GetProperty("chromeConnectorIssueDocumented").GetBoolean());
        Assert.IsTrue(root.GetProperty("manualUserQaRequired").GetBoolean());
    }

    [TestMethod]
    public void EnablementReportDeclaresNoCouplingAndNoGo()
    {
        using var doc = ReadJson(EnablementReportPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("runtimeCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerCloudCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveConsentIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityEnablementIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("sourceOfTruthPromotionIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("goForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("goForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("goForManifestChanges").GetBoolean());
    }

    [TestMethod]
    public void RunbookIncludesManualChromeExtensionSteps()
    {
        var runbook = ReadRepoText(RunbookPath);
        AssertContains(runbook, "chrome://extensions");
        AssertContains(runbook, "Load unpacked");
        AssertContains(runbook, "browser-extension\\\\onebrain-chrome-lab");
        AssertContains(runbook, "Developer mode");
        AssertContains(runbook, "DevTools");
    }

    [TestMethod]
    public void ScreenshotChecklistIncludesRequiredScenarios()
    {
        var checklist = ReadRepoText(ChecklistPath);
        foreach (var item in new[] { "Operar", "Aprender", "Recetas", "Runtime", "STOP button", "DevTools console" })
            AssertContains(checklist, item);
    }

    [TestMethod]
    public void ReadinessSummaryRequiresManualEvidenceAndBlocksFutureProductChanges()
    {
        using var doc = ReadJson(ReadinessPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("readyForManualUserQa").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForManifestNamingCleanup").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        AssertContains(root.GetProperty("recommendedNextMilestone").GetString() ?? string.Empty, "Installed Extension Manual QA Evidence Capture");
        Assert.IsTrue(root.GetProperty("requiredScreenshots").GetArrayLength() >= 10);
        Assert.IsTrue(root.GetProperty("requiredUserEvidence").GetArrayLength() >= 5);
    }

    [TestMethod]
    public void ProductSidepanelFilesRemainUnchanged()
    {
        Assert.AreEqual("8A2123D2DE578C8A026B3CB15D71C1E47A015FB66B68397DFB9DDBADF35877EB", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("69E508F153A2D58DC6824BFFF041636C9D94181C97CA2CD929DF091BD434F61B", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("5936C1B95AEC7745A76EA32CE1ED0FFE10309FA8B9879FD685F75F4FBC77F8D6", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void NewReportsAndArtifactsDoNotContainForbiddenMarkers()
    {
        foreach (var path in new[] { EnablementReportPath, RunbookPath, ChecklistPath, ReadinessPath, MarkdownReportPath })
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
            AssertDoesNotContain(text.Replace("chrome://extensions", string.Empty, StringComparison.Ordinal), "ht" + "tp://");
            AssertDoesNotContain(text, "ht" + "tps://");
        }
    }
}
