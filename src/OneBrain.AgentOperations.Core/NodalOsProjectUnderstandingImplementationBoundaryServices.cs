using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsProjectUnderstandingImplementationBoundaryService
{
    public NodalOsProjectUnderstandingImplementationBoundaryDecision CreateDecision() =>
        new()
        {
            DecisionId = "project-understanding-implementation-boundary-m546",
            DecisionName = "NODAL_OS_PROJECT_UNDERSTANDING_IMPLEMENTATION_BOUNDARY_DEFINED",
            AdrDefined = true,
            RequiresPathJailPrototypeContract = true,
            RequiresScanFixtureMatrix = true,
            RequiresSyntheticOnlyTests = true,
            RequiresDryRunSimulatorContract = true,
            RequiresAuditCheckpoint = true,
            RequiresExplicitUserConsent = true,
            RequiresNoMutationGuarantee = true,
            RequiresCancellationSemantics = true,
            RequiresEvidenceTimelinePlan = true,
            RequiresRedactionSecretExclusionPolicies = true,
            EnablesRealScan = false,
            EnablesRealFilesystemAccess = false,
            EnablesIndexing = false,
            EnablesVectorization = false,
            EnablesLlmContext = false,
            EnablesPromptConstruction = false,
            EnablesProviderActivity = false,
            EnablesCloud = false,
            EnablesRuntime = false,
            AcceptedAlternativesRedacted =
            [
                "Contract-first with synthetic fixtures.",
                "Prototype-only with symbolic paths.",
                "Explicit future gate before operational scan behavior."
            ],
            RejectedAlternativesRedacted =
            [
                "Direct operational scan.",
                "Direct content access.",
                "Source-control operations for understanding.",
                "Direct LLM context construction.",
                "Vectorization or indexing first.",
                "Cloud scan.",
                "Broad crawler.",
                "Implicit consent."
            ],
            RequiredNextMilestonesRedacted =
            [
                "Synthetic path jail prototype.",
                "Fixture-based dry-run simulator.",
                "Sensitive-data and exclusion fixture validation.",
                "Cancellation and no-mutation semantics.",
                "Audit before operational filesystem access."
            ],
            GuardrailRefs =
            [
                "guardrail-adr-first",
                "guardrail-synthetic-first",
                "guardrail-no-operational-scan",
                "guardrail-no-llm-context",
                "guardrail-no-cloud"
            ]
        };
}

public sealed class NodalOsProjectUnderstandingImplementationBoundaryJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsProjectUnderstandingImplementationBoundaryDecision decision) =>
        JsonSerializer.Serialize(decision, Options);
}

public static class NodalOsProjectUnderstandingImplementationBoundaryFixtures
{
    public static NodalOsProjectUnderstandingImplementationBoundaryDecision Decision() =>
        new NodalOsProjectUnderstandingImplementationBoundaryService().CreateDecision();
}
