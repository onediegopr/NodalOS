namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsDisabledPathJailUiReviewOption
{
    AcknowledgeDisabledState,
    RequestExplanation,
    MarkNeedsReview,
    ExportPreviewSummary
}

public sealed record NodalOsDisabledPathJailUiPreview
{
    public required string PreviewId { get; init; }
    public required string GateRef { get; init; }
    public required string CanonicalizationMatrixRef { get; init; }
    public required string NoMutationProofRef { get; init; }
    public required string RealScanReadinessAdrRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsStaticPreview { get; init; }
    public required bool IsReadOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool DisabledByDefault { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsRealCanonicalization { get; init; }
    public required bool PerformsDirectoryListing { get; init; }
    public required bool PerformsFileRead { get; init; }
    public required bool PerformsFileHash { get; init; }
    public required bool CanEnablePrototype { get; init; }
    public required bool CanAuthorizeRealScan { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required NodalOsDisabledPathJailUiSections Sections { get; init; }
    public IReadOnlyList<string> DisclosuresRedacted { get; init; } = [];
}

public sealed record NodalOsDisabledPathJailUiSections
{
    public required string GateStatusRedacted { get; init; }
    public required string WhyDisabledRedacted { get; init; }
    public IReadOnlyList<string> EnablementRequirementsRedacted { get; init; } = [];
    public required string CanonicalizationCoverageRedacted { get; init; }
    public required string NoMutationSummaryRedacted { get; init; }
    public IReadOnlyList<string> BlockedCapabilitiesRedacted { get; init; } = [];
    public IReadOnlyList<string> RiskExplanationsRedacted { get; init; } = [];
    public IReadOnlyList<string> NextRequiredGatesRedacted { get; init; } = [];
    public IReadOnlyList<string> AuditRequirementsRedacted { get; init; } = [];
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsDisabledPathJailUiReviewResult
{
    public required string ResultId { get; init; }
    public required NodalOsDisabledPathJailUiReviewOption Option { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool MutatesState { get; init; }
    public required bool EnablesPrototype { get; init; }
    public required bool AuthorizesRealScan { get; init; }
    public required bool AuthorizesFilesystemAccess { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

