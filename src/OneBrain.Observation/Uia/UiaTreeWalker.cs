using FlaUI.Core.AutomationElements;

namespace OneBrain.Observation.Uia;

/// <summary>
/// Shared UIA tree walker with safe property helpers.
/// Single source of truth for tree traversal used by both
/// UiaElementReader (snapshot) and UiaActionExecutor (action).
/// </summary>
public static class UiaTreeWalker
{
    public const int DefaultMaxElements = 250;
    public const int BrowserMaxElements = 500;
    public const int DefaultMaxDepth    = 20;

    // Always include these roles even when Name and AutomationId are both empty.
    public static readonly IReadOnlySet<string> CoreIncludeRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Document", "Edit" };

    // Broader role set for browser/web contexts where many nodes have no Name/Id.
    public static readonly IReadOnlySet<string> BrowserRelevantRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Document", "Edit", "Button", "Hyperlink", "Text",
            "Heading", "ComboBox", "MenuItem", "TabItem", "ListItem"
        };

    // ── Safe property helpers ──────────────────────────────────────────────
    // FlaUI can throw PropertyNotSupportedException on any property access.

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
        try { return e.FindAllChildren(); } catch { return []; }
    }

    // ── Tree walk ──────────────────────────────────────────────────────────

    /// <summary>
    /// Walks the UIA tree from <paramref name="root"/>, collecting qualifying elements.
    /// Returns <c>true</c> when the walk was capped by <paramref name="maxElements"/>
    /// (i.e. the tree may have more nodes than what was collected).
    /// </summary>
    public static bool Walk(
        AutomationElement root,
        List<AutomationElement> results,
        int maxElements = DefaultMaxElements,
        int maxDepth    = DefaultMaxDepth,
        IReadOnlySet<string>? alwaysIncludeRoles = null)
    {
        var roles = alwaysIncludeRoles ?? CoreIncludeRoles;
        WalkCore(root, results, 0, maxElements, maxDepth, roles);
        return results.Count >= maxElements;
    }

    private static void WalkCore(
        AutomationElement e,
        List<AutomationElement> res,
        int depth,
        int maxElements,
        int maxDepth,
        IReadOnlySet<string> alwaysIncludeRoles)
    {
        if (res.Count >= maxElements || depth > maxDepth) return;

        try
        {
            var role    = SafeRole(e);
            var include = !SafeOffscreen(e) && (
                !string.IsNullOrEmpty(SafeName(e)) ||
                !string.IsNullOrEmpty(SafeId(e))   ||
                alwaysIncludeRoles.Contains(role));

            if (include) res.Add(e);

            foreach (var child in SafeChildren(e))
                WalkCore(child, res, depth + 1, maxElements, maxDepth, alwaysIncludeRoles);
        }
        catch { }
    }
}
