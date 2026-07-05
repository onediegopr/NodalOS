namespace OneBrain.Pilot;

public sealed class PilotPlanBuilder
{
    private static readonly string[] BlockedCapabilities =
    [
        "browser.open",
        "click",
        "safe.click",
        "invoke",
        "type",
        "submit",
        "login",
        "cookies.accept",
        "cart",
        "checkout",
        "payment",
        "whatsapp",
        "arbitrary-command"
    ];

    public PilotPlan Build(PilotIntentResult intent)
    {
        var steps = intent.IsMatch
            ? new[]
            {
                "Resolve task with rules-based intent router.",
                "Verify selected recipe is present in the hard allowlist.",
                $"Prepare lab/dev recipe run candidate: {intent.Recipe!.RecipePath}.",
                "Require explicit local opt-in before OneBrain.Cli recipe runner execution.",
                "Scan local artifacts folders for latest Markdown/HTML paths.",
                "Return execution status and safety summary."
            }
            : new[]
            {
                "Resolve task with rules-based intent router.",
                "Reject because no allowlisted recipe matched the request.",
                "Do not execute anything."
            };

        return new PilotPlan(intent, steps, BlockedCapabilities, PilotSafetySummary.LabDevRuntimeFootprintDefaultBlocked);
    }
}
