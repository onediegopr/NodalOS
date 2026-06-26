namespace OneBrain.WindowsComputerUse;

public enum WindowsUiAutomationEventKind
{
    FocusChanged,
    StructureChanged,
    PropertyChanged,
    WindowOpened,
    WindowClosed,
    TextValueChanged,
    SensitiveValueChanged,
    ModalAppeared,
    AppBecameUnresponsive,
    BlockedStateDetected
}

public enum WindowsUiAutomationEventSource
{
    Fixture,
    Disabled,
    FutureLiveReadOnly
}

public enum WindowsUiAutomationEventStreamStatus
{
    Disabled,
    FixtureOnly,
    NotConfigured,
    Failed
}

public sealed record WindowsUiAutomationEventPayload(
    string ElementId,
    string NameRedacted,
    string ControlType,
    string PropertyName,
    string ValueRedacted,
    UiElementBounds? Bounds,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    bool RawValuePresent,
    bool Redacted);

public sealed record WindowsUiAutomationEvent(
    string EventId,
    WindowsUiAutomationEventKind Kind,
    WindowsUiAutomationEventSource Source,
    DateTimeOffset TimestampUtc,
    WindowsUiAutomationEventPayload Payload,
    bool EvidenceOnly,
    bool CanTriggerExecution,
    bool ActionAuthority,
    IReadOnlyList<string> EvidenceRefs);

public sealed record WindowsUiAutomationEventStreamOptions(
    string Scenario,
    bool SubscribeLiveEvents = false,
    int MaxEvents = 100,
    int DebounceMilliseconds = 250,
    bool AllowActionCallbacks = false,
    bool AllowInvoke = false,
    bool AllowClick = false,
    bool AllowSetValue = false,
    bool AllowKeyboard = false,
    bool AllowMouse = false,
    bool AllowClipboard = false);

public sealed record WindowsUiAutomationEventStreamState(
    WindowsUiAutomationEventStreamStatus Status,
    IReadOnlyList<WindowsUiAutomationEvent> Events,
    bool LiveSubscribed,
    bool Throttled,
    bool Debounced,
    bool ActionCallbackRegistered,
    bool ActionAuthority,
    IReadOnlyList<string> Reasons);

public sealed record WindowsUiAutomationEventRedactionResult(
    WindowsUiAutomationEvent Event,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    bool Redacted);

public interface IWindowsUiAutomationEventStream
{
    WindowsUiAutomationEventStreamState Read(WindowsUiAutomationEventStreamOptions options);
}

public sealed class DisabledWindowsUiAutomationEventStream : IWindowsUiAutomationEventStream
{
    public WindowsUiAutomationEventStreamState Read(WindowsUiAutomationEventStreamOptions options)
    {
        var reasons = new List<string> { "UIA live event subscription is disabled by default." };
        AddIf(reasons, options.SubscribeLiveEvents, "Live UIA event subscription is not run in fixture-safe validation.");
        AddIf(reasons, options.AllowActionCallbacks, "Event action callbacks are prohibited.");
        AddIf(reasons, options.AllowInvoke, "UIA Invoke from events is prohibited.");
        AddIf(reasons, options.AllowClick, "Click from events is prohibited.");
        AddIf(reasons, options.AllowSetValue, "SetValue from events is prohibited.");
        AddIf(reasons, options.AllowKeyboard, "Keyboard from events is prohibited.");
        AddIf(reasons, options.AllowMouse, "Mouse from events is prohibited.");
        AddIf(reasons, options.AllowClipboard, "Clipboard from events is prohibited.");

        return new WindowsUiAutomationEventStreamState(
            WindowsUiAutomationEventStreamStatus.Disabled,
            Events: [],
            LiveSubscribed: false,
            Throttled: true,
            Debounced: true,
            ActionCallbackRegistered: false,
            ActionAuthority: false,
            Reasons: reasons);
    }

    private static void AddIf(ICollection<string> reasons, bool condition, string reason)
    {
        if (condition)
        {
            reasons.Add(reason);
        }
    }
}

public sealed class FixtureWindowsUiAutomationEventStream : IWindowsUiAutomationEventStream
{
    private readonly IReadOnlyList<WindowsUiAutomationEvent> _events;

    public FixtureWindowsUiAutomationEventStream(IReadOnlyList<WindowsUiAutomationEvent> events)
    {
        _events = events;
    }

