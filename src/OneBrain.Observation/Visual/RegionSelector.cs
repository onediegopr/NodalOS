using System.Runtime.InteropServices;
using OneBrain.Core.Models;
using OneBrain.Core.Visual;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation.Visual;

public sealed class RegionSelector
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    private readonly WindowFinder _windowFinder = new();

    public VisualRegion? Resolve(VisualCaptureRequest request)
    {
        if (request.ManualRegion != null)
        {
            var m = request.ManualRegion;
            if (!m.IsValid)
                return null;

            return new VisualRegion(
                m.X, m.Y, m.Width, m.Height,
                "manual",
                "Manual region requested.",
                ProcessName: request.ProcessName,
                WindowTitle: request.WindowTitle);
        }

        if (request.FullScreen)
        {
            var b = GetFullScreenBounds();
            return new VisualRegion(
                b.Left, b.Top, b.Width, b.Height,
                "fullscreen",
                "Fullscreen capture requires explicit --allow-fullscreen.",
                ProcessName: request.ProcessName,
                WindowTitle: request.WindowTitle);
        }

        if (request.ProcessName == null)
            return null;

        var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitle);
        if (hwnd == IntPtr.Zero)
            return null;

        var reader = new ForegroundWindowReader();
        var winSnap = reader.ReadFromHandle(hwnd);
        if (winSnap == null)
            return null;

        if (request.Target != null)
        {
            return ResolveElementViaDiagnostic(request, winSnap);
        }

        return new VisualRegion(
            winSnap.Bounds.Left,
            winSnap.Bounds.Top,
            winSnap.Bounds.Width,
            winSnap.Bounds.Height,
            "window",
            "Captured window region only.",
            ProcessName: winSnap.ProcessName,
            WindowTitle: winSnap.Title);
    }

    private VisualRegion? ResolveElementViaDiagnostic(VisualCaptureRequest request, WindowSnapshot winSnap)
    {
        var target = request.Target!;

        var allHwnds = _windowFinder.FindAllWindows(request.ProcessName, request.WindowTitle);
        if (allHwnds.Count == 0) return null;

        var diagReader = new UiaDiagnosticReader();

        // Try every matching HWND until we find the element.
        // Browsers expose content via separate RenderWidgetHostHWND windows.
        foreach (var hwnd in allHwnds)
        {
            var entries = diagReader.ReadFromHandle(hwnd, request.ProcessName, raw: true);
            if (entries.Count == 0) continue;

            var match = FindBestMatch(entries, target);
            if (match == null) continue;

            var w = match.BoundsRight - match.BoundsLeft;
            var h = match.BoundsBottom - match.BoundsTop;
            if (w <= 0 || h <= 0) continue;

            return new VisualRegion(
                match.BoundsLeft, match.BoundsTop, w, h,
                "element",
                "Captured element region only.",
                ProcessName: winSnap.ProcessName,
                WindowTitle: winSnap.Title,
                TargetRef: $"name:{target.Name ?? target.AutomationId ?? target.Role ?? "element"}");
        }

        return null;
    }

    private static UiaDiagnosticEntry? FindBestMatch(
        IReadOnlyList<UiaDiagnosticEntry> entries,
        VisualElementTarget target)
    {
        if (target.Ref != null)
        {
            return entries.FirstOrDefault(e =>
                (e.Name?.Contains(target.Ref, StringComparison.OrdinalIgnoreCase) == true) ||
                (e.AutomationId?.Equals(target.Ref, StringComparison.OrdinalIgnoreCase) == true));
        }

        if (target.Name != null)
        {
            var candidates = entries
                .Where(e =>
                    !string.IsNullOrEmpty(e.Name) &&
                    e.Name.Contains(target.Name, StringComparison.OrdinalIgnoreCase) &&
                    !e.BoundsAreZero)
                .ToList();

            return candidates
                .OrderBy(e =>
                {
                    var r = e.Role ?? "";
                    if (r.Equals("Edit",     StringComparison.OrdinalIgnoreCase)) return 0;
                    if (r.Equals("Document", StringComparison.OrdinalIgnoreCase)) return 1;
                    if (r.Equals("Button",   StringComparison.OrdinalIgnoreCase)) return 2;
                    if (r.Equals("Text",     StringComparison.OrdinalIgnoreCase)) return 4;
                    if (r.Equals("Group",    StringComparison.OrdinalIgnoreCase)) return 5;
                    return 3;
                })
                .FirstOrDefault();
        }

        if (target.Role != null)
        {
            return entries.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.Role) &&
                e.Role.Equals(target.Role, StringComparison.OrdinalIgnoreCase) &&
                !e.BoundsAreZero);
        }

        if (target.AutomationId != null)
        {
            return entries.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.AutomationId) &&
                e.AutomationId.Equals(target.AutomationId, StringComparison.OrdinalIgnoreCase) &&
                !e.BoundsAreZero);
        }

        if (target.ClassName != null)
        {
            return entries.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.ClassName) &&
                e.ClassName.Equals(target.ClassName, StringComparison.OrdinalIgnoreCase) &&
                !e.BoundsAreZero);
        }

        return null;
    }

    private static (int Left, int Top, int Width, int Height) GetFullScreenBounds()
    {
        return (
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN)
        );
    }
}
