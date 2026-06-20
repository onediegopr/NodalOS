using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAssignmentSafetyAuditPackService
{
    public NodalOsAssignmentSafetyAuditPack CreateAudit(
        NodalOsAssignmentReviewSnapshot snapshot,
        NodalOsPlannerHandoffPack handoff,
        string sourceTextForBoundaryCheck = "")
    {
        var blockedMarkerDetected = ContainsExecutionCapableMarker(sourceTextForBoundaryCheck)
            || !snapshot.MockStorageOnly
            || handoff.Executable
            || handoff.PlannerRuntimeUsed
            || handoff.CallsLlmProvider
            || handoff.RuntimeExecutionAllowed
            || handoff.FilesystemAccessUsed;

        var status = blockedMarkerDetected
            ? NodalOsAssignmentSafetyAuditStatus.Fail
            : NodalOsAssignmentSafetyAuditStatus.Pass;

        return new()
        {
            AuditPackId = "assignment-safety-audit-m528-m530",
            OverallStatus = status,
            Findings = Enum.GetValues<NodalOsAssignmentSafetyAuditDimension>()
                .Select(dimension => Finding(dimension, status))
                .ToArray(),
            NextAuditTriggersRedacted =
            [
                "Any future planner runtime implementation.",
                "Any future provider or model integration.",
                "Any future filesystem or network access.",
                "Any future productive persistence implementation."
            ],
            EvidenceRefs = handoff.EvidenceRefs,
            TimelineRefs = handoff.TimelineRefs,
            GuardrailRefs = handoff.GuardrailRefs,
            ExecutionBoundaryCrossed = blockedMarkerDetected,
            PlannerRuntimeIntroduced = handoff.PlannerRuntimeUsed,
            PromptOrModelIntroduced = handoff.CallsLlmProvider || handoff.CreatesPrompt,
            FilesystemOrNetworkIntroduced = handoff.FilesystemAccessUsed,
            ProductivePersistenceIntroduced = snapshot.ProductivePersistenceUsed
        };
    }

    public bool ContainsExecutionCapableMarker(string sourceTextForBoundaryCheck)
    {
        var markers = new[]
        {
            "ExecutionRequest",
            "CanExecute=true",
            "RuntimeExecutionAllowed=true",
            "PlannerExecutionAllowed=true",
            "CallsLlmProvider=true",
            "FilesystemAccessAllowed=true",
            "NetworkAccessAllowed=true",
            "ProductivePersistenceUsed=true"
        };

        return markers.Any(marker => sourceTextForBoundaryCheck.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static NodalOsAssignmentSafetyAuditFinding Finding(
        NodalOsAssignmentSafetyAuditDimension dimension,
        NodalOsAssignmentSafetyAuditStatus status) =>
        new()
        {
            Dimension = dimension,
            Status = status,
            FindingRedacted = status == NodalOsAssignmentSafetyAuditStatus.Pass
                ? $"{dimension} passed for draft-only assignment preview."
                : $"{dimension} failed because an execution-capable marker was detected.",
            RisksRedacted = status == NodalOsAssignmentSafetyAuditStatus.Pass
                ? []
                : ["Execution boundary may have been crossed."],
            RequiredFixesRedacted = status == NodalOsAssignmentSafetyAuditStatus.Pass
                ? []
                : ["Remove execution-capable marker before closing the milestone."]
        };
}

public sealed class NodalOsAssignmentSafetyAuditPackJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsAssignmentSafetyAuditPack audit) =>
        JsonSerializer.Serialize(audit, Options);
}

public static class NodalOsAssignmentReviewHandoffSafetyFixtures
{
    public static NodalOsAssignmentReviewSnapshot Snapshot()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();
        var result = NodalOsAssignmentUiPreviewFixtures.Interaction(NodalOsTaskGraphInteractionKind.AddDraftNote);
        var persistence = new NodalOsAssignmentReviewPersistenceMockService();
        return persistence.Store(persistence.CreateSnapshot(preview, result));
    }

    public static NodalOsPlannerHandoffPack Handoff()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();
        var snapshot = Snapshot();
        var acceptance = NodalOsAssignmentUiPreviewFixtures.AcceptancePack();
        return new NodalOsPlannerHandoffService().CreateHandoff(preview, snapshot, acceptance);
    }

    public static NodalOsAssignmentSafetyAuditPack Audit()
    {
        var snapshot = Snapshot();
        var handoff = Handoff();
        return new NodalOsAssignmentSafetyAuditPackService().CreateAudit(snapshot, handoff);
    }
}
