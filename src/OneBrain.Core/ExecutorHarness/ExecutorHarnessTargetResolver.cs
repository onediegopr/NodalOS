using OneBrain.Core.Approval;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessTargetResolver
{
    public static ExecutorHarnessTargetResolution ResolveTarget(ExecutorHarnessTarget target)
    {
        var signals = new List<string>();
        var issues = new List<string>();

        if (!string.Equals(target.HarnessId, ExecutorHarnessDemoFixture.HarnessId, StringComparison.OrdinalIgnoreCase))
            issues.Add("harness id is not allowlisted");
        if (!string.Equals(target.AppProfileId, "onebrain-pilot-local", StringComparison.OrdinalIgnoreCase))
            issues.Add("app profile is not the local Pilot harness");
        if (!string.Equals(target.WindowTitleContains, "ONE BRAIN Pilot", StringComparison.OrdinalIgnoreCase))
            issues.Add("window target is not the local Pilot harness");
        if (!ExecutorHarnessDemoFixture.IsAllowlistedTargetIdentity(target.TargetRef, target.ExpectedTargetName))
            issues.Add("target identity is not the benign harness target");
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

        var success = issues.Count == 0;
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
            Signals: signals.Concat(issues.Select(issue => $"blocked={issue}")).ToList());
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
}
