using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAssignmentReviewPersistenceMockService
{
    private static readonly DateTimeOffset FixtureTime = new(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);
    private readonly Dictionary<string, NodalOsAssignmentReviewSnapshot> snapshots = new(StringComparer.Ordinal);
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsAssignmentReviewSnapshot CreateSnapshot(
        NodalOsAssignmentUiPreview preview,
        NodalOsTaskGraphInteractionNoOpResult? interaction = null)
    {
        var selected = preview.ReviewPanel.SelectedWorkItemId;
        var state = new NodalOsAssignmentReviewState
        {
            SelectedWorkItemId = SafeValue(selected),
            ExpandedWorkItemIds = preview.WorkItems.Take(2).Select(item => SafeValue(item.WorkItemId)).ToArray(),
            CollapsedWorkItemIds = preview.WorkItems.Skip(2).Select(item => SafeValue(item.WorkItemId)).ToArray(),
            FiltersRedacted = ["status:draft", "risk:visible", "blockers:visible"],
            SortMode = NodalOsAssignmentReviewSortMode.OriginalDraftOrder,
            DraftNotesRedacted = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [SafeValue(selected)] = interaction?.UserFacingExplanationRedacted ?? "Draft note is review-only and not persisted productively."
            },
            NeedsReviewWorkItemIds = preview.WorkItems.Where(item => item.BlockersRedacted.Count > 0).Select(item => SafeValue(item.WorkItemId)).ToArray(),
            ExplanationRequestWorkItemIds = [SafeValue(selected)],
            ComparedWorkItemIds = preview.WorkItems.Take(2).Select(item => SafeValue(item.WorkItemId)).ToArray(),
            VisibleEvidenceRefs = preview.EvidenceRefs.Select(SafeValue).ToArray(),
            VisibleTimelineRefs = preview.TimelineRefs.Select(SafeValue).ToArray(),
            VisibleContextRefsRedacted = preview.ContextRefsRedacted.Select(SafeValue).ToArray()
        };

        return new()
        {
            Session = new()
            {
                ReviewSessionId = $"review-session-{SafeValue(preview.AssignmentUiPreviewId)}",
                AssignmentIdRef = SafeValue(preview.Header.AssignmentIdRef),
                MissionIdRef = SafeValue(preview.Header.MissionIdRef),
                CreatedAt = FixtureTime,
                DraftOnly = true,
                IsAuthoritative = false,
                MutatesRuntimeState = false,
                CanAuthorizeExecution = false
            },
            State = state,
            MockStorageOnly = true,
            ProductivePersistenceUsed = false,
            FilesystemUsed = false,
            CloudUsed = false,
            BrowserStorageUsed = false,
            ClipboardUsed = false,
            CreatesExecutionRequest = false,
            CreatesPrompt = false,
            RestoredInteractionsRemainNoOp = true
        };
    }

    public NodalOsAssignmentReviewSnapshot Store(NodalOsAssignmentReviewSnapshot snapshot)
    {
        snapshots[snapshot.Session.ReviewSessionId] = snapshot;
        return snapshot;
    }

    public NodalOsAssignmentReviewSnapshot? Get(string reviewSessionId) =>
        snapshots.TryGetValue(reviewSessionId, out var snapshot) ? snapshot : null;

    public NodalOsAssignmentReviewRehydrationResult Rehydrate(string reviewSessionId)
    {
        var snapshot = Get(reviewSessionId) ?? throw new InvalidOperationException("Mock review session not found.");
        return new()
        {
            ReviewSessionId = snapshot.Session.ReviewSessionId,
            RestoredState = snapshot.State,
            DraftOnly = true,
            IsAuthoritative = false,
            CanAuthorizeExecution = false,
            CreatesExecutionRequest = false,
            NotesCanBecomePrompts = false,
            RestoredInteractionsRemainNoOp = true
        };
    }

    public void Clear() => snapshots.Clear();

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed class NodalOsAssignmentReviewPersistenceMockJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeSnapshot(NodalOsAssignmentReviewSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, Options);

    public string SerializeRehydration(NodalOsAssignmentReviewRehydrationResult result) =>
        JsonSerializer.Serialize(result, Options);
}
