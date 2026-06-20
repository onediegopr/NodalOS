namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsSyntheticPolicyRegressionCategory
{
    PathContainment,
    TraversalBlocking,
    PrefixTrapBlocking,
    CaseSensitivity,
    DriveBoundary,
    NetworkShareStylePath,
    SymlinkLikeSegment,
    HiddenPath,
    UnicodeNormalization,
    ExclusionGroups,
    SensitiveLikeCategories,
    NoMutationForbiddenOperations,
    NoLlmCloudExposure,
    NoFilesystemAccess,
    AuditRequiredCases
}

public enum NodalOsSyntheticPolicyRegressionDecision
{
    AllowPreview,
    BlockPreview,
    RequiresReview,
    RequiresRedaction,
    RequiresAudit
}

public sealed record NodalOsSyntheticPolicyRegressionCase
{
    public required string CaseId { get; init; }
    public required NodalOsSyntheticPolicyRegressionCategory Category { get; init; }
    public required string SyntheticInputRef { get; init; }
    public required NodalOsSyntheticPolicyRegressionDecision ExpectedDecision { get; init; }
    public required string ExpectedBlockedReasonRedacted { get; init; }
    public required bool ExpectedReviewRequirement { get; init; }
    public required bool ExpectedRedactionRequirement { get; init; }
    public required bool NeverSentToLlm { get; init; }
    public required bool NeverSentToCloud { get; init; }
    public required bool IsSyntheticOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
}

public sealed record NodalOsSyntheticPolicyRegressionPack
{
    public required string RegressionPackId { get; init; }
    public required string GateRef { get; init; }
    public required string CanonicalizationMatrixRef { get; init; }
    public required string NoMutationProofRef { get; init; }
    public required string FixtureCoverageRef { get; init; }
    public required string SecretDetectionPolicyPreviewRef { get; init; }
    public required string ExclusionPolicyPackRef { get; init; }
    public required bool UsesSyntheticFixturesOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsRealCanonicalization { get; init; }
    public required bool PerformsDirectoryListing { get; init; }
    public required bool PerformsFileRead { get; init; }
    public required bool PerformsFileHash { get; init; }
    public required bool PerformsMutation { get; init; }
    public required bool BuildsLlmContext { get; init; }
    public required bool CallsProvider { get; init; }
    public required bool UsesCloud { get; init; }
    public IReadOnlyList<NodalOsSyntheticPolicyRegressionCase> Cases { get; init; } = [];
    public required NodalOsSyntheticPolicyRegressionResult Result { get; init; }
}

public sealed record NodalOsSyntheticPolicyRegressionResult
{
    public required string ResultId { get; init; }
    public required bool ReadyForSyntheticRegression { get; init; }
    public required bool ReadyForRealPathJail { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForIndexing { get; init; }
    public required bool ReadyForRepresentationBuild { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public IReadOnlyList<string> PassingSyntheticCases { get; init; } = [];
    public IReadOnlyList<string> FailingSyntheticCases { get; init; } = [];
    public IReadOnlyList<NodalOsSyntheticPolicyRegressionCategory> MissingSyntheticCases { get; init; } = [];
    public required string UserFacingSummaryRedacted { get; init; }
}

