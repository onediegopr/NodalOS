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

public enum SimulatedPolicyDecisionType
{
    AllowSimulatedDryRun,
    DenyDenylistedCapability,
    DenyUnsupportedCapability,
    DenyPolicyViolation,
    RequireManualApprovalSimulated
}

public static class SimulatedPolicyReasonCodes
{
    public const string AllowedSimulatedFakeExecutor = "allowed_simulated_fake_executor";
    public const string DeniedDenylistedCapability = "denied_denylisted_capability";
    public const string DeniedUnsupportedCapability = "denied_unsupported_capability";
    public const string DeniedPolicyViolation = "denied_policy_violation";
    public const string RequiresManualApprovalSimulated = "requires_manual_approval_simulated";
    public const string ProductiveRuntimeProhibited = "productive_runtime_prohibited";
    public const string RealExecutorNotWired = "real_executor_not_wired";
    public const string ProviderCloudDisabled = "provider_cloud_disabled";
    public const string FilesystemWriteDisabled = "filesystem_write_disabled";
    public const string BrowserAutomationDisabled = "browser_automation_disabled";
    public const string ReleaseStoreDisabled = "release_store_disabled";
    public const string ProductBridgeCspModificationDisabled = "product_bridge_csp_modification_disabled";
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

public sealed record FakeExecutorExecutionResult(
    string ExecutorName,
    string ExecutorType,
    string RuntimeType,
    SimulatedRuntimeResult RuntimeResult,
    bool RealExecutionAllowed,
    bool LiveCallAllowed,
    bool CredentialUseAllowed,
    bool FilesystemWriteAllowed,
    bool BrowserActionAllowed,
    bool CapabilityUnlockAllowed,
    bool PublicReleaseAllowed,
    bool StoreSubmissionAllowed,
    bool ProductFilesModificationAllowed,
    bool BridgeCspModificationAllowed,
    bool FilesystemReaderRealInvoked,
    bool InMemoryLedgerOnly)
{
    public EvidenceEnvelope EvidenceEnvelope => RuntimeResult.EvidenceEnvelope;
    public IReadOnlyList<LedgerEvent> LedgerEvents => RuntimeResult.LedgerEvents;
    public RedactionProof RedactionProof => RuntimeResult.RedactionProof;
    public NoExecutionProof NoExecutionProof => RuntimeResult.Proof;
    public int SideEffectSinkInvocations => RuntimeResult.SideEffectSinkInvocations;
    public bool RealExecutorInvoked => RuntimeResult.RealExecutorInvoked;
    public bool ProviderClientInvoked => RuntimeResult.ProviderClientInvoked;
    public bool FilesystemWriterInvoked => RuntimeResult.FilesystemWriterInvoked;
    public bool BrowserAutomationInvoked => RuntimeResult.BrowserAutomationInvoked;
    public bool CapabilityUnlockInvoked => RuntimeResult.CapabilityUnlockInvoked;
    public bool PublicReleaseInvoked => RuntimeResult.PublicReleaseInvoked;
    public bool StoreSubmissionInvoked => RuntimeResult.StoreSubmissionInvoked;
    public bool SignedZipCreated => RuntimeResult.SignedZipCreated;
    public bool ProductFilesModified => RuntimeResult.ProductFilesModified;
    public bool BridgeCspModified => RuntimeResult.BridgeCspModified;
}

public interface ITestOnlyInMemoryFakeExecutor
{
    string ExecutorName { get; }
    string ExecutorType { get; }
    string CapabilityName { get; }
    FakeExecutorExecutionResult Execute();
}

public abstract class TestOnlyInMemoryFakeExecutor : ITestOnlyInMemoryFakeExecutor
{
    public const string TestOnlyExecutorType = "TEST_ONLY_IN_MEMORY_FAKE";

    protected TestOnlyInMemoryFakeExecutor(
        string executorName,
        string capabilityName,
        bool inMemoryLedgerOnly = false)
    {
        ExecutorName = executorName;
        CapabilityName = capabilityName;
        InMemoryLedgerOnly = inMemoryLedgerOnly;
    }

    public string ExecutorName { get; }
    public string ExecutorType => TestOnlyExecutorType;
    public string CapabilityName { get; }
    protected bool InMemoryLedgerOnly { get; }

    public FakeExecutorExecutionResult Execute()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);
        var result = orchestrator.Process(new SimulatedRequest(
            SimulatedDryRunOrchestrator.RequiredMode,
            SimulatedDryRunOrchestrator.RequiredFixtureType,
            CapabilityName,
            IsProhibitedAction: false));

        return new FakeExecutorExecutionResult(
            ExecutorName,
            ExecutorType,
            SimulatedDryRunOrchestrator.RuntimeType,
            result,
            RealExecutionAllowed: false,
            LiveCallAllowed: false,
            CredentialUseAllowed: false,
            FilesystemWriteAllowed: false,
            BrowserActionAllowed: false,
            CapabilityUnlockAllowed: false,
            PublicReleaseAllowed: false,
            StoreSubmissionAllowed: false,
            ProductFilesModificationAllowed: false,
            BridgeCspModificationAllowed: false,
            FilesystemReaderRealInvoked: false,
            InMemoryLedgerOnly: InMemoryLedgerOnly);
    }
}

