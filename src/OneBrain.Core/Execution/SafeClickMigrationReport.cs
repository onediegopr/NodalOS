using System.Text.Json;

namespace OneBrain.Core.Execution;

public enum SafeClickMigrationReadinessReason
{
    Ready = 0,
    ApprovalV2 = 1,
    MissingTargetObserve = 2,
    MissingIdentity = 3,
    WeakIdentity = 4,
    RuntimeIdMissing = 5,
    RuntimeIdChanged = 6,
    WouldUseLegacyFallback = 7,
    Ambiguous = 8,
    NotFound = 9,
    PolicyDenied = 10,
    MissingManifest = 11,
    Blocked = 12,
    Unknown = 13,
    InvokePatternUnavailable = 14,
    RoleNotAllowed = 15,
    NotWebUia = 16
}

public sealed record SafeClickMigrationSummary(
    int TotalSafeClicks,
    int EligibleForFsm,
    int NotEligibleForFsm,
    int ApprovalV3Strong,
    int ApprovalV2,
    int WeakIdentity,
    int TargetObservePresent,
    int TargetObserveMissing,
    int RuntimeIdPresent,
    int RuntimeIdMissing,
    int RuntimeIdStable,
    int RuntimeIdChanged,
    int RuntimeIdUnknown,
    int InvokePatternAvailable,
    int InvokePatternUnavailable,
    int InvokePatternUnknown,
    int UsesElClick,
    int UsesUiaActionExecutor,
    int WouldRequireLegacy,
    int WouldUseUnsafeFallback,
    int WebUiaEligible,
    int DesktopUiaObservable,
    int DesktopUiaStrong,
    int DesktopUiaWeak,
    int DesktopMissingIdentity,
    int RuntimeStabilityChecked,
    int RuntimeStable,
    int RuntimeChanged,
    int RuntimeMissing,
    int ReobserveAttempted,
    int ReobserveSucceeded,
    int ReobserveChanged,
    int DefaultDispatchBlockedByStaleIdentity,
    int DefaultDispatchBlockedByMissingIdentity,
    int DesktopEligibleForFsm,
    int DesktopNotEligibleForFsm,
    int DesktopRuntimeStable,
    int DesktopRuntimeChanged,
    int DesktopInvokePatternAvailable,
    int DesktopRoleAllowed,
    int DesktopRootAvailable,
    int DesktopOptInRouted,
    int DesktopOptInBlocked,
    int DesktopDefaultFsmEnabled,
    int DesktopDefaultFsmRouted,
    int DesktopDefaultEligibleButNotEnabled,
    int DesktopDefaultBlocked,
    int DesktopDefaultBlockedByStaleIdentity,
    int AllEligibleModeEnabled,
    int DefaultFsmScopeWeb,
    int DefaultFsmScopeDesktop,
    IReadOnlyDictionary<SafeClickMigrationReadinessReason, int> BlockingReasons)
{
    public double ReadinessPercent =>
        TotalSafeClicks == 0
            ? 0
            : Math.Round((double)EligibleForFsm * 100d / TotalSafeClicks, 2, MidpointRounding.AwayFromZero);
}

public sealed record SafeClickMigrationReadinessReport(
    SafeClickMigrationSummary Summary,
    string Json,
    string Markdown);

public static class SafeClickMigrationReportBuilder
{
    public static SafeClickMigrationReadinessReport Build(IEnumerable<SafeClickShadowReadiness> readinessItems)
    {
        ArgumentNullException.ThrowIfNull(readinessItems);

        var items = readinessItems.ToList();
        var blockingReasons = items
            .Where(item => !item.EligibleForFsm)
            .GroupBy(item => item.ReadinessReason)
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count());

