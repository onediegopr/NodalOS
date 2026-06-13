using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed record SafeClickExecutionPlan(
    StepState ProjectedState,
    FailureKind? FailureKind,
    string? BlockReason,
    IdentityStrength IdentityStrength,
    bool ContractValid,
    string? BindingVerdict,
    bool? ParityAgrees,
    bool WouldDispatch,
    bool WouldUseUnsafeFallback,
    IReadOnlyList<string> Reasons);
