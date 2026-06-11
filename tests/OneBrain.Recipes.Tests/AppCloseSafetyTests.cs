using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class AppCloseSafetyTests
{
    [TestMethod]
    public void AppClose_Explorer_Is_Blocked_Without_Owned_App_Session()
    {
        var recipe = new RecipeDefinition("app-close-explorer-blocked")
        {
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "close-explorer",
                    Kind = "app.close",
                    App = "explorer",
                    SaveAs = "closeExplorer"
                }
            ]
        };

        var result = new RecipeRunner().Run(recipe);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Steps[0].Message, "Explorer close requires owned session tracking");
        Assert.AreEqual("false", result.Variables!["closeExplorer.success"]);
        Assert.AreEqual("0", result.Variables!["closeExplorer.closedCount"]);
    }
}
