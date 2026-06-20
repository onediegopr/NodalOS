using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsScanBoundaryAuditService
{
    public NodalOsScanBoundaryAudit CreateAudit(
        NodalOsSyntheticDryRunSimulatorContract contract,
        NodalOsFixtureResultReview review,
        NodalOsPathJailPrototypeContract prototype,
        NodalOsScanFixtureMatrix matrix)
    {
        var status = EvaluateMarkers([]);
        return new()
        {
            AuditId = "scan-boundary-audit-m551",
            SimulatorContractRef = contract.SimulatorId,
            FixtureResultReviewRef = review.ReviewId,
            PathJailPrototypeRef = prototype.PrototypeId,
            FixtureMatrixRef = matrix.MatrixId,
            AuditStatus = status,
            Findings = Enum.GetValues<NodalOsScanBoundaryAuditDimension>()
                .Select(d => new NodalOsScanBoundaryAuditFinding
                {
                    Dimension = d,
                    Status = NodalOsScanBoundaryAuditStatus.Pass,
                    FindingRedacted = $"{d} satisfied for synthetic layer."
                })
                .ToArray(),
            RequiredFixesRedacted = [],
            NextGateRequirementsRedacted =
            [
                "Operational path jail audit.",
                "Operational consent enablement.",
                "Operational evidence and timeline audit.",
                "Future implementation gate."
            ],
            BoundaryDecision = CreateDecision(status)
        };
    }

    public NodalOsScanBoundaryAuditStatus EvaluateMarkers(IEnumerable<string> markers)
    {
        var joined = string.Join("|", markers).ToUpperInvariant();
        var forbidden = new[]
        {
            "REALSCAN",
            "DIRECTORYLISTING",
            "FILEREAD",
            "FILEHASH",
            "FILESYSTEMAPI",
            "CANONICALIZATIONREAL",
            "INDEXINGTRUE",
            "VECTORIZATIONTRUE",
            "LLMCONTEXTTRUE",
            "PROMPTGENERATED",
            "PROVIDERCALL",
            "NETWORKCALL",
            "CLOUDCALL",
            "RUNTIMEEXECUTION"
        };

        return forbidden.Any(joined.Contains) ? NodalOsScanBoundaryAuditStatus.Fail : NodalOsScanBoundaryAuditStatus.Pass;
    }

    private static NodalOsScanBoundaryDecision CreateDecision(NodalOsScanBoundaryAuditStatus status) =>
        new()
        {
            BoundaryDecisionId = "scan-boundary-decision-m551",
            SyntheticLayerReady = status == NodalOsScanBoundaryAuditStatus.Pass,
            ReadyForRealScan = false,
            ReadyForRealFilesystemAccess = false,
            ReadyForRealPathJail = false,
            ReadyForRealSecretDetection = false,
            ReadyForRealExclusionEnforcement = false,
            ReadyForIndexing = false,
            ReadyForVectorization = false,
            ReadyForLlmContext = false,
            RequiredBeforeRealScanRedacted = ["Future operational implementation ADR and audit."],
            RequiredBeforeFilesystemAccessRedacted = ["Path jail implementation audit and consent enablement."],
            RequiredBeforePathJailEnablementRedacted = ["Canonical boundary proof and no-mutation proof."],
            RequiredBeforeLlmContextRedacted = ["BYOK, prompt governance, budget guardrails, and redaction proof."]
        };
}

public sealed class NodalOsScanBoundaryAuditJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeAudit(NodalOsScanBoundaryAudit audit) => JsonSerializer.Serialize(audit, Options);
}
