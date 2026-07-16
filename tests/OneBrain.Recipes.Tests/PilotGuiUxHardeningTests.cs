using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AppProfiles;
using OneBrain.Core.Approval;
using OneBrain.Core.Memory;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotGuiUxHardeningTests
{
    [TestMethod]
    public void Home_Render_Includes_All_Primary_Navigation_Links()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "href=\"/recipes\"");
        StringAssert.Contains(html, "href=\"/variables\"");
        StringAssert.Contains(html, "href=\"/memory\"");
        StringAssert.Contains(html, "href=\"/app-profiles\"");
        StringAssert.Contains(html, "href=\"/approvals/demo\"");
        StringAssert.Contains(html, "href=\"/runs\"");
        StringAssert.Contains(html, "href=\"/pilot/legacy/pilot/legacy/pilot/legacy/ai/config\"");
        StringAssert.Contains(html, "href=\"/ai/audit\"");
        StringAssert.Contains(html, "Guiarme paso a paso");
        StringAssert.Contains(html, "Tareas");
        StringAssert.Contains(html, "Historial");
        StringAssert.Contains(html, "Configuracion");
        StringAssert.Contains(html, "Links secundarios");
    }

    [TestMethod]
    public void Internal_Pages_Keep_Safety_Always_Visible()
    {
        var pages = new[]
        {
            PilotHomePageRenderer.RenderRecipeList([]),
            PilotHomePageRenderer.RenderVariables([]),
            PilotHomePageRenderer.RenderProcessMemory([], new WorkflowRetrievalResult(new WorkflowRetrievalQuery(), [])),
            PilotHomePageRenderer.RenderAppProfiles([]),
            PilotHomePageRenderer.RenderApprovalDemo(BusinessFlowDemoFixture.CreateSendMessageApproval(), BusinessFlowDemoFixture.CreateConfidenceProfile()),
            PilotHomePageRenderer.RenderRunHistory([]),
            PilotHomePageRenderer.RenderAIConfigConsole([]),
            PilotHomePageRenderer.RenderAIAuditLog([])
        };

        foreach (var html in pages)
        {
            StringAssert.Contains(html, "Modo seguro visible");
            StringAssert.Contains(html, "0 clicks");
            StringAssert.Contains(html, "0 cookies aceptadas");
            StringAssert.Contains(html, "0 login");
            StringAssert.Contains(html, "0 carrito");
            StringAssert.Contains(html, "0 compra");
            StringAssert.Contains(html, "0 pago");
        }
    }

    [TestMethod]
    public void Core_Pilot_Pages_Explain_User_Facing_Concepts()
    {
        var recipesHtml = PilotHomePageRenderer.RenderRecipeList([]);
        var variablesHtml = PilotHomePageRenderer.RenderVariables([]);
        var memoryHtml = PilotHomePageRenderer.RenderProcessMemory([], new WorkflowRetrievalResult(new WorkflowRetrievalQuery(), []));
        var profilesHtml = PilotHomePageRenderer.RenderAppProfiles([]);
        var approvalsHtml = PilotHomePageRenderer.RenderApprovalDemo(BusinessFlowDemoFixture.CreateSendMessageApproval(), BusinessFlowDemoFixture.CreateConfidenceProfile());
        var runsHtml = PilotHomePageRenderer.RenderRunHistory([]);
        var auditHtml = PilotHomePageRenderer.RenderAIAuditLog([]);

        StringAssert.Contains(recipesHtml, "Una tarea automatizable es una accion permitida y revisable.");
        StringAssert.Contains(variablesHtml, "Los datos de la tarea son valores");
        StringAssert.Contains(memoryHtml, "Esta memoria ayuda a buscar procesos observados o aprendidos");
        StringAssert.Contains(profilesHtml, "Los perfiles de apps y sitios describen que sabe hacer ONE BRAIN");
        StringAssert.Contains(approvalsHtml, "Aprobacion humana");
        StringAssert.Contains(runsHtml, "Esta pantalla muestra ejecuciones locales");
        StringAssert.Contains(auditHtml, "Esta bitacora muestra por que el router recomendaria un perfil IA");
    }

    [TestMethod]
    public void Empty_States_Are_Explanatory()
    {
        var recipesHtml = PilotHomePageRenderer.RenderRecipeList([]);
        var variablesHtml = PilotHomePageRenderer.RenderVariables([]);
        var memoryHtml = PilotHomePageRenderer.RenderProcessMemory([], new WorkflowRetrievalResult(new WorkflowRetrievalQuery(), []));
        var profilesHtml = PilotHomePageRenderer.RenderAppProfiles([]);
        var runsHtml = PilotHomePageRenderer.RenderRunHistory([]);
        var auditHtml = PilotHomePageRenderer.RenderAIAuditLog([]);

        StringAssert.Contains(recipesHtml, "Todavia no hay tareas cargadas en esta vista.");
        StringAssert.Contains(variablesHtml, "No se detectaron datos en esta vista.");
        StringAssert.Contains(memoryHtml, "Todavia no hay procesos aprendidos cargados en esta vista.");
        StringAssert.Contains(memoryHtml, "La busqueda no encontro procesos parecidos con este criterio.");
        StringAssert.Contains(profilesHtml, "No hay perfiles cargados en esta vista.");
        StringAssert.Contains(runsHtml, "Todavia no hay ejecuciones registradas.");
        StringAssert.Contains(auditHtml, "Todavia no hay decisiones de IA registradas.");
    }

    [TestMethod]
    public void Execution_Result_Shows_Selectable_Paths_Without_Auto_Open()
    {
        var plan = new PilotPlanBuilder().Build(new PilotIntentRouter().Route("genera html demo"));
        var result = new PilotExecutionResult(
            Plan: plan,
            Executed: true,
            Success: true,
            ExitCode: 0,
            Status: "OK",
            RecipePath: plan.Intent.Recipe?.RecipePath,
            LatestMarkdownPath: @"artifacts/product-evidence-demo-reports/demo.md",
            LatestHtmlPath: @"artifacts/product-evidence-demo-html-reports/demo.html",
            ArtifactsFolder: "artifacts",
            StandardOutput: "success",
            StandardError: "",
            Safety: PilotSafetySummary.LabDevRuntimeFootprintDefaultBlocked);

        var html = PilotHomePageRenderer.Render(plan, result);

        StringAssert.Contains(html, "Selecciona y copia manualmente");
        StringAssert.Contains(html, "Pilot nunca abre archivos automaticamente");
        StringAssert.Contains(html, "Ruta del reporte Markdown");
        StringAssert.Contains(html, "Ruta del reporte HTML");
        StringAssert.Contains(html, "artifacts/product-evidence-demo-html-reports/demo.html");
    }

    [TestMethod]
    public void Missing_Detail_Pages_Show_Actionable_Not_Found_State()
    {
        var memoryHtml = PilotHomePageRenderer.RenderProcessMemoryDetail(null);
        var profileHtml = PilotHomePageRenderer.RenderAppProfileDetail(null);

        StringAssert.Contains(memoryHtml, "Proceso aprendido no encontrado");
        StringAssert.Contains(memoryHtml, "Volver al inicio");
        StringAssert.Contains(profileHtml, "App o sitio no encontrado");
        StringAssert.Contains(profileHtml, "Volver al inicio");
        StringAssert.Contains(memoryHtml, "sin ejecucion");
        StringAssert.Contains(profileHtml, "sin ejecucion");
    }

    [TestMethod]
    public void Main_User_Facing_Titles_Avoid_English_Primary_Names()
    {
        var html = string.Join('\n', [
            PilotHomePageRenderer.Render(),
            PilotHomePageRenderer.RenderRecipeList([]),
            PilotHomePageRenderer.RenderVariables([]),
            PilotHomePageRenderer.RenderRunHistory([]),
            PilotHomePageRenderer.RenderAIAuditLog([]),
            PilotHomePageRenderer.RenderAppProfiles([]),
            PilotHomePageRenderer.RenderApprovalDemo(BusinessFlowDemoFixture.CreateSendMessageApproval(), BusinessFlowDemoFixture.CreateConfidenceProfile())
        ]);

        Assert.IsFalse(html.Contains(">Recipes<", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(">Variables<", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(">Runs<", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(">AI Audit<", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(">App Profiles<", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(">Memory<", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(">Approvals<", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Pilot_Runner_Prints_Manual_Test_Routes()
    {
        var script = File.ReadAllText(GetScriptPath());

        StringAssert.Contains(script, "PILOT_ROUTE_HOME");
        StringAssert.Contains(script, "PILOT_ROUTE_RECIPES");
        StringAssert.Contains(script, "PILOT_ROUTE_VARIABLES");
        StringAssert.Contains(script, "PILOT_ROUTE_MEMORY");
        StringAssert.Contains(script, "PILOT_ROUTE_APP_PROFILES");
        StringAssert.Contains(script, "PILOT_ROUTE_APPROVALS");
        StringAssert.Contains(script, "PILOT_ROUTE_RUNS");
        StringAssert.Contains(script, "PILOT_ROUTE_AI_CONFIG");
        StringAssert.Contains(script, "PILOT_ROUTE_AI_AUDIT");
    }

    private static string GetScriptPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "tools", "scripts", "run-onebrain-pilot.ps1");
    }
}
