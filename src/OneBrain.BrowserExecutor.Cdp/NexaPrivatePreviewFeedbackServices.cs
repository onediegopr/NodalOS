using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPrivatePreviewFeedbackEvaluator
{
    public NexaPrivatePreviewFeedbackDecision Evaluate(
        NexaPrivatePreviewFeedback feedback,
        NexaPublicApiTenantContext actorTenant,
        NexaPublicApiTenantContext targetTenant,
        bool actorAuthorized,
        bool diagnosticsRedacted,
        bool auditExportRedacted)
    {
        var reasons = new List<string>();
        if (!actorAuthorized || feedback.ActorRole is NexaRole.Worker or NexaRole.Viewer)
            reasons.Add("actor unauthorized");
        if (!SameTenant(actorTenant, targetTenant) ||
            !string.Equals(feedback.TenantId, targetTenant.TenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(feedback.WorkspaceId, targetTenant.WorkspaceId, StringComparison.OrdinalIgnoreCase))
            reasons.Add("cross-tenant feedback access blocked");
        if (!feedback.Redacted || BrowserCredentialRedactor.ContainsSecret(feedback.ReproSummaryRedacted))
            reasons.Add("feedback contains secret-like content");
        if (!diagnosticsRedacted)
            reasons.Add("diagnostics attachment is not redacted");
        if (!auditExportRedacted)
            reasons.Add("audit export attachment is not redacted");

        if (reasons.Count > 0)
            return new NexaPrivatePreviewFeedbackDecision(NexaPrivatePreviewFeedbackDecisionKind.Rejected, reasons, null, Redacted: true);

        var blocker = feedback.Severity is NexaPrivatePreviewFeedbackSeverity.High or NexaPrivatePreviewFeedbackSeverity.Critical;
        var decision = feedback.Severity == NexaPrivatePreviewFeedbackSeverity.Critical
            ? NexaPrivatePreviewFeedbackDecisionKind.SecurityBlocker
            : blocker ? NexaPrivatePreviewFeedbackDecisionKind.ProductBlocker : NexaPrivatePreviewFeedbackDecisionKind.Accepted;
        var issue = new NexaPrivatePreviewIssueReport(
            $"preview-issue-{Guid.NewGuid():N}",
            feedback,
            blocker,
            blocker ? "Triage before next local private preview run." : "Track for next local preview iteration.",
            ContainsSecret: false,
            ContainsCookie: false,
            ContainsBody: false,
            Redacted: true);
        return new NexaPrivatePreviewFeedbackDecision(decision, [decision.ToString()], issue, Redacted: true);
    }

    public NexaPrivatePreviewSessionSummary CreateSummary(string sessionId, string tenantId, string workspaceId, IReadOnlyList<NexaPrivatePreviewIssueReport> issues) =>
        new(sessionId, tenantId, workspaceId, issues.Count, issues.Count(issue => issue.Blocker), issues.Any(issue => issue.Blocker) ? "Resolve blockers before widening preview." : "Continue local private preview.", Redacted: true);

    public NexaPrivatePreviewAuditReview ReviewAudit(string sessionId, IReadOnlyList<string> auditRefs, bool auditExportRedacted, bool leakDetected) =>
        new($"preview-audit-review-{Guid.NewGuid():N}", sessionId, auditRefs, auditExportRedacted, leakDetected, leakDetected || !auditExportRedacted, leakDetected ? "security blocker detected" : "audit export redacted");

    private static bool SameTenant(NexaPublicApiTenantContext actor, NexaPublicApiTenantContext target) =>
        string.Equals(actor.TenantId, target.TenantId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(actor.AccountId, target.AccountId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(actor.OrganizationId, target.OrganizationId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(actor.WorkspaceId, target.WorkspaceId, StringComparison.OrdinalIgnoreCase);
}
