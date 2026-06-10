using FlaUI.Core.AutomationElements;
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

    // ── Safe FlaUI property helpers ──────────────────────────────────────────
    // Every FlaUI property access can throw PropertyNotSupportedException.
    // These helpers guarantee a safe default is returned instead.

    private static string SafeRole(AutomationElement e)
    {
        try { return e.ControlType.ToString(); } catch { return ""; }
    }

    private static string SafeName(AutomationElement e)
    {
        try { return e.Name ?? ""; } catch { return ""; }
    }

    private static string SafeId(AutomationElement e)
    {
        try { return e.AutomationId ?? ""; } catch { return ""; }
    }

    private static string SafeClass(AutomationElement e)
    {
        try { return e.ClassName ?? ""; } catch { return ""; }
    }

    private static bool SafeOffscreen(AutomationElement e)
    {
        try { return e.IsOffscreen; } catch { return true; }
    }

    private static IEnumerable<AutomationElement> SafeChildren(AutomationElement e)
    {
        try { return e.FindAllChildren(); } catch { return []; }
    }

    // ── Roles that are always included even when Name and AutomationId are empty ─
    private static readonly HashSet<string> AlwaysIncludeRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Document", "Edit" };

    // ── Public entry point ──────────────────────────────────────────────────
    public ActionResult Execute(ActionRequest request)
    {
        try
        {
            return ExecuteCore(request);
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Executor error: {ex.Message}");
        }
    }

    // ── Core logic ──────────────────────────────────────────────────────────
    private ActionResult ExecuteCore(ActionRequest request)
    {
        using var automation = new UIA3Automation();

        AutomationElement? root = null;

        if (!string.IsNullOrEmpty(request.ProcessName) || !string.IsNullOrEmpty(request.WindowTitle))
        {
            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitle);

            if (hwnd != IntPtr.Zero)
            {
                _windowFinder.Activate(hwnd);
                try { root = automation.FromHandle(hwnd); } catch { }
            }
        }

        if (root == null)
        {
            AutomationElement? focused = null;
            try { focused = automation.FocusedElement(); } catch { }

            if (focused == null)
            {
                return new ActionResult(false, "No window found or focused.");
            }

            root = focused;

            try
            {
                while (root.Parent != null && SafeRole(root) != "Window")
                {
                    root = root.Parent;
                }
            }
            catch
            {
            }
        }

        var elements = new List<AutomationElement>();
        Walk(root, elements, 0);

        var target = ResolveTarget(request.TargetRef, elements);

        if (target is null)
        {
            var k = request.Kind.ToLowerInvariant();
            if ((k == "type" || k == "type_text") &&
                (!string.IsNullOrEmpty(request.ProcessName) || !string.IsNullOrEmpty(request.WindowTitle)))
            {
                return TypeWithFallback(request);
            }

            return new ActionResult(false, $"Target '{request.TargetRef}' not found.");
        }

        var decision = _safetyGuard.Evaluate(
            request.Kind,
            SafeRole(target),
            SafeName(target),
            SafeId(target),
            SafeClass(target));

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

            if (SafeRole(target) == "Button")
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

    // ── Focused-window fallback for type when UIA tree misses the target ────
    private ActionResult TypeWithFallback(ActionRequest request)
    {
        try
        {
            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitle);

            if (hwnd == IntPtr.Zero)
            {
                return new ActionResult(false, $"Target '{request.TargetRef}' not found and fallback window not found.");
            }

            _windowFinder.Activate(hwnd);
            System.Threading.Thread.Sleep(300);
            Keyboard.Type(request.Text ?? "");

            return new ActionResult(true, "Typed text using focused-window fallback.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Fallback type failed: {ex.Message}");
        }
    }

    // ── Target resolution ───────────────────────────────────────────────────
    private AutomationElement? ResolveTarget(string selector, List<AutomationElement> elements)
    {
        if (selector.StartsWith("@e", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(selector[2..], out var idx))
        {
            return idx >= 1 && idx <= elements.Count ? elements[idx - 1] : null;
        }

        var parts = selector.Split(':', 2);

        if (parts.Length < 2)
        {
            return null;
        }

        var kind = parts[0].ToLowerInvariant();
        var val  = parts[1];

        return kind switch
        {
            "role"          => elements.FirstOrDefault(e => SafeRole(e).Equals(val, StringComparison.OrdinalIgnoreCase)),
            "name"          => elements.FirstOrDefault(e => SafeName(e).Contains(val, StringComparison.OrdinalIgnoreCase)),
            "automation-id" => elements.FirstOrDefault(e => SafeId(e).Equals(val, StringComparison.OrdinalIgnoreCase)),
            "class"         => elements.FirstOrDefault(e => SafeClass(e).Equals(val, StringComparison.OrdinalIgnoreCase)),
            _               => null
        };
    }

    // ── Element walk ────────────────────────────────────────────────────────
    private void Walk(AutomationElement e, List<AutomationElement> res, int d)
    {
        if (res.Count >= 250 || d > 20)
        {
            return;
        }

        try
        {
            var role    = SafeRole(e);
            var include = !SafeOffscreen(e) && (
                !string.IsNullOrEmpty(SafeName(e)) ||
                !string.IsNullOrEmpty(SafeId(e))   ||
                AlwaysIncludeRoles.Contains(role));

            if (include)
            {
                res.Add(e);
            }

            foreach (var child in SafeChildren(e))
            {
                Walk(child, res, d + 1);
            }
        }
        catch
        {
        }
    }
}
