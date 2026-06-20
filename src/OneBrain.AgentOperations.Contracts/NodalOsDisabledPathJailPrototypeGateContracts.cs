namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsDisabledPathJailPrototypeGate
{
    public required string GateId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required string PathJailPrototypeRef { get; init; }
    public required string RealScanReadinessAdrRef { get; init; }
    public required string SyntheticBaselineRef { get; init; }
    public required bool DisabledByDefault { get; init; }
    public required bool RequiresExplicitFutureEnablement { get; init; }
    public required bool RequiresAuditBeforeEnablement { get; init; }
    public required bool RequiresConsentBeforeEnablement { get; init; }
    public required bool RequiresNoMutationProofBeforeEnablement { get; init; }
    public required bool RequiresCancellationPolicyBeforeEnablement { get; init; }
    public required bool RequiresEvidenceTimelinePlanBeforeEnablement { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsRealCanonicalization { get; init; }
    public required bool PerformsDirectoryListing { get; init; }
    public required bool PerformsFileRead { get; init; }
    public required bool PerformsFileHash { get; init; }
    public required bool CanAuthorizeRealScan { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public IReadOnlyList<string> EnablementRequirementsRedacted { get; init; } = [];
}

public sealed record NodalOsDisabledPathJailPrototypeGateDecision
{
    public required string DecisionId { get; init; }
    public required string GateRef { get; init; }
    public required bool PrototypeGateCreated { get; init; }
    public required bool PrototypeGateEnabled { get; init; }
    public required bool ReadyForDisabledPrototypeReview { get; init; }
    public required bool ReadyForRealPathJail { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForDirectoryListing { get; init; }
    public required bool ReadyForFileRead { get; init; }
    public required bool ReadyForFileHash { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForRepresentationBuild { get; init; }
    public required bool ReadyForLlmContext { get; init; }
}

