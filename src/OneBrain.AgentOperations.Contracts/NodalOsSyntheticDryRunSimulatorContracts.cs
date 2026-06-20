namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsSyntheticDryRunSimulatorContract
{
    public required string SimulatorId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required string FixtureMatrixRef { get; init; }
    public required string PathJailPrototypeRef { get; init; }
    public required string SecretDetectionPolicyPreviewRef { get; init; }
    public required string ExclusionPolicyPackRef { get; init; }
    public required string ScanDryRunContractRef { get; init; }
    public required bool UsesSyntheticFixturesOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsRealScan { get; init; }
    public required bool PerformsDirectoryListing { get; init; }
    public required bool PerformsFileRead { get; init; }
    public required bool PerformsFileHash { get; init; }
    public required bool PerformsSecretDetectionOnRealData { get; init; }
    public required bool AppliesExclusionsToRealFilesystem { get; init; }
    public required bool PerformsIndexing { get; init; }
    public required bool PerformsVectorization { get; init; }
    public required bool BuildsLlmContext { get; init; }
    public required bool CallsProvider { get; init; }
    public required bool UsesCloud { get; init; }
    public required bool IsSimulationOnly { get; init; }
}

public sealed record NodalOsSyntheticSimulationInput
{
    public required string InputId { get; init; }
    public required string FixtureRef { get; init; }
    public required string DeclaredSyntheticPathRef { get; init; }
    public required NodalOsScanFixtureCategory DeclaredCategory { get; init; }
    public required NodalOsScanFixtureExpectedOutcome ExpectedOutcome { get; init; }
    public required string SyntheticMetadataOnlyRedacted { get; init; }
    public required bool ContainsRawFileContent { get; init; }
    public required bool ContainsRawSecret { get; init; }
}

public sealed record NodalOsSyntheticPolicyDecision
{
    public required string DecisionId { get; init; }
    public required string FixtureRef { get; init; }
    public required NodalOsScanFixtureExpectedDisposition SimulatedDisposition { get; init; }
    public required bool RequiresReview { get; init; }
    public required bool RequiresRedaction { get; init; }
    public required bool RequiresAudit { get; init; }
    public required string ExplanationRedacted { get; init; }
}

public sealed record NodalOsSyntheticDryRunSimulationResult
{
    public required string ResultId { get; init; }
    public required string SimulatorRef { get; init; }
    public required int IncludedPreviewCount { get; init; }
    public required int ExcludedPreviewCount { get; init; }
    public required int BlockedPreviewCount { get; init; }
    public required int RequiresReviewCount { get; init; }
    public required int RedactedPreviewCount { get; init; }
    public required int AuditRequiredCount { get; init; }
    public IReadOnlyList<NodalOsSyntheticPolicyDecision> PolicyDecisions { get; init; } = [];
    public required string UserFacingSummaryRedacted { get; init; }
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];
    public IReadOnlyList<string> TimelineRefs { get; init; } = [];
    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}

public sealed record NodalOsSyntheticDryRunSimulatorReadiness
{
    public required string ReadinessId { get; init; }
    public required string SimulatorRef { get; init; }
    public required bool ReadyForSyntheticSimulation { get; init; }
    public required bool ReadyForRealDryRun { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealSecretDetection { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForVectorization { get; init; }
    public required bool ReadyForLlmContext { get; init; }
}
