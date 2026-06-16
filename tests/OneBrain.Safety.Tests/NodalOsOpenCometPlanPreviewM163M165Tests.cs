using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OpenCometLessonsDecision")]
[TestCategory("SidepanelOperatorCabIntegration")]
[TestCategory("PlanPreviewContract")]
[TestCategory("PlanPreviewTimelineAdapter")]
[TestCategory("TimelineStepper")]
[TestCategory("VerticalTimeline")]
[TestCategory("SidePanelTimeline")]
public sealed class NodalOsOpenCometPlanPreviewM163M165Tests
{
    private readonly NodalOsExecutionPlanPreviewService service = new();
    private readonly NodalOsPlanPreviewToTimelineAdapter timelineAdapter = new();

    [TestMethod]
    public void OpenCometLessonsDecisionRecordExistsAndPreservesArchitecture()
    {
        var text = File.ReadAllText(SourcePath("docs", "architecture", "opencomet-lessons-import-decision-m163-m165.md"));

        StringAssert.Contains(text, "No OpenCometAI fork");
        StringAssert.Contains(text, "No OpenCometAI dependency");
        StringAssert.Contains(text, "Core remains the authority");
        StringAssert.Contains(text, "UI/Admin/Companion remain non-authoritative");
        StringAssert.Contains(text, "Chrome/CDP remains the primary runtime");
        StringAssert.Contains(text, "Do not replace `renderTimeline`");
        StringAssert.Contains(text, "No research mode now");
    }

    [TestMethod]
    public void SidepanelOperatorCabMapIdentifiesExistingTimelineWithoutDuplication()
    {
        var text = File.ReadAllText(SourcePath("docs", "ui", "sidepanel-operator-cabinet-integration-map-m163-m165.md"));

        StringAssert.Contains(text, "sidepanel.js");
        StringAssert.Contains(text, "renderTimeline(node, items)");
        StringAssert.Contains(text, "buildStructuredTaskTimeline(goal)");
        StringAssert.Contains(text, ".nodal-timeline");
        StringAssert.Contains(text, "Do not create another timeline component");
        StringAssert.Contains(text, "Plan preview");
        StringAssert.Contains(text, "NodalOsPlanPreviewToTimelineAdapter");
    }

    [TestMethod]
    public void PlanPreviewCanCreateDraft()
    {
        var preview = service.Draft("plan-1", "Review local readiness", ["Open dashboard", "Review redacted evidence"]);

        Assert.AreEqual(NodalOsPlanPreviewStatus.PlanDrafted, preview.Status);
        Assert.AreEqual(2, preview.Steps.Count);
        Assert.IsFalse(preview.ExecutesAutomatically);
        Assert.IsTrue(preview.CoreAuthorityRequired);
        Assert.IsTrue(preview.UiAuthorityBlocked);
    }

    [TestMethod]
    public void PlanPreviewCanBecomeReady()
    {
        var draft = service.Draft("plan-2", "Review local readiness", ["Open dashboard"]);
        var ready = service.MarkPreviewReady(draft);

        Assert.AreEqual(NodalOsPlanPreviewStatus.PlanPreviewReady, ready.Status);
        Assert.IsFalse(ready.ExecutesAutomatically);
    }

    [TestMethod]
    public void PlanPreviewCanAwaitHumanApproval()
    {
        var draft = service.Draft("plan-3", "Human review required", ["Ask operator"]);
        var awaiting = service.AwaitingApproval(draft);

        Assert.AreEqual(NodalOsPlanPreviewStatus.PlanAwaitingApproval, awaiting.Status);
        Assert.IsFalse(awaiting.ExecutesAutomatically);
    }

    [TestMethod]
    public void RejectedPlanDoesNotExecute()
    {
        var preview = service.Draft("plan-4", "Review local readiness", ["Open dashboard"]);
        var rejected = service.Reject(preview);

        Assert.AreEqual(NodalOsPlanPreviewStatus.PlanRejected, rejected.Status);
        Assert.IsFalse(rejected.ExecutesAutomatically);
    }

