using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceQualityScorerTests
{
    [TestMethod]
    public void Score_ProductIdentityWithoutPrice_IsPartialAndNeedsPriceVerification()
    {
        var score = ProductEvidenceQualityScorer.Score(CreateItem(hasPrice: false, hasCurrency: false));

        Assert.AreEqual(50, score.EvidenceScore);
        Assert.AreEqual("partial", score.EvidenceGrade);
        Assert.AreEqual("partial", score.QualityStatus);
        Assert.AreEqual("needs_price_verification", score.DecisionReadiness);
        CollectionAssert.Contains(score.MissingCriticalFields.ToList(), "missing_price");
    }

    [TestMethod]
    public void Score_FullHighConfidenceEvidence_IsExcellentAndReady()
    {
        var score = ProductEvidenceQualityScorer.Score(CreateItem(
            hasPrice: true,
            hasCurrency: true,
            hasStock: true,
            confidence: "high",
            blockedOrMissingFields: []));

        Assert.AreEqual(100, score.EvidenceScore);
        Assert.AreEqual("excellent", score.EvidenceGrade);
        Assert.AreEqual("sufficient", score.QualityStatus);
        Assert.AreEqual("ready_for_comparison", score.DecisionReadiness);
    }

    [TestMethod]
    public void Score_MissingPrice_LowersReadiness()
    {
        var score = ProductEvidenceQualityScorer.Score(CreateItem(hasPrice: false, hasCurrency: true));

        Assert.AreEqual("needs_price_verification", score.DecisionReadiness);
        CollectionAssert.Contains(score.QualityReasons.ToList(), "missing_price penalty: -15");
    }

    [TestMethod]
    public void Score_DiagnosticEvidence_IsDiagnosticOnly()
    {
        var score = ProductEvidenceQualityScorer.Score(CreateItem(
            status: "diagnostic",
            confidence: "diagnostic"));

        Assert.AreEqual("diagnostic", score.QualityStatus);
        Assert.AreEqual("diagnostic_only", score.DecisionReadiness);
    }

    [TestMethod]
    public void Score_RawSignals_DoNotImproveVisiblePriceScore()
    {
        var withoutRaw = ProductEvidenceQualityScorer.Score(CreateItem(hasPrice: false, hasCurrency: false, rawSignalCount: 0));
        var withRaw = ProductEvidenceQualityScorer.Score(CreateItem(hasPrice: false, hasCurrency: false, rawSignalCount: 2));

        Assert.AreEqual(withoutRaw.EvidenceScore, withRaw.EvidenceScore);
        Assert.AreEqual("needs_price_verification", withRaw.DecisionReadiness);
    }

    [TestMethod]
    public void Score_CleanSafetyAddsReason()
    {
        var score = ProductEvidenceQualityScorer.Score(CreateItem());

        CollectionAssert.Contains(score.QualityReasons.ToList(), "safe read-only evidence: +5");
    }

    [TestMethod]
    public void Score_CommercialSignalsLowerReadiness()
    {
        var score = ProductEvidenceQualityScorer.Score(CreateItem(
            hasPrice: true,
            hasCurrency: true,
            blockedOrMissingFields: [],
            safety: new ProductEvidenceSafetySummary { BuySignals = ["comprar"] }));

        Assert.AreEqual("needs_more_evidence", score.DecisionReadiness);
        CollectionAssert.Contains(score.QualityReasons.ToList(), "cart/buy/payment signals reduce readiness: -10");
    }

    private static ProductEvidenceSummaryItem CreateItem(
        bool hasPrice = false,
        bool hasCurrency = false,
        bool hasStock = false,
        string status = "missing_price",
        string confidence = "medium",
        int rawSignalCount = 0,
        IReadOnlyList<string>? blockedOrMissingFields = null,
        ProductEvidenceSafetySummary? safety = null)
    {
        return new ProductEvidenceSummaryItem
        {
            RecipeId = "recipe",
            ProfileId = "profile",
            SourceUrl = "https://example.test/product",
            ProductName = "Piso flotante Essen",
            Category = "pisos/revestimientos",
            Price = hasPrice ? "38.18" : null,
            Currency = hasCurrency ? "USD" : null,
            Stock = hasStock ? "available" : null,
            ExtractionStatus = status,
            ExtractionConfidence = confidence,
            BlockedOrMissingFields = blockedOrMissingFields ?? ["missing_price", "missing_currency", "missing_stock"],
            HasPrice = hasPrice,
            HasCurrency = hasCurrency,
            HasStock = hasStock,
            RawSignalCount = rawSignalCount,
            SafetySummary = safety ?? new ProductEvidenceSafetySummary { Clicks = 0, CookiesAccepted = 0 }
        };
    }
}
