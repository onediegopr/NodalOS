using System.Text.Json;

namespace OneBrain.Core.Safety;

/// <summary>Pure evaluator: determines if a click on targetText would be safe. No side effects.</summary>
public static class ClickPreflightEvaluator
{
    private static readonly string[] AlwaysBlockedPrefixes = [
        "comprar", "pagar", "carrito", "checkout", "tarjeta",
        "cuotas", "crédito", "débito", "mercado pago", "transferencia",
        "iniciar sesión", "registrarse", "crear cuenta", "contraseña",
        "facturación", "financiación", "reservar", "contratar"
    ];

    private static readonly string[] RequiresApprovalPrefixes = [
        "ver más", "ver publicación", "ver producto", "descripción",
        "detalles", "características", "opiniones", "preguntas",
        "siguiente", "anterior", "comentarios"
    ];

    private static readonly string[] SafeReadonlyPrefixes = [
        "categorías", "ofertas", "cupones", "ayuda", "términos",
        "privacidad", "supermercado", "moda", "breadcrumb"
    ];

    public static ClickPreflightResult Evaluate(string targetText, string? contextJson = null)
    {
        var lower = targetText.ToLowerInvariant().Trim();
        if (string.IsNullOrWhiteSpace(lower))
            return BuildResult(targetText, "unknown", "requiresReview", "empty target text", contextJson);

        // Determine risk category and level
        string riskCategory;
        string riskLevel;

        if (AlwaysBlockedPrefixes.Any(p => lower.Contains(p)))
        {
            riskCategory = lower.Contains("pagar") || lower.Contains("tarjeta") || lower.Contains("cuotas") ||
                          lower.Contains("crédito") || lower.Contains("débito") || lower.Contains("mercado pago") ||
                          lower.Contains("transferencia") || lower.Contains("facturación") || lower.Contains("financiación")
                ? "payment-related" : "dangerous-commercial";

            if (lower.Contains("iniciar sesión") || lower.Contains("registrarse") || lower.Contains("crear cuenta") ||
                lower.Contains("contraseña"))
                riskCategory = "auth-related";

            riskLevel = riskCategory switch
            {
                "payment-related" => "critical",
                "dangerous-commercial" => "high",
                "auth-related" => "high",
                _ => "high"
            };

            return BuildResult(targetText, riskCategory, "blocked",
                $"Target '{targetText}' is a {riskCategory} action. Blocked by policy.",
                contextJson, riskLevel);
        }

        if (RequiresApprovalPrefixes.Any(p => lower.Contains(p)))
        {
            return BuildResult(targetText, "navigation-candidate", "requiresApproval",
                $"Target '{targetText}' is a navigation action. Requires explicit approval.",
                contextJson, "medium");
        }

        if (SafeReadonlyPrefixes.Any(p => lower.Contains(p)))
        {
            return BuildResult(targetText, "safe-readonly", "allowedForFuture",
                $"Target '{targetText}' is safe-readonly. Allowed for future navigation, NOT executable in this hito.",
                contextJson, "low");
        }

        return BuildResult(targetText, "unknown", "requiresReview",
            $"Target '{targetText}' is unknown. Human review required.",
            contextJson, "unknown");
    }

    private static ClickPreflightResult BuildResult(string text, string category, string decision,
        string reason, string? contextJson, string riskLevel = "unknown")
    {
        var nearby = new List<string>();
        if (!string.IsNullOrWhiteSpace(contextJson))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<Extraction.ActionItem>>(contextJson);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (AlwaysBlockedPrefixes.Any(p => item.Text.Contains(p, StringComparison.OrdinalIgnoreCase)))
                            nearby.Add(item.Text);
                    }
                }
            }
            catch { /* best-effort */ }
        }

        var evidence = new
        {
            targetText = text,
            category,
            decision,
            riskLevel,
            reason,
            blocked = decision == "blocked",
            allowed = decision == "allowedForFuture",
            requiresApproval = decision == "requiresApproval",
            requiresReview = decision == "requiresReview",
            nearbyDangerousSignals = nearby.Distinct().ToList(),
            executionAllowedInThisHito = false
        };

        return new ClickPreflightResult
        {
            TargetText = text,
            Decision = decision,
            RiskCategory = category,
            RiskLevel = riskLevel,
            Allowed = decision == "allowedForFuture",
            Blocked = decision == "blocked",
            RequiresApproval = decision == "requiresApproval",
            RequiresReview = decision == "requiresReview",
            Reason = reason,
            EvidenceJson = JsonSerializer.Serialize(evidence),
            NearbyDangerousSignalsJson = JsonSerializer.Serialize(nearby.Distinct().ToList()),
            Summary = $"{text} → {decision} ({category}, {riskLevel})"
        };
    }
}

public sealed class ClickPreflightResult
{
    public string TargetText { get; init; } = "";
    public string Decision { get; init; } = "unknown";
    public string RiskCategory { get; init; } = "unknown";
    public string RiskLevel { get; init; } = "unknown";
    public bool Allowed { get; init; }
    public bool Blocked { get; init; }
    public bool RequiresApproval { get; init; }
    public bool RequiresReview { get; init; }
    public string Reason { get; init; } = "";
    public string? EvidenceJson { get; init; }
    public string? NearbyDangerousSignalsJson { get; init; }
    public string Summary { get; init; } = "";
}
