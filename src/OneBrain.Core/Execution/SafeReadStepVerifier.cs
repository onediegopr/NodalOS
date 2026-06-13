using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;

namespace OneBrain.Core.Execution;

public sealed class SafeReadStepVerifier
{
    public StepVerificationResult Verify(RecipeSafetyContract contract, PatternReadResult readResult)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(readResult);

        var reasons = (readResult.Signals ?? Array.Empty<string>())
            .Concat(readResult.Reasons)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!readResult.Success)
        {
            return new StepVerificationResult(
                Success: false,
                FailureKind: readResult.FailureKind ?? FailureKind.Unverified,
                MatchVerdict: string.IsNullOrWhiteSpace(readResult.InvokeTimeIdentityVerdict)
                    ? "Different"
                    : readResult.InvokeTimeIdentityVerdict,
                Reasons: reasons.Count == 0 ? ["read dispatch failed before verification"] : reasons,
                ObservedIdentity: readResult.ObservedIdentity);
        }

        if (contract.ExpectedIdentity == null)
        {
            return new StepVerificationResult(
                Success: false,
                FailureKind: FailureKind.PolicyDenied,
                MatchVerdict: "Unknown",
                Reasons: reasons.Concat(["safe.read contract is missing expected identity"]).ToList(),
                ObservedIdentity: readResult.ObservedIdentity);
        }

        if (readResult.ObservedIdentity == null)
        {
            return new StepVerificationResult(
                Success: false,
                FailureKind: FailureKind.Unverified,
                MatchVerdict: "Unknown",
                Reasons: reasons.Concat(["read dispatch did not report an observed identity"]).ToList(),
                ObservedIdentity: null);
        }

        var match = ElementMatcher.Match(contract.ExpectedIdentity, [readResult.ObservedIdentity]);
        var verified = readResult.WindowFound &&
                       readResult.TargetVisible &&
                       !readResult.MutationObserved &&
                       match.Verdict == ElementMatchVerdict.Same;

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
                        $"windowFound={readResult.WindowFound.ToString().ToLowerInvariant()}",
                        $"targetVisible={readResult.TargetVisible.ToString().ToLowerInvariant()}",
                        $"mutationObserved={readResult.MutationObserved.ToString().ToLowerInvariant()}"
                    ])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ObservedIdentity: readResult.ObservedIdentity);
        }

        return new StepVerificationResult(
            Success: true,
            FailureKind: null,
            MatchVerdict: match.Verdict.ToString(),
            Reasons: reasons
                .Concat(match.ReasonsFor)
                .Concat(["safe.read verification passed"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ObservedIdentity: readResult.ObservedIdentity);
    }
}
