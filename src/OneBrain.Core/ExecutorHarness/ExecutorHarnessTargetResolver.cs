using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessTargetResolver
{
    // Temporary adapter: harness target resolution now delegates identity matching
    // to SelectorEngine, while this file still owns allowlist/policy checks.
    public static ExecutorHarnessTargetResolution ResolveTarget(ExecutorHarnessTarget target)
    {
        var signals = new List<string>();
        var issues = new List<string>();
        var selectorMatches = ResolveSelector(target, issues, signals);

        if (!string.Equals(target.HarnessId, ExecutorHarnessDemoFixture.HarnessId, StringComparison.OrdinalIgnoreCase))
            issues.Add("harness id is not allowlisted");
        if (!string.Equals(target.AppProfileId, "onebrain-pilot-local", StringComparison.OrdinalIgnoreCase))
            issues.Add("app profile is not the local Pilot harness");
        if (!string.Equals(target.WindowTitleContains, "ONE BRAIN Pilot", StringComparison.OrdinalIgnoreCase))
            issues.Add("window target is not the local Pilot harness");
        if (!target.ControlledSurface)
            issues.Add("target is not a controlled harness surface");
        if (!target.IsBenign)
            issues.Add("target is not marked benign");
        if (!target.HasSafeExecutor)
            issues.Add("safe executor is missing");
        if (!string.Equals(target.ActionKind, ApprovalActionKinds.BenignHarnessClick, StringComparison.OrdinalIgnoreCase))
            issues.Add("action kind is not the benign harness click action");
        if (string.IsNullOrWhiteSpace(target.TargetRef) || string.IsNullOrWhiteSpace(target.ExpectedTargetName))
            issues.Add("target identity is incomplete");
        if (LooksExternal(target.WindowTitleContains) || LooksExternal(target.TargetRef) || LooksExternal(target.ExpectedTargetName))
            issues.Add("target contains external navigation signal");

        signals.Add($"harnessId={target.HarnessId}");
        signals.Add($"appProfileId={target.AppProfileId}");
        signals.Add($"windowTitleContains={target.WindowTitleContains}");
        signals.Add($"targetRef={target.TargetRef}");
        signals.Add($"expectedTargetName={target.ExpectedTargetName}");
        signals.Add($"controlledSurface={target.ControlledSurface.ToString().ToLowerInvariant()}");
        signals.Add("localOnly=true");
        signals.AddRange(selectorMatches);

        var success = issues.Count == 0;
        var allowlistedIdentity = BuildAllowlistedIdentity();
        return new ExecutorHarnessTargetResolution(
            Success: success,
            Status: success ? "resolved" : ExecutorHarnessStatuses.Blocked,
            Message: success ? "benign local harness target resolved" : string.Join("; ", issues),
            HarnessId: target.HarnessId,
            AppProfileId: target.AppProfileId,
            WindowTitleContains: target.WindowTitleContains,
            TargetRef: target.TargetRef,
            ExpectedTargetName: target.ExpectedTargetName,
            ControlledSurface: target.ControlledSurface,
            LocalOnly: success,
            Signals: signals.Concat(issues.Select(issue => $"blocked={issue}")).ToList(),
            ObservedIdentity: success ? allowlistedIdentity : null,
            Candidates: [allowlistedIdentity],
            MatchVerdict: success ? "Same" : "Different");
    }

    public static ExecutorHarnessTargetResolution ResolveCommand(ExecutorHarnessClickCommand command)
    {
        var target = ExecutorHarnessDemoFixture.CreateTarget() with
        {
            HarnessId = command.HarnessId,
            WindowTitleContains = command.WindowTitleContains,
            TargetRef = command.TargetRef,
            ExpectedTargetName = command.ExpectedTargetName,
            ActionKind = command.ActionKind
        };

        return ResolveTarget(target);
    }

    private static bool LooksExternal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("https://", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("mercadolibre", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("checkout", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("pago", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("payment", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ResolveSelector(
        ExecutorHarnessTarget target,
        ICollection<string> issues,
        ICollection<string> signals)
    {
        if (!SelectorEngine.TryParseLegacySelector(target.TargetRef, out var selector))
        {
            issues.Add("target selector is not supported");
            return ["selector=invalid"];
        }

        var allowlistedIdentity = BuildAllowlistedIdentity();
        selector = selector with
        {
            Provenance = Provenance.Fixture,
            ExpectedIdentity = allowlistedIdentity
        };

        var resolution = SelectorEngine.Resolve(selector, [allowlistedIdentity]);
        signals.Add($"selectorEngine.success={resolution.Success.ToString().ToLowerInvariant()}");
        signals.Add($"selectorEngine.confidence={resolution.Confidence:0.00}");

        if (!resolution.Success)
        {
            issues.Add(resolution.Ambiguous
                ? "target identity is ambiguous for the benign harness target"
                : "target identity is not the benign harness target");
            return resolution.Reasons.Select(reason => $"selectorEngine={reason}").ToList();
        }

        if (!string.Equals(target.ExpectedTargetName, ExecutorHarnessDemoFixture.TargetName, StringComparison.OrdinalIgnoreCase))
            issues.Add("target identity is not the benign harness target");

        return resolution.Reasons.Select(reason => $"selectorEngine={reason}").ToList();
    }

    public static ElementIdentity BuildAllowlistedIdentity()
    {
        return new ElementIdentity("fixture-benign-harness-target", "Button", ExecutorHarnessDemoFixture.TargetName, "onebrain-benign-harness-target")
        {
            Role = "Button",
            ControlType = "Button",
            ClassName = "Button",
            AncestorPath = "Window:ONE BRAIN Pilot > Group:ExecutorHarness",
            WindowTitle = "ONE BRAIN Pilot",
            ProcessName = "OneBrain.Pilot",
            Provenance = Provenance.Fixture
        };
    }
}
