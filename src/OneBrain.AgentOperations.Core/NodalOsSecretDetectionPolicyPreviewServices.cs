using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsSecretDetectionPolicyPreviewService
{
    public NodalOsSecretDetectionPolicyPreview CreatePolicyPreview(
        string workspaceRef = "workspace-ref-m540",
        string missionRef = "mission-ref-m540",
        string scopePreviewRef = "scope-preview-contract-m538") =>
        CreatePreview(workspaceRef, missionRef, scopePreviewRef);

    public NodalOsSecretDetectionPolicyPreview CreatePreview(
        string workspaceRef = "workspace-ref-m540",
        string missionRef = "mission-ref-m540",
        string scopePreviewRef = "scope-preview-contract-m538") =>
        new()
        {
            PolicyPreviewId = "secret-detection-policy-preview-m540",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            ScopePreviewRef = scopePreviewRef,
            IsPreviewOnly = true,
            UsesRealContent = false,
            ReadsFiles = false,
            PerformsSecretDetectionOnRealData = false,
            CanBlockScan = true,
            CanRedactFindings = true,
            RequiresUserReview = true,
            RequiresAuditBeforeEnablement = true,
            Categories = Enum.GetValues<NodalOsSensitivePolicyCategory>(),
            PatternPolicyRefsRedacted = "pattern-policy-ref-preview-only",
            EntropyPolicyRefsRedacted = "entropy-policy-ref-preview-only",
            FilenameHintRefsRedacted = "filename-hint-ref-preview-only",
            ExtensionHintRefsRedacted = "extension-hint-ref-preview-only",
            AllowlistRefsRedacted = "allowlist-ref-preview-only",
            RequiresFalsePositiveReview = true,
            RequiresRedaction = true,
            EvidenceRefs = ["evidence-secret-policy-preview-ref-only"],
            TimelineRefs = ["timeline-secret-policy-preview-m540"]
        };

    public NodalOsSecretDetectionReadinessResult Evaluate(NodalOsSecretDetectionPolicyPreview preview) =>
        new()
        {
            ReadinessId = "secret-detection-readiness-m540",
            PolicyPreviewRef = preview.PolicyPreviewId,
            ReadyForRealSecretDetection = false,
            ReadyForRealScan = false,
            ReadyForLlmContextBuild = false,
            CanReadFile = false,
            CanInspectRealContent = false,
            CanEmitRawSecret = false,
            CanPersistRawSecret = false,
            CanSendSecretToLlm = false,
            CanSendSecretToCloud = false,
            MissingRequirementsRedacted =
            [
                "Future implementation audit.",
                "False-positive review workflow.",
                "Redaction proof.",
                "Consent and scope enforcement."
            ],
            BlockersRedacted =
            [
                "Policy is preview-only.",
                "No real content is inspected.",
                "No secret finding can leave redacted form."
            ],
            UserFacingExplanationRedacted = "Secret detection policy is preview-only. It cannot inspect real content, emit raw findings, persist sensitive values, or send anything to model or cloud surfaces.",
            GuardrailRefs =
            [
                "guardrail-secret-policy-preview-only",
                "guardrail-no-real-content-inspection",
                "guardrail-redaction-required"
            ]
        };

    public string RenderHtml(
        NodalOsSecretDetectionPolicyPreview preview,
        NodalOsSecretDetectionReadinessResult readiness) =>
        $"""
        <section class="panel">
          <h2>M540 Secret Detection Policy Preview</h2>
          <p>Policy: {preview.PolicyPreviewId}; preview only: {preview.IsPreviewOnly}</p>
          <p>UsesRealContent={preview.UsesRealContent}; ReadsFiles={preview.ReadsFiles}</p>
          <p>ReadyForRealSecretDetection={readiness.ReadyForRealSecretDetection}; ReadyForRealScan={readiness.ReadyForRealScan}</p>
        </section>
        """;
}

public sealed class NodalOsSecretDetectionPolicyPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreview(NodalOsSecretDetectionPolicyPreview preview) =>
        JsonSerializer.Serialize(preview, Options);

    public string SerializePolicy(NodalOsSecretDetectionPolicyPreview preview) =>
        SerializePreview(preview);

    public string SerializeReadiness(NodalOsSecretDetectionReadinessResult readiness) =>
        JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsSecretDetectionPolicyPreviewFixtures
{
    public static NodalOsSecretDetectionPolicyPreview Preview() =>
        new NodalOsSecretDetectionPolicyPreviewService().CreatePreview();

    public static NodalOsSecretDetectionReadinessResult Readiness()
    {
        var service = new NodalOsSecretDetectionPolicyPreviewService();
        return service.Evaluate(service.CreatePreview());
    }
}
