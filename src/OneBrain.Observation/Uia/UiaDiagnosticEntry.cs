namespace OneBrain.Observation.Uia;

/// <summary>
/// Verbose per-element record produced by the 'diagnose uia' command.
/// Carries every accessible UIA property, including extras not in UiElementSnapshot.
/// </summary>
public sealed record UiaDiagnosticEntry(
    int    Index,
    int    Depth,
    string Path,
    string Role,
    string Name,
    string RuntimeId,
    string AutomationId,
    string ClassName,
    string HelpText,
    string Value,
    string LegacyName,
    string LegacyValue,
    string LabeledByName,
    int    BoundsLeft,
    int    BoundsTop,
    int    BoundsRight,
    int    BoundsBottom,
    bool   BoundsAreZero,
    bool   IsOffscreen,
    bool   IsEnabled,
    IReadOnlyList<string> Patterns,
    bool   IncludedByWalker);
