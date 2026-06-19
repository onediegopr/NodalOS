namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsMissionControlEmptyStateKind
{
    NoMissionSelected,
    NoActiveMission,
    NoTimelineEvents,
    NoApprovalsPending,
    NoEvidenceAvailable,
    NoObservabilityReportYet,
    NoWorkspaceSelected,
    NoUiInteractionHistory,
    NoApprovalDraft,
    NoSelectedEvidenceRef,
    NoGuardrailWarnings,
    RuntimeUnavailableByDesign,
    LlmNotConfiguredByDesign,
    CloudSyncDisabledByDesign,
    BrowserAutomationDeferredByDesign
}

public enum NodalOsMissionControlGuidanceSeverity
{
    Informational,
    Attention,
    Blocked,
    Critical
}

public enum NodalOsMissionControlOnboardingTarget
{
    MissionControl,
    Timeline,
    Approvals,
    Evidence,
    ObservabilityLog,
    Guardrails,
    WorkspaceFuture,
    LlmByokFuture,
    RuntimeFuture
}

public enum NodalOsMissionControlGuidanceLevel
{
    Beginner,
    Normal,
    Advanced
}

public enum NodalOsMissionControlOnboardingState
{
    NotStarted,
    Active,
    CompletedMock,
    DismissedMock,
    ReopenedMock
}

public enum NodalOsMissionControlGuardrailBlockingCategory
{
    BlocksRuntime,
    BlocksCloud,
    BlocksLlmByok,
    BlocksBrowserAutomation,
    Informational
}

public sealed record NodalOsMissionControlEmptyState
{
    public required string EmptyStateId { get; init; }

    public required NodalOsMissionControlEmptyStateKind Kind { get; init; }

    public required string TitleRedacted { get; init; }

    public required string ShortDescriptionRedacted { get; init; }

    public required string UserFriendlyExplanationRedacted { get; init; }

    public required string RecommendedNextSafeStepRedacted { get; init; }

    public string? DisabledActionLabelRedacted { get; init; }

    public string? DisabledReasonRedacted { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required NodalOsMissionControlGuidanceSeverity Severity { get; init; }

    public required bool RequiresAttention { get; init; }

    public required bool CanExecuteAction { get; init; }

    public required bool IsReadOnly { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsMissionControlOnboardingStep
{
    public required string StepId { get; init; }

    public required NodalOsMissionControlOnboardingTarget Target { get; init; }

    public required string TitleRedacted { get; init; }

    public required string ExplanationRedacted { get; init; }

    public required string SafeNextActionRedacted { get; init; }

    public string? DisabledFutureWorkExplanationRedacted { get; init; }

    public required NodalOsMissionControlGuidanceLevel GuidanceLevel { get; init; }

    public required NodalOsMissionControlOnboardingState State { get; init; }

    public required bool IsNoOp { get; init; }

    public required bool DismissReopenMockSafe { get; init; }

    public required bool ProductivePersistenceAllowed { get; init; }

    public required bool TelemetryAllowed { get; init; }

    public required bool CloudCallAllowed { get; init; }

    public required bool LlmProviderCallAllowed { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record NodalOsMissionControlGuardrailExplainer
{
    public required string GuardrailId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string PlainLanguageExplanationRedacted { get; init; }

    public required string TechnicalReasonRedacted { get; init; }

    public required string AffectedSurfaceRedacted { get; init; }

    public required string WhatUserCanDoNowRedacted { get; init; }

    public required string WhatIsDeferredRedacted { get; init; }

    public required NodalOsMissionControlGuidanceSeverity Severity { get; init; }

    public required NodalOsMissionControlGuardrailBlockingCategory BlockingCategory { get; init; }

    public IReadOnlyList<string> ReferenceIds { get; init; } = [];

    public required bool CanUnlockExecution { get; init; }

    public required bool CanChangePolicy { get; init; }

    public required bool CanMutateRegistry { get; init; }

    public required bool CanCreateException { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
