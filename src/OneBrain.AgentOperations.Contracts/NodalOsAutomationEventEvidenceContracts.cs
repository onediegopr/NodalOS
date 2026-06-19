namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsAutomationEventKind
{
    AutomationStepStarted,
    AutomationStepCompleted,
    AutomationStepFailed,
    AutomationHandoffRequired,
    SelectorChanged,
    FallbackUsed,
    EvidenceCaptured,
    AutomationWarningRaised
}

public enum NodalOsAutomationEvidenceKind
{
    SelectorEvidence,
    DomSnapshotRedacted,
    ScreenshotReferenceFuture,
    StepLog,
    NetworkMetadataRedacted,
    HumanNote,
    FallbackEvidence,
    HandoffEvidence
}

public enum NodalOsAutomationHandoffReason
{
    LoginRequired,
    CaptchaRequired,
    TwoFactorRequired,
    CredentialRequired,
    AmbiguousState,
    PolicyBlocked,
    SelectorUnstable,
    ExternalServiceBlocked,
    UserDecisionRequired
}

public sealed record NodalOsAutomationEvent
{
    public required string EventId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RecipeId { get; init; }

    public string? StepId { get; init; }

    public required NodalOsAutomationEventKind Kind { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public string? HumanSummary { get; init; }

    public string? TechnicalSummary { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsAutomationEvidence
{
    public required string EvidenceId { get; init; }

    public string? EventId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RecipeId { get; init; }

    public string? StepId { get; init; }

    public required NodalOsAutomationEvidenceKind Kind { get; init; }

    public required bool Redacted { get; init; }

    public required bool ContainsRawSecret { get; init; }

    public required bool ContainsRawCookie { get; init; }

    public required bool ContainsRawHeader { get; init; }

    public required bool ContainsRawBody { get; init; }

    public string? SelectorPath { get; init; }

    public string? DomSnapshotRedacted { get; init; }

    public string? ScreenshotRefFuture { get; init; }

    public string? StepLogRedacted { get; init; }

    public string? NetworkMetadataRedacted { get; init; }

    public string? HumanNoteRedacted { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsAutomationHandoffState
{
    public required string HandoffId { get; init; }

    public string? EventId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RecipeId { get; init; }

    public string? StepId { get; init; }

    public required NodalOsAutomationHandoffReason Reason { get; init; }

    public required bool RequiresHumanAction { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool CanContinueAfterHumanAction { get; init; }

    public string? HumanReadableBlocker { get; init; }

    public IReadOnlyList<string> UserOptions { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsAutomationEventEvidenceValidationResult
{
    public required bool IsValid { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
