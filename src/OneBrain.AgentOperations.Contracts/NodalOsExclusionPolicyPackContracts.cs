namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsExclusionPolicyGroup
{
    DependencyFolders,
    BuildOutputs,
    CacheFolders,
    VcsMetadata,
    BinaryMediaHeavyFolders,
    EnvironmentFiles,
    SecretLikeFiles,
    GeneratedArtifacts,
    Logs,
    TemporaryFiles,
    VendorFolders,
    NodeModulesLikeFolders,
    BinObjLikeFolders
}

public enum NodalOsExclusionSeverity
{
    Info,
    Review,
    Sensitive,
    Required
}

public sealed record NodalOsExclusionRulePreview
{
    public required string RuleId { get; init; }

    public required NodalOsExclusionPolicyGroup Group { get; init; }

    public required string PatternDisplayRedacted { get; init; }

    public required string ReasonRedacted { get; init; }

    public required NodalOsExclusionSeverity Severity { get; init; }

    public required bool CanUserOverride { get; init; }

    public required bool RequiresReview { get; init; }

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];
}

public sealed record NodalOsExclusionPolicyPack
{
    public required string ExclusionPolicyId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required string ScopePreviewRef { get; init; }

    public required bool IsPreviewOnly { get; init; }

    public required bool UsesRealFilesystem { get; init; }

    public required bool DirectoryListingPerformed { get; init; }

    public required bool FileReadPerformed { get; init; }

    public IReadOnlyList<NodalOsExclusionRulePreview> Rules { get; init; } = [];
}

public sealed record NodalOsExclusionPolicyReadinessResult
{
    public required string ReadinessId { get; init; }

    public required string ExclusionPolicyRef { get; init; }

    public required bool ReadyForRealExclusionEnforcement { get; init; }

    public required bool ReadyForRealScan { get; init; }

    public required bool ReadyForIndexing { get; init; }

    public required bool ReadyForVectorization { get; init; }

    public required bool CanReadDirectory { get; init; }

    public required bool CanReadFile { get; init; }

    public required bool CanApplyToRealFilesystem { get; init; }

    public required bool CanCreateIndex { get; init; }

    public required bool CanBuildLlmContext { get; init; }

    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public required string UserFacingExplanationRedacted { get; init; }
}
