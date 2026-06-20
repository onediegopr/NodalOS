using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsTaskGraphInteractionNoOpService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsTaskGraphInteractionNoOpRequest CreateRequest(
        NodalOsTaskGraphInteractionKind kind,
        string assignmentUiPreviewId = "assignment-ui-preview-ref",
        string? workItemId = "workitem-analysis-draft",
        string? draftNote = null)
    {
        return new()
        {
            InteractionId = $"interaction-{kind}",
            InteractionKind = kind,
            AssignmentUiPreviewId = SafeValue(assignmentUiPreviewId),
            WorkItemId = workItemId is null ? null : SafeValue(workItemId),
            DraftNoteRedacted = draftNote is null ? null : SafeValue(draftNote),
            SelectedRefsRedacted = ["evidence-ref-only", "timeline-ref-only", "context-ref-only"]
        };
    }

    public NodalOsTaskGraphInteractionNoOpResult Apply(NodalOsTaskGraphInteractionNoOpRequest request)
    {
        return new()
        {
            InteractionId = SafeValue(request.InteractionId),
            InteractionKind = request.InteractionKind,
            AssignmentUiPreviewId = SafeValue(request.AssignmentUiPreviewId),
            WorkItemId = request.WorkItemId is null ? null : SafeValue(request.WorkItemId),
            IsNoOp = true,
            MutatesState = false,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            PlannerExecutionAllowed = false,
            LlmCallAllowed = false,
            FilesystemAccessAllowed = false,
            NetworkAccessAllowed = false,
            CreatesExecutionRequest = false,
            CreatesPrompt = false,
            UsesClipboard = false,
            RequiresFutureImplementation = RequiresFutureImplementation(request.InteractionKind),
            UserFacingExplanationRedacted = ExplanationFor(request.InteractionKind),
            GuardrailRefs =
            [
                "guardrail-taskgraph-interaction-no-op",
                "guardrail-no-runtime",
                "guardrail-no-provider",
                "guardrail-no-filesystem",
                "guardrail-no-network"
            ],
            EvidenceRefs = request.InteractionKind == NodalOsTaskGraphInteractionKind.ShowEvidenceRefs
                ? ["evidence-ref-only"]
                : [],
            TimelineRefs = request.InteractionKind == NodalOsTaskGraphInteractionKind.ShowTimelineRefs
                ? ["timeline-ref-only"]
                : []
        };
    }

    private static bool RequiresFutureImplementation(NodalOsTaskGraphInteractionKind kind) => kind switch
    {
        NodalOsTaskGraphInteractionKind.CopyTechnicalReportPreview => true,
        NodalOsTaskGraphInteractionKind.AskToReviseDraft => true,
        _ => false
    };

    private static string ExplanationFor(NodalOsTaskGraphInteractionKind kind) => kind switch
    {
        NodalOsTaskGraphInteractionKind.SelectWorkItem => "Selection changes only the preview focus and does not mutate state.",
        NodalOsTaskGraphInteractionKind.ExpandWorkItem => "Expand is visual-only and no-op.",
        NodalOsTaskGraphInteractionKind.CollapseWorkItem => "Collapse is visual-only and no-op.",
        NodalOsTaskGraphInteractionKind.FilterByStatus => "Filter changes only visual ordering in preview.",
        NodalOsTaskGraphInteractionKind.FilterByRisk => "Risk filter is preview-only.",
        NodalOsTaskGraphInteractionKind.FilterByBlocker => "Blocker filter is preview-only.",
        NodalOsTaskGraphInteractionKind.SortVisualOrder => "Sort changes no persisted order.",
        NodalOsTaskGraphInteractionKind.RequestExplanation => "Explanation is static and does not call a model.",
        NodalOsTaskGraphInteractionKind.MarkNeedsReview => "Needs-review mark is a visual intent only.",
        NodalOsTaskGraphInteractionKind.AddDraftNote => "Draft note is not persisted and does not mutate state.",
        NodalOsTaskGraphInteractionKind.AskToReviseDraft => "Revision request is a visual intent and does not invoke a planner.",
        NodalOsTaskGraphInteractionKind.CompareWorkItems => "Compare is preview-only and uses refs.",
        NodalOsTaskGraphInteractionKind.ShowEvidenceRefs => "Evidence display is ref-only and contains no raw payload.",
        NodalOsTaskGraphInteractionKind.ShowTimelineRefs => "Timeline display is ref-only.",
        NodalOsTaskGraphInteractionKind.ShowGuardrails => "Guardrails are read-only explanations.",
        NodalOsTaskGraphInteractionKind.CopyTechnicalReportPreview => "Copy preview is modeled only; no clipboard is used.",
        _ => "Interaction is no-op."
    };

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed class NodalOsTaskGraphInteractionNoOpJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeRequest(NodalOsTaskGraphInteractionNoOpRequest request) =>
        JsonSerializer.Serialize(request, Options);

    public string SerializeResult(NodalOsTaskGraphInteractionNoOpResult result) =>
        JsonSerializer.Serialize(result, Options);
}
