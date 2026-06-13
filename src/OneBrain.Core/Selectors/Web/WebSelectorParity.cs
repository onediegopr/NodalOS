using OneBrain.Core.Contracts;

namespace OneBrain.Core.Selectors.Web;

public sealed record WebSelectorParity
{
    public bool EngineFound { get; init; }
    public string? EngineVerdict { get; init; }
    public string? EngineSelectedName { get; init; }
    public bool AgreesWithLegacy { get; init; }
    public bool Ambiguous { get; init; }
    public FailureKind? FailureKind { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
}
