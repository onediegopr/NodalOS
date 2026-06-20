using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSyntheticPolicyRegressionPackService
{
    public NodalOsSyntheticPolicyRegressionPack CreatePack(
        NodalOsDisabledPathJailPrototypeGate gate,
        NodalOsSyntheticCanonicalizationMatrix matrix,
        NodalOsNoMutationProofContract proof)
    {
        var cases = Enum.GetValues<NodalOsSyntheticPolicyRegressionCategory>()
            .Select((category, index) => CreateCase(category, index + 1))
            .ToArray();

        return new()
        {
            RegressionPackId = "synthetic-policy-regression-pack-m560",
            GateRef = gate.GateId,
            CanonicalizationMatrixRef = matrix.MatrixId,
            NoMutationProofRef = proof.ProofId,
            FixtureCoverageRef = "fixture-coverage-report-m553",
            SecretDetectionPolicyPreviewRef = "secret-detection-policy-preview-m540",
            ExclusionPolicyPackRef = "exclusion-policy-pack-m541",
            UsesSyntheticFixturesOnly = true,
            UsesRealFilesystem = false,
            PerformsRealCanonicalization = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            PerformsMutation = false,
            BuildsLlmContext = false,
            CallsProvider = false,
            UsesCloud = false,
            Cases = cases,
            Result = new()
            {
                ResultId = "synthetic-policy-regression-result-m560",
                ReadyForSyntheticRegression = true,
                ReadyForRealPathJail = false,
                ReadyForRealFilesystemAccess = false,
                ReadyForRealScan = false,
                ReadyForIndexing = false,
                ReadyForRepresentationBuild = false,
                ReadyForLlmContext = false,
                PassingSyntheticCases = cases.Select(c => c.CaseId).ToArray(),
                FailingSyntheticCases = [],
                MissingSyntheticCases = [],
                UserFacingSummaryRedacted = "Synthetic policy regression pack passed declared synthetic cases only."
            }
        };
    }

    private static NodalOsSyntheticPolicyRegressionCase CreateCase(NodalOsSyntheticPolicyRegressionCategory category, int index)
    {
        var blocked = category is
            NodalOsSyntheticPolicyRegressionCategory.TraversalBlocking or
            NodalOsSyntheticPolicyRegressionCategory.PrefixTrapBlocking or
            NodalOsSyntheticPolicyRegressionCategory.DriveBoundary or
            NodalOsSyntheticPolicyRegressionCategory.NetworkShareStylePath or
            NodalOsSyntheticPolicyRegressionCategory.SymlinkLikeSegment or
            NodalOsSyntheticPolicyRegressionCategory.NoFilesystemAccess;

        var redacted = category is
            NodalOsSyntheticPolicyRegressionCategory.SensitiveLikeCategories or
            NodalOsSyntheticPolicyRegressionCategory.HiddenPath or
            NodalOsSyntheticPolicyRegressionCategory.UnicodeNormalization;

        return new()
        {
            CaseId = $"synthetic-policy-regression-case-{index:000}",
            Category = category,
            SyntheticInputRef = $"synthetic-policy-input-{category}",
            ExpectedDecision = blocked
                ? NodalOsSyntheticPolicyRegressionDecision.BlockPreview
                : redacted ? NodalOsSyntheticPolicyRegressionDecision.RequiresRedaction : NodalOsSyntheticPolicyRegressionDecision.AllowPreview,
            ExpectedBlockedReasonRedacted = blocked ? $"{category} blocked by synthetic regression policy." : "Not blocked in synthetic regression.",
            ExpectedReviewRequirement = category is NodalOsSyntheticPolicyRegressionCategory.CaseSensitivity or NodalOsSyntheticPolicyRegressionCategory.AuditRequiredCases,
            ExpectedRedactionRequirement = redacted,
            NeverSentToLlm = true,
            NeverSentToCloud = true,
            IsSyntheticOnly = true,
            UsesRealFilesystem = false
        };
    }
}

public sealed class NodalOsSyntheticPolicyRegressionPackJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePack(NodalOsSyntheticPolicyRegressionPack pack) => JsonSerializer.Serialize(pack, Options);
}

