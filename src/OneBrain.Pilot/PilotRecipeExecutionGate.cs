namespace OneBrain.Pilot;

public sealed record PilotRecipeExecutionGateResult(
    bool Enabled,
    string EnvironmentVariableName,
    string Status,
    PilotSafetySummary Safety);

public static class PilotRecipeExecutionGate
{
    public const string EnvironmentVariableName = "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION";
    public const string EnabledValue = "1";
    public const string BlockedStatus =
        "blocked: pilot recipe execution is a separate lab/dev runtime footprint and is disabled by default; set NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION=1 only for explicit local lab/dev opt-in";

    public static PilotRecipeExecutionGateResult Evaluate() =>
        Evaluate(Environment.GetEnvironmentVariable(EnvironmentVariableName));

    public static PilotRecipeExecutionGateResult Evaluate(string? value)
    {
        var enabled = string.Equals(value, EnabledValue, StringComparison.Ordinal);
        return new PilotRecipeExecutionGateResult(
            Enabled: enabled,
            EnvironmentVariableName: EnvironmentVariableName,
            Status: enabled
                ? "enabled: explicit local lab/dev opt-in for pilot recipe execution"
                : BlockedStatus,
            Safety: PilotSafetySummary.LabDevRuntimeFootprintDefaultBlocked);
    }
}
