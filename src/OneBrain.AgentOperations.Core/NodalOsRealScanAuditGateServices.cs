using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsRealScanAuditGateService
{
    public NodalOsRealScanAuditGate CreateGate(
        NodalOsPathJailPreconditions pathJail,
        NodalOsScopePreviewContract scopePreview)
    {
        return new()
        {
            GateId = "real-scan-audit-gate-m539",
            WorkspaceRef = pathJail.WorkspaceRef,
            MissionRef = pathJail.MissionRef,
            PathJailPreconditionsRef = pathJail.JailPreconditionsId,
            ConsentScopePreviewRef = scopePreview.ScopePreviewId,
            RedactionPolicyRef = "redaction-policy-ref-existing",
            SecretDetectionPolicyRef = "secret-detection-policy-required-future",
            EvidencePlanRef = "evidence-plan-ref-only",
            TimelinePlanRef = "timeline-plan-ref-only",
            AuditStatus = NodalOsRealScanAuditStatus.Blocked,
            Findings = Enum.GetValues<NodalOsRealScanAuditDimension>()
                .Select(dimension => new NodalOsRealScanAuditFinding
                {
                    Dimension = dimension,
                    Status = NodalOsRealScanAuditStatus.Blocked,
                    FindingRedacted = $"{dimension} is required before future activation."
                })
                .ToArray()
        };
    }

    public NodalOsRealScanGateDecision Decide(NodalOsRealScanAuditGate gate) =>
        new()
        {
            DecisionId = "real-scan-gate-decision-m539",
            GateRef = gate.GateId,
            ReadyForRealScan = false,
            ReadyForDirectoryListing = false,
            ReadyForFileRead = false,
            ReadyForFileHashing = false,
            ReadyForIndexing = false,
            ReadyForVectorization = false,
            ReadyForLlmContextBuild = false,
            ReadyForCloudSync = false,
            RequiredBeforeRealScanRedacted =
            [
                "Path jail implementation audit.",
                "Consent and scope preview activation.",
                "Secret detection implementation.",
                "Cancellation and no-mutation verification."
            ],
            RequiredBeforeFileReadRedacted =
            [
                "Separate content handling policy.",
                "Secret detection and redaction proof.",
                "Human consent and audit gate."
            ],
            RequiredBeforeIndexingRedacted =
            [
                "Indexing policy.",
                "Retention and deletion policy.",
                "Redaction proof."
            ],
            RequiredBeforeLlmContextRedacted =
            [
                "BYOK/provider policy.",
                "Prompt governance.",
                "Budget guardrails.",
                "Human review."
            ],
            RequiredBeforeCloudSyncRedacted =
            [
                "Cloud policy.",
                "Explicit consent.",
                "Separate risk review."
            ],
            UserFacingExplanationRedacted = "Future project scan remains blocked. The current gate is an audit contract only and cannot activate filesystem, indexing, LLM context, cloud, or runtime capabilities."
        };

    public NodalOsRealScanAuditStatus EvaluateImplementationMarkers(IEnumerable<string> markers)
    {
        var blocked = new[]
        {
            "DirectoryListingPerformed=true",
            "FileReadPerformed=true",
            "FileHashPerformed=true",
            "GitCommand=true",
            "VectorContextCreated=true",
            "IndexCreated=true",
            "LlmContextBuilt=true",
            "PromptGenerated=true",
            "ProviderCall=true",
            "NetworkCall=true",
            "CloudSync=true",
            "RuntimeExecution=true"
        };

        return markers.Any(marker => blocked.Contains(marker, StringComparer.Ordinal))
            ? NodalOsRealScanAuditStatus.Failed
            : NodalOsRealScanAuditStatus.Blocked;
    }

    public string RenderHtml(
        NodalOsRealScanAuditGate gate,
        NodalOsRealScanGateDecision decision) =>
        $"""
        <section class="panel">
          <h2>M539 Real Scan Audit Gate</h2>
          <p>Gate: {gate.GateId}; status: {gate.AuditStatus}</p>
          <p>ReadyForRealScan={decision.ReadyForRealScan}; ReadyForDirectoryListing={decision.ReadyForDirectoryListing}</p>
          <p>Future scan remains blocked until a separate implementation milestone and audit.</p>
        </section>
        """;
}

public sealed class NodalOsRealScanAuditGateJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeGate(NodalOsRealScanAuditGate gate) =>
        JsonSerializer.Serialize(gate, Options);

    public string SerializeDecision(NodalOsRealScanGateDecision decision) =>
        JsonSerializer.Serialize(decision, Options);
}

public static class NodalOsRealScanAuditGateFixtures
{
    public static NodalOsRealScanAuditGate Gate()
    {
        var path = NodalOsPathJailPreconditionsFixtures.Preconditions();
        var scope = NodalOsConsentScopePreviewFixtures.Scope();
        return new NodalOsRealScanAuditGateService().CreateGate(path, scope);
    }

    public static NodalOsRealScanGateDecision Decision() =>
        new NodalOsRealScanAuditGateService().Decide(Gate());
}
