namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsNoMutationProofContract
{
    public required string ProofId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required string PathJailGateRef { get; init; }
    public required string CanonicalizationMatrixRef { get; init; }
    public required bool IsContractOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsMutation { get; init; }
    public required bool PerformsFileWrite { get; init; }
    public required bool PerformsFileDelete { get; init; }
    public required bool PerformsFileMove { get; init; }
    public required bool PerformsDirectoryCreate { get; init; }
    public required bool PerformsPermissionChange { get; init; }
    public required bool PerformsMetadataWrite { get; init; }
    public required bool PerformsLocking { get; init; }
    public required bool RequiresRuntimeAuditBeforeRealUse { get; init; }
    public IReadOnlyList<string> ForbiddenOperationsRedacted { get; init; } = [];
}

public sealed record NodalOsNoMutationProofResult
{
    public required string ResultId { get; init; }
    public required string ProofRef { get; init; }
    public required bool ContractDeclaresNoMutation { get; init; }
    public required bool ReadyForSyntheticNoMutationReview { get; init; }
    public required bool ReadyForRealNoMutationGuarantee { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredAuditBeforeRealUseRedacted { get; init; } = [];
    public required string UserFacingExplanationRedacted { get; init; }
    public required string RealScanReadinessAdrRef { get; init; }
    public required string DisabledPathJailGateRef { get; init; }
    public required string SyntheticCanonicalizationMatrixRef { get; init; }
    public required bool NecessaryButNotSufficientForFutureOperationalScan { get; init; }
}

