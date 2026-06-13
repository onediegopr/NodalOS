using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;

namespace OneBrain.Core.Execution;

public sealed class SafeClickStepVerifier : IStepVerifier
{
    public StepVerificationResult Verify(RecipeSafetyContract contract, PatternExecutionResult dispatchResult)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(dispatchResult);

        var reasons = (dispatchResult.Signals ?? Array.Empty<string>())
            .Concat(dispatchResult.Reasons)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!dispatchResult.Success)
        {
            return new StepVerificationResult(
                Success: false,
                FailureKind: dispatchResult.FailureKind ?? FailureKind.Unverified,
                MatchVerdict: "Different",
                Reasons: reasons.Count == 0 ? ["dispatch failed before verification"] : reasons,
                ObservedIdentity: dispatchResult.ObservedIdentity);
        }

        if (contract.ExpectedIdentity == null)
        {
            return new StepVerificationResult(
                Success: false,
                FailureKind: FailureKind.PolicyDenied,
                MatchVerdict: "Unknown",
                Reasons: reasons.Concat(["safe.click contract is missing expected identity"]).ToList(),
                ObservedIdentity: dispatchResult.ObservedIdentity);
        }

        if (dispatchResult.ObservedIdentity == null)
        {
            return new StepVerificationResult(
                Success: false,
                FailureKind: FailureKind.Unverified,
                MatchVerdict: "Unknown",
                Reasons: reasons.Concat(["dispatch did not report an observed identity"]).ToList(),
                ObservedIdentity: null);
        }

        var match = ElementMatcher.Match(contract.ExpectedIdentity, [dispatchResult.ObservedIdentity]);
        var verified = dispatchResult.WindowFound &&
                       dispatchResult.TargetVisible &&
                       dispatchResult.ObservedActions == 1 &&
                       match.Verdict == ElementMatchVerdict.Same;

        if (!verified)
        {
            var failureKind = match.Verdict == ElementMatchVerdict.Ambiguous
                ? FailureKind.Ambiguous
                : match.Verdict == ElementMatchVerdict.Stale || match.Verdict == ElementMatchVerdict.Different
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
                        $"windowFound={dispatchResult.WindowFound.ToString().ToLowerInvariant()}",
                        $"targetVisible={dispatchResult.TargetVisible.ToString().ToLowerInvariant()}",
                        $"observedActions={dispatchResult.ObservedActions}"
                    ])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ObservedIdentity: dispatchResult.ObservedIdentity);
        }

        return new StepVerificationResult(
            Success: true,
            FailureKind: null,
            MatchVerdict: match.Verdict.ToString(),
            Reasons: reasons
                .Concat(match.ReasonsFor)
                .Concat(["safe.click verification passed"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ObservedIdentity: dispatchResult.ObservedIdentity);
    }
}
