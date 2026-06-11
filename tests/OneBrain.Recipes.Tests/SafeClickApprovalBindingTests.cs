using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickApprovalBindingTests
{
    [TestMethod]
    public void SafeClick_Target_Mismatch_Is_Blocked()
    {
        var result = RunForgedSafeClick(
            approvalTarget: "Siete",
            clickTarget: "Ocho",
            approvalMode: "controlled",
            clickMode: "controlled");

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps[0].Message, "target mismatch");
    }

    [TestMethod]
    public void SafeClick_Mode_Mismatch_Is_Blocked()
    {
        var result = RunForgedSafeClick(
            approvalTarget: "Siete",
            clickTarget: "Siete",
            approvalMode: "nonCommercialWeb",
            clickMode: "controlled");

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps[0].Message, "mode mismatch");
    }

    [TestMethod]
    public void SafeClick_Forged_ExecutionAllowed_Without_TargetMode_Binding_Is_Blocked()
    {
        var recipe = new RecipeDefinition("forged-approval")
        {
            Variables = new Dictionary<string, string>
            {
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
                        ["targettext"] = "Siete",
                        ["mode"] = "controlled",
                        ["approvalprefix"] = "approval",
                        ["proc"] = "ApplicationFrameHost"
                    }
                }
            ]
        };

        var result = new RecipeRunner().Run(recipe);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps[0].Message, "target mismatch");
    }

    private static RecipeRunResult RunForgedSafeClick(
        string approvalTarget,
        string clickTarget,
        string approvalMode,
        string clickMode)
    {
        var decision = "allowedForFuture";
        var riskCategory = "safe-readonly";
        var riskLevel = "low";
        var hash = ApprovalManifestBuilder.ComputeEvidenceHash(
            approvalTarget,
            approvalMode,
            decision,
            riskCategory,
            riskLevel,
            ApprovalManifestBuilder.PolicyVersion);

        var recipe = new RecipeDefinition("forged-approval")
        {
            Variables = new Dictionary<string, string>
            {
                ["approval.targetText"] = approvalTarget,
                ["approval.mode"] = approvalMode,
                ["approval.policyVersion"] = ApprovalManifestBuilder.PolicyVersion,
                ["approval.decision"] = decision,
                ["approval.riskCategory"] = riskCategory,
                ["approval.riskLevel"] = riskLevel,
                ["approval.evidenceHash"] = hash,
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
                        ["targettext"] = clickTarget,
                        ["mode"] = clickMode,
                        ["approvalprefix"] = "approval",
                        ["proc"] = "ApplicationFrameHost"
                    }
                }
            ]
        };

        return new RecipeRunner().Run(recipe);
    }
}
