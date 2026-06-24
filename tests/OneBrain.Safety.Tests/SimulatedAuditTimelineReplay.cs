namespace OneBrain.Safety.Tests.SimulatedRuntime;

public sealed record TimelineEvent(
    string TimelineEventId,
    string SourceLedgerEventId,
    string SourceEvidenceId,
    string SourceCapability,
    string DecisionType,
    string? ApprovalStatus,
    string ReasonCode,
    string EventKind,
    string Severity,
    int SequenceIndex,
    string RedactedSummary,
    string NoExecutionProofRef,
    string RedactionProofRef,
    string RuntimeType,
    string FixtureType,
    string CreatedAtLogical,
    bool ReplaySafe,
    bool ExecutionPerformed,
    bool ProductiveRuntime);

public sealed record AuditRoundtripSummary(
    string RoundtripId,
    string SourceDecisionId,
    string SourceCapability,
    string DecisionType,
    string EvidenceEnvelopeId,
    int LedgerEventCount,
    int TimelineEventCount,
    bool AllLedgerEventsProjected,
    bool AllTimelineEventsRedacted,
    bool NoExecutionProofPreserved,
    bool RedactionProofPreserved,
    bool OrderingPreserved,
    bool ReplaySafe,
    bool ExecutionPerformed,
    bool ProductiveRuntime,
    int SideEffectSinkInvocations,
    IReadOnlyList<TimelineEvent> TimelineEvents);

public enum ReplayMode
{
    AuditOnlyInMemory,
    ReplayDeniedExecutionProhibited,
    ReplayDeniedMissingEvidence,
    ReplayDeniedTamperedLedger,
    ReplayDeniedUnsupportedDecision
}

public sealed record ReplayGuardResult(
    string ReplayId,
    string SourceRoundtripId,
    string SourceDecisionId,
    string? SourceApprovalRequestId,
    ReplayMode ReplayMode,
    bool ReplayAllowed,
    bool ReplayExecuted,
    bool ExecutorInvoked,
    bool ProviderCloudInvoked,
    bool FilesystemWritePerformed,
    bool BrowserAutomationPerformed,
    bool CapabilityUnlocked,
    bool PublicReleasePerformed,
    bool StoreSubmissionPerformed,
    bool SignedPublicZipCreated,
    bool ProductFilesModified,
    bool BridgeCspModified,
    bool ProductiveEnabled,
    int SideEffectSinkInvocations,
    IReadOnlyList<string> ReasonCodes,
    string EvidenceEnvelope,
    IReadOnlyList<string> LedgerEvents,
    RedactionProof RedactionProof,
    NoExecutionProof NoExecutionProof);

public sealed record AuditExportPackage(
    string ExportId,
    string SourceRoundtripId,
    string SourceDecisionId,
    string? SourceApprovalRequestId,
    string RuntimeType,
    string FixtureType,
    IReadOnlyList<string> EvidenceEnvelopeRefs,
    IReadOnlyList<string> LedgerEventRefs,
    IReadOnlyList<string> TimelineEventRefs,
    IReadOnlyList<string> RedactionProofRefs,
    IReadOnlyList<string> NoExecutionProofRefs,
    IReadOnlyList<string> ReplayGuardRefs,
    IReadOnlyList<string> ReasonCodes,
    bool ExportSafe,
    bool Redacted,
    bool ContainsSecrets,
    bool ContainsCredentials,
    bool ContainsTokens,
    bool ContainsRawLogs,
    bool ContainsBrowserSessionData,
    bool ExecutionPerformed,
    bool ProductiveRuntime,
    bool ProviderCloudInvoked,
    bool FilesystemWritePerformed,
    bool BrowserAutomationPerformed,
    bool CapabilityUnlocked,
    bool PublicReleasePerformed,
    bool StoreSubmissionPerformed,
    bool SignedPublicZipCreated,
    bool ProductFilesModified,
    bool BridgeCspModified);

