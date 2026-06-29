using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
[TestCategory("EvidenceIntelligenceReadOnlyUiMount")]
public sealed class EvidenceIntelligenceReadOnlyUiMountProductSurfaceTests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";

    [TestMethod]
    public void EvidenceIntelligenceSurfaceIsVisibleInMissionControlNavigation()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var section = ExtractEilSurface(html);

        StringAssert.Contains(html, "href=\"#evidenceIntelligenceSurface\"");
        StringAssert.Contains(section, "data-eil-mount=\"evidence-intelligence.ui.read-only.mount.v1\"");
        StringAssert.Contains(section, "Evidence Intelligence");
        StringAssert.Contains(section, "READ_ONLY");
        StringAssert.Contains(section, "LOCAL_ONLY");
        StringAssert.Contains(section, "NO_RUNTIME");
        StringAssert.Contains(section, "NO_LIVE_AUTOMATION");
        StringAssert.Contains(section, "Evidence Index Summary");
        StringAssert.Contains(section, "Lexical search results");
        StringAssert.Contains(section, "Claim scan verdict");
        StringAssert.Contains(section, "Action scan verdict");
        StringAssert.Contains(section, "Typed evidence graph");
        StringAssert.Contains(section, "Action readiness matrix");
    }

    [TestMethod]
    public void EvidenceIntelligenceSurfaceRendersSafetyNotices()
    {
        var section = ExtractEilSurface(ReadRepoText(SidepanelHtmlPath));

        foreach (var expected in new[]
        {
            "Read-only local evidence audit surface",
            "Local fixture / local evidence only",
            "Semantic backend disabled",
            "No runtime actions",
            "No live automation",
            "No browser/CDP automation",
            "No WCU live",
            "No OCR live",
            "No filesystem writes",
            "No provider/cloud calls",
            "Human approval required for any real-world action"
        })
        {
            StringAssert.Contains(section, expected);
        }
    }

    [TestMethod]
    public void EvidenceIntelligenceJavascriptMountUsesPresenterFixtureSnapshot()
    {
        var js = ReadRepoText(SidepanelJsPath);

        foreach (var expected in new[]
        {
            "EIL_READ_ONLY_SURFACE",
            "mountId: 'evidence-intelligence.ui.read-only.mount.v1'",
            "route: '#evidenceIntelligenceSurface'",
            "source: 'EvidenceIntelligenceReadOnlyPresenter.CreateFixture'",
            "surfaceId: 'evidence-intelligence.read-only-surface.v1'",
            "dataSource: 'deterministic local fixture'",
            "semanticBackendStatus: 'Disabled'",
            "semanticSearchAvailable: false",
            "lexicalFallbackIsReal: true",
            "runtimeEnabled: false",
            "actionExecutionEnabled: false",
            "browserCdpAutomationEnabled: false",
            "wcuLiveEnabled: false",
            "ocrLiveEnabled: false",
            "providerCloudEnabled: false",
            "durablePersistenceEnabled: false",
            "semanticVectorBackendEnabled: false",
            "filesystemWritesEnabled: false",
            "renderEvidenceIntelligenceSurface",
            "buildEilReportPreview",
            "copyEilReportPreview"
        })
        {
            StringAssert.Contains(js, expected);
        }
    }

    [TestMethod]
    public void EvidenceIntelligenceCopyPreviewIsMetadataOnlyAndNoRuntime()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var start = js.IndexOf("function buildEilReportPreview", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "EIL report preview function is missing.");
        var end = js.IndexOf("async function copyEilReportPreview", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "EIL copy function end marker is missing.");
        var summaryFunction = js[start..end];

        foreach (var expected in new[]
        {
            "READ_ONLY / LOCAL_ONLY / NO_RUNTIME / NO_LIVE_AUTOMATION",
            "copyMode: clipboard-only text preview",
            "semanticBackendStatus:",
            "semanticSearchAvailable:",
            "runtimeEnabled:",
            "actionExecutionEnabled:",
            "browserCdpAutomationEnabled:",
            "wcuLiveEnabled:",
            "ocrLiveEnabled:",
            "providerCloudEnabled:",
            "durablePersistenceEnabled:",
            "semanticVectorBackendEnabled:",
            "filesystemWritesEnabled:",
            "safeNextStep:"
        })
        {
            StringAssert.Contains(summaryFunction, expected);
        }

        foreach (var forbidden in new[] { "po" + "st(", "chrome." + "runtime", "fe" + "tch(", "XML" + "HttpRequest", "localStorage." + "setItem", "document." + "cookie" })
        {
            Assert.IsFalse(summaryFunction.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void EvidenceIntelligenceSurfaceDoesNotExposeLiveActionAffordances()
    {
        var section = ExtractEilSurface(ReadRepoText(SidepanelHtmlPath));
        var forbidden = new[]
        {
            "R" + "un",
            "Exec" + "ute",
            "App" + "ly",
            "Auto" + "mate",
            "Fix " + "now",
            "Launch " + "browser",
            "Start " + "live",
            "Write " + "files"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(Regex.IsMatch(section, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase), term);
        }

        StringAssert.Contains(section, "Copy read-only report preview");
    }

    [TestMethod]
    public void EvidenceIntelligenceSurfaceHasResponsiveStyles()
    {
        var css = ReadRepoText(SidepanelCssPath);

        StringAssert.Contains(css, ".evidence-intelligence-surface");
        StringAssert.Contains(css, ".evidence-intelligence-grid");
        StringAssert.Contains(css, ".evidence-intelligence-layout");
        StringAssert.Contains(css, ".evidence-intelligence-report");
        StringAssert.Contains(css, ".evidence-intelligence-grid,");
        StringAssert.Contains(css, "grid-template-columns: 1fr;");
    }

    private static string ExtractEilSurface(string html)
    {
        const string startMarker = "id=\"evidenceIntelligenceSurface\"";
        const string endMarker = "<div class=\"mission-layout\">";
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        var end = html.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Evidence Intelligence surface is missing.");
        Assert.IsTrue(end > start, "Evidence Intelligence surface end marker is missing.");
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
