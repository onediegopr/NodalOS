namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsFailClosedAcceptanceStatus
{
    ContractOnlyPass,
    ConditionalPass,
    Fail
}

public enum NodalOsFailClosedCriterionKind
{
    GatesDisabledByDefault,
    UiCannotEnableGates,
    ReviewCannotAuthorizeCapability,
    LedgerMockOnly,
    MissingConsentFailsClosed,
    MissingAuditFailsClosed,
    MissingDependencyFailsClosed,
    MissingRedactionPolicyFailsClosed,
    MissingSecretPolicyFailsClosed,
    MissingExclusionPolicyFailsClosed,
    FilesystemAccessBlocked,
    LlmContextBlocked,
    CloudProviderRuntimeBlocked
}

public sealed record NodalOsFailClosedAcceptancePack
{
    public required string AcceptancePackId { get; init; }
    public required string CapabilityGateUiReviewRef { get; init; }
    public required string ConsentScopeLedgerMockRef { get; init; }
    public required string FailureModeMatrixRef { get; init; }
    public required string SyntheticPolicyRegressionRef { get; init; }
    public required string OperationalAccessAuditAdrRef { get; init; }
    public required NodalOsFailClosedAcceptanceStatus AcceptanceStatus { get; init; }
    public IReadOnlyList<string> FindingsRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredFixesRedacted { get; init; } = [];
    public IReadOnlyList<string> NextGateRequirementsRedacted { get; init; } = [];
    public IReadOnlyList<NodalOsFailClosedAcceptanceCriterion> AcceptanceCriteria { get; init; } = [];
    public required NodalOsFailClosedDecision Decision { get; init; }
}

public sealed record NodalOsFailClosedAcceptanceCriterion
{
    public required string CriterionId { get; init; }
    public required NodalOsFailClosedCriterionKind Kind { get; init; }
    public required bool Required { get; init; }
    public required bool Satisfied { get; init; }
    public required bool FailClosedExpected { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsFailClosedDecision
{
    public required string DecisionId { get; init; }
    public required bool FailClosedLayerReady { get; init; }
    public required bool ReadyForRealCapabilityEnablement { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForRealPathJail { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForRepresentationBuild { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public required bool ReadyForCloud { get; init; }
    public required bool ReadyForRuntime { get; init; }
    public IReadOnlyList<string> RequiredBeforeRealUseRedacted { get; init; } = [];
}
