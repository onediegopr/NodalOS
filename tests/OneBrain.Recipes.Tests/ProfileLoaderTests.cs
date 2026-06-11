using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Profiles;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProfileLoaderTests
{
    private readonly ProfileLoader _loader = new();

    [TestMethod]
    public void Loads_ExampleCom_Profile()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.IsNotNull(result.Profile);
        Assert.AreEqual("example-com", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
    }

    [TestMethod]
    public void Loads_Wikipedia_Profile()
    {
        var path = GetRootPath("tools/profiles/web/wikipedia-automation.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("wikipedia-automation", result.Profile!.Id);
    }

    [TestMethod]
    public void Fails_On_Missing_File()
    {
        var result = _loader.Load("tools/profiles/web/__nonexistent__.json");
        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "not found");
    }

    [TestMethod]
    public void Fails_On_Invalid_Profile()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), "test-invalid-profile.json");
        File.WriteAllText(tmpPath, "{\"id\":\"\",\"type\":\"invalid\"}");
        try
        {
            var result = _loader.Load(tmpPath);
            Assert.IsFalse(result.Success);
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [TestMethod]
    public void ToVariables_Produces_Correct_Prefix()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success);
        var vars = _loader.ToVariables(result.Profile!);
        Assert.AreEqual("example-com", vars["profile.example-com.id"]);
        Assert.AreEqual("https://example.com", vars["profile.example-com.url"]);
    }

    [TestMethod]
    public void ToVariables_With_Prefix_Uses_Provided_Prefix()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success);
        var vars = _loader.ToVariables(result.Profile!, "profile.wiki");
        Assert.AreEqual("example-com", vars["profile.wiki.id"]);
        Assert.AreEqual("https://example.com", vars["profile.wiki.url"]);
        Assert.AreEqual("Example", vars["profile.wiki.expected.titleContains"]);
    }

    [TestMethod]
    public void ToVariables_Without_Prefix_FallsBack_To_ProfileId()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        var vars = _loader.ToVariables(result.Profile!);
        Assert.AreEqual("https://example.com", vars["profile.example-com.url"]);
    }

    [TestMethod]
    public void Loads_WikipediaLaptop_Profile()
    {
        var path = GetRootPath("tools/profiles/web/wikipedia-laptop.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("wikipedia-laptop", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
    }

    [TestMethod]
    public void Loads_DuckDuckGoLite_Profile()
    {
        var path = GetRootPath("tools/profiles/web/duckduckgo-lite-laptop.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("duckduckgo-lite-laptop", result.Profile!.Id);
    }

    [TestMethod]
    public void ValidateTemplates_Detects_Misspelled_Variable()
    {
        var path = GetRootPath("tools/recipes/template-validation-negative.json");
        var json = File.ReadAllText(path);
        var recipe = System.Text.Json.JsonSerializer.Deserialize<OneBrain.Core.Recipes.RecipeDefinition>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.IsNotNull(recipe);

        var warnings = OneBrain.Cli.Recipes.RecipeRunner.ValidateTemplates(recipe!);
        Assert.AreEqual(2, warnings.Count, $"Expected exactly 2 warnings, got {warnings.Count}: {string.Join("; ", warnings)}");
        StringAssert.Contains(warnings[0], "profile.serach.url");
        StringAssert.Contains(warnings[1], "browser.titlee");
    }

    [TestMethod]
    public void ValidateTemplates_Accepts_Valid_Recipe()
    {
        var path = GetRootPath("tools/recipes/product-search-report.json");
        var json = File.ReadAllText(path);
        var recipe = System.Text.Json.JsonSerializer.Deserialize<OneBrain.Core.Recipes.RecipeDefinition>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.IsNotNull(recipe);

        var warnings = OneBrain.Cli.Recipes.RecipeRunner.ValidateTemplates(recipe!);
        Assert.AreEqual(0, warnings.Count, $"Expected 0 warnings, got: {string.Join("; ", warnings)}");
    }

    [TestMethod]
    public void Loads_MercadoLibre_Profile()
    {
        var path = GetRootPath("tools/profiles/web/mercadolibre-ar-notebook.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("mercadolibre-ar-notebook", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
    }

    [TestMethod]
    public void Loads_MercadoLibre_Product_Profile()
    {
        var path = GetRootPath("tools/profiles/web/mercadolibre-ar-product.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("mercadolibre-ar-product", result.Profile!.Id);
    }

    [TestMethod]
    public void Loads_SuministrosRoca_Profile()
    {
        var path = GetRootPath("tools/profiles/web/suministrosroca-uy-home.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("suministrosroca-uy-home", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
        Assert.AreEqual("https://suministrosroca.uy", result.Profile.Url);
    }

    [TestMethod]
    public void Loads_Sodimac_Public_Profile()
    {
        var path = GetRootPath("tools/profiles/web/sodimac-public-home.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("sodimac-public-home", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
        Assert.AreEqual("https://www.sodimac.com.uy", result.Profile.Url);
    }

    [TestMethod]
    public void Loads_SuministrosRoca_Category_Profile()
    {
        var path = GetRootPath("tools/profiles/web/suministrosroca-uy-category.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("suministrosroca-uy-category", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
        Assert.AreEqual("https://suministrosroca.uy/categoria-producto/pisos/", result.Profile.Url);
    }

    [TestMethod]
    public void Loads_Sodimac_Category_Profile()
    {
        var path = GetRootPath("tools/profiles/web/sodimac-category.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("sodimac-category", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
        Assert.AreEqual("https://www.sodimac.com.uy/sodimac-uy/category/cat20668/pisos-y-revestimientos/", result.Profile.Url);
    }

    [TestMethod]
    public void ExtractCommercialFields_Detects_Price()
    {
        var result = OneBrain.Cli.Recipes.RecipeRunner_ExtractHelper.Extract("Test | Notebook $ 1.299.999 Envio gratis");
        Assert.AreEqual("$ 1.299.999", result["product.priceCandidate"]);
        Assert.AreEqual("high", result["product.confidence"]);
    }

    [TestMethod]
    public void ExtractCommercialFields_NoPrice_IsMedium()
    {
        var result = OneBrain.Cli.Recipes.RecipeRunner_ExtractHelper.Extract("Notebook sin precio");
        Assert.AreEqual("null", result["product.priceCandidate"]);
        Assert.AreEqual("medium", result["product.confidence"]);
    }

    [TestMethod]
    public void ExtractCommercialFields_Detects_SensitiveWords()
    {
        var result = OneBrain.Cli.Recipes.RecipeRunner_ExtractHelper.Extract("Comprar ahora Notebook | Agregar al carrito");
        Assert.IsTrue(result["product.sensitiveWordsDetected"].Contains("comprar"));
        Assert.IsTrue(result["product.sensitiveWordsDetected"].Contains("carrito"));
    }

    private static string GetRootPath(string relative)
    {
        // Tests run from bin/Debug/netXX-windows. Navigate up 4 levels to solution root.
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, relative);
    }
}
