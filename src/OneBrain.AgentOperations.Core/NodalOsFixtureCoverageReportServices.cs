using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsFixtureCoverageReportService
{
    public NodalOsFixtureCoverageReport CreateReport(
        NodalOsScanFixtureMatrix matrix,
        NodalOsSyntheticDryRunSimulationResult result,
        NodalOsFixtureResultReview review)
    {
        var allCategories = Enum.GetValues<NodalOsScanFixtureCategory>();
        var covered = matrix.Fixtures.Select(f => f.Category).Distinct().ToArray();
        var missing = allCategories.Except(covered).ToArray();
        var complete = missing.Length == 0;

        return new()
        {
            CoverageReportId = "fixture-coverage-report-m553",
            FixtureMatrixRef = matrix.MatrixId,
            SimulatorResultRef = result.ResultId,
            FixtureResultReviewRef = review.ReviewId,
            TotalFixtureCategories = allCategories.Length,
            CoveredFixtureCategories = covered.Length,
            MissingFixtureCategories = missing,
            CoveragePercent = Math.Round(covered.Length * 100m / allCategories.Length, 2),
            CoverageStatus = complete
                ? NodalOsFixtureCoverageStatus.CompleteSyntheticCoverage
                : NodalOsFixtureCoverageStatus.MissingRequiredSyntheticCoverage,
            IsSyntheticCoverageOnly = true,
            UsesRealFilesystem = false,
            CanAuthorizeRealScan = false,
            CoverageDimensions = CreateDimensions(matrix),
            CoverageDecision = new()
            {
                CoverageDecisionId = "fixture-coverage-decision-m553",
                ReadyForSyntheticCoverageCloseout = complete,
                ReadyForRealScan = false,
                ReadyForFilesystemAccess = false,
                ReadyForRealPathJail = false,
                ReadyForRealSecretDetection = false,
                ReadyForRealExclusionEnforcement = false,
                ReadyForIndexing = false,
                ReadyForRepresentationBuild = false,
                ReadyForLlmContext = false
            },
            RequiredGapsRedacted =
            [
                "Operational path jail enablement remains required.",
                "Operational workspace access gate remains required.",
                "Sensitive-data policy enforcement remains required.",
                "Exclusion enforcement remains required.",
                "Evidence and timeline emission audit remains required.",
                "Cancellation and no-mutation proof remains required.",
                "Operational scan audit remains required."
            ]
        };
    }

    private static IReadOnlyList<NodalOsFixtureCoverageDimension> CreateDimensions(NodalOsScanFixtureMatrix matrix) =>
        Enum.GetValues<NodalOsFixtureCoverageDimensionKind>()
            .Select(kind =>
            {
                var categories = CategoriesFor(kind);
                var covered = matrix.Fixtures.Where(f => categories.Contains(f.Category)).Select(f => f.FixtureId).ToArray();
                return new NodalOsFixtureCoverageDimension
                {
                    Kind = kind,
                    IsCovered = covered.Length > 0,
                    CoveredCategoryRefs = covered,
                    ExplanationRedacted = $"{kind} covered by synthetic fixture references only."
                };
            })
            .ToArray();

    private static IReadOnlyList<NodalOsScanFixtureCategory> CategoriesFor(NodalOsFixtureCoverageDimensionKind kind) =>
        kind switch
        {
            NodalOsFixtureCoverageDimensionKind.PathContainment => [NodalOsScanFixtureCategory.EmptyWorkspace, NodalOsScanFixtureCategory.SmallSourceTree],
            NodalOsFixtureCoverageDimensionKind.SymlinkLike => [NodalOsScanFixtureCategory.SymlinkLike],
            NodalOsFixtureCoverageDimensionKind.CaseSensitivity => [NodalOsScanFixtureCategory.CaseSensitivity],
            NodalOsFixtureCoverageDimensionKind.OutsideJail => [NodalOsScanFixtureCategory.OutsideJailPath],
            NodalOsFixtureCoverageDimensionKind.DependencyExclusion => [NodalOsScanFixtureCategory.DependencyFolder],
            NodalOsFixtureCoverageDimensionKind.GeneratedOutput => [NodalOsScanFixtureCategory.GeneratedOutput],
            NodalOsFixtureCoverageDimensionKind.SensitiveName => [NodalOsScanFixtureCategory.SensitiveName],
            NodalOsFixtureCoverageDimensionKind.EnvironmentMarker => [NodalOsScanFixtureCategory.EnvironmentMarker],
            NodalOsFixtureCoverageDimensionKind.BinaryMedia => [NodalOsScanFixtureCategory.BinaryMedia],
            NodalOsFixtureCoverageDimensionKind.DeepTree => [NodalOsScanFixtureCategory.DeepTree],
            NodalOsFixtureCoverageDimensionKind.MaxFiles => [NodalOsScanFixtureCategory.MaxFiles],
            NodalOsFixtureCoverageDimensionKind.MaxBytes => [NodalOsScanFixtureCategory.MaxBytes],
            NodalOsFixtureCoverageDimensionKind.Cancellation => [NodalOsScanFixtureCategory.Cancellation],
            NodalOsFixtureCoverageDimensionKind.NoMutation => [NodalOsScanFixtureCategory.NoMutation],
            NodalOsFixtureCoverageDimensionKind.Redaction => [NodalOsScanFixtureCategory.EnvironmentMarker, NodalOsScanFixtureCategory.SensitiveName],
            NodalOsFixtureCoverageDimensionKind.NeverLeavesLocalPolicy => Enum.GetValues<NodalOsScanFixtureCategory>(),
            _ => []
        };
}

public sealed class NodalOsFixtureCoverageReportJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeReport(NodalOsFixtureCoverageReport report) =>
        JsonSerializer.Serialize(report, Options);
}

public static class NodalOsFixtureCoverageReportFixtures
{
    public static NodalOsFixtureCoverageReport Report()
    {
        var simulator = new NodalOsSyntheticDryRunSimulatorService();
        var matrix = NodalOsScanFixtureMatrixFixtures.Matrix();
        var contract = simulator.CreateContract(matrix, NodalOsPathJailPrototypeFixtures.Prototype());
        var result = simulator.Simulate(contract, simulator.CreateInputs(matrix));
        var review = new NodalOsFixtureResultReviewService().CreateReview(result, matrix);
        return new NodalOsFixtureCoverageReportService().CreateReport(matrix, result, review);
    }
}

