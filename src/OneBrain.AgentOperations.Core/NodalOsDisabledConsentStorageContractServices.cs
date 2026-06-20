using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsDisabledConsentStorageContractService
{
    private static readonly DateTimeOffset FixtureTime = new(2026, 06, 20, 00, 00, 00, TimeSpan.Zero);

    public NodalOsDisabledConsentStorageContract CreateContract(
        NodalOsProductiveConsentDesignReview review,
        IReadOnlyList<NodalOsCapabilityAccessGate> gates,
        string workspaceRef = "workspace-ref-m574",
        string missionRef = "mission-ref-m574") =>
        new()
        {
            StorageContractId = "disabled-consent-storage-contract-m574",
            DesignReviewRef = review.ReviewId,
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            DisabledByDefault = true,
            IsContractOnly = true,
            UsesProductivePersistence = false,
            PersistsConsent = false,
            ReadsProductiveConsent = false,
            WritesProductiveConsent = false,
            DeletesProductiveConsent = false,
            MigratesConsent = false,
            SyncsConsentToCloud = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeLlmContext = false,
            RecordDrafts = gates.Select((gate, index) => CreateRecordDraft(gate.Capability, index)).ToArray(),
            SafetyRules = Enum.GetValues<NodalOsConsentStorageSafetyRuleKind>().Select((kind, index) => new NodalOsConsentStorageSafetyRule
            {
                RuleId = $"consent-storage-safety-rule-{index + 1:000}-{kind}",
                Kind = kind,
                Required = true,
                BlocksProductiveUseIfMissing = true,
                UserFacingExplanationRedacted = $"{kind} is mandatory before storage activation."
            }).ToArray(),
            Readiness = new()
            {
                ReadinessId = "disabled-consent-storage-readiness-m574",
                ReadyForStorageDesignReview = true,
                ReadyForProductivePersistence = false,
                ReadyForConsentEnforcement = false,
                ReadyForCapabilityAuthorization = false,
                ReadyForFilesystemAccess = false,
                ReadyForLlmContext = false,
                RequiredBeforeProductiveStorageRedacted =
                [
                    "Storage ADR.",
                    "Persistence audit.",
                    "Redaction audit.",
                    "Rollback plan."
                ]
            }
        };

    private static NodalOsConsentStorageRecordDraft CreateRecordDraft(NodalOsOperationalCapability capability, int index) =>
        new()
        {
            RecordDraftId = $"consent-storage-record-draft-{index + 1:000}-{capability}",
            Capability = capability,
            ScopeRef = $"scope-ref-{capability}",
            ConsentStatus = NodalOsConsentScopeStatus.Draft,
            FreshnessStatus = NodalOsConsentScopeStatus.Draft,
            RevocationStatus = NodalOsConsentScopeStatus.Draft,
            PurposeRedacted = "Draft storage model review.",
            RiskSummaryRedacted = "Record draft cannot authorize real use.",
            EvidenceRefs = [$"evidence-ref-consent-storage-{index + 1:000}"],
            TimelineRefs = [$"timeline-ref-consent-storage-{index + 1:000}"],
            CreatedAt = FixtureTime.AddMinutes(index),
            IsDraftOnly = true,
            IsAuthoritative = false,
            ContainsRawSecret = false,
            ContainsRawFileContent = false,
            ContainsUnredactedPath = false,
            CanAuthorizeRealUse = false
        };
}

public sealed class NodalOsDisabledConsentStorageContractJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsDisabledConsentStorageContract contract) => JsonSerializer.Serialize(contract, Options);
}
