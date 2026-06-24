using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InMemoryEvidenceLedger")]
[TestCategory("M791")]
[TestCategory("M792")]
[TestCategory("M793")]
public sealed class NodalOsInMemoryEvidenceLedgerM791M793Tests
{
    private const string TerminologyPath = "artifacts/agent-operations/m792/runtime-fixture-terminology-alignment.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m793/next-milestone-recommendation.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m793/product-bridge-csp-unchanged-proof.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m791-m793/in-memory-evidence-ledger-go-no-go.json";

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
    public void InMemoryEvidenceLedgerAndObjectsExist()
    {
        var ledger = new InMemoryEvidenceLedger();
        var redaction = new RedactionProof(false, false, false, false, false, false, false, false, false);
        var proof = new NoExecutionProof(true, false, false, false, false, false, false, false, false, false, false);
        var item = ledger.Append("SIMULATED_DRY_RUN_REQUESTED", "req-1", "dry-1", "capability", "env-1");
        var envelope = new EvidenceEnvelope(
            "env-1",
            "dry-1",
            "req-1",
            "capability",
            SimulatedDecision.AllowSimulatedDryRun,
            true,
            redaction,
            [item.EventId],
            proof,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false);

        Assert.IsNotNull(ledger);
        Assert.IsNotNull(item);
        Assert.IsNotNull(envelope);
        Assert.IsNotNull(redaction);
        Assert.IsNotNull(proof);
        Assert.AreEqual(1, ledger.Events.Count);
    }

