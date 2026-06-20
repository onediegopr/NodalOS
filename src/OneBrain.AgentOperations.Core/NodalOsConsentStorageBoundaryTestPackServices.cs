using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentStorageBoundaryTestPackService
{
    public NodalOsConsentStorageBoundaryTestPack CreateTestPack(
        NodalOsProductiveConsentStorageAdrSummary adrSummary,
        NodalOsConsentGovernanceCloseout closeout,
        NodalOsDisabledConsentStorageContract storageContract) =>
        new()
        {
            TestPackId = "consent-storage-boundary-test-pack-m579",
            ProductiveConsentStorageAdrRef = adrSummary.AdrId,
            ConsentGovernanceCloseoutRef = closeout.CloseoutId,
            DisabledConsentStorageContractRef = storageContract.StorageContractId,
            WorkspaceRef = "workspace-ref-m579",
            MissionRef = "mission-ref-m579",
            IsBoundaryTestPackOnly = true,
            UsesProductivePersistence = false,
            ReadsProductiveStorage = false,
            WritesProductiveStorage = false,
            DeletesProductiveStorage = false,
            MigratesProductiveStorage = false,
            SyncsToCloud = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeLlmContext = false,
            TestCases = Enum.GetValues<NodalOsConsentStorageBoundaryCategory>().Select((category, index) => new NodalOsConsentStorageBoundaryTestCase
            {
                CaseId = $"consent-storage-boundary-case-{index + 1:000}-{category}",
                Category = category,
                SyntheticRecordDraftRef = $"synthetic-record-draft-ref-m579-{index + 1:000}",
                ExpectedDecisionRedacted = "Block and fail closed.",
                ExpectedBlockedReasonRedacted = $"{category} blocks productive storage use.",
                RequiresFailClosed = true,
                NeverAuthorizesRealUse = true,
                NeverPersistsProductively = true,
                NeverSendsToLlm = true,
                NeverSendsToCloud = true,
                IsSyntheticOnly = true
            }).ToArray(),
            Decision = new()
            {
                DecisionId = "consent-storage-boundary-decision-m579",
                ReadyForBoundaryReview = true,
                ReadyForProductiveStorageImplementation = false,
                ReadyForProductivePersistence = false,
                ReadyForConsentEnforcement = false,
                ReadyForCapabilityAuthorization = false,
                ReadyForFilesystemAccess = false,
                ReadyForLlmContext = false
            }
        };
}

public sealed class NodalOsConsentStorageBoundaryTestPackJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsConsentStorageBoundaryTestPack testPack) => JsonSerializer.Serialize(testPack, Options);
}
