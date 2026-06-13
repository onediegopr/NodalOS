using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed record ContractValidation(
    bool IsValid,
    FailureKind? FailureKind,
    IReadOnlyList<string> Reasons);
