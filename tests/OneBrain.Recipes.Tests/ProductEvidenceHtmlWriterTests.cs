using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceHtmlWriterTests
{
    [TestMethod]
    public void Write_Creates_Report_Directory_And_File()
    {
        var temp = CreateTempDirectory();
        try
        {
            var result = ProductEvidenceHtmlWriter.Write(temp, CreateSummary());

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(File.Exists(result.Path));
            StringAssert.Contains(result.RelativePath, "artifacts/product-evidence-html-reports/");
            StringAssert.Contains(File.ReadAllText(result.Path), "<!doctype html>");
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Does_Not_Write_Outside_Artifacts_Root()
    {
        var temp = CreateTempDirectory();
        try
        {
            var result = ProductEvidenceHtmlWriter.Write(temp, CreateSummary(), "artifacts/product-evidence-html-reports/../../outside");

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Error, "output path escaped");
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Does_Not_Overwrite_Existing_Report_File()
    {
        var temp = CreateTempDirectory();
        try
        {
            var summary = CreateSummary();

            var first = ProductEvidenceHtmlWriter.Write(temp, summary);
            var second = ProductEvidenceHtmlWriter.Write(temp, summary);

            Assert.IsTrue(first.Success, first.Error);
            Assert.IsTrue(second.Success, second.Error);
            Assert.AreNotEqual(first.Path, second.Path);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static ProductEvidenceSummary CreateSummary()
    {
        return new ProductEvidenceSummary
        {
            CreatedAtUtc = "2026-06-11T15:00:00Z",
            SourceArtifactCount = 1,
            ValidArtifactCount = 1,
            Items =
            [
                new ProductEvidenceSummaryItem
                {
                    ProductName = "Demo Product",
                    ProfileId = "demo-fixture",
                    HasPrice = true,
                    HasCurrency = true,
                    Price = "199.00",
                    Currency = "USD",
                    ExtractionStatus = "complete",
                    ExtractionConfidence = "high",
                    EvidenceScore = 90,
                    EvidenceGrade = "excellent",
                    DecisionReadiness = "ready_for_comparison",
                    SafetySummary = new ProductEvidenceSafetySummary { Clicks = 0 }
                }
            ],
            Totals = new ProductEvidenceSummaryTotals
            {
                ProductsWithPrice = 1,
                AverageEvidenceScore = 90,
                ReadyForComparisonCount = 1
            }
        };
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onebrain-html-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
