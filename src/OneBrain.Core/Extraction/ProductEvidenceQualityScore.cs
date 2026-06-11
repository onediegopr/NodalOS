namespace OneBrain.Core.Extraction;

public sealed record ProductEvidenceQualityScore
{
    public int EvidenceScore { get; init; }
    public string EvidenceGrade { get; init; } = "insufficient";
    public string QualityStatus { get; init; } = "insufficient";
    public IReadOnlyList<string> QualityReasons { get; init; } = [];
    public IReadOnlyList<string> MissingCriticalFields { get; init; } = [];
    public string DecisionReadiness { get; init; } = "needs_more_evidence";
}
