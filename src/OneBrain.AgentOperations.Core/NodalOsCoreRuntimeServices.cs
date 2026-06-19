using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsCoreRuntimeValidator
{
    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsCoreRuntimeValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsCoreRuntimeValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsCoreRuntimeValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsCoreRuntimeValidationResult ValidateRequest(NodalOsExecutionRequest request)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, request.RequestId, "RequestId is required.");
        AddRequired(errors, request.RequestedBy, "RequestedBy is required.");
        if (request.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoRuntimeAuthority(
            request.RuntimeExecutionAllowed,
            request.RuntimeExecutionDeferred,
            request.RequiresGlobalPolicyEvaluation,
            request.RequiresEvidenceRedaction,
            errors);
        ValidateSafeText(errors, "RequestedBy", request.RequestedBy);
        ValidateSafeText(errors, "Summary", request.Summary);
        ValidateEvidenceRefs(request.EvidenceRefs, errors, warnings);

        return Result(errors, warnings, request.RuntimeExecutionDeferred, request.RequiresGlobalPolicyEvaluation, request.RequiresEvidenceRedaction);
    }

    public NodalOsCoreRuntimeValidationResult ValidateEntry(NodalOsExecutionRegistryEntry entry)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, entry.RegistryEntryId, "RegistryEntryId is required.");
        AddRequired(errors, entry.RequestId, "RequestId is required.");
        if (entry.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (entry.UpdatedAt == default)
            errors.Add("UpdatedAt is required.");

        ValidateNoRuntimeAuthority(
            entry.RuntimeExecutionAllowed,
            entry.RuntimeExecutionDeferred,
            entry.RequiresGlobalPolicyEvaluation,
            entry.RequiresEvidenceRedaction,
            errors);
        ValidateSafeText(errors, "PolicyDecisionRef", entry.PolicyDecisionRef);
        ValidateSafeText(errors, "ApprovalRef", entry.ApprovalRef);
        ValidateSafeText(errors, "DryRunRef", entry.DryRunRef);
        ValidateSafeText(errors, "VerificationReportRef", entry.VerificationReportRef);
        ValidateSafeText(errors, "FailureReasonRedacted", entry.FailureReasonRedacted);
        foreach (var snapshotRef in entry.SnapshotRefs)
            ValidateSafeText(errors, "SnapshotRefs", snapshotRef);
        foreach (var transition in entry.Transitions)
            ValidateTransition(transition, errors);
        ValidateEvidenceRefs(entry.EvidenceRefs, errors, warnings);

        if (entry.State == NodalOsExecutionRegistryState.Completed && !entry.Transitions.Any())
            warnings.Add("Completed registry entry has no transition history; treat as imported contract snapshot.");

        return Result(errors, warnings, entry.RuntimeExecutionDeferred, entry.RequiresGlobalPolicyEvaluation, entry.RequiresEvidenceRedaction);
    }

    public NodalOsCoreRuntimeValidationResult ValidateEvent(NodalOsCoreEvent coreEvent)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, coreEvent.EventId, "EventId is required.");
        if (coreEvent.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoRuntimeAuthority(
            coreEvent.RuntimeExecutionAllowed,
            coreEvent.RuntimeExecutionDeferred,
            coreEvent.RequiresGlobalPolicyEvaluation,
            coreEvent.RequiresEvidenceRedaction,
            errors);
        ValidateSafeText(errors, "HumanSummaryRedacted", coreEvent.HumanSummaryRedacted);
        ValidateSafeText(errors, "TechnicalSummaryRedacted", coreEvent.TechnicalSummaryRedacted);
        foreach (var (key, value) in coreEvent.MetadataRedacted)
            ValidateSafeField(errors, key, value);
        ValidateEvidenceRefs(coreEvent.EvidenceRefs, errors, warnings);

        if (string.IsNullOrWhiteSpace(coreEvent.ExecutionRegistryEntryId) &&
            coreEvent.Kind is not NodalOsCoreEventKind.WarningRaised and not NodalOsCoreEventKind.RedactionApplied)
        {
            warnings.Add("Core event is not linked to an execution registry entry.");
        }

        return Result(errors, warnings, coreEvent.RuntimeExecutionDeferred, coreEvent.RequiresGlobalPolicyEvaluation, coreEvent.RequiresEvidenceRedaction);
    }

    public bool CanTransition(NodalOsExecutionRegistryState from, NodalOsExecutionRegistryState to) =>
        to switch
        {
            NodalOsExecutionRegistryState.Registered => from == NodalOsExecutionRegistryState.Created,
            NodalOsExecutionRegistryState.PolicyEvaluated => from is NodalOsExecutionRegistryState.Registered,
            NodalOsExecutionRegistryState.ApprovalRequired => from is NodalOsExecutionRegistryState.PolicyEvaluated,
            NodalOsExecutionRegistryState.Approved => from is NodalOsExecutionRegistryState.ApprovalRequired,
            NodalOsExecutionRegistryState.Rejected => from is NodalOsExecutionRegistryState.PolicyEvaluated or NodalOsExecutionRegistryState.ApprovalRequired,
            NodalOsExecutionRegistryState.DryRunPlanned => from is NodalOsExecutionRegistryState.PolicyEvaluated or NodalOsExecutionRegistryState.Approved,
            NodalOsExecutionRegistryState.ExecutionSkipped => from is NodalOsExecutionRegistryState.PolicyEvaluated or NodalOsExecutionRegistryState.ApprovalRequired or NodalOsExecutionRegistryState.Approved or NodalOsExecutionRegistryState.DryRunPlanned,
            NodalOsExecutionRegistryState.HumanHandoffRequired => from is NodalOsExecutionRegistryState.PolicyEvaluated or NodalOsExecutionRegistryState.ApprovalRequired or NodalOsExecutionRegistryState.DryRunPlanned,
            NodalOsExecutionRegistryState.Completed => from is NodalOsExecutionRegistryState.ExecutionSkipped or NodalOsExecutionRegistryState.DryRunPlanned or NodalOsExecutionRegistryState.Rejected,
            NodalOsExecutionRegistryState.Failed => from is not NodalOsExecutionRegistryState.Completed and not NodalOsExecutionRegistryState.Cancelled,
            NodalOsExecutionRegistryState.Cancelled => from is not NodalOsExecutionRegistryState.Completed and not NodalOsExecutionRegistryState.Failed,
            _ => false
        };

    public NodalOsCoreRuntimeValidationResult ValidateTransitionRequest(
        NodalOsExecutionRegistryEntry entry,
        NodalOsExecutionRegistryState targetState,
        string actor)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, actor, "Transition actor is required.");
        ValidateSafeText(errors, "Actor", actor);
        if (!CanTransition(entry.State, targetState))
            errors.Add($"Invalid registry transition from {entry.State} to {targetState}.");

        return Result(errors, warnings, entry.RuntimeExecutionDeferred, entry.RequiresGlobalPolicyEvaluation, entry.RequiresEvidenceRedaction);
    }

    public NodalOsCoreTimelineProjection ToTimelineProjection(NodalOsCoreEvent coreEvent)
    {
        var summary = coreEvent.HumanSummaryRedacted ??
                      coreEvent.TechnicalSummaryRedacted ??
                      coreEvent.Kind.ToString();

        return new NodalOsCoreTimelineProjection
        {
            ProjectionId = $"timeline-{coreEvent.EventId}",
            EventId = coreEvent.EventId,
            ExecutionRegistryEntryId = coreEvent.ExecutionRegistryEntryId,
            Kind = coreEvent.Kind,
            SummaryRedacted = Redact(summary),
            EvidenceRefs = coreEvent.EvidenceRefs,
            CreatedAt = coreEvent.CreatedAt
        };
    }

    public string Redact(string? value) => redaction.RedactValue(value).Value;

    public IReadOnlyDictionary<string, string> RedactMetadata(IReadOnlyDictionary<string, string> metadata) =>
        redaction.RedactDictionary(metadata).Values;

    public void ValidateEvidenceRefs(
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

    private void ValidateTransition(NodalOsExecutionRegistryTransition transition, List<string> errors)
    {
        AddRequired(errors, transition.TransitionId, "TransitionId is required.");
        AddRequired(errors, transition.Actor, "Transition actor is required.");
        if (transition.CreatedAt == default)
            errors.Add("Transition CreatedAt is required.");
        ValidateSafeText(errors, "TransitionActor", transition.Actor);
        ValidateSafeText(errors, "ReasonRedacted", transition.ReasonRedacted);
    }

    private void ValidateNoRuntimeAuthority(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        bool requiresEvidenceRedaction,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("Core runtime foundation contracts cannot grant runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("Core runtime foundation contracts must keep runtime execution deferred.");
        if (!requiresGlobalPolicyEvaluation)
            errors.Add("Core runtime foundation contracts must require global policy evaluation.");
        if (!requiresEvidenceRedaction)
            errors.Add("Core runtime foundation contracts must require evidence redaction.");
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before core runtime persistence.");
    }

    private void ValidateSafeField(List<string> errors, string fieldName, string? value)
    {
        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"Metadata field {fieldName} contains sensitive content and must be redacted before event persistence.");
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsCoreRuntimeValidationResult Result(
        List<string> errors,
        List<string> warnings,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        bool requiresEvidenceRedaction) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = runtimeExecutionDeferred,
            RequiresGlobalPolicyEvaluation = requiresGlobalPolicyEvaluation,
            RequiresEvidenceRedaction = requiresEvidenceRedaction,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsExecutionRegistry
{
    private readonly NodalOsCoreRuntimeValidator validator;
    private readonly Dictionary<string, NodalOsExecutionRegistryEntry> entries = new(StringComparer.Ordinal);

    public NodalOsExecutionRegistry()
        : this(new NodalOsCoreRuntimeValidator())
    {
    }

    public NodalOsExecutionRegistry(NodalOsCoreRuntimeValidator validator) =>
        this.validator = validator;

    public NodalOsExecutionRegistryEntry Register(NodalOsExecutionRequest request)
    {
        var validation = validator.ValidateRequest(request);
        if (!validation.IsValid)
            throw new ArgumentException(string.Join(" | ", validation.Errors), nameof(request));

        var now = request.CreatedAt == default ? DateTimeOffset.UtcNow : request.CreatedAt;
        var entry = new NodalOsExecutionRegistryEntry
        {
            RegistryEntryId = $"registry-{request.RequestId}",
            RequestId = request.RequestId,
            State = NodalOsExecutionRegistryState.Registered,
            ActorKind = request.ActorKind,
            SourceKind = request.SourceKind,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            EvidenceRefs = request.EvidenceRefs,
            Transitions =
            [
                new NodalOsExecutionRegistryTransition
                {
                    TransitionId = $"transition-{request.RequestId}-registered",
                    FromState = NodalOsExecutionRegistryState.Created,
                    ToState = NodalOsExecutionRegistryState.Registered,
                    Actor = request.RequestedBy,
                    ReasonRedacted = validator.Redact(request.Summary),
                    CreatedAt = now
                }
            ],
            CreatedAt = now,
            UpdatedAt = now
        };

        entries[entry.RegistryEntryId] = entry;
        return entry;
    }

    public NodalOsExecutionRegistryEntry Transition(
        string registryEntryId,
        NodalOsExecutionRegistryState targetState,
        string actor,
        string? reason = null,
        string? policyDecisionRef = null,
        string? approvalRef = null,
        string? dryRunRef = null,
        string? verificationReportRef = null,
        IReadOnlyList<string>? snapshotRefs = null,
        IReadOnlyList<NodalOsEvidenceBridgeRef>? evidenceRefs = null)
    {
        if (!entries.TryGetValue(registryEntryId, out var current))
            throw new KeyNotFoundException($"Registry entry {registryEntryId} was not found.");

        var transitionValidation = validator.ValidateTransitionRequest(current, targetState, actor);
        if (!transitionValidation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", transitionValidation.Errors));

        var now = DateTimeOffset.UtcNow;
        var transition = new NodalOsExecutionRegistryTransition
        {
            TransitionId = $"transition-{Guid.NewGuid():N}",
            FromState = current.State,
            ToState = targetState,
            Actor = validator.Redact(actor),
            ReasonRedacted = validator.Redact(reason),
            CreatedAt = now
        };

        var updatedEvidence = evidenceRefs is null
            ? current.EvidenceRefs
            : current.EvidenceRefs.Concat(evidenceRefs).ToArray();
        var updatedSnapshots = snapshotRefs is null
            ? current.SnapshotRefs
            : current.SnapshotRefs.Concat(snapshotRefs.Select(validator.Redact)).ToArray();

        var updated = current with
        {
            State = targetState,
            PolicyDecisionRef = policyDecisionRef is null ? current.PolicyDecisionRef : validator.Redact(policyDecisionRef),
            ApprovalRef = approvalRef is null ? current.ApprovalRef : validator.Redact(approvalRef),
            DryRunRef = dryRunRef is null ? current.DryRunRef : validator.Redact(dryRunRef),
            VerificationReportRef = verificationReportRef is null ? current.VerificationReportRef : validator.Redact(verificationReportRef),
            SnapshotRefs = updatedSnapshots,
            EvidenceRefs = updatedEvidence,
            FailureReasonRedacted = targetState == NodalOsExecutionRegistryState.Failed
                ? validator.Redact(reason)
                : current.FailureReasonRedacted,
            Transitions = current.Transitions.Concat([transition]).ToArray(),
            UpdatedAt = now
        };

        var validation = validator.ValidateEntry(updated);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));

        entries[registryEntryId] = updated;
        return updated;
    }

    public NodalOsExecutionRegistryEntry? TryGet(string registryEntryId) =>
        entries.GetValueOrDefault(registryEntryId);

    public IReadOnlyList<NodalOsExecutionRegistryEntry> Snapshot() =>
        entries.Values.OrderBy(entry => entry.CreatedAt).ToArray();
}

public sealed class NodalOsCoreEventBus
{
    private readonly NodalOsCoreRuntimeValidator validator;
    private readonly List<NodalOsCoreEvent> events = [];

    public NodalOsCoreEventBus()
        : this(new NodalOsCoreRuntimeValidator())
    {
    }

    public NodalOsCoreEventBus(NodalOsCoreRuntimeValidator validator) =>
        this.validator = validator;

    public NodalOsCoreRuntimeValidationResult Publish(NodalOsCoreEvent coreEvent)
    {
        var sanitized = SanitizeEvent(coreEvent);
        var validation = validator.ValidateEvent(sanitized);
        if (validation.IsValid)
            events.Add(sanitized);

        return validation;
    }

    public IReadOnlyList<NodalOsCoreEvent> Snapshot() =>
        events.OrderBy(coreEvent => coreEvent.CreatedAt).ToArray();

    public IReadOnlyList<NodalOsCoreTimelineProjection> ToTimelineProjections() =>
        Snapshot().Select(validator.ToTimelineProjection).ToArray();

    private NodalOsCoreEvent SanitizeEvent(NodalOsCoreEvent coreEvent) =>
        coreEvent with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = validator.RedactMetadata(coreEvent.MetadataRedacted),
            HumanSummaryRedacted = validator.Redact(coreEvent.HumanSummaryRedacted),
            TechnicalSummaryRedacted = validator.Redact(coreEvent.TechnicalSummaryRedacted)
        };
}

public sealed class NodalOsCoreRuntimeJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly NodalOsCoreRuntimeValidator validator;

    public NodalOsCoreRuntimeJsonSerializer()
        : this(new NodalOsCoreRuntimeValidator())
    {
    }

    public NodalOsCoreRuntimeJsonSerializer(NodalOsCoreRuntimeValidator validator) =>
        this.validator = validator;

    public string SerializeRequest(NodalOsExecutionRequest request) =>
        JsonSerializer.Serialize(SanitizeRequest(request), Options);

    public NodalOsExecutionRequest? DeserializeRequest(string json) =>
        JsonSerializer.Deserialize<NodalOsExecutionRequest>(json, Options);

    public string SerializeEntry(NodalOsExecutionRegistryEntry entry) =>
        JsonSerializer.Serialize(SanitizeEntry(entry), Options);

    public NodalOsExecutionRegistryEntry? DeserializeEntry(string json) =>
        JsonSerializer.Deserialize<NodalOsExecutionRegistryEntry>(json, Options);

    public string SerializeEvent(NodalOsCoreEvent coreEvent) =>
        JsonSerializer.Serialize(SanitizeEvent(coreEvent), Options);

    public NodalOsCoreEvent? DeserializeEvent(string json) =>
        JsonSerializer.Deserialize<NodalOsCoreEvent>(json, Options);

    public string SerializeTimelineProjection(NodalOsCoreTimelineProjection projection) =>
        JsonSerializer.Serialize(projection with { SummaryRedacted = validator.Redact(projection.SummaryRedacted) }, Options);

    private NodalOsExecutionRequest SanitizeRequest(NodalOsExecutionRequest request) =>
        request with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            RequestedBy = validator.Redact(request.RequestedBy),
            Summary = validator.Redact(request.Summary)
        };

    private NodalOsExecutionRegistryEntry SanitizeEntry(NodalOsExecutionRegistryEntry entry) =>
        entry with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            PolicyDecisionRef = validator.Redact(entry.PolicyDecisionRef),
            ApprovalRef = validator.Redact(entry.ApprovalRef),
            DryRunRef = validator.Redact(entry.DryRunRef),
            VerificationReportRef = validator.Redact(entry.VerificationReportRef),
            SnapshotRefs = entry.SnapshotRefs.Select(validator.Redact).ToArray(),
            FailureReasonRedacted = validator.Redact(entry.FailureReasonRedacted),
            Transitions = entry.Transitions.Select(transition => transition with
            {
                Actor = validator.Redact(transition.Actor),
                ReasonRedacted = validator.Redact(transition.ReasonRedacted)
            }).ToArray()
        };

    private NodalOsCoreEvent SanitizeEvent(NodalOsCoreEvent coreEvent) =>
        coreEvent with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = validator.RedactMetadata(coreEvent.MetadataRedacted),
            HumanSummaryRedacted = validator.Redact(coreEvent.HumanSummaryRedacted),
            TechnicalSummaryRedacted = validator.Redact(coreEvent.TechnicalSummaryRedacted)
        };
}

