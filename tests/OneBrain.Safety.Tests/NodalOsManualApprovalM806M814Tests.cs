using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualApprovalBoundary")]
[TestCategory("M806")]
[TestCategory("M807")]
[TestCategory("M808")]
[TestCategory("M809")]
[TestCategory("M810")]
[TestCategory("M811")]
[TestCategory("M812")]
[TestCategory("M813")]
[TestCategory("M814")]
public sealed class NodalOsManualApprovalM806M814Tests
{
    private const string ApprovalBoundaryPath = "artifacts/agent-operations/m806/manual-approval-request-boundary.json";
    private const string ApprovalOutcomesPath = "artifacts/agent-operations/m807/manual-approval-decision-outcomes.json";
    private const string ApprovalNegativePath = "artifacts/agent-operations/m808/manual-approval-boundary-negative-tests.json";
    private const string ApprovalLedgerPath = "artifacts/agent-operations/m809/approval-ledger-event-projection.json";
    private const string EvidenceConsistencyPath = "artifacts/agent-operations/m810/approval-evidence-envelope-consistency.json";
    private const string RedactionNoExecutionPath = "artifacts/agent-operations/m811/approval-redaction-no-execution-consistency.json";
    private const string AuditMatrixPath = "artifacts/agent-operations/m812/approval-audit-matrix.json";
    private const string FlakeWatchPath = "artifacts/agent-operations/m813/full-suite-flake-regression-watch.json";
    private const string FinalPath = "artifacts/agent-operations/m806-m814/manual-approval-boundary-audit-go-no-go.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m814/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m814/next-macro-milestone-recommendation.json";

    private static readonly string[] OverrideBlockedCapabilities =
    [
        "provider_cloud_live_call",
        "filesystem_write",
        "browser_automation",
        "capability_unlock",
        "public_release",
        "chrome_web_store_submission",
        "signed_public_zip_creation",
        "product_file_modification",
        "bridge_csp_modification",
        "productive_enabled",
        "unknown_future_capability",
        SimulatedRuntimeRoutingMatrix.PolicyViolationCapability
    ];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static string ReadAll(string relativePath) => File.ReadAllText(FullPath(relativePath));

    [TestMethod]
    public void RequireManualApprovalCreatesSimulatedApprovalRequest()
    {
        var request = new SimulatedManualApprovalBoundary().CreateRequest(SimulatedRuntimeRoutingMatrix.ManualApprovalCapability);

        Assert.AreEqual(SimulatedApprovalStatus.ApprovalRequiredSimulated, request.ApprovalStatus);
        Assert.AreEqual(SimulatedPolicyDecisionType.RequireManualApprovalSimulated, request.SourcePolicyDecision);
        Assert.IsTrue(request.ApprovalRequestId.StartsWith("approval-", StringComparison.Ordinal));
        Assert.AreEqual("HIGH_SIMULATED", request.RiskLevel);
        Assert.AreEqual("SIMULATED_APPROVAL_DECISION_REQUIRED", request.RequiredHumanDecision);
        Assert.IsNull(request.SelectedExecutor);
        Assert.IsFalse(request.CanExecute);
        Assert.IsFalse(request.ProductiveUnlockAllowed);
        AssertApprovalClean(request.EvidenceEnvelope, request.LedgerEvents, request.RedactionProof, request.NoExecutionProof);
    }

