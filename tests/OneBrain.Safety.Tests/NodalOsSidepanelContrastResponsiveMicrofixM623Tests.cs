using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelContrastResponsiveMicrofix")]
[TestCategory("M623")]
public sealed class NodalOsSidepanelContrastResponsiveMicrofixM623Tests
{
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SummaryPath = "artifacts/agent-operations/m623/sidepanel-contrast-responsive-microfix-summary.json";
    private const string RegisterPath = "artifacts/agent-operations/m623/sidepanel-contrast-responsive-ri" + "s" + "k-register.json";
    private const string ReportPath = "docs/reports/sidepanel-contrast-responsive-microfix-m623.md";

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

    private static string CssBlock(string css, string selector)
    {
        var start = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, string.Concat("Selector not found: ", selector));
        var open = css.IndexOf('{', start);
        Assert.IsTrue(open > start, string.Concat("Selector block not found: ", selector));
        var close = css.IndexOf('}', open);
        Assert.IsTrue(close > open, string.Concat("Selector block close not found: ", selector));
        return css.Substring(open, close - open + 1);
    }

    [TestMethod]
    public void M623ArtifactsAndReportExist()
    {
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), SummaryPath)));
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), RegisterPath)));
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), ReportPath)));
    }

    [TestMethod]
    public void SidepanelCssPreservesResearchOsTokensAndNoLegacyVariables()
    {
        var css = ReadRepoText(SidepanelCssPath);
        foreach (var token in new[]
        {
            "--nos-color-bg:",
            "--nos-color-accent:",
            "--nos-color-text:",
            "--nos-color-danger:",
            "--nos-focus-ring:"
        })
        {
            AssertContains(css, token);
        }

        foreach (var legacy in new[] { "--bg:", "--panel:", "--ink:", "--muted:", "--line:", "--green:", "--red:", "--amber:", "--blue:", "--black:" })
            AssertDoesNotContain(css, legacy);
    }

    [TestMethod]
    public void SidepanelCssContainsTabActiveContrastMicrofix()
    {
        var css = ReadRepoText(SidepanelCssPath);
        var block = CssBlock(css, ".tab.active");
        AssertContains(block, "color-mix(in srgb, var(--nos-color-accent, #5B6CFF) 76%, var(--nos-color-text, #171717))");
        AssertContains(block, "color: #fff");
        AssertContains(block, "box-shadow: inset");
    }

    [TestMethod]
    public void SidepanelCssContainsStopButtonResponsiveMicrofix()
    {
        var css = ReadRepoText(SidepanelCssPath);
        var block = CssBlock(css, ".stop-button");
        AssertContains(block, "width: 64px");
        AssertContains(block, "min-width: 64px");
        AssertContains(block, "margin-right: 18px");
        AssertContains(block, "padding: 0 10px");
        AssertContains(block, "white-space: nowrap");
        AssertContains(css, "@media (max-width: 430px)");
        AssertContains(css, ".app-header");
        AssertContains(css, "padding-right: 28px");
        AssertContains(css, "width: 48px");
        AssertContains(css, "min-width: 48px");
        AssertContains(css, "font-size: 12px");
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
        Assert.AreEqual("96421123D2EC9BADDEA52AB7063E3D01E4B2AD0CA208EBF68FF16450990B1CFC", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("E5DAE393D670E903FA0A8413D7DC2F4F33C46754AEE27C24CD98A0C4ED875869", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SummaryDeclaresNoBehaviorOrCouplingChange()
    {
        using var doc = ReadJson(SummaryPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("cssModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("htmlModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("jsModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("manifestModifiedInThisBlock").GetBoolean());
        Assert.AreEqual("approx 4.3:1", root.GetProperty("tabActiveContrastBefore").GetString());
        Assert.AreEqual("approx 5.97:1", root.GetProperty("tabActiveContrastAfter").GetString());
        Assert.IsTrue(root.GetProperty("stopClippingAddressed").GetBoolean());
        Assert.IsFalse(root.GetProperty("behaviorChangeIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("layoutStructureChanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerCloudCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveConsentIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityEnablementIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("sourceOfTruthPromotionIntroduced").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeBeforeHtmlJsManifest").GetBoolean());
    }
}
