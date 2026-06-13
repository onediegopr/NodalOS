namespace OneBrain.Core.Execution;

public enum SafeClickLegacyDeprecationSeverity
{
    None = 0,
    Warning = 1,
    ErrorFuture = 2,
    BlockedFuture = 3
}

public sealed record SafeClickLegacyDeprecationPolicy(
    bool IsLegacyDispatch,
    bool IsDeprecated,
    bool RequiresOwner,
    bool RequiresReason,
    bool RequiresReviewBy,
    string Owner,
    string Reason,
    string ReviewBy,
    SafeClickLegacyDeprecationSeverity DeprecationSeverity,
    bool IsCompliant,
    string ViolationReason)
{
    public static SafeClickLegacyDeprecationPolicy NotLegacy { get; } = new(
        IsLegacyDispatch: false,
        IsDeprecated: false,
        RequiresOwner: false,
        RequiresReason: false,
        RequiresReviewBy: false,
        Owner: "",
        Reason: "",
        ReviewBy: "",
        DeprecationSeverity: SafeClickLegacyDeprecationSeverity.None,
        IsCompliant: true,
        ViolationReason: "");
}

public static class SafeClickLegacyDeprecationPolicyEvaluator
{
    public static SafeClickLegacyDeprecationPolicy Evaluate(
        bool isLegacyDispatch,
        string? owner,
        string? reason,
        string? reviewBy)
    {
        if (!isLegacyDispatch)
            return SafeClickLegacyDeprecationPolicy.NotLegacy;

        var normalizedOwner = owner?.Trim() ?? "";
        var normalizedReason = reason?.Trim() ?? "";
        var normalizedReviewBy = reviewBy?.Trim() ?? "";
        var violations = new List<string>();

        if (string.IsNullOrWhiteSpace(normalizedOwner))
            violations.Add("MissingOwner");
        if (string.IsNullOrWhiteSpace(normalizedReason))
            violations.Add("MissingReason");
        if (string.IsNullOrWhiteSpace(normalizedReviewBy))
            violations.Add("MissingReviewBy");

        return new SafeClickLegacyDeprecationPolicy(
            IsLegacyDispatch: true,
            IsDeprecated: true,
            RequiresOwner: true,
            RequiresReason: true,
            RequiresReviewBy: true,
            Owner: normalizedOwner,
            Reason: normalizedReason,
            ReviewBy: normalizedReviewBy,
            DeprecationSeverity: SafeClickLegacyDeprecationSeverity.Warning,
            IsCompliant: violations.Count == 0,
            ViolationReason: string.Join("|", violations));
    }
}
