using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceHtmlRendererTests
{
    [TestMethod]
    public void Render_Emits_Doctype()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        StringAssert.StartsWith(html, "<!doctype html>");
    }

    [TestMethod]
    public void Render_Includes_Title_And_Header()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        StringAssert.Contains(html, "<title>ONE BRAIN - Product Evidence Report</title>");
        StringAssert.Contains(html, "ONE BRAIN \u2014 Product Evidence Report");
    }

    [TestMethod]
    public void Render_Includes_Summary_Metrics()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        StringAssert.Contains(html, "Source artifacts");
        StringAssert.Contains(html, "Products needing price verification");
        StringAssert.Contains(html, "Average evidence score");
        StringAssert.Contains(html, "Safety clicks total");
    }

    [TestMethod]
    public void Render_Includes_Products_Table()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        StringAssert.Contains(html, "<h2 id=\"products-heading\">Products</h2>");
        StringAssert.Contains(html, "<th>Product</th>");
        StringAssert.Contains(html, "<th>Readiness</th>");
    }

    [TestMethod]
    public void Render_Includes_Score_Grade_And_Readiness()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        StringAssert.Contains(html, "<th class=\"number\">Score</th>");
        StringAssert.Contains(html, "<th>Grade</th>");
        StringAssert.Contains(html, "needs_price_verification");
    }

    [TestMethod]
    public void Render_Null_Price_And_Currency_As_Dash()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        StringAssert.Contains(html, "<td class=\"number\">\u2014</td><td>\u2014</td>");
    }

    [TestMethod]
    public void Render_Escapes_Html_Content()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary(productName: "<Piso & \"especial\">", profileId: "source <one>"));

        StringAssert.Contains(html, "&lt;Piso &amp; &quot;especial&quot;&gt;");
        StringAssert.Contains(html, "source &lt;one&gt;");
        Assert.IsFalse(html.Contains("<Piso &", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Render_Does_Not_Reference_External_Css_Js_Or_Cdn()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<link", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("cdn", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("https://", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Render_Does_Not_Show_RawSignal_Price_As_Normalized_Price()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary(rawSignalCount: 2));

        Assert.IsFalse(html.Contains("38.18", StringComparison.Ordinal));
        StringAssert.Contains(html, "Raw signals are not visible normalized price.");
    }

    [TestMethod]
    public void Render_Includes_Invalid_Artifacts_Section()
    {
        var html = ProductEvidenceHtmlRenderer.Render(CreateSummary());

        StringAssert.Contains(html, "<h2 id=\"invalid-heading\">Invalid artifacts</h2>");
        StringAssert.Contains(html, "<li>None.</li>");
    }

    [TestMethod]
    public void Render_Empty_Summary_Is_Diagnostic()
    {
        var html = ProductEvidenceHtmlRenderer.Render(new ProductEvidenceSummary
        {
            CreatedAtUtc = "2026-06-11T15:00:00Z"
        });

        StringAssert.Contains(html, "diagnostic: no products");
        StringAssert.Contains(html, "diagnostic_only");
    }

    private static ProductEvidenceSummary CreateSummary(
        string productName = "Placa Marmol Blanco Firenze",
        string profileId = "suministrosroca-uy-product",
        int rawSignalCount = 0)
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
            ArtifactPath = "artifacts/product-evidence/example.json",
            EvidenceScore = 50,
            EvidenceGrade = "partial",
            QualityStatus = "partial",
            QualityReasons = ["identified product evidence floor: 50"],
            MissingCriticalFields = ["missing_price", "missing_currency", "missing_stock"],
            DecisionReadiness = "needs_price_verification"
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
                NeedsPriceVerificationCount = 1
            },
            Notes = ["rawSignals are not treated as visible normalized price"]
        };
    }
}
