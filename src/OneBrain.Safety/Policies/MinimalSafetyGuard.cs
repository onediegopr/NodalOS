namespace OneBrain.Safety.Policies;

public sealed class MinimalSafetyGuard
{
    private static readonly string[] DangerousNames =
    [
        "cerrar",
        "cerrar pestaña",
        "close",
        "minimizar",
        "maximizar",
        "eliminar",
        "borrar",
        "delete",
        "remove",
        "pagar",
        "pay",
        "enviar",
        "send",
        "emitir",
        "install",
        "instalar",
        "ejecutar",
        "run"
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
        {
            return new SafetyDecision(true, "Allowed.");
        }

        if (ContainsAny(name, DangerousNames))
        {
            return new SafetyDecision(false, $"Blocked by safety policy: dangerous target name '{name}'.");
        }

        if (EqualsAny(automationId, DangerousAutomationIds))
        {
            return new SafetyDecision(false, $"Blocked by safety policy: dangerous automation id '{automationId}'.");
        }

        if (role.Equals("Button", StringComparison.OrdinalIgnoreCase) &&
            ContainsAny(name, DangerousNames))
        {
            return new SafetyDecision(false, $"Blocked by safety policy: dangerous button '{name}'.");
        }

        return new SafetyDecision(true, "Allowed.");
    }

    private static bool IsPotentiallyDangerousAction(string actionKind)
    {
        return actionKind.Equals("invoke", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string value, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var term in terms)
        {
            if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EqualsAny(string value, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var term in terms)
        {
            if (value.Equals(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
