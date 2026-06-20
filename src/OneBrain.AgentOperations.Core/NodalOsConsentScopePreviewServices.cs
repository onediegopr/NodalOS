using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentScopePreviewService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsConsentRequestDraft CreateConsentDraft(
        string workspaceRef = "workspace-ref-m538",
        string missionRef = "mission-ref-m538")
    {
        return new()
        {
            ConsentRequestId = "consent-request-draft-m538",
            WorkspaceRef = SafeValue(workspaceRef),
            MissionRef = SafeValue(missionRef),
            RequestedCapability = NodalOsConsentScopeCapability.FutureProjectScan,
            RequestedScopeRedacted = "symbolic workspace scope only",
            UserFacingPurposeRedacted = "Explain what a future safe project scan would request before any implementation is enabled.",
            RisksRedacted =
            [
                "Future scope could expose filenames or project structure if approved by a later gate.",
                "Future content handling requires redaction and secret policy first."
            ],
            DataExposureExplanationRedacted = "This draft exposes no real file content and performs no filesystem operation.",
            NoMutationGuarantee = true,
            CloudDefault = false,
            LlmDefault = false,
            CanApproveRealScan = false,
            IsDraftOnly = true,
            IsNoOp = true
        };
    }

    public NodalOsScopePreviewContract CreateScopePreview(string workspaceRef = "workspace-ref-m538") =>
        new()
        {
            ScopePreviewId = "scope-preview-contract-m538",
            WorkspaceRef = SafeValue(workspaceRef),
            IncludePatternsRedacted = ["src/**", "docs/**", "tests/**"],
            ExcludePatternsRedacted = ["dependency folders", "build output folders", "credential patterns", "vendor folders"],
            MaxDepth = 12,
            MaxFiles = 5000,
            MaxBytes = 100_000_000,
            EstimatedOnly = true,
            UsesRealFilesystem = false,
            DirectoryListingPerformed = false,
            FileReadPerformed = false,
            FileHashPerformed = false,
            UserFacingExplanationRedacted = "Scope preview is estimated and contract-only; it does not inspect real folders or files."
        };

    public NodalOsConsentScopeResult ApplyOption(
        NodalOsConsentRequestDraft request,
        NodalOsConsentScopeOption option)
    {
        return new()
        {
            ConsentResultId = $"consent-result-{option}",
            ConsentRequestRef = request.ConsentRequestId,
            SelectedOption = option,
            IsNoOp = true,
            MutatesState = false,
            AuthorizesRealScan = false,
            AuthorizesFileRead = false,
            AuthorizesIndexing = false,
            AuthorizesVectorization = false,
            AuthorizesLlmContext = false,
            RequiresFutureExplicitGate = true,
            UserFacingExplanationRedacted = "This option records a draft intent only. It cannot authorize future scan, content handling, indexing, vector context, or LLM context.",
            GuardrailRefs =
            [
                "guardrail-consent-draft-only",
                "guardrail-scope-preview-noop",
                "guardrail-future-explicit-gate-required"
            ]
        };
    }

    public string RenderHtml(
        NodalOsConsentRequestDraft request,
        NodalOsScopePreviewContract scope,
        NodalOsConsentScopeResult result) =>
        $"""
        <section class="panel">
          <h2>M538 Consent and Scope Preview</h2>
          <p>Consent draft: {request.ConsentRequestId}; option result: {result.SelectedOption}</p>
          <p>EstimatedOnly={scope.EstimatedOnly}; UsesRealFilesystem={scope.UsesRealFilesystem}</p>
          <p>Consent options are no-op and cannot authorize future operational capabilities.</p>
        </section>
        """;

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed class NodalOsConsentScopePreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeConsent(NodalOsConsentRequestDraft request) =>
        JsonSerializer.Serialize(request, Options);

    public string SerializeScope(NodalOsScopePreviewContract scope) =>
        JsonSerializer.Serialize(scope, Options);

    public string SerializeResult(NodalOsConsentScopeResult result) =>
        JsonSerializer.Serialize(result, Options);
}

public static class NodalOsConsentScopePreviewFixtures
{
    public static NodalOsConsentRequestDraft Consent() =>
        new NodalOsConsentScopePreviewService().CreateConsentDraft();

    public static NodalOsScopePreviewContract Scope() =>
        new NodalOsConsentScopePreviewService().CreateScopePreview();

    public static NodalOsConsentScopeResult Result(NodalOsConsentScopeOption option = NodalOsConsentScopeOption.ApproveDraftOnly)
    {
        var service = new NodalOsConsentScopePreviewService();
        return service.ApplyOption(service.CreateConsentDraft(), option);
    }
}
