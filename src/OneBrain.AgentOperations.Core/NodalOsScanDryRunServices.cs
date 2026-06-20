using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsScanDryRunService
{
    public NodalOsScanDryRunRequest CreateRequest(
        NodalOsPathJailPreconditions path,
        NodalOsConsentRequestDraft consent,
        NodalOsSecretDetectionPolicyPreview secretPolicy,
        NodalOsExclusionPolicyPack exclusionPolicy) =>
        new()
        {
            DryRunId = "scan-dry-run-contract-m542",
            WorkspaceRef = path.WorkspaceRef,
            MissionRef = path.MissionRef,
            PathJailPreconditionsRef = path.JailPreconditionsId,
            ConsentScopePreviewRef = consent.ConsentRequestId,
            SecretDetectionPolicyPreviewRef = secretPolicy.PolicyPreviewId,
            ExclusionPolicyPackRef = exclusionPolicy.ExclusionPolicyId,
            IsDryRunOnly = true,
            UsesRealFilesystem = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            PerformsIndexing = false,
            PerformsVectorization = false,
            BuildsLlmContext = false
        };

    public NodalOsScanDryRunResult Evaluate(NodalOsScanDryRunRequest request, NodalOsRealScanAuditGate gate) =>
        new()
        {
            DryRunResultId = "scan-dry-run-result-m542",
            DryRunRef = request.DryRunId,
            RealScanAuditGateRef = gate.GateId,
            GateStillBlocked = true,
            ReadyForRealDryRun = false,
            ReadyForRealScan = false,
            ReadyForFileRead = false,
            ReadyForIndexing = false,
            ReadyForVectorization = false,
            ReadyForLlmContext = false,
            EstimatedOnlyScopeSummaryRedacted = "Estimated policy preview only; no real filesystem access or content inspection occurs.",
            BlockedCapabilitiesRedacted =
            [
                "Real scan.",
                "Folder enumeration.",
                "Content handling.",
                "Content fingerprinting.",
                "Indexing.",
                "Vectorization.",
                "LLM context.",
                "Cloud sync.",
                "Runtime."
            ],
            RequiredNextGatesRedacted =
            [
                "Path jail implementation audit.",
                "Consent UI activation.",
                "Secret policy implementation audit.",
                "Exclusion enforcement audit.",
                "Dry-run implementation audit."
            ],
            UserFacingExplanationRedacted = "Scan dry run is contract-only. It references policy previews and the scan audit gate, but it does not satisfy readiness for future real scan.",
            EvidenceRefs = ["evidence-scan-dry-run-contract-ref-only"],
            TimelineRefs = ["timeline-scan-dry-run-contract-m542"],
            GuardrailRefs =
            [
                "guardrail-dry-run-contract-only",
                "guardrail-audit-gate-still-blocked",
                "guardrail-no-filesystem-access",
                "guardrail-no-llm-context"
            ],
            EventsPreview = Enum.GetValues<NodalOsScanDryRunEventPreviewKind>()
                .Select((kind, index) => new NodalOsScanDryRunEventPreview
                {
                    EventPreviewId = $"dry-run-event-preview-{index + 1:000}",
                    Kind = kind,
                    SummaryRedacted = $"{kind} would be emitted by a future implementation.",
                    EmitsToRealEventBus = false,
                    ProductivePersistenceUsed = false
                })
                .ToArray()
        };

    public string RenderHtml(
        NodalOsSecretDetectionPolicyPreview secretPolicy,
        NodalOsExclusionPolicyPack exclusionPolicy,
        NodalOsScanDryRunResult result) =>
        $"""
        <section class="panel">
          <h2>M540 Secret Detection Policy Preview</h2>
          <p>Policy: {secretPolicy.PolicyPreviewId}; preview only: {secretPolicy.IsPreviewOnly}</p>
        </section>
        <section class="panel">
          <h2>M541 Exclusion Policy Pack</h2>
          <p>Policy: {exclusionPolicy.ExclusionPolicyId}; rules: {exclusionPolicy.Rules.Count}</p>
        </section>
        <section class="panel">
          <h2>M542 Scan Dry Run Contract</h2>
          <p>Result: {result.DryRunResultId}; gate blocked: {result.GateStillBlocked}</p>
          <p>ReadyForRealScan={result.ReadyForRealScan}; ReadyForLlmContext={result.ReadyForLlmContext}</p>
        </section>
        """;
}

public sealed class NodalOsScanDryRunJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeRequest(NodalOsScanDryRunRequest request) =>
        JsonSerializer.Serialize(request, Options);

    public string SerializeResult(NodalOsScanDryRunResult result) =>
        JsonSerializer.Serialize(result, Options);
}

public static class NodalOsScanDryRunFixtures
{
    public static NodalOsScanDryRunRequest Request()
    {
        var service = new NodalOsScanDryRunService();
        return service.CreateRequest(
            NodalOsPathJailPreconditionsFixtures.Preconditions(),
            NodalOsConsentScopePreviewFixtures.Consent(),
            NodalOsSecretDetectionPolicyPreviewFixtures.Preview(),
            NodalOsExclusionPolicyPackFixtures.Pack());
    }

    public static NodalOsScanDryRunResult Result()
    {
        var service = new NodalOsScanDryRunService();
        return service.Evaluate(Request(), NodalOsRealScanAuditGateFixtures.Gate());
    }
}
