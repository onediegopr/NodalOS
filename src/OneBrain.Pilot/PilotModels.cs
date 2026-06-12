namespace OneBrain.Pilot;

public enum PilotIntentStatus
{
    Matched,
    Rejected
}

public sealed record PilotRecipeDefinition(
    string Id,
    string Label,
    string Description,
    string RecipePath,
    string OutputKind);

public sealed record PilotIntentResult(
    PilotIntentStatus Status,
    string OriginalText,
    PilotRecipeDefinition? Recipe,
    string Reason)
{
    public bool IsMatch => Status == PilotIntentStatus.Matched && Recipe != null;
}

public sealed record PilotSafetySummary(
    int Clicks,
    int CookiesAccepted,
    int LoginAttempts,
    int CartActions,
    int PurchaseActions,
    int PaymentActions,
    bool BrowserOpenAllowed,
    bool ArbitraryCommandAllowed)
{
    public static PilotSafetySummary ZeroReadOnly { get; } = new(
        Clicks: 0,
        CookiesAccepted: 0,
        LoginAttempts: 0,
        CartActions: 0,
        PurchaseActions: 0,
        PaymentActions: 0,
        BrowserOpenAllowed: false,
        ArbitraryCommandAllowed: false);
}

public sealed record PilotPlan(
    PilotIntentResult Intent,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> BlockedCapabilities,
    PilotSafetySummary Safety)
{
    public bool HasExecutableRecipe => Intent.IsMatch;
}

public sealed record PilotExecutionResult(
    PilotPlan Plan,
    bool Executed,
    bool Success,
    int? ExitCode,
    string Status,
    string? RecipePath,
    string? LatestMarkdownPath,
    string? LatestHtmlPath,
    string ArtifactsFolder,
    string StandardOutput,
    string StandardError,
    PilotSafetySummary Safety);
