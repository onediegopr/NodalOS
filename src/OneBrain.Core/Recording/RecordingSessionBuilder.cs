using OneBrain.Core.Models;

namespace OneBrain.Core.Recording;

public sealed class RecordingSessionBuilder
{
    private readonly string _recordingId;
    private readonly DateTimeOffset _startedAtUtc;
    private readonly List<RecordingEvent> _events = [];
    private readonly List<string> _notes = [];
    private string _lastWindowKey = "";

    public RecordingSessionBuilder(string? recordingId = null, DateTimeOffset? startedAtUtc = null)
    {
        _recordingId = string.IsNullOrWhiteSpace(recordingId) ? Guid.NewGuid().ToString("N") : recordingId.Trim();
        _startedAtUtc = startedAtUtc ?? DateTimeOffset.UtcNow;
        _notes.Add("shadow recording: observe only, no clicks, no typing, no submit, no playback");
    }

    public RecordingSessionBuilder Observe(RecordingObservationInput input)
    {
        var snapshot = input.Snapshot;
        if (snapshot == null)
        {
            AddEvent(input.TimestampUtc, RecordingEventTypes.UnknownObservation, "", "", "", "", "", "No snapshot available", 0.2, false, []);
            return this;
        }

        var windowTitle = SensitiveTextSanitizer.Sanitize(snapshot.Window.Title, out var titleRedacted);
        var processName = SensitiveTextSanitizer.Sanitize(snapshot.Window.ProcessName, out var processRedacted);
        var windowKey = $"{processName}|{windowTitle}";

        if (!windowKey.Equals(_lastWindowKey, StringComparison.OrdinalIgnoreCase))
        {
            AddEvent(input.TimestampUtc, RecordingEventTypes.WindowChanged, windowTitle, processName, "Window", windowTitle, "", $"Window changed: {windowTitle}", 0.85, titleRedacted || processRedacted, []);
            _lastWindowKey = windowKey;
        }

        var focused = snapshot.Elements.FirstOrDefault(element => element.IsKeyboardFocusable && !element.IsOffscreen);
        if (focused != null)
            AddElementEvent(input.TimestampUtc, snapshot.Window, focused, RecordingEventTypes.FocusChanged, 0.72);

        foreach (var element in snapshot.Elements.Where(IsTextField).Take(3))
            AddElementEvent(input.TimestampUtc, snapshot.Window, element, RecordingEventTypes.TextFieldDetected, 0.7);

        foreach (var element in snapshot.Elements.Where(IsButton).Take(5))
            AddElementEvent(input.TimestampUtc, snapshot.Window, element, RecordingEventTypes.ButtonDetected, 0.68);

        if (!string.IsNullOrWhiteSpace(input.ManualSummary))
        {
            var summary = SensitiveTextSanitizer.Sanitize(input.ManualSummary, out var redacted);
            AddEvent(input.TimestampUtc, input.EventType ?? RecordingEventTypes.ManualActionObserved, windowTitle, processName, "", "", "", summary, redacted ? 0.4 : 0.62, redacted, []);
        }

        return this;
    }

    public RecordingSession Build(DateTimeOffset? endedAtUtc = null)
    {
        var ended = endedAtUtc ?? (_events.Count > 0 ? DateTimeOffset.Parse(_events[^1].TimestampUtc) : _startedAtUtc);
        return new RecordingSession(
            RecordingId: _recordingId,
            StartedAtUtc: _startedAtUtc.UtcDateTime.ToString("o"),
            EndedAtUtc: ended.UtcDateTime.ToString("o"),
            Events: _events.ToList(),
            Notes: _notes.ToList());
    }

    private void AddElementEvent(DateTimeOffset timestamp, WindowSnapshot window, UiElementSnapshot element, string eventType, double confidence)
    {
        var windowTitle = SensitiveTextSanitizer.Sanitize(window.Title, out var titleRedacted);
        var processName = SensitiveTextSanitizer.Sanitize(window.ProcessName, out var processRedacted);
        var elementName = SensitiveTextSanitizer.Sanitize(element.Name, out var nameRedacted);
        var automationId = SensitiveTextSanitizer.Sanitize(element.AutomationId, out var idRedacted);
        var summary = BuildElementSummary(element.Role, elementName, automationId);

        AddEvent(
            timestamp,
            eventType,
            windowTitle,
            processName,
            element.Role,
            elementName,
            automationId,
            summary,
            confidence,
            titleRedacted || processRedacted || nameRedacted || idRedacted,
            element.Actions);
    }

    private void AddEvent(
        DateTimeOffset timestamp,
        string eventType,
        string windowTitle,
        string processName,
        string elementRole,
        string elementName,
        string automationId,
        string summary,
        double confidence,
        bool redacted,
        IReadOnlyList<string> actions)
    {
        _events.Add(new RecordingEvent(
            Id: $"ev{_events.Count + 1:000}",
            TimestampUtc: timestamp.UtcDateTime.ToString("o"),
            OffsetMs: Math.Max(0, (int)(timestamp - _startedAtUtc).TotalMilliseconds),
            EventType: eventType,
            WindowTitle: windowTitle,
            ProcessName: processName,
            ElementRole: elementRole,
            ElementName: elementName,
            ElementAutomationId: automationId,
            ElementSummary: summary,
            Confidence: confidence,
            SensitiveTextRedacted: redacted,
            ObservedActions: actions.ToList()));
    }

    private static string BuildElementSummary(string role, string name, string automationId)
    {
        var label = string.IsNullOrWhiteSpace(name) ? automationId : name;
        if (string.IsNullOrWhiteSpace(label))
            label = "unnamed element";
        return $"{role}: {label}";
    }

    private static bool IsTextField(UiElementSnapshot element)
    {
        return element.Role.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
               element.Role.Contains("Document", StringComparison.OrdinalIgnoreCase) ||
               element.Patterns.Any(pattern => pattern.Contains("Value", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsButton(UiElementSnapshot element)
    {
        return element.Role.Contains("Button", StringComparison.OrdinalIgnoreCase) ||
               element.Patterns.Any(pattern => pattern.Contains("Invoke", StringComparison.OrdinalIgnoreCase));
    }
}
