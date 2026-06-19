using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsApprovalTimelineEvidenceValidator
{
    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsApprovalTimelineEvidenceValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsApprovalTimelineEvidenceValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsApprovalTimelineEvidenceValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsCoreRuntimeValidationResult ValidateApprovalCard(NodalOsApprovalCard card)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, card.ApprovalCardId, "ApprovalCardId is required.");
        AddRequired(errors, card.HumanExplanationRedacted, "Human explanation is required.");
        AddRequired(errors, card.PolicyGateReasonRedacted, "Policy gate reason is required.");
        if (card.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoAuthority(
            card.RuntimeExecutionAllowed,
            card.RuntimeExecutionDeferred,
            card.RequiresGlobalPolicyEvaluation,
            card.RequiresEvidenceRedaction,
            card.CanAuthorizeExecution,
            errors);

        if (card.AffectedResourcesRedacted.Count == 0 && string.IsNullOrWhiteSpace(card.NoAffectedResourcesReasonRedacted))
            errors.Add("Approval card requires affected resources or an explicit no affected resources reason.");
        if (card.UserOptions.Count == 0)
            errors.Add("Approval card requires user options.");
        if (!card.UserOptions.Contains(NodalOsApprovalUserOptionKind.Approve) ||
            !card.UserOptions.Contains(NodalOsApprovalUserOptionKind.Reject))
        {
            errors.Add("Approval card must include approve and reject user options.");
        }

        ValidateSafeText(errors, "HumanExplanationRedacted", card.HumanExplanationRedacted);
        ValidateSafeText(errors, "PolicyGateReasonRedacted", card.PolicyGateReasonRedacted);
        ValidateSafeText(errors, "NoAffectedResourcesReasonRedacted", card.NoAffectedResourcesReasonRedacted);
        ValidateSafeText(errors, "RollbackPlanRedacted", card.RollbackPlanRedacted);
        ValidateSafeText(errors, "EvidencePlanRedacted", card.EvidencePlanRedacted);
        foreach (var resource in card.AffectedResourcesRedacted)
            ValidateSafeText(errors, "AffectedResourcesRedacted", resource);
        ValidateEvidenceRefs(card.EvidenceRefs, errors, warnings);

        return Result(errors, warnings, card.RuntimeExecutionDeferred, card.RequiresGlobalPolicyEvaluation, card.RequiresEvidenceRedaction);
    }

    public NodalOsCoreRuntimeValidationResult ValidateApprovalDecision(NodalOsApprovalDecision decision)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, decision.DecisionId, "DecisionId is required.");
        AddRequired(errors, decision.ApprovalCardId, "ApprovalCardId is required.");
        AddRequired(errors, decision.DecidedByRedacted, "DecidedByRedacted is required.");
        AddRequired(errors, decision.DecisionReasonRedacted, "DecisionReasonRedacted is required.");
        if (decision.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoAuthority(
            decision.RuntimeExecutionAllowed,
            decision.RuntimeExecutionDeferred,
            decision.RequiresGlobalPolicyEvaluation,
            decision.RequiresEvidenceRedaction,
            decision.CanAuthorizeExecution,
            errors);
        ValidateSafeText(errors, "DecidedByRedacted", decision.DecidedByRedacted);
        ValidateSafeText(errors, "DecisionReasonRedacted", decision.DecisionReasonRedacted);
        ValidateEvidenceRefs(decision.EvidenceRefs, errors, warnings);

        return Result(errors, warnings, decision.RuntimeExecutionDeferred, decision.RequiresGlobalPolicyEvaluation, decision.RequiresEvidenceRedaction);
    }

    public NodalOsCoreRuntimeValidationResult ValidateTimelineEntry(NodalOsTimelineEntry entry)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, entry.TimelineEntryId, "TimelineEntryId is required.");
        AddRequired(errors, entry.EventId, "EventId is required.");
        AddRequired(errors, entry.TitleRedacted, "TitleRedacted is required.");
        AddRequired(errors, entry.MessageRedacted, "MessageRedacted is required.");
        if (entry.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (entry.RuntimeExecutionAllowed)
            errors.Add("Timeline entries cannot grant runtime execution.");
        if (!entry.RuntimeExecutionDeferred)
            errors.Add("Timeline entries must keep runtime execution deferred.");

        ValidateSafeText(errors, "TitleRedacted", entry.TitleRedacted);
        ValidateSafeText(errors, "MessageRedacted", entry.MessageRedacted);
        ValidateEvidenceRefs(entry.EvidenceRefs, errors, warnings);

        return Result(errors, warnings, entry.RuntimeExecutionDeferred, requiresPolicy: true, requiresRedaction: true);
    }

    public NodalOsCoreRuntimeValidationResult ValidateEvidenceAttachment(NodalOsEvidenceRegistryAttachment attachment)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, attachment.AttachmentId, "AttachmentId is required.");
        if (attachment.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (attachment.RawPayloadPersisted)
            errors.Add("Evidence registry integration cannot persist raw payloads.");
        if (!attachment.RequiresEvidenceRedaction)
            errors.Add("Evidence registry integration requires evidence redaction.");
        if (string.IsNullOrWhiteSpace(attachment.ExecutionRegistryEntryId) &&
            string.IsNullOrWhiteSpace(attachment.EventId) &&
            string.IsNullOrWhiteSpace(attachment.ApprovalCardId) &&
            string.IsNullOrWhiteSpace(attachment.TimelineEntryId))
        {
            errors.Add("Evidence attachment must reference a registry entry, event, approval card, or timeline entry.");
        }

        ValidateEvidenceRef(attachment.EvidenceRef, errors, warnings);
        foreach (var (key, value) in attachment.MetadataRedacted)
            ValidateSafeField(errors, key, value);

        return Result(errors, warnings, runtimeDeferred: true, requiresPolicy: true, requiresRedaction: attachment.RequiresEvidenceRedaction);
    }

    public void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
            ValidateEvidenceRef(evidenceRef, errors, warnings);
    }

    public string Redact(string? value) => redaction.RedactValue(value).Value;

    public IReadOnlyDictionary<string, string> RedactMetadata(IReadOnlyDictionary<string, string> metadata) =>
        redaction.RedactDictionary(metadata).Values;

    private void ValidateEvidenceRef(
        NodalOsEvidenceBridgeRef evidenceRef,
        List<string> errors,
        List<string> warnings)
    {
        var bridgeResult = evidenceBridge.ValidateBridgeRef(evidenceRef);
        errors.AddRange(bridgeResult.Errors.Select(Redact));
        warnings.AddRange(bridgeResult.Warnings.Select(Redact));

        var text = string.Join(" ", evidenceRef.Kind, evidenceRef.Ref, evidenceRef.Hash, evidenceRef.LedgerRef, evidenceRef.Provenance);
        if (redaction.ContainsSensitiveContent(text))
            errors.Add("Evidence ref contains sensitive content and must be redacted before registry integration.");
        if (LooksLikeInlineScreenshot(evidenceRef.Ref))
            errors.Add("Screenshot evidence must be reference-only and cannot contain inline image data.");
        if (LooksLikeRawNetwork(evidenceRef.Kind, evidenceRef.Ref))
            errors.Add("Network evidence must be metadata-redacted-only and cannot contain headers, cookies, or body content.");
        if (LooksLikeRawDom(evidenceRef.Kind, evidenceRef.Ref))
            errors.Add("DOM evidence must be redacted-only and cannot contain raw markup.");
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before approval/timeline/evidence persistence.");
    }

    private void ValidateSafeField(List<string> errors, string fieldName, string? value)
    {
        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"Metadata field {fieldName} contains sensitive content and must be redacted before evidence registry persistence.");
    }

    private static bool LooksLikeInlineScreenshot(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeRawNetwork(string kind, string? value) =>
        kind.Contains("network", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(value) &&
         (value.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("set-cookie", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("body", StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeRawDom(string kind, string? value) =>
        kind.Contains("dom", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains('<') &&
        !value.Contains("[REDACTED]", StringComparison.OrdinalIgnoreCase);

    private static void ValidateNoAuthority(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        bool requiresEvidenceRedaction,
        bool canAuthorizeExecution,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("Approval/timeline/evidence contracts cannot grant runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("Approval/timeline/evidence contracts must keep runtime execution deferred.");
        if (!requiresGlobalPolicyEvaluation)
            errors.Add("Approval/timeline/evidence contracts must require global policy evaluation.");
        if (!requiresEvidenceRedaction)
            errors.Add("Approval/timeline/evidence contracts must require evidence redaction.");
        if (canAuthorizeExecution)
            errors.Add("Approval decisions and cards cannot authorize execution by themselves.");
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsCoreRuntimeValidationResult Result(
        List<string> errors,
        List<string> warnings,
        bool runtimeDeferred,
        bool requiresPolicy,
        bool requiresRedaction) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = runtimeDeferred,
            RequiresGlobalPolicyEvaluation = requiresPolicy,
            RequiresEvidenceRedaction = requiresRedaction,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsApprovalCenterService
{
    private readonly NodalOsApprovalTimelineEvidenceValidator validator;

    public NodalOsApprovalCenterService()
        : this(new NodalOsApprovalTimelineEvidenceValidator())
    {
    }

    public NodalOsApprovalCenterService(NodalOsApprovalTimelineEvidenceValidator validator) =>
        this.validator = validator;

    public NodalOsApprovalCard CreateApprovalCard(
        NodalOsExecutionRegistryEntry registryEntry,
        NodalOsCoreEvent coreEvent,
        NodalOsApprovalSeverity severity,
        NodalOsApprovalActionKind actionKind,
        string humanExplanation,
        string policyGateReason,
        IReadOnlyList<string> affectedResources,
        string? noAffectedResourcesReason = null)
    {
        var redactedResources = affectedResources.Select(validator.Redact).ToArray();
        return new NodalOsApprovalCard
        {
            ApprovalCardId = $"approval-card-{Guid.NewGuid():N}",
            ApprovalRequestId = $"approval-request-{registryEntry.RequestId}",
            ExecutionRegistryEntryId = registryEntry.RegistryEntryId,
            EventId = coreEvent.EventId,
            MissionId = coreEvent.MissionId,
            TaskId = coreEvent.TaskId,
            Status = NodalOsApprovalStatus.PendingHumanDecision,
            Severity = severity,
            RequestedAction = actionKind,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            CanAuthorizeExecution = false,
            HumanExplanationRedacted = validator.Redact(humanExplanation),
            PolicyGateReasonRedacted = validator.Redact(policyGateReason),
            AffectedResourcesRedacted = redactedResources,
            NoAffectedResourcesReasonRedacted = noAffectedResourcesReason is null ? null : validator.Redact(noAffectedResourcesReason),
            RollbackPlanRedacted = actionKind is NodalOsApprovalActionKind.Observation or NodalOsApprovalActionKind.DryRun
                ? "No rollback required for observation/dry-run contract-only review."
                : "Rollback must be reviewed before any future runtime implementation.",
            EvidencePlanRedacted = "Approval decision will emit event/evidence refs only.",
            UserOptions =
            [
                NodalOsApprovalUserOptionKind.Approve,
                NodalOsApprovalUserOptionKind.Reject,
                NodalOsApprovalUserOptionKind.RequestChanges,
                NodalOsApprovalUserOptionKind.RequestExplanation,
                NodalOsApprovalUserOptionKind.Defer,
                NodalOsApprovalUserOptionKind.CopyTechnicalLog
            ],
            EvidenceRefs = registryEntry.EvidenceRefs.Concat(coreEvent.EvidenceRefs).DistinctBy(e => e.EvidenceId).ToArray(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsApprovalDecision CreateDecision(
        NodalOsApprovalCard card,
        NodalOsApprovalDecisionKind decisionKind,
        string decidedBy,
        string reason) =>
        new()
        {
            DecisionId = $"approval-decision-{Guid.NewGuid():N}",
            ApprovalCardId = card.ApprovalCardId,
            DecisionKind = decisionKind,
            DecidedByRedacted = validator.Redact(decidedBy),
            DecisionReasonRedacted = validator.Redact(reason),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            CanAuthorizeExecution = false,
            EvidenceRefs = card.EvidenceRefs,
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsTimelineProjectionService
{
    private readonly NodalOsApprovalTimelineEvidenceValidator validator;

    public NodalOsTimelineProjectionService()
        : this(new NodalOsApprovalTimelineEvidenceValidator())
    {
    }

    public NodalOsTimelineProjectionService(NodalOsApprovalTimelineEvidenceValidator validator) =>
        this.validator = validator;

    public NodalOsTimelineEntry ProjectEvent(NodalOsCoreEvent coreEvent) =>
        new()
        {
            TimelineEntryId = $"timeline-entry-{coreEvent.EventId}",
            EventId = coreEvent.EventId,
            ExecutionRegistryEntryId = coreEvent.ExecutionRegistryEntryId,
            ApprovalCardId = Metadata(coreEvent, "approvalCardId"),
            ApprovalDecisionId = Metadata(coreEvent, "approvalDecisionId"),
            MissionId = coreEvent.MissionId,
            TaskId = coreEvent.TaskId,
            SourceEventKind = coreEvent.Kind,
            Severity = SeverityFor(coreEvent.Kind),
            Status = StatusFor(coreEvent.Kind),
            TitleRedacted = TitleFor(coreEvent.Kind),
            MessageRedacted = validator.Redact(coreEvent.HumanSummaryRedacted ?? coreEvent.TechnicalSummaryRedacted ?? coreEvent.Kind.ToString()),
            RequiresHumanAttention = RequiresAttention(coreEvent.Kind),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            EvidenceRefs = coreEvent.EvidenceRefs,
            CreatedAt = coreEvent.CreatedAt
        };

    public IReadOnlyList<NodalOsTimelineEntry> ProjectEvents(IEnumerable<NodalOsCoreEvent> events) =>
        events.OrderBy(e => e.CreatedAt).Select(ProjectEvent).ToArray();

    public IReadOnlyDictionary<string, IReadOnlyList<NodalOsTimelineEntry>> GroupByExecution(
        IEnumerable<NodalOsTimelineEntry> entries) =>
        entries
            .GroupBy(entry => entry.ExecutionRegistryEntryId ?? "unlinked")
            .ToDictionary(group => group.Key, group => (IReadOnlyList<NodalOsTimelineEntry>)group.OrderBy(entry => entry.CreatedAt).ToArray());

    private static string? Metadata(NodalOsCoreEvent coreEvent, string key) =>
        coreEvent.MetadataRedacted.TryGetValue(key, out var value) ? value : null;

    private static string TitleFor(NodalOsCoreEventKind kind) =>
        kind switch
        {
            NodalOsCoreEventKind.ExecutionRequestRegistered => "Execution request registered",
            NodalOsCoreEventKind.PolicyGateEvaluated => "Policy gate evaluated",
            NodalOsCoreEventKind.ApprovalRequired => "Approval required",
            NodalOsCoreEventKind.ApprovalGranted => "Approval granted",
            NodalOsCoreEventKind.ApprovalRejected => "Approval rejected",
            NodalOsCoreEventKind.DryRunPlanCreated => "Dry-run plan created",
            NodalOsCoreEventKind.ExecutionCompleted => "Execution bookkeeping completed",
            NodalOsCoreEventKind.ExecutionFailed => "Execution bookkeeping failed",
            NodalOsCoreEventKind.EvidenceAttached => "Evidence attached",
            NodalOsCoreEventKind.WarningRaised => "Warning raised",
            NodalOsCoreEventKind.HumanHandoffRequired => "Human handoff required",
            NodalOsCoreEventKind.RedactionApplied => "Redaction applied",
            _ => kind.ToString()
        };

    private static NodalOsTimelineEntrySeverity SeverityFor(NodalOsCoreEventKind kind) =>
        kind switch
        {
            NodalOsCoreEventKind.ExecutionCompleted or NodalOsCoreEventKind.ApprovalGranted => NodalOsTimelineEntrySeverity.Success,
            NodalOsCoreEventKind.ExecutionFailed or NodalOsCoreEventKind.ApprovalRejected => NodalOsTimelineEntrySeverity.Error,
            NodalOsCoreEventKind.WarningRaised or NodalOsCoreEventKind.ApprovalRequired or NodalOsCoreEventKind.HumanHandoffRequired => NodalOsTimelineEntrySeverity.Warning,
            _ => NodalOsTimelineEntrySeverity.Info
        };

    private static NodalOsTimelineEntryStatus StatusFor(NodalOsCoreEventKind kind) =>
        kind switch
        {
            NodalOsCoreEventKind.ExecutionCompleted or NodalOsCoreEventKind.ApprovalGranted => NodalOsTimelineEntryStatus.Completed,
            NodalOsCoreEventKind.ExecutionFailed or NodalOsCoreEventKind.ApprovalRejected => NodalOsTimelineEntryStatus.Failed,
            NodalOsCoreEventKind.ApprovalRequired or NodalOsCoreEventKind.HumanHandoffRequired => NodalOsTimelineEntryStatus.RequiresAttention,
            NodalOsCoreEventKind.RedactionApplied => NodalOsTimelineEntryStatus.Redacted,
            _ => NodalOsTimelineEntryStatus.Recorded
        };

    private static bool RequiresAttention(NodalOsCoreEventKind kind) =>
        kind is NodalOsCoreEventKind.ApprovalRequired or
            NodalOsCoreEventKind.HumanHandoffRequired or
            NodalOsCoreEventKind.WarningRaised or
            NodalOsCoreEventKind.ExecutionFailed;
}

public sealed class NodalOsEvidenceRegistryIntegrationService
{
    private readonly NodalOsApprovalTimelineEvidenceValidator validator;

    public NodalOsEvidenceRegistryIntegrationService()
        : this(new NodalOsApprovalTimelineEvidenceValidator())
    {
    }

    public NodalOsEvidenceRegistryIntegrationService(NodalOsApprovalTimelineEvidenceValidator validator) =>
        this.validator = validator;

    public NodalOsExecutionRegistryEntry AttachToRegistryEntry(
        NodalOsExecutionRegistryEntry entry,
        NodalOsEvidenceBridgeRef evidenceRef) =>
        entry with
        {
            EvidenceRefs = entry.EvidenceRefs.Concat([evidenceRef]).DistinctBy(e => e.EvidenceId).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

    public NodalOsCoreEvent AttachToEvent(NodalOsCoreEvent coreEvent, NodalOsEvidenceBridgeRef evidenceRef) =>
        coreEvent with
        {
            EvidenceRefs = coreEvent.EvidenceRefs.Concat([evidenceRef]).DistinctBy(e => e.EvidenceId).ToArray()
        };

    public NodalOsApprovalCard AttachToApprovalCard(NodalOsApprovalCard card, NodalOsEvidenceBridgeRef evidenceRef) =>
        card with
        {
            EvidenceRefs = card.EvidenceRefs.Concat([evidenceRef]).DistinctBy(e => e.EvidenceId).ToArray()
        };

    public NodalOsEvidenceRegistryAttachment CreateAttachment(
        NodalOsEvidenceAttachmentKind attachmentKind,
        NodalOsEvidenceBridgeRef evidenceRef,
        string? registryEntryId = null,
        string? eventId = null,
        string? approvalCardId = null,
        string? timelineEntryId = null,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new()
        {
            AttachmentId = $"evidence-attachment-{Guid.NewGuid():N}",
            AttachmentKind = attachmentKind,
            ExecutionRegistryEntryId = registryEntryId,
            EventId = eventId,
            ApprovalCardId = approvalCardId,
            TimelineEntryId = timelineEntryId,
            EvidenceRef = evidenceRef,
            RawPayloadPersisted = false,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = validator.RedactMetadata(metadata ?? new Dictionary<string, string>()),
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsApprovalTimelineEvidenceJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly NodalOsApprovalTimelineEvidenceValidator validator;

    public NodalOsApprovalTimelineEvidenceJsonSerializer()
        : this(new NodalOsApprovalTimelineEvidenceValidator())
    {
    }

    public NodalOsApprovalTimelineEvidenceJsonSerializer(NodalOsApprovalTimelineEvidenceValidator validator) =>
        this.validator = validator;

    public string SerializeApprovalCard(NodalOsApprovalCard card) =>
        JsonSerializer.Serialize(Sanitize(card), Options);

    public NodalOsApprovalCard? DeserializeApprovalCard(string json) =>
        JsonSerializer.Deserialize<NodalOsApprovalCard>(json, Options);

    public string SerializeApprovalDecision(NodalOsApprovalDecision decision) =>
        JsonSerializer.Serialize(Sanitize(decision), Options);

    public NodalOsApprovalDecision? DeserializeApprovalDecision(string json) =>
        JsonSerializer.Deserialize<NodalOsApprovalDecision>(json, Options);

    public string SerializeTimelineEntry(NodalOsTimelineEntry entry) =>
        JsonSerializer.Serialize(Sanitize(entry), Options);

    public NodalOsTimelineEntry? DeserializeTimelineEntry(string json) =>
        JsonSerializer.Deserialize<NodalOsTimelineEntry>(json, Options);

    public string SerializeEvidenceAttachment(NodalOsEvidenceRegistryAttachment attachment) =>
        JsonSerializer.Serialize(Sanitize(attachment), Options);

    public NodalOsEvidenceRegistryAttachment? DeserializeEvidenceAttachment(string json) =>
        JsonSerializer.Deserialize<NodalOsEvidenceRegistryAttachment>(json, Options);

    private NodalOsApprovalCard Sanitize(NodalOsApprovalCard card) =>
        card with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            CanAuthorizeExecution = false,
            HumanExplanationRedacted = validator.Redact(card.HumanExplanationRedacted),
            PolicyGateReasonRedacted = validator.Redact(card.PolicyGateReasonRedacted),
            AffectedResourcesRedacted = card.AffectedResourcesRedacted.Select(validator.Redact).ToArray(),
            NoAffectedResourcesReasonRedacted = validator.Redact(card.NoAffectedResourcesReasonRedacted),
            RollbackPlanRedacted = validator.Redact(card.RollbackPlanRedacted),
            EvidencePlanRedacted = validator.Redact(card.EvidencePlanRedacted)
        };

    private NodalOsApprovalDecision Sanitize(NodalOsApprovalDecision decision) =>
        decision with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            CanAuthorizeExecution = false,
            DecidedByRedacted = validator.Redact(decision.DecidedByRedacted),
            DecisionReasonRedacted = validator.Redact(decision.DecisionReasonRedacted)
        };

    private NodalOsTimelineEntry Sanitize(NodalOsTimelineEntry entry) =>
        entry with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            TitleRedacted = validator.Redact(entry.TitleRedacted),
            MessageRedacted = validator.Redact(entry.MessageRedacted)
        };

    private NodalOsEvidenceRegistryAttachment Sanitize(NodalOsEvidenceRegistryAttachment attachment) =>
        attachment with
        {
            RawPayloadPersisted = false,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = validator.RedactMetadata(attachment.MetadataRedacted)
        };
}

public static class NodalOsApprovalTimelineEvidenceFixtures
{
    public static NodalOsApprovalCard ApprovalCard() =>
        new()
        {
            ApprovalCardId = $"approval-card-{Guid.NewGuid():N}",
            ApprovalRequestId = "approval-request-fixture",
            ExecutionRegistryEntryId = "registry-fixture",
            EventId = "core-event-fixture",
            MissionId = "mission-approval",
            TaskId = "task-approval",
            Status = NodalOsApprovalStatus.PendingHumanDecision,
            Severity = NodalOsApprovalSeverity.High,
            RequestedAction = NodalOsApprovalActionKind.SubmitFuture,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            CanAuthorizeExecution = false,
            HumanExplanationRedacted = "Policy requires human approval before future mutable action.",
            PolicyGateReasonRedacted = "Requested action is sensitive and future-only.",
            AffectedResourcesRedacted = ["resource://customer-record-redacted"],
            RollbackPlanRedacted = "Rollback plan must be reviewed before future runtime.",
            EvidencePlanRedacted = "Evidence refs and timeline events will be retained.",
            UserOptions =
            [
                NodalOsApprovalUserOptionKind.Approve,
                NodalOsApprovalUserOptionKind.Reject,
                NodalOsApprovalUserOptionKind.RequestChanges,
                NodalOsApprovalUserOptionKind.RequestExplanation,
                NodalOsApprovalUserOptionKind.Defer,
                NodalOsApprovalUserOptionKind.CopyTechnicalLog
            ],
            EvidenceRefs = [EvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsApprovalDecision ApprovalDecision(NodalOsApprovalDecisionKind decisionKind = NodalOsApprovalDecisionKind.Approve) =>
        new()
        {
            DecisionId = $"approval-decision-{Guid.NewGuid():N}",
            ApprovalCardId = "approval-card-fixture",
            DecisionKind = decisionKind,
            DecidedByRedacted = "operator",
            DecisionReasonRedacted = "Decision recorded for policy audit.",
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            CanAuthorizeExecution = false,
            EvidenceRefs = [EvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsTimelineEntry TimelineEntry(NodalOsCoreEventKind eventKind = NodalOsCoreEventKind.ExecutionRequestRegistered) =>
        new()
        {
            TimelineEntryId = $"timeline-entry-{Guid.NewGuid():N}",
            EventId = "core-event-fixture",
            ExecutionRegistryEntryId = "registry-fixture",
            SourceEventKind = eventKind,
            Severity = NodalOsTimelineEntrySeverity.Info,
            Status = NodalOsTimelineEntryStatus.Recorded,
            TitleRedacted = "Execution request registered",
            MessageRedacted = "Core event projected into timeline.",
            RequiresHumanAttention = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            EvidenceRefs = [EvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsEvidenceRegistryAttachment EvidenceAttachment() =>
        new()
        {
            AttachmentId = $"evidence-attachment-{Guid.NewGuid():N}",
            AttachmentKind = NodalOsEvidenceAttachmentKind.ApprovalCard,
            ApprovalCardId = "approval-card-fixture",
            EvidenceRef = EvidenceRef(),
            RawPayloadPersisted = false,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = new Dictionary<string, string>
            {
                ["source"] = "approval-card"
            },
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsEvidenceBridgeRef EvidenceRef(
        string kind = "approval-timeline-evidence",
        string? reference = "ledger:approval-timeline-evidence") =>
        new()
        {
            EvidenceId = $"evidence-{Guid.NewGuid():N}",
            Kind = kind,
            Ref = reference,
            Hash = "sha256:approval-timeline-evidence",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = reference?.StartsWith("ledger:", StringComparison.OrdinalIgnoreCase) == true ? reference : null,
            Provenance = "NODAL OS:ApprovalTimelineEvidence:RefOnly",
            CreatedAt = DateTimeOffset.UtcNow
        };
}
