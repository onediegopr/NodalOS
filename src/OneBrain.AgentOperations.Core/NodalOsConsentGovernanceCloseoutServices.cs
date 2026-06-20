using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentGovernanceCloseoutService
{
    public NodalOsConsentGovernanceCloseout CreateCloseout(
        NodalOsConsentAdversarialTestMatrix matrix,
        NodalOsProductiveConsentStorageAdrSummary adrSummary) =>
        new()
        {
            CloseoutId = "consent-governance-closeout-m578",
            AuditCheckpointRef = "audit-checkpoint-review-m570",
            ProductiveConsentDesignDraftRef = "productive-consent-design-draft-m571",
            ProductiveConsentDesignReviewRef = matrix.DesignReviewRef,
            DisabledConsentStorageContractRef = matrix.DisabledConsentStorageContractRef,
            ConsentAuditAcceptanceRef = matrix.ConsentAuditAcceptanceRef,
            ConsentAdversarialMatrixRef = matrix.MatrixId,
            ProductiveConsentStorageAdrRef = adrSummary.AdrId,
            ClosedAsGovernanceBaseline = true,
            ProductiveConsentStillBlocked = true,
            ProductiveStorageStillBlocked = true,
            ConsentEnforcementStillBlocked = true,
            CoveredDecisions =
            [
                NodalOsConsentGovernanceCoveredDecision.AccessImplementationCheckpointReady,
                NodalOsConsentGovernanceCoveredDecision.ProductiveConsentDesignReviewReady,
                NodalOsConsentGovernanceCoveredDecision.ProductiveConsentStorageNotImplementedAdrReady
            ],
            Findings = Enum.GetValues<NodalOsConsentGovernanceFindingKind>().Select((kind, index) => new NodalOsConsentGovernanceFinding
            {
                FindingId = $"consent-governance-finding-{index + 1:000}-{kind}",
                Kind = kind,
                BlocksProductiveUse = kind is not NodalOsConsentGovernanceFindingKind.DesignCompleteAsGovernanceBaseline
                    and not NodalOsConsentGovernanceFindingKind.StorageContractDefinedButDisabled
                    and not NodalOsConsentGovernanceFindingKind.AdversarialMatrixDefined
                    and not NodalOsConsentGovernanceFindingKind.AdrReady,
                UserFacingExplanationRedacted = $"{kind} is recorded for consent governance closeout."
            }).ToArray(),
            Decision = new()
            {
                DecisionId = "consent-governance-closeout-decision-m578",
                ConsentGovernanceBaselineReady = true,
                ReadyForProductiveConsentImplementation = false,
                ReadyForProductiveConsentStorage = false,
                ReadyForProductiveConsentEnforcement = false,
                ReadyForCapabilityAuthorization = false,
                ReadyForFilesystemAccess = false,
                ReadyForLlmContext = false,
                ReadyForCloud = false,
                ReadyForRuntime = false,
                RecommendedNextMilestoneRedacted = "Storage boundary test pack."
            }
        };
}

public sealed class NodalOsConsentGovernanceCloseoutJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsConsentGovernanceCloseout closeout) => JsonSerializer.Serialize(closeout, Options);
}
