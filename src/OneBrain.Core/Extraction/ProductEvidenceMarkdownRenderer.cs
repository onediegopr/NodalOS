using System.Text;

namespace OneBrain.Core.Extraction;

public static class ProductEvidenceMarkdownRenderer
{
    public static string Render(ProductEvidenceSummary summary)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# ONE BRAIN - Product Evidence Summary");
        sb.AppendLine();
        sb.AppendLine($"Generated: {ValueOrDash(summary.CreatedAtUtc)}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|---|---:|");
        AppendMetric(sb, "Source artifacts", summary.SourceArtifactCount);
        AppendMetric(sb, "Valid artifacts", summary.ValidArtifactCount);
        AppendMetric(sb, "Invalid artifacts", summary.InvalidArtifactCount);
        AppendMetric(sb, "Products with price", summary.Totals.ProductsWithPrice);
        AppendMetric(sb, "Products missing price", summary.Totals.ProductsMissingPrice);
        AppendMetric(sb, "Safety clicks total", summary.Totals.SafetyClicksTotal);
        AppendMetric(sb, "Safety payment signals total", summary.Totals.SafetyPaymentsSignalsTotal);
        sb.AppendLine();

        sb.AppendLine("## Products");
        sb.AppendLine();
        sb.AppendLine("| Product | Source | Price | Currency | Status | Confidence | Missing fields |");
        sb.AppendLine("|---|---|---:|---|---|---|---|");
        if (summary.Items.Count == 0)
        {
            sb.AppendLine("| diagnostic: no products | — | — | — | diagnostic | diagnostic | — |");
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
                sb.Append(Cell(item.HasPrice ? ValueOrDash(item.Price) : "—"));
                sb.Append(" | ");
                sb.Append(Cell(item.HasCurrency ? ValueOrDash(item.Currency) : "—"));
                sb.Append(" | ");
                sb.Append(Cell(ValueOrDash(item.ExtractionStatus)));
                sb.Append(" | ");
                sb.Append(Cell(ValueOrDash(item.ExtractionConfidence)));
                sb.Append(" | ");
                sb.Append(Cell(item.BlockedOrMissingFields.Count == 0 ? "—" : string.Join(", ", item.BlockedOrMissingFields)));
                sb.AppendLine(" |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Notes");
        sb.AppendLine();
        sb.AppendLine("- Raw signals are not treated as visible normalized price.");
        sb.AppendLine("- Missing price means the visible/UIA evidence did not confirm a price.");
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

    private static void AppendMetric(StringBuilder sb, string metric, int value)
    {
        sb.AppendLine($"| {Cell(metric)} | {value} |");
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    }

    private static string Cell(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
