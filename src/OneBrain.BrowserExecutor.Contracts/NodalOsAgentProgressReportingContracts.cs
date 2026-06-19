namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NodalOsAgentProgressReport
{
    public required string ReportId { get; init; }
    public string? MissionId { get; init; }
    public string? TaskId { get; init; }
    public string? RunId { get; init; }
    public required NodalOsAgentProgressReportKind Kind { get; init; }
    public required NodalOsAgentProgressReportStatus Status { get; init; }
    public required string Summary { get; init; }
    public string? Detail { get; init; }
    public string? ReportingAgent { get; init; }
    public string? HumanOwner { get; init; }
    public IReadOnlyList<NexaProgressNote> ProgressNotes { get; init; } = [];
    public IReadOnlyList<NexaBlockerReport> Blockers { get; init; } = [];
    public IReadOnlyList<NodalOsHumanDecisionRequest> HumanDecisionRequests { get; init; } = [];
    public IReadOnlyList<NodalOsVerificationSummary> VerificationSummaries { get; init; } = [];
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public IReadOnlyList<NodalOsReportingWarning> Warnings { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public enum NodalOsAgentProgressReportKind
{
    Progress,
    Blocker,
    Warning,
    CompletionCandidate,
    Handoff,
    Diagnostic
}

public enum NodalOsAgentProgressReportStatus
{
    Informational,
    InProgress,
    WaitingForHuman,
    WaitingForApproval,
    Blocked,
    ReadyForReview,
    ReadyToClose,
    NotReadyToClose,
    Failed,
    Cancelled
}

public sealed record NodalOsHumanDecisionRequest
{
    public required string RequestId { get; init; }
    public required string Summary { get; init; }
    public string? Detail { get; init; }
    public required NodalOsHumanDecisionKind Kind { get; init; }
    public required NodalOsHumanDecisionUrgency Urgency { get; init; }
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public enum NodalOsHumanDecisionKind
{
    MissingContext,
    MissingCredential,
    PolicyDecision,
    ApprovalRequired,
    AmbiguousInstruction,
    ExternalDependency,
    RiskAcceptance,
    RetryDecision,
    StopDecision,
    Unknown
}

public enum NodalOsHumanDecisionUrgency
{
    Low,
    Normal,
    High,
    Blocking
}

public sealed record NodalOsVerificationSummary
{
    public required string SubjectId { get; init; }
    public required NodalOsVerificationBeforeDoneSubjectKind SubjectKind { get; init; }
    public required bool CanMarkDone { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> VerificationLabels { get; init; } = [];
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
}

public sealed record NodalOsReportingWarning
{
    public required string WarningId { get; init; }
    public required string Summary { get; init; }
    public string? Detail { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsAgentProgressReportValidationResult
{
    public required bool IsValid { get; init; }
    public required bool ReadyToClose { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
