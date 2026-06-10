using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using OneBrain.Core.Actions;

namespace OneBrain.Actions.Uia;

public sealed class UiaActionExecutor
{
    private const int MaxElements = 200;
    private const int MaxDepth = 8;

    public ActionResult Execute(ActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetRef))
        {
            return new ActionResult(false, "Target ref is required.");
        }

        using var automation = new UIA3Automation();

        AutomationElement? focused;

        try
        {
            focused = automation.FocusedElement();
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Could not get focused element: {ex.Message}");
        }

        if (focused is null)
        {
            return new ActionResult(false, "No focused element.");
        }

        var root = FindContainingWindow(focused) ?? focused;

        var elements = new List<AutomationElement>(capacity: 64);
        Walk(root, elements, 0);

        var index = ParseRefIndex(request.TargetRef);

        if (index < 1 || index > elements.Count)
        {
            return new ActionResult(false, $"Target ref {request.TargetRef} not found. Elements available: {elements.Count}.");
        }

        var target = elements[index - 1];

        return request.Kind.ToLowerInvariant() switch
        {
            "type" or "type_text" => TypeText(target, request.Text ?? string.Empty),
            "invoke" => Invoke(target),
            "focus" => Focus(target),
            _ => new ActionResult(false, $"Unsupported action kind: {request.Kind}")
        };
    }

    private static ActionResult TypeText(AutomationElement target, string text)
    {
        try
        {
            target.Focus();
            Keyboard.Type(text);
            return new ActionResult(true, $"Typed {text.Length} characters.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Type failed: {ex.Message}");
        }
    }

    private static ActionResult Focus(AutomationElement target)
    {
        try
        {
            target.Focus();
            return new ActionResult(true, "Focused target.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Focus failed: {ex.Message}");
        }
    }

    private static ActionResult Invoke(AutomationElement target)
    {
        try
        {
            target.Focus();

            if (target.ControlType == ControlType.Button)
            {
                target.AsButton().Invoke();
                return new ActionResult(true, "Invoked button.");
            }

            target.Click();
            return new ActionResult(true, "Clicked target as invoke fallback.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Invoke failed: {ex.Message}");
        }
    }

    private static AutomationElement? FindContainingWindow(AutomationElement element)
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

    private static void Walk(AutomationElement element, List<AutomationElement> results, int depth)
    {
        if (results.Count >= MaxElements || depth > MaxDepth)
        {
            return;
        }

        try
        {
            if (ShouldInclude(element))
            {
                results.Add(element);
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
        try
        {
            if (element.IsOffscreen)
            {
                return false;
            }

            var hasIdentity =
                !string.IsNullOrWhiteSpace(element.Name) ||
                !string.IsNullOrWhiteSpace(element.AutomationId) ||
                !string.IsNullOrWhiteSpace(element.ClassName);

            return hasIdentity;
        }
        catch
        {
            return false;
        }
    }

    private static int ParseRefIndex(string targetRef)
    {
        if (!targetRef.StartsWith("@e", StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        return int.TryParse(targetRef[2..], out var index) ? index : -1;
    }
}
