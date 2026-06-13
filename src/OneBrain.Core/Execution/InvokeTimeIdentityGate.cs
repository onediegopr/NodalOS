using OneBrain.Core.Identity;
using OneBrain.Core.Models;

namespace OneBrain.Core.Execution;

public sealed record InvokeTimeIdentityDecision(
    bool Checked,
    bool Allowed,
    string Verdict,
    string Reason,
    string ExpectedIdentityDigest,
    string ObservedIdentityDigest,
    IReadOnlyList<string> Reasons);

public static class InvokeTimeIdentityGate
{
    public static InvokeTimeIdentityDecision Evaluate(
        ElementIdentity? expectedIdentity,
        ElementIdentity? observedIdentity)
    {
        var expectedDigest = expectedIdentity == null
            ? ""
            : ElementFingerprintBuilder.Build(expectedIdentity);
        var observedDigest = observedIdentity == null
            ? ""
            : ElementFingerprintBuilder.Build(observedIdentity);

        if (expectedIdentity == null)
        {
            return Deny(
                "Unknown",
                "InvokeTimeExpectedIdentityRequired",
                expectedDigest,
                observedDigest,
                ["expected identity is required before dispatch"]);
        }

        if (!expectedIdentity.IsStrong)
        {
            return Deny(
                "Unknown",
                "InvokeTimeExpectedIdentityRequired",
                expectedDigest,
                observedDigest,
                ["expected identity must be strong before dispatch"]);
        }

        if (observedIdentity == null)
        {
            return Deny(
                "Unknown",
                "InvokeTimeObservedIdentityRequired",
                expectedDigest,
                observedDigest,
                ["observed identity is required before dispatch"]);
        }

        if (!observedIdentity.IsStrong)
        {
            return Deny(
                "Unknown",
                "InvokeTimeObservedIdentityRequired",
                expectedDigest,
                observedDigest,
                ["observed identity must be strong before dispatch"]);
        }

        var match = ElementMatcher.Match(expectedIdentity, [observedIdentity]);
        var reasons = match.ReasonsFor
            .Concat(match.ReasonsAgainst)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (match.Verdict == ElementMatchVerdict.Same)
        {
            return new InvokeTimeIdentityDecision(
                Checked: true,
                Allowed: true,
                Verdict: match.Verdict.ToString(),
                Reason: "Same",
                ExpectedIdentityDigest: expectedDigest,
                ObservedIdentityDigest: observedDigest,
                Reasons: reasons.Count == 0 ? ["runtime id matches"] : reasons);
        }

        return Deny(
            match.Verdict.ToString(),
            "InvokeTimeIdentityMismatch",
            expectedDigest,
            observedDigest,
            reasons.Count == 0 ? ["invoke-time identity was not Same"] : reasons);
    }

    private static InvokeTimeIdentityDecision Deny(
        string verdict,
        string reason,
        string expectedDigest,
        string observedDigest,
        IReadOnlyList<string> reasons) =>
        new(
            Checked: true,
            Allowed: false,
            Verdict: verdict,
            Reason: reason,
            ExpectedIdentityDigest: expectedDigest,
            ObservedIdentityDigest: observedDigest,
            Reasons: reasons);
}
