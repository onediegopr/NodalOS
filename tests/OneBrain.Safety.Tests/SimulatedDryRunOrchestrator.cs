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
    bool BridgeCspModified);

public sealed record SimulatedRuntimeResult(
    SimulatedDecision Decision,
    string Reason,
    bool LedgerProjected,
    bool EvidenceEnvelopeCreated,
    bool RedactionProofCreated,
    NoExecutionProof Proof);

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

    private readonly ISimulatedSideEffectSink _sink;

    // The sink is retained only to make the "never invoked" guarantee testable.
    // It is intentionally never used inside Process(...).
    public SimulatedDryRunOrchestrator(ISimulatedSideEffectSink sink) => _sink = sink;

    public SimulatedRuntimeResult Process(SimulatedRequest request)
    {
        // No branch below ever calls _sink.* — non-invocation is by construction.
        _ = _sink;

        if (!string.Equals(request.RequestedMode, RequiredMode, StringComparison.Ordinal))
            return Deny("requestedMode must be SIMULATED_DRY_RUN");

        if (!string.Equals(request.FixtureType, RequiredFixtureType, StringComparison.Ordinal))
            return Deny("fixtureType must be SIMULATED_FAKE_ONLY");

        if (request.IsProhibitedAction)
            return Deny($"prohibited action denied: {request.CapabilityName}");

        if (request.RequiresManualApproval && !request.ManualApprovalGranted)
            return new SimulatedRuntimeResult(
                SimulatedDecision.RequireManualApproval,
                $"manual approval required: {request.CapabilityName}",
                LedgerProjected: true,
                EvidenceEnvelopeCreated: true,
                RedactionProofCreated: true,
                Proof: CleanProof());

        return new SimulatedRuntimeResult(
            SimulatedDecision.AllowSimulatedDryRun,
            $"simulated dry-run allowed: {request.CapabilityName}",
            LedgerProjected: true,
            EvidenceEnvelopeCreated: true,
            RedactionProofCreated: true,
            Proof: CleanProof());
    }

    private static SimulatedRuntimeResult Deny(string reason) =>
        new(SimulatedDecision.Deny, reason,
            LedgerProjected: true,
            EvidenceEnvelopeCreated: true,
            RedactionProofCreated: true,
            Proof: CleanProof());

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
