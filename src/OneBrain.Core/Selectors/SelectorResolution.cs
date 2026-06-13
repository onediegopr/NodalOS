using OneBrain.Core.Contracts;
using OneBrain.Core.Models;

namespace OneBrain.Core.Selectors;

public sealed record SelectorResolution(
    bool Success,
    bool Ambiguous,
    FailureKind? FailureKind,
    ElementIdentity? BestMatch,
    IReadOnlyList<ElementIdentity> Matches,
    double Confidence,
    IReadOnlyList<string> Reasons);
