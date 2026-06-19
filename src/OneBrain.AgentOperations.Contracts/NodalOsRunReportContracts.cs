namespace OneBrain.AgentOperations.Contracts;

public sealed record NexaRunReport
{
    public required string RunId { get; init; }
    public string? MissionId { get; init; }
    public string? TaskId { get; init; }
    public string? RecipeId { get; init; }
    public required string Goal { get; init; }
    public required NexaRunStatus Status { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required IReadOnlyList<NexaRunStepReport> Steps { get; init; }
    public IReadOnlyList<NexaPolicyDecisionReport> PolicyDecisions { get; init; } = [];
    public IReadOnlyList<NexaApprovalReport> Approvals { get; init; } = [];
    public IReadOnlyList<NexaFailureReport> Failures { get; init; } = [];
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public string? FinalSummary { get; init; }
}

public enum NexaRunStatus
{
    Planned,
    AwaitingApproval,
    Running,
    Paused,
    Blocked,
    Completed,
    CompletedWithWarnings,
    Failed,
    Cancelled
}

public sealed record NexaRunStepReport
{
    public required string StepId { get; init; }
    public required int Index { get; init; }
    public required string Label { get; init; }
    public required NexaRunStepStatus Status { get; init; }
    public required string ActionKind { get; init; }
    public string? TargetUrl { get; init; }
    public string? TargetSelector { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public string? Notes { get; init; }
}

public enum NexaRunStepStatus
{
    Pending,
    Running,
    Completed,
    Skipped,
    Blocked,
    Failed,
    WaitingForHuman
}

public sealed record NexaPolicyDecisionReport
{
    public required string DecisionId { get; init; }
    public required string StepId { get; init; }
    public required string PolicyName { get; init; }
    public required string Decision { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NexaApprovalReport
{
    public required string ApprovalId { get; init; }
    public required string StepId { get; init; }
    public required string ApprovalKind { get; init; }
    public required string Status { get; init; }
    public string? HumanActor { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NexaFailureReport
{
    public required string FailureId { get; init; }
    public required string StepId { get; init; }
    public required NexaFailureKind Kind { get; init; }
    public required string Summary { get; init; }
    public string? Detail { get; init; }
    public required NexaFailureSeverity Severity { get; init; }
    public required string SuggestedRecovery { get; init; }
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NexaRunReportValidationResult
{
    public required bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
