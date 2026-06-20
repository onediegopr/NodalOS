using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsScanFixtureMatrixService
{
    public NodalOsScanFixtureMatrix CreateMatrix(
        string workspaceRef = "workspace-ref-m548",
        string missionRef = "mission-ref-m548") =>
        new()
        {
            MatrixId = "scan-fixture-matrix-m548",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            UsesSyntheticFixturesOnly = true,
            UsesRealFilesystem = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            PerformsSecretDetectionOnRealData = false,
            PerformsIndexing = false,
            PerformsVectorization = false,
            BuildsLlmContext = false,
            Fixtures = Enum.GetValues<NodalOsScanFixtureCategory>()
                .Select((category, index) => Fixture(category, index + 1))
                .ToArray()
        };

    public NodalOsScanFixtureMatrixReadiness Evaluate(NodalOsScanFixtureMatrix matrix) =>
        new()
        {
            ReadinessId = "scan-fixture-matrix-readiness-m548",
            MatrixRef = matrix.MatrixId,
            ReadyForSyntheticDryRunTests = true,
            ReadyForRealScan = false,
            ReadyForRealFilesystemAccess = false,
            ReadyForIndexing = false,
            ReadyForVectorization = false,
            ReadyForLlmContext = false
        };

    private static NodalOsScanFixtureDefinition Fixture(NodalOsScanFixtureCategory category, int index)
    {
        var disposition = category switch
        {
            NodalOsScanFixtureCategory.DependencyFolder => NodalOsScanFixtureExpectedDisposition.ExcludedPreview,
            NodalOsScanFixtureCategory.GeneratedOutput => NodalOsScanFixtureExpectedDisposition.ExcludedPreview,
            NodalOsScanFixtureCategory.EnvironmentMarker => NodalOsScanFixtureExpectedDisposition.RedactedPreview,
            NodalOsScanFixtureCategory.SensitiveName => NodalOsScanFixtureExpectedDisposition.RedactedPreview,
            NodalOsScanFixtureCategory.BinaryMedia => NodalOsScanFixtureExpectedDisposition.ExcludedPreview,
            NodalOsScanFixtureCategory.SymlinkLike => NodalOsScanFixtureExpectedDisposition.BlockedPreview,
            NodalOsScanFixtureCategory.OutsideJailPath => NodalOsScanFixtureExpectedDisposition.BlockedPreview,
            NodalOsScanFixtureCategory.CaseSensitivity => NodalOsScanFixtureExpectedDisposition.RequiresReview,
            NodalOsScanFixtureCategory.Cancellation => NodalOsScanFixtureExpectedDisposition.AuditRequired,
            NodalOsScanFixtureCategory.NoMutation => NodalOsScanFixtureExpectedDisposition.AuditRequired,
            _ => NodalOsScanFixtureExpectedDisposition.IncludedPreview
        };

        return new()
        {
            FixtureId = $"scan-fixture-{index:000}",
            Category = category,
            DisplayNameRedacted = $"{category} fixture",
            SyntheticPathRef = $"synthetic-fixture-ref-{index:000}",
            ExpectedOutcome = new()
            {
                Disposition = disposition,
                NeverSentToLlm = true,
                NeverSentToCloud = true,
                RequiresAudit = disposition is NodalOsScanFixtureExpectedDisposition.AuditRequired or NodalOsScanFixtureExpectedDisposition.BlockedPreview,
                RequiresRedaction = disposition == NodalOsScanFixtureExpectedDisposition.RedactedPreview
            }
        };
    }
}

public sealed class NodalOsScanFixtureMatrixJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeMatrix(NodalOsScanFixtureMatrix matrix) =>
        JsonSerializer.Serialize(matrix, Options);

    public string SerializeReadiness(NodalOsScanFixtureMatrixReadiness readiness) =>
        JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsScanFixtureMatrixFixtures
{
    public static NodalOsScanFixtureMatrix Matrix() =>
        new NodalOsScanFixtureMatrixService().CreateMatrix();
}
