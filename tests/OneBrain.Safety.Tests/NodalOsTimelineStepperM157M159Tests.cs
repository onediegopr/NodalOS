using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("TimelineStepper")]
[TestCategory("VerticalTimeline")]
[TestCategory("SidePanelTimeline")]
[TestCategory("TaskStructuringTimeline")]
[TestCategory("RecipeTimeline")]
[TestCategory("OperatorTimeline")]
[TestCategory("EvidenceTimeline")]
[TestCategory("BlockerTimeline")]
[TestCategory("PrivatePreviewRunTimeline")]
[TestCategory("IssueTriageTimeline")]
public sealed class NodalOsTimelineStepperM157M159Tests
{
    private readonly NodalOsTimelineAdapter adapter = new();

    [TestMethod]
    public void TimelineStepperTaskStructuringRepresentsUserTask()
    {
        var timeline = adapter.UserTaskToTimeline("Review local diagnostics without credentials.");

        Assert.AreEqual("NODAL OS", timeline.ProductName);
        Assert.IsTrue(timeline.Steps.Any(s => s.Node.NodeType == NodalOsTimelineNodeType.UserRequest));
        Assert.IsTrue(timeline.Steps.Any(s => s.Node.NodeType == NodalOsTimelineNodeType.StructuredTask));
        Assert.IsTrue(timeline.Steps.Any(s => s.SubSteps.Count > 0));
        Assert.IsFalse(timeline.GrantsAuthority);
        Assert.IsFalse(timeline.ProductionReady);
    }

    [TestMethod]
    public void RecipeTimelineRepresentsSubtasksAndCoreBoundary()
    {
        var timeline = adapter.RecipeToTimeline(new NodalOsRecipeTimelineInput(
            "recipe-local",
            "Local diagnostics review",
            ["Open readiness", "Review evidence", "Capture local issue"]));

        Assert.AreEqual(3, timeline.Steps.Count);
        Assert.IsTrue(timeline.Steps.All(s => s.Node.NodeType == NodalOsTimelineNodeType.RecipeStep));
        Assert.IsTrue(timeline.Steps.All(s => s.CoreAuthorityRequired));
        Assert.IsTrue(timeline.Steps.All(s => !s.GrantsAuthority));
    }

