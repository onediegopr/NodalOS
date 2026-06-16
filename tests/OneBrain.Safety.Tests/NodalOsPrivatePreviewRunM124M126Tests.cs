using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsPrivatePreviewRunM124M126Tests
{
    [TestMethod]
    public void InternalLocalPrivatePreviewRunExecutesWithinAllowedScope()
    {
        var run = CreateRun();

        Assert.AreEqual(NodalOsInternalLocalPreviewRunDecision.ExecutedWithinScope, run.Decision);
        Assert.IsFalse(run.ProofLiveExecuted);
        Assert.IsFalse(run.OpenedBlockedSurface);
        Assert.IsTrue(run.AllowedFlowsExecuted.Any(flow => flow.Contains("Product/Admin", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void InternalLocalPrivatePreviewRunRecordExistsAndIsRedacted()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(SourcePath("artifacts", "private-preview-runs", "m124-m126", "run-summary.json")));
        var root = json.RootElement;

        Assert.AreEqual("NODAL OS", root.GetProperty("productName").GetString());
        Assert.AreEqual("ContinueWithMinorFixes", root.GetProperty("decision").GetString());
        Assert.IsFalse(root.GetProperty("proofLiveExecuted").GetBoolean());
        Assert.IsTrue(root.GetProperty("redacted").GetBoolean());
    }

    [TestMethod]
    public void InternalLocalPrivatePreviewRunReportExistsAndListsBlockedScope()
    {
        var text = File.ReadAllText(SourcePath("docs", "reports", "private-preview-run-m124-m126.md"));

        StringAssert.Contains(text, "NODAL OS");
        StringAssert.Contains(text, "ReadyWithRestrictions");
        StringAssert.Contains(text, "public SaaS");
        StringAssert.Contains(text, "What Was Not Tested");
    }

    [TestMethod]
    public void PrivatePreviewIssueCaptureClassifiesCriticalSecurityBlocker()
    {
        var issue = new NodalOsPrivatePreviewIssueCaptureService().Capture(
            "pp-sec-001",
            NodalOsPrivatePreviewIssueCategory.SecurityBlocker,
            NodalOsPrivatePreviewIssueSeverity.Critical,
            "secret/cookie/body leak detected");

        Assert.AreEqual(NodalOsPrivatePreviewIssueDecision.MustFixBeforeNextRun, issue.Decision);
        Assert.IsTrue(issue.BlocksPostRunGo);
    }

    [TestMethod]
    public void PrivatePreviewIssueCaptureDistinguishesUxFromScopeInflation()
    {
        var ux = new NodalOsPrivatePreviewIssueCaptureService().Capture("pp-ux", NodalOsPrivatePreviewIssueCategory.Ux, NodalOsPrivatePreviewIssueSeverity.Low, "copy issue");
        var scope = new NodalOsPrivatePreviewIssueCaptureService().Capture("pp-scope", NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk, NodalOsPrivatePreviewIssueSeverity.High, "external general inflated");

        Assert.AreEqual(NodalOsPrivatePreviewIssueDecision.AcceptForInternalOnly, ux.Decision);
        Assert.AreEqual(NodalOsPrivatePreviewIssueDecision.NeedsAudit, scope.Decision);
        Assert.IsFalse(ux.BlocksPostRunGo);
        Assert.IsTrue(scope.BlocksPostRunGo);
    }

    [TestMethod]
    public void PrivatePreviewIssueReportExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "reports", "private-preview-issues-m124-m126.md"));

        StringAssert.Contains(text, "pp-ux-001");
        StringAssert.Contains(text, "Low");
        StringAssert.Contains(text, "No Critical Findings");
    }

    [TestMethod]
    public void PrivatePreviewPostRunReviewNoIssuesContinuesInternalPreview()
    {
        var review = Review([]);

        Assert.AreEqual(NodalOsPrivatePreviewPostRunDecision.ContinueInternalPreview, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewPostRunReviewLowUxIssueContinuesWithMinorFixes()
    {
        var issue = new NodalOsPrivatePreviewIssueCaptureService().Capture("pp-ux-001", NodalOsPrivatePreviewIssueCategory.Ux, NodalOsPrivatePreviewIssueSeverity.Low, "copy issue");
        var review = Review([issue]);

        Assert.AreEqual(NodalOsPrivatePreviewPostRunDecision.ContinueWithMinorFixes, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewPostRunReviewCriticalSecurityIssueBlocks()
    {
        var issue = new NodalOsPrivatePreviewIssueCaptureService().Capture("pp-sec-001", NodalOsPrivatePreviewIssueCategory.SecurityBlocker, NodalOsPrivatePreviewIssueSeverity.Critical, "security blocker");
        var review = Review([issue]);

        Assert.AreEqual(NodalOsPrivatePreviewPostRunDecision.BlockedBySecurityIssue, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewPostRunReviewScopeInflationBlocks()
    {
        var issue = new NodalOsPrivatePreviewIssueCaptureService().Capture("pp-scope-001", NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk, NodalOsPrivatePreviewIssueSeverity.High, "scope inflation");
        var review = Review([issue]);

        Assert.AreEqual(NodalOsPrivatePreviewPostRunDecision.BlockedByScopeInflation, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewPostRunReviewMissingEvidenceNeedsMoreEvidence()
    {
        var review = new NodalOsPrivatePreviewPostRunReviewService().Review(
            CreateRun(),
            [],
            releaseGateReadyWithRestrictions: true,
            evidenceLogSummaryUsable: false,
            operatorRunbookUsable: true,
            issueTriageUsable: true,
            blockersVisibleAndEffective: true);

        Assert.AreEqual(NodalOsPrivatePreviewPostRunDecision.NeedsMoreEvidence, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewPostRunReviewReleaseGateMismatchBlocks()
    {
        var review = new NodalOsPrivatePreviewPostRunReviewService().Review(
            CreateRun(),
            [],
            releaseGateReadyWithRestrictions: false,
            evidenceLogSummaryUsable: true,
            operatorRunbookUsable: true,
            issueTriageUsable: true,
            blockersVisibleAndEffective: true);

        Assert.AreEqual(NodalOsPrivatePreviewPostRunDecision.BlockedByCriticalIssue, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewPostRunReviewAdrExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "private-preview-post-run-review-m124-m126.md"));

        StringAssert.Contains(text, "ContinueWithMinorFixes");
        StringAssert.Contains(text, "What Was Not Tested");
        StringAssert.Contains(text, "public SaaS");
    }

    private static NodalOsInternalLocalPreviewRunRecord CreateRun()
    {
        var gate = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(NodalOsRuntimeStateProbe.ForCurrentLocalPreview());
        var skipped = new NexaSkippedTestsCategoryAuditor().Audit(new NexaSkippedTestsAuditReporter().CreateReport());
        return new NodalOsInternalLocalPreviewRunService().Execute("c7cc1d46437f182cd19793af4e788d75ebb97687", gate, skipped);
    }

    private static NodalOsPrivatePreviewPostRunReview Review(IReadOnlyList<NodalOsPrivatePreviewIssue> issues) =>
        new NodalOsPrivatePreviewPostRunReviewService().Review(
            CreateRun(),
            issues,
            releaseGateReadyWithRestrictions: true,
            evidenceLogSummaryUsable: true,
            operatorRunbookUsable: true,
            issueTriageUsable: true,
            blockersVisibleAndEffective: true);

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
