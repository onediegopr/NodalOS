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
                $"Prepare read-only recipe run: {intent.Recipe!.RecipePath}.",
                "Execute through OneBrain.Cli recipe runner only.",
                "Scan local artifacts folders for latest Markdown/HTML paths.",
                "Return execution status and safety summary."
            }
            : new[]
            {
                "Resolve task with rules-based intent router.",
                "Reject because no allowlisted recipe matched the request.",
                "Do not execute anything."
            };

        return new PilotPlan(intent, steps, BlockedCapabilities, PilotSafetySummary.ZeroReadOnly);
    }
}
