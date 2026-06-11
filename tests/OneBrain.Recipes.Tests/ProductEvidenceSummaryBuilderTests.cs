using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceSummaryBuilderTests
{
    [TestMethod]
    public void Build_Loads_Two_Valid_Artifacts()
    {
        var summary = ProductEvidenceSummaryBuilder.Build([
            ValidSource("a.json", CreateArtifact("recipe-a", "profile-a", price: null, currency: null)),
            ValidSource("b.json", CreateArtifact("recipe-b", "profile-b", price: "38.18", currency: "USD"))
        ], DateTimeOffset.Parse("2026-06-11T15:00:00Z"));

        Assert.AreEqual("product-evidence-summary/v1", summary.SchemaVersion);
        Assert.AreEqual(2, summary.SourceArtifactCount);
        Assert.AreEqual(2, summary.ValidArtifactCount);
        Assert.AreEqual(0, summary.InvalidArtifactCount);
        Assert.AreEqual(2, summary.Items.Count);
    }

    [TestMethod]
    public void Build_Preserves_Null_Price_And_Marks_HasPrice_False()
    {
        var summary = ProductEvidenceSummaryBuilder.Build([
            ValidSource("a.json", CreateArtifact("recipe-a", "profile-a", price: null, currency: null))
        ]);

        Assert.IsNull(summary.Items[0].Price);
        Assert.IsNull(summary.Items[0].Currency);
        Assert.IsFalse(summary.Items[0].HasPrice);
        Assert.IsFalse(summary.Items[0].HasCurrency);
    }

    [TestMethod]
    public void Build_Does_Not_Convert_RawSignals_Price_To_Visible_Price()
    {
        var artifact = CreateArtifact("sodimac", "sodimac-product", price: null, currency: null) with
        {
            Evidence = CreateEvidence(price: null, currency: null) with
            {
                RawSignals = ["public-html-signal: price=38.18", "public-html-signal: priceCurrency=USD"]
            }
        };

        var summary = ProductEvidenceSummaryBuilder.Build([ValidSource("sodimac.json", artifact)]);

        Assert.IsNull(summary.Items[0].Price);
        Assert.IsNull(summary.Items[0].Currency);
        Assert.IsFalse(summary.Items[0].HasPrice);
        Assert.AreEqual(2, summary.Items[0].RawSignalCount);
        CollectionAssert.Contains(summary.Notes.ToList(), "rawSignals are not treated as visible normalized price");
    }

    [TestMethod]
    public void Build_Calculates_Missing_And_With_Price_Totals()
    {
        var summary = ProductEvidenceSummaryBuilder.Build([
            ValidSource("missing.json", CreateArtifact("missing", "profile", price: null, currency: null)),
            ValidSource("priced.json", CreateArtifact("priced", "profile", price: "38.18", currency: "USD"))
        ]);

        Assert.AreEqual(1, summary.Totals.ProductsMissingPrice);
        Assert.AreEqual(1, summary.Totals.ProductsWithPrice);
    }

    [TestMethod]
    public void Build_Includes_BlockedOrMissingFields()
    {
        var summary = ProductEvidenceSummaryBuilder.Build([
            ValidSource("missing.json", CreateArtifact("missing", "profile", price: null, currency: null))
        ]);

        CollectionAssert.Contains(summary.Items[0].BlockedOrMissingFields.ToList(), "missing_price");
    }

    [TestMethod]
    public void Build_Aggregates_Safety_Clicks_Total()
    {
        var artifact = CreateArtifact("recipe", "profile", price: null, currency: null) with
        {
            Safety = new ProductEvidenceSafetySummary
            {
                Clicks = 0,
                PaymentSignals = ["payment"]
            }
        };

        var summary = ProductEvidenceSummaryBuilder.Build([ValidSource("a.json", artifact)]);

        Assert.AreEqual(0, summary.Totals.SafetyClicksTotal);
        Assert.AreEqual(1, summary.Totals.SafetyPaymentsSignalsTotal);
    }

    [TestMethod]
    public void Build_Reports_Invalid_Artifact_Without_Dropping_Valid()
    {
        var summary = ProductEvidenceSummaryBuilder.Build([
            ValidSource("valid.json", CreateArtifact("valid", "profile", price: null, currency: null)),
            new ProductEvidenceSummarySource { ArtifactPath = "bad.json", Error = "invalid json" }
        ]);

        Assert.AreEqual(2, summary.SourceArtifactCount);
        Assert.AreEqual(1, summary.ValidArtifactCount);
        Assert.AreEqual(1, summary.InvalidArtifactCount);
        CollectionAssert.Contains(summary.InvalidArtifacts.ToList(), "bad.json: invalid json");
    }

    [TestMethod]
    public void Build_Empty_Source_Produces_Diagnostic_Empty_Summary()
    {
        var summary = ProductEvidenceSummaryBuilder.Build([]);

        Assert.AreEqual(0, summary.SourceArtifactCount);
        Assert.AreEqual(0, summary.ValidArtifactCount);
        Assert.AreEqual(0, summary.InvalidArtifactCount);
        CollectionAssert.Contains(summary.Notes.ToList(), "diagnostic: no product evidence artifacts found");
    }

    [TestMethod]
    public void Write_Creates_Summary_Directory_And_File()
    {
        var temp = CreateTempDirectory();
        try
        {
            WriteArtifact(temp, "one.json", CreateArtifact("recipe", "profile", price: null, currency: null));

            var result = ProductEvidenceSummaryWriter.WriteFromDirectory(temp, createdAtUtc: DateTimeOffset.Parse("2026-06-11T15:00:00Z"));

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(File.Exists(result.Path));
            StringAssert.Contains(result.RelativePath, "artifacts/product-evidence-summary/");
            Assert.AreEqual(1, result.Summary.ValidArtifactCount);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Does_Not_Write_Outside_Summary_Root()
    {
        var temp = CreateTempDirectory();
        try
        {
            WriteArtifact(temp, "one.json", CreateArtifact("recipe", "profile", price: null, currency: null));

            var result = ProductEvidenceSummaryWriter.WriteFromDirectory(temp, outputDirectory: "artifacts/product-evidence-summary/../../outside");

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Error, "output path escaped");
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Does_Not_Read_Outside_ProductEvidence_Root()
    {
        var temp = CreateTempDirectory();
        try
        {
            var result = ProductEvidenceSummaryWriter.WriteFromDirectory(temp, inputDirectory: "artifacts/product-evidence/../../outside");

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Error, "input path escaped");
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Invalid_Artifact_Is_Diagnostic_And_Does_Not_Break_Valid()
    {
        var temp = CreateTempDirectory();
        try
        {
            WriteArtifact(temp, "valid.json", CreateArtifact("recipe", "profile", price: null, currency: null));
            var artifactRoot = ProductEvidenceArtifactWriter.GetArtifactRoot(temp);
            File.WriteAllText(Path.Combine(artifactRoot, "invalid.json"), "{not-json");

            var result = ProductEvidenceSummaryWriter.WriteFromDirectory(temp);

            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(2, result.Summary.SourceArtifactCount);
            Assert.AreEqual(1, result.Summary.ValidArtifactCount);
            Assert.AreEqual(1, result.Summary.InvalidArtifactCount);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Does_Not_Overwrite_Existing_Summary_File()
    {
        var temp = CreateTempDirectory();
        try
        {
            WriteArtifact(temp, "one.json", CreateArtifact("recipe", "profile", price: null, currency: null));
            var createdAt = DateTimeOffset.Parse("2026-06-11T15:00:00Z");

            var first = ProductEvidenceSummaryWriter.WriteFromDirectory(temp, createdAtUtc: createdAt);
            var second = ProductEvidenceSummaryWriter.WriteFromDirectory(temp, createdAtUtc: createdAt);

            Assert.IsTrue(first.Success, first.Error);
            Assert.IsTrue(second.Success, second.Error);
            Assert.AreNotEqual(first.Path, second.Path);
            Assert.IsTrue(File.Exists(first.Path));
            Assert.IsTrue(File.Exists(second.Path));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static ProductEvidenceSummarySource ValidSource(string path, ProductEvidenceArtifact artifact)
    {
        return new ProductEvidenceSummarySource
        {
            Artifact = artifact,
            ArtifactPath = path
        };
    }

    private static ProductEvidenceArtifact CreateArtifact(string recipeId, string profileId, string? price, string? currency)
    {
        return ProductEvidenceArtifactWriter.Build(new ProductEvidenceArtifactInput
        {
            Evidence = CreateEvidence(price, currency) with
            {
                SourceProfileId = profileId
            },
            RecipeId = recipeId,
            ProfileId = profileId,
            RunId = recipeId + "-run",
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-11T15:00:00Z")
        });
    }

    private static ProductEvidence CreateEvidence(string? price, string? currency)
    {
        return new ProductEvidence
        {
            SourceUrl = "https://example.test/product",
            SourceProfileId = "profile",
            PageTitle = "Piso flotante Essen - Sodimac",
            ProductName = "Piso flotante Essen",
            Category = "pisos/revestimientos",
            Price = price,
            Currency = currency,
            Stock = null,
            RawSignals = [],
            BlockedOrMissingFields = price == null
                ? ["missing_price", "missing_currency", "missing_stock"]
                : ["missing_stock"],
            ExtractionConfidence = price == null ? "medium" : "high",
            ExtractionStatus = price == null ? "missing_price" : "missing_stock"
        };
    }

    private static void WriteArtifact(string baseDirectory, string fileName, ProductEvidenceArtifact artifact)
    {
        var root = ProductEvidenceArtifactWriter.GetArtifactRoot(baseDirectory);
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, fileName), JsonSerializer.Serialize(artifact, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onebrain-summary-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
