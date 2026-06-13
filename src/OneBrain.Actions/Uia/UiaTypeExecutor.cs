using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;
using OneBrain.Observation.Input;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Actions.Uia;

public sealed class UiaTypeExecutor : IUiaTypeExecutor
{
    private readonly WindowFinder _windowFinder = new();
    private readonly IDesktopOwnershipMonitor _ownershipMonitor;

    public UiaTypeExecutor(IDesktopOwnershipMonitor? ownershipMonitor = null)
    {
        _ownershipMonitor = ownershipMonitor ?? new DesktopOwnershipMonitor();
    }

    public TypeExecutionResult Type(TypeExecutionRequest request)
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
                    return Fail(
                        FailureKind.NotFound,
                        [$"uia root '{rootHwnd}' not found"],
                        approvedTextDigest: request.ApprovedTextDigest);
                }

                return TypeAgainstRoot(request, root);
            }

            var hwnd = _windowFinder.FindWindow(request.ProcessName, request.WindowTitleContains);
            if (hwnd == IntPtr.Zero)
            {
                return Fail(
                    FailureKind.NotFound,
                    [$"window '{request.ProcessName ?? request.WindowTitleContains}' not found"],
                    approvedTextDigest: request.ApprovedTextDigest);
            }

            using var automation = new UIA3Automation();
            root = automation.FromHandle(hwnd);
            if (root == null)
            {
                return Fail(
                    FailureKind.NotFound,
                    ["uia root not available for requested window"],
                    approvedTextDigest: request.ApprovedTextDigest);
            }

            return TypeAgainstRoot(request, root);
        }
        catch (Exception ex)
        {
            return Fail(
                FailureKind.Unverified,
                [$"uia type failed: {ex.Message}"],
                approvedTextDigest: request.ApprovedTextDigest);
        }
    }

    private TypeExecutionResult TypeAgainstRoot(TypeExecutionRequest request, AutomationElement root)
    {
        try
        {
            var elements = new List<AutomationElement>();
            UiaTreeWalker.Walk(root, elements, UiaTreeWalker.DefaultMaxElements, UiaTreeWalker.DefaultMaxDepth);

            var candidates = elements.Select(BuildIdentity).ToList();
            var resolution = SelectorEngine.Resolve(request.Selector, candidates);
            if (!resolution.Success || resolution.BestMatch == null)
            {
                return Fail(
                    resolution.FailureKind ?? FailureKind.NotFound,
                    resolution.Reasons,
                    approvedTextDigest: request.ApprovedTextDigest);
            }

            var match = elements.FirstOrDefault(element => SameIdentity(BuildIdentity(element), resolution.BestMatch));
            if (match == null)
            {
                return Fail(
                    FailureKind.NotFound,
                    ["resolved identity could not be reattached to a UIA element"],
                    approvedTextDigest: request.ApprovedTextDigest,
                    observedIdentity: resolution.BestMatch);
            }

            var invokeTimeDecision = InvokeTimeIdentityGate.Evaluate(request.ExpectedIdentity, resolution.BestMatch);
            if (!invokeTimeDecision.Allowed)
            {
                return Fail(
                    invokeTimeDecision.Verdict is "Stale" or "Different"
                        ? FailureKind.Stale
                        : FailureKind.PolicyDenied,
                    [invokeTimeDecision.Reason, .. invokeTimeDecision.Reasons],
                    approvedTextDigest: request.ApprovedTextDigest,
                    observedIdentity: resolution.BestMatch,
                    invokeTimeDecision: invokeTimeDecision);
            }

            var passwordKnown = TryReadPasswordFlag(match, out var isPassword);
            var role = UiaTreeWalker.SafeRole(match);
            var surfaceDecision = TypeSurfacePolicy.Decide(
                role,
                valueSupported: match.Patterns.Value.IsSupported,
                isPassword: !passwordKnown || isPassword,
                invokeSupported: match.Patterns.Invoke.IsSupported,
                toggleSupported: match.Patterns.Toggle.IsSupported,
                selectionItemSupported: match.Patterns.SelectionItem.IsSupported,
                expandCollapseSupported: match.Patterns.ExpandCollapse.IsSupported,
                scrollSupported: match.Patterns.Scroll.IsSupported,
                isEnabled: UiaTreeWalker.SafeEnabled(match),
                isOffscreen: UiaTreeWalker.SafeOffscreen(match));

            if (!surfaceDecision.Allowed)
            {
                return Fail(
                    surfaceDecision.FailureKind ?? FailureKind.PolicyDenied,
                    [
                        surfaceDecision.Reason,
                        $"role={surfaceDecision.Role ?? "unknown"}",
                        $"supportsValuePattern={surfaceDecision.SupportsValuePattern.ToString().ToLowerInvariant()}"
                    ],
                    approvedTextDigest: request.ApprovedTextDigest,
                    patternUsed: surfaceDecision.PatternUsed,
                    observedIdentity: resolution.BestMatch,
                    invokeTimeDecision: invokeTimeDecision,
                    surfaceAllowed: false,
                    surfaceReason: surfaceDecision.Reason);
            }

            var valueBefore = ReadValuePattern(match);
            var ownership = CheckOwnership();
            if (!ownership.Allowed)
            {
                return Fail(
                    FailureKind.HumanInterrupted,
                    [ownership.Reason],
                    approvedTextDigest: request.ApprovedTextDigest,
                    patternUsed: surfaceDecision.PatternUsed,
                    observedIdentity: resolution.BestMatch,
                    invokeTimeDecision: invokeTimeDecision,
                    surfaceAllowed: true,
                    surfaceReason: surfaceDecision.Reason,
                    ownershipChecked: true,
                    ownershipAllowed: false,
                    valueBefore: valueBefore);
            }

            SetValuePattern(match, request.ApprovedText);
            var valueAfter = ReadValuePattern(match);
            var mutationObserved = !string.Equals(valueBefore, valueAfter, StringComparison.Ordinal) ||
                                   string.Equals(valueAfter, request.ApprovedText, StringComparison.Ordinal);
            var success = string.Equals(valueAfter, request.ApprovedText, StringComparison.Ordinal);
            var targetVisible = elements.Any(element =>
                string.Equals(UiaTreeWalker.SafeName(element), request.ExpectedTargetName, StringComparison.OrdinalIgnoreCase));

            if (!success)
            {
                return Fail(
                    FailureKind.Unverified,
                    ["safe.type readback did not match approved text"],
                    approvedTextDigest: request.ApprovedTextDigest,
                    patternUsed: surfaceDecision.PatternUsed,
                    observedIdentity: resolution.BestMatch,
                    invokeTimeDecision: invokeTimeDecision,
                    surfaceAllowed: true,
                    surfaceReason: surfaceDecision.Reason,
                    ownershipChecked: true,
                    ownershipAllowed: true,
                    valueBefore: valueBefore,
                    valueAfter: valueAfter,
                    mutationObserved: mutationObserved,
                    windowFound: true,
                    targetVisible: targetVisible);
            }

            return new TypeExecutionResult(
                Success: true,
                FailureKind: null,
                Reasons:
                [
                    "uia type completed",
                    $"role={surfaceDecision.Role ?? "unknown"}",
                    $"pattern={surfaceDecision.PatternUsed}",
                    "typeSurface=allowlisted"
                ],
                ValueBefore: valueBefore,
                ValueAfter: valueAfter,
                ApprovedTextDigest: request.ApprovedTextDigest,
                PatternUsed: surfaceDecision.PatternUsed,
                ObservedIdentity: resolution.BestMatch,
                IdentityVerdict: invokeTimeDecision.Verdict,
                InvokeTimeIdentityChecked: true,
                InvokeTimeIdentityVerdict: invokeTimeDecision.Verdict,
                InvokeTimeIdentityReason: invokeTimeDecision.Reason,
                ExpectedIdentityDigest: invokeTimeDecision.ExpectedIdentityDigest,
                ObservedIdentityDigest: invokeTimeDecision.ObservedIdentityDigest,
                MutationObserved: mutationObserved,
                SurfaceAllowed: true,
                SurfaceReason: surfaceDecision.Reason,
                OwnershipChecked: true,
                OwnershipAllowed: true,
                WindowFound: true,
                TargetVisible: targetVisible,
                Signals:
                [
                    "type.ownershipChecked=true",
                    "type.ownershipAllowed=true",
                    "type.surfaceAllowed=true",
                    $"type.patternUsed={surfaceDecision.PatternUsed}",
                    $"type.mutationObserved={mutationObserved.ToString().ToLowerInvariant()}",
                    "invokeTimeIdentity.checked=true",
                    $"invokeTimeIdentity.verdict={invokeTimeDecision.Verdict}",
                    $"invokeTimeIdentity.expectedDigest={invokeTimeDecision.ExpectedIdentityDigest}",
                    $"invokeTimeIdentity.observedDigest={invokeTimeDecision.ObservedIdentityDigest}"
                ]);
        }
        catch (Exception ex)
        {
            return Fail(
                FailureKind.Unverified,
                [$"uia type failed: {ex.Message}"],
                approvedTextDigest: request.ApprovedTextDigest);
        }
    }

    private (bool Allowed, string Reason) CheckOwnership()
    {
        try
        {
            var baseline = _ownershipMonitor.Capture();
            if (_ownershipMonitor.HumanInputSince(baseline) || _ownershipMonitor.ForegroundChanged(baseline))
                return (false, "ownership changed before safe.type commit");

            return (true, "ownership stable before safe.type commit");
        }
        catch (Exception ex)
        {
            return (false, $"ownership check failed: {ex.Message}");
        }
    }

    private static TypeExecutionResult Fail(
        FailureKind failureKind,
        IReadOnlyList<string> reasons,
        string approvedTextDigest,
        string patternUsed = "",
        ElementIdentity? observedIdentity = null,
        InvokeTimeIdentityDecision? invokeTimeDecision = null,
        bool surfaceAllowed = false,
        string surfaceReason = "",
        bool ownershipChecked = false,
        bool ownershipAllowed = false,
        string valueBefore = "",
        string valueAfter = "",
        bool mutationObserved = false,
        bool windowFound = false,
        bool targetVisible = false)
    {
        return new TypeExecutionResult(
            Success: false,
            FailureKind: failureKind,
            Reasons: reasons,
            ValueBefore: valueBefore,
            ValueAfter: valueAfter,
            ApprovedTextDigest: approvedTextDigest,
            PatternUsed: patternUsed,
            ObservedIdentity: observedIdentity,
            IdentityVerdict: invokeTimeDecision?.Verdict ?? "",
            InvokeTimeIdentityChecked: invokeTimeDecision?.Checked ?? false,
            InvokeTimeIdentityVerdict: invokeTimeDecision?.Verdict ?? "",
            InvokeTimeIdentityReason: invokeTimeDecision?.Reason ?? "",
            ExpectedIdentityDigest: invokeTimeDecision?.ExpectedIdentityDigest ?? "",
            ObservedIdentityDigest: invokeTimeDecision?.ObservedIdentityDigest ?? "",
            MutationObserved: mutationObserved,
            SurfaceAllowed: surfaceAllowed,
            SurfaceReason: surfaceReason,
            OwnershipChecked: ownershipChecked,
            OwnershipAllowed: ownershipAllowed,
            WindowFound: windowFound,
            TargetVisible: targetVisible);
    }

    private static string ReadValuePattern(AutomationElement element)
    {
        try
        {
            dynamic pattern = element.Patterns.Value.Pattern;
            object? value = pattern.Value;
            return value?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static void SetValuePattern(AutomationElement element, string value)
    {
        dynamic pattern = element.Patterns.Value.Pattern;
        pattern.SetValue(value);
    }

    private static bool TryReadPasswordFlag(AutomationElement element, out bool isPassword)
    {
        try
        {
            dynamic property = element.Properties.IsPassword;
            isPassword = property.Value;
            return true;
        }
        catch
        {
            isPassword = true;
            return false;
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
