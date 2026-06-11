namespace OneBrain.Core.Extraction;

public sealed record ProductEvidenceSummary
{
    public string SchemaVersion { get; init; } = "product-evidence-summary/v1";
    public string CreatedAtUtc { get; init; } = "";
    public int SourceArtifactCount { get; init; }
    public int ValidArtifactCount { get; init; }
    public int InvalidArtifactCount { get; init; }
    public IReadOnlyList<ProductEvidenceSummaryItem> Items { get; init; } = [];
    public ProductEvidenceSummaryTotals Totals { get; init; } = new();
    public IReadOnlyList<string> InvalidArtifacts { get; init; } = [];
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed record ProductEvidenceSummaryItem
{
    public string RecipeId { get; init; } = "";
    public string ProfileId { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public string? ProductName { get; init; }
    public string? Category { get; init; }
    public string? Price { get; init; }
    public string? Currency { get; init; }
    public string? Stock { get; init; }
    public string ExtractionStatus { get; init; } = "diagnostic";
    public string ExtractionConfidence { get; init; } = "diagnostic";
    public IReadOnlyList<string> BlockedOrMissingFields { get; init; } = [];
    public bool HasPrice { get; init; }
    public bool HasCurrency { get; init; }
    public bool HasStock { get; init; }
    public int RawSignalCount { get; init; }
    public ProductEvidenceSafetySummary SafetySummary { get; init; } = new();
    public string ArtifactPath { get; init; } = "";
    public int EvidenceScore { get; init; }
    public string EvidenceGrade { get; init; } = "insufficient";
    public string QualityStatus { get; init; } = "insufficient";
    public IReadOnlyList<string> QualityReasons { get; init; } = [];
    public IReadOnlyList<string> MissingCriticalFields { get; init; } = [];
    public string DecisionReadiness { get; init; } = "needs_more_evidence";
}

public sealed record ProductEvidenceSummaryTotals
{
    public int ProductsWithPrice { get; init; }
    public int ProductsMissingPrice { get; init; }
    public int ProductsWithMediumConfidence { get; init; }
    public int ProductsWithHighConfidence { get; init; }
    public int ProductsWithDiagnosticStatus { get; init; }
    public int SafetyClicksTotal { get; init; }
    public int SafetyPaymentsSignalsTotal { get; init; }
    public int ArtifactsWithWarnings { get; init; }
    public int SufficientCount { get; init; }
    public int PartialCount { get; init; }
    public int InsufficientCount { get; init; }
    public int DiagnosticCount { get; init; }
    public double AverageEvidenceScore { get; init; }
    public int ReadyForComparisonCount { get; init; }
    public int NeedsPriceVerificationCount { get; init; }
}

public sealed record ProductEvidenceSummarySource
{
    public ProductEvidenceArtifact? Artifact { get; init; }
    public string ArtifactPath { get; init; } = "";
    public string? Error { get; init; }
}

public sealed record ProductEvidenceSummaryWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
    public ProductEvidenceSummary Summary { get; init; } = new();
}
