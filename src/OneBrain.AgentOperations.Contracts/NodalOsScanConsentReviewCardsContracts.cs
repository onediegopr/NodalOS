namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsScanConsentReviewStatus
{
    Draft,
    NeedsReview,
    BlockedByMissingPathJail,
    BlockedByMissingSecretDetection,
    BlockedByMissingExclusionPolicy,
    BlockedByRealScanAuditGate,
    AcknowledgedDraftOnly,
    RejectedDraftOnly
}

public enum NodalOsScanConsentReviewOption
{
    AcknowledgeDraftOnly,
    RejectDraftOnly,
    RequestNarrowerScope,
    RequestMoreExplanation,
    MarkNeedsReview,
    ExportReviewSummary
}

public sealed record NodalOsScanConsentReviewCard
{
    public required string CardId { get; init; }

    public required string ConsentRequestRef { get; init; }

    public required string ScopePreviewRef { get; init; }

    public required string RiskLevelRedacted { get; init; }

    public required string PurposeRedacted { get; init; }

    public required string RequestedCapabilityRedacted { get; init; }

    public required string DataExposureExplanationRedacted { get; init; }

    public required bool NoMutationGuarantee { get; init; }

    public required string LlmDisabledDisclosureRedacted { get; init; }

    public required string CloudDisabledDisclosureRedacted { get; init; }

    public required string FilesystemDisabledDisclosureRedacted { get; init; }

    public required NodalOsScanConsentReviewStatus ReviewStatus { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool CanAuthorizeRealScan { get; init; }

    public required bool CanAuthorizeFileRead { get; init; }

    public required bool CanAuthorizeIndexing { get; init; }

    public required bool CanAuthorizeVectorization { get; init; }

    public required bool CanAuthorizeLlmContext { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}

public sealed record NodalOsScanConsentReviewResult
{
    public required string ReviewResultId { get; init; }

    public required string CardRef { get; init; }

    public required NodalOsScanConsentReviewOption SelectedOption { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool MutatesState { get; init; }

    public required bool AuthorizesRealScan { get; init; }

    public required bool AuthorizesDirectoryListing { get; init; }

    public required bool AuthorizesFileRead { get; init; }

    public required bool AuthorizesFileHash { get; init; }

    public required bool AuthorizesIndexing { get; init; }

    public required bool AuthorizesVectorization { get; init; }

    public required bool AuthorizesLlmContext { get; init; }

    public required bool AuthorizesCloud { get; init; }

    public required bool RequiresFutureExplicitGate { get; init; }

    public required string UserFacingExplanationRedacted { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}
