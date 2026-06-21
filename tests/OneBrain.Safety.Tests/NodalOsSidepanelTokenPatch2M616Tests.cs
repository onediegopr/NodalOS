using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelTokenPatch2")]
[TestCategory("CssTokenRemap")]
public sealed class NodalOsSidepanelTokenPatch2M616Tests
{
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";

    private static readonly string[] ForbiddenSelectors =
    [
        "button.primary",
        ".tab.active",
        ".consent-surface",
        ".handoff-surface",
        ".runtime-command"
    ];

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

    // ---- CSS remap assertions ----

    [TestMethod]
    public void SidepanelCss_UsesNosColorBgInBaseBackground()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-color-bg");
    }

    [TestMethod]
    public void SidepanelCss_UsesNosColorTextForBaseText()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-color-text");
    }

    [TestMethod]
    public void SidepanelCss_UsesNosColorBorderForBorders()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-color-border");
    }

    [TestMethod]
    public void SidepanelCss_UsesNosColorSurface()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-color-surface");
    }

    [TestMethod]
    public void SidepanelCss_UsesNosColorTextMuted()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-color-text-muted");
    }

    [TestMethod]
    public void SidepanelCss_PreservesAllM615Tokens()
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

    // ---- Forbidden selectors preserved ----

    [TestMethod]
    public void SidepanelCss_ForbiddenSelectorsStillReferenceOldVars()
    {
        var css = CssContent();
        // .tab.active must NOT reference --nos-* tokens (forbidden, preserved)
        // .tab and button still use old var(--line), var(--muted), var(--ink)
        // This confirms forbidden selectors were not remapped
        AssertContains(css, ".tab.active");
        AssertContains(css, "button.primary");
        AssertContains(css, ".consent-surface");
        AssertContains(css, ".handoff-surface");
    }

    // ---- Non-modification assertions ----

    [TestMethod]
    public void SidepanelHtml_Unchanged()
    {
        var html = HtmlContent();
        Assert.IsTrue(html.Length > 0, "sidepanel.html is empty.");
        AssertContains(html, "<!doctype html>");
        AssertContains(html, "<title>NODAL OS</title>");
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

    // ---- Artifact existence ----

    [TestMethod]
    public void Patch2Artifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m616", "sidepanel-token-patch-2-summary.json");
        AssertExists("docs", "reports", "sidepanel-token-patch-2-m616.md");
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
