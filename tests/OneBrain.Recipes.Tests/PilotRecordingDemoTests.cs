using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recording;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotRecordingDemoTests
{
    [TestMethod]
    public void Recording_Demo_Render_Includes_Shadow_Mode_Timeline_And_Annotations()
    {
        var html = PilotHomePageRenderer.RenderRecordingDemo(RecordingDemoFixture.CreateTimeline());

        StringAssert.Contains(html, "Observacion supervisada");
        StringAssert.Contains(html, "modo observacion");
        StringAssert.Contains(html, "Linea de tiempo");
        StringAssert.Contains(html, "Anotaciones humanas");
        StringAssert.Contains(html, "este bloque es buscar cliente");
        StringAssert.Contains(html, "este paso requiere aprobacion");
    }

    [TestMethod]
    public void Recording_Demo_Render_States_No_Playback_And_No_Clicks()
    {
        var html = PilotHomePageRenderer.RenderRecordingDemo(RecordingDemoFixture.CreateTimeline());

        StringAssert.Contains(html, "sin reproduccion de acciones");
        StringAssert.Contains(html, "no clicks");
        StringAssert.Contains(html, "sin ejecutar acciones reales");
        StringAssert.Contains(html, "no dispara ejecucion");
    }

    [TestMethod]
    public void Recording_Demo_Timeline_Shows_Approval_For_Sensitive_Step()
    {
        var html = PilotHomePageRenderer.RenderRecordingDemo(RecordingDemoFixture.CreateTimeline());

        StringAssert.Contains(html, "requires_approval");
        StringAssert.Contains(html, "requiere aprobacion");
    }
}
