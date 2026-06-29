using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ReadOnlyProductSurfacesNavigationPolish")]
public sealed class ReadOnlyProductSurfacesNavigationPolishTests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";

    [TestMethod]
    public void MissionControlNavGroupsReadOnlyProductSurfaces()
    {
        var html = ReadRepoText(SidepanelHtmlPath);

        StringAssert.Contains(html, "Read-only surfaces");
        StringAssert.Contains(html, "href=\"#readOnlySurfacesSummary\"");
        StringAssert.Contains(html, "data-surface-kind=\"read-only-summary\"");
        StringAssert.Contains(html, "href=\"#recipeLabSurface\" data-surface-kind=\"read-only-product\" data-surface-id=\"recipe-lab\"");
        StringAssert.Contains(html, "href=\"#evidenceIntelligenceSurface\" data-surface-kind=\"read-only-product\" data-surface-id=\"evidence-intelligence\"");
    }

    [TestMethod]
    public void ReadOnlySurfacesSummaryExplainsSharedBoundary()
    {
        var summary = ExtractSection(ReadRepoText(SidepanelHtmlPath), "id=\"readOnlySurfacesSummary\"", "id=\"workspaceUnderstanding\"");

        foreach (var expected in new[]
        {
            "Read-only product surfaces",
            "Surface map",
            "Recipe Lab",
            "Evidence Intelligence",
            "Browser Skills CDP",
            "READ_ONLY",
            "NO_RUNTIME",
            "NO_LIVE_AUTOMATION",
            "HUMAN_REVIEW_REQUIRED",
            "No writes / no provider",
            "Copy previews are clipboard-only text"
        })
        {
            StringAssert.Contains(summary, expected);
        }
    }

    [TestMethod]
    public void ReadOnlySurfacesUseSharedVisualContracts()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var css = ReadRepoText(SidepanelCssPath);

        foreach (var expected in new[]
        {
            "read-only-product-surface",
            "read-only-surface-badges",
            "read-only-surface-notices",
            "read-only-surface-grid",
            "read-only-surface-layout",
            "read-only-surface-panel",
            ".read-only-surfaces-summary",
            ".read-only-surfaces-grid",
            ".mission-side-nav a[data-surface-kind]"
        })
        {
            StringAssert.Contains(html + "\n" + css, expected);
        }
    }

    [TestMethod]
    public void ReadOnlySurfaceCopyPreviewsRemainClipboardOnlyMetadata()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var previewBlock = ExtractSection(js, "function buildRecipeLabReportPreview", "function renderCdpBrowserSkillsSurface");

        foreach (var expected in new[]
        {
            "copyMode: clipboard-only text preview",
            "READ_ONLY / FIXTURE_SAFE / NO_RUNTIME / NO_LIVE_AUTOMATION",
            "READ_ONLY / LOCAL_ONLY / NO_RUNTIME / NO_LIVE_AUTOMATION",
            "navigator.clipboard.writeText",
            "runtimeEnabled:",
            "providerCloudEnabled:",
            "filesystemWritesEnabled:"
        })
        {
            StringAssert.Contains(previewBlock, expected);
        }

        foreach (var forbidden in new[] { "po" + "st(", "fe" + "tch(", "chrome." + "runtime", "XML" + "HttpRequest", "localStorage." + "setItem", "document." + "cookie" })
        {
            Assert.IsFalse(previewBlock.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void ReadOnlySurfacesDoNotExposeForbiddenActionButtons()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var recipeAndEvidence = ExtractSection(html, "id=\"recipeLabSurface\"", "<div class=\"mission-layout\">");

        foreach (var label in new[] { "R" + "un", "Exec" + "ute", "Sta" + "rt", "Launch", "Auto" + "mate", "App" + "ly", "Fix " + "now", "Write " + "files", "Open " + "browser", "Start " + "CDP", "Start " + "live", "Send", "Deploy" })
        {
            Assert.IsFalse(Regex.IsMatch(recipeAndEvidence, $@"<button[^>]*>\s*{Regex.Escape(label)}\b", RegexOptions.IgnoreCase), label);
        }
    }

    private static string ExtractSection(string text, string startMarker, string endMarker)
    {
        var start = text.IndexOf(startMarker, StringComparison.Ordinal);
        var end = text.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Missing start marker: {startMarker}");
        Assert.IsTrue(end > start, $"Missing end marker after {startMarker}: {endMarker}");
        return text[start..end];
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
