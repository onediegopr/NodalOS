using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes.Editing;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecipeLinterTests
{
    [TestMethod]
    public void Linter_Allows_Known_Safe_Demo_Recipe_To_Run()
    {
        var json = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "recipes", "demo-product-evidence-report.json"));

        var result = RecipeLinter.ValidateJson(json, "tools/recipes/demo-product-evidence-report.json");

        Assert.IsTrue(result.CanRun, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        Assert.IsFalse(result.Issues.Any(issue => issue.Severity == RecipeValidationSeverities.Blocked));
    }

    [TestMethod]
    public void Linter_Blocks_Arbitrary_Command()
    {
        var result = RecipeLinter.ValidateJson("""
        {
          "name": "bad",
          "description": "bad command",
          "steps": [
            { "id": "run", "kind": "note", "text": "powershell Remove-Item" }
          ]
        }
        """);

        Assert.IsFalse(result.CanRun);
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "arbitrary_command"));
    }

    [TestMethod]
    public void Linter_Blocks_Sensitive_Action_Without_Approval()
    {
        var result = RecipeLinter.ValidateJson("""
        {
          "name": "bad",
          "description": "bad click",
          "steps": [
            { "id": "click", "kind": "safe.click" }
          ]
        }
        """);

        Assert.IsFalse(result.CanRun);
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "sensitive_action_requires_approval"));
    }

    [TestMethod]
    public void Linter_Allows_Safe_Conditional_Branches()
    {
        var result = RecipeLinter.ValidateJson("""
        {
          "name": "conditional-safe",
          "description": "safe conditional",
          "steps": [
            {
              "id": "check",
              "kind": "if",
              "condition": { "left": "Mercado Libre", "operator": "contains", "right": "Mercado" },
              "then": [
                { "id": "ok", "kind": "assert.contains", "value": "Mercado Libre", "expected": "Mercado" }
              ],
              "else": [
                { "id": "note", "kind": "note", "text": "diagnostic only" }
              ]
            }
          ]
        }
        """);

        Assert.IsTrue(result.CanRun, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        Assert.IsFalse(result.Issues.Any(issue => issue.Code == "action_not_allowlisted"));
    }

    [TestMethod]
    public void Linter_Blocks_Payment_Purchase_Login_And_Cookies()
    {
        var result = RecipeLinter.ValidateJson("""
        {
          "name": "bad",
          "description": "bad critical",
          "steps": [
            { "id": "login", "kind": "login" },
            { "id": "cookie", "kind": "accept_cookies" },
            { "id": "pay", "kind": "payment" },
            { "id": "buy", "kind": "purchase" }
          ]
        }
        """);

        Assert.IsFalse(result.CanRun);
        Assert.IsTrue(result.Issues.Count(issue => issue.Code == "critical_action_blocked") >= 4);
    }

    [TestMethod]
    public void Linter_Blocks_Secret_Like_Content()
    {
        var secretLikeText = "secret" + "=" + "value";
        var result = RecipeLinter.ValidateJson($$"""
        {
          "name": "bad",
          "description": "secret",
          "steps": [
            { "id": "note", "kind": "note", "text": "{{secretLikeText}}" }
          ]
        }
        """);

        Assert.IsFalse(result.CanRun);
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "secret_like_content"));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }
}
