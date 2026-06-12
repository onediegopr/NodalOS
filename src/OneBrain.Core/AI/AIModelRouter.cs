namespace OneBrain.Core.AI;

public sealed class AIModelRouter
{
    public AIModelRouterResult Route(AIModelRoutingRequest request, AIModelRoutingPolicy policy)
    {
        var targetProfileId = SelectProfileId(request);
        var notes = new List<string>
        {
            "routing only; no provider call is made in v0",
            "business modules must route through OneBrain.AI.ModelRouter before future provider calls"
        };

        if (request.RequiresVision && IsHighRisk(request))
        {
            targetProfileId = AIProfileIds.CriticalReasoner;
            notes.Add("vision request with high risk/ambiguity escalated to critical reasoner");
        }

        var selected = policy.Profiles.FirstOrDefault(profile => string.Equals(profile.ProfileId, targetProfileId, StringComparison.OrdinalIgnoreCase));
        if (selected == null)
            return Result(request, null, false, "missing_profile", null, $"required profile {targetProfileId} is missing", true, notes);

        var profile = ResolveEnabledProfile(selected, policy, notes);
        if (profile == null)
            return Result(request, selected, false, "profile_disabled", selected.FallbackProfileId, "selected profile disabled and no valid fallback is available", true, notes);

        var configError = ValidateConfig(profile, request, policy);
        if (!string.IsNullOrWhiteSpace(configError))
            return Result(request, profile, false, "blocked", profile.FallbackProfileId, configError, true, notes);

        var budgetError = ValidateBudget(profile, request, policy);
        if (!string.IsNullOrWhiteSpace(budgetError))
            return Result(request, profile, false, "budget_blocked", profile.FallbackProfileId, budgetError, policy.FailClosedOnBudgetExceeded, notes);

        return Result(request, profile, true, "selected", profile.FallbackProfileId, "profile selected", false, notes);
    }

    private static string SelectProfileId(AIModelRoutingRequest request)
    {
        var text = request.TaskText.ToLowerInvariant();
        if (request.RequiresVision || request.Capability == AIModelCapabilities.VisionVerification)
            return AIProfileIds.VisionVerifier;

        if (IsHighRisk(request) || ContainsAny(text, "send", "enviar", "delete", "borrar", "pay", "pagar", "purchase", "comprar", "login", "cookie", "publish", "publicar"))
            return AIProfileIds.CriticalReasoner;

        if (request.Capability == AIModelCapabilities.Intent || request.RiskLevel == AIRiskLevels.Low)
            return AIProfileIds.CheapIntent;

        return AIProfileIds.StandardTask;
    }

    private static bool IsHighRisk(AIModelRoutingRequest request)
    {
        return request.RiskLevel is AIRiskLevels.High or AIRiskLevels.Critical ||
               request.IsAmbiguous ||
               request.IsIrreversible;
    }

    private static AIModelProfile? ResolveEnabledProfile(AIModelProfile selected, AIModelRoutingPolicy policy, List<string> notes)
    {
        if (selected.Enabled)
            return selected;

        if (string.IsNullOrWhiteSpace(selected.FallbackProfileId))
            return null;

        var fallback = policy.Profiles.FirstOrDefault(profile =>
            string.Equals(profile.ProfileId, selected.FallbackProfileId, StringComparison.OrdinalIgnoreCase));

        if (fallback is { Enabled: true })
        {
            notes.Add($"selected profile disabled; using fallback {fallback.ProfileId}");
            return fallback;
        }

        return null;
    }

    private static string? ValidateConfig(AIModelProfile profile, AIModelRoutingRequest request, AIModelRoutingPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(profile.Provider))
            return $"profile {profile.ProfileId} missing provider";

        if (string.IsNullOrWhiteSpace(profile.Model))
            return $"profile {profile.ProfileId} missing model";

        if (policy.FailClosedOnMissingApiKey &&
            !string.Equals(profile.Provider, AIProviderKinds.Mock, StringComparison.OrdinalIgnoreCase) &&
            !profile.ApiKeyConfigured)
            return $"profile {profile.ProfileId} missing api key secret {profile.ApiKeySecretName}";

        if (request.EstimatedCalls > profile.MaxCallsPerTask)
            return $"profile {profile.ProfileId} maxCallsPerTask exceeded";

        if (profile.MaxCostPerTaskUsd > 0 && request.EstimatedCostUsd > profile.MaxCostPerTaskUsd)
            return $"profile {profile.ProfileId} maxCostPerTaskUsd exceeded";

        if (!RiskAllowed(request.RiskLevel, profile.MaxRiskLevel))
            return $"profile {profile.ProfileId} maxRiskLevel exceeded";

        return null;
    }

    private static string? ValidateBudget(AIModelProfile profile, AIModelRoutingRequest request, AIModelRoutingPolicy policy)
    {
        var usage = policy.Usage.FirstOrDefault(item => string.Equals(item.ProfileId, profile.ProfileId, StringComparison.OrdinalIgnoreCase))
                    ?? new AIModelUsageSnapshot(profile.ProfileId, 0, 0, 0);

        if (profile.DailyBudgetUsd > 0 && usage.DailySpendUsd + request.EstimatedCostUsd > profile.DailyBudgetUsd)
            return $"profile {profile.ProfileId} daily budget exceeded";

        if (profile.MonthlyBudgetUsd > 0 && usage.MonthlySpendUsd + request.EstimatedCostUsd > profile.MonthlyBudgetUsd)
            return $"profile {profile.ProfileId} monthly budget exceeded";

        return null;
    }

    private static bool RiskAllowed(string requestedRisk, string maxRisk)
    {
        return RiskRank(requestedRisk) <= RiskRank(maxRisk);
    }

    private static int RiskRank(string risk)
    {
        return risk?.ToLowerInvariant() switch
        {
            AIRiskLevels.Low => 1,
            AIRiskLevels.Medium => 2,
            AIRiskLevels.High => 3,
            AIRiskLevels.Critical => 4,
            _ => 4
        };
    }

    private static AIModelRouterResult Result(
        AIModelRoutingRequest request,
        AIModelProfile? profile,
        bool success,
        string status,
        string? fallbackProfileId,
        string reason,
        bool failClosed,
        IReadOnlyList<string> notes)
    {
        return new AIModelRouterResult(
            request,
            new AIModelRoutingDecision(
                Success: success,
                Status: status,
                SelectedProfileId: profile?.ProfileId,
                FallbackProfileId: fallbackProfileId,
                Reason: reason,
                FailClosed: failClosed,
                RequiresAuditLog: profile?.RequiresAuditLog ?? true,
                WouldCallProvider: false,
                Notes: notes),
            profile);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
