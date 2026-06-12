using OneBrain.Core.History;

namespace OneBrain.Core.Flows;

public static class SupervisedPlaybackStatuses
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Blocked = "blocked";
    public const string Aborted = "aborted";
}

public static class SupervisedPlaybackStepStatuses
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Skipped = "skipped";
    public const string Blocked = "blocked";
    public const string Aborted = "aborted";
}

public sealed record SupervisedPlaybackSession(
    string PlaybackId,
    string FlowId,
    string StartedAtUtc,
    string? EndedAtUtc,
    string Status,
    int CurrentStepNumber,
    IReadOnlyList<SupervisedPlaybackStepState> Steps,
    RunSafetyCounters SafetyCounters,
    IReadOnlyList<string> ArtifactPaths,
    IReadOnlyList<string> Notes);

public sealed record SupervisedPlaybackStepState(
    int StepNumber,
    string Status,
    string Decision,
    string EvidenceSummary,
    string? ApprovalRequestId,
    string? ApprovalDecisionId,
    string UpdatedAtUtc,
    IReadOnlyList<string> Notes);

public sealed record SupervisedPlaybackActionResult(
    bool Success,
    SupervisedPlaybackSession Session,
    string Message,
    IReadOnlyList<string> Evidence,
    RunHistoryRecord RunHistory);

public sealed record SupervisedPlaybackArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
