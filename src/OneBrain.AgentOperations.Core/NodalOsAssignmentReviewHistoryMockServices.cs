using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAssignmentReviewHistoryMockService
{
    private static readonly DateTimeOffset FixtureTime = new(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);
    private readonly List<NodalOsAssignmentReviewHistoryEntry> entries = [];
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsAssignmentReviewHistoryEntry CreateEntry(
        NodalOsAssignmentReviewSnapshot snapshot,
        NodalOsPlannerHandoffPack? handoff = null,
        string label = "review snapshot")
    {
        var index = entries.Count + 1;
        return new()
        {
            HistoryEntryId = $"history-entry-{index:000}-{SafeValue(snapshot.Session.ReviewSessionId)}",
            ReviewSessionId = SafeValue(snapshot.Session.ReviewSessionId),
            MissionIdRef = SafeValue(snapshot.Session.MissionIdRef),
            AssignmentIdRef = SafeValue(snapshot.Session.AssignmentIdRef),
            HandoffIdRef = handoff is null ? null : SafeValue(handoff.HandoffPackId),
            SnapshotLabelRedacted = SafeValue(label),
            CreatedAt = FixtureTime.AddMinutes(index),
            DraftOnly = true,
            IsAuthoritative = false,
            IsMockOnly = true,
            CanRestoreAsAuthoritative = false,
            CanAuthorizeExecution = false,
            CanTriggerPlanner = false,
            CanTriggerRuntime = false,
            CanTriggerLlm = false,
            CanAccessFilesystem = false
        };
    }

    public NodalOsAssignmentReviewHistoryCollection Store(NodalOsAssignmentReviewHistoryEntry entry)
    {
        entries.Add(entry);
        return Collection();
    }

    public NodalOsAssignmentReviewHistoryCollection Collection()
    {
        var latest = entries.LastOrDefault();
        var previous = entries.Count > 1 ? entries[^2] : null;
        return new()
        {
            HistoryCollectionId = "assignment-review-history-mock",
            Entries = entries.ToArray(),
            LatestEntry = latest,
            PreviousEntry = previous,
            VisibleLabelsRedacted = entries.Select(entry => SafeValue(entry.SnapshotLabelRedacted)).ToArray(),
            DiffCandidateRefs = entries.Select(entry => SafeValue(entry.HistoryEntryId)).ToArray(),
            EvidenceRefs = ["evidence-history-ref-only"],
            TimelineRefs = ["timeline-history-ref-only"],
            GuardrailRefs = ["guardrail-history-mock-only", "guardrail-restore-visual-only"],
            MockStoreOnly = true,
            ProductivePersistenceUsed = false,
            FilesystemUsed = false,
            CloudUsed = false,
            BrowserStorageUsed = false,
            UsageMetricsUsed = false,
            ClipboardUsed = false
        };
    }

    public NodalOsAssignmentReviewHistoryRestoreResult Restore(string historyEntryId)
    {
        var entry = entries.SingleOrDefault(item => item.HistoryEntryId == historyEntryId)
            ?? throw new InvalidOperationException("Mock history entry not found.");

        return new()
        {
            HistoryEntryId = entry.HistoryEntryId,
            ReviewSessionId = entry.ReviewSessionId,
            UserFacingExplanationRedacted = "Restore is visual/mock only and cannot make the review authoritative.",
            VisualMockOnly = true,
            DraftOnly = true,
            IsAuthoritative = false,
            CreatesExecutionRequest = false,
            CreatesPrompt = false,
            CallsPlanner = false,
            CallsLlm = false,
            CallsRuntime = false,
            MutatesFilesystem = false
        };
    }

    public void Clear() => entries.Clear();

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed class NodalOsAssignmentReviewHistoryMockJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeEntry(NodalOsAssignmentReviewHistoryEntry entry) =>
        JsonSerializer.Serialize(entry, Options);

    public string SerializeCollection(NodalOsAssignmentReviewHistoryCollection collection) =>
        JsonSerializer.Serialize(collection, Options);

    public string SerializeRestore(NodalOsAssignmentReviewHistoryRestoreResult restore) =>
        JsonSerializer.Serialize(restore, Options);
}
