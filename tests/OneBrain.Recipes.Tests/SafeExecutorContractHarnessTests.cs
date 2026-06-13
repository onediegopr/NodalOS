using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Execution;
using OneBrain.Core.ExecutorHarness;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeExecutorContractHarnessTests
{
    [TestMethod]
    public void Harness_Result_Carries_Transition_Evidence_From_Fsm()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(StepState.Succeeded, result.FinalState);
        Assert.IsNotNull(result.TransitionEvidence);
        Assert.IsTrue(result.TransitionEvidence.Count >= 5);
        Assert.AreEqual(StepTransition.Verified, result.TransitionEvidence[^1].Event);
    }

    [TestMethod]
    public void Harness_Evidence_Record_Persists_FailureKind_And_Transition_Evidence()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(false));

        var evidence = ExecutorHarnessService.ToEvidenceRecord(result);

        Assert.IsNotNull(evidence.TransitionEvidence);
        Assert.IsTrue(evidence.TransitionEvidence.Count > 0);
        Assert.AreEqual(result.FailureKind, evidence.FailureKind);
    }

    private sealed class FakeHarnessExecutor(bool success) : IExecutorHarnessClickExecutor
    {
        public ExecutorHarnessExecutorResult Click(ExecutorHarnessClickCommand command)
        {
            var targetResolution = ExecutorHarnessTargetResolver.ResolveCommand(command);
            var postActionState = new ExecutorHarnessPostActionState(
                WindowFound: success,
                TargetVisible: success,
                TargetName: success ? command.ExpectedTargetName : "",
                ObservedClicks: success ? 1 : 0,
                ClickCountVerified: success,
                Signals: success
                    ? ["postAction.windowFound=true", "postAction.targetVisible=true", $"postAction.targetName={command.ExpectedTargetName}", "postAction.observedClicks=1"]
                    : ["postAction.windowFound=false", "postAction.targetVisible=false"]);

            return new ExecutorHarnessExecutorResult(
                Success: success,
                Message: success ? "fake benign click executed" : "fake click failed",
                TargetFound: success,
                Clicks: success ? 1 : 0,
                Signals: ["fake executor"],
                TargetResolution: targetResolution,
                PostActionState: postActionState);
        }
    }
}
