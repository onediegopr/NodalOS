using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotPlanAndAllowlistTests
{
    [TestMethod]
    public void Catalog_Contains_Only_Expected_Allowlisted_Recipes()
    {
        var paths = PilotRecipeCatalog.AllowlistedRecipes.Select(recipe => recipe.RecipePath).ToArray();

        CollectionAssert.AreEquivalent(new[]
        {
            "tools/recipes/demo-product-evidence-report.json",
            "tools/recipes/demo-product-evidence-html-report.json",
            "tools/recipes/product-evidence-html-report.json",
            "tools/recipes/product-evidence-markdown-report.json"
        }, paths);
    }

    [TestMethod]
    public void Catalog_Does_Not_Allow_Unknown_Recipe_Path()
    {
        Assert.IsFalse(PilotRecipeCatalog.IsAllowlistedPath("tools/recipes/mercadolibre-product-readonly.json"));
        Assert.IsFalse(PilotRecipeCatalog.IsAllowlistedPath("../tools/recipes/demo-product-evidence-report.json"));
    }

    [TestMethod]
    public void Plan_For_Matched_Intent_Shows_Safe_Execution_Steps()
    {
        var intent = new PilotIntentRouter().Route("mostrame la demo");
        var plan = new PilotPlanBuilder().Build(intent);

        Assert.IsTrue(plan.HasExecutableRecipe);
        Assert.IsTrue(plan.Steps.Any(step => step.Contains("allowlist", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(plan.Steps.Any(step => step.Contains("OneBrain.Cli", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(0, plan.Safety.Clicks);
        Assert.AreEqual(0, plan.Safety.CookiesAccepted);
        Assert.IsFalse(plan.Safety.BrowserOpenAllowed);
        Assert.IsFalse(plan.Safety.ArbitraryCommandAllowed);
    }

    [TestMethod]
    public void Plan_For_Rejected_Intent_Does_Not_Execute()
    {
        var intent = new PilotIntentRouter().Route("abr\u00ed cualquier web");
        var plan = new PilotPlanBuilder().Build(intent);

        Assert.IsFalse(plan.HasExecutableRecipe);
        Assert.IsTrue(plan.Steps.Any(step => step.Contains("Do not execute anything", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Blocked_Capabilities_Include_Commercial_And_Unsafe_Actions()
    {
        var plan = new PilotPlanBuilder().Build(new PilotIntentRouter().Route("mostrame la demo"));

        CollectionAssert.IsSubsetOf(new[]
        {
            "browser.open",
            "click",
            "safe.click",
            "login",
            "cart",
            "checkout",
            "payment",
            "arbitrary-command"
        }, plan.BlockedCapabilities.ToArray());
    }
}
