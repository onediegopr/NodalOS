using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;

namespace OneBrain.Core.Execution;

public enum SafeClickRuntimeStabilityVerdict
{
    Stable = 0,
    Changed = 1,
    Missing = 2,
    Unknown = 3,
    ReobservedStable = 4,
    ReobservedChanged = 5
}

public sealed record SafeClickRuntimeStability(
    string ApprovedDigest,
    string ObservedDigest,
    RuntimeIdentityMatch Match,
    bool RuntimeIdPresent,
    bool RuntimeIdChanged,
    string IdentitySource,
    long? ObserveAgeMs,
    bool ReobserveAttempted,
    bool ReobserveSucceeded,
    RuntimeIdentityMatch ReobserveMatch,
    SafeClickRuntimeStabilityVerdict StabilityVerdict,
    string? BlockReason)
{
    public bool AllowsDefaultDispatch =>
        ReobserveAttempted &&
        ReobserveSucceeded &&
        ReobserveMatch == RuntimeIdentityMatch.Same &&
        StabilityVerdict == SafeClickRuntimeStabilityVerdict.ReobservedStable;
}

public static class SafeClickRuntimeStabilityEvaluator
{
    public static SafeClickRuntimeStability Evaluate(
        ApprovalManifest? manifest,
        ElementIdentity? observedIdentity,
        bool reobserveAttempted,
        bool reobserveSucceeded,
        long? observeAgeMs = null)
    {
        var approvedDigest = manifest?.ApprovedIdentityDigest ?? "";
        var identitySource = manifest?.IdentitySource ?? "";
        var observedDigest = observedIdentity == null ? "" : ElementFingerprintBuilder.Build(observedIdentity);

        if (!reobserveAttempted)
        {
            return new SafeClickRuntimeStability(
                approvedDigest,
                observedDigest,
                RuntimeIdentityMatch.Unknown,
                RuntimeIdPresent: observedIdentity?.IsStrong == true,
                RuntimeIdChanged: false,
                identitySource,
                observeAgeMs,
                ReobserveAttempted: false,
                ReobserveSucceeded: false,
                ReobserveMatch: RuntimeIdentityMatch.Unknown,
                SafeClickRuntimeStabilityVerdict.Unknown,
                BlockReason: null);
        }

        if (!reobserveSucceeded || manifest == null || observedIdentity == null || !observedIdentity.IsStrong)
        {
            return new SafeClickRuntimeStability(
                approvedDigest,
                observedDigest,
                RuntimeIdentityMatch.Missing,
                RuntimeIdPresent: observedIdentity?.IsStrong == true,
                RuntimeIdChanged: false,
                identitySource,
                observeAgeMs,
                ReobserveAttempted: true,
                ReobserveSucceeded: reobserveSucceeded,
                ReobserveMatch: RuntimeIdentityMatch.Missing,
                SafeClickRuntimeStabilityVerdict.Missing,
                BlockReason: "ApprovalInvalidatedMissingIdentity");
        }

        var expectedIdentity = manifest.ApprovedSelector?.ExpectedIdentity;
        if (expectedIdentity == null || !expectedIdentity.IsStrong)
        {
            return new SafeClickRuntimeStability(
                approvedDigest,
                observedDigest,
                RuntimeIdentityMatch.Missing,
                RuntimeIdPresent: true,
                RuntimeIdChanged: false,
                identitySource,
                observeAgeMs,
                ReobserveAttempted: true,
                ReobserveSucceeded: true,
                ReobserveMatch: RuntimeIdentityMatch.Missing,
                SafeClickRuntimeStabilityVerdict.Missing,
                BlockReason: "ApprovalInvalidatedMissingIdentity");
        }

        var runtimeIdChanged =
            !string.IsNullOrWhiteSpace(expectedIdentity.RuntimeId) &&
            !string.IsNullOrWhiteSpace(observedIdentity.RuntimeId) &&
            !string.Equals(expectedIdentity.RuntimeId, observedIdentity.RuntimeId, StringComparison.Ordinal);

        var matchResult = ElementMatcher.Match(expectedIdentity, [observedIdentity]);
        var match = matchResult.Verdict == ElementMatchVerdict.Same
            ? RuntimeIdentityMatch.Same
            : RuntimeIdentityMatch.Different;

        var stable = match == RuntimeIdentityMatch.Same &&
                     !string.IsNullOrWhiteSpace(approvedDigest) &&
                     string.Equals(approvedDigest, observedDigest, StringComparison.Ordinal);

        return new SafeClickRuntimeStability(
            approvedDigest,
            observedDigest,
            match,
            RuntimeIdPresent: observedIdentity.IsStrong,
            RuntimeIdChanged: runtimeIdChanged,
            identitySource,
            observeAgeMs,
            ReobserveAttempted: true,
            ReobserveSucceeded: true,
            ReobserveMatch: match,
            stable ? SafeClickRuntimeStabilityVerdict.ReobservedStable : SafeClickRuntimeStabilityVerdict.ReobservedChanged,
            BlockReason: stable ? null : "ApprovalInvalidated");
    }
}
