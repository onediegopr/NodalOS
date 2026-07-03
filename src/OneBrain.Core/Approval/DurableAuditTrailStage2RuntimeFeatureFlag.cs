namespace OneBrain.Core.Approval;

public enum DurableAuditTrailStage2RuntimeFeatureFlagDecision
{
    Allowed,
    Rejected
}

public enum DurableAuditTrailStage2RuntimeFeatureFlagRejectReason
{
    MissingExplicitTestFixture,
    MissingFeatureFlagValue,
    UnexpectedFeatureFlagValue,
    ProductRuntimeScopeRejected
}

public sealed record DurableAuditTrailStage2RuntimeFeatureFlagResult(
    DurableAuditTrailStage2RuntimeFeatureFlagDecision Decision,
    IReadOnlyList<DurableAuditTrailStage2RuntimeFeatureFlagRejectReason> RejectReasons,
    string? EffectiveValue,
    bool RuntimeProductEnabled,
    bool ServiceRegistrationAllowed,
    bool CommandHandlersAllowed,
    bool UiProductActionsAllowed,
    bool ReleaseCommercialReady);

public sealed class DurableAuditTrailStage2RuntimeFeatureFlag
{
    public const string TestOnlyEnabledValue = "enabled:test-only";

    public DurableAuditTrailStage2RuntimeFeatureFlagResult Evaluate(
        bool explicitTestFixture,
        string? featureFlagValue)
    {
        var reasons = new List<DurableAuditTrailStage2RuntimeFeatureFlagRejectReason>();
        if (!explicitTestFixture)
        {
            reasons.Add(DurableAuditTrailStage2RuntimeFeatureFlagRejectReason.MissingExplicitTestFixture);
        }

        if (string.IsNullOrWhiteSpace(featureFlagValue))
        {
            reasons.Add(DurableAuditTrailStage2RuntimeFeatureFlagRejectReason.MissingFeatureFlagValue);
        }
        else
        {
            if (ContainsProductRuntimeScope(featureFlagValue))
            {
                reasons.Add(DurableAuditTrailStage2RuntimeFeatureFlagRejectReason.ProductRuntimeScopeRejected);
            }

            if (!string.Equals(featureFlagValue, TestOnlyEnabledValue, StringComparison.Ordinal))
            {
                reasons.Add(DurableAuditTrailStage2RuntimeFeatureFlagRejectReason.UnexpectedFeatureFlagValue);
            }
        }

        return reasons.Count == 0
            ? new DurableAuditTrailStage2RuntimeFeatureFlagResult(
                DurableAuditTrailStage2RuntimeFeatureFlagDecision.Allowed,
                [],
                featureFlagValue,
                RuntimeProductEnabled: false,
                ServiceRegistrationAllowed: false,
                CommandHandlersAllowed: false,
                UiProductActionsAllowed: false,
                ReleaseCommercialReady: false)
            : new DurableAuditTrailStage2RuntimeFeatureFlagResult(
                DurableAuditTrailStage2RuntimeFeatureFlagDecision.Rejected,
                reasons,
                featureFlagValue,
                RuntimeProductEnabled: false,
                ServiceRegistrationAllowed: false,
                CommandHandlersAllowed: false,
                UiProductActionsAllowed: false,
                ReleaseCommercialReady: false);
    }

    private static bool ContainsProductRuntimeScope(string value) =>
        value.Contains("product", StringComparison.OrdinalIgnoreCase)
        || value.Contains("prod", StringComparison.OrdinalIgnoreCase)
        || value.Contains("runtime", StringComparison.OrdinalIgnoreCase)
        || value.Contains("live", StringComparison.OrdinalIgnoreCase)
        || value.Contains("release", StringComparison.OrdinalIgnoreCase)
        || value.Contains("commercial", StringComparison.OrdinalIgnoreCase);
}
