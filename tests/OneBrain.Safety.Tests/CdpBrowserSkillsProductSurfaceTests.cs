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
        StringAssert.Contains(section, "Boundary");
        StringAssert.Contains(section, "Runtime status");
        StringAssert.Contains(section, "Última captura");
        StringAssert.Contains(section, "Freshness");
        StringAssert.Contains(section, "Última verificación CDP");
        StringAssert.Contains(section, "Runtime shutdown");
        StringAssert.Contains(section, "Proceso");
        StringAssert.Contains(section, "legacy / no-default");
        StringAssert.Contains(section, "Actualizar estado");
        StringAssert.Contains(section, "Copiar resumen CDP");
        StringAssert.Contains(section, "Última actualización");
        StringAssert.Contains(section, "Refresh source");
        StringAssert.Contains(section, "Runtime lanzado");
        StringAssert.Contains(section, "CDP live");
        StringAssert.Contains(section, "Canal local");
        StringAssert.Contains(section, "Snapshot");
        StringAssert.Contains(section, "safe-local-status-snapshot");
        StringAssert.Contains(section, "source:");
        StringAssert.Contains(section, "cloakbrowser-cdp-direct");
        StringAssert.Contains(section, "metadata-only");
        StringAssert.Contains(section, "Evidence read");
        StringAssert.Contains(section, "Sin extensión");
        StringAssert.Contains(section, "Sin navegador del sistema");
        StringAssert.Contains(section, "Sin navegación externa");
        StringAssert.Contains(section, "No se modificaron archivos");
        StringAssert.Contains(section, "Boundary read-only");
        StringAssert.Contains(section, "Artifact hash");
        StringAssert.Contains(section, "Proceso huérfano");
    }

    [TestMethod]
    public void BrowserSkillsCdpSurfaceUsesReadOnlyBridgeModel()
    {
        var js = ReadRepoText(SidepanelJsPath);

        foreach (var expected in new[]
        {
            "CDP_BROWSER_SKILLS_SURFACE",
            "runtimeLabel: 'CloakBrowser CDP'",
            "status: 'listo'",
            "freshness: 'reciente'",
            "runtimeStatus: 'configurado'",
            "lastCaptureStatus: 'disponible por harness'",
            "lastHealthcheckStatus: 'reciente por harness'",
            "evidenceStatus: 'disponible'",
            "runtimeShutdown: true",
            "processExited: true",
            "orphanProcessDetected: false",
            "hashStatus: 'verificado'",
            "artifactPinned: true",
            "commandBoundary: 'solo lectura'",
            "source: 'cloakbrowser-cdp-direct'",
            "extensionMode: 'legacy / no-default'",
            "extensionUsed: false",
            "systemBrowserUsed: false",
            "externalNavigationBlocked: true",
            "productFilesModified: false",
            "readOnly: true",
            "boundaryReadOnly: true",
            "lastRefreshAt: 'sin actualizar'",
            "refreshSource: 'local-redacted-evidence'",
            "evidenceRead: true",
            "runtimeLaunched: false",
            "cdpLiveExecuted: false",
            "runtimeLaunchedFromUi: false",
            "cdpLiveExecutedFromUi: false",
            "channel: 'safe-local-status-snapshot'",
            "snapshotGeneratedAt: 'sin snapshot'",
            "snapshotFreshness: 'sin snapshot'",
            "domIndex: 'metadata-only'",
            "CDP_STATUS_SNAPSHOT_URL",
            "generated/cdp-status.snapshot.json",
            "applyCdpStatusSnapshot",
            "applyMissingCdpStatusSnapshot",
            "applyInvalidCdpStatusSnapshot",
            "renderCdpBrowserSkillsSurface",
            "refreshCdpStatus",
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
            "freshness:",
            "runtimeStatus:",
            "lastCaptureStatus:",
            "lastHealthcheckAt:",
            "lastSessionAt:",
            "lastRefreshAt:",
            "refreshSource:",
            "evidenceRead:",
            "runtimeLaunched:",
            "cdpLiveExecuted:",
            "runtimeLaunchedFromUi:",
            "cdpLiveExecutedFromUi:",
            "channel:",
            "snapshotGeneratedAt:",
            "snapshotFreshness:",
            "evidenceStatus:",
            "artifactPinned:",
            "hashStatus:",
            "runtimeShutdown:",
            "processExited:",
            "orphanProcessDetected:",
            "boundaryReadOnly:",
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

    [TestMethod]
    public void BrowserSkillsCdpRefreshReadsGeneratedSnapshot()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var start = js.IndexOf("async function refreshCdpStatus()", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "CDP refresh function is missing.");
        var end = js.IndexOf("function applyCdpStatusSnapshot", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "CDP snapshot apply function is missing.");
        var refreshFunction = js[start..end];

        StringAssert.Contains(refreshFunction, "fetch(`${CDP_STATUS_SNAPSHOT_URL}?t=${Date.now()}`");
        StringAssert.Contains(refreshFunction, "cache: 'no-store'");
        StringAssert.Contains(refreshFunction, "applyMissingCdpStatusSnapshot");
        StringAssert.Contains(refreshFunction, "applyInvalidCdpStatusSnapshot");
        Assert.IsFalse(refreshFunction.Contains("post(", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(refreshFunction.Contains("chrome.runtime", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(refreshFunction.Contains("runtimeLaunched = true", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(refreshFunction.Contains("cdpLiveExecuted = true", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserSkillsCdpRefreshShowsClearMissingSnapshotState()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var start = js.IndexOf("function applyEmptyCdpStatusSnapshot", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "CDP empty snapshot state function is missing.");
        var end = js.IndexOf("function cdpFreshnessLabel", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "CDP freshness label function is missing.");
        var emptyStateFunction = js[start..end];

        StringAssert.Contains(emptyStateFunction, "sin snapshot");
        StringAssert.Contains(emptyStateFunction, "sin snapshot local");
        StringAssert.Contains(emptyStateFunction, "elementCount = 0");
        StringAssert.Contains(emptyStateFunction, "evidenceAvailable = false");
        StringAssert.Contains(emptyStateFunction, "runtimeLaunchedFromUi = false");
        StringAssert.Contains(emptyStateFunction, "cdpLiveExecutedFromUi = false");
    }

    [TestMethod]
    public void InstalledSidepanelHarnessRemainsLegacyCompatibilityOnly()
    {
        var harness = ReadRepoText("scripts/verify-installed-sidepanel.mjs");

        StringAssert.Contains(harness, "browserRuntimeDefault: 'cloakbrowser-cdp-no-extension'");
        StringAssert.Contains(harness, "defaultRuntimeHarness: 'legacy-installed-sidepanel-compat-only'");
        StringAssert.Contains(harness, "installedSidepanelHarnessDefault: false");
        StringAssert.Contains(harness, "extensionRuntimeDefault: false");
        StringAssert.Contains(harness, "installed sidepanel harness remains legacy compatibility only");
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
