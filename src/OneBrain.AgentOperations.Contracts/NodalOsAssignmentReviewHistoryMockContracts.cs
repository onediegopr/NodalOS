namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsAssignmentReviewHistoryEntry
{
    public required string HistoryEntryId { get; init; }

    public required string ReviewSessionId { get; init; }

    public required string MissionIdRef { get; init; }

    public required string AssignmentIdRef { get; init; }

    public string? HandoffIdRef { get; init; }

    public required string SnapshotLabelRedacted { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public required bool DraftOnly { get; init; }

    public required bool IsAuthoritative { get; init; }

    public required bool IsMockOnly { get; init; }

    public required bool CanRestoreAsAuthoritative { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool CanTriggerPlanner { get; init; }

    public required bool CanTriggerRuntime { get; init; }

    public required bool CanTriggerLlm { get; init; }

    public required bool CanAccessFilesystem { get; init; }
}

public sealed record NodalOsAssignmentReviewHistoryCollection
{
    public required string HistoryCollectionId { get; init; }

    public IReadOnlyList<NodalOsAssignmentReviewHistoryEntry> Entries { get; init; } = [];

    public NodalOsAssignmentReviewHistoryEntry? LatestEntry { get; init; }

    public NodalOsAssignmentReviewHistoryEntry? PreviousEntry { get; init; }

    public IReadOnlyList<string> VisibleLabelsRedacted { get; init; } = [];

    public IReadOnlyList<string> DiffCandidateRefs { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool MockStoreOnly { get; init; }

    public required bool ProductivePersistenceUsed { get; init; }

    public required bool FilesystemUsed { get; init; }

    public required bool CloudUsed { get; init; }

    public required bool BrowserStorageUsed { get; init; }

    public required bool UsageMetricsUsed { get; init; }

    public required bool ClipboardUsed { get; init; }
}

public sealed record NodalOsAssignmentReviewHistoryRestoreResult
{
    public required string HistoryEntryId { get; init; }

    public required string ReviewSessionId { get; init; }

    public required string UserFacingExplanationRedacted { get; init; }

    public required bool VisualMockOnly { get; init; }

    public required bool DraftOnly { get; init; }

    public required bool IsAuthoritative { get; init; }

    public required bool CreatesExecutionRequest { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool CallsPlanner { get; init; }

    public required bool CallsLlm { get; init; }

    public required bool CallsRuntime { get; init; }

    public required bool MutatesFilesystem { get; init; }
}
