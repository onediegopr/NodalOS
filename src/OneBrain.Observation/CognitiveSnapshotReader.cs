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

        var window = hwnd == IntPtr.Zero ? _windowReader.Read() : _windowReader.ReadFromHandle(hwnd);

        if (window == null)
        {
            return null;
        }

        var root = hwnd == IntPtr.Zero ? automation.FocusedElement() : automation.FromHandle(hwnd);

        if (root == null)
        {
            return null;
        }

        var elements = _elementReader.ReadFromRoot(root);

        return new CognitiveSnapshot(window, elements);
    }
}
