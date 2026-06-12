using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AI;
using OneBrain.Core.AppProfiles;
using OneBrain.Core.Flows;
using OneBrain.Core.History;
using OneBrain.Core.Memory;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotDemoRealSeparationTests
{
    [TestMethod]
    public void Demo_Fixture_Pages_Show_Demo_Mode_Banner()
    {
        var flow = BusinessFlowPlaybackFixture.CreatePromotedFlow();
        var session = BusinessFlowPlaybackFixture.CreatePlaybackSession();
        var pages = new[]
        {
            PilotHomePageRenderer.RenderPromotedFlows([flow], PilotDataOrigins.DemoFixture),
            PilotHomePageRenderer.RenderPromotedFlowDetail(flow, dataOrigin: PilotDataOrigins.DemoFixture),
            PilotHomePageRenderer.RenderSupervisedPlayback(flow, session, dataOrigin: PilotDataOrigins.DemoFixture),
            PilotHomePageRenderer.RenderRunHistory(HistoryDemoFixture.CreateRunHistory(), PilotDataOrigins.DemoFixture),
            PilotHomePageRenderer.RenderAIAuditLog(HistoryDemoFixture.CreateAIAudit(), PilotDataOrigins.DemoFixture),
            PilotHomePageRenderer.RenderProcessMemory(ProcessMemoryDemoFixture.CreateEntries(), new WorkflowRetrievalResult(new WorkflowRetrievalQuery(), []), PilotDataOrigins.DemoFixture),
            PilotHomePageRenderer.RenderAppProfiles(AppProfileDemoFixture.CreateProfiles(), PilotDataOrigins.DemoFixture)
        };

        foreach (var html in pages)
        {
            StringAssert.Contains(html, "MODO DEMO / SIMULACION SEGURA");
            StringAssert.Contains(html, "Estos datos no vienen de una ejecucion real del usuario.");
            StringAssert.Contains(html, "No hay datos reales todavia. Mostrando ejemplo de demostracion.");
            StringAssert.Contains(html, "Origen: Demo / Simulacion segura");
        }
    }

    [TestMethod]
    public void Runtime_Pages_Show_Runtime_Origin()
    {
        var run = HistoryDemoFixture.CreateRunHistory()[0];
        var html = PilotHomePageRenderer.RenderRunHistory([run], PilotDataOrigins.Runtime);

        StringAssert.Contains(html, "Dato real");
        StringAssert.Contains(html, "Origen: Datos reales/runtime local");
        StringAssert.Contains(html, "Datos locales generados por esta instalacion");
        Assert.IsFalse(html.Contains("MODO DEMO / SIMULACION SEGURA", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Home_Distinguishes_Safe_Demo_From_Real_Data()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "Separacion demo/real");
        StringAssert.Contains(html, "Probar demo HTML segura");
        StringAssert.Contains(html, "datos reales");
        StringAssert.Contains(html, "MODO DEMO / SIMULACION SEGURA");
    }

    [TestMethod]
    public void Demo_Flow_And_Playback_Do_Not_Promise_Real_Execution()
    {
        var flow = BusinessFlowPlaybackFixture.CreatePromotedFlow();
        var flowHtml = PilotHomePageRenderer.RenderPromotedFlowDetail(flow, dataOrigin: PilotDataOrigins.DemoFixture);
        var playbackHtml = PilotHomePageRenderer.RenderSupervisedPlayback(flow, BusinessFlowPlaybackFixture.CreatePlaybackSession(), dataOrigin: PilotDataOrigins.DemoFixture);

        StringAssert.Contains(flowHtml, "La ejecucion real esta desactivada en esta version.");
        StringAssert.Contains(flowHtml, "ONE BRAIN registra la decision, pero no actua sobre otras apps.");
        StringAssert.Contains(flowHtml, "Iniciar simulacion supervisada");
        StringAssert.Contains(playbackHtml, "Este flujo no ejecuta acciones reales.");
        StringAssert.Contains(playbackHtml, "Este flujo no hace clicks.");
        StringAssert.Contains(playbackHtml, "Este flujo no envia mensajes.");
        StringAssert.Contains(playbackHtml, "Este flujo no compra, no paga y no inicia sesion.");
        StringAssert.Contains(playbackHtml, "Confirmar paso de demostracion");
        Assert.IsFalse(flowHtml.Contains(">Ejecutar flujo<", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(playbackHtml.Contains(">Ejecutar flujo<", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Approval_Demo_Explains_ExecutionAllowed_False_In_User_Language()
    {
        var html = PilotHomePageRenderer.RenderApprovalDemo(
            OneBrain.Core.Approval.BusinessFlowDemoFixture.CreateSendMessageApproval(),
            OneBrain.Core.Approval.BusinessFlowDemoFixture.CreateConfidenceProfile());

        StringAssert.Contains(html, "MODO DEMO / SIMULACION SEGURA");
        StringAssert.Contains(html, "ExecutionAllowed=false significa");
        StringAssert.Contains(html, "la ejecucion real esta desactivada en esta version");
        StringAssert.Contains(html, "ONE BRAIN registra la decision, pero no actua sobre otras apps.");
    }
}
