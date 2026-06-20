using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAssignmentArchiveReviewService
{
    private static readonly DateTimeOffset FixtureTime = new(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);

    public NodalOsAssignmentArchiveReview CreateArchiveReview()
    {
        return new()
        {
            ArchiveReviewId = "assignment-archive-review-m535",
            ClosedMilestones =
            [
                Milestone("M519-M521", "Assignment Engine v1 Contracts / TaskGraph Draft / Planner Readiness Gate",
                    "ASSIGNMENT_ENGINE_CONTRACTS_READY",
                    "Tasks draft with CanExecute=false; FutureRuntimeExecution disabled.",
                    "TaskGraph is draft-only; no execution authority; planner runtime not implemented.",
                    "TaskGraph must not be promoted to executable without a separate audit and positive execution gate."),
                Milestone("M522-M524", "Mission Plan Draft Preview / TaskGraph Review Cards / Assignment Evidence Linking",
                    "MISSION_PLAN_PREVIEW_READY",
                    "Preview HTML static/redacted; evidence/timeline/context refs; claims unverified.",
                    "Preview is static only; evidence is ref-only; claims remain unverified/needs-review.",
                    "Preview HTML cannot be used as source of truth; evidence refs are not evidence content."),
                Milestone("M525-M527", "Assignment UI Preview Static / TaskGraph Interaction No-Op / Planner UX Acceptance",
                    "PLANNER_UX_ACCEPTANCE_READY",
                    "UI interactions are no-op; visual-only; no operative wiring.",
                    "All UI interactions are no-op; no operative state changes; no execution wiring.",
                    "No-op interactions must not be wired to real operations without explicit gating."),
                Milestone("M528-M530", "Assignment Review Persistence Mock / Planner Handoff Contract / Assignment Safety Audit Pack",
                    "ASSIGNMENT_REVIEW_HANDOFF_SAFETY_READY",
                    "Persistence is mock-only; handoff is draft; safety audit pack completed.",
                    "Mock persistence is not productive persistence; handoff is not execution authority.",
                    "Mock persistence must never replace productive persistence without explicit migration audit."),
                Milestone("M531-M533", "Assignment Review History Mock / Handoff Compare Preview / Planner Governance Closeout",
                    "ASSIGNMENT_PLANNER_GOVERNANCE_CLOSEOUT_READY",
                    "History mock stored; handoff compare ref-only; governance closeout declared.",
                    "History mock is not persistence; compare does not verify content; governance closeout is not runtime permission.",
                    "Governance closeout must not be interpreted as execution authorization.")
            ],
            ArchiveStatus = new()
            {
                CanArchiveAsGovernanceBaseline = true,
                CanUseAsRuntimeBaseline = false,
                CanUseAsPlannerImplementation = false,
                CanUseAsLlmPromptSource = false,
                CanUseAsFilesystemAuthority = false
            },
            ArchiveWarningsRedacted =
            [
                "History mock is not productive persistence and cannot restore authoritative state.",
                "Handoff compare preview does not verify evidence content; comparison is ref-only.",
                "Planner governance closeout is not runtime permission; execution remains blocked.",
                "Approval of governance closeout still does not unlock runtime.",
                "TaskGraph remains non-executable; no real planner implementation exists.",
                "All mock surfaces must not be reused as execution authority or LLM prompt source.",
                "Evidence refs from M519-M533 are identifiers only; content is not verified or persisted."
            ],
            EvidenceRefs =
            [
                "evidence-archive-review-m535-ref-only",
                "evidence-m519-m533-governance-baseline-ref"
            ],
            TimelineRefs =
            [
                "timeline-archive-review-created-m535",
                "timeline-m519-m533-closeout-archived"
            ],
            GuardrailRefs =
            [
                "guardrail-archive-as-governance-baseline-only",
                "guardrail-no-runtime-promotion-from-archive",
                "guardrail-no-llm-prompt-from-archive",
                "guardrail-no-filesystem-authority-from-archive"
            ],
            DraftOnly = true,
            CanAuthorizeExecution = false,
            CanCallPlanner = false,
            CanCallLlm = false,
            CanAccessFilesystem = false,
            CanCallCloud = false,
            CreatedAt = FixtureTime
        };
    }

    private static NodalOsAssignmentArchiveReviewMilestone Milestone(
        string id,
        string label,
        string decision,
        string complete,
        string remainsMock,
        string cannotPromote)
    {
        return new()
        {
            MilestoneId = id,
            LabelRedacted = label,
            DecisionRedacted = decision,
            CommitRef = null,
            ArtifactRefs = [$"artifact-{id.ToLower(System.Globalization.CultureInfo.InvariantCulture).Replace("+", "-")}-ref"],
            EvidenceRefs = [$"evidence-{id.ToLower(System.Globalization.CultureInfo.InvariantCulture).Replace("+", "-")}-ref-only"],
            TimelineRefs = [$"timeline-{id.ToLower(System.Globalization.CultureInfo.InvariantCulture).Replace("+", "-")}-closeout"],
            WhatIsCompleteRedacted = complete,
            WhatRemainsMockRedacted = remainsMock,
            CannotBePromotedToRuntimeRedacted = cannotPromote,
            AuditTriggersRedacted =
            [
                "Any future promotion to runtime.",
                "Any future LLM integration referencing these contracts.",
                "Any future filesystem access derived from these contracts."
            ]
        };
    }
}

public sealed class NodalOsAssignmentArchiveReviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsAssignmentArchiveReview review) =>
        JsonSerializer.Serialize(review, Options);
}

public sealed class NodalOsAssignmentArchiveReviewMarkdownRenderer
{
    public string Render(NodalOsAssignmentArchiveReview review)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# NODAL OS — Assignment Archive Review M535");
        sb.AppendLine();
        sb.AppendLine($"Archive Review ID: `{review.ArchiveReviewId}`");
        sb.AppendLine();
        sb.AppendLine("## Archive Status");
        sb.AppendLine();
        sb.AppendLine($"- CanArchiveAsGovernanceBaseline: **{review.ArchiveStatus.CanArchiveAsGovernanceBaseline}**");
        sb.AppendLine($"- CanUseAsRuntimeBaseline: **{review.ArchiveStatus.CanUseAsRuntimeBaseline}**");
        sb.AppendLine($"- CanUseAsPlannerImplementation: **{review.ArchiveStatus.CanUseAsPlannerImplementation}**");
        sb.AppendLine($"- CanUseAsLlmPromptSource: **{review.ArchiveStatus.CanUseAsLlmPromptSource}**");
        sb.AppendLine($"- CanUseAsFilesystemAuthority: **{review.ArchiveStatus.CanUseAsFilesystemAuthority}**");
        sb.AppendLine();
        sb.AppendLine("## Closed Milestones (M519–M533)");
        sb.AppendLine();
        foreach (var m in review.ClosedMilestones)
        {
            sb.AppendLine($"### {m.MilestoneId} — {m.LabelRedacted}");
            sb.AppendLine($"- Decision: `{m.DecisionRedacted}`");
            sb.AppendLine($"- Complete: {m.WhatIsCompleteRedacted}");
            sb.AppendLine($"- Remains mock: {m.WhatRemainsMockRedacted}");
            sb.AppendLine($"- Cannot promote: {m.CannotBePromotedToRuntimeRedacted}");
            sb.AppendLine();
        }
        sb.AppendLine("## Archive Warnings");
        sb.AppendLine();
        foreach (var w in review.ArchiveWarningsRedacted)
            sb.AppendLine($"- {w}");
        sb.AppendLine();
        sb.AppendLine("## Guardrails");
        sb.AppendLine();
        foreach (var g in review.GuardrailRefs)
            sb.AppendLine($"- `{g}`");
        return sb.ToString();
    }
}

public static class NodalOsAssignmentArchiveReviewFixtures
{
    public static NodalOsAssignmentArchiveReview ArchiveReview() =>
        new NodalOsAssignmentArchiveReviewService().CreateArchiveReview();
}
