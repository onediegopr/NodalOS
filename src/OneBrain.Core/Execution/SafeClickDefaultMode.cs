namespace OneBrain.Core.Execution;

/// <summary>
/// Global, local-first kill-switch for safe.click default routing.
/// Controls whether a safe.click step WITHOUT an explicit dispatchPath may be
/// routed to the safe-executor FSM by default.
/// </summary>
public enum SafeClickDefaultMode
{
    /// <summary>safe.click without dispatchPath always uses the legacy path (safe default).</summary>
    Disabled = 0,

    /// <summary>safe.click without dispatchPath may use the FSM only when strictly web-eligible.</summary>
    WebEligible = 1,

    /// <summary>safe.click without dispatchPath is forced to legacy (explicit revert).</summary>
    Legacy = 2,

    /// <summary>safe.click without dispatchPath may use the FSM only when strictly desktop-eligible.</summary>
    DesktopEligible = 3,

    /// <summary>safe.click without dispatchPath may use the FSM when strictly web-eligible or desktop-eligible.</summary>
    AllEligible = 4
}

/// <summary>
/// Pure parser/serializer for the safe.click default-routing kill-switch.
/// Unknown/empty values fail safe to <see cref="SafeClickDefaultMode.Disabled"/>.
/// </summary>
public static class SafeClickDefaultModePolicy
{
    public const string EnvironmentVariableName = "ONEBRAIN_SAFE_CLICK_FSM_DEFAULT";

    public static SafeClickDefaultMode Parse(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant();
        return value switch
        {
            "web-eligible" => SafeClickDefaultMode.WebEligible,
            "desktop-eligible" => SafeClickDefaultMode.DesktopEligible,
            "all-eligible" => SafeClickDefaultMode.AllEligible,
            "legacy" => SafeClickDefaultMode.Legacy,
            "disabled" => SafeClickDefaultMode.Disabled,
            _ => SafeClickDefaultMode.Disabled
        };
    }

    public static string ToWireValue(SafeClickDefaultMode mode)
    {
        return mode switch
        {
            SafeClickDefaultMode.WebEligible => "web-eligible",
            SafeClickDefaultMode.DesktopEligible => "desktop-eligible",
            SafeClickDefaultMode.AllEligible => "all-eligible",
            SafeClickDefaultMode.Legacy => "legacy",
            _ => "disabled"
        };
    }
}
