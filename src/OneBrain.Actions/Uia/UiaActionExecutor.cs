using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using OneBrain.Core.Actions;
using OneBrain.Safety.Policies;
using OneBrain.Observation.Windows;
using OneBrain.Observation.Uia;

namespace OneBrain.Actions.Uia;

public sealed class UiaActionExecutor
{
    private readonly MinimalSafetyGuard _safetyGuard  = new();
    private readonly WindowFinder        _windowFinder = new();

    // Convenience aliases — all property access goes through UiaTreeWalker helpers.
    private static string SafeRole(AutomationElement e) => UiaTreeWalker.SafeRole(e);
    private static string SafeName(AutomationElement e) => UiaTreeWalker.SafeName(e);
    private static string SafeId(AutomationElement e)   => UiaTreeWalker.SafeId(e);
    private static string SafeClass(AutomationElement e) => UiaTreeWalker.SafeClass(e);

    // ── Public entry point ──────────────────────────────────────────────────
    public ActionResult Execute(ActionRequest request)
    {
        try   { return ExecuteCore(request); }
        catch (Exception ex) { return new ActionResult(false, $"Executor error: {ex.Message}"); }
    }

    // ── Core logic ──────────────────────────────────────────────────────────
    private ActionResult ExecuteCore(ActionRequest request)
    {
        using var automation = new UIA3Automation();

        AutomationElement? root = null;

        if (!string.IsNullOrEmpty(request.ProcessName) || !string.IsNullOrEmpty(request.WindowTitle))
        {
            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitle);

            if (hwnd == IntPtr.Zero)
            {
                var desc = request.ProcessName ?? request.WindowTitle;
                return new ActionResult(false, $"Process/window '{desc}' not found or not visible.");
            }

            _windowFinder.Activate(hwnd);
            try { root = automation.FromHandle(hwnd); } catch { }
        }

        if (root == null)
        {
            AutomationElement? focused = null;
            try { focused = automation.FocusedElement(); } catch { }

            if (focused == null)
                return new ActionResult(false, "No window found or focused.");

            root = focused;

            try
            {
                while (root.Parent != null && SafeRole(root) != "Window")
                    root = root.Parent;
            }
            catch { }
        }

        var elements = new List<AutomationElement>();
        bool truncated = UiaTreeWalker.Walk(root, elements);

        var target = ResolveTarget(request.TargetRef, elements);

        if (target is null)
        {
            var kind = request.Kind.ToLowerInvariant();
            if ((kind == "type" || kind == "type_text") &&
                (!string.IsNullOrEmpty(request.ProcessName) || !string.IsNullOrEmpty(request.WindowTitle)))
            {
                return TypeWithFallback(request, truncated);
            }

            var truncatedNote = truncated
                ? " (UIA tree was capped — try a more specific selector)"
                : "";
            return new ActionResult(false, $"Target '{request.TargetRef}' not found.{truncatedNote}");
        }

        var decision = _safetyGuard.Evaluate(
            request.Kind,
            SafeRole(target),
            SafeName(target),
            SafeId(target),
            SafeClass(target));

        if (!decision.Allowed)
            return new ActionResult(false, decision.Reason);

        try
        {
            target.Focus();

            var kind = request.Kind.ToLowerInvariant();

            if (kind == "type" || kind == "type_text")
            {
                Keyboard.Type(request.Text ?? "");
                return new ActionResult(true, "Typed text.");
            }

            if (kind == "focus")
                return new ActionResult(true, "Focused target.");

            if (SafeRole(target) == "Button")
                target.AsButton().Invoke();
            else
                target.Click();

            return new ActionResult(true, "Action executed.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, ex.Message);
        }
    }

    // ── Focused-window fallback for type when UIA tree does not expose target ─
    private ActionResult TypeWithFallback(ActionRequest request, bool treeWasTruncated)
    {
        try
        {
            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitle);

            if (hwnd == IntPtr.Zero)
            {
                return new ActionResult(false,
                    $"Target '{request.TargetRef}' not found and fallback window not found.");
            }

            _windowFinder.Activate(hwnd);
            System.Threading.Thread.Sleep(300);
            Keyboard.Type(request.Text ?? "");

            var note = treeWasTruncated
                ? "Typed text using focused-window fallback. (UIA tree was capped)"
                : "Typed text using focused-window fallback.";
            return new ActionResult(true, note, UsedFallback: true);
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
        if (parts.Length < 2) return null;

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
}
