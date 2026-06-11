using System.Text;

namespace OneBrain.Core.Extraction;

public static class ProductEvidenceMarkdownRenderer
{
    private const string Dash = "\u2014";

    public static string Render(ProductEvidenceSummary summary)
    {
        var sb = new StringBuilder();
        var isDemo = IsDemoSummary(summary);

        sb.AppendLine(isDemo
            ? "# ONE BRAIN - Stable Product Evidence Demo"
            : "# ONE BRAIN - Product Evidence Summary");
        sb.AppendLine();
        sb.AppendLine($"Generated: {ValueOrDash(summary.CreatedAtUtc)}");
        sb.AppendLine();

        if (isDemo)
            AppendDemoIntro(sb);

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|---|---:|");
        AppendMetric(sb, "Source artifacts", summary.SourceArtifactCount);
        AppendMetric(sb, "Valid artifacts", summary.ValidArtifactCount);
        AppendMetric(sb, "Invalid artifacts", summary.InvalidArtifactCount);
        AppendMetric(sb, "Products with price", summary.Totals.ProductsWithPrice);
        AppendMetric(sb, "Products needing price verification", summary.Totals.ProductsMissingPrice);
        AppendMetric(sb, "Sufficient evidence", summary.Totals.SufficientCount);
        AppendMetric(sb, "Partial evidence", summary.Totals.PartialCount);
        AppendMetric(sb, "Insufficient evidence", summary.Totals.InsufficientCount);
        AppendMetric(sb, "Diagnostic evidence", summary.Totals.DiagnosticCount);
        AppendMetric(sb, "Average evidence score", summary.Totals.AverageEvidenceScore);
        AppendMetric(sb, "Ready for comparison", summary.Totals.ReadyForComparisonCount);
        AppendMetric(sb, "Needs price verification", summary.Totals.NeedsPriceVerificationCount);
        AppendMetric(sb, "Safety clicks total", summary.Totals.SafetyClicksTotal);
        AppendMetric(sb, "Safety payment signals total", summary.Totals.SafetyPaymentsSignalsTotal);
        sb.AppendLine();

        if (isDemo)
            AppendDecisionReadiness(sb);

        sb.AppendLine("## Products");
        sb.AppendLine();
        sb.AppendLine("| Product | Source | Price | Currency | Status | Confidence | Score | Grade | Readiness | Missing fields |");
        sb.AppendLine("|---|---|---:|---|---|---|---:|---|---|---|");
        if (summary.Items.Count == 0)
        {
            sb.AppendLine($"| diagnostic: no products | {Dash} | {Dash} | {Dash} | diagnostic | diagnostic | 0 | insufficient | diagnostic_only | {Dash} |");
        }
        else
        {
            foreach (var item in summary.Items)
            {
                sb.Append("| ");
                sb.Append(Cell(ValueOrDash(item.ProductName)));
                sb.Append(" | ");
                sb.Append(Cell(ValueOrDash(item.ProfileId)));
                sb.Append(" | ");
                sb.Append(Cell(item.HasPrice ? ValueOrDash(item.Price) : Dash));
                sb.Append(" | ");
                sb.Append(Cell(item.HasCurrency ? ValueOrDash(item.Currency) : Dash));
                sb.Append(" | ");
                sb.Append(Cell(ValueOrDash(item.ExtractionStatus)));
                sb.Append(" | ");
                sb.Append(Cell(ValueOrDash(item.ExtractionConfidence)));
                sb.Append(" | ");
                sb.Append(item.EvidenceScore);
                sb.Append(" | ");
                sb.Append(Cell(ValueOrDash(item.EvidenceGrade)));
                sb.Append(" | ");
                sb.Append(Cell(ValueOrDash(item.DecisionReadiness)));
                sb.Append(" | ");
                sb.Append(Cell(item.BlockedOrMissingFields.Count == 0
                    ? Dash
                    : string.Join(", ", item.BlockedOrMissingFields)));
                sb.AppendLine(" |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Notes");
        sb.AppendLine();
        sb.AppendLine("- Raw signals are not treated as visible normalized price.");
        sb.AppendLine("- Missing price means the visible/UIA evidence did not confirm a price.");
        sb.AppendLine("- Missing price is evidence incompleteness, not a technical failure by itself.");
        sb.AppendLine("- Score, grade, and readiness evaluate captured evidence only; they do not create product data.");
        sb.AppendLine("- Safety summary: no clicks, no checkout, no payments.");
        foreach (var note in summary.Notes)
            sb.AppendLine($"- {ValueOrDash(note)}");
        sb.AppendLine();

        sb.AppendLine("## Invalid artifacts");
        sb.AppendLine();
        if (summary.InvalidArtifacts.Count == 0)
        {
            sb.AppendLine("- None.");
        }
        else
        {
            foreach (var invalid in summary.InvalidArtifacts)
                sb.AppendLine($"- {ValueOrDash(invalid)}");
        }

        return sb.ToString();
    }

    private static bool IsDemoSummary(ProductEvidenceSummary summary)
    {
        return summary.Items.Any(item =>
            string.Equals(item.ProfileId, "demo-fixture", StringComparison.OrdinalIgnoreCase) ||
            item.ArtifactPath.StartsWith("samples/product-evidence/", StringComparison.OrdinalIgnoreCase) ||
            item.ArtifactPath.StartsWith("samples\\product-evidence\\", StringComparison.OrdinalIgnoreCase));
    }

    private static void AppendDemoIntro(StringBuilder sb)
    {
        sb.AppendLine("## What this demo shows");
        sb.AppendLine();
        sb.AppendLine("- A deterministic product evidence report generated from versioned sample JSON under `samples/`.");
        sb.AppendLine("- Product evidence normalization, summary aggregation, scoring, and Markdown export.");
        sb.AppendLine("- A complete demo fixture beside partial real-retail evidence where visible price is missing.");
        sb.AppendLine("- Demo fixture data is versioned under `samples/`.");
        sb.AppendLine("- Runtime outputs are written under `artifacts/` and are not committed.");
        sb.AppendLine("- No live web access is required for this demo.");
        sb.AppendLine();

        sb.AppendLine("## Important safety guarantees");
        sb.AppendLine();
        sb.AppendLine("- No browser or web navigation is required by the demo report recipe.");
        sb.AppendLine("- No clicks, login, cart, checkout, payment, cookies, or WhatsApp actions are executed.");
        sb.AppendLine("- Raw signals are preserved as evidence context, not promoted to normalized visible fields.");
        sb.AppendLine();
    }

    private static void AppendDecisionReadiness(StringBuilder sb)
    {
        sb.AppendLine("## Decision readiness");
        sb.AppendLine();
        sb.AppendLine("- `ready_for_comparison` means captured evidence is sufficient for demo comparison.");
        sb.AppendLine("- `needs_price_verification` means the product is identified, but visible price evidence is missing.");
        sb.AppendLine("- Missing price is an evidence completeness issue, not a runtime failure.");
        sb.AppendLine();
    }

    private static void AppendMetric(StringBuilder sb, string metric, int value)
    {
        sb.AppendLine($"| {Cell(metric)} | {value} |");
    }

    private static void AppendMetric(StringBuilder sb, string metric, double value)
    {
        sb.AppendLine($"| {Cell(metric)} | {value:0.##} |");
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Dash : value.Trim();
    }

    private static string Cell(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
