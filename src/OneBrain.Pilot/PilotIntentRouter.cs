using System.Globalization;
using System.Text;

namespace OneBrain.Pilot;

public sealed class PilotIntentRouter
{
    private static readonly string[] DangerousTerms =
    [
        "click",
        "clic",
        "login",
        "cookie",
        "cookies",
        "carrito",
        "compra",
        "comprar",
        "pago",
        "checkout",
        "whatsapp",
        "submit",
        "invoke",
        "type",
        "comando",
        "powershell",
        "cmd.exe"
    ];

    public PilotIntentResult Route(string? text)
    {
        var original = text?.Trim() ?? "";
        var normalized = Normalize(original);

        if (string.IsNullOrWhiteSpace(normalized))
            return Reject(original, "no_match");

        if (DangerousTerms.Any(term => normalized.Contains(term, StringComparison.OrdinalIgnoreCase)))
            return Reject(original, "safety_rejected");

        if (ContainsAny(normalized, "html", "pagina"))
        {
            var recipe = ContainsAny(normalized, "demo", "muestra", "sample")
                ? PilotRecipeCatalog.FindById("demo_html")
                : PilotRecipeCatalog.FindById("html_report");

            return Match(original, recipe!, "matched_html_report");
        }

        if (ContainsAny(normalized, "markdown", "md"))
        {
            var recipe = ContainsAny(normalized, "demo", "muestra", "sample")
                ? PilotRecipeCatalog.FindById("demo_markdown")
                : PilotRecipeCatalog.FindById("markdown_report");

            return Match(original, recipe!, "matched_markdown_report");
        }

        if (ContainsAny(normalized, "demo", "mostrame", "mostrar", "compara", "comparar", "productos", "reporte"))
            return Match(original, PilotRecipeCatalog.FindById("demo_markdown")!, "matched_demo");

        return Reject(original, "no_match");
    }

    private static PilotIntentResult Match(string original, PilotRecipeDefinition recipe, string reason)
    {
        return new PilotIntentResult(PilotIntentStatus.Matched, original, recipe, reason);
    }

    private static PilotIntentResult Reject(string original, string reason)
    {
        return new PilotIntentResult(PilotIntentStatus.Rejected, original, null, reason);
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
    {
        var formD = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);

        foreach (var ch in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
