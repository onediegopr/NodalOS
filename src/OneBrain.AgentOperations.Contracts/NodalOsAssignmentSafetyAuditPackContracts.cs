namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsAssignmentSafetyAuditDimension
{
    DraftOnlyIntegrity,
    NoOpInteractionIntegrity,
    NoPlannerRuntime,
    NoPromptLlmProvider,
    NoRuntimeExecution,
    NoAsyncDispatchRuntime,
    NoFilesystemReal,
    NoNetworkHttp,
    NoBrowserAutomationReference,
    NoEvidenceContentVerification,
    NoProductivePersistence,
    NoUsageMetricsAnalytics,
    RedactionSecretSafety,
    DeterministicSerialization,
    HumanReadableExplanations
}

public enum NodalOsAssignmentSafetyAuditStatus
{
    Pass,
    ConditionalPass,
    Fail
}

public sealed record NodalOsAssignmentSafetyAuditFinding
{
    public required NodalOsAssignmentSafetyAuditDimension Dimension { get; init; }

    public required NodalOsAssignmentSafetyAuditStatus Status { get; init; }

    public required string FindingRedacted { get; init; }

    public IReadOnlyList<string> RisksRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredFixesRedacted { get; init; } = [];
}

public sealed record NodalOsAssignmentSafetyAuditPack
{
    public required string AuditPackId { get; init; }

    public required NodalOsAssignmentSafetyAuditStatus OverallStatus { get; init; }

    public IReadOnlyList<NodalOsAssignmentSafetyAuditFinding> Findings { get; init; } = [];

    public IReadOnlyList<string> NextAuditTriggersRedacted { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool ExecutionBoundaryCrossed { get; init; }

    public required bool PlannerRuntimeIntroduced { get; init; }

    public required bool PromptOrModelIntroduced { get; init; }

    public required bool FilesystemOrNetworkIntroduced { get; init; }

    public required bool ProductivePersistenceIntroduced { get; init; }
}
