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
        StringAssert.Contains(html, "href=\"/ai/config\"");
        StringAssert.Contains(html, "href=\"/ai/audit\"");
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
            StringAssert.Contains(html, "Safety always visible");
            StringAssert.Contains(html, "0 clicks");
            StringAssert.Contains(html, "0 cookies accepted");
            StringAssert.Contains(html, "0 login");
            StringAssert.Contains(html, "0 carrito");
            StringAssert.Contains(html, "0 compra");
            StringAssert.Contains(html, "0 pago");
        }
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
            Safety: PilotSafetySummary.ZeroReadOnly);

        var html = PilotHomePageRenderer.Render(plan, result);

        StringAssert.Contains(html, "Select and copy manually");
        StringAssert.Contains(html, "Pilot never opens files automatically");
        StringAssert.Contains(html, "Markdown report path");
        StringAssert.Contains(html, "HTML report path");
        StringAssert.Contains(html, "artifacts/product-evidence-demo-html-reports/demo.html");
    }

    [TestMethod]
    public void Missing_Detail_Pages_Show_Actionable_Not_Found_State()
    {
        var memoryHtml = PilotHomePageRenderer.RenderProcessMemoryDetail(null);
        var profileHtml = PilotHomePageRenderer.RenderAppProfileDetail(null);

        StringAssert.Contains(memoryHtml, "Process memory entry not found");
        StringAssert.Contains(memoryHtml, "Back to memory");
        StringAssert.Contains(profileHtml, "App profile not found");
        StringAssert.Contains(profileHtml, "Back to app profiles");
        StringAssert.Contains(memoryHtml, "no execution");
        StringAssert.Contains(profileHtml, "no execution");
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
