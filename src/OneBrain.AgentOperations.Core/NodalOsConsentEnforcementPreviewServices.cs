using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentEnforcementPreviewService
{
    public NodalOsConsentEnforcementPreview CreatePreview(
        IReadOnlyList<NodalOsCapabilityAccessGate> gates,
        NodalOsSyntheticFailureModeMatrix failureMatrix) =>
        new()
        {
            PreviewId = "consent-enforcement-preview-m563",
            ConsentPolicyRef = "consent-policy-preview-m563",
            CapabilityGateRefs = gates.Select(g => g.GateId).ToArray(),
            FailureModeMatrixRef = failureMatrix.MatrixId,
            WorkspaceRef = "workspace-ref-m563",
            MissionRef = "mission-ref-m563",
            IsPreviewOnly = true,
            IsNoOp = true,
            UsesRealFilesystem = false,
            EnforcesConsentOnRealOperation = false,
            CanAuthorizeRealCapability = false,
            CanPersistConsent = false,
            CanBypassConsent = false,
            Rules = gates.Select(g => new NodalOsConsentEnforcementRule
            {
                RuleId = $"consent-rule-{g.Capability}",
                Capability = g.Capability,
                ConsentRequired = true,
                ConsentScopeRequired = true,
                ConsentFreshnessRequired = true,
                RevocationSupportedInFuture = true,
                NarrowScopeRequiredForSensitiveCapability = true,
                UserFacingExplanationRequired = true,
                FailClosedIfMissing = true
            }).ToArray(),
            Result = new()
            {
                ResultId = "consent-enforcement-preview-result-m563",
                ConsentEnforcementMode = NodalOsConsentEnforcementMode.PreviewOnly,
                ReadyForSyntheticConsentReview = true,
                ReadyForProductiveConsentEnforcement = false,
                ReadyForRealFilesystemAccess = false,
                ReadyForRealScan = false,
                ReadyForIndexing = false,
                ReadyForRepresentationBuild = false,
                ReadyForLlmContext = false,
                MissingRequirementsRedacted = ["Productive consent store and revocation audit."],
                RequiredBeforeRealUseRedacted = ["Explicit future milestone, user consent enforcement, and audit."],
                UserFacingSummaryRedacted = "Consent enforcement preview is no-op and cannot authorize operational capabilities."
            }
        };

    public NodalOsConsentReviewOptionResult ApplyOption(NodalOsConsentReviewOption option) =>
        new()
        {
            OptionResultId = $"consent-review-option-{option}",
            Option = option,
            IsNoOp = true,
            MutatesState = false,
            AuthorizesRealCapability = false,
            PersistsConsent = false
        };
}

public sealed class NodalOsConsentEnforcementPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreview(NodalOsConsentEnforcementPreview preview) => JsonSerializer.Serialize(preview, Options);
    public string SerializeOption(NodalOsConsentReviewOptionResult result) => JsonSerializer.Serialize(result, Options);
}

