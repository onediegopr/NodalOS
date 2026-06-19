namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsSelectorStrategyKind
{
    Semantic,
    DomStableAttribute,
    DomCssPath,
    DomXPath,
    CdpAccessibilityTreeFuture,
    VisualCheckpointFuture,
    OcrTextFallbackFuture
}

public enum NodalOsSelectorRiskKind
{
    Low,
    Medium,
    High,
    Critical
}

public enum NodalOsSelectorSafetyDecision
{
    AllowedForObservationOnly,
    RequiresHumanReview,
    RejectedSensitive,
    RejectedUnstable,
    RejectedMutableIntent,
    RejectedUnsupported
}

public sealed record NodalOsSelectorSafetyPolicy
{
    public required string PolicyId { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool ObservationOnly { get; init; }

    public required bool VisualOcrFallbackOnly { get; init; }

    public required bool RequiresEvidenceRedaction { get; init; }

    public required bool RequiresGlobalPolicyEvaluation { get; init; }

    public IReadOnlyList<NodalOsSelectorStrategyKind> PreferredStrategyOrder { get; init; } = [];

    public IReadOnlyList<string> ForbiddenSelectorMaterial { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsSelectorCandidate
{
    public required string SelectorId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RecipeId { get; init; }

    public string? StepId { get; init; }

    public required NodalOsSelectorStrategyKind StrategyKind { get; init; }

    public required string SelectorPathRedacted { get; init; }

    public string? HumanLabelRedacted { get; init; }

    public required bool ContainsRawSecret { get; init; }

    public required bool ContainsRawCookie { get; init; }

    public required bool ContainsRawHeader { get; init; }

    public required bool ContainsRawBody { get; init; }

    public required bool MutableIntentDetected { get; init; }

    public required double StabilityScore { get; init; }

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsSelectorSafetyEvaluation
{
    public required string EvaluationId { get; init; }

    public required string SelectorId { get; init; }

    public required NodalOsSelectorSafetyDecision Decision { get; init; }

    public required NodalOsSelectorRiskKind RiskKind { get; init; }

    public required bool CanAuthorizeAction { get; init; }

    public required bool ObservationOnly { get; init; }

    public required bool RequiresHumanReview { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public IReadOnlyList<string> Reasons { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public enum NodalOsHumanHandoffUserOptionKind
{
    ContinueAfterUserAction,
    PauseMission,
    ChangeInstruction,
    RetryAfterFix,
    CopyTechnicalLog,
    CancelMission,
    AskForExplanation
}

public sealed record NodalOsHumanHandoffContract
{
    public required string HandoffId { get; init; }

    public string? MissionId { get; init; }

    public string? TaskId { get; init; }

    public string? RecipeId { get; init; }

    public string? StepId { get; init; }

    public required NodalOsAutomationHandoffReason Reason { get; init; }

    public required bool RequiresHumanAction { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool CanAuthorizeAction { get; init; }

    public required string HumanReadableBlockerRedacted { get; init; }

    public string? TechnicalLogRedacted { get; init; }

    public IReadOnlyList<NodalOsHumanHandoffUserOptionKind> UserOptions { get; init; } = [];

    public IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsSelectorSafetyHumanHandoffValidationResult
{
    public required bool IsValid { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool RuntimeExecutionDeferred { get; init; }

    public required bool CanAuthorizeAction { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
