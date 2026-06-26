using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CdpBrowserSkillsProductSurface")]
public sealed class CdpBrowserSkillsProductSurfaceTests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";

    [TestMethod]
    public void BrowserSkillsCdpSurfaceIsVisibleInMissionControl()
    {
        var section = ExtractBrowserSkillsSection(ReadRepoText(SidepanelHtmlPath));

        StringAssert.Contains(section, "Browser Skills CDP");
        StringAssert.Contains(section, "CloakBrowser CDP");
        StringAssert.Contains(section, "Runtime principal");
        StringAssert.Contains(section, "legacy / no-default");
        StringAssert.Contains(section, "Copiar resumen CDP");
        StringAssert.Contains(section, "source:");
        StringAssert.Contains(section, "cloakbrowser-cdp-direct");
        StringAssert.Contains(section, "metadata-only");
        StringAssert.Contains(section, "Sin extensión");
        StringAssert.Contains(section, "Sin navegador del sistema");
        StringAssert.Contains(section, "Sin navegación externa");
        StringAssert.Contains(section, "No se modificaron archivos");
    }

    [TestMethod]
    public void BrowserSkillsCdpSurfaceUsesReadOnlyBridgeModel()
    {
        var js = ReadRepoText(SidepanelJsPath);

        foreach (var expected in new[]
        {
            "CDP_BROWSER_SKILLS_SURFACE",
            "runtimeLabel: 'CloakBrowser CDP'",
            "source: 'cloakbrowser-cdp-direct'",
            "extensionMode: 'legacy / no-default'",
            "extensionUsed: false",
            "systemBrowserUsed: false",
            "externalNavigationBlocked: true",
            "productFilesModified: false",
            "readOnly: true",
            "domIndex: 'metadata-only'",
            "renderCdpBrowserSkillsSurface",
            "copyCdpBrowserSkillSummary",
            "buildCdpBrowserSkillSummary"
        })
        {
            StringAssert.Contains(js, expected);
        }
    }

    [TestMethod]
    public void BrowserSkillsCdpCopySummaryIsMetadataOnly()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var start = js.IndexOf("function buildCdpBrowserSkillSummary()", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "CDP copy summary function is missing.");
        var end = js.IndexOf("function buildBrowserSkillSummary", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "CDP copy summary function end marker is missing.");
        var summaryFunction = js[start..end];

        foreach (var expected in new[]
        {
            "runtime:",
            "source:",
            "readOnly:",
            "interactiveElements:",
            "frictionSignals:",
            "actionMapEntries:",
            "evidenceAvailable:",
            "extensionUsed:",
            "systemBrowserUsed:",
            "externalNavigationBlocked:",
            "productFilesModified:",
            "rawDomStored: false",
            "inputValuesStored: false",
            "cookiesOrStorageStored: false"
        })
        {
            StringAssert.Contains(summaryFunction, expected);
        }

        foreach (var forbidden in new[] { "rawHtml", "innerHTML", "document.cookie", "localStorage.getItem", "sessionStorage", "password" })
        {
            Assert.IsFalse(summaryFunction.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void BrowserSkillsCdpSurfaceDoesNotExposeDangerousActions()
    {
        var section = ExtractCdpSurface(ReadRepoText(SidepanelHtmlPath));

        foreach (var forbidden in new[]
        {
            "Aplicar",
            "Ejecutar en página real",
            "Crear patch",
            "Crear diff",
            "login",
            "resolver captcha",
            "challenge handling",
            "stealth",
            "proxy"
        })
        {
            Assert.IsFalse(section.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void BrowserSkillsCdpSurfaceHasResponsiveStyles()
    {
        var css = ReadRepoText(SidepanelCssPath);

        StringAssert.Contains(css, ".browser-cdp-surface");
        StringAssert.Contains(css, ".browser-cdp-status-grid");
        StringAssert.Contains(css, ".browser-cdp-evidence");
        StringAssert.Contains(css, ".browser-cdp-status-grid,");
        StringAssert.Contains(css, "grid-template-columns: 1fr;");
    }

    private static string ExtractBrowserSkillsSection(string html)
    {
        const string startMarker = "id=\"browserSkillsWorkspace\"";
        const string endMarker = "<div class=\"mission-layout\">";
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        var end = html.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Browser Skills workspace is missing.");
        Assert.IsTrue(end > start, "Browser Skills section end marker is missing.");
        return html[start..end];
    }

    private static string ExtractCdpSurface(string html)
    {
        const string startMarker = "class=\"browser-cdp-surface\"";
        const string endMarker = "</article>";
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        var end = html.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Browser Skills CDP surface is missing.");
        Assert.IsTrue(end > start, "Browser Skills CDP surface end marker is missing.");
        return html[start..(end + endMarker.Length)];
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
