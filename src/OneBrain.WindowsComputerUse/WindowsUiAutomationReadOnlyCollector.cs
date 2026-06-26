namespace OneBrain.WindowsComputerUse;

public enum WindowsUiAutomationReadOnlyStatus
{
    SkippedDisabled,
    FixtureOnly,
    NotRun,
    Failed
}

public sealed record WindowsUiAutomationReadOnlySnapshotOptions(
    string TargetWindowHint,
    bool IncludeTextPatternMetadata,
    bool IncludeValuePatternMetadata,
    bool IncludeBoundingBoxes,
    bool AllowScreenshots = false,
    bool AllowInvoke = false,
    bool AllowClick = false,
    bool AllowSetValue = false,
    bool AllowKeyboard = false,
    bool AllowMouse = false,
    bool AllowClipboard = false);

public sealed record WindowsUiAutomationReadOnlyResult(
    WindowsUiAutomationReadOnlyStatus Status,
    ComputerUseSnapshot? Snapshot,
    bool InvokeUsed,
    bool ClickUsed,
    bool SetValueUsed,
    bool KeyboardUsed,
    bool MouseUsed,
    bool ClipboardUsed,
    bool ScreenshotCaptured,
    IReadOnlyList<string> Reasons);

public interface IWindowsUiAutomationReadOnlyCollector
{
    WindowsUiAutomationReadOnlyResult Collect(WindowsUiAutomationReadOnlySnapshotOptions options);
}

public sealed class WindowsUiAutomationReadOnlyCollectorDisabled : IWindowsUiAutomationReadOnlyCollector
{
    public WindowsUiAutomationReadOnlyResult Collect(WindowsUiAutomationReadOnlySnapshotOptions options)
    {
        var blocked = new List<string>();
        AddIf(blocked, options.AllowScreenshots, "Screenshots are disabled for WCU read-only collection.");
        AddIf(blocked, options.AllowInvoke, "UIA Invoke is prohibited.");
        AddIf(blocked, options.AllowClick, "Click is prohibited.");
        AddIf(blocked, options.AllowSetValue, "SetValue is prohibited.");
        AddIf(blocked, options.AllowKeyboard, "Keyboard input is prohibited.");
        AddIf(blocked, options.AllowMouse, "Mouse input is prohibited.");
        AddIf(blocked, options.AllowClipboard, "Clipboard access is prohibited.");

        return new WindowsUiAutomationReadOnlyResult(
            WindowsUiAutomationReadOnlyStatus.SkippedDisabled,
            Snapshot: null,
            InvokeUsed: false,
            ClickUsed: false,
            SetValueUsed: false,
            KeyboardUsed: false,
            MouseUsed: false,
            ClipboardUsed: false,
            ScreenshotCaptured: false,
            Reasons: blocked.Count == 0
                ? ["UIA live read-only collection is design-only and not run in fixture-safe validation."]
                : blocked);
    }

    private static void AddIf(ICollection<string> reasons, bool condition, string reason)
    {
        if (condition)
        {
            reasons.Add(reason);
        }
    }
}
