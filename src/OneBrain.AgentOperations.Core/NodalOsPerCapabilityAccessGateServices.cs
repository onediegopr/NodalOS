using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsPerCapabilityAccessGateService
{
    public IReadOnlyList<NodalOsCapabilityAccessGate> CreateGates(string workspaceRef = "workspace-ref-m561", string missionRef = "mission-ref-m561") =>
        Enum.GetValues<NodalOsOperationalCapability>().Select(capability => new NodalOsCapabilityAccessGate
        {
            GateId = $"capability-gate-{capability}",
            Capability = capability,
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            ParentOperationalAuditRef = "operational-access-audit-adr-m559",
            DisabledByDefault = true,
            RequiresExplicitFutureEnablement = true,
            RequiresUserConsent = true,
            RequiresAuditBeforeEnablement = true,
            RequiresEvidenceTimelinePlan = true,
            RequiresCancellationSemantics = true,
            RequiresNoMutationProof = true,
            RequiresRedactionPolicy = true,
            RequiresSecretDetectionPolicy = true,
            RequiresExclusionPolicy = true,
            FailClosed = true,
            IsContractOnly = true
        }).ToArray();

    public NodalOsCapabilityGateDecision Decide(NodalOsCapabilityAccessGate gate) =>
        new()
        {
            DecisionId = $"capability-gate-decision-{gate.Capability}",
            Capability = gate.Capability,
            GateCreated = true,
            GateEnabled = false,
            ReadyForSyntheticReview = true,
            ReadyForRealUse = false,
            CanAuthorizeCapability = false,
            CanAccessFilesystem = false,
            CanReadContent = false,
            CanFingerprintContent = false,
            CanBuildRepresentation = false,
            CanSendToLlm = false,
            CanSendToCloud = false,
            RequiredBeforeEnablementRedacted =
            [
                "Explicit future milestone.",
                "Consent enforcement.",
                "Audit before enablement.",
                "Evidence and timeline plan.",
                "Fail-closed proof."
            ],
            UserFacingExplanationRedacted = "Capability gate is contract-only and cannot authorize operational behavior."
        };

    public NodalOsCapabilityDependencyMatrix CreateDependencyMatrix() =>
        new()
        {
            MatrixId = "capability-dependency-matrix-m561",
            Dependencies =
            [
                Dependency(NodalOsOperationalCapability.PathCanonicalization),
                Dependency(NodalOsOperationalCapability.DirectoryListing, NodalOsOperationalCapability.PathCanonicalization),
                Dependency(NodalOsOperationalCapability.FileRead, NodalOsOperationalCapability.PathCanonicalization, NodalOsOperationalCapability.DirectoryListing),
                Dependency(NodalOsOperationalCapability.FileHash, NodalOsOperationalCapability.FileRead),
                Dependency(NodalOsOperationalCapability.SecretDetection, NodalOsOperationalCapability.FileRead),
                Dependency(NodalOsOperationalCapability.ExclusionEnforcement, NodalOsOperationalCapability.PathCanonicalization),
                Dependency(NodalOsOperationalCapability.Indexing, NodalOsOperationalCapability.FileRead, NodalOsOperationalCapability.SecretDetection, NodalOsOperationalCapability.ExclusionEnforcement),
                Dependency(NodalOsOperationalCapability.RepresentationBuild, NodalOsOperationalCapability.Indexing),
                Dependency(NodalOsOperationalCapability.LlmContextBuild, NodalOsOperationalCapability.FileRead, NodalOsOperationalCapability.SecretDetection),
                Dependency(NodalOsOperationalCapability.CloudSync),
                Dependency(NodalOsOperationalCapability.ProviderCall),
                new()
                {
                    Capability = NodalOsOperationalCapability.RuntimeExecution,
                    DependsOn = [],
                    PolicyDependenciesRedacted = ["Positive execution gate, not path gate."]
                }
            ]
        };

    private static NodalOsCapabilityDependency Dependency(NodalOsOperationalCapability capability, params NodalOsOperationalCapability[] dependencies) =>
        new()
        {
            Capability = capability,
            DependsOn = dependencies,
            PolicyDependenciesRedacted = capability switch
            {
                NodalOsOperationalCapability.FileRead => ["Consent policy."],
                NodalOsOperationalCapability.FileHash => ["Explicit fingerprint gate."],
                NodalOsOperationalCapability.RepresentationBuild => ["LLM/representation governance."],
                NodalOsOperationalCapability.LlmContextBuild => ["Redaction, user approval, and provider policy."],
                NodalOsOperationalCapability.CloudSync => ["Separate cloud policy and user consent."],
                _ => []
            }
        };
}

public sealed class NodalOsPerCapabilityAccessGateJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeGates(IReadOnlyList<NodalOsCapabilityAccessGate> gates) => JsonSerializer.Serialize(gates, Options);
    public string SerializeDecisions(IReadOnlyList<NodalOsCapabilityGateDecision> decisions) => JsonSerializer.Serialize(decisions, Options);
    public string SerializeDependencyMatrix(NodalOsCapabilityDependencyMatrix matrix) => JsonSerializer.Serialize(matrix, Options);
}

