using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsProductiveConsentDesignDraftService
{
    public NodalOsProductiveConsentDesignDraft CreateDraft(
        IReadOnlyList<NodalOsCapabilityAccessGate> gates,
        string workspaceRef = "workspace-ref-m571",
        string missionRef = "mission-ref-m571") =>
        new()
        {
            DesignId = "productive-consent-design-draft-m571",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            BasedOnLedgerMockRef = "consent-scope-ledger-mock-m565",
            BasedOnCapabilityGateRefs = gates.Select(gate => gate.GateId).ToArray(),
            IsDesignOnly = true,
            UsesProductivePersistence = false,
            PersistsConsent = false,
            EnforcesConsent = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanBuildLlmContext = false,
            CanUseCloud = false,
            FutureComponents = Enum.GetValues<NodalOsFutureConsentComponent>(),
            DataSafetyRules = Enum.GetValues<NodalOsConsentDataSafetyRuleKind>().Select((kind, index) => new NodalOsConsentDataSafetyRule
            {
                RuleId = $"consent-data-safety-rule-{index + 1:000}-{kind}",
                Kind = kind,
                Required = true,
                BlocksProductiveUseIfMissing = true,
                UserFacingExplanationRedacted = $"{kind} is mandatory for future productive consent."
            }).ToArray(),
            Decision = CreateDecision()
        };

    private static NodalOsProductiveConsentDesignDecision CreateDecision() =>
        new()
        {
            DecisionId = "productive-consent-design-decision-m571",
            ReadyForDesignReview = true,
            ReadyForProductiveImplementation = false,
            ReadyForConsentPersistence = false,
            ReadyForConsentEnforcement = false,
            ReadyForFilesystemAccess = false,
            ReadyForLlmContext = false,
            RequiredBeforeImplementationRedacted =
            [
                "Storage boundary review.",
                "Revocation policy review.",
                "Audit trail design.",
                "Fail-closed acceptance.",
                "Disable strategy."
            ]
        };
}

public sealed class NodalOsProductiveConsentDesignDraftJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsProductiveConsentDesignDraft draft) => JsonSerializer.Serialize(draft, Options);
}
