using System.Text.Json;

namespace OneBrain.Core.Extraction;

/// <summary>Pure discovery: input text -> classified actionable elements. No side effects.</summary>
public static class CommercialActionDiscovery
{
    private static readonly Dictionary<string, string> PaymentPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pagar"] = "payment", ["tarjeta"] = "payment", ["tarjeta de crédito"] = "payment",
        ["tarjeta de débito"] = "payment", ["cuotas"] = "payment", ["crédito"] = "payment",
        ["débito"] = "payment", ["mercado pago"] = "payment", ["medio de pago"] = "payment",
        ["facturación"] = "payment", ["financiación"] = "payment", ["transferencia"] = "payment"
    };

    private static readonly Dictionary<string, string> DangerousPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["comprar"] = "dangerous", ["comprar ahora"] = "dangerous", ["agregar al carrito"] = "dangerous",
        ["carrito"] = "dangerous", ["confirmar compra"] = "dangerous", ["finalizar compra"] = "dangerous",
        ["ofertar"] = "dangerous", ["contratar"] = "dangerous", ["reservar"] = "dangerous",
        ["checkout"] = "dangerous"
    };

    private static readonly Dictionary<string, string> AuthPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["iniciar sesión"] = "auth", ["ingresar"] = "auth", ["registrarse"] = "auth",
        ["crear cuenta"] = "auth", ["mi cuenta"] = "auth", ["usuario"] = "auth",
        ["contraseña"] = "auth", ["email"] = "auth"
    };

    private static readonly Dictionary<string, string> NavPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ver más"] = "nav", ["detalles"] = "nav", ["características"] = "nav",
        ["descripción"] = "nav", ["opiniones"] = "nav", ["preguntas"] = "nav",
        ["siguiente"] = "nav", ["anterior"] = "nav", ["ver publicación"] = "nav",
        ["ver producto"] = "nav", ["comentarios"] = "nav"
    };

    private static readonly Dictionary<string, string> SafePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["categorías"] = "safe", ["ofertas"] = "safe", ["cupones"] = "safe",
        ["ayuda"] = "safe", ["términos"] = "safe", ["privacidad"] = "safe",
        ["supermercado"] = "safe", ["moda"] = "safe", ["breadcrumb"] = "safe"
    };

    private static readonly string[] Severity = ["payment", "dangerous", "auth", "nav", "safe"];

    public static ActionDiscoveryResult Discover(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ActionDiscoveryResult { Items = new List<ActionItem>(), Summary = "empty" };

        var items = new List<ActionItem>();
        var lower = text.ToLowerInvariant();
        var seen = new HashSet<string>();

        void AddIfNew(string pattern, string category, string severity)
        {
            if (!seen.Contains(pattern) && lower.Contains(pattern))
            {
                seen.Add(pattern);
                items.Add(new ActionItem { Text = pattern, Category = category, Severity = severity, Match = pattern });
            }
        }

        foreach (var (pattern, category) in PaymentPatterns) AddIfNew(pattern, category, "payment");
        foreach (var (pattern, category) in DangerousPatterns) AddIfNew(pattern, category, "dangerous");
        foreach (var (pattern, category) in AuthPatterns) AddIfNew(pattern, category, "auth");
        foreach (var (pattern, category) in NavPatterns) AddIfNew(pattern, category, "nav");
        foreach (var (pattern, category) in SafePatterns) AddIfNew(pattern, category, "safe");

        var uniqueItems = items
            .GroupBy(i => i.Match, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var count = uniqueItems.Count;
        var safeCount = uniqueItems.Count(i => i.Category == "safe");
        var navCount = uniqueItems.Count(i => i.Category == "nav");
        var dangerousCount = uniqueItems.Count(i => i.Category == "dangerous");
        var authCount = uniqueItems.Count(i => i.Category == "auth");
        var paymentCount = uniqueItems.Count(i => i.Category == "payment");
        var unknown = Math.Max(0, count - safeCount - navCount - dangerousCount - authCount - paymentCount);

        var highest = uniqueItems.Count > 0
            ? uniqueItems.OrderBy(i => Array.IndexOf(Severity, i.Severity)).First().Severity
            : "unknown";

        var summary = count == 0 ? "no actions found"
            : $"{count} items: {safeCount}s/{navCount}n/{dangerousCount}d/{authCount}a/{paymentCount}p";

        return new ActionDiscoveryResult
        {
            Items = uniqueItems,
            Count = count,
            SafeCount = safeCount,
            NavCount = navCount,
            DangerousCount = dangerousCount,
            AuthCount = authCount,
            PaymentCount = paymentCount,
            UnknownCount = unknown,
            HighestRisk = highest,
            HasDangerous = dangerousCount > 0,
            HasAuth = authCount > 0,
            HasPayment = paymentCount > 0,
            Summary = summary,
            RawEvidence = text.Length > 500 ? text[..500] + "..." : text
        };
    }

    public static string SerializeItems(List<ActionItem> items) => JsonSerializer.Serialize(items);
    public static List<ActionItem>? DeserializeItems(string json)
    {
        try { return JsonSerializer.Deserialize<List<ActionItem>>(json); }
        catch { return null; }
    }
}

public sealed class ActionDiscoveryResult
{
    public List<ActionItem> Items { get; init; } = new();
    public int Count { get; init; }
    public int SafeCount { get; init; }
    public int NavCount { get; init; }
    public int DangerousCount { get; init; }
    public int AuthCount { get; init; }
    public int PaymentCount { get; init; }
    public int UnknownCount { get; init; }
    public string HighestRisk { get; init; } = "unknown";
    public bool HasDangerous { get; init; }
    public bool HasAuth { get; init; }
    public bool HasPayment { get; init; }
    public string Summary { get; init; } = "";
    public string? RawEvidence { get; init; }
}

public sealed class ActionItem
{
    public string Text { get; init; } = "";
    public string Category { get; init; } = "unknown";
    public string Severity { get; init; } = "unknown";
    public string Match { get; init; } = "";
}
