namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsOperationalCapability
{
    PathCanonicalization,
    DirectoryListing,
    FileRead,
    FileHash,
    SecretDetection,
    ExclusionEnforcement,
    Indexing,
    RepresentationBuild,
    LlmContextBuild,
    CloudSync,
    ProviderCall,
    RuntimeExecution
}

public sealed record NodalOsCapabilityAccessGate
{
    public required string GateId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required string ParentOperationalAuditRef { get; init; }
    public required bool DisabledByDefault { get; init; }
    public required bool RequiresExplicitFutureEnablement { get; init; }
    public required bool RequiresUserConsent { get; init; }
    public required bool RequiresAuditBeforeEnablement { get; init; }
    public required bool RequiresEvidenceTimelinePlan { get; init; }
    public required bool RequiresCancellationSemantics { get; init; }
    public required bool RequiresNoMutationProof { get; init; }
    public required bool RequiresRedactionPolicy { get; init; }
    public required bool RequiresSecretDetectionPolicy { get; init; }
    public required bool RequiresExclusionPolicy { get; init; }
    public required bool FailClosed { get; init; }
    public required bool IsContractOnly { get; init; }
}

public sealed record NodalOsCapabilityGateDecision
{
    public required string DecisionId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required bool GateCreated { get; init; }
    public required bool GateEnabled { get; init; }
    public required bool ReadyForSyntheticReview { get; init; }
    public required bool ReadyForRealUse { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAccessFilesystem { get; init; }
    public required bool CanReadContent { get; init; }
    public required bool CanFingerprintContent { get; init; }
    public required bool CanBuildRepresentation { get; init; }
    public required bool CanSendToLlm { get; init; }
    public required bool CanSendToCloud { get; init; }
    public IReadOnlyList<string> RequiredBeforeEnablementRedacted { get; init; } = [];
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsCapabilityDependency
{
    public required NodalOsOperationalCapability Capability { get; init; }
    public IReadOnlyList<NodalOsOperationalCapability> DependsOn { get; init; } = [];
    public IReadOnlyList<string> PolicyDependenciesRedacted { get; init; } = [];
}

public sealed record NodalOsCapabilityDependencyMatrix
{
    public required string MatrixId { get; init; }
    public IReadOnlyList<NodalOsCapabilityDependency> Dependencies { get; init; } = [];
}

