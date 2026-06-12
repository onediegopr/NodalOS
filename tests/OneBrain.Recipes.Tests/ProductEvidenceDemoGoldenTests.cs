using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceDemoGoldenTests
{
    [TestMethod]
    public void Demo_Html_Semantic_Snapshot_Includes_Core_Sections()
    {
        var html = ProductEvidenceHtmlRenderer.Render(BuildDemoSummary());

        StringAssert.Contains(html, "<!doctype html>");
        StringAssert.Contains(html, "ONE BRAIN \u2014 Product Evidence Report");
        StringAssert.Contains(html, "Summary");
        StringAssert.Contains(html, "Products");
        StringAssert.Contains(html, "Notes");
        StringAssert.Contains(html, "Invalid artifacts");
    }

    [TestMethod]
    public void Demo_Html_Renders_Expected_Three_Products()
    {
        var summary = BuildDemoSummary();
        var html = ProductEvidenceHtmlRenderer.Render(summary);

        Assert.AreEqual(3, summary.ValidArtifactCount);
        StringAssert.Contains(html, "Placa Marmol Blanco Firenze");
        StringAssert.Contains(html, "Piso flotante simil madera 6 mm Essen cafe claro mate interior 2.96 m2");
        StringAssert.Contains(html, "Demo Fixture Product");
    }

    [TestMethod]
    public void Demo_Html_Renders_Complete_Fixture_Price()
    {
        var html = ProductEvidenceHtmlRenderer.Render(BuildDemoSummary());

        StringAssert.Contains(html, "<td class=\"number\">199.00</td><td>USD</td>");
    }

    [TestMethod]
    public void Demo_Html_Renders_Partial_Prices_As_Dash()
    {
        var html = ProductEvidenceHtmlRenderer.Render(BuildDemoSummary());

        Assert.IsTrue(CountOccurrences(html, "<td class=\"number\">\u2014</td><td>\u2014</td>") >= 2);
    }

    [TestMethod]
    public void Demo_Html_Includes_Readiness_And_Missing_Price()
    {
        var html = ProductEvidenceHtmlRenderer.Render(BuildDemoSummary());

        StringAssert.Contains(html, "ready_for_comparison");
        StringAssert.Contains(html, "missing_price");
        StringAssert.Contains(html, "Safety clicks total");
        StringAssert.Contains(html, ">0</div>");
    }

    [TestMethod]
    public void Demo_Html_Does_Not_Promote_Sodimac_Raw_Price()
    {
        var summary = BuildDemoSummary();
        var sodimac = summary.Items.Single(item => item.ProfileId == "sodimac-product");

        Assert.IsFalse(sodimac.HasPrice);
        Assert.IsNull(sodimac.Price);

        var html = ProductEvidenceHtmlRenderer.Render(summary);
        Assert.IsFalse(html.Contains("38.18", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Demo_Html_Recipe_Has_No_Browser_Or_Web_Actions()
    {
        var recipePath = Path.Combine(GetRepoRoot(), "tools", "recipes", "demo-product-evidence-html-report.json");
        var recipe = File.ReadAllText(recipePath);

        Assert.IsFalse(recipe.Contains("browser.open", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(recipe.Contains("safe.click", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(recipe.Contains("\"invoke\"", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(recipe, "samples/product-evidence");
        StringAssert.Contains(recipe, "artifacts/product-evidence-demo-html-reports");
    }

    private static ProductEvidenceSummary BuildDemoSummary()
    {
        var repoRoot = GetRepoRoot();
        var sampleDir = Path.Combine(repoRoot, "samples", "product-evidence");
        var sources = ProductEvidenceSummaryWriter.LoadSources(sampleDir, repoRoot);

        return ProductEvidenceSummaryBuilder.Build(sources, DateTimeOffset.Parse("2026-06-11T15:00:00Z"));
    }

    private static string GetRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }
}