public static class NodalOsCoreRuntimeFixtures
{
    public static NodalOsExecutionRequest ExecutionRequest() =>
        new()
        {
            RequestId = $"execution-request-{Guid.NewGuid():N}",
            MissionId = "mission-core-runtime",
            TaskId = "task-core-runtime",
            RunId = "run-core-runtime",
            RequestedBy = "operator",
            ActorKind = NodalOsExecutionActorKind.MissionControl,
            SourceKind = NodalOsExecutionSourceKind.MissionControl,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            Summary = "Register execution request as no-runtime foundation.",
            EvidenceRefs = [EvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsExecutionRequest SecretExecutionRequest() =>
        ExecutionRequest() with
        {
            RequestedBy = "operator password=super-secret",
            Summary = "Authorization: Bearer abcdefghijklmnopqrstuvwxyz"
        };

    public static NodalOsCoreEvent CoreEvent(
        NodalOsCoreEventKind kind = NodalOsCoreEventKind.ExecutionRequestRegistered,
        string? registryEntryId = "registry-fixture",
        string? requestId = "execution-request-fixture") =>
        new()
        {
            EventId = $"core-event-{Guid.NewGuid():N}",
            Kind = kind,
            ExecutionRegistryEntryId = registryEntryId,
            ExecutionRequestId = requestId,
            MissionId = "mission-core-runtime",
            TaskId = "task-core-runtime",
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = new Dictionary<string, string>
            {
                ["source"] = "core-runtime-foundation"
            },
            EvidenceRefs = [EvidenceRef()],
            HumanSummaryRedacted = $"{kind} recorded for Mission Control timeline.",
            TechnicalSummaryRedacted = "No side effects were executed.",
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsCoreEvent SecretCoreEvent() =>
        CoreEvent() with
        {
            MetadataRedacted = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer abcdefghijklmnopqrstuvwxyz",
                ["cookie"] = "session=abc123"
            },
            HumanSummaryRedacted = "api_key=raw-fixture-key",
            TechnicalSummaryRedacted = "private key -----BEGIN PRIVATE KEY----- abc"
        };

    public static NodalOsEvidenceBridgeRef EvidenceRef() =>
        new()
        {
            EvidenceId = $"evidence-{Guid.NewGuid():N}",
            Kind = "core-runtime-foundation",
            Ref = "ledger:core-runtime-foundation",
            Hash = "sha256:core-runtime-foundation",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = "ledger:core-runtime-foundation",
            Provenance = "NODAL OS:CoreRuntime:Foundation",
            CreatedAt = DateTimeOffset.UtcNow
        };
}
