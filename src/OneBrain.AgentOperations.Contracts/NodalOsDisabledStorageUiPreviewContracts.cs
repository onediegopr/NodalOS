namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsDisabledStorageUiReviewOptionKind
{
    AcknowledgeDisabledStorage,
    RequestExplanation,
    MarkBoundaryIssue,
    MarkNeedsAudit,
    ExportStoragePreview
}

public sealed record NodalOsDisabledStorageUiPreview
{
    public required string PreviewId { get; init; }
    public required string StorageContractRef { get; init; }
    public required string BoundaryTestPackRef { get; init; }
    public required string ProductiveConsentStorageAdrRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsStaticPreview { get; init; }
    public required bool IsReadOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool DisabledByDefault { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool ReadsProductiveStorage { get; init; }
    public required bool WritesProductiveStorage { get; init; }
    public required bool DeletesProductiveStorage { get; init; }
    public required bool CanEnableStorage { get; init; }
    public required bool CanPersistConsent { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public IReadOnlyList<string> UiSectionsRedacted { get; init; } = [];
    public IReadOnlyList<NodalOsDisabledStorageUiReviewOption> ReviewOptions { get; init; } = [];
    public IReadOnlyList<string> DisclosuresRedacted { get; init; } = [];
}

public sealed record NodalOsDisabledStorageUiReviewOption
{
    public required string OptionId { get; init; }
    public required NodalOsDisabledStorageUiReviewOptionKind Kind { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool CanAuthorize { get; init; }
    public required bool CanPersist { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}
