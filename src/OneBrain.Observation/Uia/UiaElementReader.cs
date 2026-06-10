using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Core.Models;

namespace OneBrain.Observation.Uia;

public sealed class UiaElementReader
{
    private const int MaxElements = 250;

    public IReadOnlyList<UiElementSnapshot> ReadForegroundWindowElements()
    {
        using var automation = new UIA3Automation();

        var focused = automation.FocusedElement();

        if (focused == null)
        {
            return Array.Empty<UiElementSnapshot>();
        }

        return ReadFromRoot(focused);
    }

    public IReadOnlyList<UiElementSnapshot> ReadFromRoot(AutomationElement root)
    {
        var results = new List<AutomationElement>();

        Walk(root, results, 0);

        return results.Select((e, i) => Map(e, i + 1)).ToList();
    }

    private void Walk(AutomationElement e, List<AutomationElement> res, int d)
    {
        if (res.Count >= MaxElements || d > 10)
        {
            return;
        }

        try
        {
            if (!e.IsOffscreen && (!string.IsNullOrEmpty(e.Name) || !string.IsNullOrEmpty(e.AutomationId)))
            {
                res.Add(e);
            }

            foreach (var child in e.FindAllChildren())
            {
                Walk(child, res, d + 1);
            }
        }
        catch
        {
        }
    }

    private UiElementSnapshot Map(AutomationElement e, int id)
    {
        var patterns = new List<string>();

        try
        {
            if (e.Patterns.Invoke.IsSupported) patterns.Add("Invoke");
            if (e.Patterns.Value.IsSupported) patterns.Add("Value");
            if (e.Patterns.Toggle.IsSupported) patterns.Add("Toggle");
            if (e.Patterns.SelectionItem.IsSupported) patterns.Add("Selection");
        }
        catch
        {
        }

        var actions = DeriveActions(e, patterns);

        return new UiElementSnapshot(
            Ref: $"@e{id}",
            Role: Safe(() => e.ControlType.ToString()),
            Name: Safe(() => e.Name),
            AutomationId: Safe(() => e.AutomationId),
            ClassName: Safe(() => e.ClassName),
            Bounds: MapBounds(e),
            IsEnabled: SafeBool(() => e.IsEnabled),
            IsOffscreen: SafeBool(() => e.IsOffscreen),
            IsKeyboardFocusable: false,
            Patterns: patterns,
            Actions: actions);
    }

    private static IReadOnlyList<string> DeriveActions(AutomationElement e, IReadOnlyList<string> patterns)
    {
        var actions = new List<string>();
        var role = Safe(() => e.ControlType.ToString());

        if (patterns.Contains("Invoke")) actions.Add("invoke");
        if (patterns.Contains("Value")) actions.Add("set_value");
        if (patterns.Contains("Value")) actions.Add("read_value");
        if (patterns.Contains("Toggle")) actions.Add("toggle");
        if (patterns.Contains("Selection")) actions.Add("select");

        if (role.Equals("Document", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Edit", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("type_text");
        }

        if (role.Equals("Button", StringComparison.OrdinalIgnoreCase) && !actions.Contains("invoke"))
        {
            actions.Add("invoke_candidate");
        }

        if (role.Equals("Window", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("focus_window");
        }

        return actions;
    }

    private WindowBounds MapBounds(AutomationElement e)
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

    private static string Safe(Func<string?> f)
    {
        try { return f() ?? ""; } catch { return ""; }
    }

    private static bool SafeBool(Func<bool> f)
    {
        try { return f(); } catch { return false; }
    }
}

