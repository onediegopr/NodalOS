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
            RuntimeId = candidate.RuntimeId ?? "",
            Name = candidate.Name ?? "",
            Role = controlType,
            ControlType = controlType,
            AutomationId = candidate.AutomationId ?? "",
            BoundsHint = candidate.BoundingRect ?? "",
            ClassName = candidate.ClassName ?? "",
            HelpText = candidate.HelpText ?? "",
            LegacyName = candidate.LegacyName ?? "",
            FrameworkId = candidate.FrameworkId ?? "",
            AncestorPath = candidate.AncestorPath ?? "",
            ProcessName = candidate.ProcessName ?? "",
            WindowTitle = candidate.WindowTitle ?? "",
            Provenance = string.IsNullOrWhiteSpace(candidate.RuntimeId)
                ? Provenance.Inferred
                : Provenance.Uia
        };
    }
}
