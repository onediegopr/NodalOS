using System.Globalization;
using System.Text;
using System.Text.Json;
using OneBrain.Core.AppProfiles;

namespace OneBrain.Core.Safety;

/// <summary>Pure evaluator: determines if a click on targetText would be safe. No side effects.</summary>
public static class ClickPreflightEvaluator
{
    public static ClickPreflightResult Evaluate(string targetText, string? contextJson = null)
    {
        return Evaluate(targetText, ClickPreflightPolicy.Default, contextJson);
    }

    public static ClickPreflightResult Evaluate(string targetText, ClickPreflightPolicy policy, string? contextJson = null)
    {
        var effectivePolicy = policy ?? ClickPreflightPolicy.Default;
        var lower = Normalize(targetText);
        if (string.IsNullOrWhiteSpace(lower))
            return BuildResult(targetText, effectivePolicy, "unknown", "requiresReview", "empty target text", contextJson);

        string riskCategory;
        string riskLevel;

        if (ContainsAny(lower, effectivePolicy.BlockedTerms))
        {
            riskCategory = ContainsAny(lower, effectivePolicy.PaymentTerms)
                ? "payment-related"
                : "dangerous-commercial";

            if (ContainsAny(lower, effectivePolicy.AuthTerms))
                riskCategory = "auth-related";

            if (ContainsAny(lower, effectivePolicy.CookieTerms))
                riskCategory = "cookie-related";

            riskLevel = riskCategory switch
            {
                "payment-related" => "critical",
                "dangerous-commercial" => "high",
                "auth-related" => "high",
                "cookie-related" => "high",
                _ => "high"
            };

            return BuildResult(
                targetText,
                effectivePolicy,
                riskCategory,
                "blocked",
                $"Target '{targetText}' is a {riskCategory} action. Blocked by policy.",
                contextJson,
                riskLevel);
        }

        if (ContainsAny(lower, effectivePolicy.RequiresApprovalTerms))
        {
            return BuildResult(
                targetText,
                effectivePolicy,
                "navigation-candidate",
                "requiresApproval",
                $"Target '{targetText}' is a navigation action. Requires explicit approval.",
                contextJson,
                "medium");
        }

        if (ContainsAny(lower, effectivePolicy.ReadOnlyAllowedTerms))
        {
            return BuildResult(
                targetText,
                effectivePolicy,
                "safe-readonly",
                "allowedForFuture",
                $"Target '{targetText}' is safe-readonly. Allowed for future navigation, NOT executable in this hito.",
                contextJson,
                "low");
        }

        return BuildResult(
            targetText,
            effectivePolicy,
            "unknown",
            "requiresReview",
            $"Target '{targetText}' is unknown. Human review required.",
            contextJson,
            "unknown");
    }

    private static ClickPreflightResult BuildResult(
        string text,
        ClickPreflightPolicy policy,
        string category,
        string decision,
        string reason,
        string? contextJson,
        string riskLevel = "unknown")
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
                        if (ContainsAny(Normalize(item.Text), policy.BlockedTerms))
                            nearby.Add(item.Text);
                    }
                }
            }
            catch
            {
                // best-effort
            }
        }

        var evidence = new
        {
            targetText = text,
            riskCategory = category,
            category,
            decision,
            riskLevel,
            reason,
            blocked = decision == "blocked",
            allowed = decision == "allowedForFuture",
            requiresApproval = decision == "requiresApproval",
            requiresReview = decision == "requiresReview",
            nearbyDangerousSignals = nearby.Distinct().ToList(),
            nearbyDangerousSignalsJson = JsonSerializer.Serialize(nearby.Distinct().ToList()),
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
            Summary = $"{text} -> {decision} ({category}, {riskLevel})"
        };
    }

    private static bool ContainsAny(string normalizedText, IReadOnlyList<string> terms)
    {
        return terms.Any(term => normalizedText.Contains(Normalize(term), StringComparison.Ordinal));
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed record ClickPreflightPolicy(
    IReadOnlyList<string> BlockedTerms,
    IReadOnlyList<string> PaymentTerms,
    IReadOnlyList<string> AuthTerms,
    IReadOnlyList<string> CookieTerms,
    IReadOnlyList<string> RequiresApprovalTerms,
    IReadOnlyList<string> ReadOnlyAllowedTerms)
{
    public static ClickPreflightPolicy Default { get; } = new(
        BlockedTerms:
        [
            "comprar", "purchase", "buy now", "carrito", "cart", "checkout",
            "pagar", "pay", "payment", "tarjeta", "card", "cuotas", "credito", "debito", "transferencia",
            "enviar", "send", "submit", "publicar", "publish", "borrar", "eliminar", "delete",
            "login", "iniciar sesion", "sesion", "registrarse", "crear cuenta", "contrasena", "password",
            "aceptar cookies", "accept cookies", "cookies", "cookie", "aceptar terminos", "accept terms",
            "facturacion", "financiacion", "reservar", "contratar"
        ],
        PaymentTerms:
        [
            "pagar", "pay", "payment", "tarjeta", "card", "cuotas", "credito", "debito",
            "transferencia", "facturacion", "financiacion", "checkout"
        ],
        AuthTerms:
        [
            "login", "iniciar sesion", "sesion", "registrarse", "crear cuenta", "contrasena", "password"
        ],
        CookieTerms:
        [
            "aceptar cookies", "accept cookies", "cookies"
        ],
        RequiresApprovalTerms:
        [
            "ver mas", "ver publicacion", "ver producto", "descripcion", "detalles",
            "caracteristicas", "opiniones", "preguntas", "siguiente", "anterior",
            "comentarios", "archivo", "editar", "ver", "formato", "ayuda",
            "abrir informacion", "more information"
        ],
        ReadOnlyAllowedTerms:
        [
            "categorias", "ofertas", "cupones", "terminos", "privacidad", "breadcrumb"
        ]);

    public static ClickPreflightPolicy ForAppProfile(AppProfile profile)
    {
        var capabilities = profile.SupportedCapabilities.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var aliases = profile.SelectorAliases
            .Select(alias => alias.Alias)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .ToList();

        var externalFragile = profile.Status == AppProfileStatuses.ExternalFragile ||
                              capabilities.Contains(AppProfileCapabilities.ExternalFragile);
        var canExtendReadOnly = profile.RiskPolicy.ReadOnlyByDefault &&
                                profile.RiskPolicy.BlocksLogin &&
                                profile.RiskPolicy.BlocksCookies &&
                                profile.RiskPolicy.BlocksPayment &&
                                profile.RiskPolicy.BlocksPurchase &&
                                (!externalFragile ||
                                 profile.RiskPolicy.DiagnosticAllowed ||
                                 capabilities.Contains(AppProfileCapabilities.DiagnosticAllowed));

        return canExtendReadOnly && aliases.Count > 0
            ? Default with { ReadOnlyAllowedTerms = Default.ReadOnlyAllowedTerms.Concat(aliases).Distinct(StringComparer.OrdinalIgnoreCase).ToList() }
            : Default;
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
