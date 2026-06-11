using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceMarkdownRendererTests
{
    [TestMethod]
    public void Render_Generates_Main_Title()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "# ONE BRAIN - Product Evidence Summary");
    }

    [TestMethod]
    public void Render_Demo_Generates_Stable_Demo_Title()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateDemoSummary());

        StringAssert.Contains(markdown, "# ONE BRAIN - Stable Product Evidence Demo");
    }

    [TestMethod]
    public void Render_Demo_Includes_What_This_Demo_Shows()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateDemoSummary());

        StringAssert.Contains(markdown, "## What this demo shows");
        StringAssert.Contains(markdown, "No live web access is required for this demo.");
    }

    [TestMethod]
    public void Render_Demo_Includes_Safety_Guarantees()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateDemoSummary());

        StringAssert.Contains(markdown, "## Important safety guarantees");
        StringAssert.Contains(markdown, "No browser or web navigation is required by the demo report recipe.");
    }

    [TestMethod]
    public void Render_Demo_Includes_Decision_Readiness()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateDemoSummary());

        StringAssert.Contains(markdown, "## Decision readiness");
        StringAssert.Contains(markdown, "ready_for_comparison");
    }

    [TestMethod]
    public void Render_Includes_Summary_Table()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "## Summary");
        StringAssert.Contains(markdown, "| Metric | Value |");
        StringAssert.Contains(markdown, "| Source artifacts | 1 |");
    }

    [TestMethod]
    public void Render_Includes_Products_Table()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "## Products");
        StringAssert.Contains(markdown, "| Product | Source | Price | Currency | Status | Confidence | Score | Grade | Readiness | Missing fields |");
    }

    [TestMethod]
    public void Render_Null_Price_And_Currency_As_Dash()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "| Placa Marmol Blanco Firenze | suministrosroca-uy-product | \u2014 | \u2014 | missing_price | medium | 50 | partial | needs_price_verification | missing_price, missing_currency, missing_stock |");
    }

    [TestMethod]
    public void Render_Includes_Quality_Metrics()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "| Partial evidence | 1 |");
        StringAssert.Contains(markdown, "| Average evidence score | 50 |");
        StringAssert.Contains(markdown, "| Products needing price verification | 1 |");
        StringAssert.Contains(markdown, "| Needs price verification | 1 |");
    }

    [TestMethod]
    public void Render_Includes_Score_Grade_And_Readiness()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "Score | Grade | Readiness");
        StringAssert.Contains(markdown, "50 | partial | needs_price_verification");
    }

    [TestMethod]
    public void Render_Does_Not_Show_RawSignal_Price_As_Normalized_Price()
    {
        var summary = CreateSummary(rawSignalCount: 2);
        var markdown = ProductEvidenceMarkdownRenderer.Render(summary);

        Assert.IsFalse(markdown.Contains("38.18", StringComparison.Ordinal));
        StringAssert.Contains(markdown, "Raw signals are not treated as visible normalized price.");
    }

    [TestMethod]
    public void Render_Includes_Missing_Fields()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "missing_price, missing_currency, missing_stock");
    }

    [TestMethod]
    public void Render_Includes_Safety_Clicks_Total_Zero()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(CreateSummary());

        StringAssert.Contains(markdown, "| Safety clicks total | 0 |");
    }

    [TestMethod]
    public void Render_Escapes_Pipes_And_Newlines_In_Table_Cells()
    {
        var summary = CreateSummary(productName: "Piso | especial\r\nnuevo", profileId: "source|one");
        var markdown = ProductEvidenceMarkdownRenderer.Render(summary);

        StringAssert.Contains(markdown, "Piso \\| especial  nuevo");
        StringAssert.Contains(markdown, "source\\|one");
    }

    [TestMethod]
    public void Render_Empty_Summary_Is_Diagnostic()
    {
        var markdown = ProductEvidenceMarkdownRenderer.Render(new ProductEvidenceSummary
        {
            CreatedAtUtc = "2026-06-11T15:00:00Z"
        });

        StringAssert.Contains(markdown, "diagnostic: no products");
    }

    [TestMethod]
    public void Write_Creates_Report_Directory_And_File()
    {
        var temp = CreateTempDirectory();
        try
        {
            var result = ProductEvidenceMarkdownWriter.Write(temp, CreateSummary());

            Assert.IsTrue(result.Success, result.Error);
            Assert.IsTrue(File.Exists(result.Path));
            StringAssert.Contains(result.RelativePath, "artifacts/product-evidence-reports/");
            StringAssert.Contains(File.ReadAllText(result.Path), "# ONE BRAIN - Product Evidence Summary");
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [TestMethod]
    public void Write_Does_Not_Write_Outside_Report_Root()
    {
        var temp = CreateTempDirectory();
        try
        {
            var result = ProductEvidenceMarkdownWriter.Write(temp, CreateSummary(), "artifacts/product-evidence-reports/../../outside");

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

            var first = ProductEvidenceMarkdownWriter.Write(temp, summary);
            var second = ProductEvidenceMarkdownWriter.Write(temp, summary);

            Assert.IsTrue(first.Success, first.Error);
            Assert.IsTrue(second.Success, second.Error);
            Assert.AreNotEqual(first.Path, second.Path);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static ProductEvidenceSummary CreateDemoSummary()
    {
        return CreateSummary(
            profileId: "demo-fixture",
            artifactPath: "samples/product-evidence/20260611-demo-complete-fixture.json",
            decisionReadiness: "ready_for_comparison",
            readyForComparisonCount: 1);
    }

    private static ProductEvidenceSummary CreateSummary(
        string productName = "Placa Marmol Blanco Firenze",
        string profileId = "suministrosroca-uy-product",
        int rawSignalCount = 0,
        string artifactPath = "artifacts/product-evidence/example.json",
        string decisionReadiness = "needs_price_verification",
        int readyForComparisonCount = 0)
    {
        var item = new ProductEvidenceSummaryItem
        {
            RecipeId = "suministrosroca-product-readonly-report",
            ProfileId = profileId,
            SourceUrl = "https://suministrosroca.uy/producto/placa-marmol-blanco-firenze/",
            ProductName = productName,
            Category = "placa/revestimiento",
            Price = null,
            Currency = null,
            Stock = null,
            ExtractionStatus = "missing_price",
            ExtractionConfidence = "medium",
            BlockedOrMissingFields = ["missing_price", "missing_currency", "missing_stock"],
            HasPrice = false,
            HasCurrency = false,
            HasStock = false,
            RawSignalCount = rawSignalCount,
            SafetySummary = new ProductEvidenceSafetySummary { Clicks = 0, CookiesAccepted = 0 },
            ArtifactPath = artifactPath,
            EvidenceScore = 50,
            EvidenceGrade = "partial",
            QualityStatus = "partial",
            QualityReasons = ["identified product evidence floor: 50"],
            MissingCriticalFields = ["missing_price", "missing_currency", "missing_stock"],
            DecisionReadiness = decisionReadiness
        };

        return new ProductEvidenceSummary
        {
            CreatedAtUtc = "2026-06-11T15:00:00Z",
            SourceArtifactCount = 1,
            ValidArtifactCount = 1,
            InvalidArtifactCount = 0,
            Items = [item],
            Totals = new ProductEvidenceSummaryTotals
            {
                ProductsWithPrice = 0,
                ProductsMissingPrice = 1,
                ProductsWithMediumConfidence = 1,
                SafetyClicksTotal = 0,
                SafetyPaymentsSignalsTotal = 0,
                ArtifactsWithWarnings = 1,
                PartialCount = 1,
                AverageEvidenceScore = 50,
                ReadyForComparisonCount = readyForComparisonCount,
                NeedsPriceVerificationCount = 1
            },
            Notes = ["rawSignals are not treated as visible normalized price"]
        };
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onebrain-markdown-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
