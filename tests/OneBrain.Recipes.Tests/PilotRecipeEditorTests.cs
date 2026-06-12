using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes.Editing;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotRecipeEditorTests
{
    [TestMethod]
    public void Home_Render_Includes_Recipe_Editor_And_Variable_Manager_Links()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "Tareas automatizables");
        StringAssert.Contains(html, "/recipes");
        StringAssert.Contains(html, "Datos de la tarea");
        StringAssert.Contains(html, "/variables");
    }

    [TestMethod]
    public void Recipe_List_Renders_Allowlisted_Recipes()
    {
        var recipes = LoadRecipes();

        var html = PilotHomePageRenderer.RenderRecipeList(recipes);

        StringAssert.Contains(html, "lista permitida");
        StringAssert.Contains(html, "solo borradores");
        StringAssert.Contains(html, "demo-product-evidence-report");
        StringAssert.Contains(html, "tools/recipes/demo-product-evidence-report.json");
    }

    [TestMethod]
    public void Recipe_Detail_Renders_Validation_And_Safe_Edit_Copy()
    {
        var model = LoadRecipes().First();
        var json = File.ReadAllText(Path.Combine(RepoRoot(), model.RecipePath.Replace('/', Path.DirectorySeparatorChar)));
        var validation = RecipeLinter.ValidateJson(json, model.RecipePath);
        var variables = RecipeVariableManager.ExtractVariablesFromJson(json);

        var html = PilotHomePageRenderer.RenderRecipeDetail(model, validation, variables);

        StringAssert.Contains(html, "solo campos seguros");
        StringAssert.Contains(html, "sin editar acciones");
        StringAssert.Contains(html, "Puede ejecutarse");
        StringAssert.Contains(html, "Pasos explicados para humanos");
        StringAssert.Contains(html, "Campos como tipo de paso");
    }

    [TestMethod]
    public void Variables_Render_Masks_Sensitive_Values()
    {
        var variables = new[]
        {
            new RecipeVariableDefinition("customerMessage", RecipeVariableTypes.Message, true, "Mensaje privado demo", null, RecipeVariableSensitivity.Sensitive, true, null, [], null, null)
        };

        var html = PilotHomePageRenderer.RenderVariables(variables);

        StringAssert.Contains(html, "valores sensibles enmascarados");
        Assert.IsFalse(html.Contains("Mensaje privado demo", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(html, "Me...");
    }

    private static IReadOnlyList<RecipeEditorModel> LoadRecipes()
    {
        return PilotRecipeCatalog.AllowlistedRecipes
            .Select(recipe => RecipeEditorService.Load(RepoRoot(), recipe.Id, recipe.RecipePath, "supervised"))
            .ToList();
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }
}
