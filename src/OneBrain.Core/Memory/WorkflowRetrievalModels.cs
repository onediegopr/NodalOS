namespace OneBrain.Core.Memory;

public sealed record WorkflowRetrievalQuery(
    string? Text = null,
    IReadOnlyList<string>? Tags = null,
    string? AppOrSite = null,
    string? Domain = null,
    string? RiskLevel = null,
    string? Status = null,
    int? MinConfidenceScore = null);

public sealed record WorkflowRetrievalMatch(
    string ProcessMemoryId,
    string Title,
    double Score,
    IReadOnlyList<string> Reasons,
    string? RecipeId,
    string? CandidateFlowId,
    string? TimelineId,
    bool SafeToSuggest,
    bool RequiresHumanReview);

public sealed record WorkflowRetrievalResult(
    WorkflowRetrievalQuery Query,
    IReadOnlyList<WorkflowRetrievalMatch> Matches);
