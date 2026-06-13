using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;

namespace OneBrain.Core.Selectors;

public static class SelectorEngine
{
    private const double AmbiguityEpsilon = 0.02;

    public static SelectorDefinition GenerateSelector(ElementIdentity observed)
    {
        ArgumentNullException.ThrowIfNull(observed);

        return new SelectorDefinition
        {
            SchemaVersion = 1,
            SelectorId = ElementFingerprintBuilder.Build(observed),
            Provenance = observed.Provenance,
            Role = EmptyToNull(observed.EffectiveControlType),
            Name = EmptyToNull(observed.Name),
            AutomationId = EmptyToNull(observed.AutomationId),
            HelpText = EmptyToNull(observed.HelpText),
            LegacyName = EmptyToNull(observed.LegacyName),
            ClassName = EmptyToNull(observed.ClassName),
            AncestorPath = EmptyToNull(observed.AncestorPath),
            StabilityScore = observed.IsStrong || !string.IsNullOrWhiteSpace(observed.AutomationId) ? 1 : 0.6,
            SpecificityScore = CountSpecificity(observed),
            SafetyScore = 0,
            ExpectedIdentity = observed
        };
    }

    public static SelectorResolution Resolve(SelectorDefinition selector, IReadOnlyList<ElementIdentity> candidates)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(candidates);

        if (!HasAnyCriteria(selector))
        {
            return new SelectorResolution(
                Success: false,
                Ambiguous: false,
                FailureKind: FailureKind.NotFound,
                BestMatch: null,
                Matches: [],
                Confidence: 0,
                Reasons: ["selector does not contain any supported criteria"]);
        }

        var scored = candidates
            .Select(candidate => Evaluate(selector, candidate))
            .Where(item => item.IsMatch)
            .OrderByDescending(item => item.Score)
            .ToList();

        if (scored.Count == 0)
        {
            var failureKind = selector.ExpectedIdentity?.IsStrong == true
                ? FailureKind.Stale
                : FailureKind.NotFound;

            return new SelectorResolution(
                Success: false,
                Ambiguous: false,
                FailureKind: failureKind,
                BestMatch: null,
                Matches: [],
                Confidence: 0,
                Reasons: ["no candidates satisfied the selector"]);
        }

        var top = scored[0];
        var equivalentTop = scored
            .Where(item => Math.Abs(item.Score - top.Score) <= AmbiguityEpsilon)
            .ToList();