public sealed class FakeLocalModelExecutor : TestOnlyInMemoryFakeExecutor
{
    public FakeLocalModelExecutor()
        : base("FakeLocalModelExecutor", "fake_local_model_executor")
    {
    }
}

public sealed class FakeFilesystemReadMetadataExecutor : TestOnlyInMemoryFakeExecutor
{
    public FakeFilesystemReadMetadataExecutor()
        : base("FakeFilesystemReadMetadataExecutor", "fake_filesystem_read_metadata_executor")
    {
    }
}

public sealed class FakeLedgerAppendExecutor : TestOnlyInMemoryFakeExecutor
{
    public FakeLedgerAppendExecutor()
        : base("FakeLedgerAppendExecutor", "fake_ledger_append_executor", inMemoryLedgerOnly: true)
    {
    }
}

public sealed record SimulatedRoutingResult(
    string RequestId,
    string CapabilityName,
    string? SelectedExecutor,
    SimulatedDecision Decision,
    SimulatedPolicyDecisionType PolicyDecisionType,
    string ReasonCode,
    string DenyReason,
    string RuntimeType,
    string FixtureType,
    EvidenceEnvelope EvidenceEnvelope,
    IReadOnlyList<LedgerEvent> LedgerEvents,
    RedactionProof RedactionProof,
    NoExecutionProof NoExecutionProof,
    bool AuditEventCreated)
{
    public int SideEffectSinkInvocations => NoExecutionProof.SideEffectSinkInvocations;
    public bool RealExecutorInvoked => NoExecutionProof.RealExecutorInvoked;
    public bool ProviderClientInvoked => NoExecutionProof.ProviderClientInvoked;
    public bool FilesystemWriterInvoked => NoExecutionProof.FilesystemWriterInvoked;
    public bool BrowserAutomationInvoked => NoExecutionProof.BrowserAutomationInvoked;
    public bool CapabilityUnlockInvoked => NoExecutionProof.CapabilityUnlockInvoked;
    public bool PublicReleaseInvoked => NoExecutionProof.PublicReleaseInvoked;
    public bool StoreSubmissionInvoked => NoExecutionProof.StoreSubmissionInvoked;
    public bool SignedZipCreated => NoExecutionProof.SignedZipCreated;
    public bool ProductFilesModified => NoExecutionProof.ProductFilesModified;
    public bool BridgeCspModified => NoExecutionProof.BridgeCspModified;
}

public sealed record SimulatedCapabilityMatrixEntry(
    string CapabilityName,
    string? ExpectedExecutor,
    bool IsDenylisted);

public static class SimulatedRuntimeRoutingMatrix
{
    public const string ManualApprovalCapability = "high_risk_simulated_manual_approval";
    public const string PolicyViolationCapability = "simulated_policy_violation";

    public static readonly IReadOnlyDictionary<string, string> AllowedRoutingTable =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["local_provider_model"] = "FakeLocalModelExecutor",
            ["filesystem_read_metadata"] = "FakeFilesystemReadMetadataExecutor",
            ["ledger_append"] = "FakeLedgerAppendExecutor"
        };

    public static readonly IReadOnlySet<string> DenylistedCapabilities =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "provider_cloud_live_call",
            "provider_credential_use",
            "filesystem_write",
            "browser_automation",
            "credential_captcha_2fa_bypass",
            "capability_unlock",
            "public_release",
            "chrome_web_store_submission",
            "signed_public_zip_creation",
            "product_file_modification",
            "bridge_csp_modification",
            "productive_enabled"
        };

    public static IReadOnlyList<SimulatedCapabilityMatrixEntry> Entries =>
        AllowedRoutingTable
            .Select(static x => new SimulatedCapabilityMatrixEntry(x.Key, x.Value, IsDenylisted: false))
            .Concat(DenylistedCapabilities.Select(static x => new SimulatedCapabilityMatrixEntry(x, null, IsDenylisted: true)))
            .ToArray();
}

public sealed class SimulatedCapabilityRouter
{
    private readonly IReadOnlyDictionary<string, string> _allowedRoutingTable;
    private readonly IReadOnlySet<string> _denylistedCapabilities;

    public SimulatedCapabilityRouter()
        : this(SimulatedRuntimeRoutingMatrix.AllowedRoutingTable, SimulatedRuntimeRoutingMatrix.DenylistedCapabilities)
    {
    }

    public SimulatedCapabilityRouter(
        IReadOnlyDictionary<string, string> allowedRoutingTable,
        IReadOnlySet<string> denylistedCapabilities)
    {
        _allowedRoutingTable = allowedRoutingTable;
        _denylistedCapabilities = denylistedCapabilities;
    }

