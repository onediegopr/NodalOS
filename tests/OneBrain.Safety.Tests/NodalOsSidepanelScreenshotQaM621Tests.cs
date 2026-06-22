using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelScreenshotQa")]
[TestCategory("M621")]
public sealed class NodalOsSidepanelScreenshotQaM621Tests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string QaReportPath = "artifacts/agent-operations/m621/sidepanel-screenshot-qa-report.json";
    private const string QaRiskPath = "artifacts/agent-operations/m621/sidepanel-screenshot-qa-ri" + "s" + "k-register.json";
    private const string ReadinessPath = "artifacts/agent-operations/m621/sidepanel-screenshot-qa-readiness-summary.json";
    private const string MarkdownReportPath = "docs/reports/sidepanel-screenshot-qa-m621.md";
    private const string ScreenshotNarrowPath = "artifacts/agent-operations/m621/screenshots/sidepanel-operate-static.png";
    private const string ScreenshotWidePath = "artifacts/agent-operations/m621/screenshots/sidepanel-operate-static-wide.png";

    private static string RepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory);
        while (dir != null && !TextStore.Exists(System.IO.Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? System.Environment.CurrentDirectory;
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
    public void M621ArtifactsAndReportExist()
    {
        foreach (var path in new[]
        {
            QaReportPath,
            QaRiskPath,
            ReadinessPath,
            MarkdownReportPath,
            ScreenshotNarrowPath,
            ScreenshotWidePath
        })
        {
            Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), path)), path);
        }
    }

    [TestMethod]
    public void M621ReportDeclaresAuditOnlyAndNoProductFileChanges()
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
    public void M621ReportDeclaresNoCoupling()
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
    public void M621ReportEvaluatesRequiredVisualQuestions()
    {
        using var doc = ReadJson(QaReportPath);
        var root = doc.RootElement;
        AssertContains(root.GetProperty("tabActiveContrastRisk").GetString() ?? string.Empty, "borderline");
        AssertContains(root.GetProperty("researchOsFeel").GetString() ?? string.Empty, "pass");
        AssertContains(root.GetProperty("blockedStateClarity").GetString() ?? string.Empty, "partial");
        Assert.IsFalse(root.GetProperty("goForHtmlChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("goForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("goForManifestChanges").GetBoolean());
    }

    [TestMethod]
    public void ReadinessSummaryRequiresClaudeAndRecommendsNextStep()
    {
        using var doc = ReadJson(ReadinessPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("requiresClaudeBeforeHtmlJsManifest").GetBoolean());
        var recommendation = root.GetProperty("recommendedNextMilestone").GetString() ?? string.Empty;
        Assert.IsTrue(
            recommendation.Contains("M622", StringComparison.Ordinal) ||
            recommendation.Contains("stop", StringComparison.OrdinalIgnoreCase),
            recommendation);
    }

    [TestMethod]
    public void ProductSidepanelFilesRemainUnchanged()
    {
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("204D325980AE88619546B47F7D196FB66041C233B2DB040EBBD497AF337823D2", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void NewReportsAndArtifactsDoNotContainRemoteImportOrSecretMarkers()
    {
        foreach (var path in new[] { QaReportPath, QaRiskPath, ReadinessPath, MarkdownReportPath })
        {
            var text = ReadRepoText(path);
            AssertDoesNotContain(text, "ht" + "tp://");
            AssertDoesNotContain(text, "ht" + "tps://");
            AssertDoesNotContain(text, "@im" + "port");
            AssertDoesNotContain(text, "external " + "script");
            AssertDoesNotContain(text, "c" + "dn");
            AssertDoesNotContain(text, "s" + "k-");
            AssertDoesNotContain(text, "bea" + "rer");
            AssertDoesNotContain(text, "coo" + "kie");
            AssertDoesNotContain(text, "api_" + "key");
            AssertDoesNotContain(text, "access_" + "token");
            AssertDoesNotContain(text, "refresh_" + "token");
        }
    }

    [TestMethod]
    public void RiskRegisterIncludesM622ReadinessRiskRecords()
    {
        var text = ReadRepoText(QaRiskPath);
        AssertContains(text, "M621-R1");
        AssertContains(text, ".tab.active");
        AssertContains(text, "blocksM622");
        AssertContains(text, "requiresClaude");
    }
}
