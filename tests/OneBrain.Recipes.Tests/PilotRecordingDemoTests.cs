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

        StringAssert.Contains(html, "Recording timeline demo");
        StringAssert.Contains(html, "shadow mode");
        StringAssert.Contains(html, "Timeline");
        StringAssert.Contains(html, "Human annotations");
        StringAssert.Contains(html, "este bloque es buscar cliente");
        StringAssert.Contains(html, "este paso requiere aprobacion");
    }

    [TestMethod]
    public void Recording_Demo_Render_States_No_Playback_And_No_Clicks()
    {
        var html = PilotHomePageRenderer.RenderRecordingDemo(RecordingDemoFixture.CreateTimeline());

        StringAssert.Contains(html, "no playback");
        StringAssert.Contains(html, "no clicks");
        StringAssert.Contains(html, "does not capture secrets, execute actions, replay actions, or generate executable recipes");
    }

    [TestMethod]
    public void Recording_Demo_Timeline_Shows_Approval_For_Sensitive_Step()
    {
        var html = PilotHomePageRenderer.RenderRecordingDemo(RecordingDemoFixture.CreateTimeline());

        StringAssert.Contains(html, "requires_approval");
        StringAssert.Contains(html, "<td>true</td>");
    }
}
