using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsCrossSignalConsistencyGate
{
    public NodalOsCrossSignalConsistencyResult Evaluate(NodalOsCrossSignalConsistencyInput input)
    {
        var mismatches = new List<NodalOsCrossSignalMismatch>();

        if (input.IdentityActionAuthorityGranted || input.PerceptionActionAuthorityGranted || input.MemoryActionAuthorityGranted)
            mismatches.Add(NodalOsCrossSignalMismatch.UnsafeSignalPromotion);
        if (input.ExternalGeneralReady || input.ProductionEnabled || input.SafeActionSensitiveAuthorized)
            mismatches.Add(NodalOsCrossSignalMismatch.ScopeInflationDetected);
        if (input.SensitiveSurface)
            mismatches.Add(NodalOsCrossSignalMismatch.SensitiveSurface);
        if (input.OverlayBlocked || input.AmbiguousState)
            mismatches.Add(NodalOsCrossSignalMismatch.RequiresHumanReview);
        if (input.IdentityConfidence is NodalOsIdentityConfidence.Unknown or NodalOsIdentityConfidence.Low &&
            input.PerceptionReadiness == NodalOsPerceptionReadiness.UsableForReadOnlyContext)
            mismatches.Add(NodalOsCrossSignalMismatch.IdentityPerceptionMismatch);
        if (input.PerceptionReadiness is NodalOsPerceptionReadiness.Blocked or NodalOsPerceptionReadiness.Unsafe &&
            input.ActionDecision is not (NodalOsActionDecision.Denied or NodalOsActionDecision.BlockedAlways))
            mismatches.Add(NodalOsCrossSignalMismatch.PerceptionActionMismatch);
        if (input.CoreAuthorityRequired && !input.CoreApproved && !input.ObserveOnly &&
            input.ActionDecision is not (NodalOsActionDecision.Denied or NodalOsActionDecision.BlockedAlways))
            mismatches.Add(NodalOsCrossSignalMismatch.MissingCoreAuthority);
        if (input.ActionDecision is NodalOsActionDecision.Denied or NodalOsActionDecision.BlockedAlways &&
            (input.MemoryAccepted || input.MemoryConfidence is NodalOsMemoryConfidence.High or NodalOsMemoryConfidence.VerifiedFixturePattern or NodalOsMemoryConfidence.VerifiedRedactedLocalPattern))
            mismatches.Add(NodalOsCrossSignalMismatch.ActionMemoryMismatch);
        if (!input.MemoryRedacted || input.MemoryContainsSensitiveRawValues)
            mismatches.Add(NodalOsCrossSignalMismatch.UnredactedEvidence);

        var status = ResolveStatus(mismatches);
        var readiness = status == NodalOsCrossSignalConsistencyStatus.Consistent
            ? NodalOsHito162ReplacementReadiness.StableLocalFixtureFirst
            : mismatches.Contains(NodalOsCrossSignalMismatch.ScopeInflationDetected)
                ? NodalOsHito162ReplacementReadiness.BlockedByScopeInflation
                : mismatches.Contains(NodalOsCrossSignalMismatch.UnsafeSignalPromotion)
                    ? NodalOsHito162ReplacementReadiness.BlockedByUnsafeSignalPromotion
                    : NodalOsHito162ReplacementReadiness.NeedsMoreFixtureHardening;

        return new NodalOsCrossSignalConsistencyResult(
            status,
            readiness,
            mismatches.Count == 0 ? [NodalOsCrossSignalMismatch.None] : mismatches.Distinct().ToArray(),
            [
                "identity:fingerprint-v2:fixture-ready:redacted",
                "perception:robust-fixture-ready:redacted",
                "safe-action:local-fixture-boundary-ready:redacted",
                "process-memory:local-fixture-only-ready:redacted"
            ],
            status == NodalOsCrossSignalConsistencyStatus.Consistent
                ? "HITO-162 replacement is stable local fixture-first; continue local preview iteration without scope expansion."
                : "Keep local-only scope and harden mismatched fixture chain before any next phase.",
            Consistent: status == NodalOsCrossSignalConsistencyStatus.Consistent,
            ScopeInflationDetected: mismatches.Contains(NodalOsCrossSignalMismatch.ScopeInflationDetected) ||
                mismatches.Contains(NodalOsCrossSignalMismatch.SensitiveSurface),
            ActionAuthorityGranted: false,
            Redacted: true);
    }

    public static NodalOsCrossSignalConsistencyInput ConsistentFixtureInput() =>
        new(
            NodalOsIdentityConfidence.VerifiedFixture,
            IdentityActionAuthorityGranted: false,
            NodalOsPerceptionReadiness.UsableForReadOnlyContext,
            PerceptionActionAuthorityGranted: false,
            NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval,
            ActionDeniedReasons: [],
            CoreAuthorityRequired: true,
            CoreApproved: true,
            ObserveOnly: false,
            SafeActionSensitiveAuthorized: false,
            NodalOsMemoryConfidence.Medium,
            MemoryAccepted: false,
            MemoryActionAuthorityGranted: false,
            MemoryContainsSensitiveRawValues: false,
            MemoryRedacted: true,
            OverlayBlocked: false,
            AmbiguousState: false,
            SensitiveSurface: false,
            ExternalGeneralReady: false,
            ProductionEnabled: false);

    private static NodalOsCrossSignalConsistencyStatus ResolveStatus(IReadOnlyCollection<NodalOsCrossSignalMismatch> mismatches)
    {
        if (mismatches.Count == 0)
            return NodalOsCrossSignalConsistencyStatus.Consistent;
        if (mismatches.Contains(NodalOsCrossSignalMismatch.ScopeInflationDetected) ||
            mismatches.Contains(NodalOsCrossSignalMismatch.SensitiveSurface))
            return NodalOsCrossSignalConsistencyStatus.ScopeInflationDetected;
        if (mismatches.Contains(NodalOsCrossSignalMismatch.UnsafeSignalPromotion))
            return NodalOsCrossSignalConsistencyStatus.UnsafeSignalPromotion;
        if (mismatches.Contains(NodalOsCrossSignalMismatch.MissingCoreAuthority))
            return NodalOsCrossSignalConsistencyStatus.MissingCoreAuthority;
        if (mismatches.Contains(NodalOsCrossSignalMismatch.ActionMemoryMismatch) ||
            mismatches.Contains(NodalOsCrossSignalMismatch.UnredactedEvidence))
            return NodalOsCrossSignalConsistencyStatus.ActionMemoryMismatch;
        if (mismatches.Contains(NodalOsCrossSignalMismatch.PerceptionActionMismatch))
            return NodalOsCrossSignalConsistencyStatus.PerceptionActionMismatch;
        if (mismatches.Contains(NodalOsCrossSignalMismatch.IdentityPerceptionMismatch))
            return NodalOsCrossSignalConsistencyStatus.IdentityPerceptionMismatch;
        return NodalOsCrossSignalConsistencyStatus.RequiresHumanReview;
    }
}
