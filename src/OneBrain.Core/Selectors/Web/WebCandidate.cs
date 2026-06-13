namespace OneBrain.Core.Selectors.Web;

public sealed record WebCandidate
{
    public string? RuntimeId { get; init; }
    public string? Name { get; init; }
    public string? ControlType { get; init; }
    public string? AutomationId { get; init; }
    public string? BoundingRect { get; init; }
    public string? ClassName { get; init; }
    public string? HelpText { get; init; }
    public string? LegacyName { get; init; }
    public string? FrameworkId { get; init; }
    public string? AncestorPath { get; init; }
    public string? ProcessName { get; init; }
    public string? WindowTitle { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsOffscreen { get; init; }
    public bool HasInvoke { get; init; }
}
