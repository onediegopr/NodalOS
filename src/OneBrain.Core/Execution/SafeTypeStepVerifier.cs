using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;

namespace OneBrain.Core.Execution;

public sealed class SafeTypeStepVerifier
{
    public StepVerificationResult Verify(RecipeSafetyContract contract, TypeExecutionResult typeResult, string approvedText)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(typeResult);

        var reasons = (typeResult.Signals ?? Array.Empty<string>())
            .Concat(typeResult.Reasons)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!typeResult.Success)
        {
            return new StepVerificationResult(
                Success: false,
                FailureKind: typeResult.FailureKind ?? FailureKind.Unverified,
                MatchVerdict: string.IsNullOrWhiteSpace(typeResult.InvokeTimeIdentityVerdict)
                    ? "Different"
                    : typeResult.InvokeTimeIdentityVerdict,
                Reasons: reasons.Count == 0 ? ["type dispatch failed before verification"] : reasons,
                ObservedIdentity: typeResult.ObservedIdentity);
        }

        if (contract.ExpectedIdentity == null)
        {
            return Fail(FailureKind.PolicyDenied, "Unknown", reasons, "safe.type contract is missing expected identity", typeResult.ObservedIdentity);
        }

        if (typeResult.ObservedIdentity == null)
        {
            return Fail(FailureKind.Unverified, "Unknown", reasons, "safe.type dispatch did not report observed identity", null);
        }

        var match = ElementMatcher.Match(contract.ExpectedIdentity, [typeResult.ObservedIdentity]);
        var verified =
            match.Verdict == ElementMatchVerdict.Same &&
            typeResult.InvokeTimeIdentityChecked &&
            typeResult.InvokeTimeIdentityVerdict == ElementMatchVerdict.Same.ToString() &&
            typeResult.OwnershipChecked &&
            typeResult.OwnershipAllowed &&
            typeResult.SurfaceAllowed &&
            typeResult.MutationObserved &&
            string.Equals(typeResult.ValueAfter, approvedText, StringComparison.Ordinal);

        if (!verified)
        {
            var failureKind = match.Verdict == ElementMatchVerdict.Ambiguous
                ? FailureKind.Ambiguous
                : match.Verdict is ElementMatchVerdict.Stale or ElementMatchVerdict.Different
                    ? FailureKind.Stale
                    : FailureKind.Unverified;

            return new StepVerificationResult(
                Success: false,
                FailureKind: failureKind,
                MatchVerdict: match.Verdict.ToString(),
                Reasons: reasons
                    .Concat(match.ReasonsFor)
                    .Concat(match.ReasonsAgainst)
                    .Concat(
                    [
                        $"invokeTimeIdentityChecked={typeResult.InvokeTimeIdentityChecked.ToString().ToLowerInvariant()}",
                        $"ownershipChecked={typeResult.OwnershipChecked.ToString().ToLowerInvariant()}",
                        $"ownershipAllowed={typeResult.OwnershipAllowed.ToString().ToLowerInvariant()}",
                        $"surfaceAllowed={typeResult.SurfaceAllowed.ToString().ToLowerInvariant()}",
                        $"mutationObserved={typeResult.MutationObserved.ToString().ToLowerInvariant()}",
                        $"valueAfterMatchesApproved={string.Equals(typeResult.ValueAfter, approvedText, StringComparison.Ordinal).ToString().ToLowerInvariant()}"
                    ])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ObservedIdentity: typeResult.ObservedIdentity);
        }

        return new StepVerificationResult(
            Success: true,
            FailureKind: null,
            MatchVerdict: match.Verdict.ToString(),
            Reasons: reasons
                .Concat(match.ReasonsFor)
                .Concat(["safe.type verification passed"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ObservedIdentity: typeResult.ObservedIdentity);
    }

    private static StepVerificationResult Fail(
        FailureKind failureKind,
        string verdict,
        IReadOnlyList<string> reasons,
        string reason,
        OneBrain.Core.Models.ElementIdentity? observedIdentity)
    {
        return new StepVerificationResult(
            Success: false,
            FailureKind: failureKind,
            MatchVerdict: verdict,
            Reasons: reasons.Concat([reason]).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ObservedIdentity: observedIdentity);
    }
}
