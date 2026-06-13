using System.Runtime.InteropServices;
using FlaUI.Core.WindowsAPI;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using OneBrain.Core.Actions;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;
using OneBrain.Safety.Policies;
using OneBrain.Observation.Windows;
using OneBrain.Observation.Uia;

namespace OneBrain.Actions.Uia;

public sealed class UiaActionExecutor
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

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
        // Check cancellation before any heavy work
        if (request.CancellationToken?.IsCancellationRequested == true)
            return new ActionResult(false, "Step expired before physical input.");

        using var automation = new UIA3Automation();

        AutomationElement? root = null;
        IntPtr expectedHwnd = IntPtr.Zero;

        if (!string.IsNullOrEmpty(request.ProcessName) || !string.IsNullOrEmpty(request.WindowTitle))
        {
            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitle);

            if (hwnd == IntPtr.Zero)
            {
                var desc = request.ProcessName ?? request.WindowTitle;
                return new ActionResult(false, $"Process/window '{desc}' not found or not visible.");
            }

            _windowFinder.Activate(hwnd);
            expectedHwnd = hwnd;
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
                if (request.CancellationToken?.IsCancellationRequested == true)
                    return new ActionResult(false, "Step expired before physical input.");
                if (!AssertForegroundWithRetry(expectedHwnd))
                    return new ActionResult(false, "Foreground changed before input; aborting.");
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


            if (kind == "invoke" || kind == "click" || kind == "press")
            {
                if (request.CancellationToken?.IsCancellationRequested == true)
                    return new ActionResult(false, "Step expired before physical input.");

                if (SafeRole(target) == "Button")
                    target.AsButton().Invoke();
                else
                    target.Click();

                return new ActionResult(true, "Action executed.");
            }

            return new ActionResult(false, $"Unsupported action kind: {request.Kind}. Supported: type, type_text, focus, key, invoke, click.");
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
            // Retry up to 3 times if foreground is stolen
            for (int retry = 0; retry < 3; retry++)
            {
                if (GetForegroundWindow() == hwnd) break;
                _windowFinder.Activate(hwnd);
                System.Threading.Thread.Sleep(200);
            }
            if (GetForegroundWindow() != hwnd)
                return new ActionResult(false, "Foreground changed before fallback input; aborting.");
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

        if (!SelectorEngine.TryParseLegacySelector(selector, out var parsedSelector))
            return null;

        var selectorForAction = AdaptSelectorForAction(parsedSelector, actionKind);
        var candidates = elements.Select(BuildCandidate).ToList();
        var resolution = SelectorEngine.Resolve(
            selectorForAction,
            candidates.Select(candidate => candidate.Identity).ToList());

        if (!resolution.Success || resolution.BestMatch == null)
            return null;

        return candidates.FirstOrDefault(candidate => IsSameIdentity(candidate.Identity, resolution.BestMatch))
            ?.Element;
    }

    private static SelectorDefinition AdaptSelectorForAction(SelectorDefinition selector, string actionKind)
    {
        var kind = actionKind.ToLowerInvariant();
        if (selector.ExpectedIdentity != null)
            return selector;

        var expectedControlType = kind switch
        {
            "type" or "type_text" => "Edit",
            "invoke" or "click" or "press" => "Button",
            _ => ""
        };

        if (expectedControlType.Length == 0)
            return selector;

        return selector with
        {
            ExpectedIdentity = new ElementIdentity("", "", selector.Name ?? "", selector.AutomationId ?? "")
            {
                Role = expectedControlType,
                ControlType = expectedControlType,
                HelpText = selector.HelpText ?? "",
                LegacyName = selector.LegacyName ?? "",
                ClassName = selector.ClassName ?? "",
                AncestorPath = selector.AncestorPath ?? "",
                Provenance = Provenance.Uia
            }
        };
    }

    private static CandidateAdapter BuildCandidate(AutomationElement element)
    {
        var role = SafeRole(element);
        var identity = new ElementIdentity(
            UiaTreeWalker.SafeRuntimeId(element),
            role,
            SafeName(element),
            SafeId(element))
        {
            Role = role,
            ControlType = role,
            HelpText = UiaTreeWalker.SafeHelpText(element),
            LegacyName = UiaTreeWalker.SafeLegacyName(element),
            LabeledByName = UiaTreeWalker.SafeLabeledByName(element),
            ClassName = SafeClass(element),
            AncestorPath = BuildAncestorPath(element),
            Provenance = Provenance.Uia
        };

        return new CandidateAdapter(element, identity);
    }

    private static string BuildAncestorPath(AutomationElement element)
    {
        var segments = new List<string>();

        try
        {
            var current = element.Parent;
            while (current != null)
            {
                var role = SafeRole(current);
                var name = SafeName(current);
                var segment = string.IsNullOrWhiteSpace(name) ? role : $"{role}:{name}";
                if (!string.IsNullOrWhiteSpace(segment))
                    segments.Add(segment);
                current = current.Parent;
            }
        }
        catch
        {
            return "";
        }

        segments.Reverse();
        return string.Join(" > ", segments);
    }

    private static bool IsSameIdentity(ElementIdentity left, ElementIdentity right)
    {
        return string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal) &&
               string.Equals(left.AutomationId, right.AutomationId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.EffectiveControlType, right.EffectiveControlType, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.ClassName, right.ClassName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.AncestorPath, right.AncestorPath, StringComparison.OrdinalIgnoreCase);
    }

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

    private static bool AssertForeground(IntPtr expectedHwnd)
    {
        if (expectedHwnd == IntPtr.Zero) return true; // no window expected, allow
        return GetForegroundWindow() == expectedHwnd;
    }

    private bool AssertForegroundWithRetry(IntPtr expectedHwnd)
    {
        if (expectedHwnd == IntPtr.Zero) return true;
        if (GetForegroundWindow() == expectedHwnd) return true;

        // Retry: re-activate and check once more
        _windowFinder.Activate(expectedHwnd);
        System.Threading.Thread.Sleep(300);
        return GetForegroundWindow() == expectedHwnd;
    }

    private sealed record CandidateAdapter(AutomationElement Element, ElementIdentity Identity);
}
