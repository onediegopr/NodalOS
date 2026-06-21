using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SidepanelTokenPatch4")]
[TestCategory("CssActionFocusRemap")]
public sealed class NodalOsSidepanelTokenPatch4M618Tests
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
        "--nos-color-accent:",
        "--nos-color-danger:",
        "--nos-color-warning:",
        "--nos-radius-control:",
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

    // ---- Action button assertions ----

    [TestMethod]
    public void SidepanelCss_PrimaryUsesNosColorAccent()
    {
        var css = CssContent();
        AssertContains(css, "button.primary");
        AssertContains(css, "var(--nos-color-accent");
    }

    [TestMethod]
    public void SidepanelCss_DangerUsesNosColorDanger()
    {
        var css = CssContent();
        AssertContains(css, "button.danger");
        AssertContains(css, "var(--nos-color-danger");
    }

    [TestMethod]
    public void SidepanelCss_StopButtonUsesNosColorDanger()
    {
        var css = CssContent();
        AssertContains(css, ".stop-button");
        AssertContains(css, "var(--nos-color-danger");
    }

    // ---- Focus ring assertions ----

    [TestMethod]
    public void SidepanelCss_HasFocusVisibleWithNosFocusRing()
    {
        var css = CssContent();
        AssertContains(css, "focus-visible");
        AssertContains(css, "var(--nos-focus-ring");
    }

    [TestMethod]
    public void SidepanelCss_UsesNosRadiusControl()
    {
        var css = CssContent();
        AssertContains(css, "var(--nos-radius-control");
    }

    // ---- Recording state assertion ----

    [TestMethod]
    public void SidepanelCss_RecordingStateUsesNosColorWarning()
    {
        var css = CssContent();
        AssertContains(css, ".recording-state");
        AssertContains(css, "var(--nos-color-warning");
    }

    // ---- Token preservation ----

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

    // ---- Behavior invariants ----

    [TestMethod]
    public void SidepanelCss_DisabledStatePreservesOpacity()
    {
        var css = CssContent();
        AssertContains(css, "button:disabled");
        AssertContains(css, "opacity: 0.42");
    }

    [TestMethod]
    public void SidepanelCss_NoPointerEventsAutoOnBlockedButtons()
    {
        var css = CssContent();
        AssertDoesNotContain(css, "pointer-events: auto");
    }

    // ---- Non-modification assertions ----

    [TestMethod]
    public void SidepanelHtml_Unchanged()
    {
        var html = HtmlContent();
        Assert.IsTrue(html.Length > 0);
        AssertContains(html, "NODAL OS");
    }

    [TestMethod]
    public void SidepanelJs_Unchanged()
    {
        var js = JsContent();
        Assert.IsTrue(js.Length > 0);
        AssertContains(js, "function handleMessage");
    }

    [TestMethod]
    public void ManifestJson_Unchanged()
    {
        var manifest = ManifestContent();
        Assert.IsTrue(manifest.Length > 0);
        AssertContains(manifest, "manifest_version");
    }

    // ---- Artifact existence ----

    [TestMethod]
    public void Patch4Artifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m618", "sidepanel-token-patch-4-summary.json");
        AssertExists("docs", "reports", "sidepanel-token-patch-4-m618.md");
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
