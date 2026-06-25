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
[TestCategory("M1221")]
[TestCategory("M1222")]
[TestCategory("M1223")]
[TestCategory("M1224")]
[TestCategory("M1225")]
[TestCategory("M1226")]
[TestCategory("M1227")]
[TestCategory("M1228")]
[TestCategory("M1229")]
[TestCategory("M1230")]
[TestCategory("M1231")]
[TestCategory("M1232")]
[TestCategory("M1233")]
[TestCategory("M1234")]
[TestCategory("M1235")]
[TestCategory("M1236")]
[TestCategory("M1237")]
[TestCategory("M1238")]
[TestCategory("M1239")]
[TestCategory("M1240")]
[TestCategory("M1241")]
[TestCategory("M1242")]
[TestCategory("M1243")]
[TestCategory("M1244")]
[TestCategory("M1245")]
[TestCategory("M1246")]
[TestCategory("M1247")]
[TestCategory("M1248")]
[TestCategory("M1249")]
[TestCategory("M1250")]
[TestCategory("M1251")]
[TestCategory("M1252")]
[TestCategory("M1253")]
[TestCategory("M1254")]
[TestCategory("M1255")]
[TestCategory("M1256")]
[TestCategory("M1257")]
[TestCategory("M1258")]
[TestCategory("M1259")]
[TestCategory("M1260")]
[TestCategory("M1261")]
[TestCategory("M1262")]
[TestCategory("M1263")]
[TestCategory("M1264")]
[TestCategory("M1265")]
[TestCategory("M1266")]
[TestCategory("M1267")]
[TestCategory("M1268")]
[TestCategory("M1161M1172")]
public sealed class NodalOsProductVisibleLocalDemoM1161M1172Tests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ReportPath = "docs/reports/m1172-product-visible-local-demo-v0.md";
    private const string DemoV1ReportPath = "docs/reports/m1220-product-demo-v1-mission-creation-local-history.md";
    private const string DemoV2ReportPath = "docs/reports/m1232-product-demo-v2-mission-editing-demo-recording.md";
    private const string DemoV3ReportPath = "docs/reports/m1244-product-demo-v3-visual-qa-demo-recording.md";
    private const string DemoV4ReportPath = "docs/reports/m1256-product-demo-v4-guided-recording-polish.md";
    private const string DemoV5ReportPath = "docs/reports/m1268-product-demo-v5-installed-sidepanel-verification.md";

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
            "demoRunCount",
            "missionEditFields",
            "demoScriptPanel",
            "runNoteInput"
        })
        {
            StringAssert.Contains(html, id);
        }

        StringAssert.Contains(html, "Checklist para grabar");
        StringAssert.Contains(html, "Abrir Mission Control");
        StringAssert.Contains(html, "Copiar script o resumen para cerrar la demo");
        StringAssert.Contains(html, "sin shell, filesystem ni cloud");
        StringAssert.Contains(html, "Nueva misión");
        StringAssert.Contains(html, "Qué querés probar o avanzar");
        StringAssert.Contains(html, "Historial");
        StringAssert.Contains(html, "Guion demo");
        StringAssert.Contains(html, "Nota del run");
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
        StringAssert.Contains(js, "saveMissionEdit");
        StringAssert.Contains(js, "deleteActiveMission");
        StringAssert.Contains(js, "saveRunNote");
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
        StringAssert.Contains(css, ".mission-editor-card");
        StringAssert.Contains(css, ".mission-script-card");
        StringAssert.Contains(css, ".mission-run-note-card");
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
        StringAssert.Contains(html, "Alcance demo");
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
    public void DemoV2SupportsMissionEditingDeleteRunNotesAndDemoScript()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(html, "editMissionBtn");
        StringAssert.Contains(html, "saveMissionBtn");
        StringAssert.Contains(html, "cancelMissionEditBtn");
        StringAssert.Contains(html, "deleteMissionBtn");
        StringAssert.Contains(html, "saveRunNoteBtn");
        StringAssert.Contains(html, "clearRunNoteBtn");
        StringAssert.Contains(html, "copyDemoScriptBtn");
        StringAssert.Contains(js, "schemaVersion: 2");
        StringAssert.Contains(js, "DEMO_SCRIPT_STEPS");
        StringAssert.Contains(js, "copyDemoScript");
        StringAssert.Contains(js, "renderDemoScript()");
        StringAssert.Contains(js, "renderRunNoteEditor()");
        StringAssert.Contains(js, "formatDuration");
        StringAssert.Contains(js, "run.note");
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

    [TestMethod]
    public void DemoV2ReportDocumentsEditingNotesAndRecordingFlow()
    {
        var report = ReadRepoText(DemoV2ReportPath);

        StringAssert.Contains(report, "Product Demo v2");
        StringAssert.Contains(report, "Editar misión");
        StringAssert.Contains(report, "Borrar misión");
        StringAssert.Contains(report, "Nota del run");
        StringAssert.Contains(report, "Demo script");
        StringAssert.Contains(report, "Copiar script");
    }

    [TestMethod]
    public void DemoV3VisualQaKeepsMissionControlCleanAndRecordable()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var css = ReadRepoText(SidepanelCssPath);
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(html, "Checklist para grabar");
        StringAssert.Contains(html, "Abrir Mission Control");
        StringAssert.Contains(html, "Agregar una nota al run");
        StringAssert.Contains(html, "Guion demo");
        StringAssert.Contains(html, "Logs / evidencia");
        StringAssert.Contains(html, "Alcance demo");
        StringAssert.Contains(css, ".mission-timeline .timeline-meta");
        StringAssert.Contains(css, ".mission-timeline .timeline-safe-action");
        StringAssert.Contains(css, ".mission-timeline .timeline-redaction");
        StringAssert.Contains(css, "max-height: 270px");
        StringAssert.Contains(js, "Abrí NODAL OS y presentá Mission Control");
        StringAssert.Contains(js, "una demo local, visible y compartible");
        StringAssert.Contains(js, "recording_flow: Mission Control -> Run demo -> Historial -> Copiar resumen");
    }

    [TestMethod]
    public void DemoV3ReportDocumentsVisualQaAndRecordingChecklist()
    {
        var report = ReadRepoText(DemoV3ReportPath);

        StringAssert.Contains(report, "Product Demo v3");
        StringAssert.Contains(report, "QA visual real");
        StringAssert.Contains(report, "Checklist para grabar");
        StringAssert.Contains(report, "Run demo");
        StringAssert.Contains(report, "Copiar resumen");
    }

    [TestMethod]
    public void DemoV4AddsLightweightOnboardingStepperAndReadyState()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var css = ReadRepoText(SidepanelCssPath);
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(html, "demoGuidanceCard");
        StringAssert.Contains(html, "Grabá una demo en 60-90 segundos");
        StringAssert.Contains(html, "Creá una misión");
        StringAssert.Contains(html, "Ejecutá una demo");
        StringAssert.Contains(html, "Revisá timeline e historial");
        StringAssert.Contains(html, "Copiá el resumen");
        StringAssert.Contains(html, "guideStepMission");
        StringAssert.Contains(html, "guideStepRun");
        StringAssert.Contains(html, "guideStepCopy");
        StringAssert.Contains(js, "Lista para grabar");
        StringAssert.Contains(css, ".mission-guidance-card");
        StringAssert.Contains(css, ".guided-stepper");
        StringAssert.Contains(css, ".demo-ready-card.is-ready");
        StringAssert.Contains(js, "DEMO_GUIDANCE_COLLAPSED_KEY");
        StringAssert.Contains(js, "renderDemoGuidance()");
        StringAssert.Contains(js, "demoRecordingReadiness()");
        StringAssert.Contains(js, "saveDemoGuidanceCollapsed");
    }

    [TestMethod]
    public void DemoV4ScriptAndChecklistRemainProductCopy()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(js, "Abrí NODAL OS y presentá Mission Control");
        StringAssert.Contains(js, "una demo local, visible y compartible");
        StringAssert.Contains(html, "Checklist para grabar");
        StringAssert.Contains(html, "Copiar script o resumen para cerrar la demo");
        Assert.IsFalse(html.Contains("NO-GO", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("claim guard", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("operator confirmation", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("caveat", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void DemoV4ReportDocumentsGuidedRecordingPolish()
    {
        var report = ReadRepoText(DemoV4ReportPath);

        StringAssert.Contains(report, "Product Demo v4");
        StringAssert.Contains(report, "onboarding liviano");
        StringAssert.Contains(report, "Paso 1");
        StringAssert.Contains(report, "Lista para grabar");
        StringAssert.Contains(report, "60-90 segundos");
    }

    [TestMethod]
    public void DemoV5ReportDocumentsInstalledSidepanelAttemptAndDryRunBoundary()
    {
        var report = ReadRepoText(DemoV5ReportPath);

        StringAssert.Contains(report, "Product Demo v5");
        StringAssert.Contains(report, "sidepanel real instalado");
        StringAssert.Contains(report, "chrome-extension://");
        StringAssert.Contains(report, "--load-extension");
        StringAssert.Contains(report, "extensión mínima temporal");
        StringAssert.Contains(report, "dry run");
        StringAssert.Contains(report, "60-90 segundos");
        StringAssert.Contains(report, "No se inventa QA instalada");
    }

    [TestMethod]
    public void WorkspaceOpenAndProjectUnderstandingAreVisibleInMissionControl()
    {
        var html = ReadRepoText(SidepanelHtmlPath);

        StringAssert.Contains(html, "workspaceUnderstanding");
        StringAssert.Contains(html, "Proyecto activo");
        StringAssert.Contains(html, "Abrir workspace");
        StringAssert.Contains(html, "Releer");
        StringAssert.Contains(html, "Quitar");
        StringAssert.Contains(html, "workspaceStatus");
        StringAssert.Contains(html, "workspaceName");
        StringAssert.Contains(html, "workspacePath");
        StringAssert.Contains(html, "workspaceReadMode");
        StringAssert.Contains(html, "workspaceSignals");
        StringAssert.Contains(html, "workspaceKeyFiles");
        StringAssert.Contains(html, "workspaceTree");
        StringAssert.Contains(html, "workspaceEvidence");
        StringAssert.Contains(html, "missionWorkspaceContext");
    }

    [TestMethod]
    public void WorkspaceScanUsesReadOnlyPickerMetadataAndLocalStorage()
    {
        var js = ReadRepoText(SidepanelJsPath);

        foreach (var expected in new[]
        {
            "WORKSPACE_STORE_KEY",
            "nodal-os.workspaceUnderstanding.v1",
            "showDirectoryPicker({ mode: 'read' })",
            "openWorkspaceDirectory",
            "scanWorkspaceDirectory",
            "walkWorkspaceDirectory",
            "WORKSPACE_IGNORED_DIRS",
            "node_modules",
            ".git",
            "dist",
            "build",
            "coverage",
            "package.json",
            ".slnx",
            ".csproj",
            "manifest.json",
            "browser-extension",
            "Workspace leído",
            "No se ejecutaron comandos.",
            "No se modificaron archivos."
        })
        {
            StringAssert.Contains(js, expected);
        }

        Assert.IsFalse(js.Contains("createWritable(", StringComparison.Ordinal));
        Assert.IsFalse(js.Contains("removeEntry(", StringComparison.Ordinal));
        Assert.IsFalse(js.Contains("FileSystemWritableFileStream", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WorkspaceUnderstandingIsIntegratedWithMissionAndReport()
    {
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(js, "attachWorkspaceToActiveMission");
        StringAssert.Contains(js, "workspaceMissionSummary");
        StringAssert.Contains(js, "mission.workspace");
        StringAssert.Contains(js, "Workspace en contexto");
        StringAssert.Contains(js, "workspace_status:");
        StringAssert.Contains(js, "workspace_stack:");
        StringAssert.Contains(js, "workspace_counts:");
    }

    [TestMethod]
    public void WorkspaceStylingKeepsProductSurfaceReadable()
    {
        var css = ReadRepoText(SidepanelCssPath);

        StringAssert.Contains(css, ".workspace-understanding-card");
        StringAssert.Contains(css, ".workspace-status-grid");
        StringAssert.Contains(css, ".workspace-summary-grid");
        StringAssert.Contains(css, ".workspace-panel");
        StringAssert.Contains(css, ".workspace-tree");
        StringAssert.Contains(css, ".workspace-evidence");
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
