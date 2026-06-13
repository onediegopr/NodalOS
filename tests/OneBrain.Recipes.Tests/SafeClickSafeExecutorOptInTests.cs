using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickSafeExecutorOptInTests
{
    [TestMethod]
    public void SafeClickLegacyWithoutDispatchPathUnchanged()
    {
        var result = new RecipeRunner().Run(BuildLegacyRecipe());

        Assert.IsFalse(result.Success);
        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.IsFalse(result.Variables.ContainsKey("safeClick.fsm.finalState"));
    }

    [TestMethod]
    public void SafeClickUnknownDispatchPathBlocked()
    {
        var result = new RecipeRunner().Run(BuildLegacyRecipe(dispatchPath: "typo"));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps.Last().Message, "dispatchPath 'typo' is not allowed");
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables["safeClick.fsm.failureKind"]);
    }

    [TestMethod]
    public void SafeClickSafeExecutorRejectsApprovalV2()
    {
        var result = new RecipeRunner().Run(BuildOptInRecipe(includeObserve: false));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables["safeClick.fsm.failureKind"]);
        StringAssert.Contains(result.Steps.Last().Message, "approval manifest v3");
    }

    [TestMethod]
    public void SafeClickSafeExecutorRejectsWeakIdentity()
    {
        var result = RunWithOverrides(
            (_, _, _, _) => CreateWeakResolution(),
            new FakePatternExecutor(_ => throw new AssertFailedException("executor should not be called")),
            () => new RecipeRunner().Run(BuildOptInRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables["safeClick.fsm.failureKind"]);
        StringAssert.Contains(result.Steps.Last().Message, "approval manifest v3");
    }

    [TestMethod]
    public void SafeClickSafeExecutorStrongIdentitySameRuntimeSucceeds()
    {
        PatternExecutionRequest? capturedRequest = null;
        var observedIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateStrongResolution())!;
        var executor = new FakePatternExecutor(request =>
        {
            capturedRequest = request;
            return new PatternExecutionResult(
                Success: true,
                FailureKind: null,
                Reasons: ["invoke ok"],
                ObservedIdentity: observedIdentity,
                WindowFound: true,
                TargetVisible: true,
                TargetName: observedIdentity.Name,
                ObservedActions: 1,
                Signals: ["postAction.windowFound=true", "postAction.targetVisible=true"]);
        });

        var result = RunWithOverrides(
            (_, _, _, _) => CreateStrongResolution(),
            executor,
            () => new RecipeRunner().Run(BuildOptInRecipe(includeObserve: true)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("Succeeded", result.Variables["safeClick.fsm.finalState"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsm.success"]);
        Assert.AreEqual("success", result.Variables["safeClick.result"]);
        Assert.IsNotNull(capturedRequest);
        Assert.AreEqual(expected: new IntPtr(4321), actual: capturedRequest.RootHwnd);
        Assert.IsTrue(string.Equals(
            capturedRequest.ExpectedTargetName,
            "More information...",
            StringComparison.Ordinal));
    }

    [TestMethod]
    public void SafeClickSafeExecutorRuntimeIdChangedBlockedStale()
    {
        var call = 0;
        var executor = new FakePatternExecutor(_ => throw new AssertFailedException("executor should not be called when binding is stale"));
        var result = RunWithOverrides(
            (_, _, _, _) =>
            {
                call++;
                return call == 1 ? CreateStrongResolution() : CreateChangedRuntimeResolution();
            },
            executor,
            () => new RecipeRunner().Run(BuildOptInRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.Stale.ToString(), result.Variables["safeClick.fsm.failureKind"]);
        Assert.AreEqual("blocked", result.Variables["safeClick.result"]);
        StringAssert.Contains(result.Variables["safeClick.fsm.reasons"], "approved identity digest differs");
    }

    [TestMethod]
    public void SafeClickSafeExecutorAmbiguousBlocked()
    {
        var call = 0;
        var executor = new FakePatternExecutor(_ => throw new AssertFailedException("executor should not be called for ambiguous target"));
        var result = RunWithOverrides(
            (_, _, _, _) =>
            {
                call++;
                return call == 1
                    ? CreateStrongResolution()
                    : new WebTargetResult
                    {
                        Found = false,
                        CandidateCount = 2,
                        Reason = "ambiguous: 2"
                    };
            },
            executor,
            () => new RecipeRunner().Run(BuildOptInRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Blocked", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.Ambiguous.ToString(), result.Variables["safeClick.fsm.failureKind"]);
        StringAssert.Contains(result.Steps.Last().Message, "ambiguous target");
    }

    [TestMethod]
    public void SafeClickSafeExecutorNoInvokePatternPolicyDenied()
    {
        var observedIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateStrongResolution())!;
        var result = RunWithOverrides(
            (_, _, _, _) => CreateStrongResolution(),
            new FakePatternExecutor(_ => new PatternExecutionResult(
                Success: false,
                FailureKind: FailureKind.PolicyDenied,
                Reasons: ["role allowlisted but does not support InvokePattern"],
                ObservedIdentity: observedIdentity,
                WindowFound: true,
                TargetVisible: true,
                TargetName: observedIdentity.Name,
                ObservedActions: 0)),
            () => new RecipeRunner().Run(BuildOptInRecipe(includeObserve: true)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Failed", result.Variables!["safeClick.fsm.finalState"]);
        Assert.AreEqual(FailureKind.PolicyDenied.ToString(), result.Variables["safeClick.fsm.failureKind"]);
        StringAssert.Contains(result.Steps.Last().Message, "InvokePattern");
    }

    [TestMethod]
    public void SafeClickSafeExecutorWritesFsmLedger()
    {
        var observedIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateStrongResolution())!;
        var result = RunWithOverrides(
            (_, _, _, _) => CreateStrongResolution(),
            new FakePatternExecutor(_ => new PatternExecutionResult(
                Success: true,
                FailureKind: null,
                Reasons: ["invoke ok"],
                ObservedIdentity: observedIdentity,
                WindowFound: true,
                TargetVisible: true,
                TargetName: observedIdentity.Name,
                ObservedActions: 1,
                Signals: ["postAction.windowFound=true", "postAction.targetVisible=true"])),
            () => new RecipeRunner().Run(BuildOptInRecipe(includeObserve: true)));

        Assert.AreEqual("Succeeded", result.Variables!["safeClick.fsm.finalState"]);
        Assert.IsTrue(int.Parse(result.Variables["safeClick.fsm.transitionCount"]) >= 5);
        Assert.AreNotEqual("[]", result.Variables["safeClick.fsm.ledgerJson"]);
        StringAssert.Contains(result.Variables["safeClick.fsm.ledgerJson"], "\"Sequence\":1");
    }

    [TestMethod]
    public void SafeClickSafeExecutorDoesNotCallElClick()
    {
        var result = RunSuccessfulSafeExecutorPath();

        Assert.IsTrue(result.Success);
        Assert.AreEqual("FSM safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("Succeeded", result.Variables["safeClick.fsm.finalState"]);
    }

    [TestMethod]
    public void SafeClickSafeExecutorDoesNotCallUiaActionExecutor()
    {
        var result = RunSuccessfulSafeExecutorPath();

        Assert.IsTrue(result.Success);
        Assert.AreEqual("true", result.Variables!["safeClick.fsm.success"]);
    }

    [TestMethod]
    public void SafeClickSafeExecutorDoesNotUseSendInput()
    {
        var result = RunSuccessfulSafeExecutorPath();

        Assert.IsTrue(result.Success);
        Assert.AreEqual("success", result.Variables!["safeClick.result"]);
    }

    [TestMethod]
    public void SafeClickSafeExecutorDoesNotUseGetClickablePoint()
    {
        var result = RunSuccessfulSafeExecutorPath();

        Assert.IsTrue(result.Success);
        Assert.IsTrue(int.Parse(result.Variables!["safeClick.fsm.transitionCount"]) >= 5);
        Assert.AreNotEqual("[]", result.Variables["safeClick.fsm.ledgerJson"]);
    }

    private static RecipeDefinition BuildLegacyRecipe(string? dispatchPath = null)
    {
        var recipe = new RecipeDefinition("safe-click-legacy")
        {
            Variables = new Dictionary<string, string>
            {
                ["approval.targetText"] = "Categorias",
                ["approval.mode"] = "controlled",
                ["approval.policyVersion"] = "approval-v2",
                ["approval.decision"] = "allowedForFuture",
                ["approval.riskCategory"] = "safe-readonly",
                ["approval.riskLevel"] = "low",
                ["approval.evidenceHash"] = OneBrain.Core.Safety.ApprovalManifestBuilder.ComputeEvidenceHash(
                    "Categorias",
                    "controlled",
                    "allowedForFuture",
                    "safe-readonly",
                    "low",
                    OneBrain.Core.Safety.ApprovalManifestBuilder.PolicyVersion),
                ["approval.executionAllowedInThisHito"] = "true"
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = BuildSafeClickArgs("Categorias", dispatchPath)
                }
            ]
        };

        return recipe;
    }

    private static RecipeDefinition BuildOptInRecipe(bool includeObserve)
    {
        var steps = new List<RecipeStepDefinition>
        {
            new()
            {
                Id = "preflight",
                Kind = "preflight.click",
                SaveAs = "clickPreflight",
                Args = new Dictionary<string, string>
                {
                    ["targettext"] = "More information..."
                }
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
            Args = new Dictionary<string, string>
            {
                ["from"] = "clickPreflight",
                ["mode"] = "controlled"
            }
        });

        steps.Add(new RecipeStepDefinition
        {
            Id = "safe-click",
            Kind = "safe.click",
            SaveAs = "safeClick",
            Args = BuildSafeClickArgs("More information...", "safe-executor")
        });

        return new RecipeDefinition("safe-click-safe-executor")
        {
            Variables = BuildOwnedSessionVariables(),
            Steps = steps
        };
    }

    private static Dictionary<string, string> BuildSafeClickArgs(string targetText, string? dispatchPath)
    {
        var args = new Dictionary<string, string>
        {
            ["targettext"] = targetText,
            ["mode"] = "controlled",
            ["approvalprefix"] = "approval",
            ["proc"] = "msedge"
        };

        if (!string.IsNullOrWhiteSpace(dispatchPath))
            args["dispatchPath"] = dispatchPath;

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

    private static WebTargetResult CreateWeakResolution()
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
            SelectedAutomationId = "more-information-link",
            SelectedClassName = "Chrome_RenderWidgetHostHWND",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            SelectedProcessName = "msedge",
            SelectedWindowTitle = "ONE Brain",
            HasInvoke = true,
            Reason = "exact match"
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

    private static T RunWithOverrides<T>(
        Func<IntPtr, string, string, int, WebTargetResult> resolver,
        IUiaPatternExecutor executor,
        Func<T> action)
    {
        var resolverField = typeof(RecipeRunner).GetField("s_targetObserveResolverOverride", BindingFlags.Static | BindingFlags.NonPublic);
        var executorFactoryField = typeof(RecipeRunner).GetField("s_safeClickPatternExecutorFactoryOverride", BindingFlags.Static | BindingFlags.NonPublic);
        var ownershipFactoryField = typeof(RecipeRunner).GetField("s_safeClickOwnershipMonitorFactoryOverride", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(resolverField);
        Assert.IsNotNull(executorFactoryField);
        Assert.IsNotNull(ownershipFactoryField);

        var previousResolver = resolverField.GetValue(null);
        var previousExecutorFactory = executorFactoryField.GetValue(null);
        var previousOwnershipFactory = ownershipFactoryField.GetValue(null);

        resolverField.SetValue(null, resolver);
        executorFactoryField.SetValue(null, () => executor);
        ownershipFactoryField.SetValue(null, () => new PassiveOwnershipMonitor());

        try
        {
            return action();
        }
        finally
        {
            resolverField.SetValue(null, previousResolver);
            executorFactoryField.SetValue(null, previousExecutorFactory);
            ownershipFactoryField.SetValue(null, previousOwnershipFactory);
        }
    }

    private static RecipeRunResult RunSuccessfulSafeExecutorPath()
    {
        var observedIdentity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateStrongResolution())!;
        return RunWithOverrides(
            (_, _, _, _) => CreateStrongResolution(),
            new FakePatternExecutor(_ => new PatternExecutionResult(
                Success: true,
                FailureKind: null,
                Reasons: ["invoke ok"],
                ObservedIdentity: observedIdentity,
                WindowFound: true,
                TargetVisible: true,
                TargetName: observedIdentity.Name,
                ObservedActions: 1,
                Signals: ["postAction.windowFound=true", "postAction.targetVisible=true"])),
            () => new RecipeRunner().Run(BuildOptInRecipe(includeObserve: true)));
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
