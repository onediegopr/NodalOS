using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using OneBrain.Core.Models;

namespace OneBrain.Observation.Uia;

public sealed class UiaElementReader
{
    private const int MaxElements = 200;
    private const int MaxDepth = 8;

    public IReadOnlyList<UiElementSnapshot> ReadForegroundWindowElements()
    {
        using var automation = new UIA3Automation();

        AutomationElement? focused = null;

        try
        {
            focused = automation.FocusedElement();
        }
        catch
        {
            // UIA can fail if the focused element disappears.
        }

        if (focused is null)
        {
            return Array.Empty<UiElementSnapshot>();
        }

        var root = TryFindContainingWindow(focused) ?? focused;

        var results = new List<UiElementSnapshot>(capacity: 64);
        Walk(root, results, 0);

        return results;
    }

    private static AutomationElement? TryFindContainingWindow(AutomationElement element)
    {
        try
        {
            var current = element;

            for (var i = 0; i < 20; i++)
            {
                if (current.ControlType == ControlType.Window)
                {
                    return current;
                }

                var parent = current.Parent;

                if (parent is null)
                {
                    return null;
                }

                current = parent;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static void Walk(AutomationElement element, List<UiElementSnapshot> results, int depth)
    {
        if (results.Count >= MaxElements || depth > MaxDepth)
        {
            return;
        }

        try
        {
            if (ShouldInclude(element))
            {
                var index = results.Count + 1;

                results.Add(new UiElementSnapshot(
                    Ref: $"@e{index}",
                    Role: SafeControlType(element),
                    Name: SafeString(() => element.Name),
                    AutomationId: SafeString(() => element.AutomationId),
                    ClassName: SafeString(() => element.ClassName),
                    Bounds: ToBounds(element),
                    IsEnabled: SafeBool(() => element.IsEnabled),
                    IsOffscreen: SafeBool(() => element.IsOffscreen),
                    IsKeyboardFocusable: false,
                    Patterns: Array.Empty<string>()));
            }

            AutomationElement[] children;

            try
            {
                children = element.FindAllChildren();
            }
            catch
            {
                return;
            }

            foreach (var child in children)
            {
                if (results.Count >= MaxElements)
                {
                    break;
                }

                Walk(child, results, depth + 1);
            }
        }
        catch
        {
            // UIA providers can throw if an element disappears while walking.
        }
    }

    private static bool ShouldInclude(AutomationElement element)
    {
        var isOffscreen = SafeBool(() => element.IsOffscreen);
        var isEnabled = SafeBool(() => element.IsEnabled);
        var name = SafeString(() => element.Name);
        var automationId = SafeString(() => element.AutomationId);
        var className = SafeString(() => element.ClassName);

        if (isOffscreen)
        {
            return false;
        }

        if (!isEnabled && string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(automationId) &&
            string.IsNullOrWhiteSpace(className))
        {
            return false;
        }

        return true;
    }

    private static string SafeControlType(AutomationElement element)
    {
        try
        {
            return element.ControlType.ToString();
        }
        catch
        {
            return "Unknown";
        }
    }

    private static WindowBounds ToBounds(AutomationElement element)
    {
        try
        {
            var rect = element.BoundingRectangle;

            return new WindowBounds(
                Left: SafeInt(rect.Left),
                Top: SafeInt(rect.Top),
                Right: SafeInt(rect.Right),
                Bottom: SafeInt(rect.Bottom));
        }
        catch
        {
            return new WindowBounds(0, 0, 0, 0);
        }
    }

    private static int SafeInt(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)Math.Round(value);
    }

    private static string SafeString(Func<string?> read)
    {
        try
        {
            return read() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool SafeBool(Func<bool> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return false;
        }
    }
}