        if (equivalentTop.Count > 1)
        {
            return new SelectorResolution(
                Success: false,
                Ambiguous: true,
                FailureKind: FailureKind.Ambiguous,
                BestMatch: null,
                Matches: equivalentTop.Select(item => item.Candidate).ToList(),
                Confidence: top.Score,
                Reasons: top.Reasons.Concat(["multiple equivalent matches remain"]).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        return new SelectorResolution(
            Success: true,
            Ambiguous: false,
            FailureKind: null,
            BestMatch: top.Candidate,
            Matches: [top.Candidate],
            Confidence: top.Score,
            Reasons: top.Reasons);
    }

    public static bool TryParseLegacySelector(string legacySelector, out SelectorDefinition selector)
    {
        selector = new SelectorDefinition();

        if (string.IsNullOrWhiteSpace(legacySelector))
            return false;

        var clauses = legacySelector
            .Split(['|', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (clauses.Length == 0)
            return false;

        string? role = null;
        string? name = null;
        string? automationId = null;
        string? helpText = null;
        string? legacyName = null;
        string? className = null;
        string? ancestorPath = null;

        foreach (var clause in clauses)
        {
            var parts = clause.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return false;

            var kind = parts[0].ToLowerInvariant();
            var value = parts[1];

            switch (kind)
            {
                case "role":
                    role = value;
                    break;
                case "name":
                    name = value;
                    break;
                case "automation-id":
                case "automationid":
                    automationId = value;
                    break;
                case "help":
                case "help-text":
                    helpText = value;
                    break;
                case "legacy":
                case "legacy-name":
                    legacyName = value;
                    break;
                case "class":
                    className = value;
                    break;
                case "ancestor":
                case "ancestor-path":
                    ancestorPath = value;
                    break;
                default:
                    return false;
            }
        }

        selector = new SelectorDefinition
        {
            SchemaVersion = 1,
            Provenance = Provenance.Inferred,
            Role = role,
            Name = name,
            AutomationId = automationId,
            HelpText = helpText,
            LegacyName = legacyName,
            ClassName = className,
            AncestorPath = ancestorPath
        };

        return HasAnyCriteria(selector);
    }

    private static bool HasAnyCriteria(SelectorDefinition selector) =>
        HasValue(selector.Role) ||
        HasValue(selector.Name) ||
        HasValue(selector.AutomationId) ||
        HasValue(selector.HelpText) ||
        HasValue(selector.LegacyName) ||
        HasValue(selector.ClassName) ||
        HasValue(selector.AncestorPath);

    private static CandidateEvaluation Evaluate(SelectorDefinition selector, ElementIdentity candidate)
    {
        var reasons = new List<string>();
        var score = 0d;

        if (HasValue(selector.Role))
        {
            if (!Exact(selector.Role, candidate.EffectiveControlType))
                return CandidateEvaluation.NoMatch(candidate, $"role mismatch: expected {selector.Role}");

            score += 0.25;
            reasons.Add("role matches");
        }

        if (HasValue(selector.AutomationId))
        {
            if (!Exact(selector.AutomationId, candidate.AutomationId))
                return CandidateEvaluation.NoMatch(candidate, $"automation id mismatch: expected {selector.AutomationId}");

            score += 0.35;
            reasons.Add("automation id matches");
        }

        if (HasValue(selector.ClassName))
        {
            if (!Exact(selector.ClassName, candidate.ClassName))
                return CandidateEvaluation.NoMatch(candidate, $"class mismatch: expected {selector.ClassName}");

            score += 0.10;
            reasons.Add("class name matches");
        }

        if (HasValue(selector.AncestorPath))
        {
            if (!Exact(selector.AncestorPath, candidate.AncestorPath))
                return CandidateEvaluation.NoMatch(candidate, $"ancestor path mismatch: expected {selector.AncestorPath}");

            score += 0.15;
            reasons.Add("ancestor path matches");
        }

        if (HasValue(selector.Name))
        {
            var matchKind = MatchStringSignal(selector.Name!, candidate.Name, candidate.HelpText, candidate.LegacyName, candidate.LabeledByName);
            if (matchKind == MatchKind.None)
                return CandidateEvaluation.NoMatch(candidate, $"name mismatch: expected {selector.Name}");

            score += matchKind == MatchKind.DirectName ? 0.25 : 0.18;
            reasons.Add(matchKind == MatchKind.DirectName
                ? "name matches"
                : "help/legacy/labeled-by matches requested name");
        }

        if (HasValue(selector.HelpText))
        {
            if (!Contains(selector.HelpText!, candidate.HelpText))
                return CandidateEvaluation.NoMatch(candidate, $"help text mismatch: expected {selector.HelpText}");

            score += 0.10;
            reasons.Add("help text matches");
        }

        if (HasValue(selector.LegacyName))
        {
            if (!Contains(selector.LegacyName!, candidate.LegacyName))
                return CandidateEvaluation.NoMatch(candidate, $"legacy name mismatch: expected {selector.LegacyName}");

            score += 0.10;
            reasons.Add("legacy name matches");
        }

        if (selector.ExpectedIdentity != null)
        {
            var identityResult = ElementMatcher.Match(selector.ExpectedIdentity, [candidate]);
            if (identityResult.Verdict == ElementMatchVerdict.Same)
            {
                score += 0.20;
                reasons.Add("expected identity matches strongly");
            }
            else if (identityResult.Verdict == ElementMatchVerdict.LikelySame)
            {
                score += 0.12;
                reasons.Add("expected identity matches weakly");
            }
        }

        return new CandidateEvaluation(candidate, true, Math.Min(score, 1d), reasons);
    }

    private static double CountSpecificity(ElementIdentity identity)
    {
        var total = 0d;
        if (HasValue(identity.AutomationId)) total += 0.35;
        if (HasValue(identity.EffectiveControlType)) total += 0.20;
        if (HasValue(identity.Name)) total += 0.20;
        if (HasValue(identity.ClassName)) total += 0.10;
        if (HasValue(identity.AncestorPath)) total += 0.15;
        return Math.Min(total, 1d);
    }

    private static MatchKind MatchStringSignal(string expected, params string?[] candidates)
    {
        var primaryCandidate = candidates.FirstOrDefault(HasValue);
        foreach (var candidate in candidates.Where(HasValue))
        {
            if (Contains(expected, candidate))
            {
                if (primaryCandidate != null &&
                    string.Equals(candidate, primaryCandidate, StringComparison.OrdinalIgnoreCase))
                    return MatchKind.DirectName;

                return MatchKind.Auxiliary;
            }
        }

        return MatchKind.None;
    }

    private static bool Contains(string expected, string? candidate) =>
        HasValue(candidate) &&
        candidate!.Contains(expected, StringComparison.OrdinalIgnoreCase);

    private static bool Exact(string? expected, string? candidate) =>
        HasValue(expected) &&
        HasValue(candidate) &&
        string.Equals(expected, candidate, StringComparison.OrdinalIgnoreCase);

    private static bool HasValue(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private enum MatchKind
    {
        None,
        DirectName,
        Auxiliary
    }

    private sealed record CandidateEvaluation(
        ElementIdentity Candidate,
        bool IsMatch,
        double Score,
        IReadOnlyList<string> Reasons)
    {
        public static CandidateEvaluation NoMatch(ElementIdentity candidate, string reason) =>
            new(candidate, false, 0, [reason]);
    }
}
