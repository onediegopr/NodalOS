using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed class EvidenceLedger
{
    private readonly List<StepTransitionEvidence> _entries = [];

    public IReadOnlyList<StepTransitionEvidence> Entries => _entries;

    public StepTransitionEvidence Append(
        DateTimeOffset occurredAtUtc,
        StepState fromState,
        StepState toState,
        StepTransition @event,
        FailureKind? failureKind,
        string? blockReason,
        string? contractId,
        string? approvalDecisionId,
        string? approvedIdentityDigest,
        string? observedIdentityDigest,
        string? matchVerdict,
        string? ownershipSnapshotHash,
        IReadOnlyList<string>? reasons)
    {
        if (RequiresFailureKind(toState) && failureKind == null)
            throw new InvalidOperationException("terminal failure transitions must carry a failure kind");

        var evidence = new StepTransitionEvidence(
            Sequence: _entries.Count + 1,
            OccurredAtUtc: occurredAtUtc,
            FromState: fromState,
            ToState: toState,
            Event: @event,
            FailureKind: failureKind,
            BlockReason: EmptyToNull(blockReason),
            ContractId: EmptyToNull(contractId),
            ApprovalDecisionId: EmptyToNull(approvalDecisionId),
            ApprovedIdentityDigest: EmptyToNull(approvedIdentityDigest),
            ObservedIdentityDigest: EmptyToNull(observedIdentityDigest),
            MatchVerdict: EmptyToNull(matchVerdict),
            OwnershipSnapshotHash: EmptyToNull(ownershipSnapshotHash),
            Reasons: reasons?.Where(reason => !string.IsNullOrWhiteSpace(reason)).ToList() ?? []);

        _entries.Add(evidence);
        return evidence;
    }

    private static bool RequiresFailureKind(StepState state) =>
        state is StepState.Blocked or StepState.Failed or StepState.Aborted;

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
