using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivatePreviewFeedbackM55Tests
{
    [TestMethod]
    public void NexaPrivatePreviewFeedbackCreatesRedactedIssueReport()
    {
        var decision = Evaluate(Feedback());

        Assert.IsTrue(decision.Allowed);
        Assert.IsNotNull(decision.IssueReport);
        Assert.IsTrue(decision.IssueReport.Redacted);
    }

    [TestMethod]
    public void NexaPrivatePreviewFeedbackRejectsSecretLeak()
    {
        var decision = Evaluate(Feedback() with { ReproSummaryRedacted = "token=opaque-token-value-123456789" });

        Assert.AreEqual(NexaPrivatePreviewFeedbackDecisionKind.Rejected, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "feedback contains secret-like content");
    }

    [TestMethod]
    public void NexaPrivatePreviewFeedbackRequiresAuthorizedActor()
    {
        var decision = Evaluate(Feedback(), actorAuthorized: false);

        Assert.AreEqual(NexaPrivatePreviewFeedbackDecisionKind.Rejected, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "actor unauthorized");
    }

    [TestMethod]
    public void NexaPrivatePreviewFeedbackBlocksCrossTenantAccess()
    {
        var decision = new NexaPrivatePreviewFeedbackEvaluator().Evaluate(Feedback(), Tenant("tenant-a"), Tenant("tenant-b"), actorAuthorized: true, diagnosticsRedacted: true, auditExportRedacted: true);

        Assert.AreEqual(NexaPrivatePreviewFeedbackDecisionKind.Rejected, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "cross-tenant feedback access blocked");
    }

    [TestMethod]
    public void NexaPrivatePreviewFeedbackAttachesDiagnosticsRefsOnly()
    {
        var report = Evaluate(Feedback()).IssueReport!;

        Assert.IsTrue(report.Feedback.DiagnosticsRefs.All(reference => reference.StartsWith("diag-ref-", StringComparison.Ordinal)));
        Assert.IsFalse(report.Feedback.DiagnosticsRefs.Any(BrowserCredentialRedactor.ContainsSecret));
    }

    [TestMethod]
    public void NexaPrivatePreviewFeedbackAttachesAuditRefsOnly()
    {
        var report = Evaluate(Feedback()).IssueReport!;

        Assert.IsTrue(report.Feedback.AuditRefs.All(reference => reference.StartsWith("audit-ref-", StringComparison.Ordinal)));
        Assert.IsFalse(report.Feedback.AuditRefs.Any(BrowserCredentialRedactor.ContainsSecret));
    }

    [TestMethod]
    public void NexaPrivatePreviewAuditReviewMarksSecurityBlocker()
    {
        var review = new NexaPrivatePreviewFeedbackEvaluator().ReviewAudit("preview-session", ["audit-ref-1"], auditExportRedacted: false, leakDetected: true);

        Assert.IsTrue(review.SecurityBlocker);
    }

    [TestMethod]
    public void NexaPrivatePreviewSessionSummaryDoesNotExposeSecrets()
    {
        var issue = Evaluate(Feedback()).IssueReport!;
        var summary = new NexaPrivatePreviewFeedbackEvaluator().CreateSummary("preview-session", "tenant-local", "workspace-local", [issue]);
        var json = NexaLeakHardeningSerialization.ToSafeJson(summary);

        Assert.IsTrue(summary.Redacted);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(json));
    }

    private static NexaPrivatePreviewFeedbackDecision Evaluate(NexaPrivatePreviewFeedback feedback, bool actorAuthorized = true) =>
        new NexaPrivatePreviewFeedbackEvaluator().Evaluate(feedback, Tenant("tenant-local"), Tenant("tenant-local"), actorAuthorized, diagnosticsRedacted: true, auditExportRedacted: true);

    private static NexaPrivatePreviewFeedback Feedback() =>
        new(
            "feedback-local-1",
            "preview-session",
            "owner-local",
            NexaRole.Owner,
            "tenant-local",
            "workspace-local",
            "diagnostics",
            NexaPrivatePreviewFeedbackSeverity.Medium,
            "redacted reproduction summary",
            ["diag-ref-redacted"],
            ["audit-ref-redacted"],
            Redacted: true);

    private static NexaPublicApiTenantContext Tenant(string tenantId) =>
        new(tenantId, "account-local", "org-local", "workspace-local", "worker-local");
}
