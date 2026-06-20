namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentStorageSafetyRuleKind
{
    NoSensitiveMaterial,
    NoContentPayload,
    NoUnredactedBroadPaths,
    NoCloudSync,
    NoProviderSync,
    NoRuntimeAccess,
    NoAutomaticMigration,
    NoImplicitConsentInheritance,
    NoCrossCapabilityInheritance,
    ContentAccessDoesNotImplyRepresentationOrLlmContext,
    RevokedStaleMissingConsentFailsClosed
}

public sealed record NodalOsDisabledConsentStorageContract
{
    public required string StorageContractId { get; init; }
    public required string DesignReviewRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool DisabledByDefault { get; init; }
    public required bool IsContractOnly { get; init; }
    public required bool UsesProductivePersistence { get; init; }
    public required bool PersistsConsent { get; init; }
    public required bool ReadsProductiveConsent { get; init; }
    public required bool WritesProductiveConsent { get; init; }
    public required bool DeletesProductiveConsent { get; init; }
    public required bool MigratesConsent { get; init; }
    public required bool SyncsConsentToCloud { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
    public IReadOnlyList<NodalOsConsentStorageRecordDraft> RecordDrafts { get; init; } = [];
    public IReadOnlyList<NodalOsConsentStorageSafetyRule> SafetyRules { get; init; } = [];
    public required NodalOsConsentStorageReadiness Readiness { get; init; }
}

public sealed record NodalOsConsentStorageRecordDraft
{
    public required string RecordDraftId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required string ScopeRef { get; init; }
    public required NodalOsConsentScopeStatus ConsentStatus { get; init; }
    public required NodalOsConsentScopeStatus FreshnessStatus { get; init; }
    public required NodalOsConsentScopeStatus RevocationStatus { get; init; }
    public required string PurposeRedacted { get; init; }
    public required string RiskSummaryRedacted { get; init; }
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];
    public IReadOnlyList<string> TimelineRefs { get; init; } = [];
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool IsDraftOnly { get; init; }
    public required bool IsAuthoritative { get; init; }
    public required bool ContainsRawSecret { get; init; }
    public required bool ContainsRawFileContent { get; init; }
    public required bool ContainsUnredactedPath { get; init; }
    public required bool CanAuthorizeRealUse { get; init; }
}

public sealed record NodalOsConsentStorageSafetyRule
{
    public required string RuleId { get; init; }
    public required NodalOsConsentStorageSafetyRuleKind Kind { get; init; }
    public required bool Required { get; init; }
    public required bool BlocksProductiveUseIfMissing { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsConsentStorageReadiness
{
    public required string ReadinessId { get; init; }
    public required bool ReadyForStorageDesignReview { get; init; }
    public required bool ReadyForProductivePersistence { get; init; }
    public required bool ReadyForConsentEnforcement { get; init; }
    public required bool ReadyForCapabilityAuthorization { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public IReadOnlyList<string> RequiredBeforeProductiveStorageRedacted { get; init; } = [];
}
