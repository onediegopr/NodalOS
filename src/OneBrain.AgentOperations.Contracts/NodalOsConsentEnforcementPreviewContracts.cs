namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsConsentEnforcementMode
{
    PreviewOnly
}

public enum NodalOsConsentReviewOption
{
    AcknowledgePreview,
    RequestNarrowerScope,
    MarkMissingConsent,
    MarkNeedsAudit,
    ExportConsentPreview
}

public sealed record NodalOsConsentEnforcementPreview
{
    public required string PreviewId { get; init; }
    public required string ConsentPolicyRef { get; init; }
    public IReadOnlyList<string> CapabilityGateRefs { get; init; } = [];
    public required string FailureModeMatrixRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsPreviewOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool EnforcesConsentOnRealOperation { get; init; }
    public required bool CanAuthorizeRealCapability { get; init; }
    public required bool CanPersistConsent { get; init; }
    public required bool CanBypassConsent { get; init; }
    public IReadOnlyList<NodalOsConsentEnforcementRule> Rules { get; init; } = [];
    public required NodalOsConsentEnforcementPreviewResult Result { get; init; }
}

public sealed record NodalOsConsentEnforcementRule
{
    public required string RuleId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required bool ConsentRequired { get; init; }
    public required bool ConsentScopeRequired { get; init; }
    public required bool ConsentFreshnessRequired { get; init; }
    public required bool RevocationSupportedInFuture { get; init; }
    public required bool NarrowScopeRequiredForSensitiveCapability { get; init; }
    public required bool UserFacingExplanationRequired { get; init; }
    public required bool FailClosedIfMissing { get; init; }
}

public sealed record NodalOsConsentEnforcementPreviewResult
{
    public required string ResultId { get; init; }
    public required NodalOsConsentEnforcementMode ConsentEnforcementMode { get; init; }
    public required bool ReadyForSyntheticConsentReview { get; init; }
    public required bool ReadyForProductiveConsentEnforcement { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForRepresentationBuild { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredBeforeRealUseRedacted { get; init; } = [];
    public required string UserFacingSummaryRedacted { get; init; }
}

public sealed record NodalOsConsentReviewOptionResult
{
    public required string OptionResultId { get; init; }
    public required NodalOsConsentReviewOption Option { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool MutatesState { get; init; }
    public required bool AuthorizesRealCapability { get; init; }
    public required bool PersistsConsent { get; init; }
}

