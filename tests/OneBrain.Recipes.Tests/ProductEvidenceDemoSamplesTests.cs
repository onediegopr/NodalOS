using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceDemoSamplesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TestMethod]
    public void Samples_Exist_And_Are_Valid_ProductEvidenceArtifacts()
    {
        var artifacts = LoadSampleArtifacts();

        Assert.AreEqual(3, artifacts.Count);
        Assert.IsTrue(artifacts.All(artifact => artifact.SchemaVersion == ProductEvidenceArtifactWriter.SchemaVersion));
    }

    [TestMethod]
    public void Demo_Summary_Loads_Three_Valid_Artifacts()
    {
        var summary = BuildSampleSummary();

        Assert.AreEqual(3, summary.SourceArtifactCount);
        Assert.AreEqual(3, summary.ValidArtifactCount);
        Assert.AreEqual(0, summary.InvalidArtifactCount);
    }

    [TestMethod]
    public void Demo_Complete_Fixture_Has_Visible_Price()
    {
        var summary = BuildSampleSummary();
        var complete = summary.Items.Single(item => item.ProfileId == "demo-fixture");

        Assert.IsTrue(complete.HasPrice);
        Assert.IsTrue(complete.HasCurrency);
        Assert.AreEqual("ready_for_comparison", complete.DecisionReadiness);
    }

    [TestMethod]
    public void Retail_Partial_Samples_Do_Not_Have_Visible_Price()
    {
        var summary = BuildSampleSummary();

        var retail = summary.Items.Where(item => item.ProfileId is "suministrosroca-uy-product" or "sodimac-product").ToList();
        Assert.AreEqual(2, retail.Count);
        Assert.IsTrue(retail.All(item => !item.HasPrice));
        Assert.IsTrue(retail.All(item => item.DecisionReadiness == "needs_price_verification"));
    }

    [TestMethod]
    public void Sodimac_RawSignals_Price_Does_Not_Become_Visible_Price()
    {
        var summary = BuildSampleSummary();
        var sodimac = summary.Items.Single(item => item.ProfileId == "sodimac-product");

        Assert.IsFalse(sodimac.HasPrice);
        Assert.IsNull(sodimac.Price);
        Assert.AreEqual(4, sodimac.RawSignalCount);
    }

    [TestMethod]
    public void Demo_Summary_Calculates_Expected_Counts()
    {
        var summary = BuildSampleSummary();

        Assert.AreEqual(1, summary.Totals.ProductsWithPrice);
        Assert.AreEqual(2, summary.Totals.ProductsMissingPrice);
        Assert.AreEqual(2, summary.Totals.PartialCount);
        Assert.AreEqual(1, summary.Totals.SufficientCount);
        Assert.AreEqual(1, summary.Totals.ReadyForComparisonCount);
        Assert.AreEqual(66.67, summary.Totals.AverageEvidenceScore);
    }

    [TestMethod]
    public void Demo_Markdown_Contains_Score_Grade_And_Readiness()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(BuildSampleSummary());

        StringAssert.Contains(markdown, "Score | Grade | Readiness");
        StringAssert.Contains(markdown, "50 | partial | needs_price_verification");
        StringAssert.Contains(markdown, "100 | excellent | ready_for_comparison");
    }

    [TestMethod]
    public void Demo_Writers_Create_Artifacts_Under_Demo_Artifacts_Directories()
    {
        var temp = CreateTempDirectory();
        try
        {
            CopySamples(temp);

            var summary = ProductEvidenceSummaryWriter.WriteFromDirectory(
                temp,
                inputDirectory: "samples/product-evidence",
                outputDirectory: "artifacts/product-evidence-demo-summary",
                createdAtUtc: DateTimeOffset.Parse("2026-06-11T12:30:00Z"));

            Assert.IsTrue(summary.Success, summary.Error);
            StringAssert.Contains(summary.RelativePath, "artifacts/product-evidence-demo-summary/");

            var markdown = ProductEvidenceMarkdownWriter.Write(
                temp,
                summary.Summary,
                outputDirectory: "artifacts/product-evidence-demo-reports");

            Assert.IsTrue(markdown.Success, markdown.Error);
            StringAssert.Contains(markdown.RelativePath, "artifacts/product-evidence-demo-reports/");
            Assert.IsTrue(File.Exists(markdown.Path));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Demo_Recipe_Has_No_Browser_Or_Input_Actions()
    {
        var recipe = LoadDemoRecipe();
        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "browser.open",
            "click",
            "safe.click",
            "actv.invoke",
            "actv.type",
            "type",
            "submit",
            "key"
        };

        Assert.IsFalse(recipe.Steps.Any(step => forbidden.Contains(step.Kind)));
    }

    private static ProductEvidenceSummary BuildSampleSummary()
    {
        var root = GetRootPath("");
        var inputRoot = Path.Combine(root, "samples", "product-evidence");
        var sources = ProductEvidenceSummaryWriter.LoadSources(inputRoot, root);
        return ProductEvidenceSummaryBuilder.Build(sources, DateTimeOffset.Parse("2026-06-11T12:30:00Z"));
    }

    private static List<ProductEvidenceArtifact> LoadSampleArtifacts()
    {
        var root = GetRootPath("samples/product-evidence");
        return Directory.GetFiles(root, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => JsonSerializer.Deserialize<ProductEvidenceArtifact>(File.ReadAllText(path), JsonOptions))
            .Where(artifact => artifact != null)
            .Cast<ProductEvidenceArtifact>()
            .ToList();
    }

    private static RecipeDefinition LoadDemoRecipe()
    {
        var json = File.ReadAllText(GetRootPath("tools/recipes/demo-product-evidence-report.json"));
        var recipe = JsonSerializer.Deserialize<RecipeDefinition>(json, JsonOptions);
        Assert.IsNotNull(recipe);
        return recipe!;
    }

    private static void CopySamples(string temp)
    {
        var source = GetRootPath("samples/product-evidence");
        var target = Path.Combine(temp, "samples", "product-evidence");
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source, "*.json", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onebrain-demo-samples-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string GetRootPath(string relative)
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, relative);
    }
}
