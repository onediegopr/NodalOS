using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceArtifactWriterTests
{
    [TestMethod]
    public void Build_Includes_Complete_ProductEvidence()
    {
        var evidence = CreateEvidence(price: "38.18", currency: "USD");
        var artifact = ProductEvidenceArtifactWriter.Build(new ProductEvidenceArtifactInput
        {
            Evidence = evidence,
            RecipeId = "recipe",
            RunId = "run-1",
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-11T15:00:00Z")
        });

        Assert.AreEqual("product-evidence-artifact/v1", artifact.SchemaVersion);
        Assert.AreEqual("Piso flotante Essen", artifact.Evidence.ProductName);
        Assert.AreEqual("38.18", artifact.Evidence.Price);
        Assert.AreEqual("USD", artifact.Evidence.Currency);
    }

    [TestMethod]
    public void Build_Preserves_Null_Price_And_Currency_When_Missing()
    {
        var artifact = ProductEvidenceArtifactWriter.Build(new ProductEvidenceArtifactInput
        {
            Evidence = CreateEvidence(price: null, currency: null),
            RecipeId = "recipe",
            RunId = "run-1"
        });

        Assert.IsNull(artifact.Evidence.Price);
        Assert.IsNull(artifact.Evidence.Currency);
    }

    [TestMethod]
    public void Build_Includes_BlockedOrMissingFields()
    {
        var artifact = ProductEvidenceArtifactWriter.Build(new ProductEvidenceArtifactInput
        {
            Evidence = CreateEvidence(price: null, currency: null),
            RecipeId = "recipe"
        });

        CollectionAssert.Contains(artifact.Validation.BlockedOrMissingFields.ToList(), "missing_price");
        CollectionAssert.Contains(artifact.Validation.BlockedOrMissingFields.ToList(), "missing_stock");
    }

    [TestMethod]
    public void Build_Includes_Safety_Summary()
    {
        var evidence = CreateEvidence(price: null, currency: null) with
        {
            CartSignals = ["cart"],
            BuySignals = ["buy"],
            PaymentSignals = ["pay"],
            LoginSignals = ["login"],
            WhatsappSignals = ["whatsapp"]
        };

        var artifact = ProductEvidenceArtifactWriter.Build(new ProductEvidenceArtifactInput
        {
            Evidence = evidence,
            RecipeId = "recipe"
        });

        Assert.AreEqual(0, artifact.Safety.Clicks);
        Assert.AreEqual(0, artifact.Safety.CookiesAccepted);
        CollectionAssert.Contains(artifact.Safety.CartSignals.ToList(), "cart");
        CollectionAssert.Contains(artifact.Safety.PaymentSignals.ToList(), "pay");
    }

    [TestMethod]
    public void BuildFileName_Is_Safe_And_Stable_For_Input()
    {
        var artifact = ProductEvidenceArtifactWriter.Build(new ProductEvidenceArtifactInput
        {
            Evidence = CreateEvidence(price: null, currency: null),
            RecipeId = "../bad recipe",
            ProfileId = "..\\bad profile",
            RunId = "run/1",
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-11T15:00:00Z")
        });

        var fileName = ProductEvidenceArtifactWriter.BuildFileName(artifact);

        Assert.IsFalse(fileName.Contains("..", StringComparison.Ordinal));
        Assert.IsFalse(fileName.Contains('/', StringComparison.Ordinal));
        Assert.IsFalse(fileName.Contains('\\', StringComparison.Ordinal));
        StringAssert.StartsWith(fileName, "20260611-150000-");
    }

    [TestMethod]
    public void Write_Creates_Artifact_Directory_And_File()
    {
        var temp = CreateTempDirectory();
        try
        {
            var result = ProductEvidenceArtifactWriter.Write(temp, new ProductEvidenceArtifactInput
            {
                Evidence = CreateEvidence(price: null, currency: null),
                RecipeId = "recipe",
                RunId = "run-1",
                CreatedAtUtc = DateTimeOffset.Parse("2026-06-11T15:00:00Z")
            });

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(File.Exists(result.Path));
            StringAssert.Contains(result.RelativePath, "artifacts/product-evidence/");

            var json = File.ReadAllText(result.Path);
            StringAssert.Contains(json, "\"price\": null");
            StringAssert.Contains(json, "\"currency\": null");
            StringAssert.Contains(json, "\"stock\": null");
            var artifact = JsonSerializer.Deserialize<ProductEvidenceArtifact>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.IsNotNull(artifact);
            Assert.AreEqual("recipe", artifact!.RecipeId);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Does_Not_Escape_ProductEvidence_Artifacts_Root()
    {
        var temp = CreateTempDirectory();
        try
        {
            var result = ProductEvidenceArtifactWriter.Write(temp, new ProductEvidenceArtifactInput
            {
                Evidence = CreateEvidence(price: null, currency: null),
                RecipeId = "../../outside",
                ProfileId = "../profile",
                RunId = "../run",
                CreatedAtUtc = DateTimeOffset.Parse("2026-06-11T15:00:00Z")
            });

            Assert.IsTrue(result.Success, result.Error);
            var root = ProductEvidenceArtifactWriter.GetArtifactRoot(temp);
            StringAssert.StartsWith(Path.GetFullPath(result.Path), Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Sodimac_RawSignals_Do_Not_Become_Visible_Price()
    {
        var evidence = CreateEvidence(price: null, currency: null) with
        {
            RawSignals = ["public-html-signal: Product", "public-html-signal: price=38.18", "public-html-signal: priceCurrency=USD"]
        };

        var artifact = ProductEvidenceArtifactWriter.Build(new ProductEvidenceArtifactInput
        {
            Evidence = evidence,
            RecipeId = "sodimac-product-readonly-report",
            Notes = ["raw public HTML signal, not visible UIA price"]
        });

        Assert.IsNull(artifact.Evidence.Price);
        Assert.IsNull(artifact.Evidence.Currency);
        CollectionAssert.Contains(artifact.Evidence.RawSignals.ToList(), "public-html-signal: price=38.18");
        CollectionAssert.Contains(artifact.Notes.ToList(), "raw public HTML signal, not visible UIA price");
        Assert.AreEqual(1, artifact.Notes.Count(note => note == "raw public HTML signal, not visible UIA price"));
    }

    private static ProductEvidence CreateEvidence(string? price, string? currency)
    {
        return new ProductEvidence
        {
            SourceUrl = "https://example.test/product",
            SourceProfileId = "profile",
            PageTitle = "Piso flotante Essen - Sodimac",
            ProductName = "Piso flotante Essen",
            Brand = "Sodimac",
            Category = "pisos/revestimientos",
            Description = "Producto de prueba",
            Price = price,
            Currency = currency,
            Stock = null,
            Seller = "Sodimac",
            BlockedOrMissingFields = price == null
                ? ["missing_price", "missing_currency", "missing_stock"]
                : ["missing_stock"],
            ExtractionConfidence = price == null ? "medium" : "high",
            ExtractionStatus = price == null ? "missing_price" : "missing_stock",
            ExtractionNotes = ["price_not_visible_or_not_proven"]
        };
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onebrain-artifact-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
