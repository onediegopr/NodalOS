using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Extraction;

public static class ProductEvidenceExtractor
{
    public static ProductEvidence Extract(ProductEvidenceInput input)
    {
        var title = Clean(input.PageTitle);
        var visibleText = Clean(input.VisibleText);
        var combinedVisible = JoinNonEmpty(" | ", title, visibleText);
        var normalized = Normalize(combinedVisible);
        var rawSignals = SplitSignals(input.RawSignals);

        var productName = ExtractProductName(title);
        var category = FirstNonEmpty(Clean(input.CategoryHint), ExtractCategory(normalized));
        var (price, currency) = ExtractPriceAndCurrency(combinedVisible);
        var sku = ExtractSku(combinedVisible);
        var stock = ExtractStock(normalized);
        var availability = ExtractAvailability(normalized);

        var contactSignals = DetectSignals(normalized, ("contacto", "contact"), ("servicio al cliente", "customer-service"), ("telefono", "phone"), ("venta telefonica", "phone-sales"));
        var whatsappSignals = DetectSignals(normalized, ("whatsapp", "whatsapp"));
        var cartSignals = DetectSignals(normalized, ("carrito", "cart"), ("agregar al carrito", "add-to-cart"));
        var buySignals = DetectSignals(normalized, ("comprar", "buy"), ("comprar ahora", "buy-now"));
        var paymentSignals = DetectSignals(normalized, ("pagar", "pay"), ("pago", "payment"), ("tarjeta", "card"), ("credito", "credit"), ("debito", "debit"), ("cuotas", "installments"));
        var loginSignals = DetectSignals(normalized, ("iniciar sesion", "login"), ("mi cuenta", "account"), ("registrarse", "register"), ("contrasena", "password"));
        var cookieSignals = DetectSignals(normalized, ("cookie", "cookie"), ("cookies", "cookies"));
        var geolocSignals = DetectSignals(normalized, ("ubicacion", "location"), ("geolocalizacion", "geolocation"), ("elige tu tienda", "store-location"));
        var popupSignals = DetectSignals(normalized, ("popup", "popup"), ("modal", "modal"), ("ventana emergente", "popup"));

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(productName)) missing.Add("missing_product_name");
        if (string.IsNullOrWhiteSpace(price)) missing.Add("missing_price");
        if (string.IsNullOrWhiteSpace(currency)) missing.Add("missing_currency");
        if (string.IsNullOrWhiteSpace(stock)) missing.Add("missing_stock");

        var notes = new List<string>();
        if (rawSignals.Count > 0)
            notes.Add("raw_signals_recorded_without_interaction");
        if (string.IsNullOrWhiteSpace(price))
            notes.Add("price_not_visible_or_not_proven");
        if (string.IsNullOrWhiteSpace(stock))
            notes.Add("stock_not_visible_or_not_proven");

        var hasBlockingDiagnostic = cookieSignals.Count > 0 || geolocSignals.Count > 0 || popupSignals.Count > 0;
        var status = ResolveStatus(productName, price, stock, hasBlockingDiagnostic);
        var confidence = ResolveConfidence(productName, category, price, currency, sku, status);

