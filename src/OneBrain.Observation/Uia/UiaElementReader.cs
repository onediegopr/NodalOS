using System.Reflection;
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
                var role = SafeControlType(element);
                var patterns = GetPatternNames(element);
                var actions = DeriveActions(role, patterns);

                results.Add(new UiElementSnapshot(
                    Ref: $"@e{index}",
                    Role: role,
                    Name: SafeString(() => element.Name),
                    AutomationId: SafeString(() => element.AutomationId),
                    ClassName: SafeString(() => element.ClassName),
                    Bounds: ToBounds(element),
                    IsEnabled: SafeBool(() => element.IsEnabled),
                    IsOffscreen: SafeBool(() => element.IsOffscreen),
                    IsKeyboardFocusable: false,
                    Patterns: patterns,
                    Actions: actions));
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

    private static IReadOnlyList<string> GetPatternNames(AutomationElement element)
    {
        var names = new[]
        {
            "Invoke",
            "Value",
            "Text",
            "Selection",
            "SelectionItem",
            "Toggle",
            "ExpandCollapse",
            "Scroll",
            "RangeValue",
            "Window",
            "Grid",
            "GridItem",
            "Table",
            "TableItem",
            "Dock",
            "Transform"
        };

        var found = new List<string>();

        object? patternsRoot;

        try
        {
            patternsRoot = element.Patterns;
        }
        catch
        {
            return found;
        }

        if (patternsRoot is null)
        {
            return found;
        }

        var rootType = patternsRoot.GetType();

        foreach (var name in names)
        {
            try
            {
                var property = rootType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

                if (property is null)
                {
                    continue;
                }

                var patternAccessor = property.GetValue(patternsRoot);

                if (patternAccessor is null)
                {
                    continue;
                }

                var isSupportedProperty = patternAccessor.GetType().GetProperty("IsSupported", BindingFlags.Instance | BindingFlags.Public);

                if (isSupportedProperty?.GetValue(patternAccessor) is true)
                {
                    found.Add(name);
                }
            }
            catch
            {
                // Some UIA providers or FlaUI accessors can throw.
            }
        }

        return found;
    }

    private static IReadOnlyList<string> DeriveActions(string role, IReadOnlyList<string> patterns)
    {
        var actions = new List<string>();

        if (patterns.Contains("Invoke"))
        {
            actions.Add("invoke");
        }

        if (patterns.Contains("Value"))
        {
            actions.Add("set_value");
            actions.Add("read_value");
        }

        if (patterns.Contains("Text"))
        {
            actions.Add("read_text");
        }

        if (patterns.Contains("Toggle"))
        {
            actions.Add("toggle");
        }

        if (patterns.Contains("Selection") || patterns.Contains("SelectionItem"))
        {
            actions.Add("select");
        }

        if (patterns.Contains("ExpandCollapse"))
        {
            actions.Add("expand_collapse");
        }

        if (patterns.Contains("Scroll"))
        {
            actions.Add("scroll");
        }

        if (patterns.Contains("RangeValue"))
        {
            actions.Add("set_range_value");
        }

        if (role is "Window")
        {
            actions.Add("focus_window");
        }

        if (role is "Edit" or "Document")
        {
            if (!actions.Contains("type_text"))
            {
                actions.Add("type_text");
            }
        }

        if (role is "Button" && !actions.Contains("invoke"))
        {
            actions.Add("invoke_candidate");
        }

        return actions;
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
