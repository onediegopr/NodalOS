using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickPlanVariablesTests
{
    [TestMethod]
    public void SafeClickPlanVariablesAreAdditive()
    {
        var result = RunApprovalThenSafeClick(includeIdentity: true);

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.plan.projectedState"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.failureKind"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.blockReason"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.identityStrength"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.contractValid"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.bindingVerdict"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.parityAgrees"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.wouldDispatch"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.wouldUseUnsafeFallback"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.plan.reasons"));
    }

    [TestMethod]
    public void SafeClickPlanDoesNotChangeSafeClickResult()
    {
        var v2 = RunApprovalThenSafeClick(includeIdentity: false);
        var v3 = RunApprovalThenSafeClick(includeIdentity: true);

        Assert.AreEqual(v2.Success, v3.Success);
        Assert.AreEqual(v2.Steps.Last().Success, v3.Steps.Last().Success);
        Assert.AreEqual(v2.Steps.Last().Message, v3.Steps.Last().Message);
        Assert.AreEqual(v2.Variables!["safeClick.result"], v3.Variables!["safeClick.result"]);
        Assert.AreEqual(v2.Variables["safeClick.reason"], v3.Variables["safeClick.reason"]);
    }

    [TestMethod]
    public void SafeClickPlanWouldDispatchIsFalse()
    {
        var result = RunApprovalThenSafeClick(includeIdentity: true);

        Assert.AreEqual("false", result.Variables!["safeClick.plan.wouldDispatch"]);
    }

    [TestMethod]
    public void SafeClickPlanFailureDoesNotFailStep()
    {
        var result = RunApprovalThenSafeClick(includeIdentity: false);

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.plan.projectedState"));
        Assert.AreEqual("Blocked", result.Variables["safeClick.plan.projectedState"]);
        Assert.AreEqual("false", result.Variables["safeClick.plan.wouldDispatch"]);
        Assert.IsFalse(result.Steps.Last().Message.Contains("planner unavailable", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ExistingSafeClickVarsUnchanged()
    {
        var result = RunApprovalThenSafeClick(includeIdentity: true);

        Assert.AreEqual("Categorias", result.Variables!["safeClick.targetText"]);
        Assert.AreEqual("UIA safe.click", result.Variables["safeClick.method"]);
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.result"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.reason"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.summary"));
    }

    private static RecipeRunResult RunApprovalThenSafeClick(bool includeIdentity)
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        var variables = new Dictionary<string, string>
        {
            ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
        };

        if (includeIdentity)
        {
            variables["clickPreflight.identity.name"] = "Categorias";
            variables["clickPreflight.identity.controlType"] = "Button";
            variables["clickPreflight.identity.automationId"] = "categories-button";
            variables["clickPreflight.identity.source"] = "web-shadow";
            variables["clickPreflight.identity.shadowAgreesWithLegacy"] = "true";
        }

        var recipe = new RecipeDefinition("approval-plan-safe-click")
        {
            Variables = variables,
            Steps =
            [
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

        return new RecipeRunner().Run(recipe);
    }
}
