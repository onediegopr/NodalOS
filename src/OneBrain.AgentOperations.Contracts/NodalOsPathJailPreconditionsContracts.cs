namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsPathJailPreconditionsStatus
{
    NotReady,
    PreconditionsDrafted,
    BlockedMissingCanonicalizationPolicy,
    BlockedMissingContainmentPolicy,
    BlockedMissingSymlinkPolicy,
    BlockedMissingConsentScope,
    BlockedMissingAudit,
    UnknownRequiresReview
}

public sealed record NodalOsPathJailPreconditions
{
    public required string JailPreconditionsId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required string RootPathRef { get; init; }

    public required string RequiredCanonicalizationPolicy { get; init; }

    public required string RequiredPathContainmentPolicy { get; init; }

    public required string RequiredSymlinkPolicy { get; init; }

    public required string RequiredCaseSensitivityPolicy { get; init; }

    public required string RequiredDriveBoundaryPolicy { get; init; }

    public required string RequiredNetworkSharePolicy { get; init; }

    public required string RequiredHiddenFilePolicy { get; init; }

    public required string RequiredExcludedFoldersPolicy { get; init; }

    public required string RequiredMaxDepthPolicy { get; init; }

    public required string RequiredMaxFilesPolicy { get; init; }

    public required string RequiredMaxBytesPolicy { get; init; }

    public required string RequiredNoMutationGuarantee { get; init; }

    public required string RequiredCancellationPolicy { get; init; }

    public required string RequiredEvidencePlan { get; init; }

    public required string RequiredTimelinePlan { get; init; }

    public required string RequiredAuditBeforeEnablement { get; init; }

    public required NodalOsPathJailPreconditionsStatus Status { get; init; }
}

public sealed record NodalOsPathJailReadinessResult
{
    public required string ReadinessId { get; init; }

    public required string JailPreconditionsRef { get; init; }

    public required bool ReadyForRealPathJail { get; init; }

    public required bool ReadyForFilesystemScan { get; init; }

    public required bool ReadyForFileRead { get; init; }

    public required bool ReadyForFileHashing { get; init; }

    public required bool ReadyForDirectoryListing { get; init; }

    public required bool CanResolveRealPath { get; init; }

    public required bool CanReadDirectory { get; init; }

    public required bool CanReadFile { get; init; }

    public required bool CanHashFile { get; init; }

    public required bool CanFollowSymlink { get; init; }

    public required bool CanMutateFilesystem { get; init; }

    public required bool CanCreateIndex { get; init; }

    public required bool CanBuildLlmContext { get; init; }

    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public required string UserFacingExplanationRedacted { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];
}