public sealed record DeterminismSignature(
    string DecisionType,
    string SourceCapability,
    string SelectedExecutorCategory,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<string> EvidenceFlags,
    IReadOnlyList<string> LedgerEventKinds,
    IReadOnlyList<string> TimelineEventKinds,
    IReadOnlyList<string> NoExecutionFlags,
    IReadOnlyList<string> RedactionFlags,
    ReplayMode ReplayMode,
    bool ExportSafe);

public sealed class SimulatedTimelineProjector
{
    public IReadOnlyList<TimelineEvent> Project(SimulatedRoutingResult result) =>
        result.LedgerEvents
            .Select((item, index) => new TimelineEvent(
                TimelineEventId: $"timeline-{index + 1:D3}",
                SourceLedgerEventId: item.EventId,
                SourceEvidenceId: result.EvidenceEnvelope.EnvelopeId,
                SourceCapability: result.CapabilityName,
                DecisionType: result.PolicyDecisionType.ToString(),
                ApprovalStatus: null,
                ReasonCode: result.ReasonCode,
                EventKind: MapLedgerKind(item.EventType),
                Severity: result.Decision == SimulatedDecision.AllowSimulatedDryRun ? "INFO" : "WARN",
                SequenceIndex: index,
                RedactedSummary: $"REDACTED:{item.EventType}",
                NoExecutionProofRef: $"noexec-{result.RequestId}",
                RedactionProofRef: $"redaction-{result.RequestId}",
                RuntimeType: result.RuntimeType,
                FixtureType: result.FixtureType,
                CreatedAtLogical: $"logical-{index + 1:D3}",
                ReplaySafe: true,
                ExecutionPerformed: false,
                ProductiveRuntime: false))
            .ToArray();

    public IReadOnlyList<TimelineEvent> Project(SimulatedApprovalOutcome outcome) =>
        outcome.LedgerEvents
            .Select((item, index) => new TimelineEvent(
                TimelineEventId: $"approval-timeline-{index + 1:D3}",
                SourceLedgerEventId: item.EventId,
                SourceEvidenceId: outcome.EvidenceEnvelope.EvidenceId,
                SourceCapability: outcome.SourceCapability,
                DecisionType: outcome.ApprovalStatus.ToString(),
                ApprovalStatus: outcome.ApprovalStatus.ToString(),
                ReasonCode: item.ReasonCode,
                EventKind: MapApprovalKind(item.EventType),
                Severity: outcome.CanExecute ? "INFO" : "WARN",
                SequenceIndex: index,
                RedactedSummary: $"REDACTED:{item.EventType}",
                NoExecutionProofRef: outcome.EvidenceEnvelope.NoExecutionProofRef,
                RedactionProofRef: outcome.EvidenceEnvelope.RedactionProofRef,
                RuntimeType: outcome.EvidenceEnvelope.RuntimeType,
                FixtureType: outcome.EvidenceEnvelope.FixtureType,
                CreatedAtLogical: $"logical-{index + 1:D3}",
                ReplaySafe: true,
                ExecutionPerformed: false,
                ProductiveRuntime: false))
            .ToArray();

    private static string MapLedgerKind(string eventType) =>
        eventType switch
        {
            "SIMULATED_DRY_RUN_REQUESTED" => "SIMULATED_ROUTE_EVALUATED",
            "SIMULATED_POLICY_GATE_EVALUATED" => "SIMULATED_POLICY_DECISION_RECORDED",
            "SIMULATED_EVIDENCE_ENVELOPE_CREATED" => "SIMULATED_EVIDENCE_ENVELOPE_CREATED",
            "SIMULATED_NO_EXECUTION_PROOF_CREATED" => "SIMULATED_NO_EXECUTION_PROOF_CREATED",
            "SIMULATED_REDACTION_PROOF_CREATED" => "SIMULATED_REDACTION_PROOF_CREATED",
            _ => "SIMULATED_POLICY_DECISION_RECORDED"
        };

