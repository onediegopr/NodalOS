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
        StringAssert.Contains(html, "ONE BRAIN Pilot / local read-only shell");
        StringAssert.Contains(html, "textarea");
        StringAssert.Contains(html, "Comparar productos demo");
        StringAssert.Contains(html, "Generar reporte Markdown");
        StringAssert.Contains(html, "Generar reporte HTML");
        StringAssert.Contains(html, "Ver safety guarantees");
        StringAssert.Contains(html, "Start recording demo/shadow");
        StringAssert.Contains(html, "/recording/demo");
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
        StringAssert.Contains(html, "No autonomous free-agent mode");
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
