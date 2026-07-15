using System.Collections.Concurrent;

namespace OneBrain.Core.Models;

[Flags]
public enum ModelCapabilities
{
    None = 0,
    Chat = 1 << 0,
    ToolCalling = 1 << 1,
    StructuredOutput = 1 << 2,
    Streaming = 1 << 3,
    Vision = 1 << 4,
    Embeddings = 1 << 5,
    Reasoning = 1 << 6,
    Audio = 1 << 7
}

public enum ModelProviderKind
{
    Local,
    Cloud
}

public enum ModelProviderState
{
    Ready,
    Degraded,
    Unavailable
}

public enum ModelPrivacyClass
{
    LocalOnly = 0,
    PrivateCloud = 1,
    AuthorizedCloud = 2
}

public sealed record SecretReference(string StoreId, string SecretId)
{
    public override string ToString() => $"{StoreId}:[REDACTED]";
}

public sealed record ModelProviderDefinition(
    string ProviderId,
    string DisplayName,
    ModelProviderKind Kind,
    Uri Endpoint,
    bool RequiresCredential,
    IReadOnlyList<SecretReference> CredentialReferences,
    ModelProviderState State,
    int HealthScore,
    ModelPrivacyClass PrivacyClass,
    IReadOnlyList<string> Regions,
    int? RequestsPerMinute = null);

public sealed record ModelDefinition(
    string ModelId,
    string ProviderId,
    string UpstreamModelId,
    int ContextWindow,
    ModelCapabilities Capabilities,
    ModelPrivacyClass PrivacyClass,
    decimal InputCostPerMillion,
    decimal OutputCostPerMillion,
    int SpeedScore,
    int QualityScore,
    bool Available,
    string? HardwareRequirement = null,
    DateTimeOffset? DeprecatesAt = null);

public sealed record LogicalModelAlias(
    string Alias,
    ModelCapabilities RequiredCapabilities,
    bool PreferLocal,
    int MinimumSpeedScore,
    int MinimumQualityScore);

public sealed record ModelCatalogSnapshot(
    IReadOnlyList<ModelProviderDefinition> Providers,
    IReadOnlyList<ModelDefinition> Models,
    IReadOnlyList<LogicalModelAlias> Aliases);

public sealed class ModelCatalog
{
    private readonly ConcurrentDictionary<string, ModelProviderDefinition> _providers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ModelDefinition> _models =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, LogicalModelAlias> _aliases =
        new(StringComparer.OrdinalIgnoreCase);

    public ModelCatalog(
        IEnumerable<ModelProviderDefinition>? providers = null,
        IEnumerable<ModelDefinition>? models = null,
        IEnumerable<LogicalModelAlias>? aliases = null)
    {
        foreach (var provider in providers ?? Array.Empty<ModelProviderDefinition>())
            RegisterProvider(provider);
        foreach (var model in models ?? Array.Empty<ModelDefinition>())
            RegisterModel(model);
        foreach (var alias in aliases ?? Array.Empty<LogicalModelAlias>())
            RegisterAlias(alias);
    }

    public void RegisterProvider(ModelProviderDefinition provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ValidateProvider(provider);
        _providers[provider.ProviderId] = Normalize(provider);
    }

    public void RegisterModel(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ValidateModel(model);
        if (!_providers.ContainsKey(model.ProviderId))
            throw new InvalidOperationException($"Unknown provider '{model.ProviderId}'.");
        _models[ModelKey(model.ProviderId, model.ModelId)] = Normalize(model);
    }

    public void RegisterAlias(LogicalModelAlias alias)
    {
        ArgumentNullException.ThrowIfNull(alias);
        if (string.IsNullOrWhiteSpace(alias.Alias))
            throw new ArgumentException("Alias is required.", nameof(alias));
        if (alias.MinimumQualityScore is < 0 or > 100 || alias.MinimumSpeedScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(alias), "Alias scores must be between 0 and 100.");
        _aliases[alias.Alias.Trim()] = alias with { Alias = alias.Alias.Trim() };
    }

