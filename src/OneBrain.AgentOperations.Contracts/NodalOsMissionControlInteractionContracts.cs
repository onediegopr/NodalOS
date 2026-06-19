namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsMissionControlUiIntentKind
{
    SelectTimelineEntry,
    SelectApprovalCard,
    SelectEvidenceRef,
    ExpandTimelineEntry,
    CollapseTimelineEntry,
    SwitchNavigationSection,
    OpenObservabilityLogPreview,
    RequestExplanation,
    RequestChanges,
    DeferApproval,
    CopyTechnicalLogIntent,
    AcknowledgeWarning,
    OpenGuardrailsSummary
}

public enum NodalOsMissionControlUiSurfaceKind
{
    Shell,
    TopBar,
    Sidebar,
    Workspace,
    ApprovalDisplay,
    TimelineView,
    EvidenceView,
    ObservabilityLogPreview,
    GuardrailsSummary
}

public enum NodalOsApprovalDecisionDraftStatus
{
    DraftCreated,
    DraftUpdated,
    DraftValidated,
    DraftDiscarded,
    DraftReadyForReview,
    NotSubmitted,
    NoOpSubmitted
}

public sealed record NodalOsMissionControlUiIntent
{
    public required string IntentId { get; init; }

    public required NodalOsMissionControlUiIntentKind IntentKind { get; init; }

    public required NodalOsMissionControlUiSurfaceKind SourceSurface { get; init; }

    public string? ActorRedacted { get; init; }

    public string? MissionId { get; init; }

    public string? TimelineEntryId { get; init; }

    public string? ApprovalCardId { get; init; }

    public string? EvidenceId { get; init; }

    public string? ObservabilityReportId { get; init; }

    public string? NoteRedacted { get; init; }

    public IReadOnlyDictionary<string, string> MetadataRedacted { get; init; } = new Dictionary<string, string>();

    public required bool IsNoOp { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresPositiveExecutionGate { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsMissionControlNoOpEvent
{
    public required string UiEventId { get; init; }

    public required string IntentId { get; init; }

    public required NodalOsMissionControlUiIntentKind IntentKind { get; init; }

    public required NodalOsMissionControlUiSurfaceKind SourceSurface { get; init; }

    public string? MissionId { get; init; }

    public string? TimelineEntryId { get; init; }

    public string? ApprovalCardId { get; init; }

    public string? EvidenceId { get; init; }

    public required string SummaryRedacted { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresPositiveExecutionGate { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsApprovalDecisionDraft
{
    public required string DraftId { get; init; }

    public required string ApprovalCardId { get; init; }

    public required NodalOsApprovalDecisionKind SelectedDecision { get; init; }

    public required NodalOsApprovalDecisionDraftStatus Status { get; init; }

    public string? UserNoteRedacted { get; init; }

    public string? ReasonRedacted { get; init; }

    public string? RequestedChangesRedacted { get; init; }

    public string? RequestExplanationRedacted { get; init; }

    public string? DeferReasonRedacted { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineEntryIds { get; init; } = [];

    public required bool IsNoOp { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresPositiveExecutionGateForFutureExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsMissionControlUiState
{
    public required string StateId { get; init; }

    public required NodalOsMissionControlPanelKind ActiveNavigationSection { get; init; }

    public string? SelectedMissionId { get; init; }

    public string? SelectedTimelineEntryId { get; init; }

    public string? SelectedApprovalCardId { get; init; }

    public string? SelectedEvidenceId { get; init; }

    public IReadOnlyList<string> ExpandedTimelineEntryIds { get; init; } = [];

    public IReadOnlyList<string> DismissedWarningIds { get; init; } = [];

    public IReadOnlyDictionary<string, string> ActiveFiltersRedacted { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, bool> PanelCollapsed { get; init; } = new Dictionary<string, bool>();

    public required bool LogPreviewOpen { get; init; }

    public required bool ReadOnlyUi { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool MockPersistenceOnly { get; init; }

    public required bool CloudPersistenceAllowed { get; init; }

    public required bool ProductiveDatabasePersistenceAllowed { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
