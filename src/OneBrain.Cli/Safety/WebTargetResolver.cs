using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Observation.Windows;

namespace OneBrain.Cli.Safety;

/// <summary>Iterates ALL browser HWNDs to find web page content elements.</summary>
public static class WebTargetResolver
{
    public static WebTargetResult Resolve(IntPtr sessionHwnd, string targetText, string processName = "msedge", int maxDescendants = 500)
    {
        if (string.IsNullOrWhiteSpace(targetText) || sessionHwnd == IntPtr.Zero)
            return new WebTargetResult { Found = false, Reason = "invalid input" };

        var finder = new WindowFinder();
        var allHwnds = finder.FindAllWindows(processName, null);
        if (allHwnds.Count == 0)
            return new WebTargetResult { Found = false, Reason = "no browser windows" };

        var allCandidates = new List<CandidateInfo>();
        var normalized = targetText.Trim();

        foreach (var hwnd in allHwnds)
        {
            try
            {
                using var automation = new UIA3Automation();
                var root = automation.FromHandle(hwnd);
                if (root == null) continue;
                foreach (var el in root.FindAllDescendants().Take(maxDescendants))
                {
                    var name = el.Name ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (!name.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase) &&
                        !name.Contains(targetText, StringComparison.OrdinalIgnoreCase)) continue;

                    allCandidates.Add(new CandidateInfo
                    {
                        Name = name, ControlType = el.ControlType.ToString(),
                        AutomationId = el.AutomationId ?? "", IsEnabled = el.IsEnabled,
                        IsOffscreen = el.IsOffscreen, BoundingRect = $"{el.BoundingRectangle.Left},{el.BoundingRectangle.Top},{el.BoundingRectangle.Width},{el.BoundingRectangle.Height}",
                        Hwnd = hwnd, HasInvoke = SafePattern(el, e => e.Patterns.Invoke.IsSupported),
                        HasClickablePoint = SafeClickable(el)
                    });
                }
            }
            catch { }
        }

        if (allCandidates.Count == 0)
            return new WebTargetResult { Reason = $"not found in {allHwnds.Count} windows", WindowsSearched = allHwnds.Count };

        if (allCandidates.Count > 1)
        {
            var good = allCandidates.Where(c => c.IsEnabled && !c.IsOffscreen).ToList();
            if (good.Count == 1) return BuildSuccess(good[0], allCandidates, allHwnds.Count);
            return new WebTargetResult { CandidateCount = allCandidates.Count, Reason = $"ambiguous: {allCandidates.Count}", CandidatesJson = JsonSerializer.Serialize(allCandidates.Take(5)), WindowsSearched = allHwnds.Count };
        }

        return BuildSuccess(allCandidates[0], allCandidates, allHwnds.Count);
    }

    private static WebTargetResult BuildSuccess(CandidateInfo c, List<CandidateInfo> all, int windows)
    {
        if (!c.IsEnabled || c.IsOffscreen || (!c.HasInvoke && !c.HasClickablePoint))
            return new WebTargetResult { CandidateCount = 1, Reason = "target not actionable", CandidatesJson = JsonSerializer.Serialize(all.Take(5)), WindowsSearched = windows };
        return new WebTargetResult { Found = true, CandidateCount = 1, SelectedName = c.Name, SelectedControlType = c.ControlType, SelectedHwnd = c.Hwnd.ToString(), HasInvoke = c.HasInvoke, HasClickablePoint = c.HasClickablePoint, Reason = "exact match", WindowsSearched = windows };
    }

    private static bool SafePattern(AutomationElement el, Func<AutomationElement, bool> check) { try { return check(el); } catch { return false; } }
    private static bool SafeClickable(AutomationElement el) { try { var p = el.GetClickablePoint(); return p.X > 0 || p.Y > 0; } catch { return false; } }
}

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
