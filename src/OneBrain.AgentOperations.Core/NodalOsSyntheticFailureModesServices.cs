using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSyntheticFailureModesService
{
    public NodalOsSyntheticFailureModeMatrix CreateMatrix()
    {
        var modes = Enum.GetValues<NodalOsSyntheticFailureCategory>().Select((category, index) => new NodalOsSyntheticFailureMode
        {
            FailureModeId = $"synthetic-failure-mode-{index + 1:000}",
            Capability = CapabilityFor(category),
            Scenario = category,
            SyntheticInputRef = $"synthetic-failure-input-{category}",
            ExpectedFailureReasonRedacted = $"{category} fails closed in synthetic policy.",
            ExpectedBlockedCapability = CapabilityFor(category),
            ExpectedUserMessageRedacted = "Capability is blocked until future consent and audit gates are satisfied.",
            ExpectedEvidenceRef = $"synthetic-evidence-{category}",
            ExpectedTimelineRef = $"synthetic-timeline-{category}",
            IsSyntheticOnly = true,
            UsesRealFilesystem = false,
            PerformsRealOperation = false
        }).ToArray();

        return new()
        {
            MatrixId = "synthetic-failure-mode-matrix-m562",
            FailureModes = modes,
            CoveragePercent = 100m,
            MissingFailureCategories = [],
            ReadyForSyntheticFailureReview = true,
            ReadyForRealFailureHandling = false,
            ReadyForRealFilesystemAccess = false,
            ReadyForRealScan = false,
            Behavior = new()
            {
                FailClosed = true,
                RequiresUserFacingExplanation = true,
                EmitsSyntheticEvidenceOnly = true,
                EmitsSyntheticTimelineOnly = true,
                DoesNotRetryAutomatically = true,
                DoesNotEscalateToRuntime = true
            }
        };
    }

    private static NodalOsOperationalCapability CapabilityFor(NodalOsSyntheticFailureCategory category) =>
        category switch
        {
            NodalOsSyntheticFailureCategory.CloudDisabled => NodalOsOperationalCapability.CloudSync,
            NodalOsSyntheticFailureCategory.LlmDisabled => NodalOsOperationalCapability.LlmContextBuild,
            NodalOsSyntheticFailureCategory.ProviderPolicyMissing => NodalOsOperationalCapability.ProviderCall,
            NodalOsSyntheticFailureCategory.RuntimeGateMissing => NodalOsOperationalCapability.RuntimeExecution,
            NodalOsSyntheticFailureCategory.SensitiveLikeMarker => NodalOsOperationalCapability.SecretDetection,
            NodalOsSyntheticFailureCategory.ExcludedFolder => NodalOsOperationalCapability.ExclusionEnforcement,
            _ => NodalOsOperationalCapability.PathCanonicalization
        };
}

public sealed class NodalOsSyntheticFailureModesJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeMatrix(NodalOsSyntheticFailureModeMatrix matrix) => JsonSerializer.Serialize(matrix, Options);
}
