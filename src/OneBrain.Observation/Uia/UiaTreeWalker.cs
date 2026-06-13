using FlaUI.Core.AutomationElements;

namespace OneBrain.Observation.Uia;

/// <summary>
/// Shared UIA tree walker and safe property helpers.
/// Single source of truth used by Observation (snapshot) and Actions (executor).
/// </summary>
public static class UiaTreeWalker
{
    public const int DefaultMaxElements = 250;
    public const int BrowserMaxElements = 500;
    public const int DefaultMaxDepth    = 20;

    // Roles always included even when Name and AutomationId are both empty.
    public static readonly IReadOnlySet<string> CoreIncludeRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Document", "Edit" };

    // Broader set for browser/web contexts.
    public static readonly IReadOnlySet<string> BrowserRelevantRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Document", "Edit", "Button", "Hyperlink", "Text",
            "Heading", "ComboBox", "MenuItem", "TabItem", "ListItem", "Pane"
        };

    // ── Safe property helpers ──────────────────────────────────────────────
    // FlaUI can throw PropertyNotSupportedException on ANY property access.

    public static string SafeRole(AutomationElement e)
    {
        try { return e.ControlType.ToString(); } catch { return ""; }
    }

    public static string SafeName(AutomationElement e)
    {
        try { return e.Name ?? ""; } catch { return ""; }
    }

    public static string SafeId(AutomationElement e)
    {
        try { return e.AutomationId ?? ""; } catch { return ""; }
    }

    public static string SafeClass(AutomationElement e)
    {
        try { return e.ClassName ?? ""; } catch { return ""; }
    }

    public static string SafeFrameworkId(AutomationElement e)
    {
        try
        {
            var frameworkIdProperty = e.GetType().GetProperty("FrameworkId");
            var frameworkIdValue = frameworkIdProperty?.GetValue(e);
            if (frameworkIdValue is string frameworkId && !string.IsNullOrWhiteSpace(frameworkId))
                return frameworkId.Trim();

            var frameworkTypeProperty = e.GetType().GetProperty("FrameworkType");
            var frameworkTypeValue = frameworkTypeProperty?.GetValue(e);
            return frameworkTypeValue?.ToString()?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    public static string SafeRuntimeId(AutomationElement e)
    {
        try { return FormatRuntimeId(e.FrameworkAutomationElement.RuntimeId); } catch { return ""; }
    }

    public static string FormatRuntimeId(int[]? runtimeId)
    {
        return runtimeId is { Length: > 0 }
            ? string.Join(".", runtimeId)
            : "";
    }

    public static string SafeHelpText(AutomationElement e)
    {
        try { return e.HelpText ?? ""; } catch { return ""; }
    }

    public static string SafeValue(AutomationElement e)
    {
        try
        {
            if (!e.Patterns.Value.IsSupported) return "";
            return e.Patterns.Value.Pattern.Value ?? "";
        }
        catch { return ""; }
    }

    public static string SafeLegacyName(AutomationElement e)
    {
        try
        {
            if (!e.Patterns.LegacyIAccessible.IsSupported) return "";
            return e.Patterns.LegacyIAccessible.Pattern.Name ?? "";
        }
        catch { return ""; }
    }

    public static string SafeLegacyValue(AutomationElement e)
    {
        try
        {
            if (!e.Patterns.LegacyIAccessible.IsSupported) return "";
            return e.Patterns.LegacyIAccessible.Pattern.Value ?? "";
        }
        catch { return ""; }
    }

    public static string SafeLabeledByName(AutomationElement e)
    {
        // LabeledBy is not exposed as a direct property in FlaUI 5.x — returns empty.
        return "";
    }

    public static bool SafeOffscreen(AutomationElement e)
    {
        try { return e.IsOffscreen; } catch { return true; }
    }

    public static bool SafeEnabled(AutomationElement e)
    {
        try { return e.IsEnabled; } catch { return false; }
    }

    public static IEnumerable<AutomationElement> SafeChildren(AutomationElement e)
    {
        try
        {
            var cached = e.CachedChildren;
            if (cached.Length > 0) return cached;
        }
        catch { }

        try { return e.FindAllChildren(); } catch { return []; }
    }

    // ── Browser process detection ──────────────────────────────────────────

    public static bool IsBrowserProcess(string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        return processName.Contains("msedge",  StringComparison.OrdinalIgnoreCase) ||
               processName.Contains("chrome",  StringComparison.OrdinalIgnoreCase) ||
               processName.Contains("firefox", StringComparison.OrdinalIgnoreCase);
    }

    // ── Inline include check (mirrors WalkCore logic) ─────────────────────

    /// <summary>
    /// Returns true when WalkCore would include this element.
    /// Useful for diagnostic code that walks independently.
    /// </summary>
    public static bool WouldInclude(AutomationElement e, bool browserMode = false)
    {
        var roles = browserMode ? BrowserRelevantRoles : CoreIncludeRoles;
        var role            = SafeRole(e);
        bool isRelevantRole = roles.Contains(role);
        bool offscreen      = SafeOffscreen(e);
        bool passOffscreen  = !offscreen || (browserMode && isRelevantRole);

        return passOffscreen && (
            !string.IsNullOrEmpty(SafeName(e)) ||
            !string.IsNullOrEmpty(SafeId(e))   ||
            isRelevantRole);
    }

    // ── Tree walk ──────────────────────────────────────────────────────────

    /// <summary>
    /// Walks the UIA tree from <paramref name="root"/>.
    /// Returns <c>true</c> when the walk was capped by <paramref name="maxElements"/>.
    /// </summary>
    /// <param name="relaxOffscreenForRoles">
    /// When true (browser mode), elements whose role is in <paramref name="alwaysIncludeRoles"/>
    /// are included even when IsOffscreen is true.  Edge reports some HTML controls as offscreen
    /// even with --force-renderer-accessibility; relaxing this recovers them.
    /// </param>
    public static bool Walk(
        AutomationElement root,
        List<AutomationElement> results,
        int maxElements              = DefaultMaxElements,
        int maxDepth                 = DefaultMaxDepth,
        IReadOnlySet<string>? alwaysIncludeRoles = null,
        bool relaxOffscreenForRoles  = false)
    {
        var roles = alwaysIncludeRoles ?? CoreIncludeRoles;
        WalkCore(root, results, 0, maxElements, maxDepth, roles, relaxOffscreenForRoles);
        return results.Count >= maxElements;
    }

    public static AutomationElement? FindByRuntimeId(
        AutomationElement root,
        string runtimeId,
        int maxElements = DefaultMaxElements,
        int maxDepth = DefaultMaxDepth)
    {
        if (string.IsNullOrWhiteSpace(runtimeId)) return null;
        var visited = 0;
        return FindByRuntimeIdCore(root, runtimeId, 0, maxElements, maxDepth, ref visited);
    }

    private static AutomationElement? FindByRuntimeIdCore(
        AutomationElement e,
        string runtimeId,
        int depth,
        int maxElements,
        int maxDepth,
        ref int visited)
    {
        if (visited >= maxElements || depth > maxDepth) return null;
        visited++;

        try
        {
            if (SafeRuntimeId(e) == runtimeId) return e;

            foreach (var child in SafeChildren(e))
            {
                var found = FindByRuntimeIdCore(child, runtimeId, depth + 1, maxElements, maxDepth, ref visited);
                if (found != null) return found;
                if (visited >= maxElements) break;
            }
        }
        catch { }

        return null;
    }

    private static void WalkCore(
        AutomationElement e,
        List<AutomationElement> res,
        int depth,
        int maxElements,
        int maxDepth,
        IReadOnlySet<string> alwaysIncludeRoles,
        bool relaxOffscreenForRoles)
    {
        if (res.Count >= maxElements || depth > maxDepth) return;

        try
        {
            var role            = SafeRole(e);
            bool isRelevantRole = alwaysIncludeRoles.Contains(role);
            bool offscreen      = SafeOffscreen(e);
            bool passOffscreen  = !offscreen || (relaxOffscreenForRoles && isRelevantRole);

            var include = passOffscreen && (
                !string.IsNullOrEmpty(SafeName(e)) ||
                !string.IsNullOrEmpty(SafeId(e))   ||
                isRelevantRole);

            if (include) res.Add(e);

            foreach (var child in SafeChildren(e))
                WalkCore(child, res, depth + 1, maxElements, maxDepth, alwaysIncludeRoles, relaxOffscreenForRoles);
        }
        catch { }
    }
}
