using OneBrain.Core.Approval;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessDemoFixture
{
    public const string HarnessId = "onebrain-pilot-benign-click-harness-v1";
    public const string TargetName = "Objetivo benigno del harness";

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
}
