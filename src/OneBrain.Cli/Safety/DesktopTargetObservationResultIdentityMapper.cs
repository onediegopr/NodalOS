using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;

namespace OneBrain.Cli.Safety;

public static class DesktopTargetObservationResultIdentityMapper
{
    public static ElementIdentity? ToSelectedIdentity(DesktopTargetObservationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(result.SelectedRuntimeId) &&
            string.IsNullOrWhiteSpace(result.SelectedAutomationId) &&
            string.IsNullOrWhiteSpace(result.SelectedName) &&
            string.IsNullOrWhiteSpace(result.SelectedControlType) &&
            string.IsNullOrWhiteSpace(result.SelectedClassName) &&
            string.IsNullOrWhiteSpace(result.SelectedFrameworkId) &&
            string.IsNullOrWhiteSpace(result.SelectedAncestorPath) &&
            string.IsNullOrWhiteSpace(result.SelectedBoundingRect) &&
            string.IsNullOrWhiteSpace(result.SelectedProcessName) &&
            string.IsNullOrWhiteSpace(result.SelectedWindowTitle))
        {
            return null;
        }

        var provenance = string.IsNullOrWhiteSpace(result.SelectedRuntimeId)
            ? Provenance.Inferred
            : Provenance.Uia;

        return new ElementIdentity
        {
            RuntimeId = result.SelectedRuntimeId ?? "",
            AutomationId = result.SelectedAutomationId ?? "",
            Name = result.SelectedName ?? "",
            HelpText = result.SelectedHelpText ?? "",
            LegacyName = result.SelectedLegacyName ?? "",
            Role = result.SelectedControlType ?? "",
            ControlType = result.SelectedControlType ?? "",
            ClassName = result.SelectedClassName ?? "",
            FrameworkId = result.SelectedFrameworkId ?? "",
            ProcessName = result.SelectedProcessName ?? "",
            WindowTitle = result.SelectedWindowTitle ?? "",
            AncestorPath = result.SelectedAncestorPath ?? "",
            BoundsHint = result.SelectedBoundingRect ?? "",
            Provenance = provenance
        };
    }

    public static IdentityStrength ResolveIdentityStrength(DesktopTargetObservationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var identity = ToSelectedIdentity(result);
        if (identity == null)
            return IdentityStrength.None;

        if (identity.IsStrong)
            return IdentityStrength.Strong;

        return HasWeakIdentityCriteria(identity)
            ? IdentityStrength.Weak
            : IdentityStrength.None;
    }

    private static bool HasWeakIdentityCriteria(ElementIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.AutomationId) ||
               !string.IsNullOrWhiteSpace(identity.Name) ||
               !string.IsNullOrWhiteSpace(identity.HelpText) ||
               !string.IsNullOrWhiteSpace(identity.LegacyName) ||
               !string.IsNullOrWhiteSpace(identity.EffectiveControlType) ||
               !string.IsNullOrWhiteSpace(identity.ClassName) ||
               !string.IsNullOrWhiteSpace(identity.FrameworkId) ||
               !string.IsNullOrWhiteSpace(identity.AncestorPath) ||
               !string.IsNullOrWhiteSpace(identity.BoundsHint) ||
               !string.IsNullOrWhiteSpace(identity.ProcessName) ||
               !string.IsNullOrWhiteSpace(identity.WindowTitle);
    }
}
