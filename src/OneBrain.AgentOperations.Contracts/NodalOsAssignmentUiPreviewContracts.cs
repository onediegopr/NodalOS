namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsAssignmentUiPanelKind
{
    Header,
    WorkGraph,
    Review,
    Explanation,
    Empty
}

public enum NodalOsAssignmentUiPreviewState
{
    EmptyAssignment,
    DraftAvailable,
    BlockedByPlannerReadiness,
    BlockedByRuntimeDisabled,
    BlockedByLlmDisabled,
    BlockedByFilesystemDisabled,
    ContextNeedsReview,
    EvidenceRefsMissing,
    AllWorkItemsDraftOnly,
    UnknownRequiresReview
}

public sealed record NodalOsAssignmentUiPreviewHeader
{
    public required string MissionIdRef { get; init; }

    public required string AssignmentIdRef { get; init; }

    public required NodalOsPlannerReadinessState PlannerReadinessStatus { get; init; }

    public required string DraftOnlyDisclosureRedacted { get; init; }

    public required string RuntimeBlockedDisclosureRedacted { get; init; }

    public required string LlmBlockedDisclosureRedacted { get; init; }

    public required string FilesystemBlockedDisclosureRedacted { get; init; }
}

public sealed record NodalOsAssignmentUiWorkItemPreview
{
    public required string WorkItemId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string DraftStatusRedacted { get; init; }

    public required NodalOsAssignmentRiskLevel RiskLevel { get; init; }

    public IReadOnlyList<string> DependencyIds { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefIds { get; init; } = [];

    public IReadOnlyList<string> TimelineRefIds { get; init; } = [];

    public IReadOnlyList<string> ContextRefIdsRedacted { get; init; } = [];

    public required bool CanExecute { get; init; }

    public required bool IsAuthoritative { get; init; }
}

public sealed record NodalOsAssignmentUiReviewPanel
{
    public required string SelectedWorkItemId { get; init; }

    public required string SelectedSummaryRedacted { get; init; }

    public required string WhyItExistsRedacted { get; init; }

    public IReadOnlyList<string> InputRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> OutputRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> MissingReadinessRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsTaskGraphReviewOptionKind> UserReviewOptions { get; init; } = [];

    public required bool UserReviewOptionsAreNoOp { get; init; }
}

public sealed record NodalOsAssignmentUiExplanationPanel
{
    public required string CannotExecuteExplanationRedacted { get; init; }

    public required string FutureExecutionNeedsRedacted { get; init; }

    public required string ApprovalDoesNotUnlockRuntimeRedacted { get; init; }

    public required string PlannerNotImplementedRedacted { get; init; }
}

public sealed record NodalOsAssignmentUiPreview
{
    public required string AssignmentUiPreviewId { get; init; }

    public required NodalOsAssignmentUiPreviewState PreviewState { get; init; }

    public required NodalOsAssignmentUiPreviewHeader Header { get; init; }

    public IReadOnlyList<NodalOsAssignmentUiWorkItemPreview> WorkItems { get; init; } = [];

    public required NodalOsAssignmentUiReviewPanel ReviewPanel { get; init; }

    public required NodalOsAssignmentUiExplanationPanel ExplanationPanel { get; init; }

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ContextRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool DraftOnly { get; init; }

    public required bool ReadOnly { get; init; }

    public required bool IsAuthoritative { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool PlannerExecutionAllowed { get; init; }

    public required bool LlmCallAllowed { get; init; }

    public required bool FilesystemAccessAllowed { get; init; }

    public required bool NetworkAccessAllowed { get; init; }
}

public sealed record NodalOsAssignmentUiPreviewRender
{
    public required string RenderId { get; init; }

    public required string AssignmentUiPreviewId { get; init; }

    public required string HtmlRedacted { get; init; }

    public required bool StaticOnly { get; init; }

    public required bool ContainsScript { get; init; }

    public required bool ContainsExternalResource { get; init; }

    public required bool CallsNetwork { get; init; }
}
