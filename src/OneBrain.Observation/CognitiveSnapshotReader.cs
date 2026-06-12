using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Core.Models;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation;

public sealed class CognitiveSnapshotReader
{
    private readonly WindowFinder           _windowFinder  = new();
    private readonly ForegroundWindowReader  _windowReader  = new();
    private readonly UiaElementReader        _elementReader = new();

    public CognitiveSnapshot? Read(string? processName = null, string? windowTitle = null)
    {
        using var automation = new UIA3Automation();

        var hwnd = IntPtr.Zero;

        if (!string.IsNullOrEmpty(processName) || !string.IsNullOrEmpty(windowTitle))
        {
            hwnd = _windowFinder.FindWindow(processName, windowTitle);
        }
        else
        {
            hwnd = ForegroundWindowReader.GetForegroundWindow();
        }

        if (hwnd == IntPtr.Zero) return null;

        return ReadCore(automation, hwnd, processName);
    }

    /// <summary>Read snapshot from a specific HWND. Bypasses process/title search.</summary>
    public CognitiveSnapshot? ReadFromHwnd(IntPtr hwnd, string? processName = null)
    {
        if (hwnd == IntPtr.Zero) return null;
        using var automation = new UIA3Automation();
        return ReadCore(automation, hwnd, processName);
    }

    private CognitiveSnapshot? ReadCore(UIA3Automation automation, IntPtr hwnd, string? processName)
    {
        var window = _windowReader.ReadFromHandle(hwnd);
        if (window is null) return null;

        // Use browser-optimised limits and role set for known browser processes.
        var effectiveProcess = processName ?? window.ProcessName;
        bool isBrowser       = UiaTreeWalker.IsBrowserProcess(effectiveProcess);
        var maxElements      = isBrowser ? UiaTreeWalker.BrowserMaxElements : UiaTreeWalker.DefaultMaxElements;
        var alwaysInclude    = isBrowser ? UiaTreeWalker.BrowserRelevantRoles : null;

        var (elements, truncated) = _elementReader.ReadFromHandleDetailed(
            automation, hwnd, maxElements, alwaysInclude, relaxOffscreen: isBrowser);
        return new CognitiveSnapshot(window, elements, truncated);
    }
}
