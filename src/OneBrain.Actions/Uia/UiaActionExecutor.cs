using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using OneBrain.Core.Actions;
using OneBrain.Safety.Policies;

namespace OneBrain.Actions.Uia;

public sealed class UiaActionExecutor
{
    private const int MaxElements = 250;
    private const int MaxDepth = 10;

    private readonly MinimalSafetyGuard _safetyGuard = new();

    public ActionResult Execute(ActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetRef))
        {
            return new ActionResult(false, "Target ref or selector is required.");
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

        var elements = new List<AutomationElement>(capacity: 96);
        Walk(root, elements, 0);

        var target = ResolveTarget(request.TargetRef, elements);

        if (target is null)
        {
            return new ActionResult(false, $"Target '{request.TargetRef}' not found. Elements available: {elements.Count}.");
        }

        var decision = _safetyGuard.Evaluate(
            actionKind: request.Kind,
            role: SafeControlType(target),
            name: SafeString(() => target.Name),
            automationId: SafeString(() => target.AutomationId),
            className: SafeString(() => target.ClassName));

        if (!decision.Allowed)
        {
            return new ActionResult(false, decision.Reason);
        }

        return request.Kind.ToLowerInvariant() switch
        {
            "type" or "type_text" => TypeText(target, request.Text ?? string.Empty),
            "invoke" => Invoke(target),
            "focus" => Focus(target),
            _ => new ActionResult(false, $"Unsupported action kind: {request.Kind}")
        };
    }

    private static AutomationElement? ResolveTarget(string selector, IReadOnlyList<AutomationElement> elements)
    {
        selector = selector.Trim();

        if (selector.StartsWith("@e", StringComparison.OrdinalIgnoreCase))
        {
            var index = ParseRefIndex(selector);
            return index >= 1 && index <= elements.Count ? elements[index - 1] : null;
        }

        var separatorIndex = selector.IndexOf(':');

        if (separatorIndex <= 0 || separatorIndex >= selector.Length - 1)
        {
            return null;
        }

        var kind = selector[..separatorIndex].Trim().ToLowerInvariant();
        var value = selector[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return kind switch
        {
            "name" => FindByName(elements, value),
            "automation-id" or "automationid" => FindByAutomationId(elements, value),
            "role" => FindByRole(elements, value),
            "class" or "class-name" or "classname" => FindByClassName(elements, value),
            _ => null
        };
    }

    private static AutomationElement? FindByName(IReadOnlyList<AutomationElement> elements, string value)
    {
        var exact = elements.FirstOrDefault(e => SafeString(() => e.Name).Equals(value, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        return elements.FirstOrDefault(e => SafeString(() => e.Name).Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static AutomationElement? FindByAutomationId(IReadOnlyList<AutomationElement> elements, string value)
    {
        return elements.FirstOrDefault(e => SafeString(() => e.AutomationId).Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    private static AutomationElement? FindByRole(IReadOnlyList<AutomationElement> elements, string value)
    {
        return elements.FirstOrDefault(e => SafeControlType(e).Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    private static AutomationElement? FindByClassName(IReadOnlyList<AutomationElement> elements, string value)
    {
        return elements.FirstOrDefault(e => SafeString(() => e.ClassName).Equals(value, StringComparison.OrdinalIgnoreCase));
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
}
