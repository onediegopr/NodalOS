namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsOrchestrationCommandKind
{
    CreateMission,
    CreateTask,
    PrepareRun,
    ValidateRecipeManifest,
    ValidateSkill,
    RegisterPackageSnapshot,
    QuerySkillRegistry,
    PrepareWorkerRequest,
    GetRunStatus,
    PauseRun,
    ResumeRun,
    CancelRun,
    RequestHumanDecision,
    AttachEvidence,
    GetRunReport,
    GetProgressReport,
    EvaluateVerificationBeforeDone
}

public enum NodalOsOrchestrationState
{
    Draft,
    Prepared,
    AwaitingPolicy,
    AwaitingApproval,
    ReadyForDryRun,
    DryRunPrepared,
    RunningFuture,
    PausedFuture,
    Blocked,
    Completed,
    Failed,
    Cancelled
}

public enum NodalOsOrchestrationCommandRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public sealed record NodalOsOrchestrationCommandEnvelope
{
    public required string CommandId { get; init; }

    public required NodalOsOrchestrationCommandKind Kind { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RunId { get; init; }

    public string? RecipeId { get; init; }

    public string? PackageId { get; init; }

    public string? SkillId { get; init; }

    public string? WorkerId { get; init; }

    public required NodalOsOrchestrationCommandRiskLevel RiskLevel { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresHumanApproval { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];

    public string? Summary { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsOrchestrationCommandResult
{
    public required string ResultId { get; init; }

    public required string CommandId { get; init; }

    public required NodalOsOrchestrationCommandKind Kind { get; init; }

    public required bool Accepted { get; init; }

    public required bool Executed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required NodalOsOrchestrationState State { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<NexaFailureKind> FailureKinds { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsOrchestrationCommandValidationResult
{
    public required bool IsValid { get; init; }

    public required bool CanPassCommandPolicy { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
