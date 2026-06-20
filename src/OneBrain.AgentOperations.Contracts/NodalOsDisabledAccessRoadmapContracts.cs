namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsDisabledAccessRoadmapStatus
{
    DesignRoadmapReady,
    Incomplete,
    Failed
}

public enum NodalOsDisabledAccessPhaseKind
{
    ProductiveConsentDesignReview,
    ProductiveConsentStorageContract,
    ProductiveConsentEnforcementPrototype,
    PathJailImplementationDesign,
    PathJailSyntheticRuntimeHarness,
    RealPathJailPrototypeBehindFlag,
    FolderEnumerationDesignGate,
    ContentAccessDesignGate,
    RealScanDryRunDesign
}

public enum NodalOsDisabledAccessRoadmapBlockerKind
{
    NoProductiveConsent,
    NoFeatureFlagStrategy,
    NoKillSwitch,
    NoRuntimeNoMutationProof,
    NoCancellationProof,
    NoEvidenceTimelineEmissionImplementation,
    NoRedactionEnforcementImplementation,
    NoSensitiveDataEnforcementImplementation,
    NoExclusionEnforcementImplementation,
    NoAdversarialTests,
    NoAuditApproval
}

public sealed record NodalOsDisabledAccessRoadmap
{
    public required string RoadmapId { get; init; }
    public required string GovernanceBaselineRef { get; init; }
    public required string AuditCheckpointRef { get; init; }
    public required string ProductiveConsentDesignRef { get; init; }
    public required NodalOsDisabledAccessRoadmapStatus RoadmapStatus { get; init; }
    public required bool IsRoadmapOnly { get; init; }
    public required bool CanAuthorizeImplementation { get; init; }
    public required bool CanEnableRealAccess { get; init; }
    public IReadOnlyList<NodalOsDisabledAccessRoadmapPhase> Phases { get; init; } = [];
    public IReadOnlyList<NodalOsDisabledAccessRoadmapBlockerKind> Blockers { get; init; } = [];
    public required NodalOsDisabledAccessRoadmapDecision Decision { get; init; }
}

public sealed record NodalOsDisabledAccessRoadmapPhase
{
    public required string PhaseId { get; init; }
    public required NodalOsDisabledAccessPhaseKind PhaseKind { get; init; }
    public required string DescriptionRedacted { get; init; }
    public required bool DisabledByDefault { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool EnablesRealAccess { get; init; }
    public required bool RequiresFutureAudit { get; init; }
}

public sealed record NodalOsDisabledAccessRoadmapDecision
{
    public required string DecisionId { get; init; }
    public required bool ReadyForNextGovernedDesignPhase { get; init; }
    public required bool ReadyForRealImplementation { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public required string RecommendedNextMilestoneRedacted { get; init; }
}
