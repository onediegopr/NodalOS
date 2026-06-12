using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Observation.Sessions;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation.Uia;

/// <summary>
/// Deep UIA tree reader for the 'diagnose uia' CLI command.
/// Walks ALL elements (no offscreen/name filter) and collects every accessible property.
/// Unlike the snapshot walker, correctness beats completeness — it never throws.
/// </summary>
public sealed class UiaDiagnosticReader
{
    private const int MaxDiagElements = 1000;
    private const int MaxDiagDepth    = 30;

    private readonly WindowFinder _windowFinder = new();

    /// <param name="containsFilter">Include only elements where any text field contains this string.</param>
    /// <param name="roleFilter">Include only elements with exactly this role.</param>
    /// <param name="raw">Skip all post-walk filters; return everything up to the cap.</param>
    public IReadOnlyList<UiaDiagnosticEntry> Read(
        string? processName,
        string? windowTitle,
        string? containsFilter = null,
        string? roleFilter     = null,
        bool    raw            = false)
    {
        IntPtr hwnd = IntPtr.Zero;
        if (!string.IsNullOrEmpty(processName) || !string.IsNullOrEmpty(windowTitle))
            hwnd = _windowFinder.FindWindow(processName, windowTitle);
        if (hwnd == IntPtr.Zero) return Array.Empty<UiaDiagnosticEntry>();

        return ReadFromHandle(hwnd, processName, containsFilter, roleFilter, raw);
    }

    /// <summary>Walk the UIA tree directly from an explicit HWND.</summary>
    public IReadOnlyList<UiaDiagnosticEntry> ReadFromHandle(
        IntPtr  hwnd,
        string? processName    = null,
        string? containsFilter = null,
        string? roleFilter     = null,
        bool    raw            = false)
    {
        using var session = new PerceptionSession();
        return ReadFromHandle(session, hwnd, processName, containsFilter, roleFilter, raw);
    }

    public IReadOnlyList<UiaDiagnosticEntry> ReadFromHandle(
        PerceptionSession session,
        IntPtr  hwnd,
        string? processName    = null,
        string? containsFilter = null,
        string? roleFilter     = null,
        bool    raw            = false)
    {
        return ReadFromHandleCore(session.Automation, hwnd, processName, containsFilter, roleFilter, raw);
    }

