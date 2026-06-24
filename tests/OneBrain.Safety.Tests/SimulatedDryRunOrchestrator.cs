namespace OneBrain.Safety.Tests.SimulatedRuntime;

// M782-M784 simulated runtime boundary.
//
// This is a SAFETY-ONLY, FAKE-ONLY, IN-MEMORY helper that lives in the test
// project. It is NOT product code, is never wired into the Bridge, the
// extension, or any src adapter, and performs no real side effect of any kind.
//
// Its sole purpose is to make the previously declarative ("contracts + tests
// over JSON") enforcement EXECUTABLE, so that the invariant
//   "ALLOW_SIMULATED_DRY_RUN can never reach a real executor"
// is proven by construction (the orchestrator holds no path to a side effect)
// AND by test (a spy sink is injected and asserted to be never invoked).

public enum SimulatedDecision
{
    AllowSimulatedDryRun,
    Deny,
    RequireManualApproval
}

/// <summary>
/// Surface a REAL runtime would have to call to cause an effect. The simulated
/// orchestrator never references any member of this interface; the spy below
/// records invocation so tests can prove non-invocation on every branch.
/// </summary>
public interface ISimulatedSideEffectSink
{
    void InvokeRealExecutor();
    void InvokeProviderClient();
    void InvokeFilesystemWriter();
    void InvokeBrowserAutomation();
    void InvokeCapabilityUnlock();
    void InvokePublicRelease();
    void InvokeStoreSubmission();
    void CreateSignedZip();
}

/// <summary>Records whether any forbidden side effect was invoked. Default: none.</summary>
public sealed class RecordingSideEffectSink : ISimulatedSideEffectSink
{
    public bool RealExecutorInvoked { get; private set; }
    public bool ProviderClientInvoked { get; private set; }
    public bool FilesystemWriterInvoked { get; private set; }
    public bool BrowserAutomationInvoked { get; private set; }
    public bool CapabilityUnlockInvoked { get; private set; }
    public bool PublicReleaseInvoked { get; private set; }
    public bool StoreSubmissionInvoked { get; private set; }
    public bool SignedZipCreated { get; private set; }

    public bool AnyInvoked =>
        RealExecutorInvoked || ProviderClientInvoked || FilesystemWriterInvoked ||
        BrowserAutomationInvoked || CapabilityUnlockInvoked || PublicReleaseInvoked ||
        StoreSubmissionInvoked || SignedZipCreated;

    public void InvokeRealExecutor() => RealExecutorInvoked = true;
    public void InvokeProviderClient() => ProviderClientInvoked = true;
    public void InvokeFilesystemWriter() => FilesystemWriterInvoked = true;
    public void InvokeBrowserAutomation() => BrowserAutomationInvoked = true;
    public void InvokeCapabilityUnlock() => CapabilityUnlockInvoked = true;
    public void InvokePublicRelease() => PublicReleaseInvoked = true;
    public void InvokeStoreSubmission() => StoreSubmissionInvoked = true;
    public void CreateSignedZip() => SignedZipCreated = true;
}

public sealed record SimulatedRequest(
    string RequestedMode,
    string FixtureType,
    string CapabilityName,
    bool IsProhibitedAction,
    bool RequiresManualApproval = false,
    bool ManualApprovalGranted = false);

public sealed record NoExecutionProof(
    bool SimulationOnly,
    bool RealExecutorInvoked,
    bool ProviderClientInvoked,
    bool FilesystemWriterInvoked,
    bool BrowserAutomationInvoked,
    bool CapabilityUnlockInvoked,
    bool PublicReleaseInvoked,
    bool StoreSubmissionInvoked,
    bool SignedZipCreated,
    bool ProductFilesModified,
    bool BridgeCspModified)
{
    public bool ActualExecutionPerformed => false;
    public bool LiveCallPerformed => ProviderClientInvoked;
    public bool FilesystemWritePerformed => FilesystemWriterInvoked;
    public bool BrowserAutomationPerformed => BrowserAutomationInvoked;
    public bool CapabilityUnlocked => CapabilityUnlockInvoked;
    public bool PublicReleasePerformed => PublicReleaseInvoked;
    public bool StoreSubmissionPerformed => StoreSubmissionInvoked;
    public bool SignedPublicZipCreated => SignedZipCreated;
    public int SideEffectSinkInvocations => 0;
}

public sealed record RedactionProof(
    bool SecretsIncluded,
    bool CredentialsIncluded,
    bool TokensIncluded,
    bool CookiesIncluded,
    bool RawUserDataIncluded,
    bool RawLogsIncluded,
    bool ProviderKeysIncluded,
    bool PrivateKeysIncluded,
    bool BrowserSessionDataIncluded);

