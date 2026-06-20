using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAuditCheckpointReviewService
{
    public NodalOsAuditCheckpointReview CreateReview(string workspaceRef = "workspace-ref-m570", string missionRef = "mission-ref-m570") =>
        new()
        {
            CheckpointId = "audit-checkpoint-review-m570",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            CoveredMilestones =
            [
                "M534-M536",
                "M537-M539",
                "M540-M542",
                "M543-M545",
                "M546-M548",
                "M549-M551",
                "M552-M554",
                "M555-M557",
                "M558-M560",
                "M561-M563",
                "M564-M566",
                "M567-M569"
            ],
            CoveredDecisionRefs =
            [
                "PROJECT_UNDERSTANDING_PRECONDITIONS_READY",
                "PROJECT_UNDERSTANDING_SCAN_GATE_READY",
                "PROJECT_UNDERSTANDING_DRY_RUN_POLICY_READY",
                "PROJECT_UNDERSTANDING_DRY_RUN_REVIEW_READY",
                "PROJECT_UNDERSTANDING_IMPLEMENTATION_BOUNDARY_READY",
                "SYNTHETIC_DRY_RUN_SIMULATOR_READY",
                "REAL_SCAN_NOT_READY_SYNTHETIC_BASELINE_READY",
                "DISABLED_PATH_JAIL_PROTOTYPE_GATE_READY",
                "OPERATIONAL_FILESYSTEM_ACCESS_NOT_READY_AUDIT_REQUIRED",
                "PER_CAPABILITY_ACCESS_GATES_READY",
                "CAPABILITY_GATE_REVIEW_ACCEPTANCE_READY",
                "REAL_ACCESS_BLOCKER_CLOSEOUT_READY"
            ],
            CoveredArtifactRefs =
            [
                "artifacts/agent-operations/m539",
                "artifacts/agent-operations/m542",
                "artifacts/agent-operations/m545",
                "artifacts/agent-operations/m548",
                "artifacts/agent-operations/m551",
                "artifacts/agent-operations/m554",
                "artifacts/agent-operations/m557",
                "artifacts/agent-operations/m560",
                "artifacts/agent-operations/m563",
                "artifacts/agent-operations/m566",
                "artifacts/agent-operations/m569"
            ],
            GovernanceBaselineRef = "real-access-blocker-closeout-m569",
            RealAccessBlockerCloseoutRef = "real-access-blocker-closeout-m569",
            ReviewStatus = NodalOsAuditCheckpointReviewStatus.CheckpointComplete,
            IsCheckpointOnly = true,
            CanAuthorizeImplementation = false,
            CanEnableRealAccess = false,
            CanAccessFilesystem = false,
            CanBuildLlmContext = false,
            CanUseCloud = false,
            CanTriggerRuntime = false,
            CoveredScope = Enum.GetValues<NodalOsAuditCheckpointScopeItem>(),
            Findings = CreateFindings(),
            Decision = CreateDecision()
        };

    private static NodalOsAuditCheckpointFindings CreateFindings() =>
        new()
        {
            StrengthsRedacted =
            [
                "Governance baseline is explicit.",
                "Per-capability boundaries are separated.",
                "Fail-closed rules are represented."
            ],
            UnresolvedBlockersRedacted =
            [
                "Productive consent is not implemented.",
                "Operational access remains blocked.",
                "Future audited implementation is still required."
            ],
            RisksRedacted = ["Premature enablement remains the main risk."],
            RequiredNextDecisionsRedacted = ["Choose next design-only milestone."],
            AuditGapsRedacted = ["Productive implementation audit does not exist yet."],
            SafetyGapsRedacted = ["Runtime safety proof does not exist yet."],
            ImplementationGapsRedacted = ["No operational implementation has started."],
            NamingDebtNotesRedacted = ["Use redacted and ref-only labels consistently."]
        };

    private static NodalOsAuditCheckpointDecision CreateDecision() =>
        new()
        {
            DecisionId = "audit-checkpoint-decision-m570",
            GovernanceBaselineReady = true,
            ReadyForDirectRealImplementation = false,
            ReadyForProductiveConsentImplementation = false,
            ReadyForRealPathJailImplementation = false,
            ReadyForRealFilesystemAccess = false,
            ReadyForRealScan = false,
            ReadyForLlmContext = false,
            RecommendedNextPhaseRedacted = "Productive consent design review, still design-only."
        };
}

public sealed class NodalOsAuditCheckpointReviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsAuditCheckpointReview review) => JsonSerializer.Serialize(review, Options);
}
