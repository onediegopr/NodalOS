namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsPlannerGovernanceCloseoutDecision
{
    ReadyForNextGovernedPhase,
    NotReadyForRuntime,
    NotReadyForRealPlanner,
    NotReadyForLlmCalls,
    NotReadyForFilesystem,
    RequiredNextAudit,
    RequiredBeforeRealPlanner,
    RequiredBeforeLlm,
    RequiredBeforeFilesystem,
    RequiredBeforeRuntime
}

public sealed record NodalOsPlannerGovernanceStatus
{
    public required bool AssignmentContractsReady { get; init; }

    public required bool TaskGraphDraftReady { get; init; }

    public required bool MissionPlanPreviewReady { get; init; }

    public required bool ReviewCardsReady { get; init; }

    public required bool UiPreviewReady { get; init; }

    public required bool NoOpInteractionsReady { get; init; }

    public required bool MockPersistenceReady { get; init; }

    public required bool HandoffReady { get; init; }

    public required bool SafetyAuditReady { get; init; }

    public required bool HistoryMockReady { get; init; }

    public required bool HandoffComparePreviewReady { get; init; }

    public required bool PlannerRuntimeImplemented { get; init; }

    public required bool RuntimeExecutionBlocked { get; init; }

    public required bool LlmPromptBlocked { get; init; }

    public required bool FilesystemBlocked { get; init; }

    public required bool CloudBlocked { get; init; }
}

public sealed record NodalOsPlannerGovernanceCloseoutPack
{
    public required string CloseoutPackId { get; init; }

    public required NodalOsPlannerGovernanceStatus Status { get; init; }

    public IReadOnlyList<NodalOsPlannerGovernanceCloseoutDecision> Decisions { get; init; } = [];

    public IReadOnlyList<string> CompletedScopeRedacted { get; init; } = [];

    public IReadOnlyList<string> StillBlockedRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockReasonsRedacted { get; init; } = [];

    public IReadOnlyList<string> RecommendedNextStagesRedacted { get; init; } = [];

    public IReadOnlyList<string> RisksRedacted { get; init; } = [];

    public IReadOnlyList<string> AuditTriggersRedacted { get; init; } = [];

    public required string DecisionRecordRedacted { get; init; }

    public required bool DraftOnly { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool CanCallPlanner { get; init; }

    public required bool CanCallLlm { get; init; }

    public required bool CanAccessFilesystem { get; init; }

    public required bool CanCallCloud { get; init; }
}
