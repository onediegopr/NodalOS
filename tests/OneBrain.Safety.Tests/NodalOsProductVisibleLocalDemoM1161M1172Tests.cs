using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProductVisibleLocalDemo")]
[TestCategory("M1161")]
[TestCategory("M1162")]
[TestCategory("M1163")]
[TestCategory("M1164")]
[TestCategory("M1165")]
[TestCategory("M1166")]
[TestCategory("M1167")]
[TestCategory("M1168")]
[TestCategory("M1169")]
[TestCategory("M1170")]
[TestCategory("M1171")]
[TestCategory("M1172")]
[TestCategory("M1209")]
[TestCategory("M1210")]
[TestCategory("M1211")]
[TestCategory("M1212")]
[TestCategory("M1213")]
[TestCategory("M1214")]
[TestCategory("M1215")]
[TestCategory("M1216")]
[TestCategory("M1217")]
[TestCategory("M1218")]
[TestCategory("M1219")]
[TestCategory("M1220")]
[TestCategory("M1161M1172")]
public sealed class NodalOsProductVisibleLocalDemoM1161M1172Tests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ReportPath = "docs/reports/m1172-product-visible-local-demo-v0.md";
    private const string DemoV1ReportPath = "docs/reports/m1220-product-demo-v1-mission-creation-local-history.md";

    [TestMethod]
    public void MissionControlShellIsVisibleInOperateTab()
    {
        var html = ReadRepoText(SidepanelHtmlPath);

        StringAssert.Contains(html, "mission-control-shell");
        StringAssert.Contains(html, "Mission Control");
        StringAssert.Contains(html, "NODAL OS");
        StringAssert.Contains(html, "Local Operator Demo");
        StringAssert.Contains(html, "Run demo");
        StringAssert.Contains(html, "Copiar resumen");
        StringAssert.Contains(html, "Modo avanzado");
    }

    [TestMethod]
    public void DemoSurfaceIncludesTimelineEvidenceStatusAndTryItChecklist()
    {
        var html = ReadRepoText(SidepanelHtmlPath);

        foreach (var id in new[]
        {
            "demoTimeline",
            "demoEvidencePanel",
            "demoTechnicalReport",
            "demoRunState",
            "demoHostStatus",
            "demoBridgeStatus",
            "demoBrowserClaimStatus",
            "demoScopeStatus",
            "demoTryChecklist",
            "missionCreateForm",
            "missionList",
            "demoRunHistory",
            "demoRunCount"
        })
        {
            StringAssert.Contains(html, id);
        }

        StringAssert.Contains(html, "started → accepted → evidence → completed");
        StringAssert.Contains(html, "sin shell, filesystem ni cloud");
        StringAssert.Contains(html, "Nueva misión");
        StringAssert.Contains(html, "Qué querés probar o avanzar");
        StringAssert.Contains(html, "Historial");
    }

    [TestMethod]
    public void DemoMissionSeedAndSafeNoOpRunAreInMemoryAndVisible()
    {
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(js, "createDemoSeed()");
        StringAssert.Contains(js, "loadDemoStore()");
        StringAssert.Contains(js, "saveDemoStore()");
        StringAssert.Contains(js, "localStorage");
        StringAssert.Contains(js, "Local Operator Demo");
        StringAssert.Contains(js, "SafeNoOp");
        StringAssert.Contains(js, "runSafeDemo()");
        StringAssert.Contains(js, "createMissionFromForm");
        StringAssert.Contains(js, "selectDemoRun");
        StringAssert.Contains(js, "EvidenceProjection");
        StringAssert.Contains(js, "Completed with no side effects");
        StringAssert.Contains(js, "buildDemoTechnicalReport()");
    }

    [TestMethod]
    public void SafeDemoRunDoesNotUseRuntimeBridgeNetworkOrDangerousApis()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var body = ExtractFunctionBody(js, "runSafeDemo");

        foreach (var forbidden in new[]
        {
            "post(",
            "chrome.runtime",
            "fetch(",
            "XMLHttpRequest",
            "WebSocket",
            "document.cookie",
            "eval(",
            "new Function"
        })
        {
            Assert.IsFalse(body.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        StringAssert.Contains(body, "state.demo");
        StringAssert.Contains(body, "mission.runs.unshift(run)");
        StringAssert.Contains(body, "saveDemoStore()");
        StringAssert.Contains(body, "addLog('local'");
        StringAssert.Contains(body, "render();");
    }

    [TestMethod]
    public void MissionControlUsesDarkProductStyling()
    {
        var css = ReadRepoText(SidepanelCssPath);

        StringAssert.Contains(css, "--nos-color-bg: #090D12");
        StringAssert.Contains(css, ".mission-control-shell");
        StringAssert.Contains(css, ".mission-sidebar");
        StringAssert.Contains(css, ".mission-run-button");
        StringAssert.Contains(css, ".mission-workbench");
        StringAssert.Contains(css, ".run-history-list");
        StringAssert.Contains(css, ".mission-timeline");
        StringAssert.Contains(css, ".technical-report");
    }

    [TestMethod]
    public void DemoReportDocumentsHowToOpenAndWhatIsSimulated()
    {
        var report = ReadRepoText(ReportPath);

        StringAssert.Contains(report, "Product visible local demo v0");
        StringAssert.Contains(report, "Cómo abrirlo");
        StringAssert.Contains(report, "Run safe demo");
        StringAssert.Contains(report, "no-op local/in-memory");
        StringAssert.Contains(report, "M1173-M1184");
    }

    [TestMethod]
    public void DemoCopyAndMicrocopyAvoidProtocolHeavyUx()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(html, "Demo segura");
        StringAssert.Contains(html, "Listo para probar");
        StringAssert.Contains(html, "Demo scope");
        StringAssert.Contains(js, "Sin acciones peligrosas");
        StringAssert.Contains(js, "Límites");
        StringAssert.Contains(js, "Revisar antes de seguir");
        Assert.IsFalse(html.Contains("Smoke caveat", StringComparison.Ordinal));
        Assert.IsFalse(js.Contains("BrowserRuntimeSmoke caveat visible", StringComparison.Ordinal));
        Assert.IsFalse(js.Contains("Blocked by policy", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("MANUAL_QA_HOLD_ACTIVE", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("NOT_ELIGIBLE_EVIDENCE_PENDING", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DemoV1SupportsMissionCreationLocalHistoryAndReopenRun()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(html, "missionTitleInput");
        StringAssert.Contains(html, "missionDescriptionInput");
        StringAssert.Contains(html, "clearDemoHistoryBtn");
        StringAssert.Contains(js, "DEMO_STORE_KEY");
        StringAssert.Contains(js, "activeMissionId");
        StringAssert.Contains(js, "selectedRunId");
        StringAssert.Contains(js, "runs: []");
        StringAssert.Contains(js, "renderDemoMissionList()");
        StringAssert.Contains(js, "renderDemoRunHistory()");
        StringAssert.Contains(js, "data-demo-run-id");
        StringAssert.Contains(js, "composeDemoTechnicalReport");
    }

    [TestMethod]
    public void DemoV1ReportDocumentsMissionHistoryAndHowToTryIt()
    {
        var report = ReadRepoText(DemoV1ReportPath);

        StringAssert.Contains(report, "Product Demo v1");
        StringAssert.Contains(report, "Crear misión");
        StringAssert.Contains(report, "localStorage");
        StringAssert.Contains(report, "Run demo");
        StringAssert.Contains(report, "Historial");
        StringAssert.Contains(report, "Copiar resumen");
    }

    private static string ExtractFunctionBody(string source, string functionName)
    {
        var match = Regex.Match(source, $@"function\s+{Regex.Escape(functionName)}\s*\([^)]*\)\s*\{{", RegexOptions.CultureInvariant);
        Assert.IsTrue(match.Success, $"Function {functionName} was not found.");
        var start = match.Index + match.Length;
        var depth = 1;
        for (var i = start; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[start..i];
                }
            }
        }

        Assert.Fail($"Function {functionName} body was not closed.");
        return string.Empty;
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