    public bool TryGetProvider(string providerId, out ModelProviderDefinition provider) =>
        _providers.TryGetValue(providerId, out provider!);

    public bool TryGetModel(string providerId, string modelId, out ModelDefinition model) =>
        _models.TryGetValue(ModelKey(providerId, modelId), out model!);

    public bool TryGetAlias(string alias, out LogicalModelAlias definition) =>
        _aliases.TryGetValue(alias, out definition!);

    public void UpdateProviderHealth(string providerId, ModelProviderState state, int healthScore)
    {
        if (healthScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(healthScore));
        if (!_providers.TryGetValue(providerId, out var existing))
            throw new KeyNotFoundException($"Unknown provider '{providerId}'.");
        _providers[providerId] = existing with { State = state, HealthScore = healthScore };
    }

    public ModelCatalogSnapshot Snapshot() =>
        new(
            Providers: _providers.Values.OrderBy(value => value.ProviderId, StringComparer.OrdinalIgnoreCase).ToArray(),
            Models: _models.Values
                .OrderBy(value => value.ProviderId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.ModelId, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Aliases: _aliases.Values.OrderBy(value => value.Alias, StringComparer.OrdinalIgnoreCase).ToArray());

    private static string ModelKey(string providerId, string modelId) =>
        $"{providerId.Trim()}::{modelId.Trim()}";

    private static void ValidateProvider(ModelProviderDefinition provider)
    {
        if (string.IsNullOrWhiteSpace(provider.ProviderId))
            throw new ArgumentException("Provider id is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(provider.DisplayName))
            throw new ArgumentException("Provider display name is required.", nameof(provider));
        if (provider.Endpoint is null || !provider.Endpoint.IsAbsoluteUri)
            throw new ArgumentException("Provider endpoint must be absolute.", nameof(provider));
        if (provider.HealthScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(provider), "Health score must be between 0 and 100.");
        if (provider.RequiresCredential && provider.CredentialReferences.Count == 0)
            throw new ArgumentException("Credential references are required for this provider.", nameof(provider));
        if (!provider.RequiresCredential && provider.CredentialReferences.Count != 0)
            throw new ArgumentException("Credential references must be empty for a credential-free provider.", nameof(provider));
    }

    private static void ValidateModel(ModelDefinition model)
    {
        if (string.IsNullOrWhiteSpace(model.ModelId) || string.IsNullOrWhiteSpace(model.ProviderId))
            throw new ArgumentException("Model and provider ids are required.", nameof(model));
        if (string.IsNullOrWhiteSpace(model.UpstreamModelId))
            throw new ArgumentException("Upstream model id is required.", nameof(model));
        if (model.ContextWindow < 1)
            throw new ArgumentOutOfRangeException(nameof(model), "Context window must be positive.");
        if (model.SpeedScore is < 0 or > 100 || model.QualityScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(model), "Model scores must be between 0 and 100.");
        if (model.InputCostPerMillion < 0 || model.OutputCostPerMillion < 0)
            throw new ArgumentOutOfRangeException(nameof(model), "Model costs cannot be negative.");
    }

    private static ModelProviderDefinition Normalize(ModelProviderDefinition provider) =>
        provider with
        {
            ProviderId = provider.ProviderId.Trim(),
            DisplayName = provider.DisplayName.Trim(),
            CredentialReferences = provider.CredentialReferences.Distinct().ToArray(),
            Regions = provider.Regions
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };

    private static ModelDefinition Normalize(ModelDefinition model) =>
        model with
        {
            ModelId = model.ModelId.Trim(),
            ProviderId = model.ProviderId.Trim(),
            UpstreamModelId = model.UpstreamModelId.Trim(),
            HardwareRequirement = string.IsNullOrWhiteSpace(model.HardwareRequirement)
                ? null
                : model.HardwareRequirement.Trim()
        };
}