    [TestMethod]
    public void AllowBranchReturnsEvidenceLedgerRedactionAndNoExecutionObjects()
    {
        var result = Execute(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", "positive_allow_simulated_local_model", false));

        Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, result.Decision);
        Assert.AreEqual(SimulatedDryRunOrchestrator.RuntimeType, result.RuntimeType);
        Assert.AreEqual(SimulatedDryRunOrchestrator.RequiredFixtureType, result.FixtureType);
        Assert.IsNotNull(result.EvidenceEnvelope);
        Assert.IsNotNull(result.RedactionProof);
        Assert.IsNotNull(result.Proof);
        Assert.IsTrue(result.LedgerEvents.Count >= 4);
        CollectionAssert.Contains(result.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_ACTION_ALLOWED_FOR_DRY_RUN");
        AssertCleanResult(result);
    }

    [TestMethod]
    public void DenyBranchReturnsEvidenceLedgerRedactionAndNoExecutionObjects()
    {
        var result = Execute(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", "negative_deny_filesystem_write", true));

        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        Assert.IsNotNull(result.EvidenceEnvelope);
        Assert.IsNotNull(result.RedactionProof);
        Assert.IsNotNull(result.Proof);
        Assert.IsTrue(result.LedgerEvents.Count >= 3);
        CollectionAssert.Contains(result.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_ACTION_DENIED");
        AssertCleanResult(result);
    }

    [TestMethod]
    public void RequireManualApprovalBranchReturnsApprovalLedgerEvent()
    {
        var result = Execute(new SimulatedRequest(
            "SIMULATED_DRY_RUN",
            "SIMULATED_FAKE_ONLY",
            "require_approval_high_risk_simulated_request",
            false,
            RequiresManualApproval: true,
            ManualApprovalGranted: false));

        Assert.AreEqual(SimulatedDecision.RequireManualApproval, result.Decision);
        CollectionAssert.Contains(result.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_MANUAL_APPROVAL_EVALUATED");
        CollectionAssert.Contains(result.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_MANUAL_APPROVAL_REQUIRED");
        AssertCleanResult(result);
    }

    [TestMethod]
    public void EveryLedgerEventReferencesTheEvidenceEnvelopeAndNoExecutionFlags()
    {
        var result = Execute(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", "positive_allow_simulated_ledger_append", false));

        foreach (var item in result.LedgerEvents)
        {
            Assert.AreEqual(result.EvidenceEnvelope.EnvelopeId, item.EvidenceEnvelopeRef);
            Assert.IsTrue(item.SimulationOnly);
            Assert.IsFalse(item.ActualExecutionPerformed);
            Assert.IsFalse(item.LiveCallPerformed);
            Assert.IsFalse(item.FilesystemWritePerformed);
            Assert.IsFalse(item.BrowserAutomationPerformed);
            Assert.IsFalse(item.CapabilityUnlocked);
            Assert.IsFalse(item.PublicReleasePerformed);
            Assert.IsFalse(item.StoreSubmissionPerformed);
            Assert.IsFalse(item.SignedPublicZipCreated);
        }
    }

    [TestMethod]
    public void EvidenceEnvelopeCarriesRedactionLedgerRefsAndNoExecutionProof()
    {
        var result = Execute(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", "positive_allow_simulated_filesystem_read_metadata", false));
        var envelope = result.EvidenceEnvelope;

        Assert.AreEqual(result.RedactionProof, envelope.RedactionProof);
        Assert.AreEqual(result.Proof, envelope.NoExecutionProof);
        Assert.AreEqual(result.LedgerEvents.Count, envelope.LedgerEventRefs.Count);
        Assert.IsTrue(envelope.SimulationOnly);
        Assert.IsFalse(envelope.ActualExecutionPerformed);
        Assert.IsFalse(envelope.LiveCallPerformed);
        Assert.IsFalse(envelope.FilesystemWritePerformed);
        Assert.IsFalse(envelope.BrowserAutomationPerformed);
        Assert.IsFalse(envelope.CapabilityUnlocked);
        Assert.IsFalse(envelope.PublicReleasePerformed);
        Assert.IsFalse(envelope.StoreSubmissionPerformed);
        Assert.IsFalse(envelope.SignedPublicZipCreated);
    }

    [TestMethod]
    public void RedactionProofCoversAllForbiddenFields()
    {
        var result = Execute(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", "redaction_check", false));
        var proof = result.RedactionProof;

        Assert.IsFalse(proof.SecretsIncluded);
        Assert.IsFalse(proof.CredentialsIncluded);
        Assert.IsFalse(proof.TokensIncluded);
        Assert.IsFalse(proof.CookiesIncluded);
        Assert.IsFalse(proof.RawUserDataIncluded);
        Assert.IsFalse(proof.RawLogsIncluded);
        Assert.IsFalse(proof.ProviderKeysIncluded);
        Assert.IsFalse(proof.PrivateKeysIncluded);
        Assert.IsFalse(proof.BrowserSessionDataIncluded);
    }

    [TestMethod]
    public void RuntimeFixtureTerminologyAlignmentArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(TerminologyPath)));
        var content = ReadAll(TerminologyPath);
        StringAssert.Contains(content, "SIMULATED_FAKE_ONLY_IN_MEMORY");
        StringAssert.Contains(content, "SIMULATED_FAKE_ONLY");
        StringAssert.Contains(content, "contradiction");
    }

    [TestMethod]
    public void RuntimeReleaseStoreProductBridgeAndCspRemainBlocked()
    {
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedPath)));
        Assert.IsTrue(File.Exists(FullPath(ProductBridgeCspPath)));
        var consolidated = ReadAll(ConsolidatedPath);
        var productBridge = ReadAll(ProductBridgeCspPath);

        StringAssert.Contains(consolidated, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(consolidated, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(consolidated, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(consolidated, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(consolidated, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(productBridge, "\"productFilesModified\": false");
        StringAssert.Contains(productBridge, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMilestoneRecommendsFakeExecutorsCollectorEnforcementTests()
    {
        Assert.IsTrue(File.Exists(FullPath(NextMilestonePath)));
        var content = ReadAll(NextMilestonePath);
        StringAssert.Contains(content, "M794-M796");
        StringAssert.Contains(content, "Capability-Specific Fake Executors In-Memory Plan + Collector Enforcement Tests");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
        StringAssert.Contains(content, "\"publicReleaseAllowed\": false");
        StringAssert.Contains(content, "\"storeSubmissionAllowed\": false");
    }

    [TestMethod]
    public void FinalDecisionIsInMemoryEvidenceLedgerReady()
    {
        var content = ReadAll(ConsolidatedPath);
        StringAssert.Contains(content, "IN_MEMORY_EVIDENCE_LEDGER_RUNTIME_RESULT_OBJECTS_READY");
        StringAssert.Contains(content, "\"inMemoryEvidenceLedger\": \"READY\"");
        StringAssert.Contains(content, "\"simulatedRuntimeResultObjectProjection\": \"READY\"");
        StringAssert.Contains(content, "\"productiveEnabled\": \"PROHIBITED\"");
        StringAssert.Contains(content, "\"sideEffectSinkInvocations\": 0");
    }

    private static SimulatedRuntimeResult Execute(SimulatedRequest request)
    {
        var sink = new RecordingSideEffectSink();
        var result = new SimulatedDryRunOrchestrator(sink).Process(request);
        Assert.IsFalse(sink.AnyInvoked);
        return result;
    }

    private static void AssertCleanResult(SimulatedRuntimeResult result)
    {
        Assert.AreEqual(0, result.SideEffectSinkInvocations);
        Assert.IsFalse(result.RealExecutorInvoked);
        Assert.IsFalse(result.ProviderClientInvoked);
        Assert.IsFalse(result.FilesystemWriterInvoked);
        Assert.IsFalse(result.BrowserAutomationInvoked);
        Assert.IsFalse(result.CapabilityUnlockInvoked);
        Assert.IsFalse(result.PublicReleaseInvoked);
        Assert.IsFalse(result.StoreSubmissionInvoked);
        Assert.IsFalse(result.SignedZipCreated);
        Assert.IsFalse(result.ProductFilesModified);
        Assert.IsFalse(result.BridgeCspModified);
        Assert.IsFalse(result.Proof.ActualExecutionPerformed);
        Assert.IsFalse(result.Proof.LiveCallPerformed);
        Assert.IsFalse(result.Proof.FilesystemWritePerformed);
        Assert.IsFalse(result.Proof.BrowserAutomationPerformed);
        Assert.IsFalse(result.Proof.CapabilityUnlocked);
        Assert.IsFalse(result.Proof.PublicReleasePerformed);
        Assert.IsFalse(result.Proof.StoreSubmissionPerformed);
        Assert.IsFalse(result.Proof.SignedPublicZipCreated);
    }
}
