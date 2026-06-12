using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes.Editing;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecipeVariableManagerTests
{
    [TestMethod]
    public void ExtractVariables_Detects_Template_Variables()
    {
        var json = """
        {
          "name": "demo",
          "steps": [
            { "id": "note", "kind": "note", "text": "Open {{productUrl}} and message {{customerMessage}}" }
          ]
        }
        """;

        var variables = RecipeVariableManager.ExtractVariablesFromJson(json);

        Assert.IsTrue(variables.Any(variable => variable.Name == "productUrl" && variable.Type == RecipeVariableTypes.Url));
        Assert.IsTrue(variables.Any(variable => variable.Name == "customerMessage" && variable.Sensitivity == RecipeVariableSensitivity.Sensitive));
    }

    [TestMethod]
    public void DisplayValue_Masks_Sensitive_Variables()
    {
        var variable = new RecipeVariableDefinition(
            Name: "customerMessage",
            Type: RecipeVariableTypes.Message,
            Required: true,
            DefaultValue: "Mensaje privado demo",
            ExampleValue: null,
            Sensitivity: RecipeVariableSensitivity.Sensitive,
            Redacted: true,
            Regex: null,
            AllowedValues: [],
            Min: null,
            Max: null);

        var display = RecipeVariableManager.DisplayValue(variable);

        Assert.AreNotEqual("Mensaje privado demo", display);
        StringAssert.Contains(display, "Me");
    }

    [TestMethod]
    public void ValidateVariables_Detects_Missing_Required_Variable()
    {
        var variables = RecipeVariableManager.ExtractVariablesFromJson("""
        { "name": "demo", "steps": [ { "kind": "note", "text": "{{requiredValue}}" } ] }
        """);

        var issues = RecipeVariableManager.ValidateVariables(variables, new Dictionary<string, string?>());

        Assert.IsTrue(issues.Any(issue => issue.Contains("Missing required variable", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Secret_Variables_Are_Not_Stored_As_Default_Normal_Values()
    {
        var variables = RecipeVariableManager.ExtractVariablesFromJson("""
        { "variables": { "apiSecretToken": "not-a-real-token" }, "steps": [] }
        """);

        var secret = variables.Single(variable => variable.Name == "apiSecretToken");

        Assert.AreEqual(RecipeVariableSensitivity.Secret, secret.Sensitivity);
        Assert.IsNull(secret.DefaultValue);
        Assert.IsTrue(secret.Redacted);
    }
}
