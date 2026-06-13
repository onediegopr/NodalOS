using OneBrain.Core.Contracts;

namespace OneBrain.Core.Models;

public sealed record ElementIdentity
{
    public ElementIdentity()
    {
    }

    public ElementIdentity(string runtimeId, string role, string name, string automationId)
    {
        RuntimeId = runtimeId ?? "";
        Role = role ?? "";
        ControlType = role ?? "";
        Name = name ?? "";
        AutomationId = automationId ?? "";
    }

    public string RuntimeId { get; init; } = "";
    public string AutomationId { get; init; } = "";
    public string Name { get; init; } = "";
    public string HelpText { get; init; } = "";
    public string LegacyName { get; init; } = "";
    public string LegacyValue { get; init; } = "";
    public string LabeledByName { get; init; } = "";
    public string Role { get; init; } = "";
    public string ControlType { get; init; } = "";
    public string ClassName { get; init; } = "";
    public string FrameworkId { get; init; } = "";
    public string ProcessName { get; init; } = "";
    public string WindowTitle { get; init; } = "";
    public string AncestorPath { get; init; } = "";
    public int? SiblingIndex { get; init; }
    public string ParentFingerprint { get; init; } = "";
    public string BoundsHint { get; init; } = "";
    public Provenance Provenance { get; init; } = Provenance.Inferred;
    public int SchemaVersion { get; init; } = 2;

    public bool IsStrong => RuntimeId.Length > 0;

    public string EffectiveControlType =>
        string.IsNullOrWhiteSpace(ControlType) ? Role : ControlType;

    public bool MatchesStrong(ElementIdentity other) =>
        IsStrong &&
        other.IsStrong &&
        string.Equals(RuntimeId, other.RuntimeId, StringComparison.Ordinal);

    public bool MatchesWeak(ElementIdentity other) =>
        string.Equals(EffectiveControlType, other.EffectiveControlType, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(AutomationId, other.AutomationId, StringComparison.OrdinalIgnoreCase);
}
