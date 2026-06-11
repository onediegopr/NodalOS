using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Observation.Windows;

namespace OneBrain.Cli.Safety;

/// <summary>
/// Resolves web page UIA targets by enumerating browser HWNDs + child HWNDs.
/// Edge Chromium exposes page-content accessibility in RenderWidgetHostHWND child windows;
/// top-level HWNDs only expose window-chrome (tabs, toolbar), not in-page elements.
/// </summary>
public static class WebTargetResolver
{
    // ── P/Invoke ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr hWndParent, Delegates.EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private static class Delegates
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static WebTargetResult Resolve(IntPtr sessionHwnd, string targetText, string processName = "msedge", int maxDescendants = 500)
    {
        if (string.IsNullOrWhiteSpace(targetText) || sessionHwnd == IntPtr.Zero)
            return new WebTargetResult { Found = false, Reason = "invalid input" };

        // Collect all HWNDs: top-level + all child HWNDs (recursive)
        var allHwnds = new HashSet<IntPtr>();
        var finder = new WindowFinder();
        var topLevel = finder.FindAllWindows(processName, null);
        allHwnds.UnionWith(topLevel);

        var childHwnds = new List<IntPtr>();
        foreach (var hwnd in topLevel)
        {
            CollectChildHwnds(hwnd, childHwnds, 3);
        }
        allHwnds.UnionWith(childHwnds);

        // Build full diagnostic snapshot for each HWND
        var diagnostics = new List<ChildHwndDiagnostic>();
        var allCandidates = new List<CandidateInfo>();
        var normalized = targetText.Trim();

        foreach (var hwnd in allHwnds)
        {
            var diag = BuildHwndDiagnostic(hwnd, processName);
            // Skip redundant HWNDs (non-visible, non-enabled, no UIA tree)
            diag.IsTopLevel = topLevel.Contains(hwnd);

            // UIA probe
            try
            {
                using var automation = new UIA3Automation();
                var root = automation.FromHandle(hwnd);
                if (root != null)
                {
                    diag.UiaRootAvailable = true;
                    var descendants = new List<AutomationElement>();
                    descendants.Add(root); // include root itself
                    WalkUiaTreeRecursive(root, descendants, maxDescendants, 0, 30);
                    diag.DescendantCount = descendants.Count;
                    diag.HyperlinkCount = descendants.Count(e => e.ControlType.ToString() == "Hyperlink");
                    diag.ButtonCount = descendants.Count(e => e.ControlType.ToString() == "Button");

                    foreach (var el in descendants)
                    {
                        string name;
                        try { name = el.Name ?? ""; } catch { name = ""; }
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (name.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                            name.Contains(targetText, StringComparison.OrdinalIgnoreCase))
                        {
                            diag.CandidateNames.Add(name);
                            allCandidates.Add(new CandidateInfo
                            {
                                Name = name, ControlType = el.ControlType.ToString(),
                                AutomationId = el.AutomationId ?? "", IsEnabled = el.IsEnabled,
                                IsOffscreen = el.IsOffscreen,
                                BoundingRect = $"{el.BoundingRectangle.Left},{el.BoundingRectangle.Top},{el.BoundingRectangle.Width},{el.BoundingRectangle.Height}",
                                Hwnd = hwnd, HasInvoke = SafePattern(el, e => e.Patterns.Invoke.IsSupported),
                                HasClickablePoint = SafeClickable(el)
                            });
                        }
                    }
                }
            }
            catch { }
            diagnostics.Add(diag);
        }

        if (allCandidates.Count == 0)
            return new WebTargetResult
            {
                Reason = $"not found in {allHwnds.Count} windows",
                WindowsSearched = allHwnds.Count,
                ChildHwndDiagnostics = diagnostics.Select(d => d.ToSummary()).ToList()
            };

        if (allCandidates.Count > 1)
        {
            var good = allCandidates.Where(c => c.IsEnabled && !c.IsOffscreen).ToList();
            if (good.Count == 1) return BuildSuccess(good[0], allCandidates, allHwnds.Count, diagnostics);
            return new WebTargetResult
            {
                CandidateCount = allCandidates.Count,
                Reason = $"ambiguous: {allCandidates.Count}",
                CandidatesJson = JsonSerializer.Serialize(allCandidates.Take(5)),
                WindowsSearched = allHwnds.Count,
                ChildHwndDiagnostics = diagnostics.Select(d => d.ToSummary()).ToList()
            };
        }

        return BuildSuccess(allCandidates[0], allCandidates, allHwnds.Count, diagnostics);
    }

