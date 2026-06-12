namespace OneBrain.Core.AI;

public static class AIModelConfiguration
{
    public static IReadOnlyList<AIModelProfile> LoadOfficialProfiles(IReadOnlyDictionary<string, string?>? values = null)
    {
        var source = values ?? Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(entry => entry.Key.ToString() ?? "", entry => entry.Value?.ToString(), StringComparer.OrdinalIgnoreCase);

        return
        [
            Build(source, AIProfileIds.CheapIntent, AIProfileKinds.CheapIntent, "Cheap Intent Engine", AIModelCapabilities.Intent, AIRiskLevels.Low, null),
            Build(source, AIProfileIds.StandardTask, AIProfileKinds.StandardTask, "Standard Task Engine", AIModelCapabilities.StandardTask, AIRiskLevels.Medium, AIProfileIds.CheapIntent),
            Build(source, AIProfileIds.CriticalReasoner, AIProfileKinds.CriticalReasoner, "Critical Reasoner", AIModelCapabilities.CriticalReasoning, AIRiskLevels.Critical, null),
            Build(source, AIProfileIds.VisionVerifier, AIProfileKinds.VisionVerifier, "Vision Verifier", AIModelCapabilities.VisionVerification, AIRiskLevels.High, AIProfileIds.CriticalReasoner)
        ];
    }

    public static string MaskSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "[not configured]";

        var trimmed = value.Trim();
        if (trimmed.Length <= 8)
            return "[configured]";

        return $"{trimmed[..3]}...{trimmed[^4..]}";
    }

    private static AIModelProfile Build(
        IReadOnlyDictionary<string, string?> values,
        string profileId,
        string profileKind,
        string displayName,
        string capability,
        string maxRiskLevel,
        string? fallbackProfileId)
    {
        var provider = Get(values, $"{profileId}_PROVIDER", AIProviderKinds.OpenAI);
        var model = Get(values, $"{profileId}_MODEL", "");
        var apiKey = Get(values, $"{profileId}_API_KEY", "");

        return new AIModelProfile(
            ProfileId: profileId,
            ProfileKind: profileKind,
            DisplayName: displayName,
            Provider: provider,
            Model: model,
            ApiKeySecretName: $"{profileId}_API_KEY",
            ApiKeyConfigured: !string.IsNullOrWhiteSpace(apiKey),
            ApiKeyMasked: MaskSecret(apiKey),
            Enabled: GetBool(values, $"{profileId}_ENABLED", true),
            MonthlyBudgetUsd: GetDecimal(values, $"{profileId}_MONTHLY_BUDGET_USD", 0m),
            DailyBudgetUsd: GetDecimal(values, $"{profileId}_DAILY_BUDGET_USD", 0m),
            MaxCostPerTaskUsd: GetDecimal(values, $"{profileId}_MAX_COST_PER_TASK_USD", 0m),
            MaxCallsPerTask: GetInt(values, $"{profileId}_MAX_CALLS_PER_TASK", 1),
            TimeoutSeconds: GetInt(values, $"{profileId}_TIMEOUT_SECONDS", 30),
            RetryCount: GetInt(values, $"{profileId}_RETRY_COUNT", 0),
            FallbackProfileId: Get(values, $"{profileId}_FALLBACK_PROFILE", fallbackProfileId ?? ""),
            MaxRiskLevel: Get(values, $"{profileId}_MAX_RISK_LEVEL", maxRiskLevel),
            RequiresAuditLog: GetBool(values, $"{profileId}_REQUIRES_AUDIT_LOG", profileId is AIProfileIds.CriticalReasoner or AIProfileIds.VisionVerifier),
            DebugMode: GetBool(values, $"{profileId}_DEBUG_MODE", false),
            UsageLoggingEnabled: GetBool(values, $"{profileId}_USAGE_LOGGING_ENABLED", true),
            Capabilities: [capability]);
    }

    private static string Get(IReadOnlyDictionary<string, string?> values, string key, string defaultValue)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : defaultValue;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string?> values, string key, bool defaultValue)
    {
        var value = Get(values, key, "");
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetInt(IReadOnlyDictionary<string, string?> values, string key, int defaultValue)
    {
        return int.TryParse(Get(values, key, ""), out var parsed) ? parsed : defaultValue;
    }

    private static decimal GetDecimal(IReadOnlyDictionary<string, string?> values, string key, decimal defaultValue)
    {
        return decimal.TryParse(Get(values, key, ""), out var parsed) ? parsed : defaultValue;
    }
}