    [TestMethod]
    public void PlanPreviewBlocksSensitiveActionsByPolicy()
    {
        var preview = service.Draft(
            "plan-5",
            "Try submit",
            ["Click submit"],
            sensitiveActions: [NodalOsPlanSensitiveAction.Submit]);

        Assert.AreEqual(NodalOsPlanPreviewStatus.ExecutionBlockedByPolicy, preview.Status);
        Assert.IsTrue(preview.HumanApprovalRequired);
        Assert.IsTrue(preview.PolicySummary.SensitiveActionsBlocked);
        Assert.IsTrue(preview.SensitiveActionsDetected.Contains(NodalOsPlanSensitiveAction.Submit));
        Assert.IsTrue(preview.Steps.All(step => !step.ExecutesAutomatically));
    }

    [TestMethod]
    public void PlanPreviewMapsToExistingTimelineSteps()
    {
        var preview = service.Draft("plan-6", "Review local readiness", ["Open dashboard", "Review evidence"]);
        var timeline = timelineAdapter.Map(service.MarkPreviewReady(preview));

        Assert.AreEqual("NODAL OS", timeline.ProductName);
        Assert.AreEqual(2, timeline.Steps.Count);
        Assert.IsTrue(timeline.Steps.All(step => step.Node.NodeType == NodalOsTimelineNodeType.StructuredTask));
        Assert.IsTrue(timeline.Steps.All(step => step.CoreAuthorityRequired));
        Assert.IsTrue(timeline.Steps.All(step => !step.GrantsAuthority));
    }

    [TestMethod]
    public void SensitivePlanMapsToBlockedTimelineStep()
    {
        var preview = service.Draft(
            "plan-7",
            "Try payment",
            ["Click pay"],
            sensitiveActions: [NodalOsPlanSensitiveAction.Payment]);
        var timeline = timelineAdapter.Map(preview);

        Assert.AreEqual(NodalOsTimelineStepStatus.Blocked, timeline.Steps.Single().Status);
        Assert.AreEqual(NodalOsTimelineNodeType.BlockerStep, timeline.Steps.Single().Node.NodeType);
        Assert.IsTrue(timeline.Steps.Single().Blockers.Any());
        Assert.IsFalse(timeline.GrantsAuthority);
    }

    [TestMethod]
    public void PlanPreviewPreservesAllowedAndDeniedDomains()
    {
        var preview = service.Draft(
            "plan-8",
            "Local only",
            ["Review local dashboard"],
            allowedDomains: ["local-private-preview"],
            deniedDomains: ["production", "public-saas", "sensitive-sites"]);

        Assert.IsTrue(preview.AllowedDomains.Contains("local-private-preview"));
        Assert.IsTrue(preview.DeniedDomains.Contains("production"));
        Assert.IsTrue(preview.DeniedDomains.Contains("public-saas"));
        Assert.IsTrue(preview.PolicySummary.ProductionBlocked);
    }

    [TestMethod]
    public void PlanPreviewDoesNotContainSecretsCookiesOrTokens()
    {
        var preview = service.Draft(
            "plan-9",
            "token=synthetic-token-value cookie=session-secret password=abc123",
            ["Review token=synthetic-token-value"]);
        var json = JsonSerializer.Serialize(preview);

        Assert.IsFalse(json.Contains("synthetic-token-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("session-secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("password=abc123", StringComparison.Ordinal));
        Assert.IsTrue(preview.Redacted);
    }

    [TestMethod]
    public void PlanPreviewRejectsUiAuthorityAndAutoExecution()
    {
        var preview = service.Draft("plan-10", "Review local dashboard", ["Open dashboard"]);

        Assert.IsTrue(preview.CoreAuthorityRequired);
        Assert.IsTrue(preview.UiAuthorityBlocked);
        Assert.IsTrue(preview.PolicySummary.AutoExecutionBlocked);
        Assert.IsFalse(preview.ExecutesAutomatically);
        Assert.IsTrue(preview.TimelineCompatibilityMapping.Contains("renderTimeline", StringComparison.Ordinal));
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
