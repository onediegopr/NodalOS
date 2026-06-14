namespace OneBrain.Core.Execution;

public sealed record LegacyExecutionDecision(
    bool Allowed,
    string Reason,
    string Surface,
    string StepKind,
    string OptInSource,
    bool IsQuarantined);

public static class LegacyExecutionGuard
{
    public const string EnvironmentVariable = "ONEBRAIN_ALLOW_LEGACY_ACTIONS";

    public static LegacyExecutionDecision Evaluate(
        string? stepKind,
        string? surface,
        IReadOnlyDictionary<string, string> environment,
        bool explicitRecipeOptIn)
    {
        var normalizedStepKind = stepKind?.Trim() ?? "";
        var normalizedSurface = surface?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(normalizedSurface) || !IsKnownLegacySurface(normalizedSurface))
        {
            return Deny(normalizedStepKind, normalizedSurface, "LegacyUnknownSurface");
        }

        if (IsSafeAction(normalizedStepKind))
        {
            return Deny(normalizedStepKind, normalizedSurface, "LegacyBlockedForSafeAction");
        }

        if (!IsLegacyStepKind(normalizedStepKind))
        {
            return Deny(normalizedStepKind, normalizedSurface, "LegacyStepKindNotAllowed");
        }

        var envOptIn = environment.TryGetValue(EnvironmentVariable, out var value) &&
                       string.Equals(value?.Trim(), "1", StringComparison.Ordinal);

        if (!envOptIn || !explicitRecipeOptIn)
        {
            var reason = !envOptIn && !explicitRecipeOptIn
                ? "LegacyQuarantined:EnvironmentAndRecipeOptInRequired"
                : !envOptIn
                    ? "LegacyQuarantined:EnvironmentOptInRequired"
                    : "LegacyQuarantined:RecipeOptInRequired";
            return Deny(normalizedStepKind, normalizedSurface, reason);
        }

        return new LegacyExecutionDecision(
            Allowed: true,
            Reason: "LegacyExplicitOptInAllowed",
            Surface: normalizedSurface,
            StepKind: normalizedStepKind,
            OptInSource: $"{EnvironmentVariable}+allowLegacyActions",
            IsQuarantined: true);
    }

    public static IReadOnlyDictionary<string, string> ReadProcessEnvironment()
    {
        var value = Environment.GetEnvironmentVariable(EnvironmentVariable) ?? "";
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EnvironmentVariable] = value
        };
    }

    private static LegacyExecutionDecision Deny(string stepKind, string surface, string reason) =>
        new(
            Allowed: false,
            Reason: reason,
            Surface: surface,
            StepKind: stepKind,
            OptInSource: "",
            IsQuarantined: true);

    private static bool IsSafeAction(string stepKind) =>
        stepKind.StartsWith("safe.", StringComparison.OrdinalIgnoreCase);

    private static bool IsLegacyStepKind(string stepKind) =>
        stepKind.Equals("actv.type", StringComparison.OrdinalIgnoreCase) ||
        stepKind.Equals("actv.invoke", StringComparison.OrdinalIgnoreCase) ||
        stepKind.Equals("key", StringComparison.OrdinalIgnoreCase);

    private static bool IsKnownLegacySurface(string surface) =>
        surface.Equals("actv.type", StringComparison.OrdinalIgnoreCase) ||
        surface.Equals("actv.invoke", StringComparison.OrdinalIgnoreCase) ||
        surface.Equals("key", StringComparison.OrdinalIgnoreCase) ||
        surface.Equals("UiaActionExecutor", StringComparison.OrdinalIgnoreCase) ||
        surface.Equals("BasicActionVerifier", StringComparison.OrdinalIgnoreCase) ||
        surface.Equals("GetClickablePoint", StringComparison.OrdinalIgnoreCase);
}