    private static string MapApprovalKind(string eventType) =>
        eventType switch
        {
            "SIMULATED_APPROVAL_REQUEST_CREATED" => "SIMULATED_APPROVAL_REQUEST_CREATED",
            "SIMULATED_APPROVAL_GRANTED" or "SIMULATED_APPROVAL_DENIED" or "SIMULATED_APPROVAL_EXPIRED" or "SIMULATED_APPROVAL_INVALID" => "SIMULATED_APPROVAL_DECISION_RECORDED",
            "SIMULATED_APPROVAL_DENYLIST_OVERRIDE_BLOCKED" or "SIMULATED_APPROVAL_UNSUPPORTED_OVERRIDE_BLOCKED" or "SIMULATED_APPROVAL_POLICY_VIOLATION_OVERRIDE_BLOCKED" => "SIMULATED_APPROVAL_OVERRIDE_BLOCKED",
            "SIMULATED_APPROVAL_EVIDENCE_ENVELOPE_CREATED" => "SIMULATED_EVIDENCE_ENVELOPE_CREATED",
            "SIMULATED_APPROVAL_NO_EXECUTION_PROOF_CREATED" => "SIMULATED_NO_EXECUTION_PROOF_CREATED",
            "SIMULATED_APPROVAL_REDACTION_PROOF_CREATED" => "SIMULATED_REDACTION_PROOF_CREATED",
            _ => "SIMULATED_APPROVAL_DECISION_RECORDED"
        };
}

public sealed class SimulatedTimelineRoundtrip
{
    private readonly SimulatedTimelineProjector _projector = new();

    public AuditRoundtripSummary Roundtrip(SimulatedRoutingResult result)
    {
        var timeline = _projector.Project(result);
        return BuildSummary(
            roundtripId: $"roundtrip-{result.RequestId}",
            sourceDecisionId: result.RequestId,
            sourceCapability: result.CapabilityName,
            decisionType: result.PolicyDecisionType.ToString(),
            evidenceEnvelopeId: result.EvidenceEnvelope.EnvelopeId,
            ledgerEventCount: result.LedgerEvents.Count,
            timeline);
    }

    public AuditRoundtripSummary Roundtrip(SimulatedApprovalOutcome outcome)
    {
        var timeline = _projector.Project(outcome);
        return BuildSummary(
            roundtripId: $"roundtrip-{outcome.ApprovalRequestId}",
            sourceDecisionId: $"decision-{outcome.SourceCapability}",
            sourceCapability: outcome.SourceCapability,
            decisionType: outcome.ApprovalStatus.ToString(),
            evidenceEnvelopeId: outcome.EvidenceEnvelope.EvidenceId,
            ledgerEventCount: outcome.LedgerEvents.Count,
            timeline);
    }

    private static AuditRoundtripSummary BuildSummary(
        string roundtripId,
        string sourceDecisionId,
        string sourceCapability,
        string decisionType,
        string evidenceEnvelopeId,
        int ledgerEventCount,
        IReadOnlyList<TimelineEvent> timeline) => new(
            RoundtripId: roundtripId,
            SourceDecisionId: sourceDecisionId,
            SourceCapability: sourceCapability,
            DecisionType: decisionType,
            EvidenceEnvelopeId: evidenceEnvelopeId,
            LedgerEventCount: ledgerEventCount,
            TimelineEventCount: timeline.Count,
            AllLedgerEventsProjected: timeline.Count == ledgerEventCount,
            AllTimelineEventsRedacted: timeline.All(static x => x.RedactedSummary.StartsWith("REDACTED:", StringComparison.Ordinal)),
            NoExecutionProofPreserved: timeline.All(static x => !x.ExecutionPerformed),
            RedactionProofPreserved: timeline.All(static x => !string.IsNullOrWhiteSpace(x.RedactionProofRef)),
            OrderingPreserved: timeline.Select(static x => x.SequenceIndex).SequenceEqual(Enumerable.Range(0, timeline.Count)),
            ReplaySafe: timeline.All(static x => x.ReplaySafe),
            ExecutionPerformed: false,
            ProductiveRuntime: false,
            SideEffectSinkInvocations: 0,
            TimelineEvents: timeline);
}

