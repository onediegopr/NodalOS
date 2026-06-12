using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotIntentRouterTests
{
    [TestMethod]
    public void Route_Mostrame_La_Demo_To_Demo_Markdown()
    {
        var result = new PilotIntentRouter().Route("mostrame la demo");

        Assert.AreEqual(PilotIntentStatus.Matched, result.Status);
        Assert.AreEqual("demo_markdown", result.Recipe?.Id);
        Assert.AreEqual("tools/recipes/demo-product-evidence-report.json", result.Recipe?.RecipePath);
    }

    [TestMethod]
    public void Route_Genera_Html_To_Allowlisted_Html()
    {
        var result = new PilotIntentRouter().Route("genera html");

        Assert.AreEqual(PilotIntentStatus.Matched, result.Status);
        Assert.AreEqual("html_report", result.Recipe?.Id);
        Assert.AreEqual("tools/recipes/product-evidence-html-report.json", result.Recipe?.RecipePath);
    }

    [TestMethod]
    public void Route_Quiero_Reporte_Markdown_To_Allowlisted_Markdown()
    {
        var result = new PilotIntentRouter().Route("quiero reporte markdown");

        Assert.AreEqual(PilotIntentStatus.Matched, result.Status);
        Assert.AreEqual("markdown_report", result.Recipe?.Id);
        Assert.AreEqual("tools/recipes/product-evidence-markdown-report.json", result.Recipe?.RecipePath);
    }

    [TestMethod]
    public void Route_Unknown_Prompt_Is_Rejected_No_Match()
    {
        var result = new PilotIntentRouter().Route("ordena una pizza");

        Assert.AreEqual(PilotIntentStatus.Rejected, result.Status);
        Assert.IsNull(result.Recipe);
        Assert.AreEqual("no_match", result.Reason);
    }

    [TestMethod]
    public void Route_Dangerous_Commercial_Request_Is_Rejected()
    {
        var result = new PilotIntentRouter().Route("comprar el producto y pagar");

        Assert.AreEqual(PilotIntentStatus.Rejected, result.Status);
        Assert.IsNull(result.Recipe);
        Assert.AreEqual("safety_rejected", result.Reason);
    }
}
