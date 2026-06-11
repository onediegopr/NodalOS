namespace OneBrain.Core.Extraction;

public static class ProductEvidenceQualityScorer
{
    public static ProductEvidenceQualityScore Score(ProductEvidenceSummaryItem item)
    {
        var score = 0;
        var reasons = new List<string>();
        var missing = new List<string>();

        if (HasText(item.ProductName))
        {
            score += 25;
            reasons.Add("productName present: +25");
        }
        else
        {
            missing.Add("missing_productName");
        }

        if (HasText(item.SourceUrl))
        {
            score += 15;
            reasons.Add("sourceUrl present: +15");
        }
        else
        {
            missing.Add("missing_sourceUrl");
        }

        if (HasText(item.Category))
        {
            score += 10;
            reasons.Add("category present: +10");
        }
        else
        {
            missing.Add("missing_category");
        }

        if (item.HasPrice && item.HasCurrency)
        {
            score += 25;
            reasons.Add("visible price and currency present: +25");
        }
        else
        {
            if (!item.HasPrice)
                missing.Add("missing_price");
            if (!item.HasCurrency)
                missing.Add("missing_currency");
        }

        if (item.HasStock)
        {
            score += 10;
            reasons.Add("stock present: +10");
        }
        else
        {
            missing.Add("missing_stock");
        }

        if (EqualsIgnoreCase(item.ExtractionConfidence, "high"))
        {
            score += 10;
            reasons.Add("confidence high: +10");
        }
        else if (EqualsIgnoreCase(item.ExtractionConfidence, "medium"))
        {
            score += 5;
            reasons.Add("confidence medium: +5");
        }

        if (HasCleanSafety(item.SafetySummary))
        {
            score += 5;
            reasons.Add("safe read-only evidence: +5");
        }

        if (ContainsToken(item.BlockedOrMissingFields, "missing_price") || !item.HasPrice)
        {
            score -= 15;
            reasons.Add("missing_price penalty: -15");
        }

        if (ContainsToken(item.BlockedOrMissingFields, "missing_currency") || !item.HasCurrency)
        {
            score -= 10;
            reasons.Add("missing_currency penalty: -10");
        }

        var diagnostic = EqualsIgnoreCase(item.ExtractionStatus, "diagnostic") ||
            EqualsIgnoreCase(item.ExtractionConfidence, "diagnostic");
        if (diagnostic)
        {
            score -= 30;
            reasons.Add("diagnostic evidence penalty: -30");
        }

        var blocked = ContainsAny(item.BlockedOrMissingFields, ["blocked", "geoloc", "cookie", "captcha", "dynamic_content"]);
        if (blocked)
        {
            score -= 20;
            reasons.Add("blocked/geoloc/cookie evidence penalty: -20");
        }

        var commercialSignals = item.SafetySummary.PaymentSignals.Count +
            item.SafetySummary.CartSignals.Count +
            item.SafetySummary.BuySignals.Count;
        if (commercialSignals > 0)
        {
            score -= 10;
            reasons.Add("cart/buy/payment signals reduce readiness: -10");
        }

        if (!diagnostic &&
            HasText(item.ProductName) &&
            HasText(item.SourceUrl) &&
            HasText(item.Category) &&
            (EqualsIgnoreCase(item.ExtractionConfidence, "medium") || EqualsIgnoreCase(item.ExtractionConfidence, "high")) &&
            score < 50)
        {
            score = 50;
            reasons.Add("identified product evidence floor: 50");
        }

        score = Math.Clamp(score, 0, 100);

        return new ProductEvidenceQualityScore
        {
            EvidenceScore = score,
            EvidenceGrade = Grade(score),
            QualityStatus = QualityStatus(score, diagnostic),
            QualityReasons = reasons,
            MissingCriticalFields = missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            DecisionReadiness = DecisionReadiness(score, diagnostic, item, commercialSignals)
        };
    }

    private static string Grade(int score)
    {
        if (score >= 85) return "excellent";
        if (score >= 70) return "good";
        if (score >= 50) return "partial";
        if (score >= 25) return "weak";
        return "insufficient";
    }

    private static string QualityStatus(int score, bool diagnostic)
    {
        if (diagnostic) return "diagnostic";
        if (score >= 70) return "sufficient";
        if (score >= 50) return "partial";
        return "insufficient";
    }

    private static string DecisionReadiness(int score, bool diagnostic, ProductEvidenceSummaryItem item, int commercialSignals)
    {
        if (diagnostic) return "diagnostic_only";
        if (!item.HasPrice || !item.HasCurrency) return "needs_price_verification";
        if (commercialSignals > 0 || score < 70) return "needs_more_evidence";
        return "ready_for_comparison";
    }

    private static bool HasCleanSafety(ProductEvidenceSafetySummary safety)
    {
        return safety.Clicks == 0 &&
            safety.CookiesAccepted == 0 &&
            safety.PaymentSignals.Count == 0 &&
            safety.CartSignals.Count == 0 &&
            safety.BuySignals.Count == 0;
    }

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    private static bool EqualsIgnoreCase(string? value, string expected)
    {
        return string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsToken(IEnumerable<string> values, string token)
    {
        return values.Any(value => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(IEnumerable<string> values, IReadOnlyList<string> tokens)
    {
        return values.Any(value => tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }
}