public sealed class SimulatedReplayGuard
{
    public ReplayGuardResult Evaluate(AuditRoundtripSummary summary, string? approvalRequestId = null) =>
        Evaluate(summary, new RecordingSideEffectSink(), approvalRequestId);

    public ReplayGuardResult Evaluate(
        AuditRoundtripSummary summary,
        RecordingSideEffectSink sink,
        string? approvalRequestId = null) =>
        Build(
            summary,
            approvalRequestId,
            ReplayMode.AuditOnlyInMemory,
            replayAllowed: true,
            [
                "replay_audit_only_in_memory",
                "replay_execution_prohibited",
                "replay_executor_invocation_blocked"
            ],
            sink);

    public ReplayGuardResult DenyMissingEvidence(AuditRoundtripSummary summary) =>
        Build(summary, null, ReplayMode.ReplayDeniedMissingEvidence, false, ["replay_missing_evidence_denied"], new RecordingSideEffectSink());

    public ReplayGuardResult DenyMismatchedApprovalRequest(AuditRoundtripSummary summary) =>
        Build(summary, "mismatched-approval-request", ReplayMode.ReplayDeniedMissingEvidence, false, ["replay_mismatched_approval_request_denied"], new RecordingSideEffectSink());

    public ReplayGuardResult DenyTamperedLedger(AuditRoundtripSummary summary, string reasonCode = "replay_tampered_ledger_denied") =>
        Build(summary, null, ReplayMode.ReplayDeniedTamperedLedger, false, [reasonCode], new RecordingSideEffectSink());

    public ReplayGuardResult DenyUnsupportedDecision(AuditRoundtripSummary summary) =>
        Build(summary, null, ReplayMode.ReplayDeniedUnsupportedDecision, false, ["replay_unsupported_decision_denied"], new RecordingSideEffectSink());

    private static ReplayGuardResult Build(
        AuditRoundtripSummary summary,
        string? approvalRequestId,
        ReplayMode mode,
        bool replayAllowed,
        IReadOnlyList<string> reasonCodes,
        RecordingSideEffectSink sink) => new(
            ReplayId: $"replay-{summary.RoundtripId}",
            SourceRoundtripId: summary.RoundtripId,
            SourceDecisionId: summary.SourceDecisionId,
            SourceApprovalRequestId: approvalRequestId,
            ReplayMode: mode,
            ReplayAllowed: replayAllowed,
            ReplayExecuted: false,
            ExecutorInvoked: false,
            ProviderCloudInvoked: false,
            FilesystemWritePerformed: false,
            BrowserAutomationPerformed: false,
            CapabilityUnlocked: false,
            PublicReleasePerformed: false,
            StoreSubmissionPerformed: false,
            SignedPublicZipCreated: false,
            ProductFilesModified: false,
            BridgeCspModified: false,
            ProductiveEnabled: false,
            SideEffectSinkInvocations: sink.InvocationCount,
            ReasonCodes: reasonCodes,
            EvidenceEnvelope: summary.EvidenceEnvelopeId,
            LedgerEvents: summary.TimelineEvents.Select(static x => x.SourceLedgerEventId).ToArray(),
            RedactionProof: CleanRedactionProof(),
            NoExecutionProof: CleanProof(sink));

    private static RedactionProof CleanRedactionProof() => new(false, false, false, false, false, false, false, false, false);

    // F1: measured from the caller-visible sink instead of an unconditional constant.
    private static NoExecutionProof CleanProof(RecordingSideEffectSink sink) => NoExecutionProof.FromSink(sink);
}

