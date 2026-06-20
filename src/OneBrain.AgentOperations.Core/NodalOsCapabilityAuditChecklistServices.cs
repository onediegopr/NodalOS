using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsCapabilityAuditChecklistService
{
    public NodalOsCapabilityAuditChecklist CreateChecklist(
        IReadOnlyList<NodalOsCapabilityAccessGate> gates,
        string workspaceRef = "workspace-ref-m568",
        string missionRef = "mission-ref-m568")
    {
        var categories = Enum.GetValues<NodalOsCapabilityAuditRequirementCategory>();

        return new()
        {
            ChecklistId = "capability-audit-checklist-m568",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            CapabilityGateRefs = gates.Select(gate => gate.GateId).ToArray(),
            OperationalAccessAuditAdrRef = "operational-access-audit-adr-m559",
            FailClosedAcceptancePackRef = "fail-closed-acceptance-pack-m566",
            ChecklistStatus = NodalOsCapabilityAuditChecklistStatus.ContractChecklistComplete,
            IsChecklistOnly = true,
            CanAuthorizeCapability = false,
            CanEnableGate = false,
            CanAccessFilesystem = false,
            CanBuildLlmContext = false,
            CanUseCloud = false,
            Items = categories.Select((category, index) => CreateItem(category, gates[index % gates.Count].Capability, index)).ToArray(),
            Decision = CreateDecision()
        };
    }

    private static NodalOsCapabilityAuditChecklistItem CreateItem(
        NodalOsCapabilityAuditRequirementCategory category,
        NodalOsOperationalCapability capability,
        int index) =>
        new()
        {
            ItemId = $"capability-audit-item-{index + 1:000}-{category}",
            Capability = capability,
            RequirementRedacted = $"{category} must be satisfied before future enablement.",
            RequirementCategory = category,
            RequiredBeforeEnablement = true,
            Status = NodalOsCapabilityAuditChecklistItemStatus.ContractDocumented,
            EvidenceRef = $"evidence-ref-capability-audit-{index + 1:000}",
            TimelineRef = $"timeline-ref-capability-audit-{index + 1:000}",
            UserFacingExplanationRedacted = "Checklist item blocks operational use until implemented and audited.",
            BlocksRealUseIfMissing = true
        };

    private static NodalOsCapabilityAuditChecklistDecision CreateDecision() =>
        new()
        {
            DecisionId = "capability-audit-checklist-decision-m568",
            ReadyForChecklistCloseout = true,
            ReadyForRealCapabilityEnablement = false,
            ReadyForFilesystemAccess = false,
            ReadyForRealScan = false,
            ReadyForIndexing = false,
            ReadyForRepresentationBuild = false,
            ReadyForLlmContext = false,
            ReadyForCloud = false,
            ReadyForRuntime = false
        };
}

public sealed class NodalOsCapabilityAuditChecklistJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsCapabilityAuditChecklist checklist) => JsonSerializer.Serialize(checklist, Options);
}