    private IReadOnlyList<UiaDiagnosticEntry> ReadFromHandleCore(
        UIA3Automation automation,
        IntPtr  hwnd,
        string? processName,
        string? containsFilter,
        string? roleFilter,
        bool    raw)
    {
        AutomationElement? root = null;
        IDisposable? cache = null;
        try
        {
            cache = UiaSnapshotCacheRequestFactory.Create(automation).Activate();
            root = automation.FromHandle(hwnd);
        }
        catch
        {
            cache?.Dispose();
            try { root = automation.FromHandle(hwnd); } catch { return Array.Empty<UiaDiagnosticEntry>(); }
        }

        if (root == null)
        {
            cache?.Dispose();
            return Array.Empty<UiaDiagnosticEntry>();
        }

        try
        {
            bool isBrowser = UiaTreeWalker.IsBrowserProcess(processName);

            var all     = new List<UiaDiagnosticEntry>(256);
            int counter = 0;
            WalkFull(root, all, isBrowser, 0, "Window", ref counter);

            if (raw) return all;

            IEnumerable<UiaDiagnosticEntry> filtered = all;

            if (containsFilter != null)
            {
                filtered = all.Where(e =>
                    Contains(e.Name,         containsFilter) ||
                    Contains(e.HelpText,     containsFilter) ||
                    Contains(e.LegacyName,   containsFilter) ||
                    Contains(e.LabeledByName,containsFilter) ||
                    Contains(e.AutomationId, containsFilter) ||
                    Contains(e.ClassName,    containsFilter) ||
                    Contains(e.Path,         containsFilter));
            }

            if (roleFilter != null)
                filtered = filtered.Where(e =>
                    e.Role.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));

            // Default (no explicit filter): show elements that carry some content or are walker-included.
            if (containsFilter == null && roleFilter == null)
            {
                filtered = all.Where(e =>
                    e.IncludedByWalker                           ||
                    !string.IsNullOrEmpty(e.Name)               ||
                    !string.IsNullOrEmpty(e.AutomationId)       ||
                    !string.IsNullOrEmpty(e.HelpText)           ||
                    !string.IsNullOrEmpty(e.LegacyName)         ||
                    !string.IsNullOrEmpty(e.LabeledByName));
            }

            return filtered.ToList();
        }
        finally
        {
            cache?.Dispose();
        }
    }

    // ── Internal depth-first walk ─────────────────────────────────────────────

    private static void WalkFull(
        AutomationElement  e,
        List<UiaDiagnosticEntry> entries,
        bool               isBrowser,
        int                depth,
        string             parentPath,
        ref int            counter)
    {
        if (entries.Count >= MaxDiagElements || depth > MaxDiagDepth) return;

        try
        {
            counter++;

            var role  = UiaTreeWalker.SafeRole(e);
            var name  = UiaTreeWalker.SafeName(e);
            var rid   = UiaTreeWalker.SafeRuntimeId(e);
            var id    = UiaTreeWalker.SafeId(e);
            var cls   = UiaTreeWalker.SafeClass(e);
            var help  = UiaTreeWalker.SafeHelpText(e);
            var val   = UiaTreeWalker.SafeValue(e);
            var legN  = UiaTreeWalker.SafeLegacyName(e);
            var legV  = UiaTreeWalker.SafeLegacyValue(e);
            var labN  = UiaTreeWalker.SafeLabeledByName(e);
            var off   = UiaTreeWalker.SafeOffscreen(e);
            var ena   = UiaTreeWalker.SafeEnabled(e);

            int left = 0, top = 0, right = 0, bottom = 0;
            try
            {
                var b = e.BoundingRectangle;
                left  = (int)b.Left;
                top   = (int)b.Top;
                right = (int)b.Right;
                bottom= (int)b.Bottom;
            }
            catch { }

            var patterns = new List<string>(6);
            try
            {
                if (e.Patterns.Invoke.IsSupported)           patterns.Add("Invoke");
                if (e.Patterns.Value.IsSupported)             patterns.Add("Value");
                if (e.Patterns.Toggle.IsSupported)            patterns.Add("Toggle");
                if (e.Patterns.SelectionItem.IsSupported)     patterns.Add("Selection");
                if (e.Patterns.LegacyIAccessible.IsSupported) patterns.Add("LegacyIAccessible");
                if (e.Patterns.Text.IsSupported)              patterns.Add("Text");
            }
            catch { }

            var label   = string.IsNullOrEmpty(name) ? (string.IsNullOrEmpty(role) ? "?" : role) : $"{role}:{name}";
            var curPath = depth == 0 ? label : $"{parentPath} > {label}";

            entries.Add(new UiaDiagnosticEntry(
                Index:           counter,
                Depth:           depth,
                Path:            curPath,
                Role:            role,
                Name:            name,
                RuntimeId:       rid,
                AutomationId:    id,
                ClassName:       cls,
                HelpText:        help,
                Value:           val,
                LegacyName:      legN,
                LegacyValue:     legV,
                LabeledByName:   labN,
                BoundsLeft:      left,
                BoundsTop:       top,
                BoundsRight:     right,
                BoundsBottom:    bottom,
                BoundsAreZero:   left == 0 && top == 0 && right == 0 && bottom == 0,
                IsOffscreen:     off,
                IsEnabled:       ena,
                Patterns:        patterns,
                IncludedByWalker: UiaTreeWalker.WouldInclude(e, isBrowser)));

            foreach (var child in UiaTreeWalker.SafeChildren(e))
                WalkFull(child, entries, isBrowser, depth + 1, curPath, ref counter);
        }
        catch { }
    }

    private static bool Contains(string? haystack, string needle)
        => !string.IsNullOrEmpty(haystack) &&
           haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
