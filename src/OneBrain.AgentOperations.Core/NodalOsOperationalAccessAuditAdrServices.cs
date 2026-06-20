using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsOperationalAccessAuditAdrService
{
    public NodalOsOperationalAccessAuditAdrSummary CreateSummary() =>
        new()
        {
            AdrId = "operational-access-audit-adr-m559",
            DecisionRecordPath = "docs/architecture/operational-access-audit-before-filesystem-access-adr.md",
            DecisionStatus = NodalOsOperationalAccessAuditDecisionStatus.OperationalFilesystemAccessNotReadyAuditRequired,
            OperationalFilesystemAccessReady = false,
            DisabledPathJailGateIsPreconditionOnly = true,
            RequiresExplicitFutureMilestone = true,
            RequiresDisabledByDefaultGate = true,
            RequiresUserConsentEnforcement = true,
            RequiresPathJailImplementationAudit = true,
            RequiresCanonicalizationImplementationAudit = true,
            RequiresNoMutationRuntimeProof = true,
            RequiresCancellationSemantics = true,
            RequiresLocalOnlyGuarantee = true,
            RequiresRedactionAndSensitiveDataEnforcement = true,
            RequiresExclusionPolicyEnforcement = true,
            RequiresEvidenceTimelineEmission = true,
            RequiresKillSwitchRollbackDisableStrategy = true,
            RequiresFullSuiteAndAdversarialTests = true,
            CanonicalizationMustBeExplicitlyAudited = true,
            FolderEnumerationRequiresSeparateGate = true,
            ContentAccessRequiresSeparateGate = true,
            ContentFingerprintingRequiresSeparateGate = true,
            IndexingRepresentationAndLlmContextBlocked = true,
            CloudProviderRuntimeBlocked = true,
            SyntheticRegressionNecessaryNotSufficient = true,
            FuturePrototypeMustFailClosed = true
        };
}

public sealed class NodalOsOperationalAccessAuditAdrJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeSummary(NodalOsOperationalAccessAuditAdrSummary summary) => JsonSerializer.Serialize(summary, Options);
}

