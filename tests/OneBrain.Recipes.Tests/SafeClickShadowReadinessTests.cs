using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Actions;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickShadowReadinessTests
{
    [TestMethod]
    public void LegacyPathBehaviorUnchanged()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.IsFalse(result.Success);
        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.IsFalse(result.Variables.ContainsKey("safeClick.fsm.finalState"));
    }

    [TestMethod]
    public void ShadowReadinessRunsWithoutDispatch()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.fsmReady.success"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsmReady.reason"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsmReady.summary"));
    }

    [TestMethod]
    public void ShadowReadinessDoesNotExecuteFsm()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.IsFalse(result.Variables!.ContainsKey("safeClick.fsm.finalState"));
        Assert.AreEqual("false", result.Variables["safeClick.executed"]);
    }

    [TestMethod]
    public void ShadowReadinessDoesNotDispatchClick()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.IsFalse(result.Success);
        Assert.AreEqual("failed", result.Variables!["safeClick.result"]);
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.fsmReady.eligible"));
    }

    [TestMethod]
    public void ShadowReadinessDetectsApprovalV2()
    {
        var result = RunLegacyWebRecipe(
            includeObserve: false,
            safeClickResolution: CreateStrongResolution(),
            legacyWebAction: (_, _) => new ActionResult(true, "legacy web click"));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.hasApprovalV3"]);
        Assert.AreEqual("ApprovalV2", result.Variables["safeClick.fsmReady.reason"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsmReady.eligible"]);
    }

    [TestMethod]
    public void ShadowReadinessDetectsWeakIdentity()
    {
        var weakResolution = CreateWeakResolution(hasInvoke: true);
        var result = RunLegacyWebRecipe(
            includeObserve: true,
            observeResolution: weakResolution,
            safeClickResolution: weakResolution,
            legacyWebAction: (_, _) => new ActionResult(true, "legacy web click"));

        Assert.AreEqual("Weak", result.Variables!["safeClick.fsmReady.identityStrength"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsmReady.eligible"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsmReady.hasRuntimeId"]);
    }

    [TestMethod]
    public void ShadowReadinessDetectsStrongIdentity()
    {
        var strongResolution = CreateStrongResolution();
        var result = RunLegacyWebRecipe(
            includeObserve: true,
            observeResolution: strongResolution,
            safeClickResolution: strongResolution,
            legacyWebAction: (_, _) => new ActionResult(true, "legacy web click"));

        Assert.AreEqual("Strong", result.Variables!["safeClick.fsmReady.identityStrength"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsmReady.hasApprovalV3"]);
        Assert.AreEqual("true", result.Variables["safeClick.fsmReady.hasTargetObserve"]);
    }

    [TestMethod]
    public void ShadowReadinessDetectsMissingTargetObserve()
    {
        var result = RunLegacyWebRecipe(
            includeObserve: false,
            safeClickResolution: CreateStrongResolution(),
            legacyWebAction: (_, _) => new ActionResult(true, "legacy web click"));

        Assert.AreEqual("false", result.Variables!["safeClick.fsmReady.hasTargetObserve"]);
    }

    [TestMethod]
    public void ShadowReadinessFlagsWouldRequireLegacy()
    {
        var noInvokeResolution = CreateStrongResolution(hasInvoke: false);
        var result = RunLegacyWebRecipe(
            includeObserve: true,
            observeResolution: noInvokeResolution,
            safeClickResolution: noInvokeResolution,
            legacyWebAction: (_, _) => new ActionResult(true, "legacy web click"));

        Assert.AreEqual("true", result.Variables!["safeClick.fsmReady.wouldRequireLegacy"]);
        Assert.AreEqual("false", result.Variables["safeClick.fsmReady.eligible"]);
    }

    [TestMethod]
    public void ShadowReadinessFlagsUnsafeFallback()
    {
        var runner = new RecipeRunner();
        InvokeLegacyUsageSetter(runner, "safeClick", usedElClick: true, usedUiaActionExecutor: false);
        var variables = ReadRunnerVariables(runner);

        Assert.AreEqual("true", variables["safeClick.legacy.usedUnsafeFallback"]);
        Assert.AreEqual("true", variables["safeClick.legacy.usedElClick"]);
    }

    [TestMethod]
    public void LegacyUsageCountsElClick()
    {
        var runner = new RecipeRunner();
        InvokeLegacyUsageSetter(runner, "safeClick", usedElClick: true, usedUiaActionExecutor: false);
        var variables = ReadRunnerVariables(runner);

        Assert.AreEqual("true", variables["safeClick.legacy.usedElClick"]);
        Assert.AreEqual("false", variables["safeClick.legacy.usedUiaActionExecutor"]);
        Assert.AreEqual("true", variables["safeClick.legacy.usedUnsafeFallback"]);
    }

    [TestMethod]
    public void LegacyUsageCountsUiaActionExecutor()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.AreEqual("true", result.Variables!["safeClick.legacy.usedUiaActionExecutor"]);
        Assert.AreEqual("false", result.Variables["safeClick.legacy.usedElClick"]);
        Assert.AreEqual("true", result.Variables["safeClick.legacy.usedUnsafeFallback"]);
    }

    [TestMethod]
    public void LegacyBehaviorStillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.AreEqual("UIA safe.click", result.Variables!["safeClick.method"]);
        Assert.AreEqual("failed", result.Variables["safeClick.result"]);
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.summary"));
    }

    [TestMethod]
    public void ApprovalV2StillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.AreEqual(ApprovalManifestBuilder.PolicyVersion, result.Variables!["approval.policyVersion"]);
    }

    [TestMethod]
    public void EvidenceHashStillUnchanged()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());
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
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe());

        Assert.AreEqual("approval-v2", result.Variables!["approval.policyVersion"]);
    }

    private static RecipeRunResult RunLegacyWebRecipe(
        bool includeObserve,
        WebTargetResult? observeResolution = null,
        WebTargetResult? safeClickResolution = null,
        Func<WebTargetResult, string, ActionResult>? legacyWebAction = null)
    {
        var queuedResults = new Queue<WebTargetResult>();
        if (includeObserve)
            queuedResults.Enqueue(observeResolution ?? CreateStrongResolution());

        queuedResults.Enqueue(safeClickResolution ?? CreateStrongResolution());

        return RunWithLegacyOverrides(
            (_, _, _, _) => queuedResults.Dequeue(),
            legacyWebAction ?? ((_, _) => new ActionResult(true, "legacy web click")),
            () => new RecipeRunner().Run(BuildLegacyWebRecipe(includeObserve)));
    }

    private static RecipeDefinition BuildDesktopLegacyRecipe()
    {
        return new RecipeDefinition("safe-click-shadow-desktop")
        {
            Variables = new Dictionary<string, string>
            {
                ["approval.targetText"] = "Categorias",
                ["approval.mode"] = "controlled",
                ["approval.policyVersion"] = ApprovalManifestBuilder.PolicyVersion,
                ["approval.decision"] = "allowedForFuture",
                ["approval.riskCategory"] = "safe-readonly",
                ["approval.riskLevel"] = "low",
                ["approval.evidenceHash"] = ApprovalManifestBuilder.ComputeEvidenceHash(
                    "Categorias",
                    "controlled",
                    "allowedForFuture",
                    "safe-readonly",
                    "low",
                    ApprovalManifestBuilder.PolicyVersion),
                ["approval.executionAllowedInThisHito"] = "true"
            },
            Steps =
            [
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

    private static RecipeDefinition BuildLegacyWebRecipe(bool includeObserve)
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
            Args = new Dictionary<string, string>
            {
                ["targettext"] = "More information...",
                ["mode"] = "controlled",
                ["approvalprefix"] = "approval",
                ["proc"] = "msedge"
            }
        });

        return new RecipeDefinition("safe-click-shadow-web")
        {
            Variables = new Dictionary<string, string>
            {
                ["browser.hwnd"] = "1234",
                ["browser.owned"] = "true",
                ["browser.process"] = "msedge"
            },
            Steps = steps
        };
    }

    private static WebTargetResult CreateStrongResolution(bool hasInvoke = true)
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
            HasInvoke = hasInvoke,
            Reason = "exact match",
            ShadowEngineFound = true,
            ShadowEngineVerdict = "Same",
            ShadowAgreesWithLegacy = true,
            ShadowEngineSelectedName = "More information...",
            ShadowReasons = "runtime id matches"
        };
    }

    private static WebTargetResult CreateWeakResolution(bool hasInvoke = true)
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
            HasInvoke = hasInvoke,
            Reason = "exact match"
        };
    }

    private static T RunWithLegacyOverrides<T>(
        Func<IntPtr, string, string, int, WebTargetResult> resolver,
        Func<WebTargetResult, string, ActionResult> legacyWebAction,
        Func<T> action)
    {
        var resolverField = typeof(RecipeRunner).GetField("s_targetObserveResolverOverride", BindingFlags.Static | BindingFlags.NonPublic);
        var legacyWebActionField = typeof(RecipeRunner).GetField("s_safeClickLegacyWebActionOverride", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(resolverField);
        Assert.IsNotNull(legacyWebActionField);

        var previousResolver = resolverField.GetValue(null);
        var previousLegacyWebAction = legacyWebActionField.GetValue(null);

        resolverField.SetValue(null, resolver);
        legacyWebActionField.SetValue(null, legacyWebAction);

        try
        {
            return action();
        }
        finally
        {
            resolverField.SetValue(null, previousResolver);
            legacyWebActionField.SetValue(null, previousLegacyWebAction);
        }
    }

    private static void InvokeLegacyUsageSetter(RecipeRunner runner, string prefix, bool usedElClick, bool usedUiaActionExecutor)
    {
        var method = typeof(RecipeRunner).GetMethod("SetSafeClickLegacyUsageVars", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(runner, [prefix, usedElClick, usedUiaActionExecutor]);
    }

    private static Dictionary<string, string> ReadRunnerVariables(RecipeRunner runner)
    {
        var contextField = typeof(RecipeRunner).GetField("_ctx", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(contextField);
        var context = contextField.GetValue(runner);
        Assert.IsNotNull(context);

        var variablesProperty = context.GetType().GetProperty("Variables", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(variablesProperty);
        var variables = variablesProperty.GetValue(context) as Dictionary<string, string>;
        Assert.IsNotNull(variables);
        return variables;
    }
}
