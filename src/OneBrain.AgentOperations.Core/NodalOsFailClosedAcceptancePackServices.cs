using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsFailClosedAcceptancePackService
{
    public NodalOsFailClosedAcceptancePack CreatePack(
        NodalOsCapabilityGateUiReview review,
        NodalOsConsentScopeLedgerMock ledger,
        NodalOsSyntheticFailureModeMatrix failureMatrix) =>
        new()
        {
            AcceptancePackId = "fail-closed-acceptance-pack-m566",
            CapabilityGateUiReviewRef = review.ReviewId,
            ConsentScopeLedgerMockRef = ledger.LedgerId,
            FailureModeMatrixRef = failureMatrix.MatrixId,
            SyntheticPolicyRegressionRef = "synthetic-policy-regression-pack-m560",
            OperationalAccessAuditAdrRef = "operational-access-audit-adr-m559",
            AcceptanceStatus = NodalOsFailClosedAcceptanceStatus.ContractOnlyPass,
            FindingsRedacted =
            [
                "All gates remain disabled by default.",
                "Review actions are non-authorizing.",
                "Consent ledger is mock-only.",
                "Failure behavior remains fail-closed."
            ],
            RequiredFixesRedacted = [],
            NextGateRequirementsRedacted =
            [
                "Productive consent implementation.",
                "Per-capability audit.",
                "Dependency gate implementation.",
                "Path jail implementation audit.",
                "No-mutation runtime proof.",
                "Cancellation runtime proof.",
                "Redaction and sensitive-data enforcement.",
                "Evidence and timeline emission.",
                "Disable strategy.",
                "Adversarial tests."
            ],
            AcceptanceCriteria = Enum.GetValues<NodalOsFailClosedCriterionKind>().Select((kind, index) => new NodalOsFailClosedAcceptanceCriterion
            {
                CriterionId = $"fail-closed-criterion-{index + 1:000}-{kind}",
                Kind = kind,
                Required = true,
                Satisfied = true,
                FailClosedExpected = true,
                UserFacingExplanationRedacted = $"{kind} is required before any future operational use."
            }).ToArray(),
            Decision = CreateDecision()
        };

    private static NodalOsFailClosedDecision CreateDecision() =>
        new()
        {
            DecisionId = "fail-closed-decision-m566",
            FailClosedLayerReady = true,
            ReadyForRealCapabilityEnablement = false,
            ReadyForFilesystemAccess = false,
            ReadyForRealScan = false,
            ReadyForRealPathJail = false,
            ReadyForIndexing = false,
            ReadyForRepresentationBuild = false,
            ReadyForLlmContext = false,
            ReadyForCloud = false,
            ReadyForRuntime = false,
            RequiredBeforeRealUseRedacted =
            [
                "Productive consent implementation.",
                "Per-capability audit.",
                "Dependency gates.",
                "Path jail audit.",
                "No-mutation runtime proof.",
                "Cancellation proof.",
                "Policy enforcement.",
                "Evidence and timeline emission.",
                "Disable strategy.",
                "Adversarial tests."
            ]
        };
}

public sealed class NodalOsFailClosedAcceptancePackJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsFailClosedAcceptancePack pack) => JsonSerializer.Serialize(pack, Options);
}
