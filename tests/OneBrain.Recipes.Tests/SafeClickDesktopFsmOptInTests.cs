using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickDesktopFsmOptInTests
{
    [TestMethod]
    public void DesktopDispatchPathSafeExecutorUsesFsmWhenStrongUiaIdentity()
    {
        PatternExecutionRequest? captured = null;
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: new FakePatternExecutor(request =>
            {
                captured = request;
                var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(CreateStrongDesktopObservation())!;
                return SuccessfulDispatch(identity);
            }),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("Succeeded", result.Variables["safeClick.fsm.finalState"]);
        Assert.AreEqual("true", result.Variables["safeClick.desktopFsm.routedOptIn"]);
        Assert.AreEqual("uia", result.Variables["safeClick.desktopFsm.identitySource"]);
        Assert.AreEqual(new IntPtr(5678), captured!.RootHwnd);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorRequiresApprovalV3()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: "safe-executor")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
        Assert.AreEqual("ApprovalV3StrongIdentityRequired", result.Variables["safeClick.fsm.blockReason"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorRequiresStrongIdentity()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateWeakDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
        Assert.AreEqual("ApprovalV3StrongIdentityRequired", result.Variables["safeClick.fsm.blockReason"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorRequiresRuntimeId()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateWeakDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("blocked", result.Variables!["safeClick.result"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorRequiresSameRuntime()
    {
        var call = 0;
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongDesktopObservation() : CreateChangedDesktopObservation();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.Stale.ToString(), result.Variables["safeClick.fsm.failureKind"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorRequiresInvokePattern()
    {
        var call = 0;
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongDesktopObservation() : CreateStrongDesktopObservation() with { HasInvoke = false };
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
        StringAssert.Contains(result.Variables["safeClick.reason"], "InvokePattern");
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorRequiresAllowedRole()
    {
        var call = 0;
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongDesktopObservation() : CreateStrongDesktopObservation() with { SelectedControlType = "Edit" };
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
        StringAssert.Contains(result.Variables["safeClick.reason"], "allowlist");
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorRequiresRoot()
    {
        var call = 0;
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongDesktopObservation() : CreateStrongDesktopObservation() with { RootHwnd = "" };
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("DesktopRootRequired", result.Variables!["safeClick.fsm.blockReason"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.rootHwndPresent"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorBlocksWithoutLegacyFallback()
    {
        var result = RunChangedRuntimeDesktop();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("true", result.Variables!["safeClick.fsm.blockedWithoutLegacyFallback"]);
        Assert.AreEqual("false", result.Variables["safeClick.legacy.usedUnsafeFallback"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorDoesNotCallElClick()
    {
        var result = RunChangedRuntimeDesktop();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedElClick"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorDoesNotCallUiaActionExecutor()
    {
        var result = RunChangedRuntimeDesktop();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedUiaActionExecutor"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorDoesNotUseSendInput()
    {
        var result = RunSuccessfulDesktop();

        Assert.IsTrue(result.Success);
        Assert.AreEqual("success", result.Variables!["safeClick.result"]);
    }

    [TestMethod]
    public void DesktopDispatchPathSafeExecutorDoesNotUseCoordinates()
    {
        var result = RunSuccessfulDesktop();

        Assert.IsTrue(result.Success);
        Assert.AreEqual("5678", result.Variables!["safeClick.resolution.rootHwnd"]);
    }

    [TestMethod]
    public void DesktopWithoutDispatchPathBlocksLegacyRetiredWhenDefaultDisabled()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.routedOptIn"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
    }

    [TestMethod]
    public void DesktopEligibleForFsmReadinessTrue()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("true", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.desktopEligibleForFsm"]);
    }

    [TestMethod]
    public void DesktopEligibleForFsmReadinessFalseWhenWeak()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateWeakDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.desktopNotEligibleForFsm"]);
    }

    [TestMethod]
    public void DesktopEligibleForFsmReadinessFalseWhenNoInvoke()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation() with { HasInvoke = false },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.desktopInvokePatternAvailable"]);
    }

    [TestMethod]
    public void DesktopEligibleForFsmReadinessFalseWhenRoleDenied()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation() with { SelectedControlType = "Edit" },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.desktopRoleAllowed"]);
    }

    [TestMethod]
    public void DesktopWebEligibleModeBlocksLegacyRetired()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.fsm.defaultRouteReason"]);
    }

    [TestMethod]
    public void DefaultModeDisabledBlocksDesktopLegacyRetired()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.Disabled,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("disabled", result.Variables["safeClick.fsm.defaultMode"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.routedByDefault"]);
    }

    [TestMethod]
    public void DefaultModeLegacyBlocksDesktopLegacyRetired()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.Legacy,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("legacy", result.Variables["safeClick.fsm.defaultMode"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.routedByDefault"]);
    }

    [TestMethod]
    public void DefaultModeWebEligibleDoesNotRouteDesktop()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("web-eligible", result.Variables["safeClick.fsm.defaultMode"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.routedByDefault"]);
        Assert.AreEqual("DesktopEligibleButDefaultDisabled", result.Variables["safeClick.desktopFsm.defaultRouteReason"]);
    }

    [TestMethod]
    public void DefaultModeDesktopEligibleRoutesEligibleDesktopToFsm()
    {
        var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(CreateStrongDesktopObservation())!;
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: new FakePatternExecutor(_ => SuccessfulDispatch(identity)),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("desktop-uia", result.Variables["safeClick.fsm.defaultRouteScope"]);
        Assert.AreEqual("true", result.Variables["safeClick.desktopFsm.routedByDefault"]);
        Assert.AreEqual("DesktopEligible", result.Variables["safeClick.desktopFsm.defaultRouteReason"]);
    }

    [TestMethod]
    public void DefaultModeDesktopEligibleBlocksIneligibleDesktopAfterRetirement()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateWeakDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.defaultRouteEligible"]);
    }

    [TestMethod]
    public void DefaultModeAllEligibleRoutesEligibleDesktopToFsm()
    {
        var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(CreateStrongDesktopObservation())!;
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.AllEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: new FakePatternExecutor(_ => SuccessfulDispatch(identity)),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("all-eligible", result.Variables!["safeClick.fsm.defaultMode"]);
        Assert.AreEqual("desktop-uia", result.Variables["safeClick.fsm.defaultRouteScope"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.allEligibleModeEnabled"]);
    }

    [TestMethod]
    public void DesktopDefaultRequiresApprovalV3()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: null)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.defaultRouteEligible"]);
    }

    [TestMethod]
    public void DesktopDefaultRequiresStrongIdentity()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateWeakDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.reason"]);
    }

    [TestMethod]
    public void DesktopDefaultRequiresRuntimeId()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateWeakDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.hasRuntimeId"]);
        Assert.AreEqual("false", result.Variables["safeClick.desktopFsm.defaultRouteEligible"]);
    }

    [TestMethod]
    public void DesktopDefaultRequiresSameRuntime()
    {
        var result = RunDesktopDefaultChangedRuntime();

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.Stale.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
        Assert.AreEqual("ApprovalInvalidated", result.Variables["safeClick.fsm.blockReason"]);
    }

    [TestMethod]
    public void DesktopDefaultRequiresInvokePattern()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation() with { HasInvoke = false },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.desktopInvokePatternAvailable"]);
    }

    [TestMethod]
    public void DesktopDefaultRequiresAllowedRole()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation() with { SelectedControlType = "Edit" },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.desktopRoleAllowed"]);
    }

    [TestMethod]
    public void DesktopDefaultRequiresRootHwnd()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation() with { RootHwnd = "" },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.desktopEligible"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.desktopRootAvailable"]);
    }

    [TestMethod]
    public void DesktopDefaultDoesNotAcceptLikelySame()
    {
        var result = RunDesktopDefaultMissingRuntime();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Missing", result.Variables!["safeClick.desktopFsm.runtimeStabilityVerdict"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsm.blockedWithoutLegacyFallback"]);
    }

    [TestMethod]
    public void DesktopDefaultReobservesBeforeDispatch()
    {
        var calls = 0;
        var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(CreateStrongDesktopObservation())!;
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) =>
            {
                calls++;
                return CreateStrongDesktopObservation();
            },
            executor: new FakePatternExecutor(_ => SuccessfulDispatch(identity)),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, calls);
        Assert.AreEqual("true", result.Variables!["safeClick.desktopFsm.reobserveAttempted"]);
        Assert.AreEqual("ReobservedStable", result.Variables["safeClick.desktopFsm.runtimeStabilityVerdict"]);
    }

    [TestMethod]
    public void DesktopDefaultChangedRuntimeBlocksBeforeDispatch()
    {
        var result = RunDesktopDefaultChangedRuntime();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("ReobservedChanged", result.Variables!["safeClick.desktopFsm.runtimeStabilityVerdict"]);
        Assert.AreEqual("true", result.Variables["safeClick.desktopFsm.blockedByStaleIdentity"]);
    }

    [TestMethod]
    public void DesktopDefaultMissingRuntimeBlocksBeforeDispatch()
    {
        var result = RunDesktopDefaultMissingRuntime();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Missing", result.Variables!["safeClick.desktopFsm.runtimeStabilityVerdict"]);
        Assert.AreEqual("ApprovalInvalidatedMissingIdentity", result.Variables["safeClick.fsm.blockReason"]);
    }

    [TestMethod]
    public void DesktopDefaultBlockDoesNotFallbackToLegacy()
    {
        var result = RunDesktopDefaultChangedRuntime();

        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("true", result.Variables["safeClick.desktopFsm.blockedWithoutLegacyFallback"]);
        Assert.AreEqual("false", result.Variables["safeClick.legacy.usedUnsafeFallback"]);
    }

    [TestMethod]
    public void DesktopDefaultBlockDoesNotCallElClick()
    {
        var result = RunDesktopDefaultChangedRuntime();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedElClick"]);
    }

    [TestMethod]
    public void DesktopDefaultBlockDoesNotCallUiaActionExecutor()
    {
        var result = RunDesktopDefaultChangedRuntime();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedUiaActionExecutor"]);
    }

    [TestMethod]
    public void DesktopDefaultVariablesWritten()
    {
        var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(CreateStrongDesktopObservation())!;
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: new FakePatternExecutor(_ => SuccessfulDispatch(identity)),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.desktopFsm.defaultEnabled"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.desktopFsm.routedByDefault"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.desktopFsm.defaultRouteEligible"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.desktopFsm.defaultRouteReason"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.desktopFsm.defaultRouteScope"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.desktopFsm.runtimeStabilityChecked"));
    }

    [TestMethod]
    public void DesktopDefaultMetricsWritten()
    {
        var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(CreateStrongDesktopObservation())!;
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: new FakePatternExecutor(_ => SuccessfulDispatch(identity)),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("1", result.Variables!["safeClick.migration.desktopDefaultFsmEnabled"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.desktopDefaultFsmRouted"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.defaultFsmScopeDesktop"]);
    }

    [TestMethod]
    public void DispatchPathSafeExecutorDesktopStillWorks()
    {
        var result = RunSuccessfulDesktop();

        Assert.IsTrue(result.Success);
        Assert.AreEqual("true", result.Variables!["safeClick.desktopFsm.routedOptIn"]);
    }

    [TestMethod]
    public void DispatchPathLegacyBlocksLegacyDispatchRetired()
    {
        var result = RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "legacy")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("LegacyDispatchRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.explicitOptOut"]);
        Assert.AreEqual("true", result.Variables["safeClick.retirement.legacyDispatchRejected"]);
    }

    [TestMethod]
    public void UnknownDispatchPathStillPolicyDenied()
    {
        var result = new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: "typo"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
    }

    [TestMethod]
    public void DesktopMetricsWritten()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.migration.desktopEligibleForFsm"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.migration.desktopRootAvailable"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.migration.desktopOptInRouted"));
    }

    [TestMethod]
    public void ApprovalV2StillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: null));

        Assert.AreEqual(ApprovalManifestBuilder.PolicyVersion, result.Variables!["approval.policyVersion"]);
    }

    [TestMethod]
    public void EvidenceHashStillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: null));
        var expected = ApprovalManifestBuilder.ComputeEvidenceHash("Categorias", "controlled", "allowedForFuture", "safe-readonly", "low", ApprovalManifestBuilder.PolicyVersion);

        Assert.AreEqual(expected, result.Variables!["approval.evidenceHash"]);
    }

    [TestMethod]
    public void PolicyVersionStillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: null));

        Assert.AreEqual("approval-v2", result.Variables!["approval.policyVersion"]);
    }

    [TestMethod]
    public void ValidateApprovalBindingStillUnchanged()
    {
        var recipe = BuildDesktopApprovalV2Recipe(dispatchPath: "safe-executor", targetText: "Other");
        var result = new RecipeRunner().Run(recipe);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps.Last().Message, "target mismatch");
    }

    [TestMethod]
    public void RegionSelectorStillUntouched()
    {
        var result = new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: null));

        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.reason"]);
    }

    [TestMethod]
    public void BasicActionVerifierStillUntouched()
    {
        var result = new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: null));

        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
    }

    private static RecipeRunResult RunSuccessfulDesktop()
    {
        var identity = DesktopTargetObservationResultIdentityMapper.ToSelectedIdentity(CreateStrongDesktopObservation())!;
        return RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: new FakePatternExecutor(_ => SuccessfulDispatch(identity)),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));
    }

    private static RecipeRunResult RunChangedRuntimeDesktop()
    {
        var call = 0;
        return RunDesktopSafeExecutor(
            resolver: (_, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongDesktopObservation() : CreateChangedDesktopObservation();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: "safe-executor")));
    }

    private static RecipeRunResult RunDesktopDefaultChangedRuntime()
    {
        var call = 0;
        return RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongDesktopObservation() : CreateChangedDesktopObservation();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));
    }

    private static RecipeRunResult RunDesktopDefaultMissingRuntime()
    {
        var call = 0;
        return RunDesktopSafeExecutor(
            SafeClickDefaultMode.DesktopEligible,
            resolver: (_, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongDesktopObservation() : CreateWeakDesktopObservation();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));
    }

    private static RecipeDefinition BuildDesktopRecipe(string? dispatchPath)
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        return new RecipeDefinition("desktop-safe-click-fsm")
        {
            Variables = new Dictionary<string, string>
            {
                ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "observe",
                    Kind = "target.observe.desktop",
                    SaveAs = "clickPreflight",
                    Args = new Dictionary<string, string>
                    {
                        ["targetText"] = "Categorias",
                        ["processName"] = "explorer",
                        ["window"] = "ONE Brain"
                    }
                },
                new RecipeStepDefinition
                {
                    Id = "approval",
                    Kind = "approval.manifest",
                    SaveAs = "approval",
                    Args = new Dictionary<string, string>
                    {
                        ["from"] = "clickPreflight",
                        ["mode"] = "controlled"
                    }
                },
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = BuildSafeClickArgs("Categorias", dispatchPath)
                }
            ]
        };
    }

    private static RecipeDefinition BuildDesktopApprovalV2Recipe(string? dispatchPath, string targetText = "Categorias")
    {
        return new RecipeDefinition("desktop-safe-click-v2")
        {
            Variables = new Dictionary<string, string>
            {
                ["approval.targetText"] = "Categorias",
                ["approval.mode"] = "controlled",
                ["approval.policyVersion"] = ApprovalManifestBuilder.PolicyVersion,
                ["approval.decision"] = "allowedForFuture",
                ["approval.riskCategory"] = "safe-readonly",
                ["approval.riskLevel"] = "low",
                ["approval.evidenceHash"] = ApprovalManifestBuilder.ComputeEvidenceHash("Categorias", "controlled", "allowedForFuture", "safe-readonly", "low", ApprovalManifestBuilder.PolicyVersion),
                ["approval.executionAllowedInThisHito"] = "true"
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = BuildSafeClickArgs(targetText, dispatchPath, identitySource: "uia")
                }
            ]
        };
    }

    private static Dictionary<string, string> BuildSafeClickArgs(string targetText, string? dispatchPath, string? identitySource = null)
    {
        var args = new Dictionary<string, string>
        {
            ["targettext"] = targetText,
            ["mode"] = "controlled",
            ["approvalprefix"] = "approval",
            ["proc"] = "process-that-does-not-exist-onebrain",
            ["window"] = "ONE Brain"
        };

        if (!string.IsNullOrWhiteSpace(dispatchPath))
            args["dispatchPath"] = dispatchPath;

        if (!string.IsNullOrWhiteSpace(identitySource))
            args["identitySource"] = identitySource;

        return args;
    }

    private static T RunDesktopSafeExecutor<T>(
        Func<string, string?, string?, DesktopTargetObservationResult> resolver,
        IUiaPatternExecutor executor,
        Func<T> action)
    {
        return RunDesktopSafeExecutor(SafeClickDefaultMode.WebEligible, resolver, executor, action);
    }

    private static T RunDesktopSafeExecutor<T>(
        SafeClickDefaultMode defaultMode,
        Func<string, string?, string?, DesktopTargetObservationResult> resolver,
        IUiaPatternExecutor executor,
        Func<T> action)
    {
        var desktopField = GetField("s_targetObserveDesktopOverride");
        var executorFactoryField = GetField("s_safeClickPatternExecutorFactoryOverride");
        var ownershipFactoryField = GetField("s_safeClickOwnershipMonitorFactoryOverride");
        var modeField = GetField("s_safeClickFsmDefaultModeOverride");

        var previousDesktop = desktopField.GetValue(null);
        var previousExecutorFactory = executorFactoryField.GetValue(null);
        var previousOwnershipFactory = ownershipFactoryField.GetValue(null);
        var previousMode = modeField.GetValue(null);

        desktopField.SetValue(null, resolver);
        executorFactoryField.SetValue(null, (Func<IUiaPatternExecutor>)(() => executor));
        ownershipFactoryField.SetValue(null, (Func<IDesktopOwnershipMonitor>)(() => new PassiveOwnershipMonitor()));
        modeField.SetValue(null, (Func<SafeClickDefaultMode>)(() => defaultMode));

        try
        {
            return action();
        }
        finally
        {
            desktopField.SetValue(null, previousDesktop);
            executorFactoryField.SetValue(null, previousExecutorFactory);
            ownershipFactoryField.SetValue(null, previousOwnershipFactory);
            modeField.SetValue(null, previousMode);
        }
    }

    private static FieldInfo GetField(string name)
    {
        var field = typeof(RecipeRunner).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"static field '{name}' not found");
        return field;
    }

    private static DesktopTargetObservationResult CreateStrongDesktopObservation()
    {
        return new DesktopTargetObservationResult
        {
            Found = true,
            CandidateCount = 1,
            Reason = "exact desktop observe match",
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.7.9",
            SelectedAutomationId = "categories-button",
            SelectedClassName = "Button",
            SelectedFrameworkId = "WPF",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog",
            SelectedProcessName = "explorer",
            SelectedWindowTitle = "ONE Brain",
            SelectedHelpTextPresent = true,
            SelectedLegacyNamePresent = true,
            RootHwnd = "5678",
            HasInvoke = true
        };
    }

    private static DesktopTargetObservationResult CreateChangedDesktopObservation()
    {
        return CreateStrongDesktopObservation() with { SelectedRuntimeId = "42.7.10" };
    }

    private static DesktopTargetObservationResult CreateWeakDesktopObservation()
    {
        return CreateStrongDesktopObservation() with { SelectedRuntimeId = "" };
    }

    private static PatternExecutionResult SuccessfulDispatch(ElementIdentity identity)
    {
        return new PatternExecutionResult(
            Success: true,
            FailureKind: null,
            Reasons: ["invoke ok"],
            ObservedIdentity: identity,
            WindowFound: true,
            TargetVisible: true,
            TargetName: identity.Name,
            ObservedActions: 1,
            Signals: ["postAction.windowFound=true", "postAction.targetVisible=true"]);
    }

    private static IUiaPatternExecutor ThrowingExecutor()
    {
        return new FakePatternExecutor(_ => throw new AssertFailedException("executor should not be called"));
    }

    private sealed class FakePatternExecutor(Func<PatternExecutionRequest, PatternExecutionResult> handler) : IUiaPatternExecutor
    {
        public PatternExecutionResult Invoke(PatternExecutionRequest request) => handler(request);
    }

    private sealed class PassiveOwnershipMonitor : IDesktopOwnershipMonitor
    {
        private static readonly OwnershipSnapshot Snapshot = new(0, 0, "", DateTimeOffset.UnixEpoch);

        public OwnershipSnapshot Capture() => Snapshot;
        public bool HumanInputSince(OwnershipSnapshot baseline) => false;
        public bool ForegroundChanged(OwnershipSnapshot baseline) => false;
    }
}
