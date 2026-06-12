using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Models;
using OneBrain.Core.Recording;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecordingSessionBuilderTests
{
    [TestMethod]
    public void Build_Records_Window_Text_Field_And_Button_Observations()
    {
        var start = new DateTimeOffset(2026, 06, 12, 10, 0, 0, TimeSpan.Zero);
        var session = new RecordingSessionBuilder("rec-1", start)
            .Observe(new RecordingObservationInput(start, Snapshot("WhatsApp", "WhatsApp", "Search", "Edit", "Buscar cliente")))
            .Observe(new RecordingObservationInput(start.AddSeconds(2), Snapshot("WhatsApp", "WhatsApp", "Send", "Button", "Enviar")))
            .Build(start.AddSeconds(3));

        Assert.AreEqual("rec-1", session.RecordingId);
        Assert.IsTrue(session.Events.Any(e => e.EventType == RecordingEventTypes.WindowChanged));
        Assert.IsTrue(session.Events.Any(e => e.EventType == RecordingEventTypes.TextFieldDetected));
        Assert.IsTrue(session.Events.Any(e => e.EventType == RecordingEventTypes.ButtonDetected));
        Assert.IsTrue(session.Events.All(e => e.OffsetMs >= 0));
    }

    [TestMethod]
    public void Build_Sanitizes_Sensitive_Text()
    {
        var start = new DateTimeOffset(2026, 06, 12, 10, 0, 0, TimeSpan.Zero);
        var session = new RecordingSessionBuilder("rec-sensitive", start)
            .Observe(new RecordingObservationInput(start, Snapshot("Password window", "demo", "Password", "Edit", "password 1234567890123456")))
            .Build(start.AddSeconds(1));

        Assert.IsTrue(session.Events.Any(e => e.SensitiveTextRedacted));
        Assert.IsTrue(session.Events.Any(e => e.ElementName == "[REDACTED]" || e.WindowTitle == "[REDACTED]"));
    }

    [TestMethod]
    public void Unknown_Null_Snapshot_Produces_Controlled_Observation()
    {
        var start = new DateTimeOffset(2026, 06, 12, 10, 0, 0, TimeSpan.Zero);
        var session = new RecordingSessionBuilder("rec-null", start)
            .Observe(new RecordingObservationInput(start, null))
            .Build(start);

        Assert.AreEqual(1, session.Events.Count);
        Assert.AreEqual(RecordingEventTypes.UnknownObservation, session.Events[0].EventType);
    }

    [TestMethod]
    public void Demo_Fixture_Contains_Annotations_And_No_Executable_Recipe()
    {
        var timeline = RecordingDemoFixture.CreateTimeline();

        Assert.IsTrue(timeline.Steps.Count > 0);
        Assert.IsTrue(timeline.Annotations.Count >= 3);
        Assert.IsTrue(timeline.Steps.All(step => !step.CanGenerateExecutableRecipe));
    }

    private static CognitiveSnapshot Snapshot(string title, string process, string automationId, string role, string name)
    {
        return new CognitiveSnapshot(
            new WindowSnapshot(title, process, 123, new WindowBounds(0, 0, 800, 600), true),
            [
                new UiElementSnapshot(
                    Ref: "@e1",
                    Role: role,
                    Name: name,
                    AutomationId: automationId,
                    ClassName: "Demo",
                    Bounds: new WindowBounds(1, 1, 100, 30),
                    IsEnabled: true,
                    IsOffscreen: false,
                    IsKeyboardFocusable: true,
                    Patterns: role == "Button" ? ["Invoke"] : ["Value"],
                    Actions: role == "Button" ? ["invoke"] : [])
            ],
            false);
    }
}
