namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentScopeStatus
{
    Draft,
    AcknowledgedDraft,
    RejectedDraft,
    NeedsNarrowerScope,
    RevokedDraft
}

public enum NodalOsConsentScopeLedgerOperationKind
{
    AddDraftEntry,
    MarkDraftAcknowledged,
    MarkRejectedDraft,
    MarkNeedsNarrowerScope,
    MarkRevokedDraft,
    ExportLedgerPreview
}

public sealed record NodalOsConsentScopeLedgerMock
{
    public required string LedgerId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsMockOnly { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool CanPersistConsentProductively { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
    public required bool CanSendToCloud { get; init; }
    public IReadOnlyList<NodalOsConsentScopeEntry> Entries { get; init; } = [];
    public required NodalOsConsentScopeLedgerResult Result { get; init; }
}

public sealed record NodalOsConsentScopeEntry
{
    public required string EntryId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required string RequestedScopeRef { get; init; }
    public required NodalOsConsentScopeStatus ConsentStatus { get; init; }
    public required NodalOsConsentScopeStatus ScopeStatus { get; init; }
    public required NodalOsConsentScopeStatus FreshnessStatus { get; init; }
    public required NodalOsConsentScopeStatus RevocationStatus { get; init; }
    public required string UserFacingPurposeRedacted { get; init; }
    public required string RiskSummaryRedacted { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool IsMockOnly { get; init; }
    public required bool IsAuthoritative { get; init; }
    public required bool CanAuthorizeRealUse { get; init; }
}

public sealed record NodalOsConsentScopeLedgerOperationResult
{
    public required string OperationResultId { get; init; }
    public required NodalOsConsentScopeLedgerOperationKind Operation { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool IsMockOnly { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool AuthorizesCapability { get; init; }
    public required bool AuthorizesFilesystemAccess { get; init; }
    public required bool AuthorizesLlmContext { get; init; }
}

public sealed record NodalOsConsentScopeLedgerResult
{
    public required string ResultId { get; init; }
    public required bool ReadyForMockReview { get; init; }
    public required bool ReadyForProductiveConsentLedger { get; init; }
    public required bool ReadyForRealCapabilityAuthorization { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];
    public required string UserFacingExplanationRedacted { get; init; }
}
