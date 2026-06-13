using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class DesktopTargetObserveTests
{
    [TestMethod]
    public void DesktopObserveIsReadOnly()
    {
        var result = RunWithDesktopObserveOverride(
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(BuildDesktopObserveRecipe("observed")));

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Steps.Single().Success);
        Assert.IsFalse(result.Variables!.ContainsKey("observed.executed"));
        Assert.IsFalse(result.Variables.ContainsKey("safeClick.result"));
    }

    [TestMethod]
    public void DesktopObserveProducesWeakWithoutRuntimeId()
    {
        var result = RunWithDesktopObserveOverride(
            (_, _, _) => CreateWeakDesktopObservation(),
            () => new RecipeRunner().Run(BuildDesktopObserveRecipe("observed")));

        Assert.AreEqual("Weak", result.Variables!["observed.identity.strength"]);
        Assert.AreEqual("false", result.Variables["observed.resolution.identity.runtimeIdPresent"]);
    }

    [TestMethod]
    public void DesktopObserveProducesStrongWithRuntimeId()
    {
        var result = RunWithDesktopObserveOverride(
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(BuildDesktopObserveRecipe("observed")));

        Assert.AreEqual("Strong", result.Variables!["observed.identity.strength"]);
        Assert.AreEqual("42.7.9", result.Variables["observed.identity.runtimeId"]);
        Assert.AreEqual("uia", result.Variables["observed.identity.source"]);
        Assert.IsTrue(result.Variables.ContainsKey("observed.identity.digest"));
    }

    [TestMethod]
    public void DesktopObserveDoesNotClick()
    {
        var result = RunWithDesktopObserveOverride(
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(BuildDesktopObserveRecipe("observed")));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("true", result.Variables!["observed.resolution.found"]);
        Assert.IsFalse(result.Variables.ContainsKey("observed.method"));
    }

    [TestMethod]
    public void DesktopObserveFeedsApprovalManifestWithoutChangingPolicyVersion()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        var result = RunWithDesktopObserveOverride(
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(new RecipeDefinition("desktop-observe-approval")
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
                    }
                ]
            }));

        var expectedHash = ApprovalManifestBuilder.ComputeEvidenceHash(
            "Categorias",
            "controlled",
            "allowedForFuture",
            "safe-readonly",
            "low",
            ApprovalManifestBuilder.PolicyVersion);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("approval-v2", result.Variables!["approval.policyVersion"]);
        Assert.AreEqual(expectedHash, result.Variables["approval.evidenceHash"]);
        Assert.AreEqual("approval-v3", result.Variables["approval.identity.schemaVersion"]);
        Assert.AreEqual("Strong", result.Variables["approval.identity.strength"]);
    }

    private static RecipeDefinition BuildDesktopObserveRecipe(string prefix)
    {
        return new RecipeDefinition("desktop-observe")
        {
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "observe",
                    Kind = "target.observe.desktop",
                    SaveAs = prefix,
                    Args = new Dictionary<string, string>
                    {
                        ["targetText"] = "Categorias",
                        ["processName"] = "explorer",
                        ["window"] = "ONE Brain"
                    }
                }
            ]
        };
    }

    private static T RunWithDesktopObserveOverride<T>(
        Func<string, string?, string?, DesktopTargetObservationResult> resolver,
        Func<T> action)
    {
        var field = typeof(RecipeRunner).GetField("s_targetObserveDesktopOverride", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field);

        var previous = field.GetValue(null);
        field.SetValue(null, resolver);
        try
        {
            return action();
        }
        finally
        {
            field.SetValue(null, previous);
        }
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

    private static DesktopTargetObservationResult CreateWeakDesktopObservation()
    {
        return new DesktopTargetObservationResult
        {
            Found = true,
            CandidateCount = 1,
            Reason = "exact desktop observe match",
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            SelectedAutomationId = "categories-button",
            SelectedClassName = "Button",
            SelectedFrameworkId = "WPF",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog",
            SelectedProcessName = "explorer",
            SelectedWindowTitle = "ONE Brain",
            HasInvoke = true
        };
    }
}
