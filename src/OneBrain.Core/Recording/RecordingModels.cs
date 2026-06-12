using OneBrain.Core.Models;

namespace OneBrain.Core.Recording;

public static class RecordingEventTypes
{
    public const string WindowChanged = "window_changed";
    public const string FocusChanged = "focus_changed";
    public const string TextFieldDetected = "text_field_detected";
    public const string ButtonDetected = "button_detected";
    public const string ManualActionObserved = "manual_action_observed";
    public const string UnknownObservation = "unknown_observation";
}

public sealed record RecordingSession(
    string RecordingId,
    string StartedAtUtc,
    string? EndedAtUtc,
    IReadOnlyList<RecordingEvent> Events,
    IReadOnlyList<string> Notes);

public sealed record RecordingEvent(
    string Id,
    string TimestampUtc,
    int OffsetMs,
    string EventType,
    string WindowTitle,
    string ProcessName,
    string ElementRole,
    string ElementName,
    string ElementAutomationId,
    string ElementSummary,
    double Confidence,
    bool SensitiveTextRedacted,
    IReadOnlyList<string> ObservedActions);

public sealed record RecordingObservationInput(
    DateTimeOffset TimestampUtc,
    CognitiveSnapshot? Snapshot,
    string? EventType = null,
    string? ManualSummary = null);

public sealed record RecipeTimeline(
    string TimelineId,
    string RecordingId,
    string CreatedAtUtc,
    IReadOnlyList<TimelineStep> Steps,
    IReadOnlyList<HumanAnnotation> Annotations,
    IReadOnlyList<string> Notes);

public sealed record TimelineStep(
    int StepNumber,
    int OffsetMs,
    string EventType,
    string WindowOrApp,
    string ElementSummary,
    double Confidence,
    string SuggestedActionLabel,
    string RiskLevel,
    bool RequiresApproval,
    bool CanGenerateExecutableRecipe);

public sealed record HumanAnnotation(
    string AnnotationId,
    int? StepNumber,
    string AnnotationType,
    string Text,
    bool RequiresApproval,
    bool Sensitive,
    bool Ignored,
    string CreatedAtUtc);

public sealed record RecordingArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
