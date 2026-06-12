using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotShellTests
{
    [TestMethod]
    public void Home_Render_Includes_Pilot_Title_Input_And_Buttons()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "<title>ONE BRAIN Pilot</title>");
        StringAssert.Contains(html, "ONE BRAIN Pilot / recorrido local seguro");
        StringAssert.Contains(html, "textarea");
        StringAssert.Contains(html, "Guiarme paso a paso");
        StringAssert.Contains(html, "Probar demo HTML segura");
        StringAssert.Contains(html, "Ver tareas automatizables");
        StringAssert.Contains(html, "Revisar datos de la tarea");
        StringAssert.Contains(html, "Simular aprobacion humana");
        StringAssert.Contains(html, "/approvals/demo");
    }

    [TestMethod]
    public void Home_Render_Includes_Safety_Summary_Zeroes()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "0 clicks");
        StringAssert.Contains(html, "0 cookies");
        StringAssert.Contains(html, "0 login");
        StringAssert.Contains(html, "0 carrito");
        StringAssert.Contains(html, "0 compra");
        StringAssert.Contains(html, "0 pago");
        StringAssert.Contains(html, "ONE BRAIN no hace clicks, no inicia sesion, no acepta cookies, no compra, no paga y no envia nada sin aprobacion.");
    }

    [TestMethod]
    public void Home_Render_Includes_Guided_Workflow_And_Basic_Concepts()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "Flujo recomendado paso a paso");
        StringAssert.Contains(html, "Que hace ONE BRAIN Pilot");
        StringAssert.Contains(html, "Que no hara por seguridad");
        StringAssert.Contains(html, "Conceptos basicos");
        StringAssert.Contains(html, "Aprobacion humana");
        StringAssert.Contains(html, "Diagnostico");
        StringAssert.Contains(html, "Bloqueo seguro");
        StringAssert.Contains(html, "details class=\"help-text\"");
        StringAssert.Contains(html, "help-box");
        StringAssert.Contains(html, "step-number");
    }

    [TestMethod]
    public void Guide_Render_Includes_Step_Navigation_And_Resume()
    {
        var html = PilotHomePageRenderer.RenderGuide(4);

        StringAssert.Contains(html, "Guiarme paso a paso");
        StringAssert.Contains(html, "Simula aprobacion humana");
        StringAssert.Contains(html, "Anterior");
        StringAssert.Contains(html, "Siguiente");
        StringAssert.Contains(html, "Salir de la guia");
        StringAssert.Contains(html, "Retomar recorrido");
        StringAssert.Contains(html, "Abrir pantalla real");
    }

    [TestMethod]
    public void Home_Render_With_Result_Shows_Recipe_Status_And_Artifact_Paths()
    {
        var plan = new PilotPlanBuilder().Build(new PilotIntentRouter().Route("mostrame la demo"));
        var result = new PilotExecutionResult(
            Plan: plan,
            Executed: true,
            Success: true,
            ExitCode: 0,
            Status: "OK",
            RecipePath: plan.Intent.Recipe?.RecipePath,
            LatestMarkdownPath: @"C:\repo\artifacts\product-evidence-demo-reports\demo.md",
            LatestHtmlPath: @"C:\repo\artifacts\product-evidence-demo-html-reports\demo.html",
            ArtifactsFolder: @"C:\repo\artifacts",
            StandardOutput: "success",
            StandardError: "",
            Safety: PilotSafetySummary.ZeroReadOnly);

        var html = PilotHomePageRenderer.Render(plan, result);

        StringAssert.Contains(html, "OK");
        StringAssert.Contains(html, "demo.md");
        StringAssert.Contains(html, "demo.html");
        StringAssert.Contains(html, "success");
    }

    [TestMethod]
    public void Rendered_Pilot_Pages_Do_Not_Use_External_Scripts_Or_Cdns()
    {
        var pages = new[]
        {
            PilotHomePageRenderer.Render(),
            PilotHomePageRenderer.RenderRecipeList([]),
            PilotHomePageRenderer.RenderVariables([]),
            PilotHomePageRenderer.RenderRunHistory([]),
            PilotHomePageRenderer.RenderAIAuditLog([])
        };

        foreach (var html in pages)
        {
            Assert.IsFalse(html.Contains("<script src=", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("cdn", StringComparison.OrdinalIgnoreCase));
        }
    }

    [TestMethod]
    public void Recipe_Executor_StartInfo_Uses_Portable_Dotnet_And_Allowlisted_Recipe()
    {
        var startInfo = PilotRecipeExecutor.CreateRecipeRunStartInfo(
            @"C:\repo",
            PilotRecipeExecutor.DefaultDotnetPath,
            "tools/recipes/demo-product-evidence-report.json");

        Assert.AreEqual(PilotRecipeExecutor.DefaultDotnetPath, startInfo.FileName);
        Assert.IsFalse(startInfo.UseShellExecute);
        Assert.IsTrue(startInfo.CreateNoWindow);
        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "recipe");
        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "run");
        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "tools/recipes/demo-product-evidence-report.json");
    }

    [TestMethod]
    public void Recipe_Executor_Rejects_Non_Allowlisted_Recipe()
    {
        try
        {
            PilotRecipeExecutor.CreateRecipeRunStartInfo(
                @"C:\repo",
                PilotRecipeExecutor.DefaultDotnetPath,
                "tools/recipes/mercadolibre-product-readonly.json");
        }
        catch (InvalidOperationException)
        {
            return;
        }

        Assert.Fail("Expected InvalidOperationException.");
    }
}