public sealed class SimulatedAuditExporter
{
    public AuditExportPackage Export(AuditRoundtripSummary summary, ReplayGuardResult replay) =>
        Export(summary, replay, new RecordingSideEffectSink());

    public AuditExportPackage Export(
        AuditRoundtripSummary summary,
        ReplayGuardResult replay,
        RecordingSideEffectSink sink) =>
        Build(summary, replay, exportSafe: true, sink);

    public AuditExportPackage ExportWithInjectedDangerousFlag(AuditRoundtripSummary summary, ReplayGuardResult replay, string injectedFlag) =>
        Build(summary, replay, exportSafe: false, new RecordingSideEffectSink());

    private static AuditExportPackage Build(
        AuditRoundtripSummary summary,
        ReplayGuardResult replay,
        bool exportSafe,
        RecordingSideEffectSink sink) => new(
        ExportId: $"export-{summary.RoundtripId}",
        SourceRoundtripId: summary.RoundtripId,
        SourceDecisionId: summary.SourceDecisionId,
        SourceApprovalRequestId: replay.SourceApprovalRequestId,
        RuntimeType: SimulatedDryRunOrchestrator.RuntimeType,
        FixtureType: SimulatedDryRunOrchestrator.RequiredFixtureType,
        EvidenceEnvelopeRefs: [summary.EvidenceEnvelopeId],
        LedgerEventRefs: summary.TimelineEvents.Select(static x => x.SourceLedgerEventId).ToArray(),
        TimelineEventRefs: summary.TimelineEvents.Select(static x => x.TimelineEventId).ToArray(),
        RedactionProofRefs: summary.TimelineEvents.Select(static x => x.RedactionProofRef).Distinct().ToArray(),
        NoExecutionProofRefs: summary.TimelineEvents.Select(static x => x.NoExecutionProofRef).Distinct().ToArray(),
        ReplayGuardRefs: [replay.ReplayId],
        ReasonCodes: replay.ReasonCodes,
        ExportSafe: exportSafe,
        Redacted: true,
        ContainsSecrets: false,
        ContainsCredentials: false,
        ContainsTokens: false,
        ContainsRawLogs: false,
        ContainsBrowserSessionData: false,
        ExecutionPerformed: sink.InvocationCount > 0,
        ProductiveRuntime: false,
        ProviderCloudInvoked: false,
        FilesystemWritePerformed: false,
        BrowserAutomationPerformed: false,
        CapabilityUnlocked: false,
        PublicReleasePerformed: false,
        StoreSubmissionPerformed: false,
        SignedPublicZipCreated: false,
        ProductFilesModified: false,
        BridgeCspModified: false);
}

public static class SimulatedDeterminism
{
    public static DeterminismSignature Capture(AuditRoundtripSummary summary, ReplayGuardResult replay, AuditExportPackage export, string selectedExecutorCategory) => new(
        DecisionType: summary.DecisionType,
        SourceCapability: summary.SourceCapability,
        SelectedExecutorCategory: selectedExecutorCategory,
        ReasonCodes: replay.ReasonCodes.OrderBy(static x => x, StringComparer.Ordinal).ToArray(),
        EvidenceFlags: ["evidenceEnvelopeCreated", $"projected:{summary.AllLedgerEventsProjected}"],
        LedgerEventKinds: summary.TimelineEvents.Select(static x => x.SourceLedgerEventId.Split('-', 3).Last()).ToArray(),
        TimelineEventKinds: summary.TimelineEvents.Select(static x => x.EventKind).ToArray(),
        NoExecutionFlags: ["actual:false", "live:false", "filesystem:false", "browser:false", "unlock:false", "release:false", "store:false"],
        RedactionFlags: ["secrets:false", "credentials:false", "tokens:false", "rawLogs:false", "browserSessionData:false"],
        ReplayMode: replay.ReplayMode,
        ExportSafe: export.ExportSafe);
}
