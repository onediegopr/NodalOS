namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsFixtureResultReviewStatus
{
    AcceptedSynthetic,
    Mismatch,
    NeedsFixtureUpdate,
    NeedsPolicyClarification,
    NeedsAudit
}

public enum NodalOsFixtureResultReviewOption
{
    AcceptSyntheticResult,
    MarkMismatch,
    RequestFixtureUpdate,
    RequestPolicyClarification,
    MarkNeedsAudit,
    ExportReviewSummary
}

public sealed record NodalOsReviewedFixtureResult
{
    public required string FixtureId { get; init; }
    public required NodalOsScanFixtureCategory Category { get; init; }
    public required NodalOsScanFixtureExpectedDisposition ExpectedOutcome { get; init; }
    public required NodalOsScanFixtureExpectedDisposition SimulatedOutcome { get; init; }
    public required bool MatchesExpectation { get; init; }
    public required NodalOsFixtureResultReviewStatus ReviewStatus { get; init; }
    public required string ReasonRedacted { get; init; }
    public required string RedactionStatusRedacted { get; init; }
    public required bool NeverSentToLlm { get; init; }
    public required bool NeverSentToCloud { get; init; }
}

public sealed record NodalOsFixtureResultReview
{
    public required string ReviewId { get; init; }
    public required string SimulatorResultRef { get; init; }
    public required string FixtureMatrixRef { get; init; }
    public IReadOnlyList<NodalOsReviewedFixtureResult> ReviewedFixtureResults { get; init; } = [];
    public IReadOnlyList<string> OpenQuestionsRedacted { get; init; } = [];
    public IReadOnlyList<string> MismatchesRedacted { get; init; } = [];
    public IReadOnlyList<string> RisksRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredPolicyUpdatesRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredFixtureUpdatesRedacted { get; init; } = [];
    public required bool IsReviewOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool CanAuthorizeRealScan { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeIndexing { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
}

public sealed record NodalOsFixtureResultReviewActionResult
{
    public required string ActionResultId { get; init; }
    public required NodalOsFixtureResultReviewOption Option { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool MutatesState { get; init; }
    public required bool AuthorizesRealScan { get; init; }
    public required bool AuthorizesFilesystemAccess { get; init; }
    public required bool AuthorizesIndexing { get; init; }
    public required bool AuthorizesLlmContext { get; init; }
    public required string UserFacingExplanationRedacted { get; init; }
}

public sealed record NodalOsFixtureResultReviewReadiness
{
    public required string ReadinessId { get; init; }
    public required string ReviewRef { get; init; }
    public required bool ReadyForSyntheticFixtureReview { get; init; }
    public required bool ReadyForRealScan { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
}
