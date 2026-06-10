namespace OneBrain.Core.Models;

public sealed record UiElementSnapshot(
    string Ref,
    string Role,
    string Name,
    string AutomationId,
    string ClassName,
    WindowBounds Bounds,
    bool IsEnabled,
    bool IsOffscreen,
    bool IsKeyboardFocusable,
    IReadOnlyList<string> Patterns,
    IReadOnlyList<string> Actions);
