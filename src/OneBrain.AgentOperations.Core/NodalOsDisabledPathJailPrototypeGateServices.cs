using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsDisabledPathJailPrototypeGateService
{
    public NodalOsDisabledPathJailPrototypeGate CreateGate(
        string workspaceRef = "workspace-ref-m555",
        string missionRef = "mission-ref-m555") =>
        new()
        {
            GateId = "disabled-path-jail-prototype-gate-m555",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            PathJailPrototypeRef = "path-jail-prototype-contract-m547",
            RealScanReadinessAdrRef = "real-scan-readiness-adr-m554",
            SyntheticBaselineRef = "synthetic-baseline-m552-m554",
            DisabledByDefault = true,
            RequiresExplicitFutureEnablement = true,
            RequiresAuditBeforeEnablement = true,
            RequiresConsentBeforeEnablement = true,
            RequiresNoMutationProofBeforeEnablement = true,
            RequiresCancellationPolicyBeforeEnablement = true,
            RequiresEvidenceTimelinePlanBeforeEnablement = true,
            UsesRealFilesystem = false,
            PerformsRealCanonicalization = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            CanAuthorizeRealScan = false,
            CanAuthorizeFilesystemAccess = false,
            EnablementRequirementsRedacted =
            [
                "Explicit future milestone.",
                "User consent enforcement.",
                "Path containment implementation audit.",
                "Canonicalization implementation audit.",
                "No-mutation proof.",
                "Cancellation semantics.",
                "Evidence and timeline emission plan.",
                "Redaction and sensitive-data policy enforcement.",
                "Exclusion policy enforcement.",
                "Rollback and disable strategy.",
                "Local-only guarantee."
            ]
        };

    public NodalOsDisabledPathJailPrototypeGateDecision Decide(NodalOsDisabledPathJailPrototypeGate gate) =>
        new()
        {
            DecisionId = "disabled-path-jail-prototype-gate-decision-m555",
            GateRef = gate.GateId,
            PrototypeGateCreated = true,
            PrototypeGateEnabled = false,
            ReadyForDisabledPrototypeReview = true,
            ReadyForRealPathJail = false,
            ReadyForRealFilesystemAccess = false,
            ReadyForRealScan = false,
            ReadyForDirectoryListing = false,
            ReadyForFileRead = false,
            ReadyForFileHash = false,
            ReadyForIndexing = false,
            ReadyForRepresentationBuild = false,
            ReadyForLlmContext = false
        };
}

public sealed class NodalOsDisabledPathJailPrototypeGateJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeGate(NodalOsDisabledPathJailPrototypeGate gate) => JsonSerializer.Serialize(gate, Options);
    public string SerializeDecision(NodalOsDisabledPathJailPrototypeGateDecision decision) => JsonSerializer.Serialize(decision, Options);
}

public static class NodalOsDisabledPathJailPrototypeGateFixtures
{
    public static NodalOsDisabledPathJailPrototypeGate Gate() =>
        new NodalOsDisabledPathJailPrototypeGateService().CreateGate();
}

