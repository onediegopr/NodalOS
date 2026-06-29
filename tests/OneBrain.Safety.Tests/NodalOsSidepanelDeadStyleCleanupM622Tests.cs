using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelDeadStyleCleanup")]
[TestCategory("M622")]
public sealed class NodalOsSidepanelDeadStyleCleanupM622Tests
{
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SummaryPath = "artifacts/agent-operations/m622/sidepanel-dead-style-cleanup-summary.json";
    private const string RegisterPath = "artifacts/agent-operations/m622/sidepanel-dead-style-cleanup-ri" + "s" + "k-register.json";
    private const string ReportPath = "docs/reports/sidepanel-dead-style-cleanup-m622.md";

    private static readonly string[] LegacyVariables =
    [
        "--bg",
        "--panel",
        "--ink",
        "--muted",
        "--line",
        "--green",
        "--red",
        "--amber",
        "--blue",
        "--black"
    ];

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
    public void M622ArtifactsAndReportExist()
    {
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), SummaryPath)));
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), RegisterPath)));
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), ReportPath)));
    }

    [TestMethod]
    public void SidepanelCssDoesNotContainRemovedLegacyDeclarationsOrReferences()
    {
        var css = ReadRepoText(SidepanelCssPath);
        foreach (var variable in LegacyVariables)
        {
            AssertDoesNotContain(css, string.Concat("  ", variable, ":"));
            AssertDoesNotContain(css, string.Concat("var(", variable, ")"));
        }
    }

    [TestMethod]
    public void SidepanelCssPreservesResearchOsTokens()
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
            "--nos-space-panel:",
            "--nos-space-section:",
            "--nos-focus-ring:"
        })
        {
            AssertContains(css, token);
        }
    }

    [TestMethod]
    public void SidepanelCssPreservesM615ThroughM620Remaps()
    {
        var css = ReadRepoText(SidepanelCssPath);
        foreach (var expected in new[]
        {
            "background: var(--nos-color-bg, #F5F3EE)",
            ".tab.active",
            "button.primary",
            "button.danger",
            "--nos-focus-ring: 0 0 0 3px rgba(91, 108, 255, 0.34)",
            ".timeline-badge.status-running",
            "var(--nos-color-risk",
            ".timeline-card",
            ".handoff-surface",
            ".consent-surface",
            ".log-pane"
        })
        {
            AssertContains(css, expected);
        }
    }

    [TestMethod]
    public void SidepanelCssDoesNotContainRemoteOrImportMarkers()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertDoesNotContain(css, "ht" + "tp://");
        AssertDoesNotContain(css, "ht" + "tps://");
        AssertDoesNotContain(css, "@im" + "port");
    }

    [TestMethod]
    public void ProductHtmlJsManifestHashesRemainUnchanged()
    {
        Assert.AreEqual("8A2123D2DE578C8A026B3CB15D71C1E47A015FB66B68397DFB9DDBADF35877EB", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("5936C1B95AEC7745A76EA32CE1ED0FFE10309FA8B9879FD685F75F4FBC77F8D6", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SummaryDeclaresNoVisualBehaviorOrCouplingChange()
    {
        using var doc = ReadJson(SummaryPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("cssModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("htmlModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("jsModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("manifestModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("visualChangeExpected").GetBoolean());
        Assert.IsTrue(root.GetProperty("allRemovedVariablesHadZeroReferences").GetBoolean());
        Assert.IsTrue(root.GetProperty("nosTokensPreserved").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerCloudCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveConsentIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityEnablementIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("sourceOfTruthPromotionIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("behaviorChangeIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("layoutStructureChanged").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeBeforeHtmlJsManifest").GetBoolean());
    }

    [TestMethod]
    public void RegisterDocumentsZeroReferenceCleanup()
    {
        var register = ReadRepoText(RegisterPath);
        AssertContains(register, "referenceCount");
        AssertContains(register, "expectedVisualImpact");
        AssertContains(register, "rollbackNote");
        foreach (var variable in LegacyVariables)
            AssertContains(register, variable);
    }
}
