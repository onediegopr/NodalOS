namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsMissionControlPanelKind
{
    MissionControl,
    Timeline,
    Approvals,
    Evidence,
    LogsObservability,
    SettingsDisabled
}

public enum NodalOsMissionControlAttentionKind
{
    None,
    ApprovalRequired,
    EvidenceWarning,
    HumanHandoffRequired,
    RuntimeBlocked,
    FailureWarning
}

public sealed record NodalOsMissionControlNavigationItem
{
    public required NodalOsMissionControlPanelKind PanelKind { get; init; }

    public required string LabelRedacted { get; init; }

    public required bool Disabled { get; init; }

    public string? DisabledReasonRedacted { get; init; }
}

public sealed record NodalOsMissionControlTopBar
{
    public required string MissionTitleRedacted { get; init; }

    public required string OverallStatusRedacted { get; init; }

    public required int ProgressPercent { get; init; }

    public required bool ReadOnlyUi { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool BrowserAutomationAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }
}

public sealed record NodalOsMissionControlWorkspace
{
    public required string ActiveMissionRedacted { get; init; }

    public required string SummaryRedacted { get; init; }

    public required int ProgressPercent { get; init; }

    public IReadOnlyList<string> NextStepsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailBadgesRedacted { get; init; } = [];

    public required bool ReadOnlyUi { get; init; }
}

public sealed record NodalOsApprovalDisplayActionOption
{
    public required NodalOsApprovalUserOptionKind OptionKind { get; init; }

    public required string LabelRedacted { get; init; }

    public required bool Disabled { get; init; }

    public required string DisabledReasonRedacted { get; init; }

    public required bool CanAuthorizeExecution { get; init; }
}

public sealed record NodalOsApprovalDisplayView
{
    public required string ViewId { get; init; }

    public IReadOnlyList<NodalOsApprovalCardPreview> Cards { get; init; } = [];

    public IReadOnlyList<NodalOsApprovalDisplayActionOption> ActionOptions { get; init; } = [];

    public required bool ReadOnlyUi { get; init; }

    public required bool CanAuthorizeExecution { get; init; }
}

public sealed record NodalOsTimelineDisplayView
{
    public required string ViewId { get; init; }

    public IReadOnlyList<NodalOsTimelineEntry> Entries { get; init; } = [];

    public required bool ReadOnlyUi { get; init; }
}

public sealed record NodalOsEvidenceRefDisplayItem
{
    public required string EvidenceId { get; init; }

    public required string KindRedacted { get; init; }

    public required string RefRedacted { get; init; }

    public string? SourceEventId { get; init; }

    public IReadOnlyDictionary<string, string> SafeMetadataRedacted { get; init; } = new Dictionary<string, string>();

    public required bool RefOnly { get; init; }

    public required bool RawPayloadInline { get; init; }
}

public sealed record NodalOsEvidenceDisplayView
{
    public required string ViewId { get; init; }

    public IReadOnlyList<NodalOsEvidenceRefDisplayItem> EvidenceRefs { get; init; } = [];

    public required bool RefOnly { get; init; }

    public required bool ReadOnlyUi { get; init; }
}

public sealed record NodalOsObservabilityLogPreview
{
    public required string PreviewId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string RuntimeSummaryRedacted { get; init; }

    public IReadOnlyList<string> LinesRedacted { get; init; } = [];

    public required bool CopyReportAvailable { get; init; }

    public required string CopyReportDisabledReasonRedacted { get; init; }

    public required bool ReadOnlyUi { get; init; }
}

public sealed record NodalOsMissionControlShellPreview
{
    public required string ShellId { get; init; }

    public required string ProjectOperationalName { get; init; }

    public required NodalOsMissionControlTopBar TopBar { get; init; }

    public IReadOnlyList<NodalOsMissionControlNavigationItem> Navigation { get; init; } = [];

    public required NodalOsMissionControlWorkspace Workspace { get; init; }

    public required NodalOsApprovalDisplayView ApprovalDisplay { get; init; }

    public required NodalOsTimelineDisplayView Timeline { get; init; }

    public required NodalOsEvidenceDisplayView Evidence { get; init; }

    public required NodalOsObservabilityLogPreview Observability { get; init; }

    public IReadOnlyList<string> GuardrailsSummaryRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsMissionControlAttentionKind> AttentionFlags { get; init; } = [];

    public required bool ReadOnlyUi { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool BrowserAutomationAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
