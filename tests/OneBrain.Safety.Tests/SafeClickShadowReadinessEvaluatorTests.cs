using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickShadowReadinessEvaluatorTests
{
    [TestMethod]
    public void ShadowReadinessDetectsApprovalV2()
    {
        var manifest = ApprovalManifestBuilder.Build(ClickPreflightEvaluator.Evaluate("Categorias"), "controlled");
        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.None, StepState.Blocked, FailureKind.PolicyDenied, "MissingManifest"),
            observedIdentity: null);

        Assert.AreEqual("ApprovalV2", readiness.Reason);
        Assert.IsFalse(readiness.HasApprovalV3);
        Assert.IsFalse(readiness.EligibleForFsm);
    }

    [TestMethod]
    public void ShadowReadinessDetectsWeakIdentity()
    {
        var weakIdentity = CreateWeakIdentity();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(weakIdentity, "web-uia", null));

        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.Weak, StepState.Blocked, FailureKind.PolicyDenied, "ContractInvalid"),
            observedIdentity: weakIdentity,
            invokePatternAvailable: true);

        Assert.AreEqual("WeakIdentity", readiness.Reason);
        Assert.AreEqual(IdentityStrength.Weak, readiness.IdentityStrength);
        Assert.IsFalse(readiness.EligibleForFsm);
    }

    [TestMethod]
    public void ShadowReadinessDetectsStrongIdentity()
    {
        var strongIdentity = CreateStrongIdentity();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(strongIdentity, "web-uia", null));

        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.Strong, StepState.Bound, null, null),
            observedIdentity: strongIdentity,
            invokePatternAvailable: true);

        Assert.AreEqual("Ready", readiness.Reason);
        Assert.IsTrue(readiness.Success);
        Assert.IsTrue(readiness.EligibleForFsm);
        Assert.AreEqual(RuntimeIdentityMatch.Same, readiness.RuntimeIdentityMatch);
    }

    [TestMethod]
    public void ShadowReadinessDetectsMissingTargetObserve()
    {
        var manifest = ApprovalManifestBuilder.Build(ClickPreflightEvaluator.Evaluate("Categorias"), "controlled");
        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.None, StepState.Blocked, FailureKind.PolicyDenied, "MissingManifest"),
            observedIdentity: null);

        Assert.IsFalse(readiness.HasTargetObserve);
    }

    [TestMethod]
    public void ShadowReadinessDetectsRuntimeIdMissing()
    {
        var strongIdentity = CreateStrongIdentity();
        var observedWeak = CreateWeakIdentity();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(strongIdentity, "web-uia", null));

        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.Strong, StepState.Bound, null, null),
            observedIdentity: observedWeak,
            invokePatternAvailable: true);

        Assert.AreEqual("RuntimeIdMissing", readiness.Reason);
        Assert.IsFalse(readiness.HasRuntimeId);
        Assert.AreEqual(RuntimeIdentityMatch.Missing, readiness.RuntimeIdentityMatch);
    }

    [TestMethod]
    public void ShadowReadinessDetectsRuntimeIdPresent()
    {
        var strongIdentity = CreateStrongIdentity();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(strongIdentity, "web-uia", null));

        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.Strong, StepState.Bound, null, null),
            observedIdentity: strongIdentity,
            invokePatternAvailable: true);

        Assert.IsTrue(readiness.HasRuntimeId);
        Assert.AreEqual(RuntimeIdentityMatch.Same, readiness.RuntimeIdentityMatch);
    }

    [TestMethod]
    public void ShadowReadinessFlagsWouldRequireLegacy()
    {
        var strongIdentity = CreateStrongIdentity();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(strongIdentity, "web-uia", null));

        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.Strong, StepState.Blocked, FailureKind.PolicyDenied, "PolicyDenied", wouldUseUnsafeFallback: true),
            observedIdentity: strongIdentity,
            invokePatternAvailable: false);

        Assert.IsTrue(readiness.WouldRequireLegacy);
        Assert.IsFalse(readiness.EligibleForFsm);
    }

    [TestMethod]
    public void ShadowReadinessFlagsUnsafeFallback()
    {
        var strongIdentity = CreateStrongIdentity();
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            new ApprovedIdentityInput(strongIdentity, "web-uia", null));

        var readiness = SafeClickShadowReadinessEvaluator.Evaluate(
            manifest,
            CreatePlan(IdentityStrength.Strong, StepState.Blocked, FailureKind.PolicyDenied, "PolicyDenied", wouldUseUnsafeFallback: true),
            observedIdentity: strongIdentity,
            invokePatternAvailable: false,
            usesElClick: true);

        Assert.IsTrue(readiness.WouldUseUnsafeFallback);
        Assert.AreEqual(1, readiness.Metrics.UsesElClick);
        Assert.AreEqual(1, readiness.Metrics.InvokePatternUnavailable);
    }

    private static SafeClickExecutionPlan CreatePlan(
        IdentityStrength identityStrength,
        StepState projectedState,
        FailureKind? failureKind,
        string? blockReason,
        bool wouldUseUnsafeFallback = false)
    {
        return new SafeClickExecutionPlan(
            ProjectedState: projectedState,
            FailureKind: failureKind,
            BlockReason: blockReason,
            IdentityStrength: identityStrength,
            ContractValid: projectedState == StepState.Bound,
            BindingVerdict: projectedState == StepState.Bound ? "Same" : null,
            ParityAgrees: true,
            WouldDispatch: false,
            WouldUseUnsafeFallback: wouldUseUnsafeFallback,
            Reasons: blockReason == null ? ["ready"] : [blockReason]);
    }

    private static ElementIdentity CreateStrongIdentity()
    {
        return new ElementIdentity
        {
            RuntimeId = "42.1.9",
            AutomationId = "categories-button",
            Name = "Categorias",
            Role = "Button",
            ControlType = "Button",
            ClassName = "Chrome_RenderWidgetHostHWND",
            FrameworkId = "UIA",
            AncestorPath = "Window:ONE Brain > Pane:Catalog",
            ProcessName = "msedge",
            WindowTitle = "ONE Brain",
            BoundsHint = "10,10,120,24",
            Provenance = Provenance.Uia
        };
    }

    private static ElementIdentity CreateWeakIdentity()
    {
        return new ElementIdentity
        {
            RuntimeId = "",
            AutomationId = "categories-button",
            Name = "Categorias",
            Role = "Button",
            ControlType = "Button",
            ClassName = "Chrome_RenderWidgetHostHWND",
            FrameworkId = "UIA",
            AncestorPath = "Window:ONE Brain > Pane:Catalog",
            ProcessName = "msedge",
            WindowTitle = "ONE Brain",
            BoundsHint = "10,10,120,24",
            Provenance = Provenance.Inferred
        };
    }
}
