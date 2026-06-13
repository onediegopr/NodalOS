using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SensitiveClassificationParityTests
{
    [TestMethod]
    public void RecipeRunnerAndProgramDelegateToSameClassifier()
    {
        var recipeRunnerSource = File.ReadAllText(GetRepoFile("src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));
        var programSource = File.ReadAllText(GetRepoFile("src", "OneBrain.Cli", "Program.cs"));

        StringAssert.Contains(recipeRunnerSource, "SensitiveActionClassifier.IsSensitiveStepKind(step.Kind)");
        StringAssert.Contains(programSource, "SensitiveActionClassifier.IsSensitiveStepKind(kind)");
    }

    [TestMethod]
    public void AppCloseSensitiveInRecipeRunnerAndProgram()
    {
        Assert.AreEqual(ActionSensitivity.Sensitive, SensitiveActionClassifier.ClassifyStepKind("app.close"));
        Assert.IsTrue(SensitiveActionClassifier.IsSensitiveStepKind("app.close"));
    }

    [TestMethod]
    public void KnownBenignKindsRemainBenign()
    {
        foreach (var kind in SensitiveActionClassifier.GetCanonicalBenignStepKinds())
        {
            Assert.IsFalse(SensitiveActionClassifier.IsSensitiveStepKind(kind), kind);
        }
    }

    [TestMethod]
    public void UnknownKindIsSensitiveFailClosed()
    {
        Assert.IsTrue(SensitiveActionClassifier.IsSensitiveStepKind("unknown.kind"));
    }

    private static string GetRepoFile(params string[] segments)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine([root, .. segments]);
    }
}