        return new ProductEvidence
        {
            SourceUrl = Clean(input.SourceUrl),
            SourceProfileId = Clean(input.SourceProfileId),
            PageTitle = title,
            ProductName = productName,
            Brand = ExtractBrand(title, visibleText),
            Sku = sku,
            Category = category,
            Description = ExtractDescription(title, visibleText),
            Price = price,
            Currency = currency,
            Availability = availability,
            Stock = stock,
            Seller = ExtractSeller(title, visibleText),
            ContactSignals = contactSignals,
            WhatsappSignals = whatsappSignals,
            CartSignals = cartSignals,
            BuySignals = buySignals,
            PaymentSignals = paymentSignals,
            LoginSignals = loginSignals,
            CookieSignals = cookieSignals,
            GeolocSignals = geolocSignals,
            PopupSignals = popupSignals,
            EvidenceTextSample = Truncate(combinedVisible, 500),
            RawSignals = rawSignals,
            BlockedOrMissingFields = missing,
            ExtractionConfidence = confidence,
            ExtractionStatus = status,
            ExtractionNotes = notes
        };
    }

    private static string? ExtractProductName(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var cleaned = title
            .Replace(" - Personal: Microsoft\u200b Edge", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" - Microsoft Edge", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        var separators = new[] { " - ROCA", " - Suministros Roca", " - Sodimac", " | Sodimac", " - Personal" };
        foreach (var separator in separators)
        {
            var index = cleaned.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
                return cleaned[..index].Trim();
        }

        return cleaned;
    }

    private static (string? Price, string? Currency) ExtractPriceAndCurrency(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, null);

        var usdBefore = Regex.Match(text, @"(?i)\b(USD|U\$S)\s*([0-9]+(?:[.,][0-9]{1,2})?)\b");
        if (usdBefore.Success)
            return (NormalizeDecimal(usdBefore.Groups[2].Value), "USD");

        var usdAfter = Regex.Match(text, @"(?i)\b([0-9]+(?:[.,][0-9]{1,2})?)\s*(USD|U\$S)\b");
        if (usdAfter.Success)
            return (NormalizeDecimal(usdAfter.Groups[1].Value), "USD");

        var uyuBefore = Regex.Match(text, @"(?i)\b(UYU|UY\$)\s*([0-9]+(?:[.,][0-9]{1,2})?)\b");
        if (uyuBefore.Success)
            return (NormalizeDecimal(uyuBefore.Groups[2].Value), "UYU");

        return (null, null);
    }

    private static string? ExtractSku(string text)
    {
        var match = Regex.Match(text, @"(?i)\bsku\s*[=:]\s*([A-Za-z0-9._-]+)\b");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractStock(string normalized)
    {
        if (ContainsAny(normalized, "sin stock", "agotado")) return "out_of_stock";
        if (ContainsAny(normalized, "en stock", "stock disponible", "disponible")) return "available";
        return null;
    }

    private static string? ExtractAvailability(string normalized)
    {
        if (ContainsAny(normalized, "sin stock", "agotado")) return "unavailable";
        if (ContainsAny(normalized, "en stock", "stock disponible", "disponible")) return "available";
        return null;
    }

    private static string? ExtractCategory(string normalized)
    {
        if (ContainsAny(normalized, "pisos", "revestimientos")) return "pisos/revestimientos";
        if (ContainsAny(normalized, "placa", "marmol")) return "placa/revestimiento";
        return null;
    }

    private static string? ExtractBrand(string title, string text)
    {
        var combined = Normalize(JoinNonEmpty(" | ", title, text));
        if (combined.Contains("sodimac")) return "Sodimac";
        if (combined.Contains("roca")) return "ROCA";
        return null;
    }

    private static string? ExtractSeller(string title, string text)
    {
        var combined = Normalize(JoinNonEmpty(" | ", title, text));
        if (combined.Contains("sodimac")) return "Sodimac";
        if (combined.Contains("suministros roca") || combined.Contains("roca")) return "Suministros Roca";
        return null;
    }

    private static string? ExtractDescription(string title, string text)
    {
        return Truncate(JoinNonEmpty(" | ", title, text), 300);
    }

    private static string ResolveStatus(string? productName, string? price, string? stock, bool hasBlockingDiagnostic)
    {
        if (hasBlockingDiagnostic && string.IsNullOrWhiteSpace(productName)) return "diagnostic";
        if (string.IsNullOrWhiteSpace(productName)) return "diagnostic";
        if (string.IsNullOrWhiteSpace(price)) return "missing_price";
        if (string.IsNullOrWhiteSpace(stock)) return "missing_stock";
        return "complete";
    }

    private static string ResolveConfidence(string? productName, string? category, string? price, string? currency, string? sku, string status)
    {
        if (status == "diagnostic") return "diagnostic";
        if (!string.IsNullOrWhiteSpace(productName) && ((!string.IsNullOrWhiteSpace(price) && !string.IsNullOrWhiteSpace(currency)) || !string.IsNullOrWhiteSpace(sku)))
            return "high";
        if (!string.IsNullOrWhiteSpace(productName) || !string.IsNullOrWhiteSpace(category))
            return "medium";
        return "low";
    }

    private static List<string> DetectSignals(string normalized, params (string Pattern, string Signal)[] patterns)
    {
        return patterns
            .Where(p => normalized.Contains(Normalize(p.Pattern), StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Signal)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsAny(string normalized, params string[] values)
    {
        return values.Any(value => normalized.Contains(Normalize(value), StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> SplitSignals(string? rawSignals)
    {
        if (string.IsNullOrWhiteSpace(rawSignals)) return [];
        return rawSignals
            .Split(['|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeDecimal(string value) => value.Replace(',', '.').Trim();

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string JoinNonEmpty(string separator, params string?[] values)
    {
        return string.Join(separator, values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Length <= max ? value : value[..max] + "...";
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        var formD = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
