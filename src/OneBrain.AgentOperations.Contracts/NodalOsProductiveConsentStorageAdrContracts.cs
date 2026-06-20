namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsProductiveConsentStorageAdrDecisionStatus
{
    ProductiveConsentStorageNotImplementedAdrReady
}

public sealed record NodalOsProductiveConsentStorageAdrSummary
{
    public required string AdrId { get; init; }
    public required string AdrPathRef { get; init; }
    public required string DesignReviewRef { get; init; }
    public required string AdversarialMatrixRef { get; init; }
    public required NodalOsProductiveConsentStorageAdrDecisionStatus DecisionStatus { get; init; }
    public required bool ProductiveConsentStorageImplemented { get; init; }
    public required bool FutureImplementationDisabledByDefault { get; init; }
    public required bool FutureImplementationLocalFirst { get; init; }
    public required bool RequiresScopeBoundRecords { get; init; }
    public required bool RequiresCapabilityBoundRecords { get; init; }
    public required bool RequiresWorkspaceBoundRecords { get; init; }
    public required bool RequiresMissionBoundRecords { get; init; }
    public required bool StorageMayContainSensitiveMaterial { get; init; }
    public required bool StorageMayContainContentPayloads { get; init; }
    public required bool StorageMayContainUnredactedBroadPaths { get; init; }
    public required bool StorageCanImplyFilesystemAccess { get; init; }
    public required bool StorageCanImplyLlmCloudProviderRuntimePermission { get; init; }
    public required bool ConsentCanImplyAnotherCapability { get; init; }
    public required bool RevokedStaleMissingConsentFailsClosed { get; init; }
    public required bool StorageAndEnforcementSeparateMilestones { get; init; }
    public required bool StorageAndCapabilityEnablementSeparateMilestones { get; init; }
    public IReadOnlyList<string> RequiredBeforeImplementationRedacted { get; init; } = [];
    public IReadOnlyList<string> RejectedAlternativesRedacted { get; init; } = [];
}
