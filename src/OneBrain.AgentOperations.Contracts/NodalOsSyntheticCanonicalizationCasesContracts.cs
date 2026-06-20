namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsSyntheticCanonicalizationCaseGroup
{
    InsideRootSimplePath,
    OutsideRootTraversal,
    SiblingRootPrefixTrap,
    CaseVariant,
    MixedSeparators,
    DriveBoundary,
    NetworkShareStylePath,
    SymlinkLikeSegment,
    HiddenSegment,
    UnicodeNormalizationVariant,
    TrailingSeparator,
    EmptyInvalidPath,
    DeepPath,
    MaxLengthPath,
    ReservedNameStylePath,
    RelativePath
}

public enum NodalOsSyntheticContainmentDecision
{
    AllowPreview,
    BlockPreview,
    RequiresReview
}

public sealed record NodalOsSyntheticCanonicalizationCase
{
    public required string CaseId { get; init; }
    public required NodalOsSyntheticCanonicalizationCaseGroup Group { get; init; }
    public required string SyntheticInputPath { get; init; }
    public required string SyntheticRootPath { get; init; }
    public required string DeclaredNormalizedPath { get; init; }
    public required string DeclaredCaseSensitivityMode { get; init; }
    public required string DeclaredPathSeparatorMode { get; init; }
    public required bool DeclaredDriveBoundary { get; init; }
    public required bool DeclaredNetworkShare { get; init; }
    public required bool DeclaredRelativeTraversal { get; init; }
    public required bool DeclaredSymlinkLikeSegment { get; init; }
    public required bool DeclaredUnicodeVariant { get; init; }
    public required bool DeclaredHiddenSegment { get; init; }
    public required NodalOsSyntheticContainmentDecision ExpectedContainmentDecision { get; init; }
    public required bool ExpectedReviewRequirement { get; init; }
    public required string ExpectedBlockedReasonRedacted { get; init; }
    public required bool IsSyntheticOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsRealCanonicalization { get; init; }
}

public sealed record NodalOsSyntheticCanonicalizationMatrix
{
    public required string MatrixId { get; init; }
    public IReadOnlyList<NodalOsSyntheticCanonicalizationCase> Cases { get; init; } = [];
    public required decimal CoveragePercent { get; init; }
    public IReadOnlyList<NodalOsSyntheticCanonicalizationCaseGroup> MissingCaseGroups { get; init; } = [];
    public required bool ReadyForSyntheticCanonicalizationReview { get; init; }
    public required bool ReadyForRealCanonicalization { get; init; }
    public required bool ReadyForRealPathJail { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool CanResolveRealPath { get; init; }
    public required bool CanAccessFilesystem { get; init; }
    public required bool CanReadDirectory { get; init; }
    public required bool CanReadFile { get; init; }
    public required bool CanHashFile { get; init; }
    public required bool CanAuthorizeScan { get; init; }
}

