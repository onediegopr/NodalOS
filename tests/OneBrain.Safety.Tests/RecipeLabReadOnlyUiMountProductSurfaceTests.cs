using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("Recipe")]
[TestCategory("RecipeLabReadOnlyUiMount")]
public sealed class RecipeLabReadOnlyUiMountProductSurfaceTests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";

    [TestMethod]
    public void RecipeLabSurfaceIsVisibleInMissionControlNavigation()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var section = ExtractRecipeLabSurface(html);

        StringAssert.Contains(html, "href=\"#recipeLabSurface\"");
        StringAssert.Contains(section, "data-recipe-lab-mount=\"recipe-lab.ui.read-only.mount.v1\"");
        StringAssert.Contains(section, "Recipe Lab");
        StringAssert.Contains(section, "READ_ONLY");
        StringAssert.Contains(section, "FIXTURE_SAFE");
        StringAssert.Contains(section, "NO_RUNTIME");
        StringAssert.Contains(section, "NO_LIVE_AUTOMATION");
        StringAssert.Contains(section, "Recipe Catalog Summary");
        StringAssert.Contains(section, "Recipe detail preview");
        StringAssert.Contains(section, "Readiness matrix");
        StringAssert.Contains(section, "Operator preview");
        StringAssert.Contains(section, "Handoff/export preview");
    }

    [TestMethod]
    public void RecipeLabSurfaceRendersRequiredNoRuntimeNotices()
    {
        var section = ExtractRecipeLabSurface(ReadRepoText(SidepanelHtmlPath));

        foreach (var expected in new[]
        {
            "No recipe execution",
            "No runtime actions",
            "No live automation",
            "No browser/CDP automation",
            "No WCU live",
            "No OCR live",
            "No filesystem writes",
            "No provider/cloud calls",
            "No durable recipe persistence",
            "Human approval required for any real action"
        })
        {
            StringAssert.Contains(section, expected);
        }
    }

    [TestMethod]
    public void RecipeLabJavascriptMountUsesReadOnlyFixtureSnapshot()
    {
        var js = ReadRepoText(SidepanelJsPath);

        foreach (var expected in new[]
        {
            "RECIPE_LAB_READ_ONLY_SURFACE",
            "mountId: 'recipe-lab.ui.read-only.mount.v1'",
            "route: '#recipeLabSurface'",
            "source: 'RecipeLabReadOnlyUiMount.CreateFixture'",
            "selectedTemplateId: 'excel.extract_rows_to_workitems'",
            "dataSource: 'deterministic local fixture catalog'",
            "readOnly: true",
            "fixtureSafe: true",
            "previewSafe: true",
            "runtimeEnabled: false",
            "recipeExecutionEnabled: false",
            "browserCdpAutomationEnabled: false",
            "wcuLiveEnabled: false",
            "ocrLiveEnabled: false",
            "providerCloudEnabled: false",
            "durablePersistenceEnabled: false",
            "filesystemWritesEnabled: false",
            "handoffExportWritesFile: false",
            "renderRecipeLabSurface",
            "buildRecipeLabReportPreview",
            "buildRecipeLabHandoffPreview",
            "copyRecipeLabPreview",
            "copyRecipeLabHandoffPreview"
        })
        {
            StringAssert.Contains(js, expected);
        }
    }

    [TestMethod]
    public void RecipeLabCopyPreviewsAreMetadataOnlyAndNoRuntime()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var start = js.IndexOf("function buildRecipeLabReportPreview", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Recipe Lab report preview function is missing.");
        var end = js.IndexOf("function renderEvidenceIntelligenceSurface", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "Recipe Lab preview function end marker is missing.");
        var previewFunctions = js[start..end];

        foreach (var expected in new[]
        {
            "READ_ONLY / FIXTURE_SAFE / NO_RUNTIME / NO_LIVE_AUTOMATION",
            "copyMode: clipboard-only text preview",
            "runtimeEnabled:",
            "recipeExecutionEnabled:",
            "browserCdpAutomationEnabled:",
            "wcuLiveEnabled:",
            "ocrLiveEnabled:",
            "providerCloudEnabled:",
            "durablePersistenceEnabled:",
            "filesystemWritesEnabled:",
            "handoffExportWritesFile:",
            "safeNextAction:"
        })
        {
            StringAssert.Contains(previewFunctions, expected);
        }

        foreach (var forbidden in new[] { "po" + "st(", "chrome." + "runtime", "fe" + "tch(", "XML" + "HttpRequest", "localStorage." + "setItem", "document." + "cookie" })
        {
            Assert.IsFalse(previewFunctions.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void RecipeLabSurfaceDoesNotExposeForbiddenActionButtonsOrOverclaims()
    {
        var section = ExtractRecipeLabSurface(ReadRepoText(SidepanelHtmlPath));
        var forbiddenButtonLabels = new[]
        {
            "R" + "un",
            "Exec" + "ute",
            "Sta" + "rt",
            "Launch",
            "Auto" + "mate",
            "App" + "ly",
            "Fix " + "now",
            "Write " + "files",
            "Open " + "browser",
            "Start " + "CDP",
            "Start " + "live",
            "Send",
            "Deploy"
        };

        foreach (var label in forbiddenButtonLabels)
        {
            Assert.IsFalse(Regex.IsMatch(section, $@"<button[^>]*>\s*{Regex.Escape(label)}\b", RegexOptions.IgnoreCase), label);
        }

        foreach (var forbiddenClaim in new[] { "production" + "-ready", "production " + "ready", "can execute/live " + "automate", "live automation " + "ready" })
        {
            Assert.IsFalse(section.Contains(forbiddenClaim, StringComparison.OrdinalIgnoreCase), forbiddenClaim);
        }

        StringAssert.Contains(section, "Copy read-only preview");
        StringAssert.Contains(section, "Copy read-only handoff preview");
    }

    [TestMethod]
    public void RecipeLabSurfaceHasResponsiveStyles()
    {
        var css = ReadRepoText(SidepanelCssPath);

        StringAssert.Contains(css, ".recipe-lab-surface");
        StringAssert.Contains(css, ".recipe-lab-grid");
        StringAssert.Contains(css, ".recipe-lab-layout");
        StringAssert.Contains(css, ".recipe-lab-report");
        StringAssert.Contains(css, ".recipe-lab-grid,");
        StringAssert.Contains(css, "grid-template-columns: 1fr;");
    }

    private static string ExtractRecipeLabSurface(string html)
    {
        const string startMarker = "id=\"recipeLabSurface\"";
        const string endMarker = "id=\"evidenceIntelligenceSurface\"";
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        var end = html.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Recipe Lab surface is missing.");
        Assert.IsTrue(end > start, "Recipe Lab surface end marker is missing.");
        return html[start..end];
    }

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }
}
