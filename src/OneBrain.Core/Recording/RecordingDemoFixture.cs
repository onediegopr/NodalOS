using OneBrain.Core.Models;

namespace OneBrain.Core.Recording;

public static class RecordingDemoFixture
{
    public static RecordingSession CreateSession(DateTimeOffset? startedAtUtc = null)
    {
        var started = startedAtUtc ?? new DateTimeOffset(2026, 06, 12, 10, 0, 0, TimeSpan.Zero);
        var builder = new RecordingSessionBuilder("demo-shadow-recording", started);

        builder.Observe(new RecordingObservationInput(started, Snapshot("WhatsApp", "WhatsApp", "Search", "Edit", "Buscar cliente")));
        builder.Observe(new RecordingObservationInput(started.AddSeconds(4), Snapshot("WhatsApp", "WhatsApp", "Message", "Edit", "Caja de mensaje")));
        builder.Observe(new RecordingObservationInput(started.AddSeconds(9), Snapshot("WhatsApp", "WhatsApp", "Send", "Button", "Enviar"), RecordingEventTypes.ManualActionObserved, "User prepared message; send requires approval"));

        return builder.Build(started.AddSeconds(10));
    }

    public static RecipeTimeline CreateTimeline()
    {
        var annotations = new[]
        {
            HumanAnnotationBuilder.Create(2, "buscar cliente", "este bloque es buscar cliente", createdAtUtc: new DateTimeOffset(2026, 06, 12, 10, 0, 11, TimeSpan.Zero)),
            HumanAnnotationBuilder.Create(4, "preparar mensaje", "este bloque es preparar mensaje", createdAtUtc: new DateTimeOffset(2026, 06, 12, 10, 0, 12, TimeSpan.Zero)),
            HumanAnnotationBuilder.Create(6, "requiere aprobacion", "este paso requiere aprobacion humana", requiresApproval: true, sensitive: true, createdAtUtc: new DateTimeOffset(2026, 06, 12, 10, 0, 13, TimeSpan.Zero))
        };

        return RecipeTimelineBuilder.Build(CreateSession(), annotations, new DateTimeOffset(2026, 06, 12, 10, 0, 14, TimeSpan.Zero));
    }

    private static CognitiveSnapshot Snapshot(string title, string process, string automationId, string role, string name)
    {
        return new CognitiveSnapshot(
            new WindowSnapshot(title, process, 100, new WindowBounds(0, 0, 900, 700), true),
            [
                new UiElementSnapshot(
                    Ref: "@e1",
                    Role: role,
                    Name: name,
                    AutomationId: automationId,
                    ClassName: "Demo",
                    Bounds: new WindowBounds(10, 10, 200, 40),
                    IsEnabled: true,
                    IsOffscreen: false,
                    IsKeyboardFocusable: true,
                    Patterns: role == "Button" ? ["Invoke"] : ["Value"],
                    Actions: role == "Button" ? ["invoke"] : [])
            ],
            TreeTruncated: false);
    }
}
