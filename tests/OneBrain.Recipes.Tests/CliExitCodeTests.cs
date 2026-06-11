using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli;
using OneBrain.Cli.Recipes;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class CliExitCodeTests
{
    [TestMethod]
    public void Failed_Recipe_Result_Maps_To_NonZero_Exit_Code()
    {
        var result = new RecipeRunResult(
            false,
            "negative",
            1,
            0,
            1,
            0,
            [],
            []);

        Assert.AreEqual(CliExitCodes.Failure, CliExitCodes.FromRecipeResult(result));
    }

    [TestMethod]
    public void Successful_Recipe_Result_Maps_To_Zero_Exit_Code()
    {
        var result = new RecipeRunResult(
            true,
            "positive",
            1,
            1,
            0,
            0,
            [],
            []);

        Assert.AreEqual(CliExitCodes.Success, CliExitCodes.FromRecipeResult(result));
    }

    [TestMethod]
    public void Assert_Failure_Recipe_Produces_Failed_Result()
    {
        var recipe = new RecipeDefinition("exit-code-negative")
        {
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "assert-fails",
                    Kind = "assert.equals",
                    Value = "A",
                    Expected = "B"
                }
            ]
        };

        var result = new RecipeRunner().Run(recipe);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(CliExitCodes.Failure, CliExitCodes.FromRecipeResult(result));
    }
}
