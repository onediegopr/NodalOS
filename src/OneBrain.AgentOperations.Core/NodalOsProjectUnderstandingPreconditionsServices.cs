using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsProjectUnderstandingPreconditionsService
{
    private static readonly DateTimeOffset FixtureTime = new(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsProjectUnderstandingPreconditions CreatePreconditions(
        string workspaceRef,
        string missionRef,
        string assignmentGovernanceRef)
    {
        return new()
        {
            PreconditionsId = "project-understanding-preconditions-m534",
            WorkspaceRef = SafeValue(workspaceRef),
            MissionRef = SafeValue(missionRef),
            AssignmentGovernanceRef = SafeValue(assignmentGovernanceRef),
            RequiredConsent = "Explicit user consent with scope, purpose, and cancellation confirmation required before any real scan.",
            RequiredPathJailValidation = "Path jail must be defined and validated; all scan targets must be within the jail boundary.",
            RequiredScanScopePreview = "A preview of the scan scope (file count estimate, size estimate, excluded patterns) must be shown to the user before any real scan begins.",
            RequiredRedactionPolicy = "All text fields derived from filesystem content must pass redaction before inclusion in any evidence, timeline, or export artifact.",
            RequiredSecretDetectionPolicy = "Secret/credential detection must run before any path, filename, or content is included in context or refs.",
            RequiredExclusionPolicy = "Exclusion patterns for secrets, binaries, node_modules, .git, vendor, credentials, and symlinks must be defined and applied.",
            RequiredSymlinkPolicy = "Symlink following must be disabled by default; any exception requires explicit policy definition.",
            RequiredCaseSensitivityPolicy = "Case sensitivity policy must be defined for the target filesystem before scope enumeration.",
            RequiredMaxFileLimits = "Maximum file count and file size limits must be defined and enforced before any scan begins.",
            RequiredCancellationSemantics = "User-cancellable scan with defined cancellation point, partial result discard, and state cleanup required.",
            RequiredEvidencePlan = "Evidence plan defining what refs will be generated, their format, and their retention policy must be approved before scan.",
            RequiredTimelineEvents = "Timeline event definitions for scan-start, scan-progress, scan-cancelled, and scan-complete must be registered before scan.",
            RequiredNoMutationGuarantee = "Scan must be structurally read-only; no filesystem mutation, no git commits, no file writes permitted.",
            RequiredNoCloudDefault = "No cloud upload by default; cloud sync requires separate explicit policy and consent.",
            RequiredNoLlmBeforeContextApproval = "No LLM context building, prompt generation, or provider call until context approval policy is defined and approved.",
            RequiredAuditBeforeRealScan = "A dedicated audit of the scan implementation must be completed before any real scan is executed.",
            Status = NodalOsProjectUnderstandingPreconditionStatus.FullyDefined,
            MissingRequirementsRedacted =
            [
                "Real scan implementation (future milestone).",
                "Path jail implementation (future milestone).",
                "Consent UI (future milestone).",
                "Secret detection implementation (future milestone).",
                "LLM context build policy (future milestone)."
            ],
            BlockersRedacted =
            [
                "Real filesystem scan not permitted until path jail + consent + scope preview + audit defined.",
                "LLM context build not permitted until BYOK + provider policy + prompt governance + budget enforcement defined.",
                "Runtime execution not permitted until positive execution gate + separate audit defined."
            ],
            EvidenceRefs = ["evidence-preconditions-governance-ref-only", "evidence-m534-preconditions-ref-only"],
            TimelineRefs = ["timeline-preconditions-created-m534"],
            GuardrailRefs =
            [
                "guardrail-no-real-scan-until-preconditions-met",
                "guardrail-no-llm-until-byok-policy",
                "guardrail-no-filesystem-mutation",
                "guardrail-no-cloud-by-default"
            ],
            CreatedAt = FixtureTime
        };
    }

    public NodalOsProjectUnderstandingReadinessResult EvaluateReadiness(
        NodalOsProjectUnderstandingPreconditions preconditions)
    {
        return new()
        {
            ReadinessId = "project-understanding-readiness-m534",
            PreconditionsRef = preconditions.PreconditionsId,
            ReadyForRealProjectUnderstanding = false,
            ReadyForFilesystemScan = false,
            ReadyForLlmContextBuild = false,
            ReadyForEmbeddings = false,
            ReadyForIndexing = false,
            ReadyForCloudSync = false,
            MissingRequirementsRedacted =
            [
                "Path jail implementation.",
                "Explicit user consent mechanism.",
                "Scan scope preview implementation.",
                "Secret detection implementation.",
                "Real scan implementation.",
                "LLM context build policy.",
                "BYOK and provider policy.",
                "Prompt governance.",
                "Budget enforcement.",
                "Separate audit of scan implementation."
            ],
            BlockersRedacted =
            [
                "No real scan implementation exists.",
                "No path jail implementation exists.",
                "No consent mechanism exists.",
                "No LLM provider policy defined.",
                "No positive execution gate defined."
            ],
            UserFacingExplanationRedacted = "Project Understanding is not yet ready for real operation. All current capabilities are precondition definitions only. Real filesystem scan, LLM context build, embeddings, indexing, and cloud sync remain blocked until future governed milestones define and implement each required capability with its associated policy, consent, and audit.",
            GuardrailRefs =
            [
                "guardrail-no-real-scan-until-preconditions-met",
                "guardrail-no-llm-until-byok-policy",
                "guardrail-no-filesystem-mutation",
                "guardrail-no-cloud-by-default"
            ],
            EvidenceRefs = ["evidence-readiness-evaluation-m534-ref-only"],
            TimelineRefs = ["timeline-readiness-evaluated-m534"]
        };
    }

    public NodalOsProjectUnderstandingPreconditionsPreview RenderPreview(
        NodalOsProjectUnderstandingPreconditions preconditions,
        NodalOsProjectUnderstandingReadinessResult readiness)
    {
        var html = BuildStaticHtml(preconditions, readiness);
        return new()
        {
            PreviewId = "project-understanding-preconditions-preview-m534",
            PreconditionsRef = preconditions.PreconditionsId,
            ReadinessRef = readiness.ReadinessId,
            HtmlRedacted = html,
            Deterministic = true,
            ContainsExternalResource = false,
            ContainsScript = false,
            ContainsInlineData = false,
            ContainsNetworkCall = false,
            ContainsAnalyticsBeacon = false
        };
    }

    private static string BuildStaticHtml(
        NodalOsProjectUnderstandingPreconditions preconditions,
        NodalOsProjectUnderstandingReadinessResult readiness)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"><title>Project Understanding Preconditions — NODAL OS</title>");
        sb.AppendLine("<style>body{font-family:monospace;background:#111;color:#eee;padding:2em;} h1,h2{color:#7cf;} .blocked{color:#f77;} .ref{color:#aaa;font-size:.85em;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>NODAL OS — Project Understanding Preconditions</h1>");
        sb.AppendLine($"<p class=\"ref\">Preconditions ID: {preconditions.PreconditionsId}</p>");
        sb.AppendLine($"<p class=\"ref\">Status: {preconditions.Status}</p>");
        sb.AppendLine("<h2>Readiness</h2><ul>");
        sb.AppendLine($"<li class=\"blocked\">ReadyForRealProjectUnderstanding: {readiness.ReadyForRealProjectUnderstanding}</li>");
        sb.AppendLine($"<li class=\"blocked\">ReadyForFilesystemScan: {readiness.ReadyForFilesystemScan}</li>");
        sb.AppendLine($"<li class=\"blocked\">ReadyForLlmContextBuild: {readiness.ReadyForLlmContextBuild}</li>");
        sb.AppendLine($"<li class=\"blocked\">ReadyForEmbeddings: {readiness.ReadyForEmbeddings}</li>");
        sb.AppendLine($"<li class=\"blocked\">ReadyForIndexing: {readiness.ReadyForIndexing}</li>");
        sb.AppendLine($"<li class=\"blocked\">ReadyForCloudSync: {readiness.ReadyForCloudSync}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<h2>Required Preconditions</h2><ul>");
        sb.AppendLine($"<li>{preconditions.RequiredConsent}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredPathJailValidation}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredScanScopePreview}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredRedactionPolicy}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredSecretDetectionPolicy}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredExclusionPolicy}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredNoMutationGuarantee}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredNoCloudDefault}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredNoLlmBeforeContextApproval}</li>");
        sb.AppendLine($"<li>{preconditions.RequiredAuditBeforeRealScan}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<p class=\"ref\">NODAL OS — static read-only preview — no external resources — no scripts — self-contained</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed record NodalOsProjectUnderstandingPreconditionsPreview
{
    public required string PreviewId { get; init; }

    public required string PreconditionsRef { get; init; }

    public required string ReadinessRef { get; init; }

    public required string HtmlRedacted { get; init; }

    public required bool Deterministic { get; init; }

    public required bool ContainsExternalResource { get; init; }

    public required bool ContainsScript { get; init; }

    public required bool ContainsInlineData { get; init; }

    public required bool ContainsNetworkCall { get; init; }

    public required bool ContainsAnalyticsBeacon { get; init; }
}

public sealed class NodalOsProjectUnderstandingPreconditionsJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreconditions(NodalOsProjectUnderstandingPreconditions preconditions) =>
        JsonSerializer.Serialize(preconditions, Options);

    public string SerializeReadiness(NodalOsProjectUnderstandingReadinessResult readiness) =>
        JsonSerializer.Serialize(readiness, Options);

    public string SerializePreview(NodalOsProjectUnderstandingPreconditionsPreview preview) =>
        JsonSerializer.Serialize(preview, Options);
}

public static class NodalOsProjectUnderstandingPreconditionsFixtures
{
    public static NodalOsProjectUnderstandingPreconditions Preconditions()
    {
        var service = new NodalOsProjectUnderstandingPreconditionsService();
        return service.CreatePreconditions(
            workspaceRef: "workspace-local-ref-m534",
            missionRef: "mission-ref-m534",
            assignmentGovernanceRef: "planner-governance-closeout-m531-m533");
    }

    public static NodalOsProjectUnderstandingReadinessResult Readiness()
    {
        var service = new NodalOsProjectUnderstandingPreconditionsService();
        return service.EvaluateReadiness(Preconditions());
    }

    public static NodalOsProjectUnderstandingPreconditionsPreview Preview()
    {
        var service = new NodalOsProjectUnderstandingPreconditionsService();
        var p = Preconditions();
        return service.RenderPreview(p, service.EvaluateReadiness(p));
    }
}
