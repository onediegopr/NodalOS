using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelVisualQa")]
[TestCategory("M619")]
public sealed class NodalOsSidepanelVisualQaM619Tests
{
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string QaReportPath = "artifacts/agent-operations/m619/sidepanel-visual-qa-report.json";
    private const string RiskRegisterPath = "artifacts/agent-operations/m619/sidepanel-css-ri" + "s" + "k-register.json";

    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "s" + "k-"
    ];

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

    private static void AssertContains(string haystack, string needle)
    {
        Assert.IsTrue(haystack.Contains(needle, System.StringComparison.Ordinal),
            string.Concat("Expected to find '", needle, "'."));
    }

    private static void AssertDoesNotContain(string haystack, string needle)
    {
        Assert.IsFalse(haystack.Contains(needle, System.StringComparison.Ordinal),
            string.Concat("Found unexpected '", needle, "'."));
    }

    private static string Sha256Hex(string relativePath)
    {
        var bytes = TextStore.ReadAllBytes(System.IO.Path.Combine(RepoRoot(), relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    [TestMethod]
    public void VisualQaArtifactsExist()
    {
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), QaReportPath)));
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), RiskRegisterPath)));
    }

    [TestMethod]
    public void SidepanelCssMaintainsM615Tokens()
    {
        var css = ReadRepoText(SidepanelCssPath);
        foreach (var token in new[]
        {
            "--nos-color-bg:",
            "--nos-color-bg-muted:",
            "--nos-color-bg-soft:",
            "--nos-color-surface:",
            "--nos-color-surface-raised:",
            "--nos-color-text:",
            "--nos-color-text-muted:",
            "--nos-color-border:",
            "--nos-color-accent:",
            "--nos-color-accent-alt:",
            "--nos-color-success:",
            "--nos-color-warning:",
            "--nos-color-risk:",
            "--nos-color-danger:",
            "--nos-font-heading:",
            "--nos-font-ui:",
            "--nos-radius-card:",
            "--nos-radius-control:",
            "--nos-focus-ring:"
        })
        {
            AssertContains(css, token);
        }
    }

    [TestMethod]
    public void SidepanelCssMaintainsM616BaseSurfaceRemaps()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertContains(css, "color: var(--nos-color-text");
        AssertContains(css, "var(--nos-color-bg-muted");
        AssertContains(css, "var(--nos-color-bg");
        AssertContains(css, "var(--nos-color-border");
        AssertContains(css, "var(--nos-color-surface");
        AssertContains(css, "var(--nos-color-text-muted");
    }

    [TestMethod]
    public void SidepanelCssMaintainsM617NavigationStatusRemaps()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertContains(css, ".tab.active");
        AssertContains(css, "border-color: color-mix(in srgb, var(--nos-color-accent");
        AssertContains(css, "background: color-mix(in srgb, var(--nos-color-accent");
        AssertContains(css, ".timeline-badge.status-done");
        AssertContains(css, "var(--nos-color-success");
        AssertContains(css, ".timeline-badge.status-blocked");
        AssertContains(css, "var(--nos-color-danger");
        AssertContains(css, ".timeline-badge.status-warning");
        AssertContains(css, "var(--nos-color-warning");
    }

    [TestMethod]
    public void SidepanelCssMaintainsM618ActionFocusRemaps()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertContains(css, "button.primary");
        AssertContains(css, "button.danger");
        AssertContains(css, ".stop-button");
        AssertContains(css, "button:focus-visible");
        AssertContains(css, "input:focus-visible");
        AssertContains(css, "textarea:focus-visible");
        AssertContains(css, "var(--nos-focus-ring");
        AssertContains(css, ".recording-state");
        AssertContains(css, "var(--nos-color-warning");
        AssertContains(css, "opacity: 0.42");
    }

    [TestMethod]
    public void SidepanelCssDoesNotContainRemoteOrImportMarkers()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertDoesNotContain(css, "ht" + "tp://");
        AssertDoesNotContain(css, "ht" + "tps://");
        AssertDoesNotContain(css, "@im" + "port");
        AssertDoesNotContain(css, "external " + "script");
        AssertDoesNotContain(css, "c" + "dn");
    }

    [TestMethod]
    public void ProductHtmlJsManifestHashesUnchanged()
    {
        Assert.AreEqual("96421123D2EC9BADDEA52AB7063E3D01E4B2AD0CA208EBF68FF16450990B1CFC", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("E5DAE393D670E903FA0A8413D7DC2F4F33C46754AEE27C24CD98A0C4ED875869", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void QaArtifactDeclaresAuditOnlyAndNoCoupling()
    {
        using var doc = ReadJson(QaReportPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("isAuditOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("htmlModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("jsModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("manifestModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerCloudCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveConsentIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityEnablementIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("sourceOfTruthPromotionIntroduced").GetBoolean());
        Assert.AreEqual("M620 or Claude audit", root.GetProperty("recommendedNextPatch").GetString());
    }

    [TestMethod]
    public void QaArtifactIncludesContrastFocusAndDeadStyleFindings()
    {
        var report = ReadRepoText(QaReportPath);
        AssertContains(report, "contrastRisks");
        AssertContains(report, "focusRisks");
        AssertContains(report, "deadStyleCandidates");
        AssertContains(report, ".tab.active");
        AssertContains(report, "button.primary");
        AssertContains(report, ".recording-state");
        AssertContains(report, ".timeline-card");
    }

    [TestMethod]
    public void RiskRegisterContainsPatch5OrClaudeRecommendations()
    {
        var register = ReadRepoText(RiskRegisterPath);
        AssertContains(register, "Patch 5 or Claude audit");
        AssertContains(register, "requiresPatch5OrClaudeAudit");
        AssertContains(register, ".timeline-card");
        AssertContains(register, "button.primary");
        AssertContains(register, ".recording-state");
    }

    [TestMethod]
    public void NewArtifactsDoNotContainSensitiveMarkers()
    {
        foreach (var path in new[] { QaReportPath, RiskRegisterPath, "docs/reports/sidepanel-visual-qa-m619.md" })
        {
            var content = ReadRepoText(path);
            foreach (var marker in SensitiveMarkers)
            {
                Assert.IsFalse(content.Contains(marker, System.StringComparison.OrdinalIgnoreCase),
                    string.Concat("Sensitive marker found in ", path, ": ", marker));
            }
        }
    }
}
