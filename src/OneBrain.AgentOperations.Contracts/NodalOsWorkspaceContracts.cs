namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsWorkspaceStatus
{
    Draft,
    PendingValidation,
    ValidatedReadOnly,
    ImportPreviewReady,
    ActiveReadOnly,
    Blocked,
    Archived
}

public enum NodalOsWorkspacePrivacyMode
{
    LocalOnly,
    PrivatePreview,
    RedactedExportOnly
}

public enum NodalOsPathJailOperationKind
{
    ReadMetadataFuture,
    ReadFileFuture,
    CreateTextFileFuture,
    UpdateTextFileFuture,
    ExportReportFuture
}

public enum NodalOsProjectImportWizardStepKind
{
    ChooseLocalFolder,
    PreviewWorkspaceMetadata,
    ValidatePathJail,
    ExplainPrivacyLocalFirst,
    ExplainDisabledRuntimeCloudLlm,
    ConfirmReadOnlyImportPreview,
    CreateWorkspaceDraftMock,
    ShowNextSafeSteps
}

public enum NodalOsProjectImportWizardOptionKind
{
    ContinuePreview,
    Back,
    Cancel,
    RequestExplanation,
    OpenGuardrailsSummary
}

public sealed record NodalOsWorkspaceLocalModel
{
    public required string WorkspaceId { get; init; }

    public required string DisplayNameRedacted { get; init; }

    public string? DescriptionRedacted { get; init; }

    public required string LocalRootPathRedacted { get; init; }

    public required string RootPathFingerprint { get; init; }

    public required NodalOsWorkspaceStatus Status { get; init; }

    public required NodalOsWorkspacePrivacyMode PrivacyMode { get; init; }

    public required bool ReadOnlyPreview { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public string? PathJailBindingId { get; init; }

    public IReadOnlyList<string> ActiveMissionRefs { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public string? UiStateRef { get; init; }

    public string? ImportWizardRef { get; init; }

    public IReadOnlyList<string> GuardrailSummaryRedacted { get; init; } = [];

    public IReadOnlyList<string> AllowedCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> NextSafeStepsRedacted { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsPathJailBinding
{
    public required string JailId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string RootPathRedacted { get; init; }

    public required string CanonicalRootFingerprint { get; init; }

    public IReadOnlyList<string> AllowedPathPolicyRedacted { get; init; } = [];

    public IReadOnlyList<string> DeniedPathPolicyRedacted { get; init; } = [];

    public required string SymlinkPolicyRedacted { get; init; }

    public required string CaseSensitivityNoteRedacted { get; init; }

    public IReadOnlyList<NodalOsPathJailOperationKind> AllowedOperationTypesFuture { get; init; } = [];

    public IReadOnlyList<string> DisabledOperationsRedacted { get; init; } = [];

    public required bool CanMutateFilesystem { get; init; }

    public required bool CanExecuteShell { get; init; }

    public required bool CanAccessOutsideJail { get; init; }

    public required bool RequiresPositiveExecutionGate { get; init; }

    public required bool RequiresApprovalForFutureMutations { get; init; }

    public required bool RealFilesystemAccessAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsPathJailValidationRequest
{
    public required string JailId { get; init; }

    public required string RelativePathRedacted { get; init; }
}

public sealed record NodalOsPathJailValidationResult
{
    public required bool IsValid { get; init; }

    public required bool IsInsideJail { get; init; }

    public required bool FilesystemTouched { get; init; }

    public required bool CanMutateFilesystem { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record NodalOsProjectImportWizardStep
{
    public required NodalOsProjectImportWizardStepKind StepKind { get; init; }

    public required string TitleRedacted { get; init; }

    public required string ExplanationRedacted { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool FilesystemAccessAllowed { get; init; }
}

public sealed record NodalOsProjectImportWizardContract
{
    public required string WizardId { get; init; }

    public required NodalOsProjectImportWizardStepKind CurrentStep { get; init; }

    public IReadOnlyList<NodalOsProjectImportWizardStep> Steps { get; init; } = [];

    public required string SelectedPathRedacted { get; init; }

    public required NodalOsWorkspaceLocalModel WorkspaceDraft { get; init; }

    public required NodalOsCoreRuntimeValidationResult WorkspaceValidation { get; init; }

    public required NodalOsPathJailValidationResult PathJailValidation { get; init; }

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> DisabledFutureOptionsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailExplainersRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsProjectImportWizardOptionKind> UserOptions { get; init; } = [];

    public IReadOnlyList<NodalOsMissionControlUiIntent> NoOpIntents { get; init; } = [];

    public required bool ReadOnlyPreview { get; init; }

    public required bool ProjectImportMockOnly { get; init; }

    public required bool ScansFilesystem { get; init; }

    public required bool CreatesFolders { get; init; }

    public required bool ImportsFiles { get; init; }

    public required bool ProductivePersistenceAllowed { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public required bool LlmProviderCallsAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
