using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("TimelineStepper")]
[TestCategory("VerticalTimeline")]
[TestCategory("SidePanelTimeline")]
[TestCategory("TimelineStabilization")]
[TestCategory("TimelineUxIssueCapture")]
[TestCategory("PrivatePreviewRunTimeline")]
[TestCategory("IssueTriageTimeline")]
public sealed class NodalOsTimelineStabilizationM160M162Tests
{
    private readonly NodalOsTimelineStabilizationReviewer reviewer = new();

    [TestMethod]
    public void TimelineInternalPreviewRunRecordCapturesReviewedSurfaces()
    {
        var run = reviewer.CreateDefaultRun("4338a2321d315ed44b7c3534415e712d5f2b9c32");

        Assert.AreEqual("m160-m162", run.RunId);
        Assert.IsTrue(run.TimelineSurfacesReviewed.Contains("task structuring"));
        Assert.IsTrue(run.TimelineSurfacesReviewed.Contains("recipe preview"));
        Assert.IsTrue(run.TimelineSurfacesReviewed.Contains("blocker explanation"));
        Assert.IsTrue(run.TimelineSurfacesReviewed.Contains("needs-human/human intervention"));
        Assert.IsFalse(run.ScopeExpanded);
        Assert.IsTrue(run.Redacted);
    }

    [TestMethod]
    public void TimelineUxIssueCaptureCategorizesBlockerAndEvidenceClarity()
    {
        var issues = new[]
        {
            Issue("tl-blocker-001", NodalOsTimelineUxIssueCategory.TimelineBlockerClarity, NodalOsTimelineUxIssueSeverity.Low, blocks: false),
            Issue("tl-evidence-001", NodalOsTimelineUxIssueCategory.TimelineEvidenceClarity, NodalOsTimelineUxIssueSeverity.Info, blocks: false)
        };

        var review = reviewer.Review(issues);

        Assert.AreEqual(NodalOsTimelineStabilizationDecision.TimelineStableForInternalPreview, review.Decision);
        Assert.IsTrue(review.Issues.Any(i => i.Category == NodalOsTimelineUxIssueCategory.TimelineBlockerClarity));
        Assert.IsTrue(review.Issues.Any(i => i.Category == NodalOsTimelineUxIssueCategory.TimelineEvidenceClarity));
    }

    [TestMethod]
    public void TimelineScopeInflationRiskBlocksStabilization()
    {
        var review = reviewer.Review([
            Issue("tl-scope-001", NodalOsTimelineUxIssueCategory.TimelineScopeInflationRisk, NodalOsTimelineUxIssueSeverity.High, blocks: true)
        ]);

        Assert.AreEqual(NodalOsTimelineStabilizationDecision.TimelineBlockedByScopeInflation, review.Decision);
    }

    [TestMethod]
    public void TimelineSecurityLeakRiskBlocksStabilization()
    {
        var review = reviewer.Review([
            Issue("tl-leak-001", NodalOsTimelineUxIssueCategory.TimelineSecurityLeakRisk, NodalOsTimelineUxIssueSeverity.Critical, blocks: true, summary: "token=synthetic-secret-value")
        ]);

        Assert.AreEqual(NodalOsTimelineStabilizationDecision.TimelineBlockedBySecurityLeak, review.Decision);
        var json = JsonSerializer.Serialize(review);
        Assert.IsFalse(json.Contains("synthetic-secret-value", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TimelineLowReadabilityIssueDoesNotBlock()
    {
        var review = reviewer.Review([
            Issue("tl-readability-001", NodalOsTimelineUxIssueCategory.TimelineReadability, NodalOsTimelineUxIssueSeverity.Low, blocks: false)
        ]);

        Assert.AreEqual(NodalOsTimelineStabilizationDecision.TimelineContinueWithMinorFixes, review.Decision);
    }

    [TestMethod]
    public void TimelineStableIfNoBlockingIssuesAndCoreAuthorityVisible()
    {
        var review = reviewer.Review([]);

        Assert.AreEqual(NodalOsTimelineStabilizationDecision.TimelineStableForInternalPreview, review.Decision);
        Assert.IsTrue(review.BlockersVisible);
        Assert.IsTrue(review.EvidenceRedacted);
        Assert.IsTrue(review.NeedsHumanClear);
        Assert.IsTrue(review.CoreAuthorityVisible);
        Assert.IsFalse(review.UiAuthorizesActions);
        Assert.IsFalse(review.ScopeExpanded);
    }

    [TestMethod]
    public void TimelineBlockedIfUiAuthorizesActionsOrScopeExpands()
    {
        var uiAuthority = reviewer.Review([], uiAuthorizesActions: true);
        var scopeExpanded = reviewer.Review([], scopeExpanded: true);

        Assert.AreEqual(NodalOsTimelineStabilizationDecision.TimelineBlockedByScopeInflation, uiAuthority.Decision);
        Assert.AreEqual(NodalOsTimelineStabilizationDecision.TimelineBlockedByScopeInflation, scopeExpanded.Decision);
    }

    [TestMethod]
    public void TimelineRunRecordAndReportsExist()
    {
        var runJson = File.ReadAllText(SourcePath("artifacts", "private-preview-runs", "m160-m162", "run-summary.json"));
        var runReport = File.ReadAllText(SourcePath("docs", "reports", "private-preview-run-m160-m162.md"));
        var issueReport = File.ReadAllText(SourcePath("docs", "reports", "private-preview-issues-m160-m162.md"));

        StringAssert.Contains(runJson, "TimelineStableForInternalPreview");
        StringAssert.Contains(runJson, "\"scopeExpanded\": false");
        StringAssert.Contains(runReport, "Timeline Surfaces Reviewed");
        StringAssert.Contains(issueReport, "TimelineSecurityLeakRisk");
        StringAssert.Contains(issueReport, "TimelineScopeInflationRisk");
    }

    [TestMethod]
    public void TimelineStabilizationAdrExistsAndPreservesBlockers()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "timeline-ui-stabilization-review-m160-m162.md"));

        StringAssert.Contains(adr, "TimelineStableForInternalPreview");
        StringAssert.Contains(adr, "does not authorize actions");
        StringAssert.Contains(adr, "Core remains authoritative");
        StringAssert.Contains(adr, "Production/SaaS public blocked");
        StringAssert.Contains(adr, "External CDP general-ready blocked");
    }

    private static NodalOsTimelineUxIssue Issue(
        string id,
        NodalOsTimelineUxIssueCategory category,
        NodalOsTimelineUxIssueSeverity severity,
        bool blocks,
        string summary = "redacted timeline issue") =>
        new(
            id,
            category,
            severity,
            blocks ? NodalOsTimelineUxIssueDecision.MustFixBeforeNextRun : NodalOsTimelineUxIssueDecision.AcceptForInternalOnly,
            summary,
            blocks,
            Redacted: true);

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
