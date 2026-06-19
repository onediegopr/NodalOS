namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsScheduledReadOnlyScheduleStatus
{
    Draft,
    PolicyReviewRequired,
    EvidenceRequirementsReviewRequired,
    ApprovalRequiredIfSensitive,
    DryRunPreviewPrepared,
    AwaitingManualTrigger,
    ScheduledReadOnlyFuture,
    ReportProduced,
    Blocked,
    Cancelled
}

public enum NodalOsScheduledReadOnlyFrequencyKind
{
    ManualOnly,
    OnceFuture,
    HourlyFuture,
    DailyFuture,
    WeeklyFuture,
    MonthlyFuture
}

public sealed record NodalOsScheduledReadOnlySchedule
{
    public required string ScheduleId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RecipeId { get; init; }

    public string? SkillId { get; init; }

    public string? PackageId { get; init; }

    public required string HumanOwner { get; init; }

    public required NodalOsScheduledReadOnlyScheduleStatus Status { get; init; }

    public required NodalOsScheduledReadOnlyFrequencyKind FrequencyKind { get; init; }

    public required bool ReadOnly { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public IReadOnlyList<string> AllowedTargets { get; init; } = [];

    public IReadOnlyList<string> EvidenceRequirements { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public string? Summary { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsScheduledReadOnlyRunRequest
{
    public required string RequestId { get; init; }

    public required string ScheduleId { get; init; }

    public required bool ManualTriggerRequired { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool ReadOnly { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsScheduledReadOnlyPreview
{
    public required string PreviewId { get; init; }

    public required string ScheduleId { get; init; }

    public required bool DryRunOnly { get; init; }

    public required bool Executed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public IReadOnlyList<string> PlannedReadOnlyOperations { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsScheduledReadOnlyValidationResult
{
    public required bool IsValid { get; init; }

    public required bool CanPassSchedulePolicy { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
