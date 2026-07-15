using OneBrain.Core.Models;

namespace OneBrain.Core.AI;

public sealed record AIModelRuntimeAuthorization(
    bool LocalOnly,
    bool CloudAllowed,
    ModelPrivacyClass MaximumPrivacyClass,
    decimal RemainingBudget,
    decimal MaximumInputCostPerMillion,
    decimal MaximumOutputCostPerMillion,
    IReadOnlyCollection<string> AllowedProviderIds)
{
    public static AIModelRuntimeAuthorization Default { get; } = new(
        LocalOnly: false,
        CloudAllowed: true,
        MaximumPrivacyClass: ModelPrivacyClass.AuthorizedCloud,
        RemainingBudget: 10m,
        MaximumInputCostPerMillion: 1_000m,
        MaximumOutputCostPerMillion: 1_000m,
        AllowedProviderIds: Array.Empty<string>());
}

public sealed record AIModelRuntimeBridgeResult(
    bool Success,
    bool Cancelled,
    bool RequiresOperatorIntervention,
    AIModelRouterResult LegacyRouting,
    ModelRoutePlan? RuntimePlan,
    ModelRoutingResult? RuntimeRouting,
    string Decision,
    string SafeMessage);

public static class AIModelRuntimeCatalogFactory
{
    public static ModelCatalog Build(
        IReadOnlyList<AIModelProfile> profiles,
        IReadOnlyDictionary<string, Uri>? providerEndpoints = null,
        string secretStoreId = EnvironmentSecretReferenceStore.StoreId)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretStoreId);

        var eligible = profiles
            .Where(profile => profile.Enabled && !string.IsNullOrWhiteSpace(profile.Provider) && !string.IsNullOrWhiteSpace(profile.Model))
            .ToArray();
        var providers = new List<ModelProviderDefinition>();
        var models = new List<ModelDefinition>();

        foreach (var group in eligible.GroupBy(profile => profile.Provider.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var providerId = group.Key.ToLowerInvariant();
            var kind = ProviderKind(providerId);
            var credentialReferences = group
                .Where(profile => profile.ApiKeyConfigured)
                .Select(profile => new SecretReference(secretStoreId, profile.ApiKeySecretName))
                .Distinct()
                .ToArray();
            var requiresCredential = kind == ModelProviderKind.Cloud &&
                !string.Equals(providerId, AIProviderKinds.Mock, StringComparison.OrdinalIgnoreCase);
            if (requiresCredential && credentialReferences.Length == 0)
                continue;

            providers.Add(new ModelProviderDefinition(
                ProviderId: providerId,
                DisplayName: group.First().Provider,
                Kind: kind,
                Endpoint: ResolveEndpoint(providerId, providerEndpoints),
                RequiresCredential: requiresCredential,
                CredentialReferences: requiresCredential ? credentialReferences : Array.Empty<SecretReference>(),
                State: ModelProviderState.Ready,
                HealthScore: 100,
                PrivacyClass: kind == ModelProviderKind.Local
                    ? ModelPrivacyClass.LocalOnly
                    : ModelPrivacyClass.AuthorizedCloud,
                Regions: kind == ModelProviderKind.Local ? ["local"] : ["configured"]));

            foreach (var profile in group)
            {
                models.Add(new ModelDefinition(
                    ModelId: profile.ProfileId,
                    ProviderId: providerId,
                    UpstreamModelId: profile.Model,
                    ContextWindow: 128_000,
                    Capabilities: MapCapabilities(profile.Capabilities),
                    PrivacyClass: kind == ModelProviderKind.Local
                        ? ModelPrivacyClass.LocalOnly
                        : ModelPrivacyClass.AuthorizedCloud,
                    InputCostPerMillion: 0,
                    OutputCostPerMillion: 0,
                    SpeedScore: SpeedScore(profile.ProfileKind),
                    QualityScore: QualityScore(profile.ProfileKind),
                    Available: true));
            }
        }

        var aliases = eligible
            .GroupBy(profile => profile.ProfileKind, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LogicalModelAlias(
                Alias: group.Key,
                RequiredCapabilities: MapCapabilities(group.SelectMany(profile => profile.Capabilities)),
                PreferLocal: group.All(profile => ProviderKind(profile.Provider) == ModelProviderKind.Local),
                MinimumSpeedScore: 0,
                MinimumQualityScore: 0))
            .ToArray();

        return new ModelCatalog(providers, models, aliases);
    }

    public static ModelCapabilities MapCapabilities(IEnumerable<string> capabilities)
    {
        var result = ModelCapabilities.Chat;
        foreach (var capability in capabilities)
        {
            result |= capability switch
            {
                AIModelCapabilities.StandardTask => ModelCapabilities.StructuredOutput,
                AIModelCapabilities.CriticalReasoning => ModelCapabilities.Reasoning,
                AIModelCapabilities.VisionVerification => ModelCapabilities.Vision,
                _ => ModelCapabilities.None
            };
        }
        return result;
    }

    private static ModelProviderKind ProviderKind(string providerId) =>
        providerId.Equals(AIProviderKinds.Mock, StringComparison.OrdinalIgnoreCase) ||
        providerId.Contains("local", StringComparison.OrdinalIgnoreCase) ||
        providerId.Contains("ollama", StringComparison.OrdinalIgnoreCase) ||
        providerId.Contains("lmstudio", StringComparison.OrdinalIgnoreCase)
            ? ModelProviderKind.Local
            : ModelProviderKind.Cloud;

    private static Uri ResolveEndpoint(
        string providerId,
        IReadOnlyDictionary<string, Uri>? providerEndpoints)
    {
        if (providerEndpoints is not null && providerEndpoints.TryGetValue(providerId, out var configured))
            return configured;
        if (providerId.Equals(AIProviderKinds.OpenAI, StringComparison.OrdinalIgnoreCase))
            return new Uri("https://api.openai.com/v1");
        if (ProviderKind(providerId) == ModelProviderKind.Local)
            return new Uri("http://127.0.0.1");
        return new Uri($"https://{Uri.EscapeDataString(providerId)}.invalid");
    }

    private static int SpeedScore(string profileKind) => profileKind switch
    {
        AIProfileKinds.CheapIntent => 95,
        AIProfileKinds.StandardTask => 75,
        AIProfileKinds.VisionVerifier => 55,
        AIProfileKinds.CriticalReasoner => 45,
        _ => 60
    };

    private static int QualityScore(string profileKind) => profileKind switch
    {
        AIProfileKinds.CheapIntent => 55,
        AIProfileKinds.StandardTask => 75,
        AIProfileKinds.VisionVerifier => 85,
        AIProfileKinds.CriticalReasoner => 95,
        _ => 60
    };
}

