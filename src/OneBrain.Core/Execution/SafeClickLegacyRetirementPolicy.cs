namespace OneBrain.Core.Execution;

public sealed record SafeClickLegacyRetirementPolicy(
    bool Enabled,
    bool Retired,
    bool Blocked,
    string Reason,
    string DispatchPath,
    string RequiredAction,
    bool IneligibleAfterRetirement,
    bool LegacyDispatchRejected)
{
    public const string SafeExecutorRequiredAction = "Use safe-executor/FSM eligible path";
}

public static class SafeClickLegacyRetirementPolicyEvaluator
{
    public static SafeClickLegacyRetirementPolicy Evaluate(string? dispatchPath, bool ineligibleAfterRetirement)
    {
        var normalizedDispatchPath = string.IsNullOrWhiteSpace(dispatchPath)
            ? ""
            : dispatchPath.Trim();
        var legacyDispatchRejected = string.Equals(normalizedDispatchPath, "legacy", StringComparison.OrdinalIgnoreCase);
        var reason = legacyDispatchRejected ? "LegacyDispatchRetired" : "SafeClickLegacyRetired";

        return new SafeClickLegacyRetirementPolicy(
            Enabled: true,
            Retired: true,
            Blocked: true,
            Reason: reason,
            DispatchPath: normalizedDispatchPath,
            RequiredAction: SafeClickLegacyRetirementPolicy.SafeExecutorRequiredAction,
            IneligibleAfterRetirement: ineligibleAfterRetirement,
            LegacyDispatchRejected: legacyDispatchRejected);
    }
}
