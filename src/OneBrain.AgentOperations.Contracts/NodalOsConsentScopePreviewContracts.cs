namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentScopeCapability
{
    FutureProjectScan,
    FuturePathJailActivation,
    FutureScopePreview,
    FutureContextBuild,
    Unknown
}

public enum NodalOsConsentScopeOption
{
    ApproveDraftOnly,
    RejectDraftOnly,
    RequestNarrowerScope,
    RequestExplanation,
    MarkNeedsReview,
    ExportPreviewReport
}

public sealed record NodalOsConsentRequestDraft
{
    public required string ConsentRequestId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required NodalOsConsentScopeCapability RequestedCapability { get; init; }

    public required string RequestedScopeRedacted { get; init; }

    public required string UserFacingPurposeRedacted { get; init; }

    public IReadOnlyList<string> RisksRedacted { get; init; } = [];

    public required string DataExposureExplanationRedacted { get; init; }

    public required bool NoMutationGuarantee { get; init; }

    public required bool CloudDefault { get; init; }

    public required bool LlmDefault { get; init; }

    public required bool CanApproveRealScan { get; init; }

    public required bool IsDraftOnly { get; init; }

    public required bool IsNoOp { get; init; }
}

public sealed record NodalOsScopePreviewContract
{
    public required string ScopePreviewId { get; init; }

    public required string WorkspaceRef { get; init; }

    public IReadOnlyList<string> IncludePatternsRedacted { get; init; } = [];

    public IReadOnlyList<string> ExcludePatternsRedacted { get; init; } = [];

    public required int MaxDepth { get; init; }

    public required int MaxFiles { get; init; }

    public required long MaxBytes { get; init; }

    public required bool EstimatedOnly { get; init; }

    public required bool UsesRealFilesystem { get; init; }

    public required bool DirectoryListingPerformed { get; init; }

    public required bool FileReadPerformed { get; init; }

    public required bool FileHashPerformed { get; init; }

    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsConsentScopeResult
{
    public required string ConsentResultId { get; init; }

    public required string ConsentRequestRef { get; init; }

    public required NodalOsConsentScopeOption SelectedOption { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool MutatesState { get; init; }

    public required bool AuthorizesRealScan { get; init; }

    public required bool AuthorizesFileRead { get; init; }

    public required bool AuthorizesIndexing { get; init; }

    public required bool AuthorizesVectorization { get; init; }

    public required bool AuthorizesLlmContext { get; init; }

    public required bool RequiresFutureExplicitGate { get; init; }

    public required string UserFacingExplanationRedacted { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}