    public SimulatedRoutingResult Route(string capabilityName)
    {
        if (string.IsNullOrWhiteSpace(capabilityName))
            return Deny(
                capabilityName ?? string.Empty,
                SimulatedPolicyDecisionType.DenyUnsupportedCapability,
                SimulatedPolicyReasonCodes.DeniedUnsupportedCapability,
                "unsupported capability denied: empty or missing capability");

        if (_denylistedCapabilities.Contains(capabilityName))
            return Deny(
                capabilityName,
                SimulatedPolicyDecisionType.DenyDenylistedCapability,
                SimulatedPolicyReasonCodes.DeniedDenylistedCapability,
                $"denylisted capability: {capabilityName}");

        if (string.Equals(capabilityName, SimulatedRuntimeRoutingMatrix.PolicyViolationCapability, StringComparison.Ordinal))
            return Deny(
                capabilityName,
                SimulatedPolicyDecisionType.DenyPolicyViolation,
                SimulatedPolicyReasonCodes.DeniedPolicyViolation,
                $"policy violation denied: {capabilityName}");

        if (string.Equals(capabilityName, SimulatedRuntimeRoutingMatrix.ManualApprovalCapability, StringComparison.Ordinal))
            return RequireManualApproval(capabilityName);

        if (!_allowedRoutingTable.TryGetValue(capabilityName, out var selectedExecutor))
            return Deny(
                capabilityName,
                SimulatedPolicyDecisionType.DenyUnsupportedCapability,
                SimulatedPolicyReasonCodes.DeniedUnsupportedCapability,
                $"unsupported capability denied: {capabilityName}");

        var result = CreateExecutor(selectedExecutor).Execute();
        return new SimulatedRoutingResult(
            RequestId: $"route-{capabilityName}",
            CapabilityName: capabilityName,
            SelectedExecutor: selectedExecutor,
            Decision: result.RuntimeResult.Decision,
            PolicyDecisionType: SimulatedPolicyDecisionType.AllowSimulatedDryRun,
            ReasonCode: SimulatedPolicyReasonCodes.AllowedSimulatedFakeExecutor,
            DenyReason: string.Empty,
            RuntimeType: result.RuntimeType,
            FixtureType: SimulatedDryRunOrchestrator.RequiredFixtureType,
            EvidenceEnvelope: result.EvidenceEnvelope,
            LedgerEvents: result.LedgerEvents,
            RedactionProof: result.RedactionProof,
            NoExecutionProof: result.NoExecutionProof,
            AuditEventCreated: true);
    }

    private static ITestOnlyInMemoryFakeExecutor CreateExecutor(string selectedExecutor) =>
        selectedExecutor switch
        {
            "FakeLocalModelExecutor" => new FakeLocalModelExecutor(),
            "FakeFilesystemReadMetadataExecutor" => new FakeFilesystemReadMetadataExecutor(),
            "FakeLedgerAppendExecutor" => new FakeLedgerAppendExecutor(),
            _ => throw new InvalidOperationException($"Unknown allowed fake executor: {selectedExecutor}")
        };

    private static SimulatedRoutingResult Deny(
        string capabilityName,
        SimulatedPolicyDecisionType policyDecisionType,
        string reasonCode,
        string denyReason)
    {
        var sink = new RecordingSideEffectSink();
        var result = new SimulatedDryRunOrchestrator(sink).Process(new SimulatedRequest(
            SimulatedDryRunOrchestrator.RequiredMode,
            SimulatedDryRunOrchestrator.RequiredFixtureType,
            capabilityName,
            IsProhibitedAction: true));

        return new SimulatedRoutingResult(
            RequestId: $"route-{capabilityName}",
            CapabilityName: capabilityName,
            SelectedExecutor: null,
            Decision: SimulatedDecision.Deny,
            PolicyDecisionType: policyDecisionType,
            ReasonCode: reasonCode,
            DenyReason: denyReason,
            RuntimeType: result.RuntimeType,
            FixtureType: result.FixtureType,
            EvidenceEnvelope: result.EvidenceEnvelope,
            LedgerEvents: result.LedgerEvents,
            RedactionProof: result.RedactionProof,
            NoExecutionProof: result.Proof,
            AuditEventCreated: true);
    }

    private static SimulatedRoutingResult RequireManualApproval(string capabilityName)
    {
        var sink = new RecordingSideEffectSink();
        var result = new SimulatedDryRunOrchestrator(sink).Process(new SimulatedRequest(
            SimulatedDryRunOrchestrator.RequiredMode,
            SimulatedDryRunOrchestrator.RequiredFixtureType,
            capabilityName,
            IsProhibitedAction: false,
            RequiresManualApproval: true,
            ManualApprovalGranted: false));

        return new SimulatedRoutingResult(
            RequestId: $"route-{capabilityName}",
            CapabilityName: capabilityName,
            SelectedExecutor: null,
            Decision: SimulatedDecision.RequireManualApproval,
            PolicyDecisionType: SimulatedPolicyDecisionType.RequireManualApprovalSimulated,
            ReasonCode: SimulatedPolicyReasonCodes.RequiresManualApprovalSimulated,
            DenyReason: string.Empty,
            RuntimeType: result.RuntimeType,
            FixtureType: result.FixtureType,
            EvidenceEnvelope: result.EvidenceEnvelope,
            LedgerEvents: result.LedgerEvents,
            RedactionProof: result.RedactionProof,
            NoExecutionProof: result.Proof,
            AuditEventCreated: true);
    }
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
