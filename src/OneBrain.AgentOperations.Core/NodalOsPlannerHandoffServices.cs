using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsPlannerHandoffService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsPlannerHandoffPack CreateHandoff(
        NodalOsAssignmentUiPreview preview,
        NodalOsAssignmentReviewSnapshot snapshot,
        NodalOsPlannerUxAcceptancePack acceptance)
    {
        return new()
        {
            HandoffPackId = $"planner-handoff-{SafeValue(snapshot.Session.ReviewSessionId)}",
            MissionRef = SafeValue(snapshot.Session.MissionIdRef),
            AssignmentRef = SafeValue(snapshot.Session.AssignmentIdRef),
            TaskGraphDraftRefs = preview.ReviewPanel.OutputRefsRedacted.Select(SafeValue).ToArray(),
            ReviewSessionRefs = [SafeValue(snapshot.Session.ReviewSessionId)],
            SelectedBlockersRedacted = preview.WorkItems.SelectMany(item => item.BlockersRedacted).Select(SafeValue).ToArray(),
            OpenQuestionsRedacted = ["Which draft work item should be clarified first?", "Which blockers are acceptable for a future review pass?"],
            MissingReadinessGatesRedacted = preview.ReviewPanel.MissingReadinessRedacted.Select(SafeValue).ToArray(),
            EvidenceRefs = snapshot.State.VisibleEvidenceRefs.Select(SafeValue).ToArray(),
            TimelineRefs = snapshot.State.VisibleTimelineRefs.Select(SafeValue).ToArray(),
            ContextRefsRedacted = snapshot.State.VisibleContextRefsRedacted.Select(SafeValue).ToArray(),
            GuardrailRefs = acceptance.GuardrailRefs.Select(SafeValue).ToArray(),
            DisclaimersRedacted =
            [
                "Draft-only.",
                "Non-authoritative.",
                "Non-executable.",
                "Planner runtime was not used.",
                "No model call was made.",
                "No prompt was generated.",
                "Runtime execution is disabled.",
                "Filesystem access was not used.",
                "Evidence content was not verified.",
                "User review only."
            ],
            WhatWasReviewedRedacted = "Assignment UI preview, TaskGraph draft, review notes, blockers, and refs were reviewed.",
            WhatIsBlockedRedacted = "Runtime execution, model calls, prompt creation, filesystem access, and authoritative planning remain blocked.",
            WhatNeedsUserDecisionRedacted = "User must decide which draft blockers or open questions should be clarified next.",
            EvidenceRefsOnlyRedacted = "Evidence is included as refs only; no raw payload is embedded.",
            WhatIsNotVerifiedRedacted = "Claims, evidence content, and work item assumptions are not verified.",
            WhatCannotExecuteRedacted = "No work item can execute; the TaskGraph remains draft-only.",
            RecommendedNextSafeStepRedacted = "Review the handoff and decide whether another safe draft pass is needed.",
            DraftOnly = true,
            IsAuthoritative = false,
            Executable = false,
            PlannerRuntimeUsed = false,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            RuntimeExecutionAllowed = false,
            FilesystemAccessUsed = false,
            VerifiesEvidenceContent = false
        };
    }

    public NodalOsPlannerHandoffRender Render(NodalOsPlannerHandoffPack handoff)
    {
        var markdown = $"""
            # NODAL OS Planner Handoff

            ## What was reviewed
            {handoff.WhatWasReviewedRedacted}

            ## What is blocked
            {handoff.WhatIsBlockedRedacted}

            ## What still needs user decision
            {handoff.WhatNeedsUserDecisionRedacted}

            ## Evidence refs only
            {handoff.EvidenceRefsOnlyRedacted}

            ## What is not verified
            {handoff.WhatIsNotVerifiedRedacted}

            ## What cannot execute
            {handoff.WhatCannotExecuteRedacted}

            ## Recommended next safe step
            {handoff.RecommendedNextSafeStepRedacted}
            """;

        var html = """
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>NODAL OS Planner Handoff</title></head>
            <body>
              <main data-nodal-os="planner-handoff">
                <h1>NODAL OS Planner Handoff</h1>
                <p>Draft-only. Non-authoritative. Non-executable.</p>
                <p>No model call was made. No prompt was generated. Runtime execution is disabled.</p>
                <p>Evidence is refs only and content is not verified.</p>
              </main>
            </body>
            </html>
            """;

        return new()
        {
            HandoffPackId = handoff.HandoffPackId,
            MarkdownRedacted = SafeValue(markdown),
            HtmlRedacted = SafeValue(html),
            Deterministic = true,
            ContainsRawPayload = false,
            ContainsExternalResource = false
        };
    }

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed class NodalOsPlannerHandoffJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeHandoff(NodalOsPlannerHandoffPack handoff) =>
        JsonSerializer.Serialize(handoff, Options);

    public string SerializeRender(NodalOsPlannerHandoffRender render) =>
        JsonSerializer.Serialize(render, Options);
}
