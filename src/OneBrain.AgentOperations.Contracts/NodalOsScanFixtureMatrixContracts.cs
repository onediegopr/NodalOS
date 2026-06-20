namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsScanFixtureCategory
{
    EmptyWorkspace,
    SmallSourceTree,
    DependencyFolder,
    GeneratedOutput,
    HiddenItem,
    EnvironmentMarker,
    SensitiveName,
    BinaryMedia,
    SymlinkLike,
    OutsideJailPath,
    CaseSensitivity,
    DeepTree,
    MaxFiles,
    MaxBytes,
    Cancellation,
    NoMutation
}

public enum NodalOsScanFixtureExpectedDisposition
{
    IncludedPreview,
    ExcludedPreview,
    BlockedPreview,
    RequiresReview,
    RedactedPreview,
    AuditRequired
}

public sealed record NodalOsScanFixtureExpectedOutcome
{
    public required NodalOsScanFixtureExpectedDisposition Disposition { get; init; }

    public required bool NeverSentToLlm { get; init; }

    public required bool NeverSentToCloud { get; init; }

    public required bool RequiresAudit { get; init; }

    public required bool RequiresRedaction { get; init; }
}

public sealed record NodalOsScanFixtureDefinition
{
    public required string FixtureId { get; init; }

    public required NodalOsScanFixtureCategory Category { get; init; }

    public required string DisplayNameRedacted { get; init; }

    public required string SyntheticPathRef { get; init; }

    public required NodalOsScanFixtureExpectedOutcome ExpectedOutcome { get; init; }
}

public sealed record NodalOsScanFixtureMatrix
{
    public required string MatrixId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required bool UsesSyntheticFixturesOnly { get; init; }

    public required bool UsesRealFilesystem { get; init; }

    public required bool PerformsDirectoryListing { get; init; }

    public required bool PerformsFileRead { get; init; }

    public required bool PerformsFileHash { get; init; }

    public required bool PerformsSecretDetectionOnRealData { get; init; }

    public required bool PerformsIndexing { get; init; }

    public required bool PerformsVectorization { get; init; }

    public required bool BuildsLlmContext { get; init; }

    public IReadOnlyList<NodalOsScanFixtureDefinition> Fixtures { get; init; } = [];
}

public sealed record NodalOsScanFixtureMatrixReadiness
{
    public required string ReadinessId { get; init; }

    public required string MatrixRef { get; init; }

    public required bool ReadyForSyntheticDryRunTests { get; init; }

    public required bool ReadyForRealScan { get; init; }

    public required bool ReadyForRealFilesystemAccess { get; init; }

    public required bool ReadyForIndexing { get; init; }

    public required bool ReadyForVectorization { get; init; }

    public required bool ReadyForLlmContext { get; init; }
}
