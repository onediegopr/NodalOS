using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Core.Models;

namespace OneBrain.Observation.Uia;

public sealed class UiaElementReader
{
    // ── Public API ──────────────────────────────────────────────────────────

    public IReadOnlyList<UiElementSnapshot> ReadForegroundWindowElements()
    {
        using var automation = new UIA3Automation();
        AutomationElement? focused = null;
        try { focused = automation.FocusedElement(); } catch { }
        if (focused == null) return Array.Empty<UiElementSnapshot>();
        return ReadFromRoot(focused);
    }

    /// <summary>Backward-compatible: returns elements without truncation flag.</summary>
    public IReadOnlyList<UiElementSnapshot> ReadFromRoot(AutomationElement root)
        => ReadFromRootDetailed(root).Elements;

    public (IReadOnlyList<UiElementSnapshot> Elements, bool WasTruncated) ReadFromHandleDetailed(
        UIA3Automation automation,
        IntPtr hwnd,
        int maxElements = UiaTreeWalker.DefaultMaxElements,
        IReadOnlySet<string>? alwaysIncludeRoles = null,
        bool relaxOffscreen = false,
        UiaSnapshotOptions? options = null)
    {
        options ??= UiaSnapshotOptions.Default;

        if (options.UseCacheRequest)
        {
            try
            {
                using var cache = UiaSnapshotCacheRequestFactory.Create(automation, options).Activate();
                var root = automation.FromHandle(hwnd);
                return ReadFromRootDetailed(root, maxElements, alwaysIncludeRoles, relaxOffscreen);
            }
            catch
            {
                // CacheRequest is an optimisation. If UIA or provider caching fails, preserve old behaviour.
            }
        }

        try
        {
            var root = automation.FromHandle(hwnd);
            return ReadFromRootDetailed(root, maxElements, alwaysIncludeRoles, relaxOffscreen);
        }
        catch
        {
            return (Array.Empty<UiElementSnapshot>(), false);
        }
    }

    /// <summary>
    /// Full read. Returns element list plus a flag that is true when the walk
    /// was capped at <paramref name="maxElements"/> (tree may have more nodes).
    /// </summary>
    public (IReadOnlyList<UiElementSnapshot> Elements, bool WasTruncated) ReadFromRootDetailed(
        AutomationElement root,
        int maxElements = UiaTreeWalker.DefaultMaxElements,
        IReadOnlySet<string>? alwaysIncludeRoles = null,
        bool relaxOffscreen = false)
    {
        var raw = new List<AutomationElement>();
        bool truncated = UiaTreeWalker.Walk(root, raw, maxElements,
            UiaTreeWalker.DefaultMaxDepth, alwaysIncludeRoles, relaxOffscreen);
        return (raw.Select((e, i) => Map(e, i + 1)).ToList(), truncated);
    }

    // ── Element mapping ─────────────────────────────────────────────────────

    private UiElementSnapshot Map(AutomationElement e, int id)
    {
        var patterns = new List<string>();
        try
        {
            if (e.Patterns.Invoke.IsSupported)       patterns.Add("Invoke");
            if (e.Patterns.Value.IsSupported)         patterns.Add("Value");
            if (e.Patterns.Toggle.IsSupported)        patterns.Add("Toggle");
            if (e.Patterns.SelectionItem.IsSupported) patterns.Add("Selection");
        }
        catch { }

        var actions = DeriveActions(e, patterns);

        return new UiElementSnapshot(
            Ref:                 $"@e{id}",
            Role:                UiaTreeWalker.SafeRole(e),
            Name:                UiaTreeWalker.SafeName(e),
            AutomationId:        UiaTreeWalker.SafeId(e),
            ClassName:           UiaTreeWalker.SafeClass(e),
            Bounds:              MapBounds(e),
            IsEnabled:           UiaTreeWalker.SafeEnabled(e),
            IsOffscreen:         UiaTreeWalker.SafeOffscreen(e),
            IsKeyboardFocusable: false,
            Patterns:            patterns,
            Actions:             actions,
            RuntimeId:           UiaTreeWalker.SafeRuntimeId(e));
    }

    private static IReadOnlyList<string> DeriveActions(AutomationElement e, IReadOnlyList<string> patterns)
    {
        var actions = new List<string>();
        var role    = UiaTreeWalker.SafeRole(e);

        if (patterns.Contains("Invoke"))    actions.Add("invoke");
        if (patterns.Contains("Value"))     actions.Add("set_value");
        if (patterns.Contains("Value"))     actions.Add("read_value");
        if (patterns.Contains("Toggle"))    actions.Add("toggle");
        if (patterns.Contains("Selection")) actions.Add("select");

        if (role.Equals("Document", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Edit",     StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("type_text");
        }

        if (role.Equals("Button", StringComparison.OrdinalIgnoreCase) && !actions.Contains("invoke"))
            actions.Add("invoke_candidate");

        if (role.Equals("Window", StringComparison.OrdinalIgnoreCase))
            actions.Add("focus_window");

        return actions;
    }

    private static WindowBounds MapBounds(AutomationElement e)
    {
        try
        {
            var b = e.BoundingRectangle;
            return new WindowBounds((int)b.Left, (int)b.Top, (int)b.Right, (int)b.Bottom);
        }
        catch
        {
            return new WindowBounds(0, 0, 0, 0);
        }
    }
}
