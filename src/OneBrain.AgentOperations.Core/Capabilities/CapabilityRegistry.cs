using System.Collections.Concurrent;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Capabilities;

public enum CapabilityState
{
    Announced,
    Ready,
    Degraded,
    Unavailable,
    Withdrawn
}

public enum CapabilityRuntime
{
    Core,
    Browser,
    Desktop,
    Filesystem,
    Terminal,
    Model,
    Evidence,
    Verification
}

public sealed record CapabilityRecord(
    string CapabilityId,
    string ProviderId,
    string? InstanceId,
    CapabilityRuntime Runtime,
    CapabilityState State,
    int HealthScore,
    string? Version,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record CapabilitySelectionRequest(
    string CapabilityId,
    IReadOnlyCollection<string> AllowedProviderIds,
    IReadOnlyCollection<CapabilityRuntime> AllowedRuntimes,
    bool AllowDegraded = true,
    string? PreferredProviderId = null);

public sealed record CapabilitySelection(
    bool Available,
    CapabilityRecord? Selected,
    IReadOnlyList<CapabilityRecord> Fallbacks,
    string Decision,
    string SafeMessage);

public sealed class CapabilityRegistry
{
    private readonly ConcurrentDictionary<string, CapabilityRecord> _records = new(StringComparer.Ordinal);

    public void Register(CapabilityRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        Validate(record);
        _records[Key(record.CapabilityId, record.ProviderId, record.InstanceId)] = Sanitize(record);
    }

    public bool TryGet(
        string capabilityId,
        string providerId,
        string? instanceId,
        out CapabilityRecord record) =>
        _records.TryGetValue(Key(capabilityId, providerId, instanceId), out record!);

    public IReadOnlyList<CapabilityRecord> List(string? capabilityId = null) =>
        _records.Values
            .Where(record => capabilityId is null ||
                string.Equals(record.CapabilityId, capabilityId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(record => record.CapabilityId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public void UpdateHealth(
        string capabilityId,
        string providerId,
        string? instanceId,
        CapabilityState state,
        int healthScore)
    {
        if (healthScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(healthScore));
        var key = Key(capabilityId, providerId, instanceId);
        if (!_records.TryGetValue(key, out var existing))
            throw new KeyNotFoundException($"Capability '{capabilityId}' from provider '{providerId}' is not registered.");
        if (existing.State == CapabilityState.Withdrawn)
            throw new InvalidOperationException("A withdrawn capability cannot be reactivated without registration.");
        _records[key] = existing with { State = state, HealthScore = healthScore };
    }

    public bool Withdraw(string capabilityId, string providerId, string? instanceId)
    {
        var key = Key(capabilityId, providerId, instanceId);
        if (!_records.TryGetValue(key, out var existing))
            return false;
        _records[key] = existing with { State = CapabilityState.Withdrawn, HealthScore = 0 };
        return true;
    }

    public CapabilitySelection Select(CapabilitySelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CapabilityId);

        HashSet<string>? allowedProviders = request.AllowedProviderIds.Count == 0
            ? null
            : new HashSet<string>(request.AllowedProviderIds, StringComparer.OrdinalIgnoreCase);
        HashSet<CapabilityRuntime>? allowedRuntimes = request.AllowedRuntimes.Count == 0
            ? null
            : new HashSet<CapabilityRuntime>(request.AllowedRuntimes);

        var candidates = _records.Values
            .Where(record => string.Equals(record.CapabilityId, request.CapabilityId, StringComparison.OrdinalIgnoreCase))
            .Where(record => record.State == CapabilityState.Ready ||
                request.AllowDegraded && record.State == CapabilityState.Degraded)
            .Where(record => allowedProviders is null || allowedProviders.Contains(record.ProviderId))
            .Where(record => allowedRuntimes is null || allowedRuntimes.Contains(record.Runtime))
            .OrderByDescending(record => request.PreferredProviderId is not null &&
                string.Equals(record.ProviderId, request.PreferredProviderId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(record => record.State == CapabilityState.Ready ? 0 : 1)
            .ThenByDescending(record => record.HealthScore)
            .ThenBy(record => record.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new CapabilitySelection(
                false,
                null,
                Array.Empty<CapabilityRecord>(),
                "CAPABILITY_UNAVAILABLE",
                $"Capability '{SafeRuntimeText.Sanitize(request.CapabilityId, 120)}' is not currently available.");
        }

        return new CapabilitySelection(
            true,
            candidates[0],
            candidates.Skip(1).ToArray(),
            candidates[0].State == CapabilityState.Degraded
                ? "CAPABILITY_DEGRADED_SELECTED"
                : "CAPABILITY_READY_SELECTED",
            candidates[0].State == CapabilityState.Degraded
                ? "A compatible degraded capability was selected; the mission may continue within policy."
                : "A compatible ready capability was selected.");
    }

    private static string Key(string capabilityId, string providerId, string? instanceId) =>
        $"{capabilityId.Trim()}::{providerId.Trim()}::{instanceId?.Trim() ?? string.Empty}";

    private static void Validate(CapabilityRecord record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(record.CapabilityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.ProviderId);
        if (record.HealthScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(record), "Capability health score must be between 0 and 100.");
    }

    private static CapabilityRecord Sanitize(CapabilityRecord record) =>
        record with
        {
            CapabilityId = SafeRuntimeText.Sanitize(record.CapabilityId, 120),
            ProviderId = SafeRuntimeText.Sanitize(record.ProviderId, 120),
            InstanceId = string.IsNullOrWhiteSpace(record.InstanceId)
                ? null
                : SafeRuntimeText.Sanitize(record.InstanceId, 120),
            Version = string.IsNullOrWhiteSpace(record.Version)
                ? null
                : SafeRuntimeText.Sanitize(record.Version, 80),
            Metadata = SafeRuntimeText.SanitizeDimensions(record.Metadata.Select(pair =>
                new KeyValuePair<string, string?>(pair.Key, pair.Value)))
        };
}
