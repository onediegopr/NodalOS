namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsProjectUnderstandingImplementationBoundaryDecision
{
    public required string DecisionId { get; init; }

    public required string DecisionName { get; init; }

    public required bool AdrDefined { get; init; }

    public required bool RequiresPathJailPrototypeContract { get; init; }

    public required bool RequiresScanFixtureMatrix { get; init; }

    public required bool RequiresSyntheticOnlyTests { get; init; }

    public required bool RequiresDryRunSimulatorContract { get; init; }

    public required bool RequiresAuditCheckpoint { get; init; }

    public required bool RequiresExplicitUserConsent { get; init; }

    public required bool RequiresNoMutationGuarantee { get; init; }

    public required bool RequiresCancellationSemantics { get; init; }

    public required bool RequiresEvidenceTimelinePlan { get; init; }

    public required bool RequiresRedactionSecretExclusionPolicies { get; init; }

    public required bool EnablesRealScan { get; init; }

    public required bool EnablesRealFilesystemAccess { get; init; }

    public required bool EnablesIndexing { get; init; }

    public required bool EnablesVectorization { get; init; }

    public required bool EnablesLlmContext { get; init; }

    public required bool EnablesPromptConstruction { get; init; }

    public required bool EnablesProviderActivity { get; init; }

    public required bool EnablesCloud { get; init; }

    public required bool EnablesRuntime { get; init; }

    public IReadOnlyList<string> AcceptedAlternativesRedacted { get; init; } = [];

    public IReadOnlyList<string> RejectedAlternativesRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredNextMilestonesRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}
