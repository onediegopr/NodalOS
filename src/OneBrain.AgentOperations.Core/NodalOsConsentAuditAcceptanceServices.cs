using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentAuditAcceptanceService
{
    public NodalOsConsentAuditAcceptancePack CreateAcceptance(
        NodalOsProductiveConsentDesignReview review,
        NodalOsDisabledConsentStorageContract storageContract) =>
        new()
        {
            AcceptanceId = "consent-audit-acceptance-m575",
            DesignReviewRef = review.ReviewId,
            StorageContractRef = storageContract.StorageContractId,
            AuditCheckpointRef = "audit-checkpoint-review-m570",
            CapabilityAuditChecklistRef = "capability-audit-checklist-m568",
            AcceptanceStatus = NodalOsConsentAuditAcceptanceStatus.GovernanceBaselineAccepted,
            IsAcceptanceOnly = true,
            CanApproveImplementation = false,
            CanEnableProductiveConsent = false,
            CanAuthorizeCapability = false,
            CanAccessFilesystem = false,
            CanBuildLlmContext = false,
            CanUseCloud = false,
            AcceptanceCriteria = Enum.GetValues<NodalOsConsentAuditCriterionKind>().Select((kind, index) => new NodalOsConsentAuditAcceptanceCriterion
            {
                CriterionId = $"consent-audit-criterion-{index + 1:000}-{kind}",
                Kind = kind,
                Required = true,
                Satisfied = true,
                BlocksImplementationIfMissing = true,
                UserFacingExplanationRedacted = $"{kind} is required before any implementation milestone."
            }).ToArray(),
            Decision = new()
            {
                DecisionId = "consent-audit-acceptance-decision-m575",
                ConsentDesignAcceptedAsGovernanceBaseline = true,
                ReadyForProductiveConsentImplementation = false,
                ReadyForProductiveConsentStorage = false,
                ReadyForProductiveConsentEnforcement = false,
                ReadyForCapabilityAuthorization = false,
                ReadyForFilesystemAccess = false,
                ReadyForLlmContext = false,
                ReadyForCloud = false,
                ReadyForRuntime = false,
                RecommendedNextMilestoneRedacted = "Productive consent storage implementation ADR."
            }
        };
}

public sealed class NodalOsConsentAuditAcceptanceJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsConsentAuditAcceptancePack acceptance) => JsonSerializer.Serialize(acceptance, Options);
}
