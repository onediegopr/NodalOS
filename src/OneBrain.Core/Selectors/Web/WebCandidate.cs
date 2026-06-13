namespace OneBrain.Core.Selectors.Web;

public sealed record WebCandidate
{
    public string? RuntimeId { get; init; }
    public string? Name { get; init; }
    public string? ControlType { get; init; }
    public string? AutomationId { get; init; }
    public string? BoundingRect { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsOffscreen { get; init; }
    public bool HasInvoke { get; init; }
}
