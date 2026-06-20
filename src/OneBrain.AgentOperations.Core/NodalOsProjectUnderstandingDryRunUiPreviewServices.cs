using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsProjectUnderstandingDryRunUiPreviewService
{
    public NodalOsProjectUnderstandingDryRunUiPreview CreatePreview(
        NodalOsScanDryRunRequest request,
        NodalOsScanDryRunResult result) =>
        new()
        {
            PreviewId = "project-understanding-dry-run-ui-preview-m543",
            WorkspaceRef = request.WorkspaceRef,
            MissionRef = request.MissionRef,
            DryRunContractRef = request.DryRunId,
            PathJailPreconditionsRef = request.PathJailPreconditionsRef,
            ConsentScopePreviewRef = request.ConsentScopePreviewRef,
            SecretDetectionPolicyPreviewRef = request.SecretDetectionPolicyPreviewRef,
            ExclusionPolicyPackRef = request.ExclusionPolicyPackRef,
            RealScanAuditGateRef = result.RealScanAuditGateRef,
            IsStaticPreview = true,
            IsReadOnly = true,
            IsNoOp = true,
            UsesRealFilesystem = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            PerformsIndexing = false,
            PerformsVectorization = false,
            BuildsLlmContext = false,
            CallsProvider = false,
            UsesCloud = false,
            Sections =
            [
                Section("dry-run-summary", "Dry-run summary", result.UserFacingExplanationRedacted, request.DryRunId),
                Section("scope-preview-summary", "Scope preview summary", result.EstimatedOnlyScopeSummaryRedacted, request.ConsentScopePreviewRef),
                Section("consent-status", "Consent status", "Consent remains draft-only and non-authorizing.", request.ConsentScopePreviewRef),
                Section("secret-policy-summary", "Secret policy summary", "Sensitive-data policy remains preview-only.", request.SecretDetectionPolicyPreviewRef),
                Section("exclusion-policy-summary", "Exclusion policy summary", "Exclusion policy remains preview-only.", request.ExclusionPolicyPackRef),
                Section("blocked-capabilities", "Blocked capabilities", string.Join(" ", result.BlockedCapabilitiesRedacted), result.RealScanAuditGateRef),
                Section("missing-requirements", "Missing requirements", string.Join(" ", result.RequiredNextGatesRedacted), result.RealScanAuditGateRef),
                Section("evidence-plan-preview", "Evidence plan preview", "Evidence remains planned refs only.", "dry-run-evidence-plan-m545"),
                Section("timeline-events-preview", "Timeline events preview", "Timeline events are preview-only and not emitted.", "timeline-preview-m545"),
                Section("guardrails", "Guardrails", "Review cannot authorize operational scan behavior.", result.RealScanAuditGateRef)
            ],
            RequiredDisclosuresRedacted =
            [
                "No scan is performed.",
                "No content is accessed.",
                "No folder enumeration is performed.",
                "No content fingerprints are computed.",
                "No LLM context is built.",
                "No cloud is used.",
                "Approval or review does not authorize real scan."
            ],
            GuardrailRefs =
            [
                "guardrail-ui-preview-only",
                "guardrail-read-only",
                "guardrail-review-non-authorizing",
                "guardrail-real-scan-gate-blocked"
            ]
        };

    public string RenderHtml(NodalOsProjectUnderstandingDryRunUiPreview preview) =>
        $"""
        <section class="panel hero">
          <p class="eyebrow">NODAL OS / M543</p>
          <h1>Project Understanding Dry Run UI Preview</h1>
          <p>{preview.PreviewId}</p>
          <p>Static={preview.IsStaticPreview}; ReadOnly={preview.IsReadOnly}; NoOp={preview.IsNoOp}</p>
        </section>
        <section class="grid">
          {string.Join(Environment.NewLine, preview.Sections.Select(RenderSection))}
        </section>
        <section class="panel">
          <h2>Disclosures</h2>
          <ul>{string.Join(Environment.NewLine, preview.RequiredDisclosuresRedacted.Select(d => $"<li>{d}</li>"))}</ul>
        </section>
        """;

    private static NodalOsDryRunUiPreviewSection Section(string id, string title, string summary, string refId) =>
        new()
        {
            SectionId = id,
            TitleRedacted = title,
            SummaryRedacted = summary,
            RefIds = [refId]
        };

    private static string RenderSection(NodalOsDryRunUiPreviewSection section) =>
        $"""
        <article class="panel">
          <h2>{section.TitleRedacted}</h2>
          <p>{section.SummaryRedacted}</p>
          <small>{string.Join(", ", section.RefIds)}</small>
        </article>
        """;
}

public sealed class NodalOsProjectUnderstandingDryRunUiPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsProjectUnderstandingDryRunUiPreview preview) =>
        JsonSerializer.Serialize(preview, Options);
}

public static class NodalOsProjectUnderstandingDryRunUiPreviewFixtures
{
    public static NodalOsProjectUnderstandingDryRunUiPreview Preview() =>
        new NodalOsProjectUnderstandingDryRunUiPreviewService().CreatePreview(
            NodalOsScanDryRunFixtures.Request(),
            NodalOsScanDryRunFixtures.Result());
}
