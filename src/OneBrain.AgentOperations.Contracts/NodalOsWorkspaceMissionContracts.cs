namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsWorkspaceStorageMockStatus
{
    Empty,
    DraftStored,
    ActiveReadOnlyStored,
    ArchivedMock,
    InvalidRejected
}

public enum NodalOsWorkspaceMissionBindingStatus
{
    DraftBinding,
    PendingWorkspaceValidation,
    BoundReadOnly,
    Blocked,
    Archived
}

public enum NodalOsWorkspaceSwitcherItemStatus
{
    AvailableReadOnly,
    ActiveReadOnly,
    Archived,
    Blocked
}

public enum NodalOsWorkspaceSwitcherOptionKind
{
    SelectWorkspace,
    PreviewWorkspace,
    ArchiveWorkspaceMock,
    RequestExplanation,
    OpenGuardrails,
    NewWorkspaceDraft,
    ImportProjectWizardMock
}

public sealed record NodalOsWorkspaceStorageMockSummary
{
    public required string StorageId { get; init; }

    public required NodalOsWorkspaceStorageMockStatus Status { get; init; }

    public required int WorkspaceCount { get; init; }

    public required int ArchivedCount { get; init; }

    public required int InvalidRejectedCount { get; init; }

    public required bool InMemoryOnly { get; init; }

    public required bool FixtureSafe { get; init; }

    public required bool ProductivePersistenceAllowed { get; init; }

    public required bool RealFilesystemTouched { get; init; }

    public required bool DatabaseUsed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool SensitiveValuesStored { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsWorkspaceStorageMockResult
{
    public required bool Accepted { get; init; }

    public required NodalOsWorkspaceStorageMockStatus Status { get; init; }

    public NodalOsWorkspaceLocalModel? Workspace { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public required NodalOsWorkspaceStorageMockSummary Summary { get; init; }
}

public sealed record NodalOsWorkspaceMissionBinding
{
    public required string BindingId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string MissionId { get; init; }

    public required NodalOsWorkspaceMissionBindingStatus Status { get; init; }

    public required string MissionTitleRedacted { get; init; }

    public required string MissionSummaryRedacted { get; init; }

    public IReadOnlyList<string> ActiveTimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ActiveApprovalRefs { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> ActiveEvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> ObservabilityReportRefs { get; init; } = [];

    public IReadOnlyList<string> UiStateRefs { get; init; } = [];

    public string? PathJailBindingId { get; init; }

    public IReadOnlyList<string> AllowedCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailSummaryRedacted { get; init; } = [];

    public required bool ReadOnlyPreview { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public required bool TaskGraphCreated { get; init; }

    public required bool TouchesFilesystem { get; init; }

    public required bool MutatesExecutionRegistryRuntime { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsWorkspaceSwitcherListItem
{
    public required string WorkspaceId { get; init; }

    public required string DisplayNameRedacted { get; init; }

    public required NodalOsWorkspaceSwitcherItemStatus Status { get; init; }

    public required string HealthSummaryRedacted { get; init; }

    public required string PrivacyBadgeRedacted { get; init; }

    public required string PathJailStatusRedacted { get; init; }

    public DateTimeOffset? LastOpenedAtMock { get; init; }

    public required int ActiveMissionCount { get; init; }

    public required int PendingApprovalCount { get; init; }

    public required int EvidenceCount { get; init; }

    public IReadOnlyList<string> DisabledCapabilitiesSummaryRedacted { get; init; } = [];

    public required bool IsActive { get; init; }

    public required bool IsArchived { get; init; }

    public required bool IsBlocked { get; init; }

    public required bool ReadOnlyPreview { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool CanAuthorizeExecution { get; init; }
}

public sealed record NodalOsWorkspaceSwitchIntent
{
    public required string IntentId { get; init; }

    public required string WorkspaceId { get; init; }

    public required NodalOsWorkspaceSwitcherOptionKind OptionKind { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool FilesystemAccessAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsWorkspaceSwitchResultPreview
{
    public required string ResultId { get; init; }

    public required string SelectedWorkspaceId { get; init; }

    public required string ActiveWorkspaceId { get; init; }

    public required bool PreviewOnly { get; init; }

    public required bool MockOnly { get; init; }

    public required bool StateChangedProductively { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required NodalOsWorkspaceSwitcherListItem SelectedItem { get; init; }

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsWorkspaceSwitcherContract
{
    public required string SwitcherId { get; init; }

    public required string ActiveWorkspaceId { get; init; }

    public IReadOnlyList<NodalOsWorkspaceSwitcherListItem> Items { get; init; } = [];

    public IReadOnlyList<NodalOsWorkspaceSwitcherOptionKind> UserOptions { get; init; } = [];

    public required NodalOsWorkspaceSwitchIntent SwitchIntent { get; init; }

    public required NodalOsWorkspaceSwitchResultPreview SwitchResultPreview { get; init; }

    public required NodalOsWorkspaceStorageMockSummary StorageSummary { get; init; }

    public required bool ReadOnlyPreview { get; init; }

    public required bool MockOnly { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool FilesystemAccessAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
