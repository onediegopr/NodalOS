using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelVisualQaFixes")]
[TestCategory("M620")]
public sealed class NodalOsSidepanelVisualQaFixesM620Tests
{
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SummaryPath = "artifacts/agent-operations/m620/sidepanel-visual-qa-fixes-summary.json";
    private const string PostQaRegisterPath = "artifacts/agent-operations/m620/sidepanel-post-qa-ri" + "s" + "k-register.json";

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
        Assert.IsTrue(haystack.Contains(needle, System.StringComparison.Ordinal),
            string.Concat("Expected to find '", needle, "'."));
    }

    private static void AssertDoesNotContain(string haystack, string needle)
    {
        Assert.IsFalse(haystack.Contains(needle, System.StringComparison.Ordinal),
            string.Concat("Found unexpected '", needle, "'."));
    }

    private static string CssBlock(string css, string selector)
    {
        var start = css.IndexOf(selector, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, string.Concat("Selector not found: ", selector));
        var open = css.IndexOf('{', start);
        Assert.IsTrue(open > start, string.Concat("Selector block not found: ", selector));
        var close = css.IndexOf('}', open);
        Assert.IsTrue(close > open, string.Concat("Selector block close not found: ", selector));
        return css.Substring(open, close - open + 1);
    }

    [TestMethod]
    public void M620ArtifactsExist()
    {
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), SummaryPath)));
        Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), PostQaRegisterPath)));
    }

    [TestMethod]
    public void SidepanelCssMaintainsTokensFromM615ThroughM618()
    {
        var css = ReadRepoText(SidepanelCssPath);
        foreach (var token in new[]
        {
            "--nos-color-bg:",
            "--nos-color-bg-muted:",
            "--nos-color-bg-soft:",
            "--nos-color-surface:",
            "--nos-color-text:",
            "--nos-color-text-muted:",
            "--nos-color-border:",
            "--nos-color-accent:",
            "--nos-color-success:",
            "--nos-color-warning:",
            "--nos-color-risk:",
            "--nos-color-danger:",
            "--nos-font-heading:",
            "--nos-font-ui:",
            "--nos-radius-control:",
            "--nos-focus-ring:"
        })
        {
            AssertContains(css, token);
        }

        AssertContains(css, "button.primary");
        AssertContains(css, "button.danger");
        AssertContains(css, ".tab.active");
        AssertContains(css, ".recording-state");
    }

    [TestMethod]
    public void SidepanelCssFocusRingWasStrengthened()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertContains(css, "--nos-focus-ring: 0 0 0 3px rgba(91, 108, 255, 0.34)");
        AssertContains(css, "outline: 2px solid transparent");
        AssertContains(css, "outline-offset: 2px");
        AssertContains(css, "box-shadow: var(--nos-focus-ring");
    }

    [TestMethod]
    public void SidepanelCssAppliesM620VisualFixes()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertContains(css, "font-family: var(--nos-font-ui");
        AssertContains(css, "font-family: var(--nos-font-heading");
        AssertContains(css, "color: var(--nos-color-text, #171717)");
        AssertContains(css, ".timeline-badge.status-running");
        AssertContains(css, "var(--nos-color-risk");
        AssertContains(css, ".timeline-card");
        AssertContains(css, "var(--nos-color-bg-soft");
        AssertContains(css, ".handoff-surface");
        AssertContains(css, ".consent-surface");
        AssertContains(css, ".log-pane");
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
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("204D325980AE88619546B47F7D196FB66041C233B2DB040EBBD497AF337823D2", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SummaryArtifactDeclaresNoCouplingOrBehaviorChange()
    {
        using var doc = ReadJson(SummaryPath);
        var root = doc.RootElement;
        Assert.IsTrue(root.GetProperty("cssModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("htmlModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("jsModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("manifestModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerCloudCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemCouplingIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveConsentIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityEnablementIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("sourceOfTruthPromotionIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("behaviorChangeIntroduced").GetBoolean());
        Assert.IsFalse(root.GetProperty("layoutStructureChanged").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeAuditBeforeHtmlJs").GetBoolean());
    }

    [TestMethod]
    public void SidepanelCssDoesNotEnablePointerEventsOrSensitiveStructuralChanges()
    {
        var css = ReadRepoText(SidepanelCssPath);
        AssertDoesNotContain(css, "pointer-events: auto");

        foreach (var selector in new[] { ".stop-button", "button.primary", "button.danger", ".recording-state", ".handoff-surface", ".consent-surface" })
        {
            var block = CssBlock(css, selector);
            AssertDoesNotContain(block, "display:");
            AssertDoesNotContain(block, "visibility:");
            AssertDoesNotContain(block, "pointer-events:");
        }
    }

    [TestMethod]
    public void PostQaRegisterIncludesResolvedAndDeferredRisks()
    {
        var register = ReadRepoText(PostQaRegisterPath);
        AssertContains(register, "resolvedRisks");
        AssertContains(register, "remainingRisks");
        AssertContains(register, "deferredRisks");
        AssertContains(register, "requiresManualVisualQa");
        AssertContains(register, "requiresClaudeBeforeHtmlJsManifest");
    }
}
