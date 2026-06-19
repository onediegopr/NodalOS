using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsMissionControlInteractionValidator
{
    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsMissionControlInteractionValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsMissionControlInteractionValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsMissionControlInteractionValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsCoreRuntimeValidationResult ValidateIntent(NodalOsMissionControlUiIntent intent)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, intent.IntentId, "IntentId is required.");
        if (intent.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoOpAuthority(
            intent.IsNoOp,
            intent.CanAuthorizeExecution,
            intent.RuntimeExecutionAllowed,
            intent.RuntimeExecutionDeferred,
            errors);
        if (intent.RequiresPositiveExecutionGate)
            errors.Add("Mission Control UI intents must not require or invoke the positive execution gate in M483-M485.");

        ValidateSafeText(errors, "ActorRedacted", intent.ActorRedacted);
        ValidateSafeText(errors, "NoteRedacted", intent.NoteRedacted);
        foreach (var (key, value) in intent.MetadataRedacted)
            ValidateSafeField(errors, key, value);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateNoOpEvent(NodalOsMissionControlNoOpEvent noOpEvent)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, noOpEvent.UiEventId, "UiEventId is required.");
        AddRequired(errors, noOpEvent.IntentId, "IntentId is required.");
        AddRequired(errors, noOpEvent.SummaryRedacted, "SummaryRedacted is required.");
        if (noOpEvent.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoOpAuthority(
            noOpEvent.IsNoOp,
            noOpEvent.CanAuthorizeExecution,
            noOpEvent.RuntimeExecutionAllowed,
            noOpEvent.RuntimeExecutionDeferred,
            errors);
        if (noOpEvent.RequiresPositiveExecutionGate)
            errors.Add("Mission Control no-op events must not require or invoke the positive execution gate in M483-M485.");

        ValidateSafeText(errors, "SummaryRedacted", noOpEvent.SummaryRedacted);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateDecisionDraft(NodalOsApprovalDecisionDraft draft)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, draft.DraftId, "DraftId is required.");
        AddRequired(errors, draft.ApprovalCardId, "ApprovalCardId is required.");
        if (draft.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (draft.UpdatedAt == default)
            errors.Add("UpdatedAt is required.");

        ValidateNoOpAuthority(
            draft.IsNoOp,
            draft.CanAuthorizeExecution,
            draft.RuntimeExecutionAllowed,
            draft.RuntimeExecutionDeferred,
            errors);
        if (!draft.RequiresPositiveExecutionGateForFutureExecution)
            errors.Add("Approval decision drafts must require a positive gate for any future execution.");

        ValidateSafeText(errors, "UserNoteRedacted", draft.UserNoteRedacted);
        ValidateSafeText(errors, "ReasonRedacted", draft.ReasonRedacted);
        ValidateSafeText(errors, "RequestedChangesRedacted", draft.RequestedChangesRedacted);
        ValidateSafeText(errors, "RequestExplanationRedacted", draft.RequestExplanationRedacted);
        ValidateSafeText(errors, "DeferReasonRedacted", draft.DeferReasonRedacted);
        ValidateEvidenceRefs(draft.EvidenceRefs, errors, warnings);
        foreach (var timelineEntryId in draft.TimelineEntryIds)
            ValidateSafeText(errors, "TimelineEntryIds", timelineEntryId);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateUiState(NodalOsMissionControlUiState state)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, state.StateId, "StateId is required.");
        if (state.UpdatedAt == default)
            errors.Add("UpdatedAt is required.");
        if (!state.ReadOnlyUi)
            errors.Add("Mission Control UI state must remain read-only.");
        if (state.CanAuthorizeExecution)
            errors.Add("Mission Control UI state cannot authorize execution.");
        if (state.RuntimeExecutionAllowed)
            errors.Add("Mission Control UI state cannot allow runtime execution.");
        if (!state.RuntimeExecutionDeferred)
            errors.Add("Mission Control UI state must keep runtime execution deferred.");
        if (!state.MockPersistenceOnly)
            errors.Add("Mission Control UI state persistence must be mock-only in M483-M485.");
        if (state.CloudPersistenceAllowed)
            errors.Add("Mission Control UI state cannot allow cloud persistence.");
        if (state.ProductiveDatabasePersistenceAllowed)
            errors.Add("Mission Control UI state cannot allow productive DB persistence.");

        ValidateSafeText(errors, "SelectedMissionId", state.SelectedMissionId);
        ValidateSafeText(errors, "SelectedTimelineEntryId", state.SelectedTimelineEntryId);
        ValidateSafeText(errors, "SelectedApprovalCardId", state.SelectedApprovalCardId);
        ValidateSafeText(errors, "SelectedEvidenceId", state.SelectedEvidenceId);
        foreach (var entryId in state.ExpandedTimelineEntryIds)
            ValidateSafeText(errors, "ExpandedTimelineEntryIds", entryId);
        foreach (var warningId in state.DismissedWarningIds)
            ValidateSafeText(errors, "DismissedWarningIds", warningId);
        foreach (var (key, value) in state.ActiveFiltersRedacted)
            ValidateSafeField(errors, key, value);
        foreach (var (key, _) in state.PanelCollapsed)
            ValidateSafeText(errors, "PanelCollapsed", key);

        return Result(errors, warnings);
    }

    public string Redact(string? value) => redaction.RedactValue(value).Value;

    public IReadOnlyDictionary<string, string> RedactMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        var redacted = redaction.RedactDictionary(metadata).Values;
        var sanitized = new Dictionary<string, string>(StringComparer.Ordinal);
        var sensitiveIndex = 0;

        foreach (var (key, value) in redacted)
        {
            var safeKey = redaction.ContainsSensitiveField(key, value) || redaction.ContainsSensitiveContent(key)
                ? $"redactedMetadata{sensitiveIndex++}"
                : key;
            sanitized[safeKey] = value;
        }

        return sanitized;
    }

    private void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
        {
            var bridgeResult = evidenceBridge.ValidateBridgeRef(evidenceRef);
            errors.AddRange(bridgeResult.Errors.Select(Redact));
            warnings.AddRange(bridgeResult.Warnings.Select(Redact));
        }
    }

    private static void ValidateNoOpAuthority(
        bool isNoOp,
        bool canAuthorizeExecution,
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        List<string> errors)
    {
        if (!isNoOp)
            errors.Add("Mission Control interactions must be no-op.");
        if (canAuthorizeExecution)
            errors.Add("Mission Control interactions cannot authorize execution.");
        if (runtimeExecutionAllowed)
            errors.Add("Mission Control interactions cannot allow runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("Mission Control interactions must keep runtime execution deferred.");
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before Mission Control interaction persistence.");
    }

    private void ValidateSafeField(List<string> errors, string fieldName, string? value)
    {
        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"Metadata field {fieldName} contains sensitive content and must be redacted before Mission Control interaction persistence.");
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsCoreRuntimeValidationResult Result(List<string> errors, List<string> warnings) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsMissionControlInteractionService
{
    private readonly NodalOsMissionControlInteractionValidator validator;

    public NodalOsMissionControlInteractionService()
        : this(new NodalOsMissionControlInteractionValidator())
    {
    }

    public NodalOsMissionControlInteractionService(NodalOsMissionControlInteractionValidator validator) =>
        this.validator = validator;

    public NodalOsMissionControlUiIntent CreateIntent(
        NodalOsMissionControlUiIntentKind intentKind,
        NodalOsMissionControlUiSurfaceKind sourceSurface,
        string? actor = "operator",
        string? missionId = null,
        string? timelineEntryId = null,
        string? approvalCardId = null,
        string? evidenceId = null,
        string? observabilityReportId = null,
        string? note = null,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new()
        {
            IntentId = $"mission-control-intent-{Guid.NewGuid():N}",
            IntentKind = intentKind,
            SourceSurface = sourceSurface,
            ActorRedacted = validator.Redact(actor),
            MissionId = missionId,
            TimelineEntryId = timelineEntryId,
            ApprovalCardId = approvalCardId,
            EvidenceId = evidenceId,
            ObservabilityReportId = observabilityReportId,
            NoteRedacted = validator.Redact(note),
            MetadataRedacted = validator.RedactMetadata(metadata ?? new Dictionary<string, string>()),
            IsNoOp = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresPositiveExecutionGate = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public NodalOsMissionControlNoOpEvent CreateNoOpEvent(NodalOsMissionControlUiIntent intent)
    {
        var validation = validator.ValidateIntent(intent);
        if (!validation.IsValid)
            throw new ArgumentException(string.Join(" | ", validation.Errors), nameof(intent));

        return new()
        {
            UiEventId = $"mission-control-noop-event-{Guid.NewGuid():N}",
            IntentId = intent.IntentId,
            IntentKind = intent.IntentKind,
            SourceSurface = intent.SourceSurface,
            MissionId = intent.MissionId,
            TimelineEntryId = intent.TimelineEntryId,
            ApprovalCardId = intent.ApprovalCardId,
            EvidenceId = intent.EvidenceId,
            SummaryRedacted = validator.Redact($"{intent.IntentKind} captured as no-op UI event."),
            IsNoOp = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresPositiveExecutionGate = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

public sealed class NodalOsApprovalDecisionDraftService
{
    private readonly NodalOsMissionControlInteractionValidator validator;

    public NodalOsApprovalDecisionDraftService()
        : this(new NodalOsMissionControlInteractionValidator())
    {
    }

    public NodalOsApprovalDecisionDraftService(NodalOsMissionControlInteractionValidator validator) =>
        this.validator = validator;

    public NodalOsApprovalDecisionDraft CreateDraft(
        string approvalCardId,
        NodalOsApprovalDecisionKind selectedDecision,
        string? userNote = null,
        string? reason = null,
        string? requestedChanges = null,
        string? requestExplanation = null,
        string? deferReason = null,
        IReadOnlyList<NodalOsEvidenceBridgeRef>? evidenceRefs = null,
        IReadOnlyList<string>? timelineEntryIds = null,
        NodalOsApprovalDecisionDraftStatus status = NodalOsApprovalDecisionDraftStatus.DraftCreated)
    {
        var now = DateTimeOffset.UtcNow;
        return new()
        {
            DraftId = $"approval-decision-draft-{Guid.NewGuid():N}",
            ApprovalCardId = approvalCardId,
            SelectedDecision = selectedDecision,
            Status = status,
            UserNoteRedacted = validator.Redact(userNote),
            ReasonRedacted = validator.Redact(reason),
            RequestedChangesRedacted = validator.Redact(requestedChanges),
            RequestExplanationRedacted = validator.Redact(requestExplanation),
            DeferReasonRedacted = validator.Redact(deferReason),
            EvidenceRefs = evidenceRefs ?? [],
            TimelineEntryIds = (timelineEntryIds ?? []).Select(validator.Redact).ToArray(),
            IsNoOp = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresPositiveExecutionGateForFutureExecution = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public NodalOsApprovalDecisionDraft UpdateDraft(
        NodalOsApprovalDecisionDraft draft,
        string? userNote = null,
        string? reason = null,
        NodalOsApprovalDecisionDraftStatus status = NodalOsApprovalDecisionDraftStatus.DraftUpdated) =>
        draft with
        {
            Status = status,
            UserNoteRedacted = userNote is null ? draft.UserNoteRedacted : validator.Redact(userNote),
            ReasonRedacted = reason is null ? draft.ReasonRedacted : validator.Redact(reason),
            IsNoOp = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresPositiveExecutionGateForFutureExecution = true,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsMissionControlUiStateStore
{
    private readonly NodalOsMissionControlInteractionValidator validator;
    private readonly Dictionary<string, NodalOsMissionControlUiState> states = new(StringComparer.Ordinal);

    public NodalOsMissionControlUiStateStore()
        : this(new NodalOsMissionControlInteractionValidator())
    {
    }

    public NodalOsMissionControlUiStateStore(NodalOsMissionControlInteractionValidator validator) =>
        this.validator = validator;

    public NodalOsMissionControlUiState Save(NodalOsMissionControlUiState state)
    {
        var sanitized = Sanitize(state);
        var validation = validator.ValidateUiState(sanitized);
        if (!validation.IsValid)
            throw new ArgumentException(string.Join(" | ", validation.Errors), nameof(state));

        states[sanitized.StateId] = sanitized;
        return sanitized;
    }

    public NodalOsMissionControlUiState? TryGet(string stateId) =>
        states.GetValueOrDefault(stateId);

    public NodalOsMissionControlUiState CreateDefault(NodalOsMissionControlShellPreview shell) =>
        new()
        {
            StateId = $"mission-control-ui-state-{Guid.NewGuid():N}",
            ActiveNavigationSection = NodalOsMissionControlPanelKind.MissionControl,
            SelectedMissionId = "mission-control-readonly",
            SelectedTimelineEntryId = shell.Timeline.Entries.FirstOrDefault()?.TimelineEntryId,
            SelectedApprovalCardId = shell.ApprovalDisplay.Cards.FirstOrDefault()?.ApprovalCardId,
            SelectedEvidenceId = shell.Evidence.EvidenceRefs.FirstOrDefault()?.EvidenceId,
            ExpandedTimelineEntryIds = shell.Timeline.Entries.Take(1).Select(entry => entry.TimelineEntryId).ToArray(),
            DismissedWarningIds = [],
            ActiveFiltersRedacted = new Dictionary<string, string>
            {
                ["severity"] = "all",
                ["surface"] = "mission-control"
            },
            PanelCollapsed = new Dictionary<string, bool>
            {
                ["rightPanel"] = false,
                ["logPanel"] = false
            },
            LogPreviewOpen = true,
            ReadOnlyUi = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            MockPersistenceOnly = true,
            CloudPersistenceAllowed = false,
            ProductiveDatabasePersistenceAllowed = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private NodalOsMissionControlUiState Sanitize(NodalOsMissionControlUiState state) =>
        state with
        {
            SelectedMissionId = validator.Redact(state.SelectedMissionId),
            SelectedTimelineEntryId = validator.Redact(state.SelectedTimelineEntryId),
            SelectedApprovalCardId = validator.Redact(state.SelectedApprovalCardId),
            SelectedEvidenceId = validator.Redact(state.SelectedEvidenceId),
            ExpandedTimelineEntryIds = state.ExpandedTimelineEntryIds.Select(validator.Redact).ToArray(),
            DismissedWarningIds = state.DismissedWarningIds.Select(validator.Redact).ToArray(),
            ActiveFiltersRedacted = validator.RedactMetadata(state.ActiveFiltersRedacted),
            ReadOnlyUi = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            MockPersistenceOnly = true,
            CloudPersistenceAllowed = false,
            ProductiveDatabasePersistenceAllowed = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsMissionControlInteractionJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly NodalOsMissionControlInteractionValidator validator;

    public NodalOsMissionControlInteractionJsonSerializer()
        : this(new NodalOsMissionControlInteractionValidator())
    {
    }

    public NodalOsMissionControlInteractionJsonSerializer(NodalOsMissionControlInteractionValidator validator) =>
        this.validator = validator;

    public string SerializeIntent(NodalOsMissionControlUiIntent intent) =>
        JsonSerializer.Serialize(Sanitize(intent), Options);

    public string SerializeNoOpEvent(NodalOsMissionControlNoOpEvent noOpEvent) =>
        JsonSerializer.Serialize(Sanitize(noOpEvent), Options);

    public string SerializeDecisionDraft(NodalOsApprovalDecisionDraft draft) =>
        JsonSerializer.Serialize(Sanitize(draft), Options);

    public string SerializeUiState(NodalOsMissionControlUiState state) =>
        JsonSerializer.Serialize(Sanitize(state), Options);

    public NodalOsMissionControlUiIntent? DeserializeIntent(string json) =>
        JsonSerializer.Deserialize<NodalOsMissionControlUiIntent>(json, Options);

    public NodalOsApprovalDecisionDraft? DeserializeDecisionDraft(string json) =>
        JsonSerializer.Deserialize<NodalOsApprovalDecisionDraft>(json, Options);

    public NodalOsMissionControlUiState? DeserializeUiState(string json) =>
        JsonSerializer.Deserialize<NodalOsMissionControlUiState>(json, Options);

    private NodalOsMissionControlUiIntent Sanitize(NodalOsMissionControlUiIntent intent) =>
        intent with
        {
            ActorRedacted = validator.Redact(intent.ActorRedacted),
            NoteRedacted = validator.Redact(intent.NoteRedacted),
            MetadataRedacted = validator.RedactMetadata(intent.MetadataRedacted),
            IsNoOp = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresPositiveExecutionGate = false
        };

    private NodalOsMissionControlNoOpEvent Sanitize(NodalOsMissionControlNoOpEvent noOpEvent) =>
        noOpEvent with
        {
            SummaryRedacted = validator.Redact(noOpEvent.SummaryRedacted),
            IsNoOp = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresPositiveExecutionGate = false
        };

    private NodalOsApprovalDecisionDraft Sanitize(NodalOsApprovalDecisionDraft draft) =>
        draft with
        {
            UserNoteRedacted = validator.Redact(draft.UserNoteRedacted),
            ReasonRedacted = validator.Redact(draft.ReasonRedacted),
            RequestedChangesRedacted = validator.Redact(draft.RequestedChangesRedacted),
            RequestExplanationRedacted = validator.Redact(draft.RequestExplanationRedacted),
            DeferReasonRedacted = validator.Redact(draft.DeferReasonRedacted),
            TimelineEntryIds = draft.TimelineEntryIds.Select(validator.Redact).ToArray(),
            IsNoOp = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresPositiveExecutionGateForFutureExecution = true
        };

    private NodalOsMissionControlUiState Sanitize(NodalOsMissionControlUiState state) =>
        state with
        {
            SelectedMissionId = validator.Redact(state.SelectedMissionId),
            SelectedTimelineEntryId = validator.Redact(state.SelectedTimelineEntryId),
            SelectedApprovalCardId = validator.Redact(state.SelectedApprovalCardId),
            SelectedEvidenceId = validator.Redact(state.SelectedEvidenceId),
            ExpandedTimelineEntryIds = state.ExpandedTimelineEntryIds.Select(validator.Redact).ToArray(),
            DismissedWarningIds = state.DismissedWarningIds.Select(validator.Redact).ToArray(),
            ActiveFiltersRedacted = validator.RedactMetadata(state.ActiveFiltersRedacted),
            ReadOnlyUi = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            MockPersistenceOnly = true,
            CloudPersistenceAllowed = false,
            ProductiveDatabasePersistenceAllowed = false
        };
}

public static class NodalOsMissionControlInteractionFixtures
{
    public static NodalOsMissionControlUiIntent SelectTimelineIntent() =>
        new NodalOsMissionControlInteractionService().CreateIntent(
            NodalOsMissionControlUiIntentKind.SelectTimelineEntry,
            NodalOsMissionControlUiSurfaceKind.TimelineView,
            missionId: "mission-control-readonly",
            timelineEntryId: NodalOsMissionControlShellFixtures.ShellPreview().Timeline.Entries.First().TimelineEntryId,
            note: "Select timeline entry for visual inspection.");

    public static NodalOsMissionControlNoOpEvent NoOpEvent() =>
        new NodalOsMissionControlInteractionService().CreateNoOpEvent(SelectTimelineIntent());

    public static NodalOsApprovalDecisionDraft ApprovalDecisionDraft(
        NodalOsApprovalDecisionKind decisionKind = NodalOsApprovalDecisionKind.Approve)
    {
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();
        var card = shell.ApprovalDisplay.Cards.First();
        return new NodalOsApprovalDecisionDraftService().CreateDraft(
            card.ApprovalCardId,
            decisionKind,
            userNote: "Draft decision captured for local review.",
            reason: "No-op approval draft only.",
            requestedChanges: decisionKind == NodalOsApprovalDecisionKind.RequestChanges ? "Clarify requested resource." : null,
            requestExplanation: decisionKind == NodalOsApprovalDecisionKind.RequestExplanation ? "Explain policy gate." : null,
            deferReason: decisionKind == NodalOsApprovalDecisionKind.Defer ? "Wait for human review." : null,
            evidenceRefs: card.EvidenceRefs,
            timelineEntryIds: card.TimelineEntryIds);
    }

    public static NodalOsMissionControlUiState UiState() =>
        new NodalOsMissionControlUiStateStore().CreateDefault(NodalOsMissionControlShellFixtures.ShellPreview());
}