    [TestMethod]
    public void ExecutionTimelineShowsBlockedStepWithReason()
    {
        var timeline = adapter.ExecutionToTimeline(new NodalOsExecutionTimelineInput(
            "run-1",
            [
                new("Open local panel", "passed", null),
                new("Try submit", "blocked", "submit/pay/sign/delete remains blocked")
            ]));

        var blocked = timeline.Steps.Single(s => s.Status == NodalOsTimelineStepStatus.Blocked);
        Assert.AreEqual(NodalOsTimelineNodeType.BlockerStep, blocked.Node.NodeType);
        Assert.IsTrue(blocked.Blockers.Any());
        Assert.IsTrue(blocked.Decision.BlockedOptions.Any(o => o.Contains("submit/pay/sign/delete", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void EvidenceTimelineContainsRedactedRefsOnly()
    {
        var timeline = adapter.EvidenceToTimeline(new NodalOsEvidenceTimelineInput(
            "evidence-summary",
            ["ledger:m51:redacted", "ledger:m65:redacted"]));

        Assert.IsTrue(timeline.Steps.All(s => s.Status == NodalOsTimelineStepStatus.EvidenceReady));
        Assert.IsTrue(timeline.Steps.SelectMany(s => s.EvidenceRefs).All(e => e.Redacted));
        AssertTimelineDoesNotLeak(timeline);
    }

    [TestMethod]
    public void BlockerTimelineShowsNeedsHumanAndBlockedOptions()
    {
        var timeline = adapter.BlockerToTimeline(new NodalOsBlockerTimelineInput(
            "credential prompt detected",
            "Human must stop; do not enter real credentials in preview.",
            ["credentials", "login", "submit"],
            NeedsHuman: true));

        var step = timeline.Steps.Single();
        Assert.AreEqual(NodalOsTimelineStepStatus.NeedsHuman, step.Status);
        Assert.IsTrue(step.HumanInterventionRequired);
        Assert.IsTrue(step.Blockers.Single().BlockedOptions.Contains("credentials"));
    }

    [TestMethod]
    public void OperatorTimelineCanRenderSummaryWithoutScopeExpansion()
    {
        var timeline = adapter.OperatorSummaryToTimeline(new NodalOsOperatorSummaryTimelineInput(
            "ReadyWithRestrictions",
            "Continue internal local preview only.",
            ["public SaaS blocked", "external CDP general-ready false"],
            ["release-gate:verified:redacted"]));

        Assert.IsTrue(timeline.ReadyWithRestrictions);
        Assert.IsFalse(timeline.ProductionReady);
        Assert.IsTrue(timeline.Steps.Any(s => s.Blockers.Count > 0));
        Assert.IsTrue(timeline.Steps.All(s => !s.GrantsAuthority));
    }

    [TestMethod]
    public void PrivatePreviewRunTimelineShowsAllowedAndDeniedScope()
    {
        var timeline = adapter.PrivatePreviewRunToTimeline(new NodalOsPrivatePreviewRunTimelineInput(
            "preview-run-3",
            "ContinueInternalPreviewStable",
            ["Product/Admin local", "redacted evidence review"],
            ["production/SaaS", "real credentials", "submit/pay/sign/delete"],
            ["run-summary:redacted"]));

        Assert.IsTrue(timeline.Steps.Any(s => s.Title.Contains("Allowed", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(timeline.Steps.Any(s => s.Status == NodalOsTimelineStepStatus.Blocked));
        Assert.IsTrue(timeline.Steps.SelectMany(s => s.Blockers).Any(b => b.Reason.Contains("production", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void IssueTriageTimelineBlocksBlockingIssues()
    {
        var timeline = adapter.IssueTriageToTimeline(new NodalOsIssueTriageTimelineInput(
            "pp-scope-001",
            "High",
            "ScopeInflationRisk",
            "MustFixBeforeNextRun",
            BlocksRun: true));

        Assert.AreEqual(NodalOsTimelineStepStatus.Blocked, timeline.Steps.Single().Status);
        Assert.IsTrue(timeline.Steps.Single().Blockers.Any());
    }

    [TestMethod]
    public void TimelineEvidenceDoesNotContainSecretsCookiesOrTokens()
    {
        var timeline = adapter.UserTaskToTimeline("token=synthetic-secret-value cookie=session-abc password=abc123");
        var json = JsonSerializer.Serialize(timeline);

        Assert.IsTrue(timeline.Redacted);
        Assert.IsFalse(json.Contains("synthetic-secret-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("session-abc", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("password=abc123", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TimelineReadyWithRestrictionsIsNotProduction()
    {
        var timeline = adapter.UserTaskToTimeline("Start a local private preview task.");

        Assert.IsTrue(timeline.ReadyWithRestrictions);
        Assert.IsFalse(timeline.ProductionReady);
        Assert.IsTrue(timeline.Steps.All(s => s.StatusCard.ReadyWithRestrictions));
        Assert.IsTrue(timeline.Steps.All(s => !s.StatusCard.ProductionReady));
    }

    [TestMethod]
    public void SidePanelTimelineRendererExistsWithVerticalStepperClasses()
    {
        var js = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.js"));
        var css = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.css"));
        var html = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.html"));

        StringAssert.Contains(js, "function renderTimelineStep");
        StringAssert.Contains(js, "buildStructuredTaskTimeline");
        StringAssert.Contains(js, "buildRecipeRunTimeline");
        StringAssert.Contains(css, ".timeline-node-dot");
        StringAssert.Contains(css, ".timeline-card");
        StringAssert.Contains(html, "NODAL OS");
    }

    [TestMethod]
    public void SidePanelTimelineRendererShowsBlockersEvidenceAndHumanStates()
    {
        var js = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.js"));

        StringAssert.Contains(js, "timeline-blocker-card");
        StringAssert.Contains(js, "timeline-evidence");
        StringAssert.Contains(js, "Human intervention");
        StringAssert.Contains(js, "Core authority required");
        StringAssert.Contains(js, "ReadyWithRestrictions no significa produccion");
    }

    [TestMethod]
    public void TimelineAdrAndOperatorGuideExist()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "vertical-timeline-stepper-ui-m157-m159.md"));
        var guide = File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md"));

        StringAssert.Contains(adr, "Vertical Timeline");
        StringAssert.Contains(adr, "does not authorize actions");
        StringAssert.Contains(adr, "ReadyWithRestrictions");
        StringAssert.Contains(guide, "Reading the Vertical Timeline / Stepper");
        StringAssert.Contains(guide, "Timeline output never authorizes");
    }

    private static void AssertTimelineDoesNotLeak(NodalOsTimeline timeline)
    {
        var json = JsonSerializer.Serialize(timeline);
        Assert.IsFalse(json.Contains("cookie=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("token=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("password=", StringComparison.OrdinalIgnoreCase));
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
