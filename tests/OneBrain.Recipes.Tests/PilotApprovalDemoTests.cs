using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotApprovalDemoTests
{
    [TestMethod]
    public void Home_Render_Includes_Approval_Demo_Link()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "Simular aprobacion humana");
        StringAssert.Contains(html, "/approvals/demo");
        StringAssert.Contains(html, "Aprobaciones y confianza");
    }

    [TestMethod]
    public void Approval_Demo_Render_Shows_Request_Confidence_And_Safety()
    {
        var html = PilotHomePageRenderer.RenderApprovalDemo(
            BusinessFlowDemoFixture.CreateSendMessageApproval(),
            BusinessFlowDemoFixture.CreateConfidenceProfile());

        StringAssert.Contains(html, "Aprobaciones humanas");
        StringAssert.Contains(html, "send");
        StringAssert.Contains(html, "blocked");
        StringAssert.Contains(html, "Puntaje de confianza");
        StringAssert.Contains(html, "La ejecucion real esta desactivada en esta version");
        StringAssert.Contains(html, "0 clicks");
        StringAssert.Contains(html, "0 cookies aceptadas");
        StringAssert.Contains(html, "0 login");
        StringAssert.Contains(html, "0 compra");
        StringAssert.Contains(html, "0 pago");
    }

    [TestMethod]
    public void Approval_Demo_Render_With_Decision_Still_Shows_No_Execution()
    {
        var request = BusinessFlowDemoFixture.CreateSendMessageApproval();
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "reviewed");
        var html = PilotHomePageRenderer.RenderApprovalDemo(request, BusinessFlowDemoFixture.CreateConfidenceProfile(), decision);

        StringAssert.Contains(html, "approved");
        StringAssert.Contains(html, "reviewed");
        StringAssert.Contains(html, "La ejecucion real esta desactivada en esta version");
        Assert.IsFalse(html.Contains("safe.click", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("browser.open", StringComparison.OrdinalIgnoreCase));
    }
}