public sealed record LedgerEvent(
    string EventId,
    string EventType,
    string RequestId,
    string DryRunId,
    string CapabilityName,
    bool SimulationOnly,
    string EvidenceEnvelopeRef,
    bool ActualExecutionPerformed,
    bool LiveCallPerformed,
    bool FilesystemWritePerformed,
    bool BrowserAutomationPerformed,
    bool CapabilityUnlocked,
    bool PublicReleasePerformed,
    bool StoreSubmissionPerformed,
    bool SignedPublicZipCreated);

public sealed record EvidenceEnvelope(
    string EnvelopeId,
    string DryRunId,
    string RequestId,
    string CapabilityName,
    SimulatedDecision Decision,
    bool SimulationOnly,
    RedactionProof RedactionProof,
    IReadOnlyList<string> LedgerEventRefs,
    NoExecutionProof NoExecutionProof,
    bool ActualExecutionPerformed,
    bool LiveCallPerformed,
    bool FilesystemWritePerformed,
    bool BrowserAutomationPerformed,
    bool CapabilityUnlocked,
    bool PublicReleasePerformed,
    bool StoreSubmissionPerformed,
    bool SignedPublicZipCreated);

public sealed class InMemoryEvidenceLedger
{
    private readonly List<LedgerEvent> _events = [];

    public IReadOnlyList<LedgerEvent> Events => _events;

    public LedgerEvent Append(
        string eventType,
        string requestId,
        string dryRunId,
        string capabilityName,
        string evidenceEnvelopeRef)
    {
        var item = new LedgerEvent(
            EventId: $"evt-{_events.Count + 1:D3}-{eventType.ToLowerInvariant()}",
            EventType: eventType,
            RequestId: requestId,
            DryRunId: dryRunId,
            CapabilityName: capabilityName,
            SimulationOnly: true,
            EvidenceEnvelopeRef: evidenceEnvelopeRef,
            ActualExecutionPerformed: false,
            LiveCallPerformed: false,
            FilesystemWritePerformed: false,
            BrowserAutomationPerformed: false,
            CapabilityUnlocked: false,
            PublicReleasePerformed: false,
            StoreSubmissionPerformed: false,
            SignedPublicZipCreated: false);

        _events.Add(item);
        return item;
    }
}

public sealed record SimulatedRuntimeResult(
    SimulatedDecision Decision,
    string Reason,
    bool LedgerProjected,
    bool EvidenceEnvelopeCreated,
    bool RedactionProofCreated,
    NoExecutionProof Proof,
    string RuntimeType,
    string FixtureType,
    EvidenceEnvelope EvidenceEnvelope,
    IReadOnlyList<LedgerEvent> LedgerEvents,
    RedactionProof RedactionProof)
{
    public int SideEffectSinkInvocations => Proof.SideEffectSinkInvocations;
    public bool RealExecutorInvoked => Proof.RealExecutorInvoked;
    public bool ProviderClientInvoked => Proof.ProviderClientInvoked;
    public bool FilesystemWriterInvoked => Proof.FilesystemWriterInvoked;
    public bool BrowserAutomationInvoked => Proof.BrowserAutomationInvoked;
    public bool CapabilityUnlockInvoked => Proof.CapabilityUnlockInvoked;
    public bool PublicReleaseInvoked => Proof.PublicReleaseInvoked;
    public bool StoreSubmissionInvoked => Proof.StoreSubmissionInvoked;
    public bool SignedZipCreated => Proof.SignedZipCreated;
    public bool ProductFilesModified => Proof.ProductFilesModified;
    public bool BridgeCspModified => Proof.BridgeCspModified;
}

/// <summary>
/// Fake-only, in-memory simulated dry-run orchestrator. Produces a decision plus
/// ledger/evidence/no-execution projections. It never invokes the side-effect
/// sink on any branch, so no decision (including ALLOW_SIMULATED_DRY_RUN) can
/// reach a real executor.
/// </summary>
public sealed class SimulatedDryRunOrchestrator
{
    public const string RequiredMode = "SIMULATED_DRY_RUN";
    public const string RequiredFixtureType = "SIMULATED_FAKE_ONLY";
    public const string RuntimeType = "SIMULATED_FAKE_ONLY_IN_MEMORY";

    private readonly ISimulatedSideEffectSink _sink;

    // The sink is retained only to make the "never invoked" guarantee testable.
    // It is intentionally never used inside Process(...).
    public SimulatedDryRunOrchestrator(ISimulatedSideEffectSink sink) => _sink = sink;

