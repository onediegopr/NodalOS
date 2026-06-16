using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PlanPreviewUIBinding")]
[TestCategory("PlanPreviewTimelineAdapter")]
[TestCategory("RuntimeStagnationDetector")]
[TestCategory("StagnationDetector")]
[TestCategory("RecoveryUX")]
[TestCategory("RecoveryTimeline")]
[TestCategory("TimelineStepper")]
[TestCategory("VerticalTimeline")]
[TestCategory("SidePanelTimeline")]
[TestCategory("OperatorTimeline")]
[TestCategory("BlockerTimeline")]
public sealed class NodalOsPlanPreviewStagnationRecoveryM166M168Tests
{
    private readonly NodalOsExecutionPlanPreviewService planService = new();
    private readonly NodalOsPlanPreviewToTimelineAdapter planTimeline = new();
    private readonly NodalOsRuntimeStagnationDetector detector = new();
    private readonly NodalOsRecoveryUxService recovery = new();
    private readonly NodalOsRecoveryTimelineAdapter recoveryTimeline = new();

    [TestMethod]
    public void PlanPreviewUiBindingUsesExistingTimelineRenderer()
    {
        var js = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.js"));
        var html = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.html"));

        StringAssert.Contains(js, "case 'planPreview'");
        StringAssert.Contains(js, "buildPlanPreviewTimeline");
        StringAssert.Contains(js, "renderTimeline(el.operatorTimeline");
        Assert.AreEqual(3, CountOccurrences(html, "class=\"timeline\""));
        Assert.AreEqual(0, CountOccurrences(html, "planPreviewTimeline"));
    }

    [TestMethod]
    public void PlanPreviewAwaitingApprovalShowsNeedsHumanCoreAuthority()
    {
        var preview = planService.AwaitingApproval(planService.Draft("plan-await", "Review local state", ["Open dashboard"]));
        var timeline = planTimeline.Map(preview);

        Assert.AreEqual(NodalOsTimelineStepStatus.NeedsHuman, timeline.Steps.Single().Status);
        Assert.IsTrue(timeline.Steps.Single().CoreAuthorityRequired);
        Assert.IsFalse(timeline.Steps.Single().GrantsAuthority);
    }

    [TestMethod]
    public void PlanPreviewBlockedByPolicyShowsBlockerCardAndDoesNotExecute()
    {
        var preview = planService.Draft("plan-block", "Try payment", ["Click payment"], sensitiveActions: [NodalOsPlanSensitiveAction.Payment]);
        var timeline = planTimeline.Map(preview);

        Assert.AreEqual(NodalOsPlanPreviewStatus.ExecutionBlockedByPolicy, preview.Status);
        Assert.IsFalse(preview.ExecutesAutomatically);
        Assert.AreEqual(NodalOsTimelineStepStatus.Blocked, timeline.Steps.Single().Status);
        Assert.IsTrue(timeline.Steps.Single().Blockers.Any());
    }

    [TestMethod]
    public void RejectedPlanDoesNotExecute()
    {
        var rejected = planService.Reject(planService.Draft("plan-reject", "Review local state", ["Open dashboard"]));

        Assert.AreEqual(NodalOsPlanPreviewStatus.PlanRejected, rejected.Status);
        Assert.IsFalse(rejected.ExecutesAutomatically);
    }

    [TestMethod]
    public void RepeatedUrlAndDomHashDetectStagnation()
    {
        var snapshots = Snapshots(3, url: "http://local/preview", domHash: "dom-a", screenshotHash: "shot-1", action: "observe");
        var signals = detector.Detect(snapshots, threshold: 3);

        Assert.IsTrue(signals.Any(signal => signal.Kind == NodalOsStagnationKind.RepeatedUrl));
        Assert.IsTrue(signals.Any(signal => signal.Kind == NodalOsStagnationKind.RepeatedDomHash));
        Assert.IsTrue(signals.All(signal => signal.Redacted));
        Assert.IsTrue(signals.All(signal => !signal.GrantsAuthority));
    }

    [TestMethod]
    public void RepeatedActionBlocksAfterThreshold()
    {
        var snapshots = Snapshots(4, action: "click:#same-target");
        var signals = detector.Detect(snapshots, threshold: 3);
        var signal = signals.Single(s => s.Kind == NodalOsStagnationKind.RepeatedAction);

        Assert.AreEqual(NodalOsStagnationSeverity.Blocked, signal.Severity);
        Assert.AreEqual(NodalOsRecoveryRecommendation.Replan, signal.Recommendation);
    }

    [TestMethod]
    public void SelectorRepeatedFailureCreatesAskHumanRecommendation()
    {
        var snapshots = Snapshots(3, selector: "#missing", error: "selector failed");
        var signal = detector.Detect(snapshots, threshold: 3).Single(s => s.Kind == NodalOsStagnationKind.SelectorRepeatedFailure);

        Assert.AreEqual(NodalOsRecoveryRecommendation.AskHuman, signal.Recommendation);
        Assert.AreEqual(NodalOsStagnationSeverity.Blocked, signal.Severity);
    }

