using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsMissionPlanPreviewService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsMissionPlanDraftPreview CreateMissionPlanDraftPreview(
        NodalOsAssignmentRequestDraft request,
        NodalOsTaskGraphDraft taskGraph,
        NodalOsPlannerReadinessGateResult? readiness = null)
    {
        var blockedCount = taskGraph.Tasks.Count(workItem => workItem.Status == NodalOsAssignmentTaskStatus.Blocked || workItem.RequiresRuntimeFuture);
        var highRiskCount = taskGraph.Tasks.Count(workItem => workItem.RiskLevel == NodalOsAssignmentRiskLevel.High);

        return new()
        {
            MissionPlanPreviewId = $"mission-plan-preview-{SafeValue(request.AssignmentRequestId)}",
            AssignmentRequestId = SafeValue(request.AssignmentRequestId),
            TaskGraphId = SafeValue(taskGraph.TaskGraphId),
            WorkspaceId = SafeValue(request.WorkspaceId),
            MissionId = SafeValue(request.MissionId),
            TitleRedacted = "NODAL OS mission plan draft preview",
            SummaryRedacted = "Draft-only mission plan based on a non-authoritative TaskGraph.",
            PlanningPurpose = request.RequestedPlanningPurpose,
            ReadinessStatus = readiness?.ReadinessState ?? NodalOsPlannerReadinessState.ReadyForManualDraftOnly,
            DraftStatus = NodalOsMissionPlanDraftStatus.DraftOnly,
            WorkItemsSummaryRedacted = $"{taskGraph.Tasks.Count} draft work items; all non-executable.",
            DependencySummaryRedacted = taskGraph.DependenciesRedacted.Count == 0
                ? "No dependency summary provided."
                : SafeValue(string.Join("; ", taskGraph.DependenciesRedacted)),
            RiskSummaryRedacted = $"{highRiskCount} high-risk draft items; future execution remains blocked.",
            BlockedItemsSummaryRedacted = $"{blockedCount} blocked draft items require review before future use.",
            HumanReviewRequirementsRedacted =
            [
                "Human review is required before future use.",
                SafeValue(taskGraph.HumanReviewRequirementRedacted)
            ],
            DisabledCapabilitiesRedacted =
            [
                "runtime execution",
                "provider planning",
                "prompt construction",
                "filesystem access",
                "work dispatch"
            ],
            NextSafeStepsRedacted =
            [
                "Review the draft-only plan.",
                "Review blocked work items.",
                "Link evidence refs without raw payloads.",
                "Keep future runtime and model use disabled."
            ],
            EvidenceRefs = taskGraph.EvidenceRefs.Count > 0 ? taskGraph.EvidenceRefs : [EvidenceRef("evidence-mission-plan-preview-ref-only")],
            TimelineRefs = taskGraph.TimelineRefs.Count > 0 ? taskGraph.TimelineRefs : ["timeline-mission-plan-preview-ref-only"],
            GuardrailRefs =
            [
                "guardrail-mission-plan-draft-only",
                "guardrail-workitem-non-executable",
                "guardrail-no-model-call",
                "guardrail-no-prompt",
                "guardrail-no-runtime"
            ],
            DisclosuresRedacted =
            [
                "Mission plan is draft-only.",
                "No task is executable.",
                "No model was called.",
                "No prompt was generated.",
                "No runtime action was created.",
                "Human review is required before future use."
            ],
            DraftOnly = true,
            ReadOnly = true,
            TaskGraphExecutable = false,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            CreatesRuntimeAction = false,
            TouchesFilesystem = false,
            SchedulesWork = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public IReadOnlyList<NodalOsTaskGraphReviewCard> CreateTaskGraphReviewCards(NodalOsTaskGraphDraft taskGraph) =>
        taskGraph.Tasks.Select(workItem => CreateTaskGraphReviewCard(taskGraph, workItem)).ToArray();

    public NodalOsTaskGraphReviewCard CreateTaskGraphReviewCard(
        NodalOsTaskGraphDraft taskGraph,
        NodalOsAssignmentTaskDraft workItem)
    {
        var state = workItem.TaskKind == NodalOsAssignmentTaskKind.FutureExecutionPlaceholder
            ? NodalOsTaskGraphReviewCardState.FutureExecutionBlocked
            : workItem.Status switch
            {
                NodalOsAssignmentTaskStatus.NeedsReview => NodalOsTaskGraphReviewCardState.NeedsReview,
                NodalOsAssignmentTaskStatus.ReadyForManualReview => NodalOsTaskGraphReviewCardState.ReadyForManualReview,
                NodalOsAssignmentTaskStatus.Blocked => NodalOsTaskGraphReviewCardState.Blocked,
                NodalOsAssignmentTaskStatus.Deferred => NodalOsTaskGraphReviewCardState.Deferred,
                NodalOsAssignmentTaskStatus.ArchivedMock => NodalOsTaskGraphReviewCardState.DiscardedMock,
                _ => NodalOsTaskGraphReviewCardState.Draft
            };

        return new()
        {
            ReviewCardId = $"review-card-{SafeValue(workItem.TaskId)}",
            TaskGraphId = SafeValue(taskGraph.TaskGraphId),
            WorkItemId = SafeValue(workItem.TaskId),
            TitleRedacted = SafeValue(workItem.TitleRedacted),
            SummaryRedacted = SafeValue(workItem.SummaryRedacted),
            TaskKind = workItem.TaskKind,
            ReviewState = state,
            RiskLevel = workItem.RiskLevel,
            DependencyIds = workItem.DependencyIds.Select(SafeValue).ToArray(),
            BlockersRedacted = workItem.BlockedByIds.Count == 0 ? [] : workItem.BlockedByIds.Select(SafeValue).ToArray(),
            DisabledCapabilitiesRedacted =
            [
                ..workItem.DisabledCapabilitiesRedacted.Select(SafeValue),
                "future runtime blocked",
                "future model call blocked",
                "future filesystem access blocked"
            ],
            RequiresHumanReview = true,
            RequiresFutureLlm = workItem.RequiresLlmFuture,
            RequiresFutureApproval = workItem.RequiresApproval,
            RequiresFutureFilesystem = workItem.RequiresFilesystemFuture,
            RequiresFutureRuntime = workItem.RequiresRuntimeFuture,
            EvidenceRefs = workItem.EvidenceRefs,
            TimelineRefs = workItem.TimelineRefs,
            ContextRefsRedacted = taskGraph.ContextRefsRedacted.Select(SafeValue).ToArray(),
            GuardrailRefs =
            [
                "guardrail-review-card-no-authority",
                "guardrail-user-options-no-op",
                "guardrail-workitem-non-executable"
            ],
            UserOptions = Enum.GetValues<NodalOsTaskGraphReviewOptionKind>(),
            UserOptionsAreNoOp = true,
            NonAuthoritative = true,
            CanExecute = false
        };
    }

    public NodalOsAssignmentEvidenceLink CreateAssignmentEvidenceLink(
        NodalOsAssignmentEvidenceLinkType linkType = NodalOsAssignmentEvidenceLinkType.SupportsPlanDraft,
        NodalOsAssignmentEvidenceLinkStatus status = NodalOsAssignmentEvidenceLinkStatus.LinkedRefOnly,
        string assignmentRequestId = "assignment-request-ref",
        string taskGraphId = "taskgraph-draft-ref",
        string? workItemId = "workitem-analysis-draft",
        string evidenceRefId = "evidence-assignment-link-ref-only",
        string? timelineRefId = "timeline-assignment-link-ref-only",
        string? contextRefId = "context-ref-safe",
        string reason = "User-provided claim remains unverified and needs review.")
    {
        var unsafeEvidence = classifier.ContainsSensitiveContent(evidenceRefId)
            || evidenceRefId.Contains("raw", StringComparison.OrdinalIgnoreCase)
            || evidenceRefId.Contains("screenshot", StringComparison.OrdinalIgnoreCase)
            || evidenceRefId.Contains("dom", StringComparison.OrdinalIgnoreCase)
            || evidenceRefId.Contains("network", StringComparison.OrdinalIgnoreCase);
        var finalStatus = unsafeEvidence ? NodalOsAssignmentEvidenceLinkStatus.BlockedUnsafeEvidence : status;

        return new()
        {
            AssignmentEvidenceLinkId = $"assignment-evidence-link-{linkType}-{finalStatus}",
            AssignmentRequestId = SafeValue(assignmentRequestId),
            TaskGraphId = SafeValue(taskGraphId),
            WorkItemId = workItemId is null ? null : SafeValue(workItemId),
            EvidenceRefId = unsafeEvidence ? "blocked-unsafe-evidence-ref" : SafeValue(evidenceRefId),
            TimelineRefId = timelineRefId is null ? null : SafeValue(timelineRefId),
            ContextRefId = contextRefId is null ? null : SafeValue(contextRefId),
            LinkType = linkType,
            LinkStatus = finalStatus,
            LinkReasonRedacted = SafeValue(reason),
            ProvenanceRedacted = "NODAL OS assignment evidence link; ref-only; claim unverified.",
            ValidationResultRedacted = unsafeEvidence
                ? "Blocked unsafe evidence ref. No raw payload accepted."
                : "Linked as ref-only. Content remains unverified and requires review.",
            RefOnly = true,
            ContainsRawEvidencePayload = false,
            ContainsInlineScreenshot = false,
            ContainsRawDom = false,
            ContainsRawNetwork = false,
            ReadsFiles = false,
            VerifiesRealContent = false,
            ConvertsPlanToAuthoritativeTruth = false,
            CallsLlmProvider = false,
            CallsCloud = false,
            ExecutesRuntime = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsMissionPlanDraftPreviewRender RenderMissionPlanDraftPreview(NodalOsMissionPlanDraftPreview preview)
    {
        var html = $"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>{Escape(preview.TitleRedacted)}</title></head>
            <body>
              <main data-nodal-os="mission-plan-draft-preview">
                <h1>{Escape(preview.TitleRedacted)}</h1>
                <p>{Escape(preview.SummaryRedacted)}</p>
                <section aria-label="Draft disclosures">
                  <ul>
                    {string.Join(Environment.NewLine, preview.DisclosuresRedacted.Select(item => $"<li>{Escape(item)}</li>"))}
                  </ul>
                </section>
                <section aria-label="Draft summaries">
                  <p>{Escape(preview.WorkItemsSummaryRedacted)}</p>
                  <p>{Escape(preview.DependencySummaryRedacted)}</p>
                  <p>{Escape(preview.RiskSummaryRedacted)}</p>
                  <p>{Escape(preview.BlockedItemsSummaryRedacted)}</p>
                </section>
              </main>
            </body>
            </html>
            """;

        return new()
        {
            RenderId = $"render-{preview.MissionPlanPreviewId}",
            MissionPlanPreviewId = preview.MissionPlanPreviewId,
            HtmlRedacted = SafeValue(html),
            StaticOnly = true,
            ReadOnly = true,
            ContainsRawSecrets = false,
            ContainsRuntimeControl = false
        };
    }

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("sk-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static NodalOsEvidenceBridgeRef EvidenceRef(string evidenceId) =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = "mission-plan-preview-ref-only",
            SourceKind = NodalOsEvidenceBridgeSourceKind.Manual,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = $"ledger:{evidenceId}",
            Provenance = "M522-M524 mission plan draft review contract",
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsMissionPlanPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreview(NodalOsMissionPlanDraftPreview preview) =>
        JsonSerializer.Serialize(preview, Options);

    public string SerializeRender(NodalOsMissionPlanDraftPreviewRender render) =>
        JsonSerializer.Serialize(render, Options);

    public string SerializeReviewCards(IReadOnlyList<NodalOsTaskGraphReviewCard> cards) =>
        JsonSerializer.Serialize(cards, Options);

    public string SerializeEvidenceLink(NodalOsAssignmentEvidenceLink link) =>
        JsonSerializer.Serialize(link, Options);
}

public static class NodalOsMissionPlanPreviewFixtures
{
    public static NodalOsMissionPlanDraftPreview Preview()
    {
        var assignment = NodalOsAssignmentEngineFixtures.AssignmentRequest();
        var taskGraph = new NodalOsAssignmentEngineDraftService().CreateTaskGraphDraft(assignment);
        var readiness = NodalOsAssignmentEngineFixtures.PlannerReadiness();
        return new NodalOsMissionPlanPreviewService().CreateMissionPlanDraftPreview(assignment, taskGraph, readiness);
    }

    public static IReadOnlyList<NodalOsTaskGraphReviewCard> ReviewCards()
    {
        var taskGraph = NodalOsAssignmentEngineFixtures.TaskGraphDraft();
        return new NodalOsMissionPlanPreviewService().CreateTaskGraphReviewCards(taskGraph);
    }

    public static NodalOsAssignmentEvidenceLink EvidenceLink() =>
        new NodalOsMissionPlanPreviewService().CreateAssignmentEvidenceLink();
}
