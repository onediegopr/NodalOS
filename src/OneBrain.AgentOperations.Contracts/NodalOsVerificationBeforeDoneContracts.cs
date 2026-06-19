namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NodalOsVerificationBeforeDoneResult
{
    public required bool CanMarkDone { get; init; }
    public required NodalOsVerificationBeforeDoneSubjectKind SubjectKind { get; init; }
    public required string SubjectId { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<NexaEvidenceRef> EvidenceRefs { get; init; } = [];
    public IReadOnlyList<string> VerificationLabels { get; init; } = [];
    public string? CompletionReason { get; init; }
}

public enum NodalOsVerificationBeforeDoneSubjectKind
{
    Mission,
    AgentTask,
    RunReport
}

public sealed record NodalOsVerificationBeforeDoneOptions
{
    public bool RequireEvidenceOrReason { get; init; } = true;
    public bool RequirePassedRequiredVerification { get; init; } = true;
    public bool BlockOnBlockingOrCriticalBlockers { get; init; } = true;
    public bool AllowCompletedWithWarnings { get; init; } = true;
    public bool RequireRunTerminalTimestamp { get; init; } = true;
}
