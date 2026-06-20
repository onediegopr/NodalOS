using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsProductiveConsentStorageAdrService
{
    public NodalOsProductiveConsentStorageAdrSummary CreateSummary(NodalOsConsentAdversarialTestMatrix matrix) =>
        new()
        {
            AdrId = "productive-consent-storage-implementation-adr-m577",
            AdrPathRef = "docs/architecture/productive-consent-storage-implementation-adr.md",
            DesignReviewRef = matrix.DesignReviewRef,
            AdversarialMatrixRef = matrix.MatrixId,
            DecisionStatus = NodalOsProductiveConsentStorageAdrDecisionStatus.ProductiveConsentStorageNotImplementedAdrReady,
            ProductiveConsentStorageImplemented = false,
            FutureImplementationDisabledByDefault = true,
            FutureImplementationLocalFirst = true,
            RequiresScopeBoundRecords = true,
            RequiresCapabilityBoundRecords = true,
            RequiresWorkspaceBoundRecords = true,
            RequiresMissionBoundRecords = true,
            StorageMayContainSensitiveMaterial = false,
            StorageMayContainContentPayloads = false,
            StorageMayContainUnredactedBroadPaths = false,
            StorageCanImplyFilesystemAccess = false,
            StorageCanImplyLlmCloudProviderRuntimePermission = false,
            ConsentCanImplyAnotherCapability = false,
            RevokedStaleMissingConsentFailsClosed = true,
            StorageAndEnforcementSeparateMilestones = true,
            StorageAndCapabilityEnablementSeparateMilestones = true,
            RequiredBeforeImplementationRedacted =
            [
                "Storage boundary ADR.",
                "Migration, rollback, and disable strategy.",
                "Adversarial test matrix.",
                "Redaction review.",
                "Evidence and timeline emission.",
                "Audit before enablement.",
                "Full suite and guard checks.",
                "No operational access as part of storage implementation."
            ],
            RejectedAlternativesRedacted =
            [
                "Direct productive storage.",
                "Implicit capability inheritance.",
                "Storage plus enforcement in one milestone.",
                "Storage plus capability enablement in one milestone.",
                "Cloud-backed default storage."
            ]
        };
}

public sealed class NodalOsProductiveConsentStorageAdrJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsProductiveConsentStorageAdrSummary summary) => JsonSerializer.Serialize(summary, Options);
}
