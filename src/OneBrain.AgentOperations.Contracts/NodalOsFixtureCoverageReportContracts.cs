namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsFixtureCoverageStatus
{
    CompleteSyntheticCoverage,
    PartialSyntheticCoverage,
    MissingRequiredSyntheticCoverage,
    UnknownRequiresReview
}

public enum NodalOsFixtureCoverageDimensionKind
{
    PathContainment,
    SymlinkLike,
    CaseSensitivity,
    OutsideJail,
    DependencyExclusion,
    GeneratedOutput,
    SensitiveName,
    EnvironmentMarker,
    BinaryMedia,
    DeepTree,
    MaxFiles,
    MaxBytes,
    Cancellation,
    NoMutation,
    Redaction,
    NeverLeavesLocalPolicy
}

public sealed record NodalOsFixtureCoverageDimension
{
    public required NodalOsFixtureCoverageDimensionKind Kind { get; init; }
    public required bool IsCovered { get; init; }
    public IReadOnlyList<string> CoveredCategoryRefs { get; init; } = [];
    public required string ExplanationRedacted { get; init; }
}

public sealed record NodalOsFixtureCoverageReport
{
    public required string CoverageReportId { get; init; }
    public required string FixtureMatrixRef { get; init; }
    public required string SimulatorResultRef { get; init; }
    public required string FixtureResultReviewRef { get; init; }
    public required int TotalFixtureCategories { get; init; }
    public required int CoveredFixtureCategories { get; init; }
    public IReadOnlyList<NodalOsScanFixtureCategory> MissingFixtureCategories { get; init; } = [];
    public required decimal CoveragePercent { get; init; }
    public required NodalOsFixtureCoverageStatus CoverageStatus { get; init; }
    public required bool IsSyntheticCoverageOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool CanAuthorizeRealScan { get; init; }
    public IReadOnlyList<NodalOsFixtureCoverageDimension> CoverageDimensions { get; init; } = [];
    public required NodalOsFixtureCoverageDecision CoverageDecision { get; init; }
    public IReadOnlyList<string> RequiredGapsRedacted { get; init; } = [];
}

public sealed record NodalOsFixtureCoverageDecision
{
    public required string CoverageDecisionId { get; init; }
    public required bool ReadyForSyntheticCoverageCloseout { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForRealPathJail { get; init; }
    public required bool ReadyForRealSecretDetection { get; init; }
    public required bool ReadyForRealExclusionEnforcement { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForRepresentationBuild { get; init; }
    public required bool ReadyForLlmContext { get; init; }
}

