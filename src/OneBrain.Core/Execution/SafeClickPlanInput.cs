using OneBrain.Core.Safety;
using OneBrain.Core.Selectors.Web;

namespace OneBrain.Core.Execution;

public sealed record SafeClickPlanInput
{
    public string? Mode { get; init; }
    public string? TargetText { get; init; }
    public string ActionKind { get; init; } = "click";
    public ApprovalManifest? Manifest { get; init; }
    public IReadOnlyList<WebCandidate> Candidates { get; init; } = Array.Empty<WebCandidate>();
    public bool Reversible { get; init; }
}