    [TestMethod]
    public void SimulatedApprovalRequestIsInMemoryOnlyAndDoesNotExecuteAutomatically()
    {
        var request = new SimulatedManualApprovalBoundary().CreateRequest(SimulatedRuntimeRoutingMatrix.ManualApprovalCapability);

        Assert.AreEqual(SimulatedDryRunOrchestrator.RuntimeType, request.EvidenceEnvelope.RuntimeType);
        Assert.AreEqual(SimulatedDryRunOrchestrator.RequiredFixtureType, request.EvidenceEnvelope.FixtureType);
        Assert.AreEqual(0, request.SideEffectSinkInvocations);
        Assert.IsFalse(request.NoExecutionProof.ProductiveEnabled);
        Assert.IsFalse(request.NoExecutionProof.ActualExecutionPerformed);
        CollectionAssert.Contains(request.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_APPROVAL_REQUEST_CREATED");
        CollectionAssert.Contains(request.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_APPROVAL_REQUIRED_EVALUATED");
    }

    [TestMethod]
    public void ApprovalGrantedSimulatedForAllowedCapabilityRemainsFakeOnly()
    {
        var outcome = new SimulatedManualApprovalBoundary().Decide("local_provider_model", SimulatedApprovalStatus.ApprovalGrantedSimulated);

        Assert.AreEqual(SimulatedApprovalStatus.ApprovalGrantedSimulated, outcome.ApprovalStatus);
        Assert.AreEqual(SimulatedApprovalCapabilityClass.Allowed, outcome.CapabilityClass);
        Assert.AreEqual(SimulatedApprovalReasonCodes.ApprovalGrantedSimulatedFakeOnly, outcome.ReasonCode);
        Assert.AreEqual("FakeLocalModelExecutor", outcome.SelectedExecutor);
        Assert.IsTrue(outcome.CanExecute);
        Assert.IsFalse(outcome.ProductiveUnlockAllowed);
        CollectionAssert.Contains(outcome.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_APPROVAL_GRANTED");
        AssertApprovalClean(outcome.EvidenceEnvelope, outcome.LedgerEvents, outcome.RedactionProof, outcome.NoExecutionProof);
    }

    [TestMethod]
    public void ApprovalGrantedSimulatedDoesNotOverrideDenylistUnsupportedOrPolicyViolation()
    {
        var boundary = new SimulatedManualApprovalBoundary();

        foreach (var capability in OverrideBlockedCapabilities)
        {
            var outcome = boundary.Decide(capability, SimulatedApprovalStatus.ApprovalGrantedSimulated);

            Assert.AreEqual(SimulatedApprovalStatus.ApprovalGrantedSimulated, outcome.ApprovalStatus, capability);
            Assert.IsNull(outcome.SelectedExecutor, capability);
            Assert.IsFalse(outcome.CanExecute, capability);
            Assert.IsFalse(outcome.ProductiveUnlockAllowed, capability);
            Assert.IsTrue(outcome.AuditEventCreated, capability);
            Assert.AreNotEqual(SimulatedApprovalReasonCodes.ApprovalGrantedSimulatedFakeOnly, outcome.ReasonCode, capability);
            AssertApprovalClean(outcome.EvidenceEnvelope, outcome.LedgerEvents, outcome.RedactionProof, outcome.NoExecutionProof);
        }
    }

    [TestMethod]
    public void ApprovalDeniedExpiredAndInvalidReturnNoExecution()
    {
        foreach (var status in new[]
        {
            SimulatedApprovalStatus.ApprovalDeniedSimulated,
            SimulatedApprovalStatus.ApprovalExpiredSimulated,
            SimulatedApprovalStatus.ApprovalInvalidSimulated
        })
        {
            var outcome = new SimulatedManualApprovalBoundary().Decide("local_provider_model", status);

            Assert.AreEqual(status, outcome.ApprovalStatus);
            Assert.IsNull(outcome.SelectedExecutor);
            Assert.IsFalse(outcome.CanExecute);
            Assert.IsFalse(outcome.ProductiveUnlockAllowed);
            AssertApprovalClean(outcome.EvidenceEnvelope, outcome.LedgerEvents, outcome.RedactionProof, outcome.NoExecutionProof);
        }
    }

    [TestMethod]
    public void ApprovalLedgerEventsAreProjectedRedactedAndNeverUnlockProductiveRuntime()
    {
        foreach (var outcome in new SimulatedManualApprovalBoundary().BuildAuditMatrix())
        {
            Assert.IsTrue(outcome.LedgerEvents.Count >= 5, outcome.SourceCapability);
            foreach (var item in outcome.LedgerEvents)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.EventId));
                Assert.AreEqual(outcome.ApprovalRequestId, item.ApprovalRequestId);
                Assert.AreEqual(outcome.SourceCapability, item.SourceCapability);
                Assert.AreEqual(outcome.ApprovalStatus, item.DecisionType);
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.ReasonCode));
                Assert.AreEqual("REDACTED_SIMULATED_APPROVAL_PAYLOAD", item.RedactedPayload);
                Assert.IsFalse(item.SecretsIncluded);
                Assert.IsFalse(item.RawUserDataIncluded);
                Assert.IsFalse(item.ExecutionPerformed);
                Assert.IsFalse(item.ProductiveUnlock);
            }
        }
    }

    [TestMethod]
    public void ApprovalEvidenceEnvelopeLinksLedgerRedactionAndNoExecutionProof()
    {
        var outcome = new SimulatedManualApprovalBoundary().Decide("local_provider_model", SimulatedApprovalStatus.ApprovalGrantedSimulated);
        var envelope = outcome.EvidenceEnvelope;

        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.EvidenceId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.SourceDecisionId));
        Assert.AreEqual(outcome.ApprovalRequestId, envelope.ApprovalRequestId);
        Assert.AreEqual(outcome.SourceCapability, envelope.SourceCapability);
        Assert.AreEqual(outcome.ApprovalStatus, envelope.ApprovalStatus);
        Assert.IsTrue(envelope.ReasonCodes.Count > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.NoExecutionProofRef));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.RedactionProofRef));
        CollectionAssert.AreEquivalent(outcome.LedgerEvents.Select(x => x.EventId).ToArray(), envelope.LedgerEventRefs.ToArray());
        Assert.AreEqual(SimulatedDryRunOrchestrator.RuntimeType, envelope.RuntimeType);
        Assert.AreEqual(SimulatedDryRunOrchestrator.RequiredFixtureType, envelope.FixtureType);
        AssertEnvelopeClean(envelope);
    }

    [TestMethod]
    public void ApprovalRedactionAndNoExecutionProofRemainCleanForEveryOutcome()
    {
        foreach (var outcome in new SimulatedManualApprovalBoundary().BuildAuditMatrix())
            AssertApprovalClean(outcome.EvidenceEnvelope, outcome.LedgerEvents, outcome.RedactionProof, outcome.NoExecutionProof);
    }

    [TestMethod]
    public void ApprovalAuditMatrixContainsDecisionTypesCapabilityClassesAndExpectedResults()
    {
        var boundary = new SimulatedManualApprovalBoundary();
        var matrix = boundary.BuildAuditMatrix();

        CollectionAssert.AreEquivalent(
            SimulatedManualApprovalBoundary.AuditDecisionTypes.ToArray(),
            matrix.Select(x => x.ApprovalStatus).Distinct().ToArray());
        CollectionAssert.AreEquivalent(
            SimulatedManualApprovalBoundary.AuditCapabilityClasses.ToArray(),
            matrix.Select(x => x.CapabilityClass).Distinct().ToArray());

        var allowedGranted = matrix.Single(x =>
            x.CapabilityClass == SimulatedApprovalCapabilityClass.Allowed &&
            x.ApprovalStatus == SimulatedApprovalStatus.ApprovalGrantedSimulated);
        Assert.IsTrue(allowedGranted.CanExecute);
        Assert.AreEqual("FakeLocalModelExecutor", allowedGranted.SelectedExecutor);

        foreach (var item in matrix.Where(x => x != allowedGranted))
        {
            Assert.IsFalse(item.CanExecute, $"{item.CapabilityClass}/{item.ApprovalStatus}");
            Assert.IsFalse(item.ProductiveUnlockAllowed, $"{item.CapabilityClass}/{item.ApprovalStatus}");
            if (item.CapabilityClass != SimulatedApprovalCapabilityClass.Allowed ||
                item.ApprovalStatus != SimulatedApprovalStatus.ApprovalGrantedSimulated)
            {
                Assert.IsNull(item.SelectedExecutor, $"{item.CapabilityClass}/{item.ApprovalStatus}");
            }
        }
    }

    [TestMethod]
    public void ApprovalArtifactsExistAndDescribeBoundaryProjectionAndAuditGate()
    {
        foreach (var path in new[]
        {
            ApprovalBoundaryPath,
            ApprovalOutcomesPath,
            ApprovalNegativePath,
            ApprovalLedgerPath,
            EvidenceConsistencyPath,
            RedactionNoExecutionPath,
            AuditMatrixPath,
            FlakeWatchPath,
            FinalPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        var boundary = ReadAll(ApprovalBoundaryPath);
        StringAssert.Contains(boundary, "APPROVAL_REQUIRED_SIMULATED");
        StringAssert.Contains(boundary, "\"productiveUnlockAllowed\": false");

        var final = ReadAll(FinalPath);
        StringAssert.Contains(final, "SIMULATED_MANUAL_APPROVAL_BOUNDARY_AUDIT_PROJECTION_READY");
        StringAssert.Contains(final, "\"manualApprovalSimulatedBoundary\": \"READY\"");
        StringAssert.Contains(final, "\"productiveEnabled\": \"PROHIBITED\"");
    }

    [TestMethod]
    public void ProductRuntimeReleaseStoreBridgeAndCspRemainBlocked()
    {
        var final = ReadAll(FinalPath);
        var productBridge = ReadAll(ProductBridgeCspPath);

        StringAssert.Contains(final, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(final, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(final, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(final, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(final, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(productBridge, "\"productFilesModified\": false");
        StringAssert.Contains(productBridge, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMilestoneRecommendsEvidenceTimelineRoundtrip()
    {
        var content = ReadAll(NextMilestonePath);

        StringAssert.Contains(content, "M815-M823");
        StringAssert.Contains(content, "Simulated Runtime Evidence Timeline Roundtrip + Approval Decision Replay Guard + Audit Export Readiness");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
    }

    private static void AssertApprovalClean(
        ApprovalEvidenceEnvelope envelope,
        IReadOnlyList<ApprovalLedgerEvent> ledgerEvents,
        RedactionProof redactionProof,
        NoExecutionProof proof)
    {
        Assert.IsNotNull(envelope);
        Assert.IsNotNull(ledgerEvents);
        Assert.IsTrue(ledgerEvents.Count > 0);
        Assert.IsNotNull(redactionProof);
        Assert.IsNotNull(proof);
        AssertEnvelopeClean(envelope);

        Assert.IsFalse(redactionProof.SecretsIncluded);
        Assert.IsFalse(redactionProof.CredentialsIncluded);
        Assert.IsFalse(redactionProof.TokensIncluded);
        Assert.IsFalse(redactionProof.CookiesIncluded);
        Assert.IsFalse(redactionProof.RawUserDataIncluded);
        Assert.IsFalse(redactionProof.RawLogsIncluded);
        Assert.IsFalse(redactionProof.ProviderKeysIncluded);
        Assert.IsFalse(redactionProof.PrivateKeysIncluded);
        Assert.IsFalse(redactionProof.BrowserSessionDataIncluded);

        Assert.IsFalse(proof.ActualExecutionPerformed);
        Assert.IsFalse(proof.LiveCallPerformed);
        Assert.IsFalse(proof.FilesystemWritePerformed);
        Assert.IsFalse(proof.BrowserAutomationPerformed);
        Assert.IsFalse(proof.CapabilityUnlocked);
        Assert.IsFalse(proof.PublicReleasePerformed);
        Assert.IsFalse(proof.StoreSubmissionPerformed);
        Assert.IsFalse(proof.SignedPublicZipCreated);
        Assert.IsFalse(proof.ProductFilesModified);
        Assert.IsFalse(proof.BridgeCspModified);
        Assert.IsFalse(proof.ProductiveEnabled);
        Assert.AreEqual(0, proof.SideEffectSinkInvocations);
    }

    private static void AssertEnvelopeClean(ApprovalEvidenceEnvelope envelope)
    {
        Assert.IsFalse(envelope.ProductiveRuntime);
        Assert.IsFalse(envelope.ProviderCloudInvoked);
        Assert.IsFalse(envelope.FilesystemWritePerformed);
        Assert.IsFalse(envelope.BrowserAutomationPerformed);
        Assert.IsFalse(envelope.CapabilityUnlocked);
        Assert.IsFalse(envelope.ReleasePerformed);
        Assert.IsFalse(envelope.StoreSubmissionPerformed);
        Assert.IsFalse(envelope.ProductFilesModified);
        Assert.IsFalse(envelope.BridgeCspModified);
    }
}