    public SimulatedRuntimeResult Process(SimulatedRequest request)
    {
        // No branch below ever calls _sink.* — non-invocation is by construction.
        _ = _sink;

        if (!string.Equals(request.RequestedMode, RequiredMode, StringComparison.Ordinal))
            return BuildResult(
                request,
                SimulatedDecision.Deny,
                "requestedMode must be SIMULATED_DRY_RUN",
                "SIMULATED_ACTION_DENIED");

        if (!string.Equals(request.FixtureType, RequiredFixtureType, StringComparison.Ordinal))
            return BuildResult(
                request,
                SimulatedDecision.Deny,
                "fixtureType must be SIMULATED_FAKE_ONLY",
                "SIMULATED_ACTION_DENIED");

        if (request.IsProhibitedAction)
            return BuildResult(
                request,
                SimulatedDecision.Deny,
                $"prohibited action denied: {request.CapabilityName}",
                "SIMULATED_ACTION_DENIED");

        if (request.RequiresManualApproval && !request.ManualApprovalGranted)
            return BuildResult(
                request,
                SimulatedDecision.RequireManualApproval,
                $"manual approval required: {request.CapabilityName}",
                "SIMULATED_MANUAL_APPROVAL_REQUIRED");

        return BuildResult(
            request,
            SimulatedDecision.AllowSimulatedDryRun,
            $"simulated dry-run allowed: {request.CapabilityName}",
            "SIMULATED_ACTION_ALLOWED_FOR_DRY_RUN");
    }

    private static SimulatedRuntimeResult BuildResult(
        SimulatedRequest request,
        SimulatedDecision decision,
        string reason,
        string decisionEventType)
    {
        var requestId = $"req-{request.CapabilityName.ToLowerInvariant().Replace(' ', '-')}";
        var dryRunId = $"dry-{requestId}";
        var envelopeId = $"env-{requestId}";
        var ledger = new InMemoryEvidenceLedger();

        ledger.Append("SIMULATED_DRY_RUN_REQUESTED", requestId, dryRunId, request.CapabilityName, envelopeId);
        ledger.Append("SIMULATED_POLICY_GATE_EVALUATED", requestId, dryRunId, request.CapabilityName, envelopeId);
        if (decision == SimulatedDecision.RequireManualApproval)
            ledger.Append("SIMULATED_MANUAL_APPROVAL_EVALUATED", requestId, dryRunId, request.CapabilityName, envelopeId);

        ledger.Append(decisionEventType, requestId, dryRunId, request.CapabilityName, envelopeId);
        ledger.Append("SIMULATED_EVIDENCE_ENVELOPE_CREATED", requestId, dryRunId, request.CapabilityName, envelopeId);
        ledger.Append("SIMULATED_REDACTION_PROOF_CREATED", requestId, dryRunId, request.CapabilityName, envelopeId);
        ledger.Append("SIMULATED_NO_EXECUTION_PROOF_CREATED", requestId, dryRunId, request.CapabilityName, envelopeId);

        var proof = CleanProof();
        var redactionProof = CleanRedactionProof();
        var envelope = new EvidenceEnvelope(
            EnvelopeId: envelopeId,
            DryRunId: dryRunId,
            RequestId: requestId,
            CapabilityName: request.CapabilityName,
            Decision: decision,
            SimulationOnly: true,
            RedactionProof: redactionProof,
            LedgerEventRefs: ledger.Events.Select(static x => x.EventId).ToArray(),
            NoExecutionProof: proof,
            ActualExecutionPerformed: false,
            LiveCallPerformed: false,
            FilesystemWritePerformed: false,
            BrowserAutomationPerformed: false,
            CapabilityUnlocked: false,
            PublicReleasePerformed: false,
            StoreSubmissionPerformed: false,
            SignedPublicZipCreated: false);

        return new SimulatedRuntimeResult(
            decision,
            reason,
            LedgerProjected: ledger.Events.Count > 0,
            EvidenceEnvelopeCreated: true,
            RedactionProofCreated: true,
            Proof: proof,
            RuntimeType: RuntimeType,
            FixtureType: request.FixtureType,
            EvidenceEnvelope: envelope,
            LedgerEvents: ledger.Events.ToArray(),
            RedactionProof: redactionProof);
    }

    private static RedactionProof CleanRedactionProof() => new(
        SecretsIncluded: false,
        CredentialsIncluded: false,
        TokensIncluded: false,
        CookiesIncluded: false,
        RawUserDataIncluded: false,
        RawLogsIncluded: false,
        ProviderKeysIncluded: false,
        PrivateKeysIncluded: false,
        BrowserSessionDataIncluded: false);

    private static NoExecutionProof CleanProof() => new(
        SimulationOnly: true,
        RealExecutorInvoked: false,
        ProviderClientInvoked: false,
        FilesystemWriterInvoked: false,
        BrowserAutomationInvoked: false,
        CapabilityUnlockInvoked: false,
        PublicReleaseInvoked: false,
        StoreSubmissionInvoked: false,
        SignedZipCreated: false,
        ProductFilesModified: false,
        BridgeCspModified: false);
}
