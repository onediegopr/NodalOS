namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsApprovalCardPreview
{
    public required string PreviewCardId { get; init; }

    public required string ApprovalCardId { get; init; }

    public string? ExecutionRegistryEntryId { get; init; }

    public string? EventId { get; init; }

    public IReadOnlyList<string> TimelineEntryIds { get; init; } = [];

    public required string TitleRedacted { get; init; }

    public required string ShortSummaryRedacted { get; init; }

    public required string FullExplanationRedacted { get; init; }

    public required NodalOsApprovalSeverity Severity { get; init; }

    public required NodalOsApprovalStatus Status { get; init; }

    public required NodalOsApprovalActionKind RequestedAction { get; init; }

    public IReadOnlyList<string> AffectedResourcesRedacted { get; init; } = [];

    public required string PolicyGateReasonRedacted { get; init; }

    public IReadOnlyList<NodalOsApprovalUserOptionKind> UserOptions { get; init; } = [];

    public required bool RollbackAvailable { get; init; }

    public string? NoRollbackReasonRedacted { get; init; }

    public required string ExpectedEvidenceRedacted { get; init; }

    public required bool RequiresAttention { get; init; }

    public required bool Blocked { get; init; }

    public required bool RequiresHuman { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsApprovalUxPreview
{
    public required string PreviewId { get; init; }

    public required string ProjectOperationalName { get; init; }

    public IReadOnlyList<NodalOsApprovalCardPreview> Cards { get; init; } = [];

    public IReadOnlyList<NodalOsTimelineEntry> TimelineEntries { get; init; } = [];

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsHandoffDataPack
{
    public required string PackId { get; init; }

    public required string ProjectOperationalName { get; init; }

    public string? Milestone { get; init; }

    public string? CurrentDecision { get; init; }

    public required string RequestedSummaryRedacted { get; init; }

    public required string DecisionSummaryRedacted { get; init; }

    public IReadOnlyList<string> ProposedActionsRedacted { get; init; } = [];

    public IReadOnlyList<string> PendingItemsRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsExecutionRegistryEntry> RegistryEntries { get; init; } = [];

    public IReadOnlyList<NodalOsApprovalCardPreview> ApprovalPreviews { get; init; } = [];

    public IReadOnlyList<NodalOsApprovalDecision> ApprovalDecisions { get; init; } = [];

    public IReadOnlyList<NodalOsTimelineEntry> TimelineEntries { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> FailuresRedacted { get; init; } = [];

    public IReadOnlyList<string> HumanHandoffRequirementsRedacted { get; init; } = [];

    public required string RedactionSummaryRedacted { get; init; }

    public required string GuardrailsSummaryRedacted { get; init; }

    public string? TestsValidationSummaryRedacted { get; init; }

    public IReadOnlyList<string> NextStepsRedacted { get; init; } = [];

    public required bool CloudRequired { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsRuntimeObservabilityReport
{
    public required string ReportId { get; init; }

    public required string ProjectOperationalName { get; init; }

    public required string UserRequestRedacted { get; init; }

    public required string SystemInterpretationRedacted { get; init; }

    public IReadOnlyList<string> RegistryEntryIds { get; init; } = [];

    public IReadOnlyList<string> EventIds { get; init; } = [];

    public IReadOnlyList<string> TimelineEntryIds { get; init; } = [];

    public IReadOnlyList<string> ApprovalCardIds { get; init; } = [];

    public IReadOnlyList<string> EvidenceIds { get; init; } = [];

    public required string ExecutionRegistrySummaryRedacted { get; init; }

    public required string EventBusSummaryRedacted { get; init; }

    public required string TimelineSummaryRedacted { get; init; }

    public required string ApprovalSummaryRedacted { get; init; }

    public required string EvidenceSummaryRedacted { get; init; }

    public required string RedactionAppliedSummaryRedacted { get; init; }

    public required string GuardrailsSummaryRedacted { get; init; }

    public IReadOnlyList<string> BlockedActionsRedacted { get; init; } = [];

    public IReadOnlyList<string> FailuresRedacted { get; init; } = [];

    public IReadOnlyList<string> WarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> HumanHandoffRequirementsRedacted { get; init; } = [];

    public required string ValidationSummaryRedacted { get; init; }

    public required string NextRecommendedActionRedacted { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool UiRequired { get; init; }

    public required bool CloudRequired { get; init; }

    public required bool LlmProviderCallRequired { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
