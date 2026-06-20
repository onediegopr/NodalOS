namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsSyntheticDryRunUiResultsPreview
{
    public required string ResultsPreviewId { get; init; }
    public required string SimulatorResultRef { get; init; }
    public required string FixtureResultReviewRef { get; init; }
    public required string ScanBoundaryAuditRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsStaticPreview { get; init; }
    public required bool IsReadOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool UsesSyntheticFixturesOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsRealScan { get; init; }
    public required bool PerformsDirectoryListing { get; init; }
    public required bool PerformsFileRead { get; init; }
    public required bool PerformsFileHash { get; init; }
    public required bool PerformsIndexing { get; init; }
    public required bool PerformsRepresentationBuild { get; init; }
    public required bool BuildsLlmContext { get; init; }
    public required bool CallsProvider { get; init; }
    public required bool UsesCloud { get; init; }
    public required NodalOsSyntheticDryRunUiResultSections Sections { get; init; }
    public IReadOnlyList<string> DisclosuresRedacted { get; init; } = [];
}

public sealed record NodalOsSyntheticDryRunUiResultSections
{
    public required string SimulationSummaryRedacted { get; init; }
    public required int IncludedPreviewCount { get; init; }
    public required int ExcludedPreviewCount { get; init; }
    public required int BlockedPreviewCount { get; init; }
    public required int RequiresReviewCount { get; init; }
    public required int RedactedPreviewCount { get; init; }
    public required int AuditRequiredCount { get; init; }
    public IReadOnlyList<string> FixtureMismatchesRedacted { get; init; } = [];
    public IReadOnlyList<string> OpenQuestionsRedacted { get; init; } = [];
    public IReadOnlyList<string> BlockedCapabilitiesRedacted { get; init; } = [];
    public IReadOnlyList<string> NextGateRequirementsRedacted { get; init; } = [];
    public required string UserFacingExplanationRedacted { get; init; }
}

