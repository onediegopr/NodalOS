namespace OneBrain.Safety.Policies;

public sealed class MinimalSafetyGuard
{
    // Blocked when ANY of these appears as a substring of the element name.
    // "run" and "ejecutar" are intentionally NOT here; they are too generic.
    // Use DangerousRunPhrases below for run-specific dangerous contexts.
    private static readonly string[] DangerousSubstrings =
    [
        "cerrar",
        "cerrar pestaña",
        "close",
        "close tab",
        "minimizar",
        "minimize",
        "maximizar",
        "maximize",
        "eliminar",
        "borrar",
        "delete",
        "remove",
        "pagar",
        "pay",
        "checkout",
        "confirmar pago",
        "enviar",
        "send",
        "emitir",
        "install",
        "instalar",
        "destruir",
        "execute",    // "execute" alone is dangerous; "run" alone is not
    ];

    // Dangerous only when the name contains one of these specific phrases.
    // Prevents "Run ONE Brain Search" from being blocked while still catching
    // "Run as administrator", "Run script", etc.
    private static readonly string[] DangerousRunPhrases =
    [
        "run command",
        "run script",
        "run executable",
        "run as administrator",
        "run as admin",
        "ejecutar como",
        "ejecutar script",
        "ejecutar comando",
    ];

    private static readonly string[] DangerousAutomationIds =
    [
        "Close",
        "CloseButton",
        "Minimize-Restore",
        "Maximize-Restore"
    ];

    public SafetyDecision Evaluate(
        string actionKind,
        string role,
        string name,
        string automationId,
        string className)
    {
        if (!IsPotentiallyDangerousAction(actionKind))
            return new SafetyDecision(true, "Allowed.");

        if (ContainsAny(name, DangerousSubstrings) || ContainsAny(name, DangerousRunPhrases))
            return new SafetyDecision(false, $"Blocked by safety policy: dangerous target name '{name}'.");

        if (EqualsAny(automationId, DangerousAutomationIds))
            return new SafetyDecision(false, $"Blocked by safety policy: dangerous automation id '{automationId}'.");

        return new SafetyDecision(true, "Allowed.");
    }

    private static bool IsPotentiallyDangerousAction(string actionKind)
    {
        // Any kind that triggers a physical Invoke/Click/Press on a UI element
        // is potentially dangerous and must pass safety evaluation.
        var kind = actionKind.ToLowerInvariant();
        return kind is "invoke" or "click" or "press";
    }

    private static bool ContainsAny(string value, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (var term in terms)
        {
            if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool EqualsAny(string value, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (var term in terms)
        {
            if (value.Equals(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
