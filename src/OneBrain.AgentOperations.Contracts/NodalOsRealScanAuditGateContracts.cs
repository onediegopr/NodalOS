namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsRealScanAuditStatus
{
    Blocked,
    ConditionalBlock,
    Failed,
    UnknownRequiresReview
}

public enum NodalOsRealScanAuditDimension
{
    PathJailPreconditionsExist,
    ConsentContractExists,
    ScopePreviewContractExists,
    RedactionPolicyExists,
    SecretDetectionPolicyExists,
    ExclusionPolicyExists,
    SymlinkPolicyExists,
    CancellationSemanticsExist,
    NoMutationGuaranteeExists,
    EvidencePlanExists,
    TimelinePlanExists,
    CloudDisabledByDefault,
    LlmContextDisabledByDefault,
    AuditBeforeEnablementRequired
}

public sealed record NodalOsRealScanAuditFinding
{
    public required NodalOsRealScanAuditDimension Dimension { get; init; }

    public required NodalOsRealScanAuditStatus Status { get; init; }

    public required string FindingRedacted { get; init; }
}

public sealed record NodalOsRealScanAuditGate
{
    public required string GateId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required string PathJailPreconditionsRef { get; init; }

    public required string ConsentScopePreviewRef { get; init; }

    public required string RedactionPolicyRef { get; init; }

    public required string SecretDetectionPolicyRef { get; init; }

    public required string EvidencePlanRef { get; init; }

    public required string TimelinePlanRef { get; init; }

    public required NodalOsRealScanAuditStatus AuditStatus { get; init; }

    public IReadOnlyList<NodalOsRealScanAuditFinding> Findings { get; init; } = [];
}

public sealed record NodalOsRealScanGateDecision
{
    public required string DecisionId { get; init; }

    public required string GateRef { get; init; }

    public required bool ReadyForRealScan { get; init; }

    public required bool ReadyForDirectoryListing { get; init; }

    public required bool ReadyForFileRead { get; init; }

    public required bool ReadyForFileHashing { get; init; }

    public required bool ReadyForIndexing { get; init; }

    public required bool ReadyForVectorization { get; init; }

    public required bool ReadyForLlmContextBuild { get; init; }

    public required bool ReadyForCloudSync { get; init; }

    public IReadOnlyList<string> RequiredBeforeRealScanRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredBeforeFileReadRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredBeforeIndexingRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredBeforeLlmContextRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredBeforeCloudSyncRedacted { get; init; } = [];

    public required string UserFacingExplanationRedacted { get; init; }
}
