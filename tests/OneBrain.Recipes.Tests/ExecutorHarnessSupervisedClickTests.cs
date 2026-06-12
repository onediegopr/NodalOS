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
    public void Pilot_Render_Shows_Executor_Harness_Safety_And_Evidence()
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget();
        var request = ExecutorHarnessService.CreateApprovalRequest(target);

        var html = PilotHomePageRenderer.RenderExecutorHarness(target, request);
        var home = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "Click benigno supervisado");
        StringAssert.Contains(html, "requiere aprobacion");
        StringAssert.Contains(html, "sin MercadoLibre");
        StringAssert.Contains(html, "sin compra/pago/login/cookies");
        StringAssert.Contains(html, "Objetivo benigno del harness");
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
}
