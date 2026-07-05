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
    string ScopeLabel,
    int Clicks,
    int CookiesAccepted,
    int LoginAttempts,
    int CartActions,
    int PurchaseActions,
    int PaymentActions,
    bool BrowserOpenAllowed,
    bool ArbitraryCommandAllowed,
    bool RecipeExecutionEnabledByDefault,
    bool PublicDeployClaimed,
    bool ReleaseCommercialReady)
{
    public static PilotSafetySummary LabDevRuntimeFootprintDefaultBlocked { get; } = new(
        ScopeLabel: "LAB_DEV_RUNTIME_FOOTPRINT_RECIPE_EXECUTION_DEFAULT_BLOCKED_NOT_PRODUCT_LEDGER_LOCAL_ONLY_AUTHORITY",
        Clicks: 0,
        CookiesAccepted: 0,
        LoginAttempts: 0,
        CartActions: 0,
        PurchaseActions: 0,
        PaymentActions: 0,
        BrowserOpenAllowed: false,
        ArbitraryCommandAllowed: false,
        RecipeExecutionEnabledByDefault: false,
        PublicDeployClaimed: false,
        ReleaseCommercialReady: false);
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