    [TestMethod]
    public void ClickNoVisualChangeCreatesNoProgressRecovery()
    {
        var snapshots = Snapshots(3, action: "click:#ok", visualChanged: false);
        var signal = detector.Detect(snapshots, threshold: 3).Single(s => s.Kind == NodalOsStagnationKind.ClickNoVisualChange);

        Assert.AreEqual(NodalOsRecoveryRecommendation.Replan, signal.Recommendation);
    }

    [TestMethod]
    public void CaptchaLoginTwoFactorAsksHumanWithoutBypass()
    {
        var signal = detector.Detect(Snapshots(1, captcha: true), threshold: 3)
            .Single(s => s.Kind == NodalOsStagnationKind.CaptchaLoginTwoFactorDetected);
        var decision = recovery.CreateDecision(signal);

        Assert.AreEqual(NodalOsRecoveryState.BlockedByCaptchaLoginTwoFactor, decision.State);
        Assert.IsTrue(decision.Options.All(option => !option.ExecutesSensitiveWorkaround));
        Assert.IsTrue(decision.Explanation.OperatorMessage.Contains("will not bypass", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(decision.GrantsAuthority);
    }

    [TestMethod]
    public void RecoveryExplanationIncludesCauseAndSafeOptionsOnly()
    {
        var signal = detector.Detect(Snapshots(3, action: "click:#same", visualChanged: false), threshold: 3)
            .First(s => s.Kind == NodalOsStagnationKind.ClickNoVisualChange);
        var decision = recovery.CreateDecision(signal);

        StringAssert.Contains(decision.Explanation.Cause, "ClickNoVisualChange");
        Assert.IsTrue(decision.Options.Any(option => option.Label == "Copiar LOG"));
        Assert.IsTrue(decision.Options.Any(option => option.Label == "Ver evidencia"));
        Assert.IsTrue(decision.Options.All(option => option.Safe || !option.ExecutesSensitiveWorkaround));
        Assert.IsFalse(decision.GrantsAuthority);
    }

    [TestMethod]
    public void RecoveryTimelineShowsRecoveryStepEvidenceAndHumanIntervention()
    {
        var signal = detector.Detect(Snapshots(1, captcha: true), threshold: 3).Single();
        var decision = recovery.CreateDecision(signal);
        var timeline = recoveryTimeline.Map(decision);

        Assert.AreEqual(NodalOsTimelineNodeType.HumanIntervention, timeline.Steps.Single().Node.NodeType);
        Assert.IsTrue(timeline.Steps.Single().HumanInterventionRequired);
        Assert.IsTrue(timeline.Steps.Single().EvidenceRefs.Any());
        Assert.IsTrue(timeline.Steps.Single().Blockers.Any());
        Assert.IsFalse(timeline.GrantsAuthority);
    }

    [TestMethod]
    public void StagnationAndRecoveryDoNotLeakSecrets()
    {
        var snapshots = Snapshots(3, url: "http://local/?token=synthetic-secret", error: "cookie=session-secret password=abc123");
        var signal = detector.Detect(snapshots, threshold: 3).First();
        var decision = recovery.CreateDecision(signal);
        var json = JsonSerializer.Serialize(decision);

        Assert.IsFalse(json.Contains("synthetic-secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("session-secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("password=abc123", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PlanPreviewStagnationRecoveryAdrAndRunbookExist()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "plan-preview-stagnation-recovery-m166-m168.md"));
        var runbook = File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md"));

        StringAssert.Contains(adr, "Plan Preview");
        StringAssert.Contains(adr, "Runtime Stagnation V1");
        StringAssert.Contains(adr, "No second timeline is created");
        StringAssert.Contains(runbook, "Reading Plan Preview");
        StringAssert.Contains(runbook, "Reading Recovery / Stagnation");
    }

    private static IReadOnlyList<NodalOsRuntimeProgressSnapshot> Snapshots(
        int count,
        string url = "http://local/preview",
        string domHash = "dom-hash",
        string screenshotHash = "screenshot-hash",
        string action = "observe",
        string selector = "#target",
        string error = "",
        bool visualChanged = true,
        bool pageLoaded = true,
        bool captcha = false) =>
        Enumerable.Range(0, count)
            .Select(index => new NodalOsRuntimeProgressSnapshot(
                "runtime-local",
                "tab-local",
                $"step-{index}",
                url,
                domHash,
                screenshotHash,
                action,
                selector,
                error,
                visualChanged,
                pageLoaded,
                captcha,
                DateTimeOffset.UtcNow.AddSeconds(index),
                Redacted: true))
            .ToArray();

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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
