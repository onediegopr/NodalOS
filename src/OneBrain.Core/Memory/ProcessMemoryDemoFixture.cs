namespace OneBrain.Core.Memory;

public static class ProcessMemoryDemoFixture
{
    public static IReadOnlyList<ProcessMemoryEntry> CreateEntries()
    {
        return
        [
            CreateDemoReportEntry(),
            CreateWhatsAppCandidateEntry(),
            CreateArchivedEntry()
        ];
    }

    public static ProcessMemoryEntry CreateDemoReportEntry()
    {
        return new ProcessMemoryEntry(
            Id: "process-demo-product-evidence-report",
            Title: "Generate stable product evidence demo report",
            Description: "Runs local sample product evidence summary and Markdown/HTML reports without live web access.",
            Source: ProcessMemorySources.Recipe,
            Status: ProcessMemoryStatuses.Stable,
            AppOrSite: "ONE BRAIN Pilot",
            Domain: "local-demo",
            Tags: ["demo", "product-evidence", "report", "html", "markdown"],
            RiskLevel: "low",
            ConfidenceScore: 92,
            CreatedAtUtc: "2026-06-12T12:00:00Z",
            UpdatedAtUtc: "2026-06-12T12:10:00Z",
            LastUsedAtUtc: "2026-06-12T12:15:00Z",
            Summary: new ProcessMemorySummary(
                "Stable local demo creates auditable product evidence reports.",
                ["Load samples/product-evidence", "Build summary", "Write Markdown and HTML reports"],
                ["Runtime outputs remain under artifacts/"],
                ["Safe to suggest for demo/report tasks"]),
            Links: new ProcessMemoryLink(
                RecordingSessionId: null,
                TimelineId: null,
                CandidateFlowId: null,
                RecipeDraftId: null,
                RecipeId: "demo-product-evidence-report",
                ApprovalRequestId: null,
                ApprovalDecisionId: null,
                RunId: "run-demo-product-evidence-html",
                AiAuditId: "ai-audit-demo-intent",
                ConfidenceId: "demo-product-evidence-confidence"),
            Decisions: [],
            Errors: [],
            EvidenceLinks:
            [
                new ProcessMemoryEvidenceLink("sample", "samples/product-evidence", "versioned sample artifacts"),
                new ProcessMemoryEvidenceLink("report", "artifacts/product-evidence-demo-html-reports/latest-product-evidence-report.html", "runtime HTML report")
            ],
            ArtifactPaths: ["artifacts/product-evidence-demo-reports/latest-product-evidence-report.md"],
            Notes: ["retrieval fixture; no secrets stored; local artifacts only"]);
    }

    public static ProcessMemoryEntry CreateWhatsAppCandidateEntry()
    {
        return new ProcessMemoryEntry(
            Id: "process-whatsapp-send-preview-candidate",
            Title: "Prepare WhatsApp/browser message preview",
            Description: "Observed candidate flow prepares a message preview and stops before send.",
            Source: ProcessMemorySources.Timeline,
            Status: ProcessMemoryStatuses.Candidate,
            AppOrSite: "WhatsApp/browser fixture",
            Domain: "messaging",
            Tags: ["whatsapp", "message", "approval", "candidate"],
            RiskLevel: "critical",
            ConfidenceScore: 45,
            CreatedAtUtc: "2026-06-12T12:20:00Z",
            UpdatedAtUtc: "2026-06-12T12:25:00Z",
            LastUsedAtUtc: null,
            Summary: new ProcessMemorySummary(
                "Candidate flow requires human approval and has no executor.",
                ["Find customer", "Prepare message", "Stop before send"],
                ["send requires approval", "no safe executor attached"],
                ["Keep as supervised candidate"]),
            Links: new ProcessMemoryLink(
                RecordingSessionId: "demo-shadow-recording",
                TimelineId: "timeline-demo-whatsapp",
                CandidateFlowId: "candidate-whatsapp-browser-demo-v0",
                RecipeDraftId: null,
                RecipeId: null,
                ApprovalRequestId: "approval-send-message-demo",
                ApprovalDecisionId: null,
                RunId: "run-demo-approval-send-preview",
                AiAuditId: "ai-audit-critical-send-preview",
                ConfidenceId: "confidence-whatsapp-browser-demo-flow"),
            Decisions:
            [
                new ProcessMemoryDecision("decision-preview-only", "2026-06-12T12:26:00Z", "Execution remains blocked", "send is sensitive", "approval_required")
            ],
            Errors:
            [
                new ProcessMemoryError("no_safe_executor", "ExecutionAllowed=false in v0.", "2026-06-12T12:26:00Z")
            ],
            EvidenceLinks:
            [
                new ProcessMemoryEvidenceLink("timeline", "artifacts/recipe-timelines/latest-whatsapp-demo-timeline.json", "candidate timeline")
            ],
            ArtifactPaths: ["artifacts/approvals/latest-approval-request.json"],
            Notes: ["candidate memory; retrieval can suggest only with human review"]);
    }

    private static ProcessMemoryEntry CreateArchivedEntry()
    {
        return CreateDemoReportEntry() with
        {
            Id = "process-archived-unsafe-demo",
            Title = "Archived unsafe experiment",
            Status = ProcessMemoryStatuses.Archived,
            RiskLevel = "high",
            ConfidenceScore = 5,
            Tags = ["archived", "unsafe"],
            Notes = ["not safe to suggest"]
        };
    }
}
