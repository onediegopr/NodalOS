namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsCapabilityAuditChecklistStatus
{
    ContractChecklistComplete,
    Incomplete,
    Failed
}

public enum NodalOsCapabilityAuditRequirementCategory
{
    ExplicitFutureMilestone,
    UserConsentEnforcement,
    ScopeNarrowing,
    ConsentFreshness,
    RevocationSupport,
    PathJailImplementationAudit,
    CanonicalizationImplementationAudit,
    FolderEnumerationGateAudit,
    ContentAccessGateAudit,
    ContentFingerprintGateAudit,
    RedactionEnforcement,
    SensitiveDataDetectionEnforcement,
    ExclusionEnforcement,
    NoMutationRuntimeProof,
    CancellationRuntimeProof,
    EvidenceTimelineEmission,
    FailClosedBehavior,
    RollbackDisableKillSwitch,
    AdversarialTests,
    LocalOnlyGuarantee,
    SeparateGovernanceForLlmCloudProviderRuntime
}

public enum NodalOsCapabilityAuditChecklistItemStatus
{
    RequiredNotImplemented,
    ContractDocumented
}

public sealed record NodalOsCapabilityAuditChecklist
{
    public required string ChecklistId { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public IReadOnlyList<string> CapabilityGateRefs { get; init; } = [];
    public required string OperationalAccessAuditAdrRef { get; init; }
    public required string FailClosedAcceptancePackRef { get; init; }
    public required NodalOsCapabilityAuditChecklistStatus ChecklistStatus { get; init; }
    public required bool IsChecklistOnly { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanEnableGate { get; init; }
    public required bool CanAccessFilesystem { get; init; }
    public required bool CanBuildLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public IReadOnlyList<NodalOsCapabilityAuditChecklistItem> Items { get; init; } = [];
    public required NodalOsCapabilityAuditChecklistDecision Decision { get; init; }
}

public sealed record NodalOsCapabilityAuditChecklistItem
{
    public required string ItemId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required string RequirementRedacted { get; init; }
    public required NodalOsCapabilityAuditRequirementCategory RequirementCategory { get; init; }
    public required bool RequiredBeforeEnablement { get; init; }
    public required NodalOsCapabilityAuditChecklistItemStatus Status { get; init; }
    public required string EvidenceRef { get; init; }
    public required string TimelineRef { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
    public required bool BlocksRealUseIfMissing { get; init; }
}

public sealed record NodalOsCapabilityAuditChecklistDecision
{
    public required string DecisionId { get; init; }
    public required bool ReadyForChecklistCloseout { get; init; }
    public required bool ReadyForRealCapabilityEnablement { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForRepresentationBuild { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public required bool ReadyForCloud { get; init; }
    public required bool ReadyForRuntime { get; init; }
}
