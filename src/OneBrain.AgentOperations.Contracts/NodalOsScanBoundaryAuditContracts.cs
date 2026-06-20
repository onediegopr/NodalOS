namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsScanBoundaryAuditStatus
{
    Pass,
    ConditionalPass,
    Fail
}

public enum NodalOsScanBoundaryAuditDimension
{
    SyntheticOnlyIntegrity,
    NoRealFilesystem,
    NoDirectoryListing,
    NoFileRead,
    NoFileHash,
    NoRealSecretDetection,
    NoRealExclusionEnforcement,
    NoIndexing,
    NoVectorization,
    NoLlmContext,
    NoProviderCalls,
    NoCloud,
    NoRuntime,
    NoProductivePersistence,
    DeterministicArtifacts,
    RedactionSafety
}

public sealed record NodalOsScanBoundaryAuditFinding
{
    public required NodalOsScanBoundaryAuditDimension Dimension { get; init; }
    public required NodalOsScanBoundaryAuditStatus Status { get; init; }
    public required string FindingRedacted { get; init; }
}

public sealed record NodalOsScanBoundaryDecision
{
    public required string BoundaryDecisionId { get; init; }
    public required bool SyntheticLayerReady { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealPathJail { get; init; }
    public required bool ReadyForRealSecretDetection { get; init; }
    public required bool ReadyForRealExclusionEnforcement { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForVectorization { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public IReadOnlyList<string> RequiredBeforeRealScanRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredBeforeFilesystemAccessRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredBeforePathJailEnablementRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredBeforeLlmContextRedacted { get; init; } = [];
}

public sealed record NodalOsScanBoundaryAudit
{
    public required string AuditId { get; init; }
    public required string SimulatorContractRef { get; init; }
    public required string FixtureResultReviewRef { get; init; }
    public required string PathJailPrototypeRef { get; init; }
    public required string FixtureMatrixRef { get; init; }
    public required NodalOsScanBoundaryAuditStatus AuditStatus { get; init; }
    public IReadOnlyList<NodalOsScanBoundaryAuditFinding> Findings { get; init; } = [];
    public IReadOnlyList<string> RequiredFixesRedacted { get; init; } = [];
    public IReadOnlyList<string> NextGateRequirementsRedacted { get; init; } = [];
    public required NodalOsScanBoundaryDecision BoundaryDecision { get; init; }
}
