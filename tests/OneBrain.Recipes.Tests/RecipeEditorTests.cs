using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes.Editing;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecipeEditorTests
{
    [TestMethod]
    public void Can_Load_Allowlisted_Demo_Recipe_For_Editor()
    {
        var recipe = PilotRecipeCatalog.FindById("demo_markdown")!;

        var model = RecipeEditorService.Load(RepoRoot(), recipe.Id, recipe.RecipePath, "supervised");

        Assert.AreEqual("demo_markdown", model.RecipeId);
        StringAssert.Contains(model.Title, "demo-product-evidence-report");
        Assert.AreEqual("low", model.RiskLevel);
        Assert.IsTrue(model.Steps.Count >= 3);
        Assert.IsTrue(model.EditableFields.Any(field => field.Field == "title" && field.CanEdit));
        Assert.IsTrue(model.EditableFields.Any(field => field.Field == "steps.kind" && !field.CanEdit));
    }

    [TestMethod]
    public void EditPolicy_Allows_Safe_Metadata_Fields()
    {
        var request = new RecipeEditRequest(
            RecipeId: "demo_markdown",
            RecipePath: "tools/recipes/demo-product-evidence-report.json",
            Title: "Demo report",
            Description: "Safe description",
            Tags: ["demo", "report"],
            Notes: ["local only"],
            HumanReadableLabels: new Dictionary<string, string>(),
            UnsafeFieldAttempts: new Dictionary<string, string>());

        var result = RecipeEditPolicy.Evaluate(request);

        Assert.IsTrue(result.Allowed, string.Join("; ", result.Errors));
    }

    [TestMethod]
    public void EditPolicy_Rejects_Dangerous_Action_Edit()
    {
        var request = new RecipeEditRequest(
            RecipeId: "demo_markdown",
            RecipePath: "tools/recipes/demo-product-evidence-report.json",
            Title: "Demo report",
            Description: "Safe description",
            Tags: [],
            Notes: [],
            HumanReadableLabels: new Dictionary<string, string>(),
            UnsafeFieldAttempts: new Dictionary<string, string> { ["steps.0.kind"] = "run_script" });

        var result = RecipeEditPolicy.Evaluate(request);

        Assert.IsFalse(result.Allowed);
        StringAssert.Contains(string.Join(" ", result.Errors), "Unsafe edit rejected");
    }

    [TestMethod]
    public void DraftStore_Writes_Draft_Under_Artifacts_Recipe_Drafts()
    {
        var temp = CreateTempDir();
        var draft = CreateDraft();

        var result = RecipeDraftStore.Write(temp, draft);
        var drafts = RecipeDraftStore.ReadAll(temp);

        Assert.IsTrue(result.Success, result.Error);
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/recipe-drafts/");
        StringAssert.Contains(File.ReadAllText(result.Path), RecipeDraftStore.SchemaVersion);
        Assert.AreEqual(1, drafts.Count);
        Assert.AreEqual(draft.SourceRecipeId, drafts[0].SourceRecipeId);
    }

    private static RecipeDraft CreateDraft()
    {
        return new RecipeDraft(
            DraftId: "draft-1",
            CreatedAtUtc: "2026-06-12T12:00:00Z",
            SourceRecipeId: "demo_markdown",
            SourceRecipePath: "tools/recipes/demo-product-evidence-report.json",
            Status: RecipeDraftStatuses.NeedsValidation,
            Title: "Demo draft",
            Description: "Safe metadata draft",
            Tags: ["demo"],
            Notes: ["draft only"],
            HumanReadableLabels: new Dictionary<string, string>(),
            ValidationNotes: []);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-recipe-editor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
