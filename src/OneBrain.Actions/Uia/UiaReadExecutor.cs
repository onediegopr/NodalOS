using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Actions.Uia;

public sealed class UiaReadExecutor : IUiaReadExecutor
{
    private readonly WindowFinder _windowFinder = new();

    public PatternReadResult Read(PatternReadRequest request)
    {
        try
        {
            AutomationElement? root;
            if (request.RootHwnd is { } rootHwnd && rootHwnd != IntPtr.Zero)
            {
                using var rootAutomation = new UIA3Automation();
                root = rootAutomation.FromHandle(rootHwnd);
                if (root == null)
                {
                    return new PatternReadResult(
                        Success: false,
                        FailureKind: FailureKind.NotFound,
                        Reasons: [$"uia root '{rootHwnd}' not found"]);
                }

                return ReadAgainstRoot(request, root);
            }

            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitleContains);
            if (hwnd == IntPtr.Zero)
            {
                return new PatternReadResult(
                    Success: false,
                    FailureKind: FailureKind.NotFound,
                    Reasons: [$"window '{request.ProcessName ?? request.WindowTitleContains}' not found"]);
            }

            using var automation = new UIA3Automation();
            root = automation.FromHandle(hwnd);
            if (root == null)
            {
                return new PatternReadResult(
                    Success: false,
                    FailureKind: FailureKind.NotFound,
                    Reasons: ["uia root not available for requested window"]);
            }

            return ReadAgainstRoot(request, root);
        }
        catch (Exception ex)
        {
            return new PatternReadResult(
                Success: false,
                FailureKind: FailureKind.Unverified,
                Reasons: [$"uia read failed: {ex.Message}"]);
        }
    }

    private static PatternReadResult ReadAgainstRoot(PatternReadRequest request, AutomationElement root)
    {
        try
        {
            var elements = new List<AutomationElement>();
            UiaTreeWalker.Walk(root, elements, UiaTreeWalker.DefaultMaxElements, UiaTreeWalker.DefaultMaxDepth);

            var candidates = elements.Select(BuildIdentity).ToList();
            var resolution = SelectorEngine.Resolve(request.Selector, candidates);
            if (!resolution.Success || resolution.BestMatch == null)
            {
                return new PatternReadResult(
                    Success: false,
                    FailureKind: resolution.FailureKind ?? FailureKind.NotFound,
                    Reasons: resolution.Reasons);
            }

            var match = elements.FirstOrDefault(element => SameIdentity(BuildIdentity(element), resolution.BestMatch));
            if (match == null)
            {
                return new PatternReadResult(
                    Success: false,
                    FailureKind: FailureKind.NotFound,
                    Reasons: ["resolved identity could not be reattached to a UIA element"]);
            }

            var invokeTimeDecision = InvokeTimeIdentityGate.Evaluate(request.ExpectedIdentity, resolution.BestMatch);
            if (!invokeTimeDecision.Allowed)
            {
                return ReadIdentityBlocked(
                    invokeTimeDecision.Verdict is "Stale" or "Different"
                        ? FailureKind.Stale
                        : FailureKind.PolicyDenied,
                    invokeTimeDecision,
                    resolution.BestMatch);
            }

            var role = UiaTreeWalker.SafeRole(match);
            var readSurface = ReadSurfacePolicy.Decide(
                role,
                valueSupported: match.Patterns.Value.IsSupported,
                textSupported: match.Patterns.Text.IsSupported,
                invokeSupported: match.Patterns.Invoke.IsSupported,
                mutationPatternSupported:
                    match.Patterns.Toggle.IsSupported ||
                    match.Patterns.SelectionItem.IsSupported ||
                    match.Patterns.ExpandCollapse.IsSupported);

            if (!readSurface.Allowed)
            {
                return new PatternReadResult(
                    Success: false,
                    FailureKind: readSurface.FailureKind ?? FailureKind.PolicyDenied,
                    Reasons:
                    [
                        readSurface.Reason,
                        $"role={readSurface.Role ?? "unknown"}",
                        "readSurface=read-only"
                    ],
                    ObservedIdentity: resolution.BestMatch,
                    InvokeTimeIdentityChecked: true,
                    InvokeTimeIdentityVerdict: invokeTimeDecision.Verdict,
                    InvokeTimeIdentityReason: invokeTimeDecision.Reason,
                    ExpectedIdentityDigest: invokeTimeDecision.ExpectedIdentityDigest,
                    ObservedIdentityDigest: invokeTimeDecision.ObservedIdentityDigest);
            }

            var value = readSurface.PatternUsed == "ValuePattern"
                ? ReadValuePattern(match)
                : ReadTextPattern(match);
            var targetVisible = elements.Any(element =>
                string.Equals(UiaTreeWalker.SafeName(element), request.ExpectedTargetName, StringComparison.OrdinalIgnoreCase));

            return new PatternReadResult(
                Success: true,
                FailureKind: null,
                Reasons:
                [
                    "uia read completed",
                    $"role={readSurface.Role ?? "unknown"}",
                    $"pattern={readSurface.PatternUsed}",
                    "readSurface=read-only"
                ],
                Value: value,
                PatternUsed: readSurface.PatternUsed,
                ObservedIdentity: resolution.BestMatch,
                WindowFound: true,
                TargetVisible: targetVisible,
                MutationObserved: false,
                Signals:
                [
                    $"read.windowFound=true",
                    $"read.targetVisible={targetVisible.ToString().ToLowerInvariant()}",
                    $"read.patternUsed={readSurface.PatternUsed}",
                    "read.mutationObserved=false",
                    "invokeTimeIdentity.checked=true",
                    $"invokeTimeIdentity.verdict={invokeTimeDecision.Verdict}",
                    $"invokeTimeIdentity.expectedDigest={invokeTimeDecision.ExpectedIdentityDigest}",
                    $"invokeTimeIdentity.observedDigest={invokeTimeDecision.ObservedIdentityDigest}"
                ],
                InvokeTimeIdentityChecked: true,
                InvokeTimeIdentityVerdict: invokeTimeDecision.Verdict,
                InvokeTimeIdentityReason: invokeTimeDecision.Reason,
                ExpectedIdentityDigest: invokeTimeDecision.ExpectedIdentityDigest,
                ObservedIdentityDigest: invokeTimeDecision.ObservedIdentityDigest);
        }
        catch (Exception ex)
        {
            return new PatternReadResult(
                Success: false,
                FailureKind: FailureKind.Unverified,
                Reasons: [$"uia read failed: {ex.Message}"]);
        }
    }

    private static PatternReadResult ReadIdentityBlocked(
        FailureKind failureKind,
        InvokeTimeIdentityDecision decision,
        ElementIdentity observedIdentity)
    {
        return new PatternReadResult(
            Success: false,
            FailureKind: failureKind,
            Reasons:
            [
                decision.Reason,
                .. decision.Reasons,
                "invokeTimeIdentity.checked=true",
                $"invokeTimeIdentity.verdict={decision.Verdict}",
                $"invokeTimeIdentity.expectedDigest={decision.ExpectedIdentityDigest}",
                $"invokeTimeIdentity.observedDigest={decision.ObservedIdentityDigest}"
            ],
            ObservedIdentity: observedIdentity,
            InvokeTimeIdentityChecked: true,
            InvokeTimeIdentityVerdict: decision.Verdict,
            InvokeTimeIdentityReason: decision.Reason,
            ExpectedIdentityDigest: decision.ExpectedIdentityDigest,
            ObservedIdentityDigest: decision.ObservedIdentityDigest);
    }

    private static string ReadValuePattern(AutomationElement element)
    {
        try
        {
            var pattern = element.Patterns.Value.Pattern;
            var value = pattern.GetType().GetProperty("Value")?.GetValue(pattern);
            return value?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static string ReadTextPattern(AutomationElement element)
    {
        try
        {
            var pattern = element.Patterns.Text.Pattern;
            var range = pattern.GetType().GetProperty("DocumentRange")?.GetValue(pattern);
            var getText = range?.GetType().GetMethod("GetText", [typeof(int)]);
            return getText?.Invoke(range, [4096])?.ToString() ?? "";
        }
        catch
        {
            return "";
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
