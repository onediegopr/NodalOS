using FlaUI.Core.WindowsAPI;
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

        bool isBrowser  = UiaTreeWalker.IsBrowserProcess(request.ProcessName ?? request.WindowTitle);
        var walkRoles   = isBrowser ? UiaTreeWalker.BrowserRelevantRoles : null;
        var walkMax     = isBrowser ? UiaTreeWalker.BrowserMaxElements : UiaTreeWalker.DefaultMaxElements;

        var elements = new List<AutomationElement>();
        bool truncated = UiaTreeWalker.Walk(root, elements, walkMax,
            UiaTreeWalker.DefaultMaxDepth, walkRoles, isBrowser);

        var target = ResolveTarget(request.TargetRef, request.Kind, elements);

        if (target is null)
        {
            var kind = request.Kind.ToLowerInvariant();
            if ((kind == "type" || kind == "type_text") &&
                (!string.IsNullOrEmpty(request.ProcessName) || !string.IsNullOrEmpty(request.WindowTitle)))
            {
                return TypeWithFallback(request, truncated);
            }

            return new ActionResult(false,
                BuildNotFoundMessage(request.TargetRef, elements, truncated, isBrowser));
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
            if (kind == "key")
            {
                if (request.Text == "Ctrl+C") { Keyboard.Type(VirtualKeyShort.KEY_C, VirtualKeyShort.CONTROL); return new ActionResult(true, "Sent Ctrl+C"); }
                if (request.Text == "Ctrl+V") { Keyboard.Type(VirtualKeyShort.KEY_V, VirtualKeyShort.CONTROL); return new ActionResult(true, "Sent Ctrl+V"); }
                if (request.Text == "Enter") { Keyboard.Press(VirtualKeyShort.ENTER); return new ActionResult(true, "Sent Enter"); }
                return new ActionResult(false, $"Unsupported key combo: {request.Text}");
            }


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

    private AutomationElement? ResolveTarget(
        string selector, string actionKind, List<AutomationElement> elements)
    {
        if (selector.StartsWith("@e", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(selector[2..], out var idx))
        {
            return idx >= 1 && idx <= elements.Count ? elements[idx - 1] : null;
        }

        var parts = selector.Split(':', 2);
        if (parts.Length < 2) return null;

        var selectorKind = parts[0].ToLowerInvariant();
        var val          = parts[1];

        return selectorKind switch
        {
            "role"          => elements.FirstOrDefault(e => SafeRole(e).Equals(val, StringComparison.OrdinalIgnoreCase)),
            // --name: collect all matching candidates, then pick best for actionKind.
            // Edge exposes label Text nodes before the actual Edit in tree order, so
            // FirstOrDefault would silently return the label instead of the input.
            "name"          => BestNameMatch(elements, val, actionKind),
            "automation-id" => elements.FirstOrDefault(e => SafeId(e).Equals(val, StringComparison.OrdinalIgnoreCase)),
            "class"         => elements.FirstOrDefault(e => SafeClass(e).Equals(val, StringComparison.OrdinalIgnoreCase)),
            _               => null
        };
    }

    private static AutomationElement? BestNameMatch(
        List<AutomationElement> elements, string val, string actionKind)
    {
        var candidates = elements.Where(e => MatchesName(e, val)).ToList();
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        var kind = actionKind.ToLowerInvariant();

        if (kind == "type" || kind == "type_text")
            return candidates.OrderBy(RankForType).First();

        if (kind == "invoke")
            return candidates.OrderBy(RankForInvoke).First();

        return candidates[0];
    }

    // Rank candidates for type/type_text: prefer Edit > Document > ValuePattern holder > other.
    // Text/StaticText label nodes get rank 99 so they are never chosen when a real input exists.
    private static int RankForType(AutomationElement e)
    {
        var role = SafeRole(e);
        if (role.Equals("Edit", StringComparison.OrdinalIgnoreCase))
        {
            try { return e.Patterns.Value.IsSupported ? 0 : 1; } catch { return 1; }
        }
        if (role.Equals("Document", StringComparison.OrdinalIgnoreCase)) return 2;
        if (role.Equals("Text",     StringComparison.OrdinalIgnoreCase)) return 99;
        try { return e.Patterns.Value.IsSupported ? 3 : 50; } catch { return 50; }
    }

    // Rank candidates for invoke: prefer Button > MenuItem > InvokePattern holder > other.
    private static int RankForInvoke(AutomationElement e)
    {
        var role = SafeRole(e);
        if (role.Equals("Button",   StringComparison.OrdinalIgnoreCase))
        {
            try { return e.Patterns.Invoke.IsSupported ? 0 : 1; } catch { return 1; }
        }
        if (role.Equals("MenuItem", StringComparison.OrdinalIgnoreCase))
        {
            try { return e.Patterns.Invoke.IsSupported ? 2 : 3; } catch { return 3; }
        }
        if (role.Equals("Text", StringComparison.OrdinalIgnoreCase)) return 99;
        try { return e.Patterns.Invoke.IsSupported ? 4 : 50; } catch { return 50; }
    }

    private static bool MatchesName(AutomationElement e, string val)
        => SafeName(e).Contains(val, StringComparison.OrdinalIgnoreCase) ||
           UiaTreeWalker.SafeHelpText(e).Contains(val, StringComparison.OrdinalIgnoreCase) ||
           UiaTreeWalker.SafeLegacyName(e).Contains(val, StringComparison.OrdinalIgnoreCase) ||
           UiaTreeWalker.SafeLabeledByName(e).Contains(val, StringComparison.OrdinalIgnoreCase);

    private static string BuildNotFoundMessage(
        string selector, List<AutomationElement> elements, bool truncated, bool isBrowser)
    {
        var sb = new System.Text.StringBuilder($"Target '{selector}' not found.");

        if (isBrowser)
        {
            var roleSummary = elements
                .Select(e => UiaTreeWalker.SafeRole(e))
                .Where(r => !string.IsNullOrEmpty(r))
                .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key)
                .Select(g => g.Count() == 1 ? g.Key : $"{g.Key}×{g.Count()}");
            sb.Append($" Browser tree roles: {string.Join(", ", roleSummary)}.");

            if (selector.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
            {
                var editButtons = elements
                    .Where(e => { var r = UiaTreeWalker.SafeRole(e);
                                  return r.Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
                                         r.Equals("Button", StringComparison.OrdinalIgnoreCase); })
                    .Take(5)
                    .Select(e => $"{UiaTreeWalker.SafeRole(e)}" +
                                 $"[name='{UiaTreeWalker.SafeName(e)}'" +
                                 $",help='{UiaTreeWalker.SafeHelpText(e)}'" +
                                 $",leg='{UiaTreeWalker.SafeLegacyName(e)}']")
                    .ToList();
                if (editButtons.Count > 0)
                    sb.Append($" Edit/Button found: {string.Join("; ", editButtons)}.");
                else
                    sb.Append(" No Edit or Button in tree.");
            }
        }

        if (truncated) sb.Append(" (tree capped — use 'diagnose uia' for full view)");
        return sb.ToString();
    }
}
