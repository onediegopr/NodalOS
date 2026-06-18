namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NexaMission
{
    public required string MissionId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required NexaMissionStatus Status { get; init; }
    public required string HumanOwner { get; init; }
    public IReadOnlyList<NexaAgentTask> Tasks { get; init; } = [];
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public enum NexaMissionStatus
{
    Draft,
    Ready,
    InProgress,
    Blocked,
    InReview,
    Completed,
    Cancelled
}

public sealed record NexaAgentTask
{
    public required string TaskId { get; init; }
    public required string MissionId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required NexaAgentTaskStatus Status { get; init; }
    public string? AssignedAgent { get; init; }
    public required string HumanOwner { get; init; }
    public int Priority { get; init; }
    public IReadOnlyList<NexaProgressNote> ProgressNotes { get; init; } = [];
    public IReadOnlyList<NexaBlockerReport> Blockers { get; init; } = [];
    public IReadOnlyList<NexaVerificationCheck> VerificationChecks { get; init; } = [];
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public string? CompletionReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public enum NexaAgentTaskStatus
{
    Draft,
    Ready,
    Assigned,
    InProgress,
    Blocked,
    WaitingForHuman,
    WaitingForApproval,
    InReview,
    Completed,
    Cancelled,
    Failed
}

public sealed record NexaProgressNote
{
    public required string NoteId { get; init; }
    public required string TaskId { get; init; }
    public required string Author { get; init; }
    public required string Summary { get; init; }
    public string? Detail { get; init; }
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NexaBlockerReport
{
    public required string BlockerId { get; init; }
    public required string TaskId { get; init; }
    public required NexaBlockerKind Kind { get; init; }
    public required string Summary { get; init; }
    public string? Detail { get; init; }
    public required NexaBlockerSeverity Severity { get; init; }
    public required NexaBlockerResolutionMode SuggestedResolution { get; init; }
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public enum NexaBlockerKind
{
    MissingContext,
    MissingCredential,
    PolicyBlocked,
    RuntimeBlocked,
    TestFailure,
    BuildFailure,
    GitConflict,
    ExternalDependency,
    HumanDecisionRequired,
    ApprovalRequired,
    Unknown
}

public enum NexaBlockerSeverity
{
    Info,
    Warning,
    Blocking,
    Critical
}

public enum NexaBlockerResolutionMode
{
    AskHuman,
    Replan,
    Retry,
    Escalate,
    StopWithEvidence,
    WaitForExternalInput
}

public sealed record NexaVerificationCheck
{
    public required string CheckId { get; init; }
    public required string TaskId { get; init; }
    public required string Label { get; init; }
    public required NexaVerificationStatus Status { get; init; }
    public bool Required { get; init; } = true;
    public string? Detail { get; init; }
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
}

public enum NexaVerificationStatus
{
    Pending,
    Passed,
    Failed,
    SkippedWithReason
}

public sealed record NexaEvidenceRef
{
    public required string EvidenceId { get; init; }
    public required string Kind { get; init; }
    public string? Ref { get; init; }
    public string? Hash { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NexaTaskValidationResult
{
    public required bool CanComplete { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