    // ── Child HWND enumeration (recursive) ────────────────────────────────────

    private static void CollectChildHwnds(IntPtr parent, List<IntPtr> results, int maxDepth)
    {
        if (maxDepth <= 0) return;
        var children = new List<IntPtr>();
        EnumChildWindows(parent, (child, _) => { children.Add(child); return true; }, IntPtr.Zero);
        foreach (var child in children)
        {
            results.Add(child);
            CollectChildHwnds(child, results, maxDepth - 1);
        }
    }

    // ── HWND diagnostic snapshot ──────────────────────────────────────────────

    private static ChildHwndDiagnostic BuildHwndDiagnostic(IntPtr hwnd, string processName)
    {
        var diag = new ChildHwndDiagnostic { Hwnd = hwnd, HwndHex = $"0x{hwnd.ToInt64():X}" };

        // Class name
        var cn = new StringBuilder(256);
        if (GetClassName(hwnd, cn, cn.Capacity) > 0)
            diag.ClassName = cn.ToString();

        // Title
        var sb = new StringBuilder(256);
        if (GetWindowText(hwnd, sb, sb.Capacity) > 0)
            diag.Title = sb.ToString();

        // Rect
        if (GetWindowRect(hwnd, out var r))
            diag.Rect = new RectInfo { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };

        // Visibility
        diag.IsVisible = IsWindowVisible(hwnd);
        diag.IsEnabled = IsWindowEnabled(hwnd);

        // Process/Thread ID
        uint pid;
        if (GetWindowThreadProcessId(hwnd, out pid) > 0)
            diag.ProcessId = pid;

        return diag;
    }

    // ── Build success result ─────────────────────────────────────────────────

    private static WebTargetResult BuildSuccess(CandidateInfo c, List<CandidateInfo> all, int windows, List<ChildHwndDiagnostic> diagnostics)
    {
        if (!c.IsEnabled || c.IsOffscreen || (!c.HasInvoke && !c.HasClickablePoint))
            return new WebTargetResult
            {
                CandidateCount = 1, Reason = "target not actionable",
                CandidatesJson = JsonSerializer.Serialize(all.Take(5)), WindowsSearched = windows,
                ChildHwndDiagnostics = diagnostics.Select(d => d.ToSummary()).ToList()
            };
        return new WebTargetResult
        {
            Found = true, CandidateCount = 1,
            SelectedName = c.Name, SelectedControlType = c.ControlType,
            SelectedHwnd = c.Hwnd.ToString(), HasInvoke = c.HasInvoke,
            HasClickablePoint = c.HasClickablePoint, Reason = "exact match",
            WindowsSearched = windows,
            ChildHwndDiagnostics = diagnostics.Select(d => d.ToSummary()).ToList()
        };
    }

    // ── Safe wrappers ────────────────────────────────────────────────────────

    private static void WalkUiaTreeRecursive(AutomationElement parent, List<AutomationElement> results, int max, int depth, int maxDepth)
    {
        if (results.Count >= max || depth > maxDepth) return;
        try
        {
            var children = new List<AutomationElement>();
            try { children.AddRange(parent.FindAllChildren()); } catch { }

            foreach (var child in children)
            {
                if (results.Count >= max) return;
                results.Add(child);
                WalkUiaTreeRecursive(child, results, max, depth + 1, maxDepth);
            }
        }
        catch { }
    }

