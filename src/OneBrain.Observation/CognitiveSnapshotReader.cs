using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Core.Models;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation;

public sealed class CognitiveSnapshotReader
{
    private readonly WindowFinder _windowFinder = new();
    private readonly ForegroundWindowReader _windowReader = new();
    private readonly UiaElementReader _elementReader = new();

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

        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var window = _windowReader.ReadFromHandle(hwnd);

        if (window is null)
        {
            return null;
        }

        AutomationElement? root;

        try
        {
            root = automation.FromHandle(hwnd);
        }
        catch
        {
            return null;
        }

        if (root is null)
        {
            return null;
        }

        var elements = _elementReader.ReadFromRoot(root);

        return new CognitiveSnapshot(window, elements);
    }
}
