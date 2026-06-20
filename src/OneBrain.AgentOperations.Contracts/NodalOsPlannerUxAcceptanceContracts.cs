namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsPlannerUxAcceptanceState
{
    EmptyAssignment,
    DraftAvailable,
    BlockedByPlannerReadiness,
    BlockedByRuntimeDisabled,
    BlockedByLlmDisabled,
    BlockedByFilesystemDisabled,
    EvidenceRefsMissing,
    ContextNeedsReview,
    WorkItemDependencyBlocked,
    AllWorkItemsDraftOnly
}

public sealed record NodalOsPlannerUxAcceptanceCriterion
{
    public required string CriterionId { get; init; }

    public required string UserFacingTextRedacted { get; init; }

    public required bool PassedByContract { get; init; }
}

public sealed record NodalOsPlannerUxAcceptancePack
{
    public required string AcceptancePackId { get; init; }

    public IReadOnlyList<NodalOsPlannerUxAcceptanceCriterion> Criteria { get; init; } = [];

    public IReadOnlyList<NodalOsPlannerUxAcceptanceState> CoveredStates { get; init; } = [];

    public IReadOnlyList<string> UserFacingExplanationsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool DraftOnly { get; init; }

    public required bool UiInteractionsAreNoOp { get; init; }

    public required bool CanAuthorizeRuntime { get; init; }

    public required bool CanTriggerLlmProvider { get; init; }

    public required bool CanAccessFilesystem { get; init; }

    public required bool CanCallNetwork { get; init; }

    public required bool ApprovalUnlocksRuntime { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
