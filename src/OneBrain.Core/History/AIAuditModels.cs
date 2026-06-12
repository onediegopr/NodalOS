namespace OneBrain.Core.History;

public static class AIBudgetDecisions
{
    public const string Allowed = "allowed";
    public const string Blocked = "blocked";
}

public static class AIAuditResultStatuses
{
    public const string Routed = "routed";
    public const string Blocked = "blocked";
    public const string FailedClosed = "failed_closed";
    public const string Mocked = "mocked";
}

public sealed record AIAuditRecord(
    string AiAuditId,
    string TimestampUtc,
    string? RecommendedProfileId,
    string? UsedProfileId,
    string Provider,
    string Model,
    string TaskType,
    string RiskLevel,
    bool RequiresVision,
    bool RequiresHumanApproval,
    bool FallbackUsed,
    string? FallbackFrom,
    string? FallbackTo,
    string BudgetDecision,
    decimal EstimatedCostUsd,
    decimal? ActualCostUsd,
    int? TokensIn,
    int? TokensOut,
    string ResultStatus,
    string Reason,
    string Error);

public sealed record AIAuditArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
