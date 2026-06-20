using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsStorageAuditReadinessService
{
    public NodalOsStorageAuditReadiness CreateReadiness(
        NodalOsConsentStorageBoundaryTestPack testPack,
        NodalOsDisabledStorageUiPreview preview) =>
        new()
        {
            ReadinessId = "storage-audit-readiness-m581",
            BoundaryTestPackRef = testPack.TestPackId,
            DisabledStorageUiPreviewRef = preview.PreviewId,
            ProductiveConsentStorageAdrRef = testPack.ProductiveConsentStorageAdrRef,
            ConsentGovernanceCloseoutRef = testPack.ConsentGovernanceCloseoutRef,
            ReadinessStatus = NodalOsStorageAuditReadinessStatus.ReadyForAuditPlanningOnly,
            IsReadinessOnly = true,
            CanAuthorizeImplementation = false,
            CanEnableProductiveStorage = false,
            CanPersistConsent = false,
            CanEnforceConsent = false,
            CanAuthorizeCapability = false,
            CanAccessFilesystem = false,
            CanBuildLlmContext = false,
            CanUseCloud = false,
            Criteria = Enum.GetValues<NodalOsStorageAuditReadinessCriterionKind>().Select((kind, index) => new NodalOsStorageAuditReadinessCriterion
            {
                CriterionId = $"storage-audit-readiness-criterion-{index + 1:000}-{kind}",
                Kind = kind,
                Required = true,
                SatisfiedForPlanning = true,
                BlocksImplementationIfMissing = true,
                UserFacingExplanationRedacted = $"{kind} is required before productive storage work."
            }).ToArray(),
            Decision = new()
            {
                DecisionId = "storage-audit-readiness-decision-m581",
                ReadyForAuditPlanning = true,
                ReadyForProductiveStorageImplementation = false,
                ReadyForProductivePersistence = false,
                ReadyForConsentEnforcement = false,
                ReadyForCapabilityAuthorization = false,
                ReadyForFilesystemAccess = false,
                ReadyForLlmContext = false,
                RecommendedNextMilestoneRedacted = "Productive consent storage audit plan."
            }
        };
}

public sealed class NodalOsStorageAuditReadinessJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsStorageAuditReadiness readiness) => JsonSerializer.Serialize(readiness, Options);
}
