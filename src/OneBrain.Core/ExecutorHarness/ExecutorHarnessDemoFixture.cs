using OneBrain.Core.Approval;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessDemoFixture
{
    public const string HarnessId = "onebrain-pilot-benign-click-harness-v1";
    public const string FlowId = "onebrain-pilot-benign-flow-v1";
    public const string TargetName = "Objetivo benigno del harness";
    public const string SecondaryTargetName = "Objetivo benigno secundario del harness";

    public static ExecutorHarnessTarget CreateTarget()
    {
        return new ExecutorHarnessTarget(
            HarnessId: HarnessId,
            Title: "Click benigno supervisado en harness local",
            Description: "Primer click real permitido solo contra un boton benigno de ONE BRAIN Pilot. No toca sitios externos ni acciones comerciales.",
            AppProfileId: "onebrain-pilot-local",
            WindowTitleContains: "ONE BRAIN Pilot",
            TargetRef: $"name:{TargetName}",
            ExpectedTargetName: TargetName,
            ActionKind: ApprovalActionKinds.BenignHarnessClick,
            ControlledSurface: true,
            IsBenign: true,
            HasSafeExecutor: true,
            Notes:
            [
                "controlled local Pilot harness",
                "requires explicit approval decision",
                "no MercadoLibre",
                "no login, cookies, cart, purchase, payment, or submit"
            ]);
    }

    public static IReadOnlyList<ExecutorHarnessTarget> CreateFlowTargets()
    {
        return
        [
            CreateTarget() with
            {
                Title = "Paso 1 del harness benigno local",
                Description = "Primer paso benigno del flujo local controlado."
            },
            CreateTarget() with
            {
                Title = "Paso 2 del harness benigno local",
                Description = "Segundo paso benigno del flujo local controlado.",
                TargetRef = $"name:{SecondaryTargetName}",
                ExpectedTargetName = SecondaryTargetName,
                Notes = CreateTarget().Notes
                    .Concat(["second allowlisted benign harness target"])
                    .ToList()
            }
        ];
    }

    public static bool IsAllowlistedTargetIdentity(string targetRef, string expectedTargetName)
    {
        foreach (var target in CreateFlowTargets())
        {
            if (string.Equals(target.TargetRef, targetRef, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(target.ExpectedTargetName, expectedTargetName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
