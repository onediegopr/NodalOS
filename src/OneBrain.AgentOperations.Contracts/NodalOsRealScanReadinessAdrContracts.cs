namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsRealScanReadinessDecisionStatus
{
    RealScanNotReadySyntheticBaselineReady,
    UnknownRequiresReview
}

public sealed record NodalOsRealScanReadinessAdrSummary
{
    public required string AdrId { get; init; }
    public required string DecisionRecordPath { get; init; }
    public required NodalOsRealScanReadinessDecisionStatus DecisionStatus { get; init; }
    public required bool RealScanReady { get; init; }
    public required bool SyntheticBaselineReady { get; init; }
    public required bool FuturePathJailPrototypeMayProceedIfDisabledByDefault { get; init; }
    public required bool RealFilesystemAccessBlocked { get; init; }
    public required bool DirectoryEnumerationBlocked { get; init; }
    public required bool ContentAccessBlocked { get; init; }
    public required bool ContentFingerprintingBlocked { get; init; }
    public required bool IndexingBlocked { get; init; }
    public required bool RepresentationBuildBlocked { get; init; }
    public required bool LlmContextBlocked { get; init; }
    public required bool CloudBlocked { get; init; }
    public required bool ProviderBlocked { get; init; }
    public required bool RuntimeBlocked { get; init; }
    public required bool FixtureCoverageNecessaryNotSufficient { get; init; }
    public IReadOnlyList<string> RequiredNextMilestonesRedacted { get; init; } = [];
    public IReadOnlyList<string> AuditTriggersRedacted { get; init; } = [];
    public IReadOnlyList<string> RollbackDisableStrategyRedacted { get; init; } = [];
}

