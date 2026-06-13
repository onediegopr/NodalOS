using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed record StepTransitionEvidence(
    int Sequence,
    DateTimeOffset OccurredAtUtc,
    StepState FromState,
    StepState ToState,
    StepTransition Event,
    FailureKind? FailureKind,
    string? BlockReason,
    string? ContractId,
    string? ApprovalDecisionId,
    string? ApprovedIdentityDigest,
    string? ObservedIdentityDigest,
    string? MatchVerdict,
    string? OwnershipSnapshotHash,
    IReadOnlyList<string> Reasons);
