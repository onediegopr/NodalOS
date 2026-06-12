namespace OneBrain.Core.Memory;

public static class ProcessMemorySources
{
    public const string Recording = "recording";
    public const string Timeline = "timeline";
    public const string RecipeDraft = "recipe_draft";
    public const string Recipe = "recipe";
    public const string Approval = "approval";
    public const string RunHistory = "run_history";
    public const string AiAudit = "ai_audit";
    public const string Fixture = "fixture";
}

public static class ProcessMemoryStatuses
{
    public const string Observed = "observed";
    public const string Annotated = "annotated";
    public const string Candidate = "candidate";
    public const string Supervised = "supervised";
    public const string Stable = "stable";
    public const string Rejected = "rejected";
    public const string Archived = "archived";
}

public sealed record ProcessMemoryLink(
    string? RecordingSessionId,
    string? TimelineId,
    string? CandidateFlowId,
    string? RecipeDraftId,
    string? RecipeId,
    string? ApprovalRequestId,
    string? ApprovalDecisionId,
    string? RunId,
    string? AiAuditId,
    string? ConfidenceId);

public sealed record ProcessMemoryDecision(
    string DecisionId,
    string CreatedAtUtc,
    string Summary,
    string Reason,
    string Outcome);

public sealed record ProcessMemoryError(
    string Code,
    string Message,
    string LastSeenAtUtc);

public sealed record ProcessMemoryEvidenceLink(
    string Kind,
    string RelativePath,
    string Label);

public sealed record ProcessMemorySummary(
    string Summary,
    IReadOnlyList<string> StepSummaries,
    IReadOnlyList<string> KeyRisks,
    IReadOnlyList<string> NextActions);

public sealed record ProcessMemoryEntry(
    string Id,
    string Title,
    string Description,
    string Source,
    string Status,
    string AppOrSite,
    string Domain,
    IReadOnlyList<string> Tags,
    string RiskLevel,
    int ConfidenceScore,
    string CreatedAtUtc,
    string UpdatedAtUtc,
    string? LastUsedAtUtc,
    ProcessMemorySummary Summary,
    ProcessMemoryLink Links,
    IReadOnlyList<ProcessMemoryDecision> Decisions,
    IReadOnlyList<ProcessMemoryError> Errors,
    IReadOnlyList<ProcessMemoryEvidenceLink> EvidenceLinks,
    IReadOnlyList<string> ArtifactPaths,
    IReadOnlyList<string> Notes);

public sealed record ProcessMemoryArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
