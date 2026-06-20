namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsCapabilityGateReviewOption
{
    AcknowledgeDisabledGate,
    RequestExplanation,
    MarkNeedsAudit,
    RequestNarrowerScope,
    ExportReviewSummary
}

public enum NodalOsCapabilityGateReviewStatus
{
    DraftReview,
    NeedsAudit,
    ScopeReviewRequested,
    AcknowledgedDisabled
}

public sealed record NodalOsCapabilityGateUiReview
{
    public required string ReviewId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public IReadOnlyList<string> CapabilityGateRefs { get; init; } = [];
    public required string FailureModeMatrixRef { get; init; }
    public required string ConsentEnforcementPreviewRef { get; init; }
    public required string OperationalAccessAuditAdrRef { get; init; }
    public required bool IsStaticPreview { get; init; }
    public required bool IsReadOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool CanEnableGate { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanPersistConsent { get; init; }
    public required bool CanAccessFilesystem { get; init; }
    public required bool CanReadContent { get; init; }
    public required bool CanFingerprintContent { get; init; }
    public required bool CanBuildRepresentation { get; init; }
    public required bool CanSendToLlm { get; init; }
    public required bool CanSendToCloud { get; init; }
    public IReadOnlyList<string> UiSectionsRedacted { get; init; } = [];
    public IReadOnlyList<NodalOsCapabilityReviewCard> ReviewCards { get; init; } = [];
    public IReadOnlyList<NodalOsCapabilityGateReviewOptionResult> ReviewOptions { get; init; } = [];
}

public sealed record NodalOsCapabilityReviewCard
{
    public required string CardId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required bool GateEnabled { get; init; }
    public required bool DisabledByDefault { get; init; }
    public required bool RequiredConsent { get; init; }
    public required bool RequiredAudit { get; init; }
    public IReadOnlyList<NodalOsOperationalCapability> RequiredDependencies { get; init; } = [];
    public required bool FailClosed { get; init; }
    public required string UserFacingRiskExplanationRedacted { get; init; }
    public required NodalOsCapabilityGateReviewStatus ReviewStatus { get; init; }
}

public sealed record NodalOsCapabilityGateReviewOptionResult
{
    public required string OptionResultId { get; init; }
    public required NodalOsCapabilityGateReviewOption Option { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool MutatesState { get; init; }
    public required bool CanEnableGate { get; init; }
    public required bool AuthorizesCapability { get; init; }
    public required bool PersistsConsent { get; init; }
}
