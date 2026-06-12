using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.ExecutorHarness;
using OneBrain.Core.History;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ExecutorHarnessSupervisedClickTests
{
    [TestMethod]
    public void Harness_Approval_Is_Required_For_Benign_Click()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target, new DateTimeOffset(2026, 06, 12, 12, 0, 0, TimeSpan.Zero));

        Assert.IsTrue(request.RequiresApproval);
        Assert.IsFalse(request.FailClosed);
        Assert.AreEqual(ApprovalActionKinds.BenignHarnessClick, request.ActionKind);
        StringAssert.Contains(string.Join(" ", request.Notes), "approval required");
    }

    [TestMethod]
    public void Harness_Fails_Closed_Without_Approval_Decision()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();

        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, null, new FakeHarnessExecutor(true));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Blocked, result.Status);
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.Clicks);
        StringAssert.Contains(result.Message, "approval decision is required");
    }

    [TestMethod]
    public void Harness_Blocks_When_Decision_Does_Not_Allow_Execution()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Audit only.");

        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Blocked, result.Status);
        StringAssert.Contains(result.Message, "does not allow executor harness action");
    }

    [TestMethod]
    public void ApprovalPolicy_Does_Not_Allow_Execution_For_Non_Harness_Actions()
    {
        var request = ApprovalPolicy.CreateRequest(
            source: "approval_demo",
            candidateFlowId: "candidate-demo",
            actionKind: ApprovalActionKinds.ViewReport,
            riskLevel: ApprovalRiskLevels.Low,
            title: "View report",
            description: "Non harness action.",
            preview: "report",
            policy: ApprovalPolicy.DefaultPlatformPolicy with { HumanInTheLoopMode = HumanInTheLoopModes.AlwaysRequired },
            confidenceScore: 100,
            hasSafeExecutor: true);

        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        Assert.IsFalse(request.FailClosed);
        Assert.IsFalse(decision.ExecutionAllowed);
        StringAssert.Contains(string.Join(" ", decision.Notes), "not the benign executor harness action");
    }

    [TestMethod]
    public void Harness_Blocks_When_Target_Is_Not_Allowlisted_Local_Pilot_Target()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget() with
        {
            WindowTitleContains = "External Browser",
            TargetRef = "name:Comprar"
        };
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Blocked, result.Status);
        StringAssert.Contains(result.Message, "window target is not the local Pilot harness");
        StringAssert.Contains(result.Message, "target identity is not the benign harness target");
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.Clicks);
    }

    [TestMethod]
    public void Harness_Target_Resolver_Rejects_External_Or_Non_Local_Targets()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget() with
        {
            WindowTitleContains = "https://example.invalid",
            TargetRef = "name:Pagar ahora"
        };

        var resolution = ExecutorHarnessTargetResolver.ResolveTarget(target);

        Assert.IsFalse(resolution.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Blocked, resolution.Status);
        StringAssert.Contains(resolution.Message, "window target is not the local Pilot harness");
        StringAssert.Contains(resolution.Message, "target identity is not the benign harness target");
        StringAssert.Contains(resolution.Message, "external navigation signal");
    }

    [TestMethod]
    public void Harness_Safety_Matrix_Allows_Only_Approved_Benign_Local_Target()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var matrix = ExecutorHarnessSafetyMatrix.Evaluate(target, decision);

        Assert.IsTrue(matrix.Allowed);
        Assert.AreEqual("allowed", matrix.Status);
        CollectionAssert.Contains(matrix.Passed.ToList(), "target resolved to allowlisted local harness");
        CollectionAssert.Contains(matrix.Passed.ToList(), "execution allowed by scoped approval decision");
    }

    [TestMethod]
    public void Harness_Safety_Matrix_Fails_Closed_Without_Scoped_Approval()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();

        var matrix = ExecutorHarnessSafetyMatrix.Evaluate(target, null);

        Assert.IsFalse(matrix.Allowed);
        Assert.AreEqual(ExecutorHarnessStatuses.Blocked, matrix.Status);
        CollectionAssert.Contains(matrix.RequiresApproval.ToList(), "approval decision is required");
        CollectionAssert.Contains(matrix.Blocked.ToList(), "approval decision is required");
    }

    [TestMethod]
    public void Harness_Safety_Matrix_Fails_Closed_For_Invalid_Action_Kind()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget() with { ActionKind = ApprovalActionKinds.Purchase };
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Blocked, result.Status);
        StringAssert.Contains(result.Message, "action kind is outside benign harness scope");
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.Clicks);
    }

    [TestMethod]
    public void Harness_Interaction_Contract_Captures_Target_Approval_Safety_And_Post_State_Expectation()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var contract = ExecutorHarnessService.BuildInteractionContract(target, request, decision, dryRunOnly: false);

        Assert.AreEqual(target.HarnessId, contract.HarnessId);
        Assert.AreEqual(target.AppProfileId, contract.AppProfileId);
        Assert.AreEqual(target.WindowTitleContains, contract.WindowConstraints.TitleContains);
        Assert.AreEqual(target.TargetRef, contract.TargetConstraints.TargetRef);
        Assert.IsFalse(contract.TargetConstraints.UserConfigurableTargetAllowed);
        Assert.IsTrue(contract.ResolvedTarget.Success);
        Assert.IsTrue(contract.ApprovalState.Approved);
        Assert.IsTrue(contract.ApprovalState.ExecutionAllowed);
        Assert.AreEqual("allowed", contract.SafetyMatrix.Status);
        Assert.IsFalse(contract.PreActionState.DryRunOnly);
        Assert.IsTrue(contract.PreActionState.ExecutorWillRun);
        Assert.IsTrue(string.Equals(target.ExpectedTargetName, contract.PostActionExpectation.ExpectedTargetName, StringComparison.Ordinal));
        if (contract.PostActionExpectation.ExpectedClickCount != 1)
            Assert.Fail($"Expected click count 1, got {contract.PostActionExpectation.ExpectedClickCount}.");
        StringAssert.Contains(contract.LogicalEvidencePath, "artifacts/executor-harness");
    }

    [TestMethod]
    public void Harness_Dry_Run_Explains_Fail_Closed_Without_Calling_Executor()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();

        var dryRun = ExecutorHarnessService.BuildDryRunExplanation(target);

        Assert.IsFalse(dryRun.WouldExecute);
        Assert.AreEqual("fail_closed_dry_run", dryRun.Status);
        Assert.IsTrue(dryRun.Contract.PreActionState.DryRunOnly);
        Assert.IsFalse(dryRun.Contract.PreActionState.ExecutorWillRun);
        CollectionAssert.Contains(dryRun.BlockingConditions.ToList(), "approval decision is required");
        StringAssert.Contains(string.Join(" ", dryRun.Notes), "does not click");
    }

    [TestMethod]
    public void Harness_Flow_Dry_Run_Has_At_Least_Two_Steps_And_Does_Not_Execute()
    {
        var flow = ExecutorHarnessService.BuildFlowPlan();

        Assert.AreEqual(ExecutorHarnessDemoFixture.FlowId, flow.FlowId);
        Assert.IsTrue(flow.Steps.Count >= 2);
        Assert.AreEqual("fail_closed_dry_run", flow.Status);
        Assert.AreEqual("step-1", flow.Steps[0].StepId);
        Assert.AreEqual("step-2", flow.Steps[1].StepId);
        Assert.IsFalse(flow.Steps[0].SafetyDecision.Allowed);
        StringAssert.Contains(flow.FailureRecoveryPolicy, "fail_closed");
    }

    [TestMethod]
    public void Harness_Executes_One_Benign_Click_And_Verifies_Post_Action()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(
            request,
            ApprovalDecisionKinds.Approved,
            "Approved for local harness.",
            executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Succeeded, result.Status);
        Assert.IsTrue(result.Verification.Success);
        Assert.IsTrue(result.Verification.TargetFound);
        Assert.IsTrue(result.Verification.ClickObserved);
        Assert.AreEqual(1, result.RunHistory.SafetyCounters.Clicks);
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.CookiesAccepted);
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.Login);
        Assert.AreEqual(RunHistoryStatuses.Succeeded, result.RunHistory.Status);
    }

    [TestMethod]
    public void Harness_Fails_When_Post_Action_State_Is_Not_Independently_Verified()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true, includePostActionSignals: false));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Failed, result.Status);
        StringAssert.Contains(result.Message, "post-action verification failed");
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.Clicks);
    }

    [TestMethod]
    public void Harness_Fails_When_Post_Action_Target_Name_Does_Not_Match()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedClick(
            target,
            decision,
            new FakeHarnessExecutor(true, postActionTargetName: "Otro boton"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Failed, result.Status);
        StringAssert.Contains(result.Message, "post-action verification failed");
        Assert.AreEqual(0, result.RunHistory.SafetyCounters.Clicks);
    }

    [TestMethod]
    public void Harness_Writes_Evidence_Under_Artifacts()
    {
        var root = NewTempRoot();
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));
        var evidence = ExecutorHarnessService.ToEvidenceRecord(result);

        var write = ExecutorHarnessArtifactStore.Write(root, evidence);

        Assert.IsTrue(write.Success, write.Error);
        StringAssert.Contains(write.RelativePath, "artifacts/executor-harness");
        Assert.IsTrue(File.Exists(write.Path));
    }

    [TestMethod]
    public void Harness_Evidence_Record_Embeds_Interaction_Contract_For_Replay()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));

        var evidence = ExecutorHarnessService.ToEvidenceRecord(result);

        Assert.IsNotNull(evidence.InteractionContract);
        Assert.AreEqual(target.HarnessId, evidence.InteractionContract.HarnessId);
        Assert.AreEqual("allowed", evidence.InteractionContract.SafetyMatrix.Status);
        Assert.IsTrue(evidence.InteractionContract.ApprovalState.ExecutionAllowed);
    }

    [TestMethod]
    public void Harness_Replay_Returns_Empty_State_When_No_Evidence_Exists()
    {
        var root = NewTempRoot();

        var replay = ExecutorHarnessArtifactStore.ReadLatest(root);

        Assert.IsTrue(replay.Success);
        Assert.AreEqual("empty", replay.Status);
        Assert.IsNull(replay.Evidence);
        StringAssert.Contains(replay.Message, "No executor harness evidence");
    }

    [TestMethod]
    public void Harness_Replay_Reads_Latest_Evidence_Read_Only()
    {
        var root = NewTempRoot();
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));
        var write = ExecutorHarnessArtifactStore.Write(root, ExecutorHarnessService.ToEvidenceRecord(result));

        var replay = ExecutorHarnessArtifactStore.ReadLatest(root);

        Assert.IsTrue(write.Success, write.Error);
        Assert.IsTrue(replay.Success, replay.Message);
        Assert.IsNotNull(replay.Evidence);
        Assert.AreEqual(result.Target.HarnessId, replay.Evidence.HarnessId);
        Assert.IsNotNull(replay.Evidence.InteractionContract);
        StringAssert.Contains(replay.RelativePath, "artifacts/executor-harness");
        StringAssert.Contains(string.Join(" ", replay.Notes), "no click is executed");
    }

    [TestMethod]
    public void Harness_Flow_Evidence_Record_Serializes_Steps_And_Replay_Shows_Them()
    {
        var root = NewTempRoot();
        var targets = ExecutorHarnessDemoFixture.CreateFlowTargets();
        var request = ExecutorHarnessService.CreateApprovalRequest(targets[0]);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var flowResult = ExecutorHarnessService.ExecuteSupervisedFlow(targets, decision, new FakeHarnessExecutor(true));
        var evidence = ExecutorHarnessService.ToEvidenceRecord(flowResult);
        var write = ExecutorHarnessArtifactStore.Write(root, evidence);

        var replay = ExecutorHarnessArtifactStore.ReadLatest(root);

        Assert.IsTrue(write.Success, write.Error);
        Assert.IsTrue(flowResult.Success);
        Assert.IsNotNull(replay.Evidence);
        Assert.AreEqual(2, replay.Evidence.Steps?.Count);
        Assert.AreEqual(ExecutorHarnessDemoFixture.FlowId, replay.Evidence.FlowId);
        Assert.AreEqual(ExecutorHarnessStatuses.Succeeded, replay.Evidence.FlowStatus);
    }

    [TestMethod]
    public void Harness_Evidence_Index_Returns_Empty_State_When_No_Evidence_Exists()
    {
        var root = NewTempRoot();

        var index = ExecutorHarnessArtifactStore.ReadIndex(root);

        Assert.IsTrue(index.Success);
        Assert.AreEqual("empty", index.Status);
        Assert.AreEqual(0, index.Items.Count);
        StringAssert.Contains(index.Message, "No executor harness evidence");
        StringAssert.Contains(string.Join(" ", index.Notes), "read-only");
    }

    [TestMethod]
    public void Harness_Evidence_Index_Lists_Evidence_With_Trace_Link()
    {
        var root = NewTempRoot();
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));
        var write = ExecutorHarnessArtifactStore.Write(root, ExecutorHarnessService.ToEvidenceRecord(result));

        var index = ExecutorHarnessArtifactStore.ReadIndex(root);

        Assert.IsTrue(write.Success, write.Error);
        Assert.IsTrue(index.Success, index.Message);
        Assert.AreEqual("indexed", index.Status);
        Assert.AreEqual(1, index.Items.Count);
        var item = index.Items[0];
        Assert.AreEqual(target.HarnessId, item.HarnessId);
        Assert.AreEqual(ApprovalActionKinds.BenignHarnessClick, item.ActionKind);
        Assert.AreEqual("allowed", item.SafetyDecision);
        Assert.AreEqual(ExecutorHarnessStatuses.Succeeded, item.VerificationResult);
        StringAssert.Contains(item.LogicalPath, "artifacts/executor-harness");
        Assert.IsTrue(item.TraceLink.IsSynthetic);
        StringAssert.Contains(item.TraceLink.RunId, "local-trace");
        Assert.AreEqual("/executor-harness/replay", item.TraceLink.ReplayPath);
        Assert.AreEqual("approved", item.TraceLink.ApprovalDecision);
        StringAssert.Contains(item.TraceLink.PostStateResult, "verified");
    }

    [TestMethod]
    public void Harness_Evidence_Index_Shows_Step_Count_For_Flow_Evidence()
    {
        var root = NewTempRoot();
        var targets = ExecutorHarnessDemoFixture.CreateFlowTargets();
        var request = ExecutorHarnessService.CreateApprovalRequest(targets[0]);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var flowResult = ExecutorHarnessService.ExecuteSupervisedFlow(targets, decision, new FakeHarnessExecutor(true));
        var write = ExecutorHarnessArtifactStore.Write(root, ExecutorHarnessService.ToEvidenceRecord(flowResult));

        var index = ExecutorHarnessArtifactStore.ReadIndex(root);

        Assert.IsTrue(write.Success, write.Error);
        Assert.AreEqual(1, index.Items.Count);
        Assert.AreEqual(2, index.Items[0].StepCount);
        Assert.AreEqual(ExecutorHarnessStatuses.Succeeded, index.Items[0].FlowStatus);
    }

    [TestMethod]
    public void Harness_Evidence_Index_Lists_Latest_First()
    {
        var root = NewTempRoot();
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var first = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));
        var second = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));
        var firstWrite = ExecutorHarnessArtifactStore.Write(root, ExecutorHarnessService.ToEvidenceRecord(first, new DateTimeOffset(2026, 06, 12, 12, 0, 0, TimeSpan.Zero)));
        Thread.Sleep(20);
        var secondEvidence = ExecutorHarnessService.ToEvidenceRecord(second, new DateTimeOffset(2026, 06, 12, 12, 1, 0, TimeSpan.Zero));
        var secondWrite = ExecutorHarnessArtifactStore.Write(root, secondEvidence);

        var index = ExecutorHarnessArtifactStore.ReadIndex(root);

        Assert.IsTrue(firstWrite.Success, firstWrite.Error);
        Assert.IsTrue(secondWrite.Success, secondWrite.Error);
        Assert.AreEqual(2, index.Items.Count);
        Assert.AreEqual(secondEvidence.EvidenceId, index.Items[0].EvidenceId);
    }

    [TestMethod]
    public void Harness_Replay_Exposes_Run_Trace_Link_Without_Executing()
    {
        var root = NewTempRoot();
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);
        var result = ExecutorHarnessService.ExecuteSupervisedClick(target, decision, new FakeHarnessExecutor(true));
        _ = ExecutorHarnessArtifactStore.Write(root, ExecutorHarnessService.ToEvidenceRecord(result));

        var replay = ExecutorHarnessArtifactStore.ReadLatest(root);

        Assert.IsNotNull(replay.TraceLink);
        Assert.IsTrue(replay.TraceLink.IsSynthetic);
        StringAssert.Contains(replay.TraceLink.EvidencePath, "artifacts/executor-harness");
        Assert.AreEqual("/executor-harness/replay", replay.TraceLink.ReplayPath);
        StringAssert.Contains(string.Join(" ", replay.TraceLink.Notes), "does not yet embed full run history id");
    }

    [TestMethod]
    public void Harness_Replay_Remains_Compatible_With_Old_Evidence_Without_Steps()
    {
        var root = NewTempRoot();
        var evidence = new ExecutorHarnessEvidenceRecord(
            EvidenceId: "legacy-evidence",
            CreatedAtUtc: "2026-06-12T12:00:00Z",
            HarnessId: ExecutorHarnessDemoFixture.HarnessId,
            Status: ExecutorHarnessStatuses.Succeeded,
            Message: "legacy single-step evidence",
            ApprovalRequestId: "approval-legacy",
            ApprovalDecisionId: "decision-legacy",
            Verification: new ExecutorHarnessPostActionVerification(true, ExecutorHarnessStatuses.Succeeded, "ok", true, true, ["postAction.windowFound=true"]),
            SafetyCounters: new RunSafetyCounters(1, 0, 0, 0, 0, 0),
            Notes: ["legacy"]);
        var write = ExecutorHarnessArtifactStore.Write(root, evidence);

        var replay = ExecutorHarnessArtifactStore.ReadLatest(root);

        Assert.IsTrue(write.Success, write.Error);
        Assert.IsNotNull(replay.Evidence);
        Assert.IsNull(replay.Evidence.Steps);
        Assert.AreEqual("", replay.Evidence.FlowId ?? "");
    }

    [TestMethod]
    public void Harness_Flow_Failure_Policy_Blocks_Continuation_When_Step_Fails()
    {
        var targets = ExecutorHarnessDemoFixture.CreateFlowTargets();
        var request = ExecutorHarnessService.CreateApprovalRequest(targets[0]);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedFlow(targets, decision, new ScriptedFlowHarnessExecutor(
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [targets[0].ExpectedTargetName] = true,
                [targets[1].ExpectedTargetName] = false
            }));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ExecutorHarnessStatuses.Blocked, result.Status);
        Assert.IsFalse(result.RecoveryDecision.ContinueAllowed);
        Assert.AreEqual("step-2", result.RecoveryDecision.FailedStepId);
        Assert.AreEqual(2, result.Steps.Count);
        StringAssert.Contains(result.Message, "post-action verification failed");
    }

    [TestMethod]
    public void Harness_Flow_Fails_Closed_For_Invalid_Target_In_Step()
    {
        var targets = ExecutorHarnessDemoFixture.CreateFlowTargets().ToList();
        targets[1] = targets[1] with
        {
            TargetRef = "name:Comprar",
            ExpectedTargetName = "Comprar"
        };
        var request = ExecutorHarnessService.CreateApprovalRequest(targets[0]);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedFlow(targets, decision, new FakeHarnessExecutor(true));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("step-2", result.RecoveryDecision.FailedStepId);
        StringAssert.Contains(result.Message, "target identity is not the benign harness target");
        Assert.AreEqual(1, result.RunHistory.SafetyCounters.Clicks);
    }

    [TestMethod]
    public void Harness_Flow_Fails_Closed_For_Invalid_Action_Kind_In_Step()
    {
        var targets = ExecutorHarnessDemoFixture.CreateFlowTargets().ToList();
        targets[1] = targets[1] with
        {
            ActionKind = ApprovalActionKinds.Purchase
        };
        var request = ExecutorHarnessService.CreateApprovalRequest(targets[0]);
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "Approved.", executionAllowed: true);

        var result = ExecutorHarnessService.ExecuteSupervisedFlow(targets, decision, new FakeHarnessExecutor(true));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("step-2", result.RecoveryDecision.FailedStepId);
        StringAssert.Contains(result.Message, "action kind is outside benign harness scope");
    }

    [TestMethod]
    public void Pilot_Render_Shows_Executor_Harness_Safety_And_Evidence()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);
        var index = new ExecutorHarnessEvidenceIndex(
            Success: true,
            Status: "empty",
            Message: "No executor harness evidence exists yet.",
            Items: [],
            Notes: ["evidence index is read-only"]);

        var html = PilotHomePageRenderer.RenderExecutorHarness(target, request);
        var indexHtml = PilotHomePageRenderer.RenderExecutorHarnessEvidenceIndex(index);
        var flowHtml = PilotHomePageRenderer.RenderExecutorHarnessFlow(ExecutorHarnessService.BuildFlowPlan(), dryRunOnly: true);
        var home = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "Click benigno supervisado");
        StringAssert.Contains(html, "requiere aprobacion");
        StringAssert.Contains(html, "sin MercadoLibre");
        StringAssert.Contains(html, "sin compra/pago/login/cookies");
        StringAssert.Contains(html, "Objetivo benigno del harness");
        StringAssert.Contains(html, "Dry-run explicable antes de ejecutar");
        StringAssert.Contains(html, "Ver replay de evidencia");
        StringAssert.Contains(html, "Ver indice de evidencia");
        StringAssert.Contains(html, "Ver flow multi-step");
        StringAssert.Contains(indexHtml, "Indice de evidencia del executor harness");
        StringAssert.Contains(indexHtml, "read-only");
        StringAssert.Contains(indexHtml, "no ejecuta acciones");
        StringAssert.Contains(flowHtml, "Flow multi-step");
        StringAssert.Contains(flowHtml, "step-1");
        StringAssert.Contains(flowHtml, "step-2");
        StringAssert.Contains(home, "href=\"/executor-harness\"");
    }

    [TestMethod]
    public void Pilot_Runner_Prints_Executor_Harness_Route()
    {
        var script = File.ReadAllText(GetScriptPath());

        StringAssert.Contains(script, "PILOT_ROUTE_EXECUTOR_HARNESS");
        StringAssert.Contains(script, "/executor-harness");
    }

    private static string NewTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "onebrain-executor-harness-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string GetScriptPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "tools", "scripts", "run-onebrain-pilot.ps1");
    }

    private sealed class FakeHarnessExecutor(
        bool success,
        bool includePostActionSignals = true,
        string? postActionTargetName = null) : IExecutorHarnessClickExecutor
    {
        public ExecutorHarnessExecutorResult Click(ExecutorHarnessClickCommand command)
        {
            var targetResolution = ExecutorHarnessTargetResolver.ResolveCommand(command);
            var targetName = postActionTargetName ?? command.ExpectedTargetName;
            var signals = new List<string>
            {
                command.HarnessId,
                command.TargetRef,
                "fake executor; no real click in tests"
            };
            if (includePostActionSignals)
            {
                signals.Add("postAction.windowFound=true");
                signals.Add("postAction.targetVisible=true");
            }

            var postActionState = new ExecutorHarnessPostActionState(
                WindowFound: includePostActionSignals,
                TargetVisible: includePostActionSignals,
                TargetName: includePostActionSignals ? targetName : "",
                ObservedClicks: success ? 1 : 0,
                ClickCountVerified: success && includePostActionSignals,
                Signals: includePostActionSignals
                    ?
                    [
                        "postAction.windowFound=true",
                        "postAction.targetVisible=true",
                        $"postAction.targetName={targetName}",
                        $"postAction.observedClicks={(success ? 1 : 0)}"
                    ]
                    : ["postAction.windowFound=false", "postAction.targetVisible=false"]);

            return new ExecutorHarnessExecutorResult(
                Success: success,
                Message: success ? "fake benign click executed" : "fake click failed",
                TargetFound: success,
                Clicks: success ? 1 : 0,
                Signals: signals,
                TargetResolution: targetResolution,
                PostActionState: postActionState);
        }
    }

    private sealed class ScriptedFlowHarnessExecutor(
        IReadOnlyDictionary<string, bool> outcomes) : IExecutorHarnessClickExecutor
    {
        public ExecutorHarnessExecutorResult Click(ExecutorHarnessClickCommand command)
        {
            var success = outcomes.TryGetValue(command.ExpectedTargetName, out var value) && value;
            var targetResolution = ExecutorHarnessTargetResolver.ResolveCommand(command);
            var signals = new List<string>
            {
                command.HarnessId,
                command.TargetRef,
                "fake executor; no real click in tests"
            };
            if (success)
            {
                signals.Add("postAction.windowFound=true");
                signals.Add("postAction.targetVisible=true");
                signals.Add($"postAction.targetName={command.ExpectedTargetName}");
            }

            var postActionState = new ExecutorHarnessPostActionState(
                WindowFound: success,
                TargetVisible: success,
                TargetName: success ? command.ExpectedTargetName : "",
                ObservedClicks: success ? 1 : 0,
                ClickCountVerified: success,
                Signals: success
                    ?
                    [
                        "postAction.windowFound=true",
                        "postAction.targetVisible=true",
                        $"postAction.targetName={command.ExpectedTargetName}",
                        "postAction.observedClicks=1"
                    ]
                    : ["postAction.windowFound=false", "postAction.targetVisible=false"]);

            return new ExecutorHarnessExecutorResult(
                Success: success,
                Message: success ? "fake benign click executed" : "post-action verification failed",
                TargetFound: success,
                Clicks: success ? 1 : 0,
                Signals: signals,
                TargetResolution: targetResolution,
                PostActionState: postActionState);
        }
    }
}
