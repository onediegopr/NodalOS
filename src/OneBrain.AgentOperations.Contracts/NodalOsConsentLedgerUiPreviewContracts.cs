namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentLedgerUiReviewOption
{
    AcknowledgeMockEntry,
    RequestNarrowerScope,
    MarkStale,
    MarkRevokedDraft,
    MarkNeedsAudit,
    ExportLedgerPreview
}

public enum NodalOsConsentLedgerEntryReviewStatus
{
    DraftReview,
    NeedsAudit,
    StaleDraft,
    RevokedDraft,
    AcknowledgedMock
}

public sealed record NodalOsConsentLedgerUiPreview
{
    public required string PreviewId { get; init; }
    public required string LedgerRef { get; init; }
    public required string CapabilityGateUiReviewRef { get; init; }
    public required string FailClosedAcceptancePackRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsStaticPreview { get; init; }
    public required bool IsReadOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool CanPersistConsent { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
    public required bool CanSendToCloud { get; init; }
    public IReadOnlyList<string> UiSectionsRedacted { get; init; } = [];
    public IReadOnlyList<NodalOsConsentLedgerEntryCard> EntryCards { get; init; } = [];
    public IReadOnlyList<NodalOsConsentLedgerUiReviewOptionResult> ReviewOptions { get; init; } = [];
}

public sealed record NodalOsConsentLedgerEntryCard
{
    public required string CardId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required NodalOsConsentScopeStatus ConsentStatus { get; init; }
    public required NodalOsConsentScopeStatus ScopeStatus { get; init; }
    public required NodalOsConsentScopeStatus FreshnessStatus { get; init; }
    public required NodalOsConsentScopeStatus RevocationStatus { get; init; }
    public required bool IsMockOnly { get; init; }
    public required bool IsAuthoritative { get; init; }
    public required bool CanAuthorizeRealUse { get; init; }
    public required string UserFacingPurposeRedacted { get; init; }
    public required string RiskSummaryRedacted { get; init; }
    public required NodalOsConsentLedgerEntryReviewStatus ReviewStatus { get; init; }
}

public sealed record NodalOsConsentLedgerUiReviewOptionResult
{
    public required string OptionResultId { get; init; }
    public required NodalOsConsentLedgerUiReviewOption Option { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool MutatesState { get; init; }
    public required bool PersistsConsent { get; init; }
    public required bool AuthorizesCapability { get; init; }
    public required bool AuthorizesFilesystemAccess { get; init; }
    public required bool AuthorizesLlmContext { get; init; }
    public required bool SendsToCloud { get; init; }
}
