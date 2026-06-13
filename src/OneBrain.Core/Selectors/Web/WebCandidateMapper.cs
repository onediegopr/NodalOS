using OneBrain.Core.Contracts;
using OneBrain.Core.Models;

namespace OneBrain.Core.Selectors.Web;

public static class WebCandidateMapper
{
    public static ElementIdentity ToElementIdentity(WebCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var controlType = candidate.ControlType ?? "";

        return new ElementIdentity
        {
            RuntimeId = "",
            Name = candidate.Name ?? "",
            Role = controlType,
            ControlType = controlType,
            AutomationId = candidate.AutomationId ?? "",
            BoundsHint = candidate.BoundingRect ?? "",
            Provenance = Provenance.Inferred
        };
    }
}
