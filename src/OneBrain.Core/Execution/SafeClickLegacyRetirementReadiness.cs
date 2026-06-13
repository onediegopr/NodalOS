using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Core.Execution;

public sealed record SafeClickLegacyRetirementReadiness(
    int TotalSafeClicks,
    int DefaultFsmRouted,
    int ExplicitLegacyOptOut,
    int LegacyPathUsed,
    int ElClickUsed,
    int UiaActionExecutorUsed,
    int UnsafeFallbackUsed,
    int NonCompliantLegacyOptOut,
    int DesktopExcluded,
    int WebExcluded,
    int AllEligibleModeObserved,
    bool IsReadyForRetirement,
    IReadOnlyList<string> BlockingReasons)
{
    [JsonIgnore]
    public string Summary =>
        IsReadyForRetirement
            ? "safe.click legacy retirement: ready for this run"
            : $"safe.click legacy retirement: blocked ({string.Join(", ", BlockingReasons)})";

    [JsonIgnore]
    public string ReportJson => JsonSerializer.Serialize(this);
}

public static class SafeClickLegacyRetirementReadinessEvaluator
{
    public static SafeClickLegacyRetirementReadiness Evaluate(
        int totalSafeClicks,
        int defaultFsmRouted,
        int explicitLegacyOptOut,
        int legacyPathUsed,
        int elClickUsed,
        int uiaActionExecutorUsed,
        int unsafeFallbackUsed,
        int nonCompliantLegacyOptOut,
        int desktopExcluded,
        int webExcluded,
        int allEligibleModeObserved,
        int unknownDispatchPathBlocked)
    {
        var blockingReasons = new List<string>();

        if (legacyPathUsed > 0)
            blockingReasons.Add("LegacyPathUsed");
        if (elClickUsed > 0)
            blockingReasons.Add("ElClickUsed");
        if (uiaActionExecutorUsed > 0)
            blockingReasons.Add("UiaActionExecutorUsed");
        if (unsafeFallbackUsed > 0)
            blockingReasons.Add("UnsafeFallbackUsed");
        if (nonCompliantLegacyOptOut > 0)
            blockingReasons.Add("NonCompliantLegacyOptOut");
        if (unknownDispatchPathBlocked > 0)
            blockingReasons.Add("UnknownDispatchPathBlocked");

        return new SafeClickLegacyRetirementReadiness(
            TotalSafeClicks: totalSafeClicks,
            DefaultFsmRouted: defaultFsmRouted,
            ExplicitLegacyOptOut: explicitLegacyOptOut,
            LegacyPathUsed: legacyPathUsed,
            ElClickUsed: elClickUsed,
            UiaActionExecutorUsed: uiaActionExecutorUsed,
            UnsafeFallbackUsed: unsafeFallbackUsed,
            NonCompliantLegacyOptOut: nonCompliantLegacyOptOut,
            DesktopExcluded: desktopExcluded,
            WebExcluded: webExcluded,
            AllEligibleModeObserved: allEligibleModeObserved,
            IsReadyForRetirement: totalSafeClicks > 0 && blockingReasons.Count == 0,
            BlockingReasons: blockingReasons);
    }
}
