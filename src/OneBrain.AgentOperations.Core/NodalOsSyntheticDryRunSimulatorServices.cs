using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSyntheticDryRunSimulatorService
{
    public NodalOsSyntheticDryRunSimulatorContract CreateContract(
        NodalOsScanFixtureMatrix matrix,
        NodalOsPathJailPrototypeContract prototype) =>
        new()
        {
            SimulatorId = "synthetic-dry-run-simulator-m549",
            WorkspaceRef = matrix.WorkspaceRef,
            MissionRef = matrix.MissionRef,
            FixtureMatrixRef = matrix.MatrixId,
            PathJailPrototypeRef = prototype.PrototypeId,
            SecretDetectionPolicyPreviewRef = "secret-detection-policy-preview-m540",
            ExclusionPolicyPackRef = "exclusion-policy-pack-m541",
            ScanDryRunContractRef = "scan-dry-run-contract-m542",
            UsesSyntheticFixturesOnly = true,
            UsesRealFilesystem = false,
            PerformsRealScan = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            PerformsSecretDetectionOnRealData = false,
            AppliesExclusionsToRealFilesystem = false,
            PerformsIndexing = false,
            PerformsVectorization = false,
            BuildsLlmContext = false,
            CallsProvider = false,
            UsesCloud = false,
            IsSimulationOnly = true
        };

    public IReadOnlyList<NodalOsSyntheticSimulationInput> CreateInputs(NodalOsScanFixtureMatrix matrix) =>
        matrix.Fixtures.Select(f => new NodalOsSyntheticSimulationInput
        {
            InputId = $"simulation-input-{f.FixtureId}",
            FixtureRef = f.FixtureId,
            DeclaredSyntheticPathRef = f.SyntheticPathRef,
            DeclaredCategory = f.Category,
            ExpectedOutcome = f.ExpectedOutcome,
            SyntheticMetadataOnlyRedacted = $"{f.Category} metadata only.",
            ContainsRawFileContent = false,
            ContainsRawSecret = false
        }).ToArray();

    public NodalOsSyntheticDryRunSimulationResult Simulate(
        NodalOsSyntheticDryRunSimulatorContract contract,
        IReadOnlyList<NodalOsSyntheticSimulationInput> inputs)
    {
        var decisions = inputs.Select(input => new NodalOsSyntheticPolicyDecision
        {
            DecisionId = $"synthetic-policy-decision-{input.FixtureRef}",
            FixtureRef = input.FixtureRef,
            SimulatedDisposition = input.ExpectedOutcome.Disposition,
            RequiresReview = input.ExpectedOutcome.Disposition == NodalOsScanFixtureExpectedDisposition.RequiresReview,
            RequiresRedaction = input.ExpectedOutcome.RequiresRedaction,
            RequiresAudit = input.ExpectedOutcome.RequiresAudit,
            ExplanationRedacted = $"{input.DeclaredCategory} evaluated from synthetic metadata only."
        }).ToArray();

        return new()
        {
            ResultId = "synthetic-dry-run-result-m549",
            SimulatorRef = contract.SimulatorId,
            IncludedPreviewCount = Count(decisions, NodalOsScanFixtureExpectedDisposition.IncludedPreview),
            ExcludedPreviewCount = Count(decisions, NodalOsScanFixtureExpectedDisposition.ExcludedPreview),
            BlockedPreviewCount = Count(decisions, NodalOsScanFixtureExpectedDisposition.BlockedPreview),
            RequiresReviewCount = Count(decisions, NodalOsScanFixtureExpectedDisposition.RequiresReview),
            RedactedPreviewCount = Count(decisions, NodalOsScanFixtureExpectedDisposition.RedactedPreview),
            AuditRequiredCount = decisions.Count(d => d.RequiresAudit),
            PolicyDecisions = decisions,
            UserFacingSummaryRedacted = "Synthetic dry-run simulator evaluated declared fixture metadata only.",
            EvidenceRefs = ["evidence-synthetic-dry-run-ref-only"],
            TimelineRefs = ["timeline-synthetic-dry-run-m549"],
            GuardrailRefs = ["guardrail-synthetic-only", "guardrail-no-operational-scan"]
        };
    }

    public NodalOsSyntheticDryRunSimulatorReadiness Evaluate(NodalOsSyntheticDryRunSimulatorContract contract) =>
        new()
        {
            ReadinessId = "synthetic-dry-run-readiness-m549",
            SimulatorRef = contract.SimulatorId,
            ReadyForSyntheticSimulation = true,
            ReadyForRealDryRun = false,
            ReadyForRealScan = false,
            ReadyForRealFilesystemAccess = false,
            ReadyForRealSecretDetection = false,
            ReadyForIndexing = false,
            ReadyForVectorization = false,
            ReadyForLlmContext = false
        };

    private static int Count(IEnumerable<NodalOsSyntheticPolicyDecision> decisions, NodalOsScanFixtureExpectedDisposition disposition) =>
        decisions.Count(d => d.SimulatedDisposition == disposition);
}

public sealed class NodalOsSyntheticDryRunSimulatorJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeContract(NodalOsSyntheticDryRunSimulatorContract contract) => JsonSerializer.Serialize(contract, Options);
    public string SerializeInputs(IReadOnlyList<NodalOsSyntheticSimulationInput> inputs) => JsonSerializer.Serialize(inputs, Options);
    public string SerializeResult(NodalOsSyntheticDryRunSimulationResult result) => JsonSerializer.Serialize(result, Options);
    public string SerializeReadiness(NodalOsSyntheticDryRunSimulatorReadiness readiness) => JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsSyntheticDryRunSimulatorFixtures
{
    public static NodalOsSyntheticDryRunSimulatorContract Contract()
    {
        var matrix = NodalOsScanFixtureMatrixFixtures.Matrix();
        return new NodalOsSyntheticDryRunSimulatorService().CreateContract(matrix, NodalOsPathJailPrototypeFixtures.Prototype());
    }
}
