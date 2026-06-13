using OneBrain.Core.Contracts;
using OneBrain.Core.Models;

namespace OneBrain.Core.Execution;

public sealed record StepVerificationResult(
    bool Success,
    FailureKind? FailureKind,
    string MatchVerdict,
    IReadOnlyList<string> Reasons,
    ElementIdentity? ObservedIdentity = null);

public interface IStepVerifier
{
    StepVerificationResult Verify(RecipeSafetyContract contract, PatternExecutionResult dispatchResult);
}
