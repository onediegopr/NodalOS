using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsDisabledStorageUiPreviewService
{
    public NodalOsDisabledStorageUiPreview CreatePreview(NodalOsConsentStorageBoundaryTestPack testPack) =>
        new()
        {
            PreviewId = "disabled-storage-ui-preview-m580",
            StorageContractRef = testPack.DisabledConsentStorageContractRef,
            BoundaryTestPackRef = testPack.TestPackId,
            ProductiveConsentStorageAdrRef = testPack.ProductiveConsentStorageAdrRef,
            WorkspaceRef = "workspace-ref-m580",
            MissionRef = "mission-ref-m580",
            IsStaticPreview = true,
            IsReadOnly = true,
            IsNoOp = true,
            DisabledByDefault = true,
            UsesProductivePersistence = false,
            ReadsProductiveStorage = false,
            WritesProductiveStorage = false,
            DeletesProductiveStorage = false,
            CanEnableStorage = false,
            CanPersistConsent = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeLlmContext = false,
            CanUseCloud = false,
            UiSectionsRedacted =
            [
                "Storage disabled status.",
                "Boundary rules summary.",
                "Record draft shape.",
                "Fail-closed behavior.",
                "Missing requirements.",
                "Audit requirements.",
                "Rollback and disable requirements.",
                "User-facing risk explanation.",
                "Next allowed design step."
            ],
            ReviewOptions = Enum.GetValues<NodalOsDisabledStorageUiReviewOptionKind>().Select((kind, index) => new NodalOsDisabledStorageUiReviewOption
            {
                OptionId = $"disabled-storage-review-option-{index + 1:000}-{kind}",
                Kind = kind,
                IsNoOp = true,
                CanAuthorize = false,
                CanPersist = false,
                UserFacingExplanationRedacted = $"{kind} is preview-only and cannot change storage state."
            }).ToArray(),
            DisclosuresRedacted =
            [
                "No consent is persisted.",
                "No storage is read.",
                "No storage is written.",
                "No consent is enforced.",
                "No capability is authorized.",
                "No filesystem, LLM, cloud, or runtime access is granted."
            ]
        };
}

public sealed class NodalOsDisabledStorageUiPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsDisabledStorageUiPreview preview) => JsonSerializer.Serialize(preview, Options);
}
