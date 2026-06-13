using OneBrain.Core.Models;

namespace OneBrain.Core.Identity;

public static class ElementMatcher
{
    private const double AmbiguityEpsilon = 0.02;

    public static ElementMatchResult Match(ElementIdentity expected, IReadOnlyList<ElementIdentity> candidates)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            return new ElementMatchResult(
                expected.IsStrong || HasWeakIdentity(expected) ? ElementMatchVerdict.Stale : ElementMatchVerdict.Unknown,
                0,
                [],
                ["no candidates available"],
                null,
                []);
        }

        var scored = candidates
            .Select(candidate => ScoreCandidate(expected, candidate))
            .OrderByDescending(item => item.Score)
            .ToList();

        var top = scored[0];
        var equivalentTop = scored
            .Where(item => Math.Abs(item.Score - top.Score) <= AmbiguityEpsilon)
            .ToList();

        if (equivalentTop.Count > 1)
        {
            return new ElementMatchResult(
                ElementMatchVerdict.Ambiguous,
                top.Score,
                top.ReasonsFor,
                top.ReasonsAgainst.Concat(["multiple equivalent candidates remain"]).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                null,
                equivalentTop.Select(item => item.Candidate).ToList());
        }

        var verdict = top switch
        {
            { RuntimeIdExact: true } => ElementMatchVerdict.Same,
            { AutomationIdExact: true, ControlTypeExact: true, Score: >= 0.55 } => ElementMatchVerdict.LikelySame,
            { Score: >= 0.70 } => ElementMatchVerdict.LikelySame,
            { Score: >= 0.40 } => ElementMatchVerdict.Unknown,
            _ => ElementMatchVerdict.Different
        };

        return new ElementMatchResult(
            verdict,
            top.Score,
            top.ReasonsFor,
            top.ReasonsAgainst,
            top.Candidate,
            [top.Candidate]);
    }

    private static bool HasWeakIdentity(ElementIdentity identity) =>
        !string.IsNullOrWhiteSpace(identity.AutomationId) ||
        !string.IsNullOrWhiteSpace(identity.Name) ||
        !string.IsNullOrWhiteSpace(identity.EffectiveControlType);

    private static CandidateScore ScoreCandidate(ElementIdentity expected, ElementIdentity candidate)
    {
        var reasonsFor = new List<string>();
        var reasonsAgainst = new List<string>();
        var score = 0d;
        var runtimeIdExact = false;
        var automationIdExact = false;
        var controlTypeExact = false;

        if (expected.IsStrong && candidate.IsStrong)
        {
            if (string.Equals(expected.RuntimeId, candidate.RuntimeId, StringComparison.Ordinal))
            {
                reasonsFor.Add("runtime id matches");
                return new CandidateScore(candidate, 1d, reasonsFor, reasonsAgainst, true, false, false);
            }

            reasonsAgainst.Add("runtime id differs");
        }

        if (Exact(expected.AutomationId, candidate.AutomationId))
        {
            automationIdExact = true;
            score += 0.30;
            reasonsFor.Add("automation id matches");
        }
        else if (HasValue(expected.AutomationId))
        {
            reasonsAgainst.Add("automation id differs");
        }

        if (Exact(expected.EffectiveControlType, candidate.EffectiveControlType))
        {
            controlTypeExact = true;
            score += 0.20;
            reasonsFor.Add("control type matches");
        }
        else if (HasValue(expected.EffectiveControlType))
        {
            reasonsAgainst.Add("control type differs");
        }

        if (Exact(expected.AncestorPath, candidate.AncestorPath))
        {
            score += 0.15;
            reasonsFor.Add("ancestor path matches");
        }
        else if (HasValue(expected.AncestorPath))
        {
            reasonsAgainst.Add("ancestor path differs");
        }

        if (Exact(expected.ParentFingerprint, candidate.ParentFingerprint))
        {
            score += 0.10;
            reasonsFor.Add("parent fingerprint matches");
        }
        else if (HasValue(expected.ParentFingerprint))
        {
            reasonsAgainst.Add("parent fingerprint differs");
        }

        if (expected.SiblingIndex.HasValue && candidate.SiblingIndex == expected.SiblingIndex)
        {
            score += 0.05;
            reasonsFor.Add("sibling index matches");
        }
        else if (expected.SiblingIndex.HasValue)
        {
            reasonsAgainst.Add("sibling index differs");
        }

        if (MatchesNameSignal(expected, candidate))
        {
            score += 0.15;
            reasonsFor.Add("name/help/legacy signal matches");
        }
        else if (HasAnyNameSignal(expected))
        {
            reasonsAgainst.Add("name/help/legacy signal differs");
        }

        if (Exact(expected.ClassName, candidate.ClassName))
        {
            score += 0.05;
            reasonsFor.Add("class name matches");
        }
        else if (HasValue(expected.ClassName))
        {
            reasonsAgainst.Add("class name differs");
        }

        if (!runtimeIdExact &&
            HasValue(expected.RuntimeId) &&
            score >= 0.70)
        {
            reasonsFor.Add("strong weak match suggests recreated element");
        }

        return new CandidateScore(candidate, Math.Min(score, 0.99), reasonsFor, reasonsAgainst, runtimeIdExact, automationIdExact, controlTypeExact);
    }

    private static bool MatchesNameSignal(ElementIdentity expected, ElementIdentity candidate)
    {
        var expectedSignals = GetNameSignals(expected);
        var candidateSignals = GetNameSignals(candidate);

        return expectedSignals.Any(expectedSignal =>
            candidateSignals.Any(candidateSignal =>
                candidateSignal.Contains(expectedSignal, StringComparison.OrdinalIgnoreCase) ||
                expectedSignal.Contains(candidateSignal, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasAnyNameSignal(ElementIdentity identity) =>
        GetNameSignals(identity).Count > 0;

    private static IReadOnlyList<string> GetNameSignals(ElementIdentity identity)
    {
        return
        [
            .. new[]
            {
                identity.Name,
                identity.HelpText,
                identity.LegacyName,
                identity.LegacyValue,
                identity.LabeledByName
            }
            .Where(HasValue)
            .Select(ElementFingerprintBuilder.Normalize)
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static bool Exact(string? left, string? right) =>
        HasValue(left) &&
        HasValue(right) &&
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool HasValue(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    private sealed record CandidateScore(
        ElementIdentity Candidate,
        double Score,
        IReadOnlyList<string> ReasonsFor,
        IReadOnlyList<string> ReasonsAgainst,
        bool RuntimeIdExact,
        bool AutomationIdExact,
        bool ControlTypeExact);
}
