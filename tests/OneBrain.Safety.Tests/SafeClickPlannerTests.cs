using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;
using OneBrain.Core.Selectors.Web;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickPlannerTests
{
    [TestMethod]
    public void V2ManifestWithoutIdentityProjectsBlocked()
    {
        var manifest = ApprovalManifestBuilder.Build(ClickPreflightEvaluator.Evaluate("Categorias"), "controlled");

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.AreEqual(FailureKind.PolicyDenied, plan.FailureKind);
        Assert.IsFalse(plan.ContractValid);
        Assert.IsFalse(plan.WouldDispatch);
    }

    [TestMethod]
    public void V3WeakWebIdentityProjectsBlockedForIrreversible()
    {
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            CreateWeakIdentityInput());

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Candidates =
            [
                new WebCandidate
                {
                    Name = "Categorias",
                    ControlType = "Button",
                    AutomationId = "categories-button",
                    HasInvoke = true
                }
            ]
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.AreEqual(IdentityStrength.Weak, plan.IdentityStrength);
        Assert.IsFalse(plan.WouldDispatch);
    }

    [TestMethod]
    public void V3StrongIdentityProjectsBoundButNeverDispatches()
    {
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            CreateStrongIdentityInput());

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Candidates =
            [
                new WebCandidate
                {
                    RuntimeId = "42.1.9",
                    Name = "Categorias",
                    ControlType = "Button",
                    AutomationId = "categories-button",
                    HasInvoke = true
                }
            ]
        });

        Assert.AreEqual(StepState.Bound, plan.ProjectedState);
        Assert.AreEqual(IdentityStrength.Strong, plan.IdentityStrength);
        Assert.IsTrue(plan.ContractValid);
        Assert.IsFalse(plan.WouldDispatch);
    }

    [TestMethod]
    public void ReversibleLikelySameBlocksForSafeClick()
    {
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            CreateStrongIdentityInput());

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Reversible = true,
            Candidates =
            [
                new WebCandidate
                {
                    RuntimeId = "new-runtime",
                    Name = "Categorias",
                    ControlType = "Button",
                    AutomationId = "categories-button",
                    HasInvoke = true
                }
            ]
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.IsFalse(plan.ContractValid);
        CollectionAssert.Contains(plan.Reasons.ToList(), "ClickMustBeIrreversible");
        Assert.IsFalse(plan.WouldDispatch);
    }

    [TestMethod]
    public void CandidateWithoutInvokeMarksWouldUseUnsafeFallback()
    {
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            CreateStrongIdentityInput());

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Candidates =
            [
                new WebCandidate
                {
                    RuntimeId = "42.1.9",
                    Name = "Categorias",
                    ControlType = "Button",
                    AutomationId = "categories-button",
                    HasInvoke = false
                }
            ]
        });

        Assert.AreEqual(StepState.Bound, plan.ProjectedState);
        Assert.IsTrue(plan.WouldUseUnsafeFallback);
        Assert.IsFalse(plan.WouldDispatch);
    }

    [TestMethod]
    public void PlannerNeverReferencesExecutor()
    {
        Assert.IsFalse(
            typeof(SafeClickPlanner)
                .GetMethods()
                .SelectMany(method => method.GetParameters())
                .Any(parameter => parameter.ParameterType == typeof(IUiaPatternExecutor)));
    }

    [TestMethod]
    public void PlannerMatchesContractValidatorFailure()
    {
        var manifest = new ApprovalManifest
        {
            TargetText = "Categorias",
            Mode = "controlled",
            PolicyVersion = ApprovalManifestBuilder.PolicyVersion,
            EvidenceHash = "hash"
        };

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.AreEqual(FailureKind.PolicyDenied, plan.FailureKind);
        Assert.AreEqual("ContractInvalid", plan.BlockReason);
    }

    [TestMethod]
    public void PlannerMatchesApprovalBindingValidatorFailure()
    {
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            CreateStrongIdentityInput());

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Candidates =
            [
                new WebCandidate
                {
                    RuntimeId = "wrong-runtime",
                    Name = "Categorias",
                    ControlType = "Button",
                    AutomationId = "other-button",
                    HasInvoke = true
                }
            ]
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.AreEqual(FailureKind.Stale, plan.FailureKind);
        Assert.AreEqual("ApprovalInvalidated", plan.BlockReason);
    }

    [TestMethod]
    public void EmptyCandidatesProjectBlocked()
    {
        var manifest = ApprovalManifestBuilder.Build(
            ClickPreflightEvaluator.Evaluate("Categorias"),
            "controlled",
            CreateWeakIdentityInput());

        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = manifest.Mode,
            TargetText = manifest.TargetText,
            Manifest = manifest,
            Candidates = Array.Empty<WebCandidate>()
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.IsFalse(plan.WouldDispatch);
    }

    [TestMethod]
    public void NullOrMissingManifestProjectsBlockedFailClosed()
    {
        var plan = SafeClickPlanner.Plan(new SafeClickPlanInput
        {
            Mode = "controlled",
            TargetText = "Categorias",
            Manifest = null
        });

        Assert.AreEqual(StepState.Blocked, plan.ProjectedState);
        Assert.AreEqual(FailureKind.PolicyDenied, plan.FailureKind);
        Assert.AreEqual(IdentityStrength.None, plan.IdentityStrength);
    }

    private static ApprovedIdentityInput CreateWeakIdentityInput()
    {
        return new ApprovedIdentityInput(
            new ElementIdentity
            {
                Name = "Categorias",
                ControlType = "Button",
                AutomationId = "categories-button",
                BoundsHint = "10,10,120,24",
                Provenance = Provenance.Inferred
            },
            "web-shadow",
            new OneBrain.Core.Selectors.Web.WebSelectorParity
            {
                EngineFound = true,
                EngineVerdict = "LikelySame",
                EngineSelectedName = "Categorias",
                AgreesWithLegacy = true
            });
    }

    private static ApprovedIdentityInput CreateStrongIdentityInput()
    {
        return new ApprovedIdentityInput(
            new ElementIdentity
            {
                RuntimeId = "42.1.9",
                Name = "Categorias",
                ControlType = "Button",
                AutomationId = "categories-button",
                Provenance = Provenance.Uia
            },
            "uia",
            null);
    }
}
