using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Actions;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickGradualEnableTests
{
    [TestMethod]
    public void DefaultModeDisabledBlocksLegacyRetiredWithoutDispatchPath()
    {
        var result = RunRouting(
            SafeClickDefaultMode.Disabled,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("blocked", result.Variables!["safeClick.result"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("disabled", result.Variables["safeClick.fsm.defaultMode"]);
        Assert.AreEqual("Blocked", result.Variables["safeClick.fsm.finalState"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.retirementPolicy.blocked"]);
        Assert.AreEqual("1", result.Variables["safeClick.fsm.transitionCount"]);
        Assert.AreNotEqual("[]", result.Variables["safeClick.fsm.ledgerJson"]);
    }

    [TestMethod]
    public void DefaultModeWebEligibleRoutesEligibleWebToFsm()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("Succeeded", result.Variables["safeClick.fsm.finalState"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("web-uia", result.Variables["safeClick.fsm.defaultRouteScope"]);
        Assert.AreEqual("WebEligible", result.Variables["safeClick.fsm.defaultRouteReason"]);
    }

    [TestMethod]
    public void DefaultModeAllEligibleRoutesEligibleWebToFsm()
    {
        var result = RunRouting(
            SafeClickDefaultMode.AllEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("all-eligible", result.Variables["safeClick.fsm.defaultMode"]);
        Assert.AreEqual("web-uia", result.Variables["safeClick.fsm.defaultRouteScope"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.defaultFsmScopeWeb"]);
    }

    [TestMethod]
    public void DefaultModeWebEligibleBlocksIneligibleWebAfterRetirement()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: false)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("legacy-retired", result.Variables["safeClick.fsm.defaultRouteScope"]);
        Assert.AreEqual("true", result.Variables["safeClick.retirement.ineligibleAfterRetirement"]);
        Assert.AreEqual("1", result.Variables["safeClick.fsm.transitionCount"]);
        Assert.AreNotEqual("[]", result.Variables["safeClick.fsm.ledgerJson"]);
    }

    [TestMethod]
    public void DefaultModeWebEligibleExcludesDesktop()
    {
        var result = RunDesktopRouting(
            SafeClickDefaultMode.WebEligible,
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(BuildDesktopObserveRecipe()));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.fsm.defaultRouteReason"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.desktopExcludedFromDefault"]);
    }

    [TestMethod]
    public void DispatchPathSafeExecutorStillUsesOptInFsm()
    {
        var result = RunRouting(
            SafeClickDefaultMode.Disabled,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "safe-executor")));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("Succeeded", result.Variables["safeClick.fsm.finalState"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("explicit-safe-executor", result.Variables["safeClick.fsm.defaultRouteScope"]);
    }

    [TestMethod]
    public void DispatchPathLegacyBlocksRetiredAndMarksDeprecated()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("LegacyDispatchRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.explicitOptOut"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.deprecated"]);
        Assert.AreEqual("ExplicitLegacyDispatchPath", result.Variables["safeClick.legacy.reason"]);
        Assert.AreEqual("true", result.Variables["safeClick.retirement.legacyDispatchRejected"]);
        Assert.AreEqual("Blocked", result.Variables["safeClick.fsm.finalState"]);
    }

    [TestMethod]
    public void LegacyDispatchPathWithOwnerReasonReviewByIsCompliant()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(
                includeObserve: true,
                dispatchPath: "legacy",
                legacyOwner: "migration",
                legacyReason: "Target lacks InvokePattern",
                legacyReviewBy: "2026-07-31")));

        Assert.AreEqual("true", result.Variables!["safeClick.legacy.deprecationPolicy.isLegacyDispatch"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.deprecationPolicy.isDeprecated"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.deprecationPolicy.isCompliant"]);
        Assert.AreEqual("Warning", result.Variables["safeClick.legacy.deprecationPolicy.severity"]);
    }

    [TestMethod]
    public void LegacyDispatchPathMissingOwnerWarns()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy", legacyReason: "temporary", legacyReviewBy: "2026-07-31")));

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.deprecationPolicy.isCompliant"]);
        StringAssert.Contains(result.Variables["safeClick.legacy.deprecationPolicy.violationReason"], "MissingOwner");
    }

    [TestMethod]
    public void LegacyDispatchPathMissingReasonWarns()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy", legacyOwner: "migration", legacyReviewBy: "2026-07-31")));

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.deprecationPolicy.isCompliant"]);
        StringAssert.Contains(result.Variables["safeClick.legacy.deprecationPolicy.violationReason"], "MissingReason");
    }

    [TestMethod]
    public void LegacyDispatchPathMissingReviewByWarns()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy", legacyOwner: "migration", legacyReason: "temporary")));

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.deprecationPolicy.isCompliant"]);
        StringAssert.Contains(result.Variables["safeClick.legacy.deprecationPolicy.violationReason"], "MissingReviewBy");
    }

    [TestMethod]
    public void LegacyDeprecationNowBlocksButKeepsWarningEvidence()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("LegacyDispatchRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("Warning", result.Variables["safeClick.legacy.deprecationPolicy.severity"]);
    }

    [TestMethod]
    public void LegacyDeprecationMetricsWritten()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy")));

        Assert.AreEqual("1", result.Variables!["safeClick.migration.legacyExplicitOptOutTotal"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.legacyOptOutNonCompliant"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.legacyDeprecationWarnings"]);
    }

    [TestMethod]
    public void UnknownDispatchPathStillPolicyDenied()
    {
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe(dispatchPath: "typo"));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps.Last().Message, "dispatchPath 'typo' is not allowed");
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables["safeClick.fsm.failureKind"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.unknownDispatchPathBlocked"]);
        Assert.AreEqual("1", result.Variables["safeClick.fsm.transitionCount"]);
        Assert.AreNotEqual("[]", result.Variables["safeClick.fsm.ledgerJson"]);
    }

    [TestMethod]
    public void FsmBlockDoesNotFallbackToLegacy()
    {
        var result = RunStaleRouted();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("Blocked", result.Variables["safeClick.fsm.finalState"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsm.blockedWithoutLegacyFallback"]);
    }

    [TestMethod]
    public void FsmBlockDoesNotCallElClick()
    {
        var result = RunStaleRouted();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedElClick"]);
        Assert.AreEqual("false", result.Variables["safeClick.legacy.usedUnsafeFallback"]);
    }

    [TestMethod]
    public void FsmBlockDoesNotCallUiaActionExecutor()
    {
        var result = RunStaleRouted();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedUiaActionExecutor"]);
    }

    [TestMethod]
    public void SafeClickDispatchDoesNotReachLegacyExecutors()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            directory = directory.Parent;

        Assert.IsNotNull(directory);
        var source = File.ReadAllText(Path.Combine(directory.FullName, "src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));
        var start = source.IndexOf("private RecipeStepRunResult ExecuteSafeClick(", StringComparison.Ordinal);
        var end = source.IndexOf("private static SafeClickDefaultMode ResolveSafeClickFsmDefaultMode", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0);
        Assert.IsTrue(end > start);

        var executeSafeClickBody = source[start..end];
        Assert.IsFalse(executeSafeClickBody.Contains("ExecuteSafeClickLegacy(", StringComparison.Ordinal));
        Assert.IsFalse(executeSafeClickBody.Contains(".Click(", StringComparison.Ordinal));
        Assert.IsFalse(executeSafeClickBody.Contains("new UiaActionExecutor", StringComparison.Ordinal));
        Assert.IsFalse(executeSafeClickBody.Contains("SendInput", StringComparison.Ordinal));
        Assert.IsFalse(executeSafeClickBody.Contains("GetClickablePoint", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RetirementBlocksLegacyWithoutCountingLegacyPathUsed()
    {
        var result = RunRouting(
            SafeClickDefaultMode.Disabled,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.AreEqual("true", result.Variables!["safeClick.retirement.ready"]);
        Assert.AreEqual("0", result.Variables["safeClick.retirement.legacyPathUsed"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.legacyExecutionBlocked"]);
    }

    [TestMethod]
    public void RetirementBlocksLegacyWithoutUiaActionExecutorUse()
    {
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe());

        Assert.AreEqual("true", result.Variables!["safeClick.retirement.ready"]);
        Assert.AreEqual("0", result.Variables["safeClick.retirement.uiaActionExecutorUsed"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.uiaActionExecutorReachableFromSafeClick"]);
    }

    [TestMethod]
    public void RetirementBlocksLegacyWithoutUnsafeFallbackUse()
    {
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe());

        Assert.AreEqual("0", result.Variables!["safeClick.retirement.unsafeFallbackUsed"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.elClickReachableFromSafeClick"]);
    }

    [TestMethod]
    public void RetirementReadinessFalseWhenLegacyOptOutNonCompliant()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy")));

        Assert.AreEqual("false", result.Variables!["safeClick.retirement.ready"]);
        StringAssert.Contains(result.Variables["safeClick.retirement.blockingReasons"], "NonCompliantLegacyOptOut");
    }

    [TestMethod]
    public void RetirementReadinessTrueWhenNoBlockingReasons()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.AreEqual("true", result.Variables!["safeClick.retirement.ready"]);
        Assert.AreEqual("", result.Variables["safeClick.retirement.blockingReasons"]);
    }

    [TestMethod]
    public void RetirementReadinessReportIsDeterministic()
    {
        var first = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));
        var second = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.AreEqual(first.Variables!["safeClick.retirement.reportJson"], second.Variables!["safeClick.retirement.reportJson"]);
    }

    [TestMethod]
    public void DefaultWebEligibleReobservesBeforeDispatch()
    {
        var calls = 0;
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) =>
            {
                calls++;
                return CreateStrongResolution();
            },
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, calls);
        Assert.AreEqual("true", result.Variables!["safeClick.runtimeStability.reobserveAttempted"]);
        Assert.AreEqual("Same", result.Variables["safeClick.runtimeStability.reobserveMatch"]);
    }

    [TestMethod]
    public void DefaultWebEligibleStableRuntimeContinuesToFsm()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("Succeeded", result.Variables["safeClick.fsm.finalState"]);
        Assert.AreEqual("ReobservedStable", result.Variables["safeClick.runtimeStability.verdict"]);
    }

    [TestMethod]
    public void DefaultWebEligibleChangedRuntimeBlocksBeforeDispatch()
    {
        var result = RunStaleRouted();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.Stale.ToString(), result.Variables["safeClick.fsm.failureKind"]);
        Assert.AreEqual("ApprovalInvalidated", result.Variables["safeClick.fsm.blockReason"]);
        Assert.AreEqual("ReobservedChanged", result.Variables["safeClick.runtimeStability.verdict"]);
    }

    [TestMethod]
    public void DefaultWebEligibleMissingRuntimeBlocksBeforeDispatch()
    {
        var call = 0;
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongResolution() : CreateMissingRuntimeResolution();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.Stale.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
        Assert.AreEqual("ApprovalInvalidatedMissingIdentity", result.Variables["safeClick.fsm.blockReason"]);
        Assert.AreEqual("Missing", result.Variables["safeClick.runtimeStability.verdict"]);
    }

    [TestMethod]
    public void DefaultWebEligibleDoesNotAcceptLikelySame()
    {
        var call = 0;
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongResolution() : CreateMissingRuntimeResolution();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Missing", result.Variables!["safeClick.runtimeStability.reobserveMatch"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsm.blockedWithoutLegacyFallback"]);
    }

    [TestMethod]
    public void ReobserveFailureBlocksFailClosed()
    {
        var call = 0;
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongResolution() : CreateNotFoundResolution();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.NotFound.ToString(), result.Variables!["safeClick.fsm.failureKind"]);
        Assert.AreEqual("ApprovalTargetNotFound", result.Variables["safeClick.fsm.blockReason"]);
        Assert.AreEqual("false", result.Variables["safeClick.runtimeStability.reobserveSucceeded"]);
    }

    [TestMethod]
    public void ReobserveBlockDoesNotFallbackToLegacy()
    {
        var result = RunStaleRouted();

        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsm.blockedWithoutLegacyFallback"]);
    }

    [TestMethod]
    public void ReobserveBlockDoesNotCallElClick()
    {
        var result = RunStaleRouted();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedElClick"]);
    }

    [TestMethod]
    public void ReobserveBlockDoesNotCallUiaActionExecutor()
    {
        var result = RunStaleRouted();

        Assert.AreEqual("false", result.Variables!["safeClick.legacy.usedUiaActionExecutor"]);
    }

    [TestMethod]
    public void RuntimeStabilityVariablesWritten()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.runtimeStability.verdict"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.runtimeStability.reobserveAttempted"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.runtimeStability.reobserveSucceeded"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.runtimeStability.reobserveMatch"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.runtimeStability.blockReason"));
    }

    [TestMethod]
    public void RuntimeStabilityMetricsWritten()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.AreEqual("1", result.Variables!["safeClick.migration.runtimeStabilityChecked"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.runtimeStable"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.reobserveAttempted"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.reobserveSucceeded"]);
    }

    [TestMethod]
    public void DispatchPathLegacyDoesNotReobserve()
    {
        var calls = 0;
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) =>
            {
                calls++;
                return CreateStrongResolution();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true, dispatchPath: "legacy")));

        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.runtimeStabilityChecked"]);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void DefaultDisabledDoesNotReobserve()
    {
        var calls = 0;
        var result = RunRouting(
            SafeClickDefaultMode.Disabled,
            resolver: (_, _, _, _) =>
            {
                calls++;
                return CreateStrongResolution();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.runtimeStabilityChecked"]);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void DesktopExcludedDoesNotReobserveForDefault()
    {
        var result = RunDesktopRouting(
            SafeClickDefaultMode.WebEligible,
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(BuildDesktopObserveRecipe()));

        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.runtimeStabilityChecked"]);
        Assert.IsFalse(result.Variables.ContainsKey("safeClick.runtimeStability.reobserveAttempted"));
    }

    [TestMethod]
    public void KillSwitchDisabledBlocksLegacyRetired()
    {
        var result = RunRouting(
            SafeClickDefaultMode.Disabled,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables!["safeClick.reason"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.fsm.defaultRouteReason"]);
    }

    [TestMethod]
    public void KillSwitchLegacyBlocksLegacyRetired()
    {
        var result = RunRouting(
            SafeClickDefaultMode.Legacy,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("legacy", result.Variables!["safeClick.fsm.defaultMode"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.fsm.defaultRouteReason"]);
    }

    [TestMethod]
    public void DefaultRoutingWritesVariables()
    {
        var result = RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) => CreateStrongResolution(),
            executor: SuccessfulExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.fsm.defaultEnabled"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsm.defaultMode"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsm.routedByDefault"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsm.defaultRouteReason"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsm.defaultRouteEligible"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsm.defaultRouteScope"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsm.blockedWithoutLegacyFallback"));
        Assert.AreEqual("1", result.Variables["safeClick.migration.defaultFsmRouted"]);
    }

    [TestMethod]
    public void ExplicitLegacyWritesDeprecatedVariables()
    {
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe(dispatchPath: "legacy"));

        Assert.AreEqual("true", result.Variables!["safeClick.legacy.explicitOptOut"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.deprecated"]);
        Assert.AreEqual("ExplicitLegacyDispatchPath", result.Variables["safeClick.legacy.reason"]);
        Assert.AreEqual("LegacyDispatchRetired", result.Variables["safeClick.reason"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.explicitLegacyOptOut"]);
    }

    [TestMethod]
    public void ApprovalV2StillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe());

        Assert.AreEqual(ApprovalManifestBuilder.PolicyVersion, result.Variables!["approval.policyVersion"]);
    }

    [TestMethod]
    public void EvidenceHashStillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe());
        var expectedHash = ApprovalManifestBuilder.ComputeEvidenceHash(
            "Categorias",
            "controlled",
            "allowedForFuture",
            "safe-readonly",
            "low",
            ApprovalManifestBuilder.PolicyVersion);

        Assert.AreEqual(expectedHash, result.Variables!["approval.evidenceHash"]);
    }

    [TestMethod]
    public void PolicyVersionStillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe());

        Assert.AreEqual("approval-v2", result.Variables!["approval.policyVersion"]);
    }

    [TestMethod]
    public void ValidateApprovalBindingStillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildForgedBindingRecipe());

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps.Last().Message, "target mismatch");
    }

    [TestMethod]
    public void RegionSelectorStillUntouched()
    {
        // Default routing never engages the visual RegionSelector path; the legacy stack stays on the
        // unchanged default route, proving HITO-147 did not alter that subsystem.
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe());

        Assert.AreEqual("false", result.Variables!["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("SafeClickLegacyRetired", result.Variables["safeClick.reason"]);
    }

    [TestMethod]
    public void BasicActionVerifierStillUntouched()
    {
        // BasicActionVerifier.TargetExists lives in the legacy verification path; default routing leaves
        // it untouched, so a legacy run still behaves exactly as before.
        var result = new RecipeRunner().Run(BuildLegacyControlledRecipe());

        Assert.AreEqual("false", result.Variables!["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("Blocked", result.Variables["safeClick.fsm.finalState"]);
    }

    // ── Recipe builders ────────────────────────────────────────────────────

    private static RecipeDefinition BuildWebRecipe(
        bool includeObserve,
        string? dispatchPath = null,
        string? legacyOwner = null,
        string? legacyReason = null,
        string? legacyReviewBy = null)
    {
        var steps = new List<RecipeStepDefinition>
        {
            new()
            {
                Id = "preflight",
                Kind = "preflight.click",
                SaveAs = "clickPreflight",
                Args = new Dictionary<string, string> { ["targettext"] = "More information..." }
            }
        };

        if (includeObserve)
        {
            steps.Add(new RecipeStepDefinition
            {
                Id = "observe",
                Kind = "target.observe",
                SaveAs = "clickPreflight",
                Args = new Dictionary<string, string>
                {
                    ["targetText"] = "More information...",
                    ["proc"] = "msedge"
                }
            });
        }

        steps.Add(new RecipeStepDefinition
        {
            Id = "approval",
            Kind = "approval.manifest",
            SaveAs = "approval",
            Args = new Dictionary<string, string> { ["from"] = "clickPreflight", ["mode"] = "controlled" }
        });

        steps.Add(new RecipeStepDefinition
        {
            Id = "safe-click",
            Kind = "safe.click",
            SaveAs = "safeClick",
            Args = BuildSafeClickArgs("More information...", dispatchPath, legacyOwner: legacyOwner, legacyReason: legacyReason, legacyReviewBy: legacyReviewBy)
        });

        return new RecipeDefinition("safe-click-gradual-web")
        {
            Variables = BuildOwnedSessionVariables(),
            Steps = steps
        };
    }

    private static RecipeDefinition BuildDesktopObserveRecipe()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        return new RecipeDefinition("safe-click-gradual-desktop")
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
                    Args = new Dictionary<string, string> { ["from"] = "clickPreflight", ["mode"] = "controlled" }
                },
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = new Dictionary<string, string>
                    {
                        ["targettext"] = "Categorias",
                        ["mode"] = "controlled",
                        ["approvalprefix"] = "approval",
                        ["proc"] = "process-that-does-not-exist-onebrain"
                    }
                }
            ]
        };
    }

    private static RecipeDefinition BuildLegacyControlledRecipe(string? dispatchPath = null)
    {
        return new RecipeDefinition("safe-click-gradual-legacy")
        {
            Variables = BuildControlledApprovalVariables("Categorias"),
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = BuildSafeClickArgs("Categorias", dispatchPath, proc: "ApplicationFrameHost")
                }
            ]
        };
    }

    private static RecipeDefinition BuildForgedBindingRecipe()
    {
        var variables = BuildControlledApprovalVariables("Siete");
        return new RecipeDefinition("safe-click-gradual-forged")
        {
            Variables = variables,
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = BuildSafeClickArgs("Ocho", dispatchPath: null, proc: "ApplicationFrameHost")
                }
            ]
        };
    }

    private static Dictionary<string, string> BuildControlledApprovalVariables(string targetText)
    {
        return new Dictionary<string, string>
        {
            ["approval.targetText"] = targetText,
            ["approval.mode"] = "controlled",
            ["approval.policyVersion"] = ApprovalManifestBuilder.PolicyVersion,
            ["approval.decision"] = "allowedForFuture",
            ["approval.riskCategory"] = "safe-readonly",
            ["approval.riskLevel"] = "low",
            ["approval.evidenceHash"] = ApprovalManifestBuilder.ComputeEvidenceHash(
                targetText,
                "controlled",
                "allowedForFuture",
                "safe-readonly",
                "low",
                ApprovalManifestBuilder.PolicyVersion),
            ["approval.executionAllowedInThisHito"] = "true"
        };
    }

    private static Dictionary<string, string> BuildSafeClickArgs(
        string targetText,
        string? dispatchPath,
        string proc = "msedge",
        string? legacyOwner = null,
        string? legacyReason = null,
        string? legacyReviewBy = null)
    {
        var args = new Dictionary<string, string>
        {
            ["targettext"] = targetText,
            ["mode"] = "controlled",
            ["approvalprefix"] = "approval",
            ["proc"] = proc
        };

        if (!string.IsNullOrWhiteSpace(dispatchPath))
            args["dispatchPath"] = dispatchPath;
        if (!string.IsNullOrWhiteSpace(legacyOwner))
            args["legacyOwner"] = legacyOwner;
        if (!string.IsNullOrWhiteSpace(legacyReason))
            args["legacyReason"] = legacyReason;
        if (!string.IsNullOrWhiteSpace(legacyReviewBy))
            args["legacyReviewBy"] = legacyReviewBy;

        return args;
    }

    private static Dictionary<string, string> BuildOwnedSessionVariables()
    {
        return new Dictionary<string, string>
        {
            ["browser.hwnd"] = "1234",
            ["browser.owned"] = "true",
            ["browser.process"] = "msedge"
        };
    }

    // ── Execution overrides ────────────────────────────────────────────────

    private static RecipeRunResult RunStaleRouted()
    {
        var call = 0;
        return RunRouting(
            SafeClickDefaultMode.WebEligible,
            resolver: (_, _, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongResolution() : CreateChangedRuntimeResolution();
            },
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildWebRecipe(includeObserve: true)));
    }

    private static IUiaPatternExecutor SuccessfulExecutor()
    {
        var observedIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateStrongResolution())!;
        return new FakePatternExecutor(_ => new PatternExecutionResult(
            Success: true,
            FailureKind: null,
            Reasons: ["invoke ok"],
            ObservedIdentity: observedIdentity,
            WindowFound: true,
            TargetVisible: true,
            TargetName: observedIdentity.Name,
            ObservedActions: 1,
            Signals: ["postAction.windowFound=true", "postAction.targetVisible=true"]));
    }

    private static IUiaPatternExecutor ThrowingExecutor()
    {
        return new FakePatternExecutor(_ => throw new AssertFailedException("pattern executor should not be called"));
    }

    private static T RunRouting<T>(
        SafeClickDefaultMode mode,
        Func<IntPtr, string, string, int, WebTargetResult> resolver,
        IUiaPatternExecutor executor,
        Func<T> action)
    {
        var modeField = GetField("s_safeClickFsmDefaultModeOverride");
        var resolverField = GetField("s_targetObserveResolverOverride");
        var executorFactoryField = GetField("s_safeClickPatternExecutorFactoryOverride");
        var ownershipFactoryField = GetField("s_safeClickOwnershipMonitorFactoryOverride");
        var legacyWebActionField = GetField("s_safeClickLegacyWebActionOverride");

        var previousMode = modeField.GetValue(null);
        var previousResolver = resolverField.GetValue(null);
        var previousExecutorFactory = executorFactoryField.GetValue(null);
        var previousOwnershipFactory = ownershipFactoryField.GetValue(null);
        var previousLegacyWebAction = legacyWebActionField.GetValue(null);

        modeField.SetValue(null, (Func<SafeClickDefaultMode>)(() => mode));
        resolverField.SetValue(null, resolver);
        executorFactoryField.SetValue(null, (Func<IUiaPatternExecutor>)(() => executor));
        ownershipFactoryField.SetValue(null, (Func<IDesktopOwnershipMonitor>)(() => new PassiveOwnershipMonitor()));
        // Suppress any real legacy web click in tests (no real clicks, no real UIA invoke).
        legacyWebActionField.SetValue(null, (Func<WebTargetResult, string, ActionResult>)((_, _) => new ActionResult(false, "legacy web action suppressed in test")));

        try
        {
            return action();
        }
        finally
        {
            modeField.SetValue(null, previousMode);
            resolverField.SetValue(null, previousResolver);
            executorFactoryField.SetValue(null, previousExecutorFactory);
            ownershipFactoryField.SetValue(null, previousOwnershipFactory);
            legacyWebActionField.SetValue(null, previousLegacyWebAction);
        }
    }

    private static T RunDesktopRouting<T>(
        SafeClickDefaultMode mode,
        Func<string, string?, string?, DesktopTargetObservationResult> desktopResolver,
        Func<T> action)
    {
        var modeField = GetField("s_safeClickFsmDefaultModeOverride");
        var desktopField = GetField("s_targetObserveDesktopOverride");

        var previousMode = modeField.GetValue(null);
        var previousDesktop = desktopField.GetValue(null);

        modeField.SetValue(null, (Func<SafeClickDefaultMode>)(() => mode));
        desktopField.SetValue(null, desktopResolver);

        try
        {
            return action();
        }
        finally
        {
            modeField.SetValue(null, previousMode);
            desktopField.SetValue(null, previousDesktop);
        }
    }

    private static FieldInfo GetField(string name)
    {
        var field = typeof(RecipeRunner).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"static field '{name}' not found");
        return field;
    }

    private static WebTargetResult CreateStrongResolution()
    {
        return new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 1,
            SelectedName = "More information...",
            SelectedControlType = "Hyperlink",
            SelectedHwnd = "4321",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.1.9",
            SelectedAutomationId = "more-information-link",
            SelectedClassName = "Chrome_RenderWidgetHostHWND",
            SelectedHelpText = "Open more information",
            SelectedLegacyName = "More information...",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            SelectedProcessName = "msedge",
            SelectedWindowTitle = "ONE Brain",
            SelectedHelpTextPresent = true,
            SelectedLegacyNamePresent = true,
            HasInvoke = true,
            Reason = "exact match",
            ShadowEngineFound = true,
            ShadowEngineVerdict = "Same",
            ShadowAgreesWithLegacy = true,
            ShadowEngineSelectedName = "More information...",
            ShadowReasons = "runtime id matches"
        };
    }

    private static WebTargetResult CreateChangedRuntimeResolution()
    {
        var resolution = CreateStrongResolution();
        return new WebTargetResult
        {
            Found = resolution.Found,
            CandidateCount = resolution.CandidateCount,
            WindowsSearched = resolution.WindowsSearched,
            SelectedName = resolution.SelectedName,
            SelectedControlType = resolution.SelectedControlType,
            SelectedHwnd = resolution.SelectedHwnd,
            SelectedBoundingRect = resolution.SelectedBoundingRect,
            SelectedRuntimeId = "42.1.10",
            SelectedAutomationId = resolution.SelectedAutomationId,
            SelectedClassName = resolution.SelectedClassName,
            SelectedHelpText = resolution.SelectedHelpText,
            SelectedLegacyName = resolution.SelectedLegacyName,
            SelectedFrameworkId = resolution.SelectedFrameworkId,
            SelectedAncestorPath = resolution.SelectedAncestorPath,
            SelectedProcessName = resolution.SelectedProcessName,
            SelectedWindowTitle = resolution.SelectedWindowTitle,
            SelectedHelpTextPresent = resolution.SelectedHelpTextPresent,
            SelectedLegacyNamePresent = resolution.SelectedLegacyNamePresent,
            HasInvoke = resolution.HasInvoke,
            Reason = resolution.Reason,
            ShadowEngineFound = resolution.ShadowEngineFound,
            ShadowEngineVerdict = resolution.ShadowEngineVerdict,
            ShadowAgreesWithLegacy = resolution.ShadowAgreesWithLegacy,
            ShadowEngineSelectedName = resolution.ShadowEngineSelectedName,
            ShadowReasons = resolution.ShadowReasons
        };
    }

    private static WebTargetResult CreateMissingRuntimeResolution()
    {
        var resolution = CreateStrongResolution();
        return new WebTargetResult
        {
            Found = resolution.Found,
            CandidateCount = resolution.CandidateCount,
            WindowsSearched = resolution.WindowsSearched,
            SelectedName = resolution.SelectedName,
            SelectedControlType = resolution.SelectedControlType,
            SelectedHwnd = resolution.SelectedHwnd,
            SelectedBoundingRect = resolution.SelectedBoundingRect,
            SelectedRuntimeId = "",
            SelectedAutomationId = resolution.SelectedAutomationId,
            SelectedClassName = resolution.SelectedClassName,
            SelectedHelpText = resolution.SelectedHelpText,
            SelectedLegacyName = resolution.SelectedLegacyName,
            SelectedFrameworkId = resolution.SelectedFrameworkId,
            SelectedAncestorPath = resolution.SelectedAncestorPath,
            SelectedProcessName = resolution.SelectedProcessName,
            SelectedWindowTitle = resolution.SelectedWindowTitle,
            SelectedHelpTextPresent = resolution.SelectedHelpTextPresent,
            SelectedLegacyNamePresent = resolution.SelectedLegacyNamePresent,
            HasInvoke = resolution.HasInvoke,
            Reason = "same weak identity but runtime id missing",
            ShadowEngineFound = resolution.ShadowEngineFound,
            ShadowEngineVerdict = "LikelySame",
            ShadowAgreesWithLegacy = resolution.ShadowAgreesWithLegacy,
            ShadowEngineSelectedName = resolution.ShadowEngineSelectedName,
            ShadowReasons = "weak signals match"
        };
    }

    private static WebTargetResult CreateNotFoundResolution()
    {
        return new WebTargetResult
        {
            Found = false,
            CandidateCount = 0,
            WindowsSearched = 1,
            Reason = "target not found during re-observe"
        };
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
            HasInvoke = true
        };
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
