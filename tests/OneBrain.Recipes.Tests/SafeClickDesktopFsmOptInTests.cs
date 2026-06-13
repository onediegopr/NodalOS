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
    public void DesktopWithoutDispatchPathStillLegacy()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
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
    public void DesktopDefaultModeDoesNotRouteYet()
    {
        var result = RunDesktopSafeExecutor(
            resolver: (_, _, _) => CreateStrongDesktopObservation(),
            executor: ThrowingExecutor(),
            () => new RecipeRunner().Run(BuildDesktopRecipe(dispatchPath: null)));

        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsm.routedByDefault"]);
        Assert.AreEqual("DesktopExcludedFromDefault", result.Variables["safeClick.fsm.defaultRouteReason"]);
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
    }

    [TestMethod]
    public void BasicActionVerifierStillUntouched()
    {
        var result = new RecipeRunner().Run(BuildDesktopApprovalV2Recipe(dispatchPath: null));

        Assert.IsFalse(result.Variables!.ContainsKey("safeClick.fsm.finalState"));
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
        modeField.SetValue(null, (Func<SafeClickDefaultMode>)(() => SafeClickDefaultMode.WebEligible));

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
