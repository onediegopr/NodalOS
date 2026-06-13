using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ResolutionIdentityVariablesTests
{
    [TestMethod]
    public void ResolutionIdentityVariablesAreAdditive()
    {
        var runner = new RecipeRunner();
        InvokeSetResolutionVars(runner, new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 3,
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.1.9",
            SelectedClassName = "Chrome_RenderWidgetHostHWND",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            SelectedHelpTextPresent = true,
            SelectedLegacyNamePresent = true,
            HasInvoke = true,
            HasClickablePoint = false,
            Reason = "exact match",
            ShadowEngineFound = true,
            ShadowEngineVerdict = "LikelySame",
            ShadowAgreesWithLegacy = true,
            ShadowEngineSelectedName = "Categorias"
        });

        var variables = ReadVariables(runner);

        Assert.AreEqual("true", variables["safeClick.resolution.found"]);
        Assert.AreEqual("Categorias", variables["safeClick.resolution.selectedName"]);
        Assert.AreEqual("true", variables["safeClick.resolution.identity.runtimeIdPresent"]);
        Assert.AreEqual("42.1.9", variables["safeClick.resolution.identity.runtimeId"]);
        Assert.AreEqual("Strong", variables["safeClick.resolution.identity.strength"]);
        Assert.AreEqual("Window:ONE Brain > Pane:Catalog > Document:Main", variables["safeClick.resolution.identity.ancestorPath"]);
        Assert.AreEqual("UIA", variables["safeClick.resolution.identity.frameworkId"]);
        Assert.AreEqual("Chrome_RenderWidgetHostHWND", variables["safeClick.resolution.identity.className"]);
        Assert.AreEqual("true", variables["safeClick.resolution.identity.helpTextPresent"]);
        Assert.AreEqual("true", variables["safeClick.resolution.identity.legacyNamePresent"]);
    }

    [TestMethod]
    public void RuntimeIdPresentVariableDoesNotChangeLegacyResolutionVars()
    {
        var withoutRuntime = new RecipeRunner();
        InvokeSetResolutionVars(withoutRuntime, new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 3,
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            HasInvoke = true,
            HasClickablePoint = false,
            Reason = "exact match"
        });

        var withRuntime = new RecipeRunner();
        InvokeSetResolutionVars(withRuntime, new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 3,
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.1.9",
            SelectedClassName = "Chrome_RenderWidgetHostHWND",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            HasInvoke = true,
            HasClickablePoint = false,
            Reason = "exact match"
        });

        var legacy = ReadVariables(withoutRuntime);
        var enriched = ReadVariables(withRuntime);

        Assert.AreEqual(legacy["safeClick.resolution.found"], enriched["safeClick.resolution.found"]);
        Assert.AreEqual(legacy["safeClick.resolution.selectedName"], enriched["safeClick.resolution.selectedName"]);
        Assert.AreEqual(legacy["safeClick.resolution.selectedControlType"], enriched["safeClick.resolution.selectedControlType"]);
        Assert.AreEqual(legacy["safeClick.resolution.selectedBoundingRect"], enriched["safeClick.resolution.selectedBoundingRect"]);
        Assert.AreEqual(legacy["safeClick.resolution.reason"], enriched["safeClick.resolution.reason"]);
        Assert.AreEqual("true", enriched["safeClick.resolution.identity.runtimeIdPresent"]);
    }

    private static void InvokeSetResolutionVars(RecipeRunner runner, WebTargetResult result)
    {
        var method = typeof(RecipeRunner).GetMethod("SetResolutionVars", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(runner, ["safeClick", result]);
    }

    private static Dictionary<string, string> ReadVariables(RecipeRunner runner)
    {
        var field = typeof(RecipeRunner).GetField("_ctx", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        var context = field.GetValue(runner);
        Assert.IsNotNull(context);

        var property = context.GetType().GetProperty("Variables", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property);
        var variables = property.GetValue(context) as Dictionary<string, string>;
        Assert.IsNotNull(variables);
        return variables;
    }
}
