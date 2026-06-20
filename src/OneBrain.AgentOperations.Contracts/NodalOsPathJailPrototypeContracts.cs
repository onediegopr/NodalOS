namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsPathJailPrototypePathKind
{
    SyntheticRoot,
    SyntheticSource,
    SyntheticDependencyFolder,
    SyntheticGeneratedOutput,
    SyntheticHiddenItem,
    SyntheticEnvironmentMarker,
    SyntheticBinaryMedia,
    SyntheticSymlinkLike,
    SyntheticOutsideJail,
    SyntheticCaseVariant,
    SyntheticDeepTree,
    Unknown
}

public enum NodalOsPathJailPrototypeExpectedDecision
{
    AllowedForFuturePreview,
    BlockedOutsideJail,
    BlockedSymlinkLike,
    BlockedNetworkShare,
    BlockedDriveBoundary,
    RequiresReview,
    BlockedUnknown
}

public sealed record NodalOsPathJailPrototypeContract
{
    public required string PrototypeId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string RootPathRef { get; init; }

    public required bool SyntheticRootOnly { get; init; }

    public required bool UsesRealFilesystem { get; init; }

    public required bool PerformsRealCanonicalization { get; init; }

    public required bool PerformsDirectoryListing { get; init; }

    public required bool PerformsFileRead { get; init; }

    public required bool PerformsFileHash { get; init; }

    public required bool CanMutateFilesystem { get; init; }

    public required bool CanAuthorizeScan { get; init; }

    public required bool IsPrototypeOnly { get; init; }
}

public sealed record NodalOsPathJailCandidatePreview
{
    public required string CandidateId { get; init; }

    public required string SyntheticPathRedacted { get; init; }

    public required NodalOsPathJailPrototypePathKind PathKind { get; init; }

    public required bool DeclaredInsideJail { get; init; }

    public required bool DeclaredOutsideJail { get; init; }

    public required bool DeclaredSymlink { get; init; }

    public required bool DeclaredCaseVariant { get; init; }

    public required bool DeclaredNetworkShare { get; init; }

    public required bool DeclaredDriveBoundary { get; init; }

    public required NodalOsPathJailPrototypeExpectedDecision ExpectedPolicyDecision { get; init; }

    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsPathJailPolicyDecisionPreview
{
    public required string DecisionPreviewId { get; init; }

    public required string CandidateRef { get; init; }

    public required bool AllowedForFutureScanPreview { get; init; }

    public required string BlockedReasonRedacted { get; init; }

    public required bool RequiresUserReview { get; init; }

    public required bool RequiresAudit { get; init; }

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}

public sealed record NodalOsPathJailPrototypeReadiness
{
    public required string ReadinessId { get; init; }

    public required string PrototypeRef { get; init; }

    public required bool ReadyForRealPathJail { get; init; }

    public required bool ReadyForRealCanonicalization { get; init; }

    public required bool ReadyForDirectoryListing { get; init; }

    public required bool ReadyForFileRead { get; init; }

    public required bool ReadyForFileHash { get; init; }

    public required bool ReadyForRealScan { get; init; }

    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];
}