        var summary = new SafeClickMigrationSummary(
            TotalSafeClicks: items.Count,
            EligibleForFsm: items.Count(item => item.EligibleForFsm),
            NotEligibleForFsm: items.Count(item => !item.EligibleForFsm),
            ApprovalV3Strong: items.Sum(item => item.Metrics.ApprovalV3Strong),
            ApprovalV2: items.Sum(item => item.Metrics.ApprovalV2),
            WeakIdentity: items.Sum(item => item.Metrics.WeakIdentity),
            TargetObservePresent: items.Sum(item => item.Metrics.TargetObservePresent),
            TargetObserveMissing: items.Sum(item => item.Metrics.TargetObserveMissing),
            RuntimeIdPresent: items.Sum(item => item.Metrics.RuntimeIdPresent),
            RuntimeIdMissing: items.Sum(item => item.Metrics.RuntimeIdMissing),
            RuntimeIdStable: items.Sum(item => item.Metrics.RuntimeIdStable),
            RuntimeIdChanged: items.Sum(item => item.Metrics.RuntimeIdChanged),
            RuntimeIdUnknown: items.Sum(item => item.Metrics.RuntimeIdUnknown),
            InvokePatternAvailable: items.Sum(item => item.Metrics.InvokePatternAvailable),
            InvokePatternUnavailable: items.Sum(item => item.Metrics.InvokePatternUnavailable),
            InvokePatternUnknown: items.Sum(item => item.Metrics.InvokePatternUnknown),
            UsesElClick: items.Sum(item => item.Metrics.UsesElClick),
            UsesUiaActionExecutor: items.Sum(item => item.Metrics.UsesUiaActionExecutor),
            WouldRequireLegacy: items.Sum(item => item.Metrics.WouldRequireLegacy),
            WouldUseUnsafeFallback: items.Sum(item => item.Metrics.WouldUseUnsafeFallback),
            WebUiaEligible: items.Sum(item => item.Metrics.WebUiaEligible),
            DesktopUiaObservable: items.Sum(item => item.Metrics.DesktopUiaObservable),
            DesktopUiaStrong: items.Sum(item => item.Metrics.DesktopUiaStrong),
            DesktopUiaWeak: items.Sum(item => item.Metrics.DesktopUiaWeak),
            DesktopMissingIdentity: items.Sum(item => item.Metrics.DesktopMissingIdentity),
            RuntimeStabilityChecked: items.Sum(item => item.Metrics.RuntimeStabilityChecked),
            RuntimeStable: items.Sum(item => item.Metrics.RuntimeStable),
            RuntimeChanged: items.Sum(item => item.Metrics.RuntimeChanged),
            RuntimeMissing: items.Sum(item => item.Metrics.RuntimeMissing),
            ReobserveAttempted: items.Sum(item => item.Metrics.ReobserveAttempted),
            ReobserveSucceeded: items.Sum(item => item.Metrics.ReobserveSucceeded),
            ReobserveChanged: items.Sum(item => item.Metrics.ReobserveChanged),
            DefaultDispatchBlockedByStaleIdentity: items.Sum(item => item.Metrics.DefaultDispatchBlockedByStaleIdentity),
            DefaultDispatchBlockedByMissingIdentity: items.Sum(item => item.Metrics.DefaultDispatchBlockedByMissingIdentity),
            DesktopEligibleForFsm: items.Sum(item => item.Metrics.DesktopEligibleForFsm),
            DesktopNotEligibleForFsm: items.Sum(item => item.Metrics.DesktopNotEligibleForFsm),
            DesktopRuntimeStable: items.Sum(item => item.Metrics.DesktopRuntimeStable),
            DesktopRuntimeChanged: items.Sum(item => item.Metrics.DesktopRuntimeChanged),
            DesktopInvokePatternAvailable: items.Sum(item => item.Metrics.DesktopInvokePatternAvailable),
            DesktopRoleAllowed: items.Sum(item => item.Metrics.DesktopRoleAllowed),
            DesktopRootAvailable: items.Sum(item => item.Metrics.DesktopRootAvailable),
            DesktopOptInRouted: items.Sum(item => item.Metrics.DesktopOptInRouted),
            DesktopOptInBlocked: items.Sum(item => item.Metrics.DesktopOptInBlocked),
            DesktopDefaultFsmEnabled: items.Sum(item => item.Metrics.DesktopDefaultFsmEnabled),
            DesktopDefaultFsmRouted: items.Sum(item => item.Metrics.DesktopDefaultFsmRouted),
            DesktopDefaultEligibleButNotEnabled: items.Sum(item => item.Metrics.DesktopDefaultEligibleButNotEnabled),
            DesktopDefaultBlocked: items.Sum(item => item.Metrics.DesktopDefaultBlocked),
            DesktopDefaultBlockedByStaleIdentity: items.Sum(item => item.Metrics.DesktopDefaultBlockedByStaleIdentity),
            AllEligibleModeEnabled: items.Sum(item => item.Metrics.AllEligibleModeEnabled),
            DefaultFsmScopeWeb: items.Sum(item => item.Metrics.DefaultFsmScopeWeb),
            DefaultFsmScopeDesktop: items.Sum(item => item.Metrics.DefaultFsmScopeDesktop),
            BlockingReasons: blockingReasons);

        var json = JsonSerializer.Serialize(summary);
        var markdown = BuildMarkdown(summary);
        return new SafeClickMigrationReadinessReport(summary, json, markdown);
    }

    private static string BuildMarkdown(SafeClickMigrationSummary summary)
    {
        var blockingReasons = summary.BlockingReasons.Count == 0
            ? "none"
            : string.Join(", ",
                summary.BlockingReasons
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key.ToString(), StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}={entry.Value}"));

        return string.Join(Environment.NewLine,
        [
            "# safe.click migration readiness",
            $"total={summary.TotalSafeClicks}",
            $"eligible={summary.EligibleForFsm}",
            $"notEligible={summary.NotEligibleForFsm}",
            $"readinessPercent={summary.ReadinessPercent.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}",
            $"legacyFallbackCount={summary.WouldUseUnsafeFallback}",
            $"blockingReasons={blockingReasons}"
        ]);
    }
}
