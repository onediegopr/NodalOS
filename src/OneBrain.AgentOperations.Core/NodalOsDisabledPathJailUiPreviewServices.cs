using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsDisabledPathJailUiPreviewService
{
    public NodalOsDisabledPathJailUiPreview CreatePreview(
        NodalOsDisabledPathJailPrototypeGate gate,
        NodalOsSyntheticCanonicalizationMatrix matrix,
        NodalOsNoMutationProofContract proof) =>
        new()
        {
            PreviewId = "disabled-path-jail-ui-preview-m558",
            GateRef = gate.GateId,
            CanonicalizationMatrixRef = matrix.MatrixId,
            NoMutationProofRef = proof.ProofId,
            RealScanReadinessAdrRef = gate.RealScanReadinessAdrRef,
            WorkspaceRef = gate.WorkspaceRef,
            MissionRef = gate.MissionRef,
            IsStaticPreview = true,
            IsReadOnly = true,
            IsNoOp = true,
            DisabledByDefault = true,
            UsesRealFilesystem = false,
            PerformsRealCanonicalization = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            CanEnablePrototype = false,
            CanAuthorizeRealScan = false,
            CanAuthorizeFilesystemAccess = false,
            Sections = new()
            {
                GateStatusRedacted = "Prototype gate is disabled by default.",
                WhyDisabledRedacted = "Operational access requires a separate audited milestone.",
                EnablementRequirementsRedacted = gate.EnablementRequirementsRedacted,
                CanonicalizationCoverageRedacted = $"{matrix.Cases.Count} synthetic canonicalization groups are represented.",
                NoMutationSummaryRedacted = "No-mutation contract is necessary but not sufficient.",
                BlockedCapabilitiesRedacted =
                [
                    "OS path resolution remains blocked.",
                    "Operational workspace access remains blocked.",
                    "Indexing, representation build, LLM context, provider, cloud, and runtime remain blocked."
                ],
                RiskExplanationsRedacted =
                [
                    "Synthetic path cases do not prove OS-specific behavior.",
                    "Contract-only no-mutation does not prove runtime no-mutation."
                ],
                NextRequiredGatesRedacted =
                [
                    "Operational access audit ADR.",
                    "Runtime no-mutation proof.",
                    "Consent and rollback gate."
                ],
                AuditRequirementsRedacted =
                [
                    "Path jail implementation audit.",
                    "Canonicalization implementation audit.",
                    "Adversarial policy regression audit."
                ],
                UserFacingExplanationRedacted = "This preview is static and cannot enable operational behavior."
            },
            DisclosuresRedacted =
            [
                "No operational filesystem was accessed.",
                "No OS path resolution was performed.",
                "No operational scan behavior was performed.",
                "No content access, fingerprinting, or folder enumeration was performed.",
                "No mutation is possible from this preview.",
                "UI cannot enable the prototype.",
                "Future enablement requires a separate milestone, consent, and audit."
            ]
        };

    public NodalOsDisabledPathJailUiReviewResult ApplyOption(NodalOsDisabledPathJailUiReviewOption option) =>
        new()
        {
            ResultId = $"disabled-path-jail-ui-option-{option}",
            Option = option,
            IsNoOp = true,
            MutatesState = false,
            EnablesPrototype = false,
            AuthorizesRealScan = false,
            AuthorizesFilesystemAccess = false,
            UserFacingExplanationRedacted = "Review option is no-op and cannot authorize operational behavior."
        };
}

public sealed class NodalOsDisabledPathJailUiPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreview(NodalOsDisabledPathJailUiPreview preview) => JsonSerializer.Serialize(preview, Options);
    public string SerializeReviewResult(NodalOsDisabledPathJailUiReviewResult result) => JsonSerializer.Serialize(result, Options);
}

