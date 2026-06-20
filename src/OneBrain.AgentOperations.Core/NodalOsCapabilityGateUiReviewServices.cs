using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsCapabilityGateUiReviewService
{
    public NodalOsCapabilityGateUiReview CreateReview(
        IReadOnlyList<NodalOsCapabilityAccessGate> gates,
        NodalOsCapabilityDependencyMatrix dependencyMatrix,
        string workspaceRef = "workspace-ref-m564",
        string missionRef = "mission-ref-m564") =>
        new()
        {
            ReviewId = "capability-gate-ui-review-m564",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            CapabilityGateRefs = gates.Select(gate => gate.GateId).ToArray(),
            FailureModeMatrixRef = "synthetic-failure-mode-matrix-m562",
            ConsentEnforcementPreviewRef = "consent-enforcement-preview-m563",
            OperationalAccessAuditAdrRef = "operational-access-audit-adr-m559",
            IsStaticPreview = true,
            IsReadOnly = true,
            IsNoOp = true,
            UsesRealFilesystem = false,
            CanEnableGate = false,
            CanAuthorizeCapability = false,
            CanPersistConsent = false,
            CanAccessFilesystem = false,
            CanReadContent = false,
            CanFingerprintContent = false,
            CanBuildRepresentation = false,
            CanSendToLlm = false,
            CanSendToCloud = false,
            UiSectionsRedacted =
            [
                "Capability gate summary.",
                "Dependency map.",
                "Synthetic failure summary.",
                "Consent requirement summary.",
                "Disabled-by-default disclosures.",
                "Fail-closed disclosures.",
                "Blocked capabilities.",
                "Missing requirements.",
                "User-facing explanations.",
                "Next required gates and audits."
            ],
            ReviewCards = gates.Select(gate => CreateCard(gate, dependencyMatrix)).ToArray(),
            ReviewOptions = Enum.GetValues<NodalOsCapabilityGateReviewOption>().Select(ApplyOption).ToArray()
        };

    public NodalOsCapabilityGateReviewOptionResult ApplyOption(NodalOsCapabilityGateReviewOption option) =>
        new()
        {
            OptionResultId = $"capability-gate-review-option-{option}",
            Option = option,
            IsNoOp = true,
            MutatesState = false,
            CanEnableGate = false,
            AuthorizesCapability = false,
            PersistsConsent = false
        };

    private static NodalOsCapabilityReviewCard CreateCard(
        NodalOsCapabilityAccessGate gate,
        NodalOsCapabilityDependencyMatrix dependencyMatrix)
    {
        var dependency = dependencyMatrix.Dependencies.Single(item => item.Capability == gate.Capability);

        return new()
        {
            CardId = $"capability-review-card-{gate.Capability}",
            Capability = gate.Capability,
            GateEnabled = false,
            DisabledByDefault = true,
            RequiredConsent = gate.RequiresUserConsent,
            RequiredAudit = gate.RequiresAuditBeforeEnablement,
            RequiredDependencies = dependency.DependsOn,
            FailClosed = gate.FailClosed,
            UserFacingRiskExplanationRedacted = "Capability remains disabled until separate audited enablement exists.",
            ReviewStatus = NodalOsCapabilityGateReviewStatus.DraftReview
        };
    }
}

public sealed class NodalOsCapabilityGateUiReviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsCapabilityGateUiReview review) => JsonSerializer.Serialize(review, Options);
    public string SerializeOption(NodalOsCapabilityGateReviewOptionResult result) => JsonSerializer.Serialize(result, Options);
}