    private static bool SafePattern(AutomationElement el, Func<AutomationElement, bool> check) { try { return check(el); } catch { return false; } }
    private static bool SafeClickable(AutomationElement el) { try { var p = el.GetClickablePoint(); return p.X > 0 || p.Y > 0; } catch { return false; } }

    /// <summary>Find a UIA element by name using recursive tree walk (finds content deep inside Document panes).</summary>
    public static AutomationElement? FindElementByName(IntPtr hwnd, string nameContains, int max = 2000)
    {
        try
        {
            using var automation = new UIA3Automation();
            var root = automation.FromHandle(hwnd);
            if (root == null) return null;

            var candidates = new List<AutomationElement>();
            candidates.Add(root);
            WalkUiaTreeRecursive(root, candidates, max, 0, 30);
            return candidates.FirstOrDefault(e =>
            {
                try { return (e.Name ?? "").Contains(nameContains, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            });
        }
        catch { return null; }
    }
}

// ── Result types ──────────────────────────────────────────────────────────────

public sealed class WebTargetResult
{
    public bool Found { get; init; }
    public int CandidateCount { get; init; }
    public int WindowsSearched { get; init; }
    public string? SelectedName { get; init; }
    public string? SelectedControlType { get; init; }
    public string? SelectedHwnd { get; init; }
    public bool HasInvoke { get; init; }
    public bool HasClickablePoint { get; init; }
    public string? CandidatesJson { get; init; }
    public string Reason { get; init; } = "";
    /// <summary>Per-HWND diagnostic summary (for recipe variable exposure).</summary>
    public IReadOnlyList<string> ChildHwndDiagnostics { get; init; } = Array.Empty<string>();
}

public sealed class CandidateInfo
{
    public string Name { get; init; } = "";
    public string ControlType { get; init; } = "";
    public string AutomationId { get; init; } = "";
    public bool IsEnabled { get; init; }
    public bool IsOffscreen { get; init; }
    public bool HasInvoke { get; init; }
    public bool HasClickablePoint { get; init; }
    public string BoundingRect { get; init; } = "";
    public IntPtr Hwnd { get; init; }
}

/// <summary>Per-HWND diagnostic snapshot during child-HWND enumeration.</summary>
public sealed class ChildHwndDiagnostic
{
    public IntPtr Hwnd { get; set; }
    public string HwndHex { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Title { get; set; } = "";
    public RectInfo? Rect { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsTopLevel { get; set; }
    public uint ProcessId { get; set; }
    public bool UiaRootAvailable { get; set; }
    public int DescendantCount { get; set; }
    public int HyperlinkCount { get; set; }
    public int ButtonCount { get; set; }
    public List<string> CandidateNames { get; set; } = new();

    public string ToSummary()
    {
        var vis = IsVisible ? "VIS" : "HID";
        var ena = IsEnabled ? "ENA" : "DIS";
        var lvl = IsTopLevel ? "TOP" : "CHD";
        var cls = string.IsNullOrEmpty(ClassName) ? "?" : ClassName;
        var title = string.IsNullOrEmpty(Title) ? "" : $" title='{Title}'";
        var rect = Rect is { } r ? $" rect=({r.Left},{r.Top},{r.Right - r.Left}x{r.Bottom - r.Top})" : "";
        var uia = UiaRootAvailable ? $" UIA=root desc={DescendantCount} hl={HyperlinkCount} btn={ButtonCount}" : " UIA=none";
        var cands = CandidateNames.Count > 0 ? $" candidates={string.Join(",", CandidateNames)}" : "";
        return $"{HwndHex} [{lvl}] [{vis}/{ena}] cls='{cls}'{title}{rect}{uia}{cands}";
    }
}

public sealed class RectInfo
{
    public int Left { get; init; }
    public int Top { get; init; }
    public int Right { get; init; }
    public int Bottom { get; init; }
}
