using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsNoMutationProofContractService
{
    public NodalOsNoMutationProofContract CreateContract(
        NodalOsDisabledPathJailPrototypeGate gate,
        NodalOsSyntheticCanonicalizationMatrix matrix) =>
        new()
        {
            ProofId = "no-mutation-proof-contract-m557",
            WorkspaceRef = gate.WorkspaceRef,
            MissionRef = gate.MissionRef,
            PathJailGateRef = gate.GateId,
            CanonicalizationMatrixRef = matrix.MatrixId,
            IsContractOnly = true,
            UsesRealFilesystem = false,
            PerformsMutation = false,
            PerformsFileWrite = false,
            PerformsFileDelete = false,
            PerformsFileMove = false,
            PerformsDirectoryCreate = false,
            PerformsPermissionChange = false,
            PerformsMetadataWrite = false,
            PerformsLocking = false,
            RequiresRuntimeAuditBeforeRealUse = true,
            ForbiddenOperationsRedacted =
            [
                "Write content.",
                "Delete content.",
                "Move or rename content.",
                "Create folder.",
                "Delete folder.",
                "Change permissions.",
                "Touch timestamp.",
                "Write metadata.",
                "Lock content.",
                "Create temporary content.",
                "Create hidden marker content.",
                "Write cache.",
                "Write index.",
                "Write usage metrics."
            ]
        };

    public NodalOsNoMutationProofResult Evaluate(NodalOsNoMutationProofContract contract) =>
        new()
        {
            ResultId = "no-mutation-proof-result-m557",
            ProofRef = contract.ProofId,
            ContractDeclaresNoMutation = true,
            ReadyForSyntheticNoMutationReview = true,
            ReadyForRealNoMutationGuarantee = false,
            ReadyForRealFilesystemAccess = false,
            ReadyForRealScan = false,
            MissingRequirementsRedacted =
            [
                "Runtime-level no-mutation audit remains required.",
                "Operational workspace access audit remains required.",
                "Rollback disable strategy remains required."
            ],
            RequiredAuditBeforeRealUseRedacted =
            [
                "Real scan readiness ADR re-review.",
                "Disabled prototype gate audit.",
                "Synthetic canonicalization matrix review."
            ],
            UserFacingExplanationRedacted = "No-mutation proof is contract-only and necessary but not sufficient for future operational scan behavior.",
            RealScanReadinessAdrRef = "real-scan-readiness-adr-m554",
            DisabledPathJailGateRef = contract.PathJailGateRef,
            SyntheticCanonicalizationMatrixRef = contract.CanonicalizationMatrixRef,
            NecessaryButNotSufficientForFutureOperationalScan = true
        };
}

public sealed class NodalOsNoMutationProofContractJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeContract(NodalOsNoMutationProofContract contract) => JsonSerializer.Serialize(contract, Options);
    public string SerializeResult(NodalOsNoMutationProofResult result) => JsonSerializer.Serialize(result, Options);
}

