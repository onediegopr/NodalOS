namespace OneBrain.Core.Recipes;

public enum RecipeLifecycleStage
{
    Preflight,
    Prepare,
    Perceive,
    Plan,
    Act,
    Verify,
    Recover,
    Evidence,
    Cleanup,
    Handoff
}

public enum RecipeBlockType
{
    BrowserGoal,
    BrowserAction,
    DesktopActionDraft,
    Extract,
    Validate,
    Wait,
    Conditional,
    Loop,
    HumanIntervention,
    Approval,
    FileDownloadEvidence,
    CaptureArtifact,
    ConnectorDraft,
    WorkitemPop,
    WorkitemUpdate,
    WorkitemCreateNextStage,
    Cleanup
}

public enum RecipeRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum RecipeApprovalRequirement
{
    None,
    Required,
    RequiredBeforeLive,
    RequiredForSensitiveSurface,
    HumanIntervention
}

public enum RecipeRunStatus
{
    Created,
    ReadyForPreview,
    DryRunPreview,
    FixtureRunning,
    AssistedWaitingForHuman,
    Succeeded,
    RetryScheduled,
    Failed,
    Cancelled,
    BlockedByPolicy
}

public enum RecipeRunMode
{
    CatalogPreview,
    DryRun,
    FixtureRun,
    AssistedRun,
    LiveRunBlocked,
    LiveRunAllowedFuture
}

public enum RecipeRunStepResult
{
    NotStarted,
    Previewed,
    DryRunPlanned,
    FixturePassed,
    Succeeded,
    RetryScheduled,
    Failed,
    NeedsHuman,
    Blocked
}

public enum RecipeReadinessStatus
{
    ReadyForPreview,
    ReadyForDryRun,
    ReadyForFixtureRun,
    BlockedMissingLimits,
    BlockedMissingValidation,
    BlockedMissingEvidencePolicy,
    BlockedMissingApprovalPolicy,
    BlockedMissingToolTrust,
    BlockedMissingSecretReference,
    BlockedLiveRuntimeDisabled,
    BlockedByProtectedScope
}

public sealed record RecipeReference(string RefId, string Kind, bool SecretValueExposed = false);

public sealed record RecipeBlock(
    string BlockId,
    RecipeBlockType BlockType,
    string Label,
    string Intent,
    string? TargetRef,
    string? InputBinding,
    string? OutputBinding,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> Postconditions,
    IReadOnlyList<string> ValidationRefs,
    RecipeRiskLevel RiskLevel,
    RecipeApprovalRequirement ApprovalRequirement,
    string? EvidenceExpectationRef,
    string? FailurePolicyRef,
    IReadOnlyList<string> NextBlockRefs);

public sealed record RecipeReadinessResult(
    bool IsReady,
    RecipeReadinessStatus Status,
    IReadOnlyList<string> Reasons,
    bool LiveRuntimeEnabled);

public sealed record RecipeRun(
    string RunId,
    string RecipeId,
    string RecipeVersion,
    string? MissionIdRef,
    RecipeRunStatus Status,
    RecipeRunMode Mode,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? CurrentBlockId,
    int StepCount,
    int AttemptCount,
    string? EvidencePackRef,
    IReadOnlyList<string> TimelineRefs,
    IReadOnlyList<string> ApprovalRefs,
    IReadOnlyList<string> WorkitemRefs,
    string? FailureSummary,
    RecipeReadinessResult ReadinessResult)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeRunStep(
    string StepId,
    string RunId,
    string BlockId,
    int SequenceNumber,
    string IntendedAction,
    string? ResolvedTargetSummary,
    string? StateBeforeRef,
    string? StateAfterRef,
    RecipeRunStepResult Result,
    IReadOnlyList<string> ValidationResultRefs,
    IReadOnlyList<string> EvidenceRefs,
    WorkitemFailureType? FailureType,
    WorkitemRetryDecision RetryDecision,
    string? ApprovalDecisionRef)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public static class RecipeReadinessEvaluator
{
    public static RecipeReadinessResult Evaluate(RecipeDefinition definition, RecipeRunMode mode)
    {
        if (mode is RecipeRunMode.LiveRunBlocked or RecipeRunMode.LiveRunAllowedFuture)
        {
            return Blocked(RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Live runtime is disabled in this foundation block.");
        }

        if (string.IsNullOrWhiteSpace(definition.LimitsRef))
            return Blocked(RecipeReadinessStatus.BlockedMissingLimits, "Recipe limits reference is required.");

        if (definition.Blocks.Any(block => RequiresValidation(block.BlockType) && block.ValidationRefs.Count == 0))
            return Blocked(RecipeReadinessStatus.BlockedMissingValidation, "Action-like recipe blocks require validation references.");

        if (definition.EvidenceExpectationRefs.Count == 0)
            return Blocked(RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Evidence expectation reference is required.");

        if (definition.RequiredToolTrustRefs.Count == 0)
            return Blocked(RecipeReadinessStatus.BlockedMissingToolTrust, "Tool trust reference is required.");

        if (definition.RequiredSecretRefs.Any(string.IsNullOrWhiteSpace))
            return Blocked(RecipeReadinessStatus.BlockedMissingSecretReference, "Secret references must be ids only.");

        return new RecipeReadinessResult(true, RecipeReadinessStatus.ReadyForDryRun, [], LiveRuntimeEnabled: false);
    }

    private static RecipeReadinessResult Blocked(RecipeReadinessStatus status, string reason) =>
        new(false, status, [reason], LiveRuntimeEnabled: false);

    private static bool RequiresValidation(RecipeBlockType blockType) =>
        blockType is RecipeBlockType.BrowserAction or
            RecipeBlockType.DesktopActionDraft or
            RecipeBlockType.ConnectorDraft or
            RecipeBlockType.WorkitemUpdate or
            RecipeBlockType.WorkitemCreateNextStage;
}
