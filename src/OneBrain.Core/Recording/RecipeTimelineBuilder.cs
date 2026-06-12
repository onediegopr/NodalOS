namespace OneBrain.Core.Recording;

public static class RecipeTimelineBuilder
{
    public static RecipeTimeline Build(RecordingSession session, IReadOnlyList<HumanAnnotation>? annotations = null, DateTimeOffset? createdAtUtc = null)
    {
        var steps = session.Events
            .Select((recordingEvent, index) => BuildStep(recordingEvent, index + 1))
            .ToList();

        return new RecipeTimeline(
            TimelineId: $"timeline-{session.RecordingId}",
            RecordingId: session.RecordingId,
            CreatedAtUtc: (createdAtUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o"),
            Steps: steps,
            Annotations: annotations?.ToList() ?? [],
            Notes:
            [
                "candidate timeline only; no executable recipe generated",
                "sensitive or low-confidence steps require human approval"
            ]);
    }

    private static TimelineStep BuildStep(RecordingEvent recordingEvent, int stepNumber)
    {
        var risk = DetermineRisk(recordingEvent);
        var requiresApproval = risk is "medium" or "high" ||
                               recordingEvent.EventType == RecordingEventTypes.ManualActionObserved ||
                               recordingEvent.SensitiveTextRedacted;

        return new TimelineStep(
            StepNumber: stepNumber,
            OffsetMs: recordingEvent.OffsetMs,
            EventType: recordingEvent.EventType,
            WindowOrApp: FirstNonEmpty(recordingEvent.WindowTitle, recordingEvent.ProcessName, "unknown"),
            ElementSummary: recordingEvent.ElementSummary,
            Confidence: recordingEvent.Confidence,
            SuggestedActionLabel: BuildSuggestedActionLabel(recordingEvent, risk),
            RiskLevel: risk,
            RequiresApproval: requiresApproval,
            CanGenerateExecutableRecipe: false);
    }

    private static string DetermineRisk(RecordingEvent recordingEvent)
    {
        var combined = $"{recordingEvent.EventType} {recordingEvent.ElementSummary} {recordingEvent.ElementName}".ToLowerInvariant();
        if (recordingEvent.SensitiveTextRedacted)
            return "high";
        if (ContainsAny(combined, "send", "enviar", "submit", "pagar", "payment", "checkout", "comprar", "carrito", "whatsapp"))
            return "high";
        if (recordingEvent.EventType is RecordingEventTypes.ButtonDetected or RecordingEventTypes.ManualActionObserved)
            return "medium";
        return "low";
    }

    private static string BuildSuggestedActionLabel(RecordingEvent recordingEvent, string risk)
    {
        return recordingEvent.EventType switch
        {
            RecordingEventTypes.WindowChanged => $"User opened/focused window: {FirstNonEmpty(recordingEvent.WindowTitle, "unknown")}",
            RecordingEventTypes.FocusChanged => $"User focused: {recordingEvent.ElementSummary}",
            RecordingEventTypes.TextFieldDetected => $"Text input candidate detected: {recordingEvent.ElementSummary}",
            RecordingEventTypes.ButtonDetected when risk == "high" => $"Sensitive button candidate: {recordingEvent.ElementSummary}",
            RecordingEventTypes.ButtonDetected => $"Button candidate detected: {recordingEvent.ElementSummary}",
            RecordingEventTypes.ManualActionObserved => $"Manual action observed: {recordingEvent.ElementSummary}",
            _ => $"Observation: {recordingEvent.ElementSummary}"
        };
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}
