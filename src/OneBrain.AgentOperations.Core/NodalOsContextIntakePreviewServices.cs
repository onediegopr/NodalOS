using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsContextIntakePreviewService
{
    public NodalOsContextIntakeUiPreview CreatePreview(
        NodalOsWorkspaceLocalModel workspace,
        IReadOnlyList<NodalOsUserProvidedContextCapture> captures,
        IReadOnlyList<NodalOsContextReviewCard> reviewCards,
        IReadOnlyList<NodalOsContextEvidenceLink> evidenceLinks)
    {
        return new()
        {
            PreviewId = $"context-intake-preview-{workspace.WorkspaceId}",
            WorkspaceId = workspace.WorkspaceId,
            MissionId = workspace.ActiveMissionRefs.FirstOrDefault(),
            ContextCaptures = captures,
            ReviewCards = reviewCards,
            EvidenceLinks = evidenceLinks,
            SafeCount = reviewCards.Count(IsSafe),
            BlockedCount = reviewCards.Count(IsBlocked),
            RequiresReviewCount = reviewCards.Count(card => card.Status == NodalOsContextReviewCardStatus.RequiresReview),
            MissingInformationRedacted = reviewCards.SelectMany(card => card.MissingInformationRedacted).Distinct().ToArray(),
            QuestionsForUserRedacted = reviewCards.SelectMany(card => card.QuestionsForUserRedacted).Distinct().ToArray(),
            ProvenanceLabelsRedacted = reviewCards.Select(card => card.ProvenanceLabelRedacted).Distinct().ToArray(),
            ConfidenceLabelsRedacted = reviewCards.Select(card => card.ConfidenceLabelRedacted).Distinct().ToArray(),
            FreshnessLabelsRedacted = reviewCards.Select(card => card.FreshnessLabelRedacted).Distinct().ToArray(),
            SensitivityLabelsRedacted = reviewCards.Select(card => card.SensitivityLabelRedacted).Distinct().ToArray(),
            AllowedUsageChipsRedacted = reviewCards.SelectMany(card => card.AllowedUsageChipsRedacted).Distinct().ToArray(),
            DisallowedUsageChipsRedacted = reviewCards.SelectMany(card => card.DisallowedUsageChipsRedacted).Distinct().ToArray(),
            DisabledFutureCapabilitiesRedacted =
            [
                "real project understanding",
                "filesystem scan",
                "provider prompt",
                "runtime execution",
                "cloud sync"
            ],
            NoFilesReadDisclosureRedacted = "No files were read. Context is user-provided or mock-safe.",
            NoLlmDisclosureRedacted = "No LLM provider was called.",
            NoPromptCreationDisclosureRedacted = "No provider prompt was created.",
            NoRealProjectUnderstandingDisclosureRedacted = "Real project understanding has not started.",
            NextSafeStepsRedacted =
            [
                "Review safe context cards.",
                "Resolve blocked or requires-review context.",
                "Keep evidence links ref-only."
            ],
            GuardrailExplainersRedacted =
            [
                "Context is user-provided and unverified.",
                "Safe Context Boundary blocks sensitive, credential, raw and unknown context.",
                "This preview cannot authorize execution."
            ],
            StaticPreviewOnly = true,
            ReadOnlyPreview = true,
            UserProvidedAndUnverified = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            ReadsFiles = false,
            VerifiesPaths = false,
            MutatesProductiveState = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsContextValidationSummary CreateValidationSummary(
        NodalOsWorkspaceLocalModel workspace,
        IReadOnlyList<NodalOsUserProvidedContextCapture> captures,
        IReadOnlyList<NodalOsContextReviewCard> reviewCards,
        IReadOnlyList<NodalOsContextEvidenceLink> evidenceLinks)
    {
        var blocked = reviewCards.Where(IsBlocked).ToArray();
        var requiresReview = reviewCards.Where(card => card.Status == NodalOsContextReviewCardStatus.RequiresReview).ToArray();
        return new()
        {
            ValidationSummaryId = $"context-validation-summary-{workspace.WorkspaceId}",
            WorkspaceId = workspace.WorkspaceId,
            MissionId = workspace.ActiveMissionRefs.FirstOrDefault(),
            TotalCaptures = captures.Count,
            SafeCaptures = reviewCards.Count(IsSafe),
            BlockedCaptures = blocked.Length,
            RequiresReviewCaptures = requiresReview.Length,
            BlockedByReasonRedacted = blocked.GroupBy(card => card.Status.ToString()).ToDictionary(group => group.Key, group => group.Count()),
            MissingInfoCount = reviewCards.Sum(card => card.MissingInformationRedacted.Count),
            QuestionsCount = reviewCards.Sum(card => card.QuestionsForUserRedacted.Count),
            EvidenceLinkedCount = evidenceLinks.Count(link => link.LinkStatus == NodalOsContextEvidenceLinkStatus.LinkedRefOnly),
            UnverifiedClaimsCount = evidenceLinks.Count(link => !link.ConvertsClaimToAuthoritativeTruth),
            RawPayloadBlockedCount = reviewCards.Count(card => card.Status == NodalOsContextReviewCardStatus.BlockedRawPayload),
            CredentialBlockedCount = reviewCards.Count(card => card.Status == NodalOsContextReviewCardStatus.BlockedSecret),
            SensitivityDistributionRedacted = captures.GroupBy(capture => capture.SensitivityLevel.ToString()).ToDictionary(group => group.Key, group => group.Count()),
            AllowedUsageDistributionRedacted = reviewCards.SelectMany(card => card.AllowedUsageChipsRedacted).GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count()),
            DisallowedUsageDistributionRedacted = reviewCards.SelectMany(card => card.DisallowedUsageChipsRedacted).GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count()),
            ReadinessImpactRedacted = blocked.Length > 0 ? "Blocked context must be removed or reviewed before future understanding readiness." : "Context is usable for safe display/review only.",
            NextSafeStepsRedacted =
            [
                "Keep context review non-authoritative.",
                "Ask user for missing information.",
                "Do not treat linked evidence as proof."
            ],
            HumanReadableSummaryRedacted = $"Mission Control has {captures.Count} user-provided context item(s), {blocked.Length} blocked item(s), and {requiresReview.Length} item(s) needing review.",
            TechnicalSummaryRedacted = "Validation summary aggregates boundary and review-card state only; it does not verify claims, call providers, read files, or authorize execution.",
            WarningsRedacted = requiresReview.Length > 0 ? ["Some context needs human review."] : ["Claims remain unverified."],
            BlockersRedacted = blocked.Select(card => card.Status.ToString()).Distinct().ToArray(),
            RecommendationsRedacted = ["Use safe display/export only.", "Defer provider and filesystem policies to future milestones."],
            ReadinessDeltaRedacted = blocked.Length > 0 ? "Readiness remains blocked by unsafe context." : "Readiness can advance to review-only project understanding preparation.",
            NonAuthoritative = true,
            CanAuthorizeExecution = false,
            ConvertsClaimsToTruth = false,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            ReadsFiles = false,
            ValidationTimestamp = DateTimeOffset.UtcNow
        };
    }

    public NodalOsProjectUnderstandingReadinessReport CreateReadinessReport(
        NodalOsWorkspaceLocalModel? workspace,
        NodalOsWorkspaceReadinessGateResult? workspaceReadiness,
        NodalOsContextValidationSummary? validationSummary,
        IReadOnlyList<NodalOsSafeContextBoundaryDecision> boundaryDecisions,
        NodalOsProjectUnderstandingReadinessState? forcedState = null)
    {
        var state = forcedState ?? InferReadiness(workspace, validationSummary, boundaryDecisions);
        return new()
        {
            ReportId = $"project-understanding-readiness-{workspace?.WorkspaceId ?? "missing"}-{state}",
            WorkspaceId = workspace?.WorkspaceId,
            MissionId = workspace?.ActiveMissionRefs.FirstOrDefault(),
            State = state,
            WorkspaceReadinessGateRef = workspaceReadiness?.GateId,
            ContextValidationSummaryRef = validationSummary?.ValidationSummaryId,
            SafeContextBoundaryRefs = boundaryDecisions.Select(boundary => boundary.BoundaryId).ToArray(),
            EvidenceRefs = workspace?.EvidenceRefs ?? [],
            TimelineRefs = workspace?.TimelineRefs ?? [],
            GuardrailRefs = ["no-real-understanding", "no-filesystem-scan", "no-provider-prompt", "positive-gate-missing"],
            SafeContextAvailableRedacted = validationSummary?.SafeCaptures > 0 ? ["User-provided context safe for display/export review."] : [],
            MissingInformationRedacted = validationSummary is null ? ["Context validation summary missing."] : ["Real source structure not verified.", "Provider policy not configured."],
            DisplayExportOnlyRedacted = ["Safe context may be displayed or exported as redacted review data only."],
            HumanReviewRequiredRedacted = state is NodalOsProjectUnderstandingReadinessState.ReadyForSafeContextBoundaryReview or NodalOsProjectUnderstandingReadinessState.UnknownRequiresReview
                ? ["Safe Context Boundary review is required."]
                : [],
            FutureLlmPolicyRequiredRedacted = ["Future BYOK/LLM policy required before any provider context use."],
            FutureFilesystemPolicyRequiredRedacted = ["Future filesystem policy required before any real scan."],
            ApprovalConsentRequiredRedacted = ["Future user approval and consent required before real understanding."],
            BlockersRedacted = BlockersFor(state),
            WarningsRedacted = ["Report is readiness-only and cannot start project understanding."],
            NextSafeStepsRedacted = NextStepsFor(state),
            UserFacingExplanationRedacted = UserFacingExplanationFor(state),
            TechnicalExplanationRedacted = "Readiness report combines workspace readiness, validation summary, and context boundary decisions without scanning, provider calls, prompt creation, embeddings, or runtime execution.",
            ReadinessOnly = true,
            StartsRealProjectUnderstanding = false,
            ScansFilesystem = false,
            ReadsFiles = false,
            UsesEmbeddings = false,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            CanAuthorizeExecution = false,
            MutatesWorkspaceProductively = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public string RenderStaticHtml(NodalOsContextIntakeUiPreview preview, NodalOsContextValidationSummary summary, NodalOsProjectUnderstandingReadinessReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>NODAL OS Context Intake Preview</title>");
        html.AppendLine("<style>body{margin:0;background:#0D1117;color:#F5F7FA;font-family:Segoe UI,sans-serif}.shell{display:grid;grid-template-columns:260px 1fr 360px;gap:16px;padding:20px}.panel{background:#161B22;border:1px solid #30363D;border-radius:18px;padding:18px}.badge{display:inline-block;border:1px solid #4F7CFF;color:#AAB4C0;border-radius:999px;padding:4px 10px;margin:2px}.blocked{border-color:#D29922}.ok{border-color:#00C2A8}.muted{color:#AAB4C0}.card{background:#1C2128;border:1px solid #30363D;border-radius:14px;padding:14px;margin:10px 0}.timeline{border-left:2px solid #4F7CFF;padding-left:14px}</style></head><body>");
        html.AppendLine("<main class=\"shell\">");
        html.AppendLine("<aside class=\"panel\"><h1>NODAL OS</h1><p class=\"muted\">Mission Control context intake</p><span class=\"badge\">Read-only preview</span><span class=\"badge\">No runtime</span><span class=\"badge\">No LLM</span><span class=\"badge\">No prompt</span></aside>");
        html.AppendLine("<section class=\"panel\"><h2>Context Intake UI Preview</h2>");
        html.AppendLine($"<p>{Encode(preview.NoFilesReadDisclosureRedacted)}</p><p>{Encode(preview.NoLlmDisclosureRedacted)}</p><p>{Encode(preview.NoPromptCreationDisclosureRedacted)}</p><p>{Encode(preview.NoRealProjectUnderstandingDisclosureRedacted)}</p>");
        html.AppendLine($"<div><span class=\"badge ok\">safe {preview.SafeCount}</span><span class=\"badge blocked\">blocked {preview.BlockedCount}</span><span class=\"badge\">requires review {preview.RequiresReviewCount}</span></div>");
        foreach (var card in preview.ReviewCards)
        {
            html.AppendLine("<article class=\"card\">");
            html.AppendLine($"<h3>{Encode(card.CardTitleRedacted)}</h3><p>{Encode(card.SummaryRedacted)}</p>");
            html.AppendLine($"<p class=\"muted\">provenance: {Encode(card.ProvenanceLabelRedacted)} / confidence: {Encode(card.ConfidenceLabelRedacted)} / freshness: {Encode(card.FreshnessLabelRedacted)} / sensitivity: {Encode(card.SensitivityLabelRedacted)}</p>");
            html.AppendLine($"<p>status: {Encode(card.Status.ToString())}; options are disabled/no-op.</p>");
            html.AppendLine("</article>");
        }
        html.AppendLine("<div class=\"timeline\"><h3>Questions for user</h3>");
        foreach (var question in preview.QuestionsForUserRedacted)
            html.AppendLine($"<p>{Encode(question)}</p>");
        html.AppendLine("</div></section>");
        html.AppendLine("<aside class=\"panel\"><h2>Validation Summary</h2>");
        html.AppendLine($"<p>{Encode(summary.HumanReadableSummaryRedacted)}</p><p>{Encode(summary.TechnicalSummaryRedacted)}</p>");
        html.AppendLine($"<h2>Readiness</h2><p>{Encode(report.State.ToString())}</p><p>{Encode(report.UserFacingExplanationRedacted)}</p>");
        html.AppendLine("<h3>Guardrails</h3>");
        foreach (var guardrail in preview.GuardrailExplainersRedacted.Concat(report.GuardrailRefs).Distinct())
            html.AppendLine($"<span class=\"badge\">{Encode(guardrail)}</span>");
        html.AppendLine("</aside></main></body></html>");
        return html.ToString();
    }

    private static NodalOsProjectUnderstandingReadinessState InferReadiness(
        NodalOsWorkspaceLocalModel? workspace,
        NodalOsContextValidationSummary? validationSummary,
        IReadOnlyList<NodalOsSafeContextBoundaryDecision> boundaryDecisions)
    {
        if (workspace is null)
            return NodalOsProjectUnderstandingReadinessState.BlockedByMissingWorkspace;
        if (validationSummary is null)
            return NodalOsProjectUnderstandingReadinessState.BlockedByMissingContext;
        if (validationSummary.CredentialBlockedCount > 0)
            return NodalOsProjectUnderstandingReadinessState.BlockedBySecretContext;
        if (validationSummary.RawPayloadBlockedCount > 0 || boundaryDecisions.Any(boundary => boundary.SensitivityLevel == NodalOsContextSensitivityLevel.SensitiveBlocked))
            return NodalOsProjectUnderstandingReadinessState.BlockedBySensitiveContext;
        if (validationSummary.RequiresReviewCaptures > 0)
            return NodalOsProjectUnderstandingReadinessState.ReadyForSafeContextBoundaryReview;
        if (validationSummary.SafeCaptures > 0)
            return NodalOsProjectUnderstandingReadinessState.ReadyForUserProvidedContextReview;
        return NodalOsProjectUnderstandingReadinessState.NotReady;
    }

    private static IReadOnlyList<string> BlockersFor(NodalOsProjectUnderstandingReadinessState state) => state switch
    {
        NodalOsProjectUnderstandingReadinessState.BlockedByMissingWorkspace => ["Workspace is missing."],
        NodalOsProjectUnderstandingReadinessState.BlockedByMissingContext => ["User-provided context is missing."],
        NodalOsProjectUnderstandingReadinessState.BlockedBySensitiveContext => ["Sensitive or raw context is blocked."],
        NodalOsProjectUnderstandingReadinessState.BlockedBySecretContext => ["Credential-like context is blocked."],
        NodalOsProjectUnderstandingReadinessState.BlockedByLlmPolicyMissing => ["Future BYOK/LLM policy is missing."],
        NodalOsProjectUnderstandingReadinessState.BlockedByFilesystemPolicyMissing => ["Future filesystem policy is missing."],
        NodalOsProjectUnderstandingReadinessState.BlockedByPositiveExecutionGate => ["Positive execution gate is missing."],
        NodalOsProjectUnderstandingReadinessState.UnknownRequiresReview => ["Unknown readiness requires human review."],
        _ => []
    };

    private static IReadOnlyList<string> NextStepsFor(NodalOsProjectUnderstandingReadinessState state) => state switch
    {
        NodalOsProjectUnderstandingReadinessState.ReadyForUserProvidedContextReview => ["Review user-provided context cards.", "Prepare governance before real understanding."],
        NodalOsProjectUnderstandingReadinessState.ReadyForMockSummaryOnly => ["Use mock summary only.", "Do not scan workspace."],
        NodalOsProjectUnderstandingReadinessState.ReadyForSafeContextBoundaryReview => ["Review boundary decisions.", "Resolve requires-review cards."],
        _ => ["Resolve blockers before project understanding readiness."]
    };

    private static string UserFacingExplanationFor(NodalOsProjectUnderstandingReadinessState state) => state switch
    {
        NodalOsProjectUnderstandingReadinessState.ReadyForUserProvidedContextReview => "Safe user-provided context can be reviewed, but no real project understanding is running.",
        NodalOsProjectUnderstandingReadinessState.ReadyForMockSummaryOnly => "Only mock summary context is ready.",
        NodalOsProjectUnderstandingReadinessState.ReadyForSafeContextBoundaryReview => "Some context needs Safe Context Boundary review before future use.",
        _ => "Project understanding is not ready; review blockers and keep Mission Control read-only."
    };

    private static bool IsSafe(NodalOsContextReviewCard card) =>
        card.Status is NodalOsContextReviewCardStatus.SafeForDisplay or NodalOsContextReviewCardStatus.SafeForExport;

    private static bool IsBlocked(NodalOsContextReviewCard card) =>
        card.Status is NodalOsContextReviewCardStatus.BlockedSensitive or NodalOsContextReviewCardStatus.BlockedSecret or NodalOsContextReviewCardStatus.BlockedRawPayload;

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}

public sealed class NodalOsContextIntakePreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreview(NodalOsContextIntakeUiPreview preview) => JsonSerializer.Serialize(preview, Options);

    public string SerializeValidationSummary(NodalOsContextValidationSummary summary) => JsonSerializer.Serialize(summary, Options);

    public string SerializeReadinessReport(NodalOsProjectUnderstandingReadinessReport report) => JsonSerializer.Serialize(report, Options);
}

public static class NodalOsContextIntakePreviewFixtures
{
    public static (NodalOsContextIntakeUiPreview Preview, NodalOsContextValidationSummary Summary, NodalOsProjectUnderstandingReadinessReport Report) SafeContextIntakeSet()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var userContext = new NodalOsUserContextService();
        var previewService = new NodalOsContextIntakePreviewService();
        var capture = userContext.CreateCapture(workspace, NodalOsUserContextCaptureType.UserSummary);
        var card = userContext.CreateReviewCard(capture);
        var link = userContext.CreateEvidenceLink(capture, card, capture.EvidenceRefs[0]);
        var preview = previewService.CreatePreview(workspace, [capture], [card], [link]);
        var summary = previewService.CreateValidationSummary(workspace, [capture], [card], [link]);
        var report = previewService.CreateReadinessReport(workspace, NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake(), summary, [capture.BoundaryDecision]);
        return (preview, summary, report);
    }
}
