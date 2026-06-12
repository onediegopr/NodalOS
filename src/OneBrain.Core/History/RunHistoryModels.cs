namespace OneBrain.Core.History;

public static class RunHistoryStatuses
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Diagnostic = "diagnostic";
    public const string Blocked = "blocked";
}

public static class RunHistorySources
{
    public const string Pilot = "pilot";
    public const string Cli = "cli";
    public const string Recipe = "recipe";
    public const string Recording = "recording";
    public const string Approval = "approval";
    public const string AiRouter = "ai_router";
    public const string Mock = "mock";
}

public sealed record RunSafetyCounters(
    int Clicks,
    int CookiesAccepted,
    int Login,
    int Cart,
    int Purchase,
    int Payment)
{
    public static RunSafetyCounters Zero { get; } = new(0, 0, 0, 0, 0, 0);
}

public sealed record RunHistoryRecord(
    string RunId,
    string StartedAtUtc,
    string? EndedAtUtc,
    string Status,
    string Source,
    string? RecipeId,
    string? CandidateFlowId,
    string? ApprovalRequestId,
    string? ApprovalDecisionId,
    string? RecordingSessionId,
    string? TimelineId,
    string? ConfidenceId,
    string? AiRoutingDecisionId,
    int? ExitCode,
    RunSafetyCounters SafetyCounters,
    IReadOnlyList<string> ArtifactPaths,
    string ErrorSummary,
    IReadOnlyList<string> Notes);

public sealed record RunHistoryArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
