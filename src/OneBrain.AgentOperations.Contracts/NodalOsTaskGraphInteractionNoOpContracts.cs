namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsTaskGraphInteractionKind
{
    SelectWorkItem,
    ExpandWorkItem,
    CollapseWorkItem,
    FilterByStatus,
    FilterByRisk,
    FilterByBlocker,
    SortVisualOrder,
    RequestExplanation,
    MarkNeedsReview,
    AddDraftNote,
    AskToReviseDraft,
    CompareWorkItems,
    ShowEvidenceRefs,
    ShowTimelineRefs,
    ShowGuardrails,
    CopyTechnicalReportPreview
}

public sealed record NodalOsTaskGraphInteractionNoOpRequest
{
    public required string InteractionId { get; init; }

    public required NodalOsTaskGraphInteractionKind InteractionKind { get; init; }

    public required string AssignmentUiPreviewId { get; init; }

    public string? WorkItemId { get; init; }

    public string? DraftNoteRedacted { get; init; }

    public IReadOnlyList<string> SelectedRefsRedacted { get; init; } = [];
}

public sealed record NodalOsTaskGraphInteractionNoOpResult
{
    public required string InteractionId { get; init; }

    public required NodalOsTaskGraphInteractionKind InteractionKind { get; init; }

    public required string AssignmentUiPreviewId { get; init; }

    public string? WorkItemId { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool MutatesState { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool PlannerExecutionAllowed { get; init; }

    public required bool LlmCallAllowed { get; init; }

    public required bool FilesystemAccessAllowed { get; init; }

    public required bool NetworkAccessAllowed { get; init; }

    public required bool CreatesExecutionRequest { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool UsesClipboard { get; init; }

    public required bool RequiresFutureImplementation { get; init; }

    public required string UserFacingExplanationRedacted { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];
}
