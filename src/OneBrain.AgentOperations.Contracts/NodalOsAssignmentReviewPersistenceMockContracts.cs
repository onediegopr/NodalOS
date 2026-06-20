namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsAssignmentReviewSortMode
{
    OriginalDraftOrder,
    RiskThenBlocked,
    BlockedFirst,
    ManualVisualOnly
}

public sealed record NodalOsAssignmentReviewSession
{
    public required string ReviewSessionId { get; init; }

    public required string AssignmentIdRef { get; init; }

    public required string MissionIdRef { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public required bool DraftOnly { get; init; }

    public required bool IsAuthoritative { get; init; }

    public required bool MutatesRuntimeState { get; init; }

    public required bool CanAuthorizeExecution { get; init; }
}

public sealed record NodalOsAssignmentReviewState
{
    public required string SelectedWorkItemId { get; init; }

    public IReadOnlyList<string> ExpandedWorkItemIds { get; init; } = [];

    public IReadOnlyList<string> CollapsedWorkItemIds { get; init; } = [];

    public IReadOnlyList<string> FiltersRedacted { get; init; } = [];

    public required NodalOsAssignmentReviewSortMode SortMode { get; init; }

    public IReadOnlyDictionary<string, string> DraftNotesRedacted { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> NeedsReviewWorkItemIds { get; init; } = [];

    public IReadOnlyList<string> ExplanationRequestWorkItemIds { get; init; } = [];

    public IReadOnlyList<string> ComparedWorkItemIds { get; init; } = [];

    public IReadOnlyList<string> VisibleEvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> VisibleTimelineRefs { get; init; } = [];

    public IReadOnlyList<string> VisibleContextRefsRedacted { get; init; } = [];
}

public sealed record NodalOsAssignmentReviewSnapshot
{
    public required NodalOsAssignmentReviewSession Session { get; init; }

    public required NodalOsAssignmentReviewState State { get; init; }

    public required bool MockStorageOnly { get; init; }

    public required bool ProductivePersistenceUsed { get; init; }

    public required bool FilesystemUsed { get; init; }

    public required bool CloudUsed { get; init; }

    public required bool BrowserStorageUsed { get; init; }

    public required bool ClipboardUsed { get; init; }

    public required bool CreatesExecutionRequest { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool RestoredInteractionsRemainNoOp { get; init; }
}

public sealed record NodalOsAssignmentReviewRehydrationResult
{
    public required string ReviewSessionId { get; init; }

    public required NodalOsAssignmentReviewState RestoredState { get; init; }

    public required bool DraftOnly { get; init; }

    public required bool IsAuthoritative { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool CreatesExecutionRequest { get; init; }

    public required bool NotesCanBecomePrompts { get; init; }

    public required bool RestoredInteractionsRemainNoOp { get; init; }
}
