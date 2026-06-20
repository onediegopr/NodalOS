namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsNextPhaseAdrDecision
{
    NoDirectMoveToRealProjectUnderstanding,
    RealScanBlockedUntilFutureMilestone,
    FilesystemScanBlockedUntilGatesDefined,
    LlmContextBuildBlockedUntilFutureMilestone,
    EmbeddingsBlockedUntilFutureMilestone,
    IndexingBlockedUntilFutureMilestone,
    CloudSyncBlockedUntilFutureMilestone,
    AssignmentOutputsAreRefsAndGovernanceContextOnly,
    MockHistoryIsNotSourceOfTruth,
    ByokAndProviderPolicyRequiredBeforeLlm,
    PathJailAndConsentRequiredBeforeFilesystem,
    PositiveExecutionGateRequiredBeforeRuntime,
    SeparateAuditRequiredBeforeRuntime
}

public sealed record NodalOsNextPhaseAdrGuardrail
{
    public required string GuardrailId { get; init; }

    public required string DescriptionRedacted { get; init; }

    public required bool EnforcedStructurally { get; init; }

    public required bool RequiresSeparateMilestone { get; init; }
}

public sealed record NodalOsNextPhaseAdrRequiredMilestone
{
    public required string MilestoneRef { get; init; }

    public required string PurposeRedacted { get; init; }

    public required bool BlocksRealScan { get; init; }

    public required bool BlocksLlmContext { get; init; }

    public required bool BlocksRuntime { get; init; }
}

public sealed record NodalOsNextPhaseAdr
{
    public required string AdrId { get; init; }

    public required string TitleRedacted { get; init; }

    public required string ContextRedacted { get; init; }

    public required string DecisionSummaryRedacted { get; init; }

    public required string ConsequencesRedacted { get; init; }

    public IReadOnlyList<NodalOsNextPhaseAdrDecision> Decisions { get; init; } = [];

    public IReadOnlyList<string> AcceptedAlternativesRedacted { get; init; } = [];

    public IReadOnlyList<string> RejectedAlternativesRedacted { get; init; } = [];

    public IReadOnlyList<NodalOsNextPhaseAdrGuardrail> Guardrails { get; init; } = [];

    public IReadOnlyList<NodalOsNextPhaseAdrRequiredMilestone> RequiredNextMilestones { get; init; } = [];

    public IReadOnlyList<string> ExplicitNonGoalsRedacted { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public required bool RealProjectUnderstandingAllowed { get; init; }

    public required bool RealScanAllowed { get; init; }

    public required bool FilesystemReadAllowed { get; init; }

    public required bool FilesystemHashAllowed { get; init; }

    public required bool GitCommandsAllowed { get; init; }

    public required bool EmbeddingsAllowed { get; init; }

    public required bool IndexingAllowed { get; init; }

    public required bool LlmContextBuildAllowed { get; init; }

    public required bool PromptGenerationAllowed { get; init; }

    public required bool LlmProviderCallAllowed { get; init; }

    public required bool CloudSyncAllowed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
