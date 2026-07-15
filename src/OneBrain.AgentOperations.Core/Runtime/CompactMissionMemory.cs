using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Runtime;

public enum CompactMissionMemoryStatus
{
    Active,
    Blocked,
    Done
}

public sealed record CompactMissionArtifact(string Kind, string Label, string Reference);

public sealed record CompactMissionMemory(
    CompactMissionMemoryStatus Status,
    string? Goal,
    string? CurrentStep,
    IReadOnlyList<string> ConfirmedFacts,
    IReadOnlyList<CompactMissionArtifact> Artifacts,
    IReadOnlyList<string> Blockers,
    string? NextStep,
    IReadOnlyList<string> WorkingAssumptions,
    IReadOnlyList<string> CompletionCriteria,
    string? LastFailureReason,
    DateTimeOffset UpdatedAt,
    string SourceEventId);

public sealed record CompactMissionMemoryUpdate(
    CompactMissionMemoryStatus? Status = null,
    string? Goal = null,
    string? CurrentStep = null,
    IReadOnlyList<string>? ConfirmedFacts = null,
    IReadOnlyList<CompactMissionArtifact>? Artifacts = null,
    IReadOnlyList<string>? Blockers = null,
    string? NextStep = null,
    IReadOnlyList<string>? WorkingAssumptions = null,
    IReadOnlyList<string>? CompletionCriteria = null,
    string? LastFailureReason = null,
    bool NewMission = false,
    bool ClearBlockers = false,
    bool ClearNextStep = false,
    bool ClearLastFailure = false);

public static class CompactMissionMemoryProjector
{
    public const int ConfirmedFactsLimit = 10;
    public const int ArtifactsLimit = 8;
    public const int BlockersLimit = 5;
    public const int WorkingAssumptionsLimit = 6;
    public const int CompletionCriteriaLimit = 6;

    public static CompactMissionMemory Empty(string sourceEventId) =>
        new(
            CompactMissionMemoryStatus.Active,
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<CompactMissionArtifact>(),
            Array.Empty<string>(),
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            DateTimeOffset.UtcNow,
            SafeRuntimeText.Sanitize(sourceEventId, 120));

    public static CompactMissionMemory Apply(
        CompactMissionMemory? existing,
        CompactMissionMemoryUpdate update,
        string sourceEventId,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEventId);

        var basis = update.NewMission || existing is null ? Empty(sourceEventId) : existing;
        var result = basis with
        {
            Status = update.Status ?? basis.Status,
            Goal = PreferNonEmpty(update.Goal, basis.Goal),
            CurrentStep = PreferNonEmpty(update.CurrentStep, basis.CurrentStep),
            ConfirmedFacts = MergeStrings(basis.ConfirmedFacts, update.ConfirmedFacts, ConfirmedFactsLimit),
            Artifacts = MergeArtifacts(basis.Artifacts, update.Artifacts, ArtifactsLimit),
            Blockers = update.ClearBlockers
                ? MergeStrings(Array.Empty<string>(), update.Blockers, BlockersLimit)
                : MergeStrings(basis.Blockers, update.Blockers, BlockersLimit),
            NextStep = update.ClearNextStep ? null : PreferNonEmpty(update.NextStep, basis.NextStep),
            WorkingAssumptions = MergeStrings(basis.WorkingAssumptions, update.WorkingAssumptions, WorkingAssumptionsLimit),
            CompletionCriteria = MergeStrings(basis.CompletionCriteria, update.CompletionCriteria, CompletionCriteriaLimit),
            LastFailureReason = update.ClearLastFailure
                ? null
                : PreferNonEmpty(update.LastFailureReason, basis.LastFailureReason),
            UpdatedAt = now ?? DateTimeOffset.UtcNow,
            SourceEventId = SafeRuntimeText.Sanitize(sourceEventId, 120)
        };

        if (result.Status == CompactMissionMemoryStatus.Done)
        {
            result = result with
            {
                Blockers = Array.Empty<string>(),
                NextStep = null,
                LastFailureReason = null
            };
        }

        return result;
    }

    public static bool IsVisible(CompactMissionMemory? memory) =>
        memory is not null &&
        (!string.IsNullOrWhiteSpace(memory.Goal) ||
         !string.IsNullOrWhiteSpace(memory.CurrentStep) ||
         memory.ConfirmedFacts.Count > 0 ||
         memory.Artifacts.Count > 0 ||
         memory.Blockers.Count > 0 ||
         !string.IsNullOrWhiteSpace(memory.NextStep) ||
         memory.Status is CompactMissionMemoryStatus.Blocked or CompactMissionMemoryStatus.Done);

    private static string? PreferNonEmpty(string? next, string? existing)
    {
        var sanitized = SafeRuntimeText.Sanitize(next);
        return sanitized.Length == 0 ? existing : sanitized;
    }

    private static IReadOnlyList<string> MergeStrings(
        IReadOnlyList<string> existing,
        IReadOnlyList<string>? next,
        int limit)
    {
        if (next is null)
            return existing;

        return existing.Concat(next)
            .Select(value => SafeRuntimeText.Sanitize(value))
            .Where(value => value.Length > 0)
            .Reverse()
            .Distinct(StringComparer.Ordinal)
            .Take(limit)
            .Reverse()
            .ToArray();
    }

    private static IReadOnlyList<CompactMissionArtifact> MergeArtifacts(
        IReadOnlyList<CompactMissionArtifact> existing,
        IReadOnlyList<CompactMissionArtifact>? next,
        int limit)
    {
        if (next is null)
            return existing;

        var map = new Dictionary<string, CompactMissionArtifact>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var artifact in existing.Concat(next))
        {
            var sanitized = Sanitize(artifact);
            if (sanitized.Reference.Length == 0)
                continue;
            var key = $"{sanitized.Kind}::{sanitized.Reference}";
            if (!map.ContainsKey(key))
                order.Add(key);
            map[key] = sanitized;
        }

        return order.TakeLast(limit).Select(key => map[key]).ToArray();
    }

    private static CompactMissionArtifact Sanitize(CompactMissionArtifact artifact) =>
        new(
            SafeRuntimeText.Sanitize(artifact.Kind, 40),
            SafeRuntimeText.Sanitize(artifact.Label, 120),
            SafeRuntimeText.Sanitize(artifact.Reference, 160));
}
