using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ApprovalPolicyTests
{
    [TestMethod]
    public void Sensitive_Action_Kinds_Always_Require_Approval()
    {
        Assert.IsTrue(ApprovalPolicy.AlwaysRequiresApproval(ApprovalActionKinds.Send));
        Assert.IsTrue(ApprovalPolicy.AlwaysRequiresApproval(ApprovalActionKinds.Submit));
        Assert.IsTrue(ApprovalPolicy.AlwaysRequiresApproval(ApprovalActionKinds.Pay));
        Assert.IsTrue(ApprovalPolicy.AlwaysRequiresApproval(ApprovalActionKinds.Purchase));
        Assert.IsTrue(ApprovalPolicy.AlwaysRequiresApproval(ApprovalActionKinds.Login));
        Assert.IsTrue(ApprovalPolicy.AlwaysRequiresApproval(ApprovalActionKinds.AcceptCookies));
        Assert.IsTrue(ApprovalPolicy.AlwaysRequiresApproval(ApprovalActionKinds.RunScript));
    }

    [TestMethod]
    public void Default_Platform_Policy_Requires_Approval_For_Sensitive_Action()
    {
        var requiresApproval = ApprovalPolicy.RequiresHumanInTheLoop(
            ApprovalActionKinds.Send,
            ApprovalRiskLevels.Low,
            confidenceScore: 100,
            hasSafeExecutor: true);

        Assert.IsTrue(requiresApproval);
    }

    [TestMethod]
    public void High_Confidence_Low_Risk_Can_Be_Configured_Not_To_Require_Approval()
    {
        var policy = new PlatformApprovalPolicy(
            HumanInTheLoopMode: HumanInTheLoopModes.ConfidenceBased,
            MinConfidenceForLowRiskAutoProceed: 80,
            SensitiveActionKinds: ApprovalPolicy.DefaultPlatformPolicy.SensitiveActionKinds,
            CriticalEnvironments: [],
            FailClosedWhenMissingInformation: true,
            FailClosedWithoutSafeExecutor: true);

        var requiresApproval = ApprovalPolicy.RequiresHumanInTheLoop(
            ApprovalActionKinds.ViewReport,
            ApprovalRiskLevels.Low,
            confidenceScore: 95,
            policy: policy,
            hasSafeExecutor: true);

        Assert.IsFalse(requiresApproval);
    }

    [TestMethod]
    public void Critical_Action_Defaults_To_Approval_Required()
    {
        var requiresApproval = ApprovalPolicy.RequiresHumanInTheLoop(
            ApprovalActionKinds.ViewReport,
            ApprovalRiskLevels.Critical,
            confidenceScore: 100,
            hasSafeExecutor: true);

        Assert.IsTrue(requiresApproval);
    }

    [TestMethod]
    public void Missing_Policy_Context_Or_Unsafe_Execution_Fails_Closed()
    {
        var request = ApprovalPolicy.CreateRequest(
            source: "test",
            candidateFlowId: "candidate",
            actionKind: ApprovalActionKinds.ViewReport,
            riskLevel: ApprovalRiskLevels.Low,
            title: "view report",
            description: "safe read-only report",
            preview: "report",
            confidenceScore: 100,
            hasSafeExecutor: false,
            createdAtUtc: DateTimeOffset.Parse("2026-06-12T10:00:00Z"));

        Assert.IsTrue(request.RequiresApproval);
        Assert.IsTrue(request.FailClosed);
        StringAssert.Contains(string.Join(" ", request.Notes), "no safe executor");
    }

    [TestMethod]
    public void Approval_Request_Fail_Closes_When_Information_Is_Missing()
    {
        var request = ApprovalPolicy.CreateRequest(
            source: "test",
            candidateFlowId: "candidate",
            actionKind: ApprovalActionKinds.Send,
            riskLevel: ApprovalRiskLevels.High,
            title: "",
            description: "",
            preview: "hello",
            missingInformation: ["target"],
            createdAtUtc: DateTimeOffset.Parse("2026-06-12T10:00:00Z"));

        Assert.IsTrue(request.RequiresApproval);
        Assert.IsTrue(request.FailClosed);
        CollectionAssert.Contains(request.MissingInformation.ToArray(), "target");
    }

    [TestMethod]
    public void Approval_Request_Redacts_Sensitive_Preview()
    {
        var request = ApprovalPolicy.CreateRequest(
            source: "test",
            candidateFlowId: "candidate",
            actionKind: ApprovalActionKinds.Send,
            riskLevel: ApprovalRiskLevels.High,
            title: "send",
            description: "desc",
            preview: "password 1234",
            createdAtUtc: DateTimeOffset.Parse("2026-06-12T10:00:00Z"));

        Assert.AreEqual("[REDACTED]", request.Preview);
        Assert.IsTrue(request.RequiresApproval);
    }

    [TestMethod]
    public void Approval_Decision_Never_Allows_Execution_In_V0()
    {
        var request = BusinessFlowDemoFixture.CreateSendMessageApproval();
        var decision = ApprovalPolicy.Decide(request, ApprovalDecisionKinds.Approved, "reviewed");

        Assert.AreEqual(ApprovalDecisionKinds.Approved, decision.Decision);
        Assert.IsFalse(decision.ExecutionAllowed);
        StringAssert.Contains(string.Join(" ", decision.Notes), "no safe action executor exists");
    }

    [TestMethod]
    public void Business_Flow_Demo_Creates_Send_Message_Approval()
    {
        var request = BusinessFlowDemoFixture.CreateSendMessageApproval();

        Assert.AreEqual(ApprovalActionKinds.Send, request.ActionKind);
        Assert.AreEqual(ApprovalRiskLevels.Critical, request.RiskLevel);
        Assert.IsTrue(request.RequiresApproval);
        StringAssert.Contains(request.Preview, "presupuesto demo");
        StringAssert.Contains(string.Join(" ", request.Notes), "no WhatsApp send action is executed");
    }
}
