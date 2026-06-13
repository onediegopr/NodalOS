using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Actions.Uia;

public sealed class UiaPatternExecutor : IUiaPatternExecutor
{
    private readonly WindowFinder _windowFinder = new();

    public PatternExecutionResult Invoke(PatternExecutionRequest request)
    {
        try
        {
            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitleContains);
            if (hwnd == IntPtr.Zero)
            {
                return new PatternExecutionResult(
                    Success: false,
                    FailureKind: FailureKind.NotFound,
                    Reasons: [$"window '{request.ProcessName ?? request.WindowTitleContains}' not found"]);
            }

            _windowFinder.Activate(hwnd);

            using var automation = new UIA3Automation();
            var root = automation.FromHandle(hwnd);
            var elements = new List<AutomationElement>();
            UiaTreeWalker.Walk(root, elements, UiaTreeWalker.DefaultMaxElements, UiaTreeWalker.DefaultMaxDepth);

            var candidates = elements.Select(BuildIdentity).ToList();
            var resolution = SelectorEngine.Resolve(request.Selector, candidates);
            if (!resolution.Success || resolution.BestMatch == null)
            {
                return new PatternExecutionResult(
                    Success: false,
                    FailureKind: resolution.FailureKind ?? FailureKind.NotFound,
                    Reasons: resolution.Reasons);
            }

            var match = elements.FirstOrDefault(element => SameIdentity(BuildIdentity(element), resolution.BestMatch));
            if (match == null)
            {
                return new PatternExecutionResult(
                    Success: false,
                    FailureKind: FailureKind.NotFound,
                    Reasons: ["resolved identity could not be reattached to a UIA element"]);
            }

            if (!string.Equals(UiaTreeWalker.SafeRole(match), "Button", StringComparison.OrdinalIgnoreCase))
            {
                return new PatternExecutionResult(
                    Success: false,
                    FailureKind: FailureKind.PolicyDenied,
                    Reasons: ["only UIA button invoke is supported in this hito"],
                    ObservedIdentity: resolution.BestMatch);
            }

            match.AsButton().Invoke();

            var targetVisible = elements.Any(element =>
                string.Equals(UiaTreeWalker.SafeName(element), request.ExpectedTargetName, StringComparison.OrdinalIgnoreCase));
            return new PatternExecutionResult(
                Success: true,
                FailureKind: null,
                Reasons: ["uia invoke executed"],
                ObservedIdentity: resolution.BestMatch,
                WindowFound: true,
                TargetVisible: targetVisible,
                TargetName: request.ExpectedTargetName,
                ObservedActions: 1,
                Signals:
                [
                    $"postAction.windowFound=true",
                    $"postAction.targetVisible={targetVisible.ToString().ToLowerInvariant()}",
                    $"postAction.targetName={request.ExpectedTargetName}",
                    "postAction.observedClicks=1"
                ]);
        }
        catch (Exception ex)
        {
            return new PatternExecutionResult(
                Success: false,
                FailureKind: FailureKind.Unverified,
                Reasons: [$"uia invoke failed: {ex.Message}"]);
        }
    }

    private static ElementIdentity BuildIdentity(AutomationElement element)
    {
        var role = UiaTreeWalker.SafeRole(element);
        return new ElementIdentity(
            UiaTreeWalker.SafeRuntimeId(element),
            role,
            UiaTreeWalker.SafeName(element),
            UiaTreeWalker.SafeId(element))
        {
            Role = role,
            ControlType = role,
            HelpText = UiaTreeWalker.SafeHelpText(element),
            LegacyName = UiaTreeWalker.SafeLegacyName(element),
            LabeledByName = UiaTreeWalker.SafeLabeledByName(element),
            ClassName = UiaTreeWalker.SafeClass(element),
            AncestorPath = BuildAncestorPath(element),
            Provenance = Provenance.Uia
        };
    }

    private static string BuildAncestorPath(AutomationElement element)
    {
        var segments = new List<string>();
        try
        {
            var current = element.Parent;
            while (current != null)
            {
                var role = UiaTreeWalker.SafeRole(current);
                var name = UiaTreeWalker.SafeName(current);
                segments.Add(string.IsNullOrWhiteSpace(name) ? role : $"{role}:{name}");
                current = current.Parent;
            }
        }
        catch
        {
            return "";
        }

        segments.Reverse();
        return string.Join(" > ", segments.Where(segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private static bool SameIdentity(ElementIdentity left, ElementIdentity right)
    {
        return string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal) &&
               string.Equals(left.AutomationId, right.AutomationId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.EffectiveControlType, right.EffectiveControlType, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.AncestorPath, right.AncestorPath, StringComparison.OrdinalIgnoreCase);
    }
}
