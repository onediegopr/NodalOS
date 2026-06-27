namespace OneBrain.Core.Recipes;

public enum WorkitemStatus
{
    New,
    Ready,
    Processing,
    Succeeded,
    RetryScheduled,
    FailedBusiness,
    FailedApplication,
    FailedPolicy,
    FailedValidation,
    NeedsHuman,
    Cancelled,
    Skipped
}

public enum WorkitemFailureType
{
    Business,
    Application,
    Policy,
    Validation,
    Perception,
    Locator,
    Auth,
    Challenge,
    Timeout,
    RateLimit,
    ExternalSystem,
    Unknown
}

public enum WorkitemBackoffStrategy
{
    None,
    FixedDelay,
    Linear,
    Exponential
}

public enum WorkitemRetryDecisionKind
{
    DoNotRetry,
    RetryScheduled,
    NeedsHuman,
    Blocked
}

public sealed record WorkitemPayloadRef(
    string RefId,
    string? JsonPayloadRedacted = null,
    string? SchemaRef = null,
    bool ContainsRawSecretValue = false);

public sealed record WorkitemAttachmentRef(
    string AttachmentId,
    string Kind,
    string EvidenceRef,
    bool RawBytesEmbedded = false);

public sealed record WorkitemRetryPolicy(
    int MaxAttempts,
    WorkitemBackoffStrategy BackoffStrategy,
    TimeSpan BaseDelay,
    IReadOnlySet<WorkitemFailureType> RetryableFailureTypes,
    IReadOnlySet<WorkitemFailureType> NonRetryableFailureTypes)
{
    public static WorkitemRetryPolicy Default { get; } =
        new(
            MaxAttempts: 3,
            WorkitemBackoffStrategy.FixedDelay,
            TimeSpan.FromMinutes(5),
            RetryableFailureTypes: new HashSet<WorkitemFailureType>
            {
                WorkitemFailureType.Application,
                WorkitemFailureType.Timeout,
                WorkitemFailureType.RateLimit,
                WorkitemFailureType.ExternalSystem
            },
            NonRetryableFailureTypes: new HashSet<WorkitemFailureType>
            {
                WorkitemFailureType.Business,
                WorkitemFailureType.Policy,
                WorkitemFailureType.Validation,
                WorkitemFailureType.Auth,
                WorkitemFailureType.Challenge
            });
}

public sealed record WorkitemRetryDecision(
    WorkitemRetryDecisionKind Decision,
    WorkitemStatus ResultingStatus,
    DateTimeOffset? NextRunAt,
    string Reason,
    bool LiveExecutionEnabled = false,
    bool ActionAuthorityGranted = false);

public sealed record MissionWorkItem(
    string ItemId,
    string QueueId,
    string QueueName,
    string? RecipeId,
    string? MissionId,
    WorkitemPayloadRef Payload,
    IReadOnlyList<WorkitemAttachmentRef> AttachmentRefs,
    int Priority,
    WorkitemStatus Status,
    DateTimeOffset? NextRunAt,
    int AttemptCount,
    int MaxAttempts,
    string? LockedBy,
    DateTimeOffset? LockedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    WorkitemFailureType? FailureType,
    string? FailureReason,
    string? BusinessKey,
    string? IdempotencyKey,
    string? EvidencePackRef,
    IReadOnlyList<string> TimelineEventRefs,
    string? ParentItemRef,
    IReadOnlyList<string> ChildItemRefs)
{
    public bool LiveExecutionEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeRunItem(
    string RunItemId,
    string RecipeRunId,
    string WorkitemId,
    WorkitemStatus Status,
    string? EvidencePackRef,
    IReadOnlyList<string> TimelineEventRefs);

public sealed record WorkitemQueueContract(
    string QueueId,
    string QueueName,
    WorkitemRetryPolicy RetryPolicy,
    bool LiveExecutionEnabled = false,
    bool SchedulerEnabled = false,
    bool BackgroundWorkerEnabled = false);

public static class WorkitemRetryPolicyEvaluator
{
    public static WorkitemRetryDecision Decide(
        MissionWorkItem item,
        WorkitemFailureType failureType,
        WorkitemRetryPolicy policy,
        DateTimeOffset observedAt)
    {
        if (failureType is WorkitemFailureType.Policy or WorkitemFailureType.Auth or WorkitemFailureType.Challenge)
        {
            return new WorkitemRetryDecision(
                WorkitemRetryDecisionKind.NeedsHuman,
                WorkitemStatus.NeedsHuman,
                null,
                "Policy, auth, and challenge failures require human intervention or remain blocked.");
        }

        if (policy.NonRetryableFailureTypes.Contains(failureType))
        {
            return new WorkitemRetryDecision(
                WorkitemRetryDecisionKind.DoNotRetry,
                StatusForNonRetryable(failureType),
                null,
                "Failure type is non-retryable by policy.");
        }

        if (item.AttemptCount >= policy.MaxAttempts || !policy.RetryableFailureTypes.Contains(failureType))
        {
            return new WorkitemRetryDecision(
                WorkitemRetryDecisionKind.DoNotRetry,
                WorkitemStatus.FailedApplication,
                null,
                "Retry policy does not allow another attempt.");
        }

        return new WorkitemRetryDecision(
            WorkitemRetryDecisionKind.RetryScheduled,
            WorkitemStatus.RetryScheduled,
            CalculateNextRunAt(policy, observedAt, item.AttemptCount),
            "Retry scheduled by policy representation.");
    }

    private static DateTimeOffset CalculateNextRunAt(WorkitemRetryPolicy policy, DateTimeOffset observedAt, int attemptCount) =>
        policy.BackoffStrategy switch
        {
            WorkitemBackoffStrategy.None => observedAt,
            WorkitemBackoffStrategy.FixedDelay => observedAt.Add(policy.BaseDelay),
            WorkitemBackoffStrategy.Linear => observedAt.Add(TimeSpan.FromTicks(policy.BaseDelay.Ticks * Math.Max(1, attemptCount + 1))),
            WorkitemBackoffStrategy.Exponential => observedAt.Add(TimeSpan.FromTicks(policy.BaseDelay.Ticks * (1L << Math.Min(10, Math.Max(0, attemptCount))))),
            _ => observedAt.Add(policy.BaseDelay)
        };

    private static WorkitemStatus StatusForNonRetryable(WorkitemFailureType failureType) =>
        failureType switch
        {
            WorkitemFailureType.Business => WorkitemStatus.FailedBusiness,
            WorkitemFailureType.Validation => WorkitemStatus.FailedValidation,
            WorkitemFailureType.Policy => WorkitemStatus.FailedPolicy,
            _ => WorkitemStatus.FailedApplication
        };
}
