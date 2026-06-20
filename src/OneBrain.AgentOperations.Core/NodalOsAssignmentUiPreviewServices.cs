using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAssignmentUiPreviewService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsAssignmentUiPreview CreatePreview(
        NodalOsAssignmentRequestDraft request,
        NodalOsTaskGraphDraft taskGraph,
        NodalOsPlannerReadinessGateResult readiness,
        NodalOsMissionPlanDraftPreview missionPlan)
    {
        var selected = taskGraph.Tasks.FirstOrDefault() ?? EmptyWorkItem();
        var workItems = taskGraph.Tasks.Select(workItem => new NodalOsAssignmentUiWorkItemPreview
        {
            WorkItemId = SafeValue(workItem.TaskId),
            TitleRedacted = SafeValue(workItem.TitleRedacted),
            DraftStatusRedacted = SafeValue(workItem.Status.ToString()),
            RiskLevel = workItem.RiskLevel,
            DependencyIds = workItem.DependencyIds.Select(SafeValue).ToArray(),
            BlockersRedacted = workItem.BlockedByIds.Select(SafeValue).ToArray(),
            EvidenceRefIds = workItem.EvidenceRefs.Select(evidence => SafeValue(evidence.EvidenceId)).ToArray(),
            TimelineRefIds = workItem.TimelineRefs.Select(SafeValue).ToArray(),
            ContextRefIdsRedacted = taskGraph.ContextRefsRedacted.Select(SafeValue).ToArray(),
            CanExecute = false,
            IsAuthoritative = false
        }).ToArray();

        return new()
        {
            AssignmentUiPreviewId = $"assignment-ui-preview-{SafeValue(request.AssignmentRequestId)}",
            PreviewState = workItems.Length == 0
                ? NodalOsAssignmentUiPreviewState.EmptyAssignment
                : NodalOsAssignmentUiPreviewState.DraftAvailable,
            Header = new()
            {
                MissionIdRef = SafeValue(request.MissionId),
                AssignmentIdRef = SafeValue(request.AssignmentRequestId),
                PlannerReadinessStatus = readiness.ReadinessState,
                DraftOnlyDisclosureRedacted = "Assignment UI is draft-only and review-only.",
                RuntimeBlockedDisclosureRedacted = "Runtime execution is blocked.",
                LlmBlockedDisclosureRedacted = "LLM/provider calls are blocked.",
                FilesystemBlockedDisclosureRedacted = "Filesystem access is blocked."
            },
            WorkItems = workItems,
            ReviewPanel = new()
            {
                SelectedWorkItemId = SafeValue(selected.TaskId),
                SelectedSummaryRedacted = SafeValue(selected.SummaryRedacted),
                WhyItExistsRedacted = "This work item exists as a draft review surface from the non-authoritative TaskGraph.",
                InputRefsRedacted = taskGraph.ContextRefsRedacted.Select(SafeValue).ToArray(),
                OutputRefsRedacted = [SafeValue(missionPlan.MissionPlanPreviewId), SafeValue(taskGraph.TaskGraphId)],
                GuardrailRefs = missionPlan.GuardrailRefs,
                MissingReadinessRedacted =
                [
                    "Positive execution gate is not implemented.",
                    "Planner runtime is not implemented.",
                    "Provider and filesystem policies remain blocked."
                ],
                UserReviewOptions = Enum.GetValues<NodalOsTaskGraphReviewOptionKind>(),
                UserReviewOptionsAreNoOp = true
            },
            ExplanationPanel = new()
            {
                CannotExecuteExplanationRedacted = "This assignment cannot execute because the TaskGraph is draft-only and every work item has CanExecute=false.",
                FutureExecutionNeedsRedacted = "Future execution would require separate runtime policy, readiness gates, human approval, and implementation not present here.",
                ApprovalDoesNotUnlockRuntimeRedacted = "Approval display does not unlock runtime execution.",
                PlannerNotImplementedRedacted = "Planner runtime is not implemented."
            },
            EvidenceRefs = taskGraph.EvidenceRefs.Select(evidence => SafeValue(evidence.EvidenceId)).ToArray(),
            TimelineRefs = taskGraph.TimelineRefs.Select(SafeValue).ToArray(),
            ContextRefsRedacted = taskGraph.ContextRefsRedacted.Select(SafeValue).ToArray(),
            GuardrailRefs =
            [
                "guardrail-assignment-ui-preview-only",
                "guardrail-interactions-no-op",
                "guardrail-no-runtime",
                "guardrail-no-provider-call",
                "guardrail-no-filesystem"
            ],
            DraftOnly = true,
            ReadOnly = true,
            IsAuthoritative = false,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            PlannerExecutionAllowed = false,
            LlmCallAllowed = false,
            FilesystemAccessAllowed = false,
            NetworkAccessAllowed = false
        };
    }

    public NodalOsAssignmentUiPreviewRender Render(NodalOsAssignmentUiPreview preview)
    {
        var items = string.Join(
            Environment.NewLine,
            preview.WorkItems.Select(workItem =>
                $"<li><strong>{Escape(workItem.TitleRedacted)}</strong><span>CanExecute=false</span><span>IsAuthoritative=false</span></li>"));

        var html = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <title>NODAL OS Assignment UI Preview</title>
              <style>
                body {{ margin: 0; background: #10130f; color: #edf5e8; font-family: Georgia, 'Times New Roman', serif; }}
                main {{ max-width: 1120px; margin: 0 auto; padding: 40px 20px; }}
                section {{ border: 1px solid #33402f; border-radius: 18px; margin: 16px 0; padding: 18px; background: #1b2119; }}
                .blocked {{ color: #d9b65b; }}
              </style>
            </head>
            <body>
              <main data-nodal-os="assignment-ui-preview">
                <h1>NODAL OS Assignment UI Preview</h1>
                <section aria-label="assignment header">
                  <p>__DRAFT_DISCLOSURE__</p>
                  <p class="blocked">__RUNTIME_DISCLOSURE__</p>
                  <p class="blocked">__LLM_DISCLOSURE__</p>
                  <p class="blocked">__FILESYSTEM_DISCLOSURE__</p>
                </section>
                <section aria-label="work graph">
                  <h2>TaskGraph Draft</h2>
                  <ul>__WORK_ITEMS__</ul>
                </section>
                <section aria-label="review panel">
                  <h2>Review</h2>
                  <p>__SELECTED_SUMMARY__</p>
                  <p>__WHY_IT_EXISTS__</p>
                </section>
                <section aria-label="explanation panel">
                  <h2>Why blocked</h2>
                  <p>__CANNOT_EXECUTE__</p>
                  <p>__APPROVAL_NO_RUNTIME__</p>
                  <p>__PLANNER_NOT_IMPLEMENTED__</p>
                </section>
              </main>
            </body>
            </html>
            """
            .Replace("__DRAFT_DISCLOSURE__", Escape(preview.Header.DraftOnlyDisclosureRedacted), StringComparison.Ordinal)
            .Replace("__RUNTIME_DISCLOSURE__", Escape(preview.Header.RuntimeBlockedDisclosureRedacted), StringComparison.Ordinal)
            .Replace("__LLM_DISCLOSURE__", Escape(preview.Header.LlmBlockedDisclosureRedacted), StringComparison.Ordinal)
            .Replace("__FILESYSTEM_DISCLOSURE__", Escape(preview.Header.FilesystemBlockedDisclosureRedacted), StringComparison.Ordinal)
            .Replace("__WORK_ITEMS__", items, StringComparison.Ordinal)
            .Replace("__SELECTED_SUMMARY__", Escape(preview.ReviewPanel.SelectedSummaryRedacted), StringComparison.Ordinal)
            .Replace("__WHY_IT_EXISTS__", Escape(preview.ReviewPanel.WhyItExistsRedacted), StringComparison.Ordinal)
            .Replace("__CANNOT_EXECUTE__", Escape(preview.ExplanationPanel.CannotExecuteExplanationRedacted), StringComparison.Ordinal)
            .Replace("__APPROVAL_NO_RUNTIME__", Escape(preview.ExplanationPanel.ApprovalDoesNotUnlockRuntimeRedacted), StringComparison.Ordinal)
            .Replace("__PLANNER_NOT_IMPLEMENTED__", Escape(preview.ExplanationPanel.PlannerNotImplementedRedacted), StringComparison.Ordinal);

        return new()
        {
            RenderId = $"assignment-ui-render-{preview.AssignmentUiPreviewId}",
            AssignmentUiPreviewId = preview.AssignmentUiPreviewId,
            HtmlRedacted = SafeValue(html),
            StaticOnly = true,
            ContainsScript = false,
            ContainsExternalResource = false,
            CallsNetwork = false
        };
    }

    private NodalOsAssignmentTaskDraft EmptyWorkItem() =>
        new()
        {
            TaskId = "workitem-empty-assignment",
            TitleRedacted = "Empty assignment",
            SummaryRedacted = "No draft work item is selected.",
            TaskKind = NodalOsAssignmentTaskKind.Unknown,
            Status = NodalOsAssignmentTaskStatus.Draft,
            RiskLevel = NodalOsAssignmentRiskLevel.UnknownRequiresReview,
            SuggestedAssigneeType = NodalOsSuggestedAssigneeType.Unknown,
            RequiresApproval = false,
            RequiresLlmFuture = false,
            RequiresRuntimeFuture = false,
            RequiresFilesystemFuture = false,
            CanExecute = false
        };

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}

public sealed class NodalOsAssignmentUiPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePreview(NodalOsAssignmentUiPreview preview) =>
        JsonSerializer.Serialize(preview, Options);

    public string SerializeRender(NodalOsAssignmentUiPreviewRender render) =>
        JsonSerializer.Serialize(render, Options);
}
