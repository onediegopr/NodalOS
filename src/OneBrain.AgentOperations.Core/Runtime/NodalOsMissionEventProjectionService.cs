using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed class NodalOsMissionEventProjectionService
{
    public IReadOnlyList<NodalOsCoreTimelineProjection> Project(
        IEnumerable<MissionEventEnvelope> missionEvents,
        NodalOsCoreEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(missionEvents);
        ArgumentNullException.ThrowIfNull(eventBus);

        var existing = eventBus.Snapshot()
            .Select(value => value.EventId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var missionEvent in missionEvents.OrderBy(value => value.Sequence))
        {
            var eventId = EventId(missionEvent);
            if (!existing.Add(eventId))
                continue;

            var validation = eventBus.Publish(ToCoreEvent(missionEvent, eventId));
            if (!validation.IsValid)
                throw new InvalidOperationException(string.Join(" | ", validation.Errors));
        }

        return eventBus.ToTimelineProjections();
    }

    private static NodalOsCoreEvent ToCoreEvent(MissionEventEnvelope missionEvent, string eventId) =>
        new()
        {
            EventId = eventId,
            Kind = MapKind(missionEvent.Kind),
            ExecutionRegistryEntryId = $"mission-runtime-{SafeRuntimeText.Sanitize(missionEvent.RunId, 120)}",
            ExecutionRequestId = $"mission-request-{SafeRuntimeText.Sanitize(missionEvent.MissionId, 120)}",
            MissionId = SafeRuntimeText.Sanitize(missionEvent.MissionId, 120),
            TaskId = string.IsNullOrWhiteSpace(missionEvent.StepId)
                ? null
                : SafeRuntimeText.Sanitize(missionEvent.StepId, 120),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["run_id"] = SafeRuntimeText.Sanitize(missionEvent.RunId, 120),
                ["sequence"] = missionEvent.Sequence.ToString(),
                ["correlation_id"] = SafeRuntimeText.Sanitize(missionEvent.CorrelationId, 120),
                ["causation_id"] = SafeRuntimeText.Sanitize(missionEvent.CausationId, 120),
                ["mission_event_kind"] = missionEvent.Kind.ToString(),
                ["severity"] = missionEvent.Severity.ToString()
            },
            EvidenceRefs = missionEvent.EvidenceRefs.Select(reference => new NodalOsEvidenceBridgeRef
            {
                EvidenceId = SafeRuntimeText.Sanitize(reference, 160),
                Kind = "mission-runtime-evidence-ref",
                Ref = null,
                Hash = null,
                SourceKind = NodalOsEvidenceBridgeSourceKind.Mission,
                UseKind = missionEvent.Kind == MissionEventKind.StepVerified
                    ? NodalOsEvidenceBridgeUseKind.VerificationSupport
                    : NodalOsEvidenceBridgeUseKind.AuditTrail,
                Authority = missionEvent.Kind == MissionEventKind.StepVerified
                    ? NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly
                    : NodalOsEvidenceBridgeAuthority.NoAuthority,
                Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
                RedactionState = NodalOsEvidenceRedactionState.NotRequired,
                LedgerRef = null,
                Provenance = "Lightweight mission runtime projection into the canonical NODAL OS event bus.",
                CreatedAt = missionEvent.Timestamp
            }).ToArray(),
            HumanSummaryRedacted = SafeRuntimeText.Sanitize(missionEvent.Summary, 500),
            TechnicalSummaryRedacted = $"{missionEvent.Kind}:{missionEvent.Severity}",
            CreatedAt = missionEvent.Timestamp
        };

    private static string EventId(MissionEventEnvelope value) =>
        $"mission-event-{SafeRuntimeText.Sanitize(value.RunId, 80)}-{value.Sequence}";

    private static NodalOsCoreEventKind MapKind(MissionEventKind kind) => kind switch
    {
        MissionEventKind.ApprovalRequired => NodalOsCoreEventKind.ApprovalRequired,
        MissionEventKind.ApprovalResolved => NodalOsCoreEventKind.ApprovalGranted,
        MissionEventKind.FallbackApplied or MissionEventKind.CapabilityDegraded => NodalOsCoreEventKind.WarningRaised,
        MissionEventKind.ToolCallCompleted or MissionEventKind.StepReadyForVerification or MissionEventKind.StepVerified => NodalOsCoreEventKind.EvidenceAttached,
        MissionEventKind.RunCompleted => NodalOsCoreEventKind.ExecutionCompleted,
        MissionEventKind.StepFailed or MissionEventKind.RunFailed or MissionEventKind.RunCancelled or MissionEventKind.RunTimeout => NodalOsCoreEventKind.ExecutionFailed,
        MissionEventKind.StepBlocked or MissionEventKind.RunBlocked => NodalOsCoreEventKind.HumanHandoffRequired,
        _ => NodalOsCoreEventKind.ExecutionRequestRegistered
    };
}
