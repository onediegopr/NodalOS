namespace OneBrain.Core.Confidence;

public static class RecipeConfidenceStatuses
{
    public const string New = "new";
    public const string Candidate = "candidate";
    public const string Supervised = "supervised";
    public const string Stable = "stable";
    public const string Critical = "critical";
    public const string Disabled = "disabled";
    public const string Blocked = "blocked";
}

public static class RecipeConfidenceRiskLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";
}

public sealed record RecipeConfidenceInput(
    string RecipeId,
    string? CandidateFlowId,
    string Status,
    string RiskLevel,
    int Runs,
    int Successes,
    int Failures,
    string? LastError,
    string? LastVerifiedAt,
    string? ApprovalRequiredUntil,
    IReadOnlyList<string> Notes);

public sealed record RecipeConfidenceProfile(
    string RecipeId,
    string? CandidateFlowId,
    string Status,
    int ConfidenceScore,
    string RiskLevel,
    int Runs,
    int Successes,
    int Failures,
    string? LastError,
    string? LastVerifiedAt,
    string? ApprovalRequiredUntil,
    IReadOnlyList<string> Notes);

public sealed record RecipeConfidenceArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
