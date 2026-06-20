using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSyntheticCanonicalizationCasesService
{
    public NodalOsSyntheticCanonicalizationMatrix CreateMatrix()
    {
        var cases = Enum.GetValues<NodalOsSyntheticCanonicalizationCaseGroup>()
            .Select((group, index) => CreateCase(group, index + 1))
            .ToArray();
        var missing = Enum.GetValues<NodalOsSyntheticCanonicalizationCaseGroup>()
            .Except(cases.Select(c => c.Group))
            .ToArray();

        return new()
        {
            MatrixId = "synthetic-canonicalization-matrix-m556",
            Cases = cases,
            CoveragePercent = 100m,
            MissingCaseGroups = missing,
            ReadyForSyntheticCanonicalizationReview = missing.Length == 0,
            ReadyForRealCanonicalization = false,
            ReadyForRealPathJail = false,
            ReadyForFilesystemAccess = false,
            CanResolveRealPath = false,
            CanAccessFilesystem = false,
            CanReadDirectory = false,
            CanReadFile = false,
            CanHashFile = false,
            CanAuthorizeScan = false
        };
    }

    private static NodalOsSyntheticCanonicalizationCase CreateCase(NodalOsSyntheticCanonicalizationCaseGroup group, int index)
    {
        var block = group is
            NodalOsSyntheticCanonicalizationCaseGroup.OutsideRootTraversal or
            NodalOsSyntheticCanonicalizationCaseGroup.SiblingRootPrefixTrap or
            NodalOsSyntheticCanonicalizationCaseGroup.DriveBoundary or
            NodalOsSyntheticCanonicalizationCaseGroup.NetworkShareStylePath or
            NodalOsSyntheticCanonicalizationCaseGroup.SymlinkLikeSegment or
            NodalOsSyntheticCanonicalizationCaseGroup.EmptyInvalidPath or
            NodalOsSyntheticCanonicalizationCaseGroup.ReservedNameStylePath;

        var review = group is
            NodalOsSyntheticCanonicalizationCaseGroup.CaseVariant or
            NodalOsSyntheticCanonicalizationCaseGroup.MixedSeparators or
            NodalOsSyntheticCanonicalizationCaseGroup.UnicodeNormalizationVariant or
            NodalOsSyntheticCanonicalizationCaseGroup.HiddenSegment or
            NodalOsSyntheticCanonicalizationCaseGroup.DeepPath or
            NodalOsSyntheticCanonicalizationCaseGroup.MaxLengthPath;

        return new()
        {
            CaseId = $"synthetic-canonicalization-case-{index:000}",
            Group = group,
            SyntheticInputPath = $"synthetic-input-{group}",
            SyntheticRootPath = "synthetic-root",
            DeclaredNormalizedPath = $"synthetic-normalized-{group}",
            DeclaredCaseSensitivityMode = group == NodalOsSyntheticCanonicalizationCaseGroup.CaseVariant ? "case-variant-review" : "declared",
            DeclaredPathSeparatorMode = group == NodalOsSyntheticCanonicalizationCaseGroup.MixedSeparators ? "mixed-review" : "declared",
            DeclaredDriveBoundary = group == NodalOsSyntheticCanonicalizationCaseGroup.DriveBoundary,
            DeclaredNetworkShare = group == NodalOsSyntheticCanonicalizationCaseGroup.NetworkShareStylePath,
            DeclaredRelativeTraversal = group == NodalOsSyntheticCanonicalizationCaseGroup.OutsideRootTraversal,
            DeclaredSymlinkLikeSegment = group == NodalOsSyntheticCanonicalizationCaseGroup.SymlinkLikeSegment,
            DeclaredUnicodeVariant = group == NodalOsSyntheticCanonicalizationCaseGroup.UnicodeNormalizationVariant,
            DeclaredHiddenSegment = group == NodalOsSyntheticCanonicalizationCaseGroup.HiddenSegment,
            ExpectedContainmentDecision = block
                ? NodalOsSyntheticContainmentDecision.BlockPreview
                : review ? NodalOsSyntheticContainmentDecision.RequiresReview : NodalOsSyntheticContainmentDecision.AllowPreview,
            ExpectedReviewRequirement = review,
            ExpectedBlockedReasonRedacted = block ? $"{group} blocked by declared synthetic policy." : "Not blocked in synthetic preview.",
            IsSyntheticOnly = true,
            UsesRealFilesystem = false,
            PerformsRealCanonicalization = false
        };
    }
}

public sealed class NodalOsSyntheticCanonicalizationCasesJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeMatrix(NodalOsSyntheticCanonicalizationMatrix matrix) =>
        JsonSerializer.Serialize(matrix, Options);
}

public static class NodalOsSyntheticCanonicalizationCasesFixtures
{
    public static NodalOsSyntheticCanonicalizationMatrix Matrix() =>
        new NodalOsSyntheticCanonicalizationCasesService().CreateMatrix();
}

