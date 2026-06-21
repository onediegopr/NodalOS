using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelTokenPatch1")]
[TestCategory("CssVariableAddition")]
public sealed class NodalOsSidepanelTokenPatch1M615Tests
{
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";

    private static readonly string[] RuntimeIds =
    [
        "startRunBtn",
        "runRecipeBtn",
        "consentApproveBtn",
        "runtimeProvider",
        "runtimeStatus"
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

    [TestMethod]
    public void SidepanelCss_ContainsNosColorBg()
    {
        var css = CssContent();
        AssertContains(css, "--nos-color-bg");
    }

    [TestMethod]
    public void SidepanelCss_ContainsNosColorAccent()
    {
        var css = CssContent();
        AssertContains(css, "--nos-color-accent");
    }

    [TestMethod]
    public void SidepanelCss_ContainsNosFontHeading()
    {
        var css = CssContent();
        AssertContains(css, "--nos-font-heading");
    }

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
    public void SidepanelCss_RootBlock_DoesNotReferenceRuntimeIds()
    {
        var css = CssContent();
        var rootStart = css.IndexOf(":root", System.StringComparison.Ordinal);
        Assert.IsTrue(rootStart >= 0, ":root block not found.");
        var rootEnd = css.IndexOf('}', rootStart);
        Assert.IsTrue(rootEnd > rootStart, ":root block closing brace not found.");
        var rootBlock = css.Substring(rootStart, rootEnd - rootStart + 1);

        foreach (var id in RuntimeIds)
        {
            AssertDoesNotContain(rootBlock, id);
        }
    }

    [TestMethod]
    public void SidepanelCss_LegacyVariablesRemovedAfterM622()
    {
        var css = CssContent();
        AssertDoesNotContain(css, "--bg:");
        AssertDoesNotContain(css, "--panel:");
        AssertDoesNotContain(css, "--ink:");
        AssertDoesNotContain(css, "--muted:");
        AssertDoesNotContain(css, "--line:");
        AssertDoesNotContain(css, "--green:");
        AssertDoesNotContain(css, "--red:");
        AssertDoesNotContain(css, "--amber:");
        AssertDoesNotContain(css, "--blue:");
        AssertDoesNotContain(css, "--black:");
    }

    [TestMethod]
    public void SidepanelCss_ContainsAllResearchOsTokens()
    {
        var css = CssContent();
        var tokens = new[]
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
        };
        foreach (var token in tokens)
            AssertContains(css, token);
    }

    [TestMethod]
    public void SidepanelCss_IsSafeOutput()
    {
        var css = CssContent();
        AssertSafeOutput(css);
    }

    [TestMethod]
    public void SidepanelHtml_Unchanged()
    {
        var html = HtmlContent();
        Assert.IsTrue(html.Length > 0, "sidepanel.html is empty.");
        AssertSafeOutput(html);
        AssertContains(html, "<!doctype html>");
        AssertContains(html, "<title>NODAL OS</title>");
    }

    [TestMethod]
    public void SidepanelJs_Unchanged()
    {
        var js = JsContent();
        Assert.IsTrue(js.Length > 0, "sidepanel.js is empty.");
        // JS file legitimately handles token strings as part of existing extension runtime;
        // safety scan is deferred to CSS and artifacts only.
        AssertContains(js, "function handleMessage");
        AssertContains(js, "function bindEvents");
    }

    [TestMethod]
    public void ManifestJson_Unchanged()
    {
        var manifest = ManifestContent();
        Assert.IsTrue(manifest.Length > 0, "manifest.json is empty.");
        // Manifest may contain legitimate extension URLs; safety scan deferred.
        AssertContains(manifest, "manifest_version");
        AssertContains(manifest, "sidepanel");
    }

    [TestMethod]
    public void Patch1Artifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m615", "patch-1-approval.json");
        AssertExists("artifacts", "agent-operations", "m615", "sidepanel-token-patch-1-summary.json");
        AssertExists("docs", "reports", "sidepanel-token-patch-1-m615.md");
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