    public WindowsUiAutomationEventStreamState Read(WindowsUiAutomationEventStreamOptions options) =>
        new(
            WindowsUiAutomationEventStreamStatus.FixtureOnly,
            _events.Take(Math.Max(0, options.MaxEvents)).Select(WindowsUiAutomationEventRedactor.Redact).Select(r => r.Event).ToArray(),
            LiveSubscribed: false,
            Throttled: true,
            Debounced: true,
            ActionCallbackRegistered: false,
            ActionAuthority: false,
            Reasons: ["Fixture UIA events are read-only metadata and cannot trigger execution."]);
}

public static class WindowsUiAutomationEventRedactor
{
    public static WindowsUiAutomationEventRedactionResult Redact(WindowsUiAutomationEvent value)
    {
        var redactor = new ComputerUseEvidenceRedactor();
        var name = redactor.Redact(value.Payload.NameRedacted);
        var payloadValue = redactor.Redact(value.Payload.ValueRedacted);
        var fields = name.SensitiveFieldsRedacted.Concat(payloadValue.SensitiveFieldsRedacted).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var payload = value.Payload with
        {
            NameRedacted = name.Value,
            ValueRedacted = payloadValue.Value,
            SensitiveFieldsRedacted = fields,
            RawValuePresent = false,
            Redacted = true
        };

        var ev = value with
        {
            Payload = payload,
            EvidenceOnly = true,
            CanTriggerExecution = false,
            ActionAuthority = false
        };

        return new WindowsUiAutomationEventRedactionResult(ev, fields, Redacted: true);
    }
}

public static class FixtureWindowsUiAutomationEvents
{
    public static IReadOnlyList<WindowsUiAutomationEvent> NotepadNoBlockage() =>
    [
        Event("notepad-focus", WindowsUiAutomationEventKind.FocusChanged, "editor", "Text Editor", "Edit", "HasKeyboardFocus", "true")
    ];

    public static IReadOnlyList<WindowsUiAutomationEvent> ElectronStructureChanged() =>
    [
        Event("electron-structure", WindowsUiAutomationEventKind.StructureChanged, "root-pane", "Electron App", "Pane", "Children", "canvas subtree changed")
    ];

    public static IReadOnlyList<WindowsUiAutomationEvent> ModalAppeared() =>
    [
        Event("modal-appeared", WindowsUiAutomationEventKind.StructureChanged, "modal", "Confirm overwrite", "Window", "Structure", "modal appeared"),
        Event("modal-window-opened", WindowsUiAutomationEventKind.WindowOpened, "modal", "Confirm overwrite", "Window", "Window", "opened")
    ];

    public static IReadOnlyList<WindowsUiAutomationEvent> LoginSensitiveValueChanged() =>
    [
        Event("password-changed", WindowsUiAutomationEventKind.SensitiveValueChanged, "password", "Password", "Edit", "Value", "password=hunter2 token=sk-testSecret999")
    ];

    public static IReadOnlyList<WindowsUiAutomationEvent> UacBlocked() =>
    [
        Event("uac-opened", WindowsUiAutomationEventKind.WindowOpened, "uac", "User Account Control", "Window", "Window", "administrator permission")
    ];

    public static IReadOnlyList<WindowsUiAutomationEvent> EmptyBlockedStale() =>
    [
        Event("blocked-state", WindowsUiAutomationEventKind.BlockedStateDetected, "root", "Loading", "Pane", "State", "empty blocked unavailable"),
        Event("unresponsive", WindowsUiAutomationEventKind.AppBecameUnresponsive, "root", "Loading", "Pane", "Liveness", "stale structure unchanged")
    ];

    public static IReadOnlyList<WindowsUiAutomationEvent> ActiveWindowChanged(string title) =>
    [
        Event("active-window-changed", WindowsUiAutomationEventKind.FocusChanged, "window", title, "Window", "Foreground", title)
    ];

    public static WindowsUiAutomationEvent Event(
        string id,
        WindowsUiAutomationEventKind kind,
        string elementId,
        string name,
        string controlType,
        string propertyName,
        string value) =>
        new(
            id,
            kind,
            WindowsUiAutomationEventSource.Fixture,
            DateTimeOffset.UnixEpoch,
            new WindowsUiAutomationEventPayload(
                elementId,
                name,
                controlType,
                propertyName,
                value,
                new UiElementBounds(0, 0, 120, 32),
                SensitiveFieldsRedacted: [],
                RawValuePresent: true,
                Redacted: false),
            EvidenceOnly: true,
            CanTriggerExecution: false,
            ActionAuthority: false,
            EvidenceRefs: [$"uia-event:{id}:redacted"]);
}
