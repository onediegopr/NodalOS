namespace OneBrain.Core.History;

public static class HistoryDemoFixture
{
    public static IReadOnlyList<RunHistoryRecord> CreateRunHistory()
    {
        return
        [
            new RunHistoryRecord(
                RunId: "run-demo-product-evidence-html",
                StartedAtUtc: "2026-06-12T11:00:00Z",
                EndedAtUtc: "2026-06-12T11:00:03Z",
                Status: RunHistoryStatuses.Succeeded,
                Source: RunHistorySources.Pilot,
                RecipeId: "demo-product-evidence-html-report",
                CandidateFlowId: null,
                ApprovalRequestId: null,
                ApprovalDecisionId: null,
                RecordingSessionId: null,
                TimelineId: null,
                ConfidenceId: "demo-product-evidence-confidence",
                AiRoutingDecisionId: "ai-audit-demo-intent",
                ExitCode: 0,
                SafetyCounters: RunSafetyCounters.Zero,
                ArtifactPaths:
                [
                    "artifacts/product-evidence-demo-summary/latest-product-evidence-summary.json",
                    "artifacts/product-evidence-demo-html-reports/latest-product-evidence-report.html"
                ],
                ErrorSummary: "",
                Notes: ["fixture run history; local artifacts only; no secrets stored"]),
            new RunHistoryRecord(
                RunId: "run-demo-approval-send-preview",
                StartedAtUtc: "2026-06-12T11:05:00Z",
                EndedAtUtc: "2026-06-12T11:05:01Z",
                Status: RunHistoryStatuses.Blocked,
                Source: RunHistorySources.Approval,
                RecipeId: null,
                CandidateFlowId: "whatsapp-browser-demo-flow",
                ApprovalRequestId: "approval-send-message-demo",
                ApprovalDecisionId: null,
                RecordingSessionId: "recording-demo-session",
                TimelineId: "timeline-demo-whatsapp",
                ConfidenceId: "confidence-whatsapp-browser-demo-flow",
                AiRoutingDecisionId: "ai-audit-critical-send-preview",
                ExitCode: null,
                SafetyCounters: RunSafetyCounters.Zero,
                ArtifactPaths: ["artifacts/approvals/latest-approval-request.json"],
                ErrorSummary: "ExecutionAllowed=false in v0; no safe executor attached.",
                Notes: ["sensitive send action requires human approval; decision records audit only"])
        ];
    }

    public static IReadOnlyList<AIAuditRecord> CreateAIAudit()
    {
        return
        [
            new AIAuditRecord(
                AiAuditId: "ai-audit-demo-intent",
                TimestampUtc: "2026-06-12T11:00:00Z",
                RecommendedProfileId: "OB_AI_CHEAP_INTENT",
                UsedProfileId: "OB_AI_CHEAP_INTENT",
                Provider: "mock",
                Model: "mock-cheap-intent",
                TaskType: "intent",
                RiskLevel: "low",
                RequiresVision: false,
                RequiresHumanApproval: false,
                FallbackUsed: false,
                FallbackFrom: null,
                FallbackTo: null,
                BudgetDecision: AIBudgetDecisions.Allowed,
                EstimatedCostUsd: 0.01m,
                ActualCostUsd: null,
                TokensIn: null,
                TokensOut: null,
                ResultStatus: AIAuditResultStatuses.Mocked,
                Reason: "Low-risk intent can route to cheap intent profile.",
                Error: ""),
            new AIAuditRecord(
                AiAuditId: "ai-audit-critical-send-preview",
                TimestampUtc: "2026-06-12T11:05:00Z",
                RecommendedProfileId: "OB_AI_CRITICAL_REASONER",
                UsedProfileId: null,
                Provider: "mock",
                Model: "mock-critical-reasoner",
                TaskType: "critical_reasoning",
                RiskLevel: "high",
                RequiresVision: false,
                RequiresHumanApproval: true,
                FallbackUsed: false,
                FallbackFrom: null,
                FallbackTo: null,
                BudgetDecision: AIBudgetDecisions.Blocked,
                EstimatedCostUsd: 0.20m,
                ActualCostUsd: null,
                TokensIn: null,
                TokensOut: null,
                ResultStatus: AIAuditResultStatuses.FailedClosed,
                Reason: "Send action is sensitive and remains approval-gated.",
                Error: "No safe executor attached; blocked before provider call.")
        ];
    }
}
