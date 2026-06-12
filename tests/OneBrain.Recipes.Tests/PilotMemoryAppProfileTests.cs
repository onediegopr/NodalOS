using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AppProfiles;
using OneBrain.Core.Memory;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotMemoryAppProfileTests
{
    [TestMethod]
    public void Home_Render_Includes_Memory_And_App_Profile_Links()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "/memory");
        StringAssert.Contains(html, "/app-profiles");
        StringAssert.Contains(html, "Procesos aprendidos");
        StringAssert.Contains(html, "Apps y sitios");
    }

    [TestMethod]
    public void Process_Memory_Render_Shows_Retrieval_Safety_And_No_OpenAI()
    {
        var entries = ProcessMemoryDemoFixture.CreateEntries();
        var retrieval = WorkflowRetrievalService.Search(entries, new WorkflowRetrievalQuery(Text: "report", Tags: ["product-evidence"]));
        var html = PilotHomePageRenderer.RenderProcessMemory(entries, retrieval);

        StringAssert.Contains(html, "Procesos aprendidos");
        StringAssert.Contains(html, "solo busqueda");
        StringAssert.Contains(html, "sin llamada a OpenAI");
        StringAssert.Contains(html, "process-demo-product-evidence-report");
        StringAssert.Contains(html, "seguro para sugerir");
        StringAssert.Contains(html, "title match");
    }

    [TestMethod]
    public void Process_Memory_Detail_Renders_Links_And_Artifacts()
    {
        var html = PilotHomePageRenderer.RenderProcessMemoryDetail(ProcessMemoryDemoFixture.CreateWhatsAppCandidateEntry());

        StringAssert.Contains(html, "candidate-whatsapp-browser-demo-v0");
        StringAssert.Contains(html, "approval-send-message-demo");
        StringAssert.Contains(html, "artifacts/approvals");
        StringAssert.Contains(html, "sin ejecucion");
    }

    [TestMethod]
    public void App_Profile_Render_Shows_Diagnostic_And_Blocked_Policies()
    {
        var html = PilotHomePageRenderer.RenderAppProfiles(AppProfileDemoFixture.CreateProfiles());

        StringAssert.Contains(html, "Apps y sitios");
        StringAssert.Contains(html, "mercadolibre-readonly-diagnostic");
        StringAssert.Contains(html, "diagnostic_allowed");
        StringAssert.Contains(html, "login bloqueado=si");
        StringAssert.Contains(html, "pago bloqueado=si");
        StringAssert.Contains(html, "compra bloqueada=si");
    }

    [TestMethod]
    public void App_Profile_Detail_Renders_Policy_Validation()
    {
        var html = PilotHomePageRenderer.RenderAppProfileDetail(AppProfileDemoFixture.CreateMercadoLibreProfile());

        StringAssert.Contains(html, "Mercado Libre readonly diagnostic");
        StringAssert.Contains(html, "Diagnostico permitido: si");
        StringAssert.Contains(html, "Bloquea login: si");
        StringAssert.Contains(html, "Puede activarse: <strong>si</strong>");
    }
}
