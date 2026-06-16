using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsProductAdminPolishM148M150Tests
{
    [TestMethod]
    public void ProductAdminPolishShowsHito162StableAndNodalOsVisible()
    {
        var summary = new NodalOsProductAdminPolishService().BuildSummary();

        Assert.AreEqual("NODAL OS", summary.ProductName);
        StringAssert.Contains(summary.Hito162ReplacementStatus, "stable local fixture-first");
        Assert.IsTrue(summary.VisibleStates.Contains(NodalOsProductAdminPolishState.Hito162ReplacementStable));
    }

    [TestMethod]
    public void ProductAdminPolishKeepsExternalGeneralCdpAndRecorderReplayBlocked()
    {
        var summary = new NodalOsProductAdminPolishService().BuildSummary();

        Assert.IsFalse(summary.ExternalGeneralCdpReady);
        Assert.IsFalse(summary.RecorderReplayProductiveEnabled);
        Assert.IsTrue(summary.ActiveBlockers.Any(b => b.Contains("external CDP general-ready blocked", StringComparison.Ordinal)));
        Assert.IsTrue(summary.ActiveBlockers.Any(b => b.Contains("productive recorder/replay blocked", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ProductAdminPolishRequiresCoreAuthorityAndDoesNotDeclareProductionOrSaas()
    {
        var summary = new NodalOsProductAdminPolishService().BuildSummary();

        Assert.IsTrue(summary.CoreAuthorityRequired);
        Assert.IsFalse(summary.ProductionReady);
        Assert.IsFalse(summary.PublicSaasReady);
        Assert.IsTrue(summary.VisibleStates.Contains(NodalOsProductAdminPolishState.ActionAuthorityCoreOnly));
    }

    [TestMethod]
    public void OperatorUxDecisionClarityProvidesSafeNextActionAndBlockedCause()
    {
        var summary = new NodalOsOperatorUxDecisionClarityService().BuildSummary();

        Assert.AreEqual(NodalOsOperatorUxDecision.ReadyWithRestrictions, summary.CurrentDecision);
        StringAssert.Contains(summary.AllowedNextAction, "Continue internal local private preview");
        StringAssert.Contains(summary.BlockedNextAction, "Do not use production");
        StringAssert.Contains(summary.WhyBlocked, "approved local private preview scope");
    }

    [TestMethod]
    public void OperatorUxDecisionClarityShowsScopeInflationWarningIssuePathAndStopConditions()
    {
        var summary = new NodalOsOperatorUxDecisionClarityService().BuildSummary();

        Assert.IsTrue(summary.Warnings.Any(w => w.Contains("Scope inflation", StringComparison.Ordinal)));
        StringAssert.Contains(summary.IssueReportingPath, "local private preview issue report");
        Assert.IsTrue(summary.StopConditions.Any(s => s.Contains("credential/login", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void OperatorUxDecisionClarityDoesNotExposeSecretsCookiesTokens()
    {
        var json = JsonSerializer.Serialize(new NodalOsOperatorUxDecisionClarityService().BuildSummary());

        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-bearer-token", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OperatorUxDecisionClarityGuideExists()
    {
        var text = ReadDoc("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md");

        StringAssert.Contains(text, "ReadyWithRestrictions Is Not Production");
        StringAssert.Contains(text, "Reading M51 and M65");
        StringAssert.Contains(text, "Reading HITO-162 Replacement");
        StringAssert.Contains(text, "Stop Conditions");
    }

    [TestMethod]
    public void InternalPreviewIterationThirdRunRecordExists()
    {
        var text = ReadDoc("artifacts", "private-preview-runs", "m148-m150", "run-summary.json");

        StringAssert.Contains(text, "m148-m150-internal-preview-iteration-3");
        StringAssert.Contains(text, "ContinueInternalPreviewStable");
        StringAssert.Contains(text, "\"scopeExpanded\": false");
    }

    [TestMethod]
    public void InternalPreviewIterationModeledRunDoesNotExpandScopeAndKeepsBlockers()
    {
        var run = new NodalOsInternalPreviewIterationService().RunModeledIteration("test-commit");

        Assert.IsFalse(run.ScopeExpanded);
        Assert.IsFalse(run.ProofLiveExecuted);
        Assert.AreEqual(NodalOsOperatorUxDecision.ContinueInternalPreviewStable, run.Decision);
        Assert.IsTrue(run.BlockedFlowsObserved.Any(b => b.Contains("production/SaaS public blocked", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void InternalPreviewIterationNoHighCriticalIssuesMeansStable()
    {
        var decision = new NodalOsInternalPreviewIterationService().DecidePostRun([], scopeExpanded: false);

        Assert.AreEqual(NodalOsOperatorUxDecision.ContinueInternalPreviewStable, decision);
    }

    [TestMethod]
    public void InternalPreviewIterationUxIssueMeansMinorFixes()
    {
        var issue = new NodalOsPrivatePreviewIssue(
            "pp-ux-polish-001",
            NodalOsPrivatePreviewIssueCategory.BlockerExplanationWeak,
            NodalOsPrivatePreviewIssueSeverity.Low,
            NodalOsPrivatePreviewIssueDecision.ShouldFixSoon,
            "Operator text can be clearer.",
            BlocksPostRunGo: false,
            Redacted: true);

        var decision = new NodalOsInternalPreviewIterationService().DecidePostRun([issue], scopeExpanded: false);

        Assert.AreEqual(NodalOsOperatorUxDecision.NeedsOperatorUxFixes, decision);
    }

    [TestMethod]
    public void InternalPreviewIterationScopeInflationBlocks()
    {
        var decision = new NodalOsInternalPreviewIterationService().DecidePostRun([], scopeExpanded: true);

        Assert.AreEqual(NodalOsOperatorUxDecision.BlockedByScopeInflation, decision);
    }

    [TestMethod]
    public void InternalPreviewIterationReportsExist()
    {
        var run = ReadDoc("docs", "reports", "private-preview-run-m148-m150.md");
        var issues = ReadDoc("docs", "reports", "private-preview-issues-m148-m150.md");
        var adr = ReadDoc("docs", "adr", "private-preview-polish-review-m148-m150.md");

        StringAssert.Contains(run, "ContinueInternalPreviewStable");
        StringAssert.Contains(issues, "No issues found");
        StringAssert.Contains(adr, "ContinueInternalPreviewStable");
    }

    [TestMethod]
    public void ProductAdminPolishMaintainsNodalOsNaming()
    {
        var summary = new NodalOsProductAdminPolishService().BuildSummary();

        Assert.AreEqual("NODAL OS", summary.ProductName);
        Assert.IsFalse(JsonSerializer.Serialize(summary).Contains("Welcome to NEXA", StringComparison.Ordinal));
    }

    private static string ReadDoc(params string[] relativePath)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(new[] { root }.Concat(relativePath).ToArray());
        Assert.IsTrue(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
