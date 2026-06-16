using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsPrivatePreviewStabilizationM127M129Tests
{
    [TestMethod]
    public void PrivatePreviewStabilizationReviewLowIssueResolvedIsStable()
    {
        var review = new NodalOsPrivatePreviewStabilizationReviewService().Review(
            "pp-ux-001 Fixed VerifiedInSecondRun",
            [],
            activeBlockersRemainTrue: true,
            scopeExpanded: false,
            productAdminReady: true);

        Assert.AreEqual(NodalOsPrivatePreviewStabilizationDecision.ContinueInternalPreviewStable, review.Decision);
        Assert.IsTrue(review.ActiveBlockersRemainTrue);
    }

    [TestMethod]
    public void PrivatePreviewStabilizationReviewHighSecurityIssueBlocks()
    {
        var issue = new NodalOsPrivatePreviewIssueCaptureService().Capture(
            "pp-sec-001",
            NodalOsPrivatePreviewIssueCategory.SecurityBlocker,
            NodalOsPrivatePreviewIssueSeverity.High,
            "security issue");
        var review = new NodalOsPrivatePreviewStabilizationReviewService().Review(
            "pp-ux-001 Fixed VerifiedInSecondRun",
            [issue],
            activeBlockersRemainTrue: true,
            scopeExpanded: false,
            productAdminReady: true);

        Assert.AreEqual(NodalOsPrivatePreviewStabilizationDecision.BlockedBySecurityIssue, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewStabilizationReviewScopeInflationBlocks()
    {
        var review = new NodalOsPrivatePreviewStabilizationReviewService().Review(
            "pp-ux-001 Fixed VerifiedInSecondRun",
            [],
            activeBlockersRemainTrue: true,
            scopeExpanded: true,
            productAdminReady: true);

        Assert.AreEqual(NodalOsPrivatePreviewStabilizationDecision.BlockedByScopeInflation, review.Decision);
    }

    [TestMethod]
    public void PrivatePreviewStabilizationReviewActiveBlockersRemainTrue()
    {
        var runText = File.ReadAllText(SourcePath("docs", "reports", "private-preview-run-m127-m129.md"));

        StringAssert.Contains(runText, "public SaaS");
        StringAssert.Contains(runText, "real credentials");
        StringAssert.Contains(runText, "external CDP general-ready");
        StringAssert.Contains(runText, "new external targets without dedicated evidence");
    }

    [TestMethod]
    public void PrivatePreviewStabilizationReviewAdrExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "private-preview-stabilization-review-m127-m129.md"));

        StringAssert.Contains(text, "ContinueInternalPreviewStable");
        StringAssert.Contains(text, "pp-ux-001");
        StringAssert.Contains(text, "No scope expansion");
        StringAssert.Contains(text, "HITO-162");
    }

    [TestMethod]
    public void PrivatePreviewIssuePpUx001IsFixedAndVerified()
    {
        var issueText = File.ReadAllText(SourcePath("docs", "reports", "private-preview-issues-m124-m126.md"));
        var secondIssueText = File.ReadAllText(SourcePath("docs", "reports", "private-preview-issues-m127-m129.md"));

        StringAssert.Contains(issueText, "Fixed / VerifiedInSecondRun");
        StringAssert.Contains(secondIssueText, "Fixed");
        StringAssert.Contains(secondIssueText, "VerifiedInSecondRun");
    }

    [TestMethod]
    public void SecondRunRecordExistsAndNoNewIssues()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(SourcePath("artifacts", "private-preview-runs", "m127-m129", "run-summary.json")));
        var root = json.RootElement;

        Assert.AreEqual("NODAL OS", root.GetProperty("productName").GetString());
        Assert.AreEqual("ContinueInternalPreviewStable", root.GetProperty("decision").GetString());
        Assert.AreEqual(0, root.GetProperty("newIssues").GetArrayLength());
        Assert.IsFalse(root.GetProperty("proofLiveExecuted").GetBoolean());
        Assert.IsFalse(root.GetProperty("openedBlockedSurface").GetBoolean());
    }

    [TestMethod]
    public void OperatorUxReadinessClarifiesTargetOwnedProofIsNotGeneralReady()
    {
        var summary = new NodalOsOperatorUxReadinessService().BuildDefaultSummary();
        var combined = string.Join(" ", summary.EvidenceSummary.Concat([summary.LastProofSummary]));

        StringAssert.Contains(combined, "target-owned");
        StringAssert.Contains(combined, "external general-ready remains false");
    }

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
