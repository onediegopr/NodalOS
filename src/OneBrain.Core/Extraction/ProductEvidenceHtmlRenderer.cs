using System.Globalization;
using System.Net;
using System.Text;

namespace OneBrain.Core.Extraction;

public static class ProductEvidenceHtmlRenderer
{
    private const string Dash = "\u2014";

    public static string Render(ProductEvidenceSummary summary)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("  <title>ONE BRAIN - Product Evidence Report</title>");
        AppendStyle(sb);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <main class=\"report-shell\">");
        sb.AppendLine("    <header class=\"hero\">");
        sb.AppendLine($"      <p class=\"eyebrow\">Generated: {Html(ValueOrDash(summary.CreatedAtUtc))}</p>");
        sb.AppendLine("      <h1>ONE BRAIN \u2014 Product Evidence Report</h1>");
        sb.AppendLine("      <p class=\"subtitle\">Local, auditable product evidence summary with explicit missing fields, scoring, and readiness.</p>");
        sb.AppendLine("    </header>");

        AppendReportIntro(sb);
        AppendSummary(sb, summary);
        AppendDecisionReadiness(sb);
        AppendProducts(sb, summary);
        AppendNotes(sb, summary);
        AppendInvalidArtifacts(sb, summary);

        sb.AppendLine("  </main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void AppendStyle(StringBuilder sb)
    {
        sb.AppendLine("  <style>");
        sb.AppendLine("    :root { --ink:#18201b; --muted:#657066; --paper:#fbf7ef; --card:#ffffff; --line:#dfd4c2; --accent:#2f6b4f; --accent-2:#c17f32; --soft:#eef4ec; }");
        sb.AppendLine("    * { box-sizing: border-box; }");
        sb.AppendLine("    body { margin: 0; color: var(--ink); background: linear-gradient(135deg, #f7efe2 0%, #edf4ee 55%, #fdfbf5 100%); font-family: Georgia, 'Times New Roman', serif; }");
        sb.AppendLine("    .report-shell { max-width: 1180px; margin: 0 auto; padding: 40px 24px 56px; }");
        sb.AppendLine("    .hero { border: 1px solid var(--line); background: rgba(255,255,255,.78); border-radius: 24px; padding: 30px; box-shadow: 0 18px 48px rgba(42,47,39,.12); }");
        sb.AppendLine("    .eyebrow { margin: 0 0 10px; color: var(--accent); text-transform: uppercase; letter-spacing: .12em; font: 700 12px Verdana, sans-serif; }");
        sb.AppendLine("    h1 { margin: 0; font-size: clamp(34px, 6vw, 64px); line-height: .98; letter-spacing: -.04em; }");
        sb.AppendLine("    h2 { margin: 34px 0 14px; font-size: 24px; letter-spacing: -.02em; }");
        sb.AppendLine("    .subtitle { max-width: 760px; margin: 18px 0 0; color: var(--muted); font-size: 18px; }");
        sb.AppendLine("    .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: 14px; }");
        sb.AppendLine("    .card { background: var(--card); border: 1px solid var(--line); border-radius: 18px; padding: 16px; }");
        sb.AppendLine("    .metric { color: var(--muted); font: 700 12px Verdana, sans-serif; text-transform: uppercase; letter-spacing: .08em; }");
        sb.AppendLine("    .value { margin-top: 8px; font: 700 28px Verdana, sans-serif; color: var(--accent); }");
        sb.AppendLine("    .intro-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 14px; margin-top: 22px; }");
        sb.AppendLine("    .intro-card { background: rgba(255,255,255,.72); border: 1px solid var(--line); border-radius: 18px; padding: 18px 20px; }");
        sb.AppendLine("    .intro-card h2 { margin-top: 0; }");
        sb.AppendLine("    .table-wrap { overflow-x: auto; border: 1px solid var(--line); border-radius: 18px; background: var(--card); }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-family: Verdana, sans-serif; font-size: 13px; }");
        sb.AppendLine("    th, td { padding: 12px 14px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }");
        sb.AppendLine("    th { background: var(--soft); color: #25352b; font-size: 11px; text-transform: uppercase; letter-spacing: .08em; }");
        sb.AppendLine("    tr:last-child td { border-bottom: 0; }");
        sb.AppendLine("    .number { text-align: right; }");
        sb.AppendLine("    .badge { display: inline-block; border-radius: 999px; padding: 4px 9px; font-weight: 700; border: 1px solid transparent; }");
        sb.AppendLine("    .badge-excellent, .badge-ready-for-comparison { background: #e6f4ea; color: #1f6b43; border-color: #b9dfc5; }");
        sb.AppendLine("    .badge-good { background: #edf5ff; color: #285b91; border-color: #bdd7f5; }");
        sb.AppendLine("    .badge-partial, .badge-needs-price-verification, .badge-missing-price { background: #fff1dc; color: #86520e; border-color: #e8c58f; }");
        sb.AppendLine("    .badge-weak, .badge-insufficient, .badge-diagnostic, .badge-diagnostic-only { background: #f6e7e6; color: #8a352d; border-color: #e2b7b2; }");
        sb.AppendLine("    .notes, .invalid { background: rgba(255,255,255,.72); border: 1px solid var(--line); border-radius: 18px; padding: 18px 22px; }");
        sb.AppendLine("    li { margin: 7px 0; }");
        sb.AppendLine("  </style>");
    }

    private static void AppendReportIntro(StringBuilder sb)
    {
        sb.AppendLine("    <section class=\"intro-grid\" aria-label=\"report context\">");
        AppendIntroCard(sb, "What this report shows",
            "Normalized product evidence, summary metrics, explicit missing fields, score, grade, and decision readiness.");
        AppendIntroCard(sb, "Safety guarantees",
            "No live web access is required for the stable demo. This report does not execute clicks, login, cookies, cart, purchase, or payment actions.");
        AppendIntroCard(sb, "Decision readiness",
            "Missing price means the visible evidence did not confirm price. Ready items can be compared; partial items need verification.");
        sb.AppendLine("    </section>");
    }

    private static void AppendIntroCard(StringBuilder sb, string title, string text)
    {
        sb.AppendLine("      <article class=\"intro-card\">");
        sb.AppendLine($"        <h2>{Html(title)}</h2>");
        sb.AppendLine($"        <p>{Html(text)}</p>");
        sb.AppendLine("      </article>");
    }

    private static void AppendSummary(StringBuilder sb, ProductEvidenceSummary summary)
    {
        sb.AppendLine("    <section aria-labelledby=\"summary-heading\">");
        sb.AppendLine("      <h2 id=\"summary-heading\">Summary</h2>");
        sb.AppendLine("      <div class=\"cards\">");
        AppendCard(sb, "Source artifacts", summary.SourceArtifactCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(sb, "Valid artifacts", summary.ValidArtifactCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(sb, "Invalid artifacts", summary.InvalidArtifactCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(sb, "Products with price", summary.Totals.ProductsWithPrice.ToString(CultureInfo.InvariantCulture));
        AppendCard(sb, "Products needing price verification", summary.Totals.ProductsMissingPrice.ToString(CultureInfo.InvariantCulture));
        AppendCard(sb, "Average evidence score", summary.Totals.AverageEvidenceScore.ToString("0.##", CultureInfo.InvariantCulture));
        AppendCard(sb, "Ready for comparison", summary.Totals.ReadyForComparisonCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(sb, "Safety clicks total", summary.Totals.SafetyClicksTotal.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("      </div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendCard(StringBuilder sb, string metric, string value)
    {
        sb.AppendLine("        <article class=\"card\">");
        sb.AppendLine($"          <div class=\"metric\">{Html(metric)}</div>");
        sb.AppendLine($"          <div class=\"value\">{Html(value)}</div>");
        sb.AppendLine("        </article>");
    }

    private static void AppendProducts(StringBuilder sb, ProductEvidenceSummary summary)
    {
        sb.AppendLine("    <section aria-labelledby=\"products-heading\">");
        sb.AppendLine("      <h2 id=\"products-heading\">Products</h2>");
        sb.AppendLine("      <div class=\"table-wrap\">");
        sb.AppendLine("        <table>");
        sb.AppendLine("          <thead>");
        sb.AppendLine("            <tr><th>Product</th><th>Source</th><th class=\"number\">Price</th><th>Currency</th><th>Status</th><th>Confidence</th><th class=\"number\">Score</th><th>Grade</th><th>Readiness</th><th>Missing fields</th></tr>");
        sb.AppendLine("          </thead>");
        sb.AppendLine("          <tbody>");

        if (summary.Items.Count == 0)
        {
            sb.AppendLine($"            <tr><td>diagnostic: no products</td><td>{Dash}</td><td class=\"number\">{Dash}</td><td>{Dash}</td><td>diagnostic</td><td>diagnostic</td><td class=\"number\">0</td><td>insufficient</td><td>diagnostic_only</td><td>{Dash}</td></tr>");
        }
        else
        {
            foreach (var item in summary.Items)
            {
                sb.Append("            <tr><td>");
                sb.Append(Html(ValueOrDash(item.ProductName)));
                sb.Append("</td><td>");
                sb.Append(Html(ValueOrDash(item.ProfileId)));
                sb.Append("</td><td class=\"number\">");
                sb.Append(Html(item.HasPrice ? ValueOrDash(item.Price) : Dash));
                sb.Append("</td><td>");
                sb.Append(Html(item.HasCurrency ? ValueOrDash(item.Currency) : Dash));
                sb.Append("</td><td>");
                sb.Append(Html(ValueOrDash(item.ExtractionStatus)));
                sb.Append("</td><td>");
                sb.Append(Html(ValueOrDash(item.ExtractionConfidence)));
                sb.Append("</td><td class=\"number\">");
                sb.Append(item.EvidenceScore.ToString(CultureInfo.InvariantCulture));
                sb.Append("</td><td>");
                AppendBadge(sb, item.EvidenceGrade);
                sb.Append("</td><td>");
                AppendBadge(sb, item.DecisionReadiness);
                sb.Append("</td><td>");
                sb.Append(Html(item.BlockedOrMissingFields.Count == 0 ? Dash : string.Join(", ", item.BlockedOrMissingFields)));
                sb.AppendLine("</td></tr>");
            }
        }

        sb.AppendLine("          </tbody>");
        sb.AppendLine("        </table>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendDecisionReadiness(StringBuilder sb)
    {
        sb.AppendLine("    <section aria-labelledby=\"readiness-heading\">");
        sb.AppendLine("      <h2 id=\"readiness-heading\">Decision readiness</h2>");
        sb.AppendLine("      <div class=\"notes\"><ul>");
        sb.AppendLine("        <li><span class=\"badge badge-ready-for-comparison\">ready_for_comparison</span> means captured evidence is enough for demo comparison.</li>");
        sb.AppendLine("        <li><span class=\"badge badge-needs-price-verification\">needs_price_verification</span> means product evidence is useful but price needs human verification.</li>");
        sb.AppendLine("        <li><span class=\"badge badge-partial\">partial</span> evidence is intentionally explicit; missing fields are not invented.</li>");
        sb.AppendLine("      </ul></div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendBadge(StringBuilder sb, string? value)
    {
        var label = ValueOrDash(value);
        var cssToken = label.Replace('_', '-').Replace(' ', '-').ToLowerInvariant();
        sb.Append("<span class=\"badge badge-");
        sb.Append(Html(cssToken));
        sb.Append("\">");
        sb.Append(Html(label));
        sb.Append("</span>");
    }

    private static void AppendNotes(StringBuilder sb, ProductEvidenceSummary summary)
    {
        sb.AppendLine("    <section aria-labelledby=\"notes-heading\">");
        sb.AppendLine("      <h2 id=\"notes-heading\">Notes</h2>");
        sb.AppendLine("      <div class=\"notes\"><ul>");
        sb.AppendLine("        <li>Raw signals are not visible normalized price.</li>");
        sb.AppendLine("        <li>Missing price means needs verification.</li>");
        sb.AppendLine("        <li>Demo/runtime outputs are local artifacts.</li>");
        foreach (var note in summary.Notes)
            sb.AppendLine($"        <li>{Html(ValueOrDash(note))}</li>");
        sb.AppendLine("      </ul></div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendInvalidArtifacts(StringBuilder sb, ProductEvidenceSummary summary)
    {
        sb.AppendLine("    <section aria-labelledby=\"invalid-heading\">");
        sb.AppendLine("      <h2 id=\"invalid-heading\">Invalid artifacts</h2>");
        sb.AppendLine("      <div class=\"invalid\"><ul>");
        if (summary.InvalidArtifacts.Count == 0)
        {
            sb.AppendLine("        <li>None.</li>");
        }
        else
        {
            foreach (var invalid in summary.InvalidArtifacts)
                sb.AppendLine($"        <li>{Html(ValueOrDash(invalid))}</li>");
        }

        sb.AppendLine("      </ul></div>");
        sb.AppendLine("    </section>");
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Dash : value.Trim();
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
