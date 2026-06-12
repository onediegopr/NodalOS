using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Flows;
using OneBrain.Core.Recording;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class CandidateFlowPromotionPlaybackTests
{
    [TestMethod]
    public void Promotion_Succeeds_Only_For_Validated_Supervised_Flow()
    {
        var result = CandidateFlowPromotionService.Promote(BusinessFlowPlaybackFixture.CreatePromotionRequest());

        Assert.IsTrue(result.Success);
        Assert.AreEqual(CandidateFlowStatuses.ApprovedForSupervisedPlayback, result.Status);
        Assert.IsNotNull(result.Flow);
        Assert.IsFalse(result.Flow.AllowsAutonomousPlayback);
        Assert.IsTrue(result.Flow.RequiresHumanApproval);
        StringAssert.Contains(string.Join(" ", result.Flow.Notes), "no clicks");
    }

    [TestMethod]
    public void Promotion_Fails_Closed_When_Linter_Fails()
    {
        var request = BusinessFlowPlaybackFixture.CreatePromotionRequest() with { LinterPassed = false };

        var result = CandidateFlowPromotionService.Promote(request);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(CandidateFlowStatuses.Rejected, result.Status);
        StringAssert.Contains(string.Join(" ", result.Issues), "linter");
    }

    [TestMethod]
    public void Promotion_Fails_When_Sensitive_Step_Has_No_Approval_Policy()
    {
        var request = BusinessFlowPlaybackFixture.CreatePromotionRequest() with { ApprovalPolicyPresent = false };

        var result = CandidateFlowPromotionService.Promote(request);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(string.Join(" ", result.Issues), "approval policy");
    }

    [TestMethod]
    public void Promotion_Blocks_Payment_Login_Or_Cookie_Action()
    {
        var timeline = RecordingDemoFixture.CreateTimeline();
        var blockedStep = timeline.Steps[0] with
        {
            SuggestedActionLabel = "User clicked payment checkout",
            ElementSummary = "payment checkout"
        };
        var blockedTimeline = timeline with { Steps = [blockedStep] };
        var request = BusinessFlowPlaybackFixture.CreatePromotionRequest() with { Timeline = blockedTimeline };

        var result = CandidateFlowPromotionService.Promote(request);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(string.Join(" ", result.Issues), "blocked action");
    }

    [TestMethod]
    public void Supervised_Playback_Confirms_Safe_Fixture_Step()
    {
        var flow = BusinessFlowPlaybackFixture.CreatePromotedFlow();
        var session = SupervisedPlaybackService.Start(flow, new DateTimeOffset(2026, 06, 12, 12, 0, 0, TimeSpan.Zero));

        var result = SupervisedPlaybackService.ConfirmStep(flow, session, 1, now: new DateTimeOffset(2026, 06, 12, 12, 1, 0, TimeSpan.Zero));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(SupervisedPlaybackStepStatuses.Confirmed, result.Session.Steps.First().Status);
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.Clicks);
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.CookiesAccepted);
    }

    [TestMethod]
    public void Supervised_Playback_Blocks_Sensitive_Step_Without_Approval()
    {
        var flow = BusinessFlowPlaybackFixture.CreatePromotedFlow();
        var sensitive = flow.Steps.First(step => step.RequiresApproval);
        var session = SupervisedPlaybackService.Start(flow);

        var result = SupervisedPlaybackService.ConfirmStep(flow, session, sensitive.StepNumber);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SupervisedPlaybackStatuses.Blocked, result.Session.Status);
        StringAssert.Contains(result.Message, "approval required");
    }

    [TestMethod]
    public void Supervised_Playback_Blocks_When_No_Safe_Executor_Exists_Even_With_Approval()
    {
        var flow = BusinessFlowPlaybackFixture.CreatePromotedFlow();
        var sensitive = flow.Steps.First(step => step.RequiresApproval);
        var session = SupervisedPlaybackService.Start(flow);
        var approval = BusinessFlowPlaybackFixture.CreateSendApprovalDecision();

        var result = SupervisedPlaybackService.ConfirmStep(flow, session, sensitive.StepNumber, approval);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SupervisedPlaybackStatuses.Blocked, result.Session.Status);
        StringAssert.Contains(result.Message, "no safe executor");
    }

    [TestMethod]
    public void Promoted_Flow_Store_Writes_And_Reads_Local_Artifact()
    {
        var root = NewTempRoot();
        var flow = BusinessFlowPlaybackFixture.CreatePromotedFlow();

        var write = PromotedFlowStore.Write(root, flow);
        var read = PromotedFlowStore.ReadById(root, flow.FlowId);

        Assert.IsTrue(write.Success, write.Error);
        StringAssert.Contains(write.RelativePath, "artifacts/promoted-flows");
        Assert.IsNotNull(read);
        Assert.AreEqual(flow.FlowId, read.FlowId);
    }

    [TestMethod]
    public void Playback_Store_Writes_And_Reads_Local_Artifact()
    {
        var root = NewTempRoot();
        var session = BusinessFlowPlaybackFixture.CreatePlaybackSession();

        var write = SupervisedPlaybackStore.Write(root, session);
        var read = SupervisedPlaybackStore.ReadById(root, session.PlaybackId);

        Assert.IsTrue(write.Success, write.Error);
        StringAssert.Contains(write.RelativePath, "artifacts/supervised-playback");
        Assert.IsNotNull(read);
        Assert.AreEqual(session.PlaybackId, read.PlaybackId);
    }

    [TestMethod]
    public void Pilot_Render_Shows_Promoted_Flows_And_Playback()
    {
        var flow = BusinessFlowPlaybackFixture.CreatePromotedFlow();
        var flowsHtml = PilotHomePageRenderer.RenderPromotedFlows([flow]);
        var detailHtml = PilotHomePageRenderer.RenderPromotedFlowDetail(flow);
        var playbackHtml = PilotHomePageRenderer.RenderSupervisedPlayback(flow, SupervisedPlaybackService.Start(flow));

        StringAssert.Contains(flowsHtml, "Flujos supervisados");
        StringAssert.Contains(flowsHtml, "Promover demo segura");
        StringAssert.Contains(detailHtml, "Playback autonomo");
        StringAssert.Contains(playbackHtml, "Confirmar paso de demostracion");
        StringAssert.Contains(playbackHtml, "Este flujo no ejecuta acciones reales.");
        StringAssert.Contains(playbackHtml, "Abortar flujo");
    }

    private static string NewTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "onebrain-flow-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
