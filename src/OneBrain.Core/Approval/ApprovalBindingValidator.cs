using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.Approval;

public sealed class ApprovalBindingValidator
{
    public ApprovalBindingResult Validate(
        ApprovalBinding binding,
        ElementIdentity expectedIdentity,
        IReadOnlyList<ElementIdentity> candidates,
        bool reversible)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(expectedIdentity);
        ArgumentNullException.ThrowIfNull(candidates);

        var resolution = SelectorEngine.Resolve(binding.Selector, candidates);
        if (!resolution.Success || resolution.BestMatch == null)
        {
            var failure = resolution.FailureKind ?? FailureKind.NotFound;
            return new ApprovalBindingResult(
                Success: false,
                FailureKind: failure,
                BlockReason: failure == FailureKind.Ambiguous ? "ApprovalAmbiguous" : failure == FailureKind.NotFound ? "ApprovalTargetNotFound" : "ApprovalInvalidated",
                MatchVerdict: failure.ToString(),
                ObservedIdentityDigest: null,
                ObservedIdentity: null,
                Reasons: resolution.Reasons);
        }

        var observed = resolution.BestMatch;
        var observedDigest = ElementFingerprintBuilder.Build(observed);
        var match = ElementMatcher.Match(expectedIdentity, [observed]);

        var digestMatches = string.Equals(binding.ApprovedIdentityDigest, observedDigest, StringComparison.Ordinal);

        if (digestMatches && match.Verdict == ElementMatchVerdict.Same)
        {
            return new ApprovalBindingResult(
                Success: true,
                FailureKind: null,
                BlockReason: "",
                MatchVerdict: match.Verdict.ToString(),
                ObservedIdentityDigest: observedDigest,
                ObservedIdentity: observed,
                Reasons: match.ReasonsFor.Count == 0 ? ["approval binding matched strongly"] : match.ReasonsFor);
        }

        if (reversible && match.Verdict == ElementMatchVerdict.LikelySame)
        {
            return new ApprovalBindingResult(
                Success: true,
                FailureKind: null,
                BlockReason: "",
                MatchVerdict: match.Verdict.ToString(),
                ObservedIdentityDigest: observedDigest,
                ObservedIdentity: observed,
                Reasons: match.ReasonsFor.Concat(digestMatches
                        ? ["reversible contract accepted likely-same match"]
                        : ["reversible contract accepted likely-same match even though digest changed"])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }

        if (!digestMatches)
        {
            return new ApprovalBindingResult(
                Success: false,
                FailureKind: FailureKind.Stale,
                BlockReason: "ApprovalInvalidated",
                MatchVerdict: match.Verdict.ToString(),
                ObservedIdentityDigest: observedDigest,
                ObservedIdentity: observed,
                Reasons: match.ReasonsAgainst.Concat(["approved identity digest differs from observed identity digest"]).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        var failureKind = match.Verdict switch
        {
            ElementMatchVerdict.Ambiguous => FailureKind.Ambiguous,
            ElementMatchVerdict.Stale => FailureKind.Stale,
            _ => FailureKind.Stale
        };

        return new ApprovalBindingResult(
            Success: false,
            FailureKind: failureKind,
            BlockReason: "ApprovalInvalidated",
            MatchVerdict: match.Verdict.ToString(),
            ObservedIdentityDigest: observedDigest,
            ObservedIdentity: observed,
            Reasons: match.ReasonsAgainst.Concat(["observed identity no longer matches the approved identity"]).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }
}
