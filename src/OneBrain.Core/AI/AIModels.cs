namespace OneBrain.Core.AI;

public static class AIProfileKinds
{
    public const string CheapIntent = "cheap_intent";
    public const string StandardTask = "standard_task";
    public const string CriticalReasoner = "critical_reasoner";
    public const string VisionVerifier = "vision_verifier";
}

public static class AIProfileIds
{
    public const string CheapIntent = "OB_AI_CHEAP_INTENT";
    public const string StandardTask = "OB_AI_STANDARD_TASK";
    public const string CriticalReasoner = "OB_AI_CRITICAL_REASONER";
    public const string VisionVerifier = "OB_AI_VISION_VERIFIER";
}

public static class AIProviderKinds
{
    public const string OpenAI = "openai";
    public const string Mock = "mock";
}

public static class AIRiskLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";
}

public static class AIModelCapabilities
{
    public const string Intent = "intent";
    public const string StandardTask = "standard_task";
    public const string CriticalReasoning = "critical_reasoning";
    public const string VisionVerification = "vision_verification";
}

public sealed record AIModelProfile(
    string ProfileId,
    string ProfileKind,
    string DisplayName,
    string Provider,
    string Model,
    string ApiKeySecretName,
    bool ApiKeyConfigured,
    string ApiKeyMasked,
    bool Enabled,
    decimal MonthlyBudgetUsd,
    decimal DailyBudgetUsd,
    decimal MaxCostPerTaskUsd,
    int MaxCallsPerTask,
    int TimeoutSeconds,
    int RetryCount,
    string? FallbackProfileId,
    string MaxRiskLevel,
    bool RequiresAuditLog,
    bool DebugMode,
    bool UsageLoggingEnabled,
    IReadOnlyList<string> Capabilities);

public sealed record AIModelUsageSnapshot(
    string ProfileId,
    decimal DailySpendUsd,
    decimal MonthlySpendUsd,
    int CallsToday);

public sealed record AIModelRoutingPolicy(
    IReadOnlyList<AIModelProfile> Profiles,
    IReadOnlyList<AIModelUsageSnapshot> Usage,
    bool FailClosedOnMissingProfile = true,
    bool FailClosedOnBudgetExceeded = true,
    bool FailClosedOnMissingApiKey = true);

public sealed record AIModelRoutingRequest(
    string TaskText,
    string Capability,
    string RiskLevel,
    bool RequiresVision,
    bool IsAmbiguous,
    bool IsIrreversible,
    decimal EstimatedCostUsd,
    int EstimatedCalls,
    string? Environment,
    string? Profile);

public sealed record AIModelRoutingDecision(
    bool Success,
    string Status,
    string? SelectedProfileId,
    string? FallbackProfileId,
    string Reason,
    bool FailClosed,
    bool RequiresAuditLog,
    bool WouldCallProvider,
    IReadOnlyList<string> Notes);

public sealed record AIModelRouterResult(
    AIModelRoutingRequest Request,
    AIModelRoutingDecision Decision,
    AIModelProfile? Profile);
