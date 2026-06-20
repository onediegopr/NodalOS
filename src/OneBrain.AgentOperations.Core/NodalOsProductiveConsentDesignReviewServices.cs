using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsProductiveConsentDesignReviewService
{
    public NodalOsProductiveConsentDesignReview CreateReview(string workspaceRef = "workspace-ref-m573", string missionRef = "mission-ref-m573") =>
        new()
        {
            ReviewId = "productive-consent-design-review-m573",
            DesignDraftRef = "productive-consent-design-draft-m571",
            AuditCheckpointRef = "audit-checkpoint-review-m570",
            DisabledAccessRoadmapRef = "disabled-access-roadmap-m572",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            IsReviewOnly = true,
            IsNoOp = true,
            CanApproveImplementation = false,
            CanAuthorizeProductiveConsent = false,
            CanPersistConsent = false,
            CanEnforceConsent = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeLlmContext = false,
            CanUseCloud = false,
            ReviewSections = Enum.GetValues<NodalOsProductiveConsentReviewSection>(),
            Findings = new()
            {
                AcceptedDesignAssumptionsRedacted =
                [
                    "Consent is per-capability.",
                    "Consent is local-first.",
                    "Missing consent fails closed."
                ],
                OpenQuestionsRedacted = ["Storage shape requires future ADR."],
                BlockersRedacted = ["No productive persistence exists."],
                RequiredAuditsRedacted = ["Storage audit.", "Enforcement audit."],
                RequiredAdversarialTestsRedacted = ["Stale consent.", "Revoked consent.", "Cross-capability misuse."],
                ImplementationPrerequisitesRedacted = ["ADR and disabled-by-default feature strategy."],
                UserFacingRiskNotesRedacted = ["Design review cannot authorize access."]
            },
            Decision = new()
            {
                DecisionId = "productive-consent-design-review-decision-m573",
                ReadyForDesignReviewCloseout = true,
                ReadyForProductiveConsentImplementation = false,
                ReadyForConsentPersistence = false,
                ReadyForConsentEnforcement = false,
                ReadyForCapabilityAuthorization = false,
                ReadyForFilesystemAccess = false,
                ReadyForLlmContext = false,
                RequiredBeforeImplementationRedacted =
                [
                    "Implementation ADR.",
                    "Storage contract review.",
                    "Audit acceptance.",
                    "Adversarial test matrix."
                ]
            }
        };
}

public sealed class NodalOsProductiveConsentDesignReviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsProductiveConsentDesignReview review) => JsonSerializer.Serialize(review, Options);
}
