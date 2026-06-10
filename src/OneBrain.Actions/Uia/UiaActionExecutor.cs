using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using OneBrain.Core.Actions;
using OneBrain.Safety.Policies;
using OneBrain.Observation.Windows;

namespace OneBrain.Actions.Uia;

public sealed class UiaActionExecutor
{
    private readonly MinimalSafetyGuard _safetyGuard = new();
    private readonly WindowFinder _windowFinder = new();

    public ActionResult Execute(ActionRequest request)
    {
        using var automation = new UIA3Automation();

        AutomationElement? root = null;

        if (!string.IsNullOrEmpty(request.ProcessName) || !string.IsNullOrEmpty(request.WindowTitle))
        {
            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitle);

            if (hwnd != IntPtr.Zero)
            {
                _windowFinder.Activate(hwnd);
                root = automation.FromHandle(hwnd);
            }
        }

        if (root == null)
        {
            var focused = automation.FocusedElement();

            if (focused == null)
            {
                return new ActionResult(false, "No window found or focused.");
            }

            root = focused;

            while (root.Parent != null && root.ControlType != ControlType.Window)
            {
                root = root.Parent;
            }
        }

        var elements = new List<AutomationElement>();
        Walk(root, elements, 0);

        var target = ResolveTarget(request.TargetRef, elements);

        if (target is null)
        {
            return new ActionResult(false, $"Target '{request.TargetRef}' not found.");
        }

        var decision = _safetyGuard.Evaluate(
            request.Kind,
            target.ControlType.ToString(),
            target.Name ?? "",
            target.AutomationId ?? "",
            target.ClassName ?? "");

        if (!decision.Allowed)
        {
            return new ActionResult(false, decision.Reason);
        }

        try
        {
            target.Focus();

            if (request.Kind.ToLowerInvariant() == "type")
            {
                Keyboard.Type(request.Text ?? "");
                return new ActionResult(true, "Typed text.");
            }

            if (request.Kind.ToLowerInvariant() == "focus")
            {
                return new ActionResult(true, "Focused target.");
            }

            if (target.ControlType == ControlType.Button)
            {
                target.AsButton().Invoke();
            }
            else
            {
                target.Click();
            }

            return new ActionResult(true, "Action executed.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, ex.Message);
        }
    }

    private AutomationElement? ResolveTarget(string selector, List<AutomationElement> elements)
    {
        if (selector.StartsWith("@e", StringComparison.OrdinalIgnoreCase) && int.TryParse(selector[2..], out var idx))
        {
            return idx >= 1 && idx <= elements.Count ? elements[idx - 1] : null;
        }

        var parts = selector.Split(':', 2);

        if (parts.Length < 2)
        {
            return null;
        }

        var kind = parts[0].ToLowerInvariant();
        var val = parts[1];

        return kind switch
        {
            "role" => elements.FirstOrDefault(e => e.ControlType.ToString().Equals(val, StringComparison.OrdinalIgnoreCase)),
            "name" => elements.FirstOrDefault(e => (e.Name ?? "").Contains(val, StringComparison.OrdinalIgnoreCase)),
            "automation-id" => elements.FirstOrDefault(e => (e.AutomationId ?? "").Equals(val, StringComparison.OrdinalIgnoreCase)),
            "class" => elements.FirstOrDefault(e => (e.ClassName ?? "").Equals(val, StringComparison.OrdinalIgnoreCase)),
            _ => null
        };
    }

    private void Walk(AutomationElement e, List<AutomationElement> res, int d)
    {
        if (res.Count >= 250 || d > 10)
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
}
