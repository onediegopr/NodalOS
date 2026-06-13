using OneBrain.Core.Models;

namespace OneBrain.Core.Identity;

public sealed record ElementMatchResult(
    ElementMatchVerdict Verdict,
    double Score,
    IReadOnlyList<string> ReasonsFor,
    IReadOnlyList<string> ReasonsAgainst,
    ElementIdentity? BestMatch,
    IReadOnlyList<ElementIdentity> Candidates);
