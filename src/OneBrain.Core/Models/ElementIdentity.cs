namespace OneBrain.Core.Models;

public sealed record ElementIdentity(
    string RuntimeId,
    string Role,
    string Name,
    string AutomationId)
{
    public bool IsStrong => RuntimeId.Length > 0;

    public bool MatchesStrong(ElementIdentity other) =>
        IsStrong && RuntimeId == other.RuntimeId;

    public bool MatchesWeak(ElementIdentity other) =>
        Role == other.Role &&
        Name == other.Name &&
        AutomationId == other.AutomationId;
}
