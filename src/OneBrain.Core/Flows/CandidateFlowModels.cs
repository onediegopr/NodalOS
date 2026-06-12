using OneBrain.Core.Recording;

namespace OneBrain.Core.Flows;

public static class CandidateFlowStatuses
{
    public const string Observed = "observed";
    public const string Annotated = "annotated";
    public const string Candidate = "candidate";
    public const string ApprovedForSupervisedPlayback = "approved_for_supervised_playback";
    public const string Rejected = "rejected";
    public const string Archived = "archived";
}

public static class CandidateFlowStepExecutionModes
{
    public const string FixtureOnly = "fixture_only";
    public const string PreviewOnly = "preview_only";
    public const string HumanConfirmed = "human_confirmed";
    public const string BlockedNoExecutor = "blocked_no_safe_executor";
}

public sealed record CandidateFlowPromotionRequest(
    string CandidateFlowId,
    string Title,
    string Description,
    RecipeTimeline Timeline,
    bool LinterPassed,
    bool VariablesResolvedOrDeclared,
    bool RiskPolicyConsistent,
    bool ApprovalPolicyPresent,
    bool HasBlockedActions,
    string RequestedBy,
    string CreatedAtUtc,
    IReadOnlyList<string> Variables,
    IReadOnlyList<string> Notes);

public sealed record CandidateFlowPromotionResult(
    bool Success,
    string Status,
    PromotedCandidateFlow? Flow,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Notes);

public sealed record PromotedCandidateFlow(
    string FlowId,
    string CandidateFlowId,
    string TimelineId,
    string Title,
    string Description,
    string Status,
    string CreatedAtUtc,
    string UpdatedAtUtc,
    string RiskLevel,
    int ConfidenceScore,
    bool RequiresHumanApproval,
    bool AllowsAutonomousPlayback,
    IReadOnlyList<string> Variables,
    IReadOnlyList<PromotedFlowStep> Steps,
    IReadOnlyList<string> ArtifactPaths,
    IReadOnlyList<string> Notes);

public sealed record PromotedFlowStep(
    int StepNumber,
    int SourceTimelineStepNumber,
    string Title,
    string Description,
    string ActionKind,
    string RiskLevel,
    double Confidence,
    bool RequiresApproval,
    bool HasSafeExecutor,
    string ExecutionMode,
    bool CanSkip,
    IReadOnlyList<string> EvidenceLabels,
    IReadOnlyList<string> Notes);

public sealed record PromotedFlowArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