public sealed class AIModelRuntimeBridge
{
    private readonly AIModelRouter _legacyRouter;
    private readonly PolicyAwareModelRouter _runtimeRouter;

    public AIModelRuntimeBridge(
        PolicyAwareModelRouter runtimeRouter,
        AIModelRouter? legacyRouter = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeRouter);
        _runtimeRouter = runtimeRouter;
        _legacyRouter = legacyRouter ?? new AIModelRouter();
    }

    public AIModelRuntimeBridgeResult Plan(
        AIModelRoutingRequest request,
        AIModelRoutingPolicy policy,
        AIModelRuntimeAuthorization? authorization = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policy);
        authorization ??= AIModelRuntimeAuthorization.Default;

        var legacy = _legacyRouter.Route(request, policy);
        if (!legacy.Decision.Success || legacy.Profile is null)
            return Blocked(legacy, legacy.Decision.Status, legacy.Decision.Reason);

        var routeRequest = BuildRouteRequest(request, policy, legacy.Profile, authorization);
        var plan = _runtimeRouter.Plan(routeRequest);
        return plan.IsRoutable
            ? new AIModelRuntimeBridgeResult(
                Success: true,
                Cancelled: false,
                RequiresOperatorIntervention: false,
                LegacyRouting: legacy,
                RuntimePlan: plan,
                RuntimeRouting: null,
                Decision: "MODEL_RUNTIME_PLAN_READY",
                SafeMessage: "A compatible model route is ready within the authorized scope.")
            : new AIModelRuntimeBridgeResult(
                Success: false,
                Cancelled: false,
                RequiresOperatorIntervention: true,
                LegacyRouting: legacy,
                RuntimePlan: plan,
                RuntimeRouting: null,
                Decision: "MODEL_RUNTIME_PLAN_BLOCKED",
                SafeMessage: plan.BlockedReason ?? "No compatible runtime model route is available.");
    }

    public async ValueTask<AIModelRuntimeBridgeResult> ExecuteAsync(
        AIModelRoutingRequest request,
        AIModelRoutingPolicy policy,
        ModelExecutionRequest executionRequest,
        AIModelRuntimeAuthorization? authorization = null,
        ModelFallbackPolicy? fallbackPolicy = null,
        CancellationToken cancellationToken = default)
    {
        var planned = Plan(request, policy, authorization);
        if (!planned.Success || planned.LegacyRouting.Profile is null || planned.RuntimePlan is null)
            return planned;

        authorization ??= AIModelRuntimeAuthorization.Default;
        var routeRequest = BuildRouteRequest(request, policy, planned.LegacyRouting.Profile, authorization);
        var runtime = await _runtimeRouter.ExecuteAsync(
            routeRequest,
            executionRequest,
            fallbackPolicy,
            cancellationToken).ConfigureAwait(false);

        return new AIModelRuntimeBridgeResult(
            Success: runtime.Success,
            Cancelled: runtime.Cancelled,
            RequiresOperatorIntervention: runtime.RequiresOperatorIntervention,
            LegacyRouting: planned.LegacyRouting,
            RuntimePlan: planned.RuntimePlan,
            RuntimeRouting: runtime,
            Decision: runtime.Decision,
            SafeMessage: runtime.SafeMessage);
    }

    private static ModelRouteRequest BuildRouteRequest(
        AIModelRoutingRequest request,
        AIModelRoutingPolicy policy,
        AIModelProfile selected,
        AIModelRuntimeAuthorization authorization)
    {
        var allowedProviders = authorization.AllowedProviderIds.Count > 0
            ? authorization.AllowedProviderIds
            : ResolveAuthorizedProviders(selected, policy.Profiles);

        return new ModelRouteRequest(
            LogicalModel: selected.ProfileKind,
            RequiredCapabilities: AIModelRuntimeCatalogFactory.MapCapabilities(selected.Capabilities),
            RequiredContextWindow: 1,
            LocalOnly: authorization.LocalOnly,
            CloudAllowed: authorization.CloudAllowed,
            MaximumPrivacyClass: authorization.MaximumPrivacyClass,
            MaximumInputCostPerMillion: authorization.MaximumInputCostPerMillion,
            MaximumOutputCostPerMillion: authorization.MaximumOutputCostPerMillion,
            RemainingBudget: authorization.RemainingBudget,
            AllowedProviderIds: allowedProviders,
            PreferSpeed: selected.ProfileKind is AIProfileKinds.CheapIntent or AIProfileKinds.StandardTask,
            PreferQuality: selected.ProfileKind is AIProfileKinds.CriticalReasoner or AIProfileKinds.VisionVerifier);
    }

    private static IReadOnlyCollection<string> ResolveAuthorizedProviders(
        AIModelProfile selected,
        IReadOnlyList<AIModelProfile> profiles)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = selected;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (visited.Add(current.ProfileId))
        {
            if (!string.IsNullOrWhiteSpace(current.Provider))
                result.Add(current.Provider.Trim().ToLowerInvariant());
            if (string.IsNullOrWhiteSpace(current.FallbackProfileId))
                break;
            var fallback = profiles.FirstOrDefault(profile =>
                string.Equals(profile.ProfileId, current.FallbackProfileId, StringComparison.OrdinalIgnoreCase));
            if (fallback is null)
                break;
            current = fallback;
        }
        return result.ToArray();
    }

    private static AIModelRuntimeBridgeResult Blocked(
        AIModelRouterResult legacy,
        string decision,
        string reason) =>
        new(
            Success: false,
            Cancelled: false,
            RequiresOperatorIntervention: true,
            LegacyRouting: legacy,
            RuntimePlan: null,
            RuntimeRouting: null,
            Decision: decision,
            SafeMessage: reason);
}
