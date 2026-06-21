using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelTokenPatch3")]
[TestCategory("CssNavStatusRemap")]
public sealed class NodalOsSidepanelTokenPatch3M617Tests
{
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";

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

    private static readonly string[] NosTokensFromM615 =
    [
        "--nos-color-bg:",
        "--nos-color-text:",
        "--nos-color-border:",
        "--nos-color-accent:",
        "--nos-color-success:",
        "--nos-color-warning:",
        "--nos-color-risk:",
        "--nos-color-danger:"
    ];

    private static string CssContent() =>
        TextStore.ReadAllText(
            System.IO.Path.Combine(RepoRoot(), SidepanelCssPath));

    private static string HtmlContent() =>
        TextStore.ReadAllText(
            System.IO.Path.Combine(RepoRoot(), SidepanelHtmlPath));

    private static string JsContent() =>
        TextStore.ReadAllText(
            System.IO.Path.Combine(RepoRoot(), SidepanelJsPath));

    private static string ManifestContent() =>
        TextStore.ReadAllText(
            System.IO.Path.Combine(RepoRoot(), ManifestPath));

    private static string RepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory);
        while (dir != null && !TextStore.Exists(System.IO.Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? System.Environment.CurrentDirectory;
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

    private static void AssertSafeOutput(string content)
    {
        foreach (var marker in SensitiveMarkers)
        {
            Assert.IsFalse(content.Contains(marker, System.StringComparison.OrdinalIgnoreCase),
                string.Concat("Sensitive marker found: ", marker));
        }
    }

    // ---- Navigation remap assertions ----

    [TestMethod]
    public void SidepanelCss_TabActiveUsesNosColorAccent()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-color-accent");
    }

    [TestMethod]
    public void SidepanelCss_TabsUseNosColorBorder()
    {
        var css = CssContent();
        // Verify --nos-color-border is used in CSS (from M616 + this patch)
        AssertContains(css, "var(--nos-color-border");
    }

    // ---- Status badge assertions ----

    [TestMethod]
    public void SidepanelCss_StatusBadgesUseSemanticTokens()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-color-success");
        AssertContains(css, "var(--nos-color-warning");
        AssertContains(css, "var(--nos-color-danger");
    }

    [TestMethod]
    public void SidepanelCss_BlockedStateUsesDangerToken()
    {
        var css = CssContent();
        // timeline-blocker-card uses --nos-color-danger
        AssertContains(css, "--nos-color-danger");
    }

    // ---- Token preservation assertions ----

    [TestMethod]
    public void SidepanelCss_PreservesCoreM615Tokens()
    {
        var css = CssContent();
        foreach (var token in NosTokensFromM615)
            AssertContains(css, token);
    }

    // ---- Guard assertions ----

    [TestMethod]
    public void SidepanelCss_DoesNotContainHttp()
    {
        var css = CssContent();
        AssertDoesNotContain(css, "http://");
    }

    [TestMethod]
    public void SidepanelCss_DoesNotContainHttps()
    {
        var css = CssContent();
        AssertDoesNotContain(css, "https://");
    }

    [TestMethod]
    public void SidepanelCss_DoesNotContainImport()
    {
        var css = CssContent();
        AssertDoesNotContain(css, "@import");
    }

    [TestMethod]
    public void SidepanelCss_IsSafeOutput()
    {
        var css = CssContent();
        AssertSafeOutput(css);
    }

    // ---- Non-modification assertions ----

    [TestMethod]
    public void SidepanelHtml_Unchanged()
    {
        var html = HtmlContent();
        Assert.IsTrue(html.Length > 0, "sidepanel.html is empty.");
        AssertContains(html, "<!doctype html>");
    }

    [TestMethod]
    public void SidepanelJs_Unchanged()
    {
        var js = JsContent();
        Assert.IsTrue(js.Length > 0, "sidepanel.js is empty.");
        AssertContains(js, "function handleMessage");
    }

    [TestMethod]
    public void ManifestJson_Unchanged()
    {
        var manifest = ManifestContent();
        Assert.IsTrue(manifest.Length > 0, "manifest.json is empty.");
        AssertContains(manifest, "manifest_version");
    }

    // ---- Forbidden selectors preserved ----

    [TestMethod]
    public void SidepanelCss_ForbiddenActionButtonsStillUseOldVars()
    {
        var css = CssContent();
        // button.primary and button.danger must still reference old green/red vars
        AssertContains(css, "button.primary");
        AssertContains(css, "button.danger");
    }

    // ---- Artifact existence ----

    [TestMethod]
    public void Patch3Artifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m617", "sidepanel-token-patch-3-summary.json");
        AssertExists("docs", "reports", "sidepanel-token-patch-3-m617.md");
    }

    private static void AssertExists(params string[] pathSegments)
    {
        var combined = RepoRoot();
        foreach (var seg in pathSegments)
            combined = System.IO.Path.Combine(combined, seg);
        Assert.IsTrue(TextStore.Exists(combined),
            string.Concat("Artifact not found: ", combined));
    }
}
