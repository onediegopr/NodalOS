using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSyntheticDryRunUiResultsService
{
    public NodalOsSyntheticDryRunUiResultsPreview CreatePreview(
        NodalOsSyntheticDryRunSimulationResult result,
        NodalOsFixtureResultReview review,
        NodalOsScanBoundaryAudit audit,
        string workspaceRef = "workspace-ref-m552",
        string missionRef = "mission-ref-m552") =>
        new()
        {
            ResultsPreviewId = "synthetic-dry-run-ui-results-m552",
            SimulatorResultRef = result.ResultId,
            FixtureResultReviewRef = review.ReviewId,
            ScanBoundaryAuditRef = audit.AuditId,
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            IsStaticPreview = true,
            IsReadOnly = true,
            IsNoOp = true,
            UsesSyntheticFixturesOnly = true,
            UsesRealFilesystem = false,
            PerformsRealScan = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            PerformsIndexing = false,
            PerformsRepresentationBuild = false,
            BuildsLlmContext = false,
            CallsProvider = false,
            UsesCloud = false,
            Sections = new()
            {
                SimulationSummaryRedacted = result.UserFacingSummaryRedacted,
                IncludedPreviewCount = result.IncludedPreviewCount,
                ExcludedPreviewCount = result.ExcludedPreviewCount,
                BlockedPreviewCount = result.BlockedPreviewCount,
                RequiresReviewCount = result.RequiresReviewCount,
                RedactedPreviewCount = result.RedactedPreviewCount,
                AuditRequiredCount = result.AuditRequiredCount,
                FixtureMismatchesRedacted = review.MismatchesRedacted,
                OpenQuestionsRedacted = review.OpenQuestionsRedacted,
                BlockedCapabilitiesRedacted =
                [
                    "Operational workspace access remains blocked.",
                    "Index and representation build remain blocked.",
                    "LLM context, provider, cloud, and runtime remain blocked."
                ],
                NextGateRequirementsRedacted = audit.NextGateRequirementsRedacted,
                UserFacingExplanationRedacted = "These results are synthetic only and cannot authorize operational behavior."
            },
            DisclosuresRedacted =
            [
                "Results are synthetic only.",
                "No workspace filesystem was accessed.",
                "No content was inspected.",
                "No sensitive-data scan ran on operational data.",
                "No index, representation build, or LLM context was created.",
                "No cloud, provider, or runtime was used.",
                "Review does not authorize operational scan behavior."
            ]
        };
}

public sealed class NodalOsSyntheticDryRunUiResultsJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreview(NodalOsSyntheticDryRunUiResultsPreview preview) =>
        JsonSerializer.Serialize(preview, Options);
}

public static class NodalOsSyntheticDryRunUiResultsFixtures
{
    public static NodalOsSyntheticDryRunUiResultsPreview Preview()
    {
        var simulator = new NodalOsSyntheticDryRunSimulatorService();
        var matrix = NodalOsScanFixtureMatrixFixtures.Matrix();
        var contract = simulator.CreateContract(matrix, NodalOsPathJailPrototypeFixtures.Prototype());
        var result = simulator.Simulate(contract, simulator.CreateInputs(matrix));
        var review = new NodalOsFixtureResultReviewService().CreateReview(result, matrix);
        var audit = new NodalOsScanBoundaryAuditService().CreateAudit(contract, review, NodalOsPathJailPrototypeFixtures.Prototype(), matrix);
        return new NodalOsSyntheticDryRunUiResultsService().CreatePreview(result, review, audit);
    }
}

