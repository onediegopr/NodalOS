using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;

namespace OneBrain.Core.Selectors.Web;

public static class WebSelectorBridge
{
    public static WebSelectorParity Evaluate(
        IReadOnlyList<WebCandidate>? candidates,
        string? targetText,
        string? legacySelectedName)
    {
        if (string.IsNullOrWhiteSpace(targetText))
        {
            return new WebSelectorParity
            {
                EngineFound = false,
                EngineVerdict = "InvalidInput",
                AgreesWithLegacy = string.IsNullOrWhiteSpace(legacySelectedName),
                Reasons = ["targetText is required for shadow selector evaluation"]
            };
        }

        var selector = new SelectorDefinition
        {
            SchemaVersion = 1,
            Provenance = Provenance.Inferred,
            Name = targetText.Trim()
        };

        return Evaluate(selector, candidates, legacySelectedName);
    }

    public static WebSelectorParity Evaluate(
        SelectorDefinition selector,
        IReadOnlyList<WebCandidate>? candidates,
        string? legacySelectedName)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var safeCandidates = candidates ?? Array.Empty<WebCandidate>();
        var reasons = BuildCandidateReasons(safeCandidates);
        var identities = safeCandidates.Select(WebCandidateMapper.ToElementIdentity).ToList();
        var resolution = SelectorEngine.Resolve(selector, identities);

        if (!resolution.Success)
        {
            var verdict = resolution.Ambiguous
                ? "Ambiguous"
                : resolution.FailureKind?.ToString() ?? "NotFound";

            return new WebSelectorParity
            {
                EngineFound = false,
                EngineVerdict = verdict,
                EngineSelectedName = null,
                AgreesWithLegacy = string.IsNullOrWhiteSpace(legacySelectedName),
                Ambiguous = resolution.Ambiguous,
                FailureKind = resolution.FailureKind,
                Reasons = MergeReasons(reasons, resolution.Reasons)
            };
        }

        var bestMatch = resolution.BestMatch;
        var verdictName = "LikelySame";
        if (bestMatch != null)
        {
            var expectedIdentity = selector.ExpectedIdentity ?? BuildWeakExpectedIdentity(selector);
            var match = ElementMatcher.Match(expectedIdentity, [bestMatch]);
            verdictName = match.Verdict switch
            {
                ElementMatchVerdict.Same when !bestMatch.IsStrong => ElementMatchVerdict.LikelySame.ToString(),
                ElementMatchVerdict.Different or ElementMatchVerdict.Unknown => ElementMatchVerdict.LikelySame.ToString(),
                _ => match.Verdict.ToString()
            };
        }

        var selectedName = EmptyToNull(bestMatch?.Name);
        return new WebSelectorParity
        {
            EngineFound = true,
            EngineVerdict = verdictName,
            EngineSelectedName = selectedName,
            AgreesWithLegacy = SelectedNamesAgree(legacySelectedName, selectedName),
            Ambiguous = false,
            FailureKind = null,
            Reasons = MergeReasons(reasons, resolution.Reasons)
        };
    }

    private static ElementIdentity BuildWeakExpectedIdentity(SelectorDefinition selector)
    {
        return new ElementIdentity
        {
            RuntimeId = "",
            Name = selector.Name ?? "",
            Role = selector.Role ?? "",
            ControlType = selector.Role ?? "",
            AutomationId = selector.AutomationId ?? "",
            HelpText = selector.HelpText ?? "",
            LegacyName = selector.LegacyName ?? "",
            ClassName = selector.ClassName ?? "",
            AncestorPath = selector.AncestorPath ?? "",
            Provenance = selector.Provenance
        };
    }

    private static IReadOnlyList<string> BuildCandidateReasons(IReadOnlyList<WebCandidate> candidates)
    {
        var reasons = new List<string>();
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            var label = string.IsNullOrWhiteSpace(candidate.Name)
                ? $"candidate[{index}]"
                : $"candidate[{index}] '{candidate.Name}'";

            if (!candidate.IsEnabled)
                reasons.Add($"{label} is disabled");

            if (candidate.IsOffscreen)
                reasons.Add($"{label} is offscreen");

            if (!candidate.HasInvoke)
                reasons.Add($"{label} does not expose invoke");
        }

        return reasons;
    }

    private static IReadOnlyList<string> MergeReasons(
        IReadOnlyList<string> candidateReasons,
        IReadOnlyList<string> resolutionReasons)
    {
        return candidateReasons
            .Concat(resolutionReasons)
            .Where(static reason => !string.IsNullOrWhiteSpace(reason))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool SelectedNamesAgree(string? legacySelectedName, string? engineSelectedName)
    {
        if (string.IsNullOrWhiteSpace(legacySelectedName) && string.IsNullOrWhiteSpace(engineSelectedName))
            return true;

        if (string.IsNullOrWhiteSpace(legacySelectedName) || string.IsNullOrWhiteSpace(engineSelectedName))
            return false;

        return string.Equals(
            legacySelectedName.Trim(),
            engineSelectedName.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
