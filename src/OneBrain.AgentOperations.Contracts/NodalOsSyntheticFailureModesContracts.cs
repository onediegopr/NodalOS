namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsSyntheticFailureCategory
{
    MissingConsent,
    MissingAudit,
    PathOutsideJail,
    TraversalAttempt,
    SymlinkLikeUncertainty,
    CaseAmbiguity,
    NetworkShareStylePath,
    HiddenSensitiveSegment,
    SensitiveLikeMarker,
    ExcludedFolder,
    TooManyFiles,
    TooLargeScope,
    CancellationRequested,
    NoMutationProofMissing,
    RedactionPolicyMissing,
    CloudDisabled,
    LlmDisabled,
    ProviderPolicyMissing,
    RuntimeGateMissing
}

public sealed record NodalOsSyntheticFailureMode
{
    public required string FailureModeId { get; init; }
    public required NodalOsOperationalCapability Capability { get; init; }
    public required NodalOsSyntheticFailureCategory Scenario { get; init; }
    public required string SyntheticInputRef { get; init; }
    public required string ExpectedFailureReasonRedacted { get; init; }
    public required NodalOsOperationalCapability ExpectedBlockedCapability { get; init; }
    public required string ExpectedUserMessageRedacted { get; init; }
    public required string ExpectedEvidenceRef { get; init; }
    public required string ExpectedTimelineRef { get; init; }
    public required bool IsSyntheticOnly { get; init; }
    public required bool UsesRealFilesystem { get; init; }
    public required bool PerformsRealOperation { get; init; }
}

public sealed record NodalOsSyntheticFailureBehavior
{
    public required bool FailClosed { get; init; }
    public required bool RequiresUserFacingExplanation { get; init; }
    public required bool EmitsSyntheticEvidenceOnly { get; init; }
    public required bool EmitsSyntheticTimelineOnly { get; init; }
    public required bool DoesNotRetryAutomatically { get; init; }
    public required bool DoesNotEscalateToRuntime { get; init; }
}

public sealed record NodalOsSyntheticFailureModeMatrix
{
    public required string MatrixId { get; init; }
    public IReadOnlyList<NodalOsSyntheticFailureMode> FailureModes { get; init; } = [];
    public required decimal CoveragePercent { get; init; }
    public IReadOnlyList<NodalOsSyntheticFailureCategory> MissingFailureCategories { get; init; } = [];
    public required bool ReadyForSyntheticFailureReview { get; init; }
    public required bool ReadyForRealFailureHandling { get; init; }
    public required bool ReadyForRealFilesystemAccess { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required NodalOsSyntheticFailureBehavior Behavior { get; init; }
}

