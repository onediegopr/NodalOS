using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentAdversarialTestMatrixService
{
    public NodalOsConsentAdversarialTestMatrix CreateMatrix(
        NodalOsProductiveConsentDesignReview review,
        NodalOsDisabledConsentStorageContract storageContract,
        NodalOsConsentAuditAcceptancePack acceptance) =>
        new()
        {
            MatrixId = "consent-adversarial-test-matrix-m576",
            DesignReviewRef = review.ReviewId,
            DisabledConsentStorageContractRef = storageContract.StorageContractId,
            ConsentAuditAcceptanceRef = acceptance.AcceptanceId,
            WorkspaceRef = "workspace-ref-m576",
            MissionRef = "mission-ref-m576",
            IsSyntheticOnly = true,
            IsAdversarialMatrixOnly = true,
            UsesProductivePersistence = false,
            PersistsConsent = false,
            EnforcesConsent = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeLlmContext = false,
            CanUseCloud = false,
            TestCases = Enum.GetValues<NodalOsConsentAdversarialCategory>().Select((category, index) => new NodalOsConsentAdversarialTestCase
            {
                CaseId = $"consent-adversarial-case-{index + 1:000}-{category}",
                Category = category,
                SyntheticInputRef = $"synthetic-consent-input-{index + 1:000}",
                ExpectedDecisionRedacted = "Block and fail closed.",
                ExpectedBlockedReasonRedacted = $"{category} blocks productive use.",
                ExpectedUserFacingExplanationRedacted = "Consent governance requires a separate audited milestone before use.",
                ExpectedEvidenceRef = $"synthetic-evidence-ref-m576-{index + 1:000}",
                ExpectedTimelineRef = $"synthetic-timeline-ref-m576-{index + 1:000}",
                RequiresFailClosed = true,
                NeverAuthorizesRealUse = true,
                NeverPersistsProductively = true,
                NeverSendsToLlm = true,
                NeverSendsToCloud = true,
                IsSyntheticOnly = true
            }).ToArray(),
            Decision = new()
            {
                DecisionId = "consent-adversarial-matrix-decision-m576",
                ReadyForAdversarialReview = true,
                ReadyForProductiveConsentImplementation = false,
                ReadyForProductivePersistence = false,
                ReadyForConsentEnforcement = false,
                ReadyForCapabilityAuthorization = false,
                ReadyForFilesystemAccess = false,
                ReadyForLlmContext = false
            }
        };
}

public sealed class NodalOsConsentAdversarialTestMatrixJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsConsentAdversarialTestMatrix matrix) => JsonSerializer.Serialize(matrix, Options);
}
