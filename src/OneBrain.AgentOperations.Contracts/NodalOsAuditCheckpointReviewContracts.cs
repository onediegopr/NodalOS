namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsAuditCheckpointReviewStatus
{
    CheckpointComplete,
    Incomplete,
    Failed
}

public enum NodalOsAuditCheckpointScopeItem
{
    ProjectUnderstandingPreconditions,
    RealScanAuditGate,
    SecretDetectionPolicyPreview,
    ExclusionPolicyPack,
    DryRunContract,
    DryRunReview,
    ImplementationBoundaryAdr,
    PathJailPrototypeContract,
    ScanFixtureMatrix,
    SyntheticSimulator,
    RealScanReadinessAdr,
    DisabledPathJailGate,
    OperationalAccessAuditAdr,
    PerCapabilityGates,
    ConsentLedgerMock,
    RealAccessBlockerCloseout
}

public sealed record NodalOsAuditCheckpointReview
{
    public required string CheckpointId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public IReadOnlyList<string> CoveredMilestones { get; init; } = [];
    public IReadOnlyList<string> CoveredDecisionRefs { get; init; } = [];
    public IReadOnlyList<string> CoveredArtifactRefs { get; init; } = [];
    public required string GovernanceBaselineRef { get; init; }
    public required string RealAccessBlockerCloseoutRef { get; init; }
    public required NodalOsAuditCheckpointReviewStatus ReviewStatus { get; init; }
    public required bool IsCheckpointOnly { get; init; }
    public required bool CanAuthorizeImplementation { get; init; }
    public required bool CanEnableRealAccess { get; init; }
    public required bool CanAccessFilesystem { get; init; }
    public required bool CanBuildLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public required bool CanTriggerRuntime { get; init; }
    public IReadOnlyList<NodalOsAuditCheckpointScopeItem> CoveredScope { get; init; } = [];
    public required NodalOsAuditCheckpointFindings Findings { get; init; }
    public required NodalOsAuditCheckpointDecision Decision { get; init; }
}

public sealed record NodalOsAuditCheckpointFindings
{
    public IReadOnlyList<string> StrengthsRedacted { get; init; } = [];
    public IReadOnlyList<string> UnresolvedBlockersRedacted { get; init; } = [];
    public IReadOnlyList<string> RisksRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredNextDecisionsRedacted { get; init; } = [];
    public IReadOnlyList<string> AuditGapsRedacted { get; init; } = [];
    public IReadOnlyList<string> SafetyGapsRedacted { get; init; } = [];
    public IReadOnlyList<string> ImplementationGapsRedacted { get; init; } = [];
    public IReadOnlyList<string> NamingDebtNotesRedacted { get; init; } = [];
}

public sealed record NodalOsAuditCheckpointDecision
{
    public required string DecisionId { get; init; }
    public required bool GovernanceBaselineReady { get; init; }
    public required bool ReadyForDirectRealImplementation { get; init; }
    public required bool ReadyForProductiveConsentImplementation { get; init; }
    public required bool ReadyForRealPathJailImplementation { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public required string RecommendedNextPhaseRedacted { get; init; }
}
