using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedRuntimeExecution")]
[TestCategory("M785")]
[TestCategory("M786")]
[TestCategory("M787")]
public sealed class NodalOsSimulatedRuntimeExecutionM785M787Tests
{
    private static readonly string[] PositiveCases =
    [
        "positive_allow_simulated_local_model",
        "positive_allow_simulated_filesystem_read_metadata",
        "positive_allow_simulated_ledger_append"
    ];

    private static readonly string[] NegativeCases =
    [
        "negative_deny_provider_live_call",
        "negative_deny_filesystem_write",
        "negative_deny_browser_action",
        "negative_deny_capability_unlock",
        "negative_deny_public_release",
        "negative_deny_store_submission"
    ];

    private const string ExecutionTestsPath = "artifacts/agent-operations/m785/simulated-runtime-in-memory-execution-tests.json";
    private const string PositiveCasesPath = "artifacts/agent-operations/m785/in-memory-positive-execution-cases.json";
    private const string NegativeCasesPath = "artifacts/agent-operations/m785/in-memory-negative-execution-cases.json";
    private const string RequireApprovalCasesPath = "artifacts/agent-operations/m785/in-memory-require-approval-cases.json";
    private const string NoSideEffectPath = "artifacts/agent-operations/m785/in-memory-no-side-effect-proof.json";
    private const string EnforcementProofPath = "artifacts/agent-operations/m786/enforcement-proof.json";
    private const string AllowProofPath = "artifacts/agent-operations/m786/allow-path-enforcement-proof.json";
    private const string DenyProofPath = "artifacts/agent-operations/m786/deny-path-enforcement-proof.json";
    private const string RequireApprovalProofPath = "artifacts/agent-operations/m786/require-approval-enforcement-proof.json";
    private const string NoRealExecutorPath = "artifacts/agent-operations/m786/no-real-executor-invocation-proof.json";
    private const string NoSideEffectSinkPath = "artifacts/agent-operations/m786/no-side-effect-sink-invocation-proof.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m787/simulated-runtime-next-decision.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m787/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m787/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m785-m787/simulated-runtime-enforcement-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static JsonDocument ReadJson(string relativePath) => JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));
    private static string Str(JsonDocument d, string p) => d.RootElement.GetProperty(p).GetString() ?? "";
    private static bool Bool(JsonDocument d, string p) => d.RootElement.GetProperty(p).GetBoolean();

    [TestMethod]
    public void InMemorySimulatedRuntimeArtifactsExist()
    {
        foreach (var path in new[]
        {
            ExecutionTestsPath,
            PositiveCasesPath,
            NegativeCasesPath,
            RequireApprovalCasesPath,
            NoSideEffectPath,
            EnforcementProofPath,
            AllowProofPath,
            DenyProofPath,
            RequireApprovalProofPath,
            NoRealExecutorPath,
            NoSideEffectSinkPath,
            NextDecisionPath,
            ProductBridgeCspPath,
            NextMilestonePath,
            ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void PositiveNegativeAndRequireApprovalCasesExist()
    {
        using var positives = ReadJson(PositiveCasesPath);
        using var negatives = ReadJson(NegativeCasesPath);
        using var approvals = ReadJson(RequireApprovalCasesPath);

        var positiveCases = positives.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();
        foreach (var item in PositiveCases)
            CollectionAssert.Contains(positiveCases, item);

        var negativeCases = negatives.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();
        foreach (var item in NegativeCases)
            CollectionAssert.Contains(negativeCases, item);

        var approvalCases = approvals.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.Contains(approvalCases, "require_approval_high_risk_simulated_request");
        Assert.AreEqual("SIMULATED_FAKE_ONLY_IN_MEMORY", Str(positives, "runtimeType"));
        Assert.AreEqual("SIMULATED_FAKE_ONLY_IN_MEMORY", Str(negatives, "runtimeType"));
        Assert.AreEqual("SIMULATED_FAKE_ONLY_IN_MEMORY", Str(approvals, "runtimeType"));
    }

    [TestMethod]
    public void AllowBranchReturnsExpectedSimulatedResponseWithoutSideEffects()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);

        foreach (var capability in PositiveCases)
        {
            var result = orchestrator.Process(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", capability, false));
            Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, result.Decision, capability);
            Assert.IsTrue(result.EvidenceEnvelopeCreated, capability);
            Assert.IsTrue(result.LedgerProjected, capability);
            Assert.IsTrue(result.RedactionProofCreated, capability);
            AssertNoExecutionProof(result.Proof);
        }

        AssertSinkUntouched(sink);
    }

    [TestMethod]
    public void DenyBranchReturnsExpectedDeniedResponseWithoutSideEffects()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);

        foreach (var capability in NegativeCases)
        {
            var result = orchestrator.Process(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", capability, true));
            Assert.AreEqual(SimulatedDecision.Deny, result.Decision, capability);
            Assert.IsTrue(result.EvidenceEnvelopeCreated, capability);
            Assert.IsTrue(result.LedgerProjected, capability);
            AssertNoExecutionProof(result.Proof);
        }

        AssertSinkUntouched(sink);
    }

    [TestMethod]
    public void RequireManualApprovalBranchReturnsApprovalGateWithoutSideEffects()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);
        var result = orchestrator.Process(new SimulatedRequest(
            "SIMULATED_DRY_RUN",
            "SIMULATED_FAKE_ONLY",
            "require_approval_high_risk_simulated_request",
            false,
            RequiresManualApproval: true,
            ManualApprovalGranted: false));

        Assert.AreEqual(SimulatedDecision.RequireManualApproval, result.Decision);
        Assert.IsTrue(result.EvidenceEnvelopeCreated);
        Assert.IsTrue(result.LedgerProjected);
        Assert.IsTrue(result.RedactionProofCreated);
        AssertNoExecutionProof(result.Proof);
        AssertSinkUntouched(sink);
    }

    [TestMethod]
    public void EachBranchArtifactKeepsNoExecutionFlagsAndZeroSideEffects()
    {
        foreach (var path in new[] { PositiveCasesPath, NegativeCasesPath, RequireApprovalCasesPath, ExecutionTestsPath })
        {
            using var doc = ReadJson(path);
            Assert.AreEqual("SIMULATED_FAKE_ONLY_IN_MEMORY", Str(doc, "runtimeType"));
            AssertNoExecutionFlags(doc);
            Assert.AreEqual(0, doc.RootElement.GetProperty("sideEffectSinkInvocations").GetInt32());
        }
    }

    [TestMethod]
    public void EnforcementProofCoversAllowDenyAndRequireApproval()
    {
        using var enforcement = ReadJson(EnforcementProofPath);
        using var allow = ReadJson(AllowProofPath);
        using var deny = ReadJson(DenyProofPath);
        using var approval = ReadJson(RequireApprovalProofPath);

        Assert.AreEqual("READY", Str(enforcement, "status"));
        Assert.AreEqual("ALLOW_SIMULATED_DRY_RUN", Str(allow, "decision"));
        Assert.IsTrue(Bool(allow, "simulatedOnly"));
        Assert.IsTrue(Bool(allow, "evidenceEnvelopeCreated"));
        Assert.IsTrue(Bool(allow, "ledgerProjectionCreated"));
        Assert.AreEqual("DENY", Str(deny, "decision"));
        Assert.IsTrue(Bool(deny, "auditEventCreated"));
        Assert.IsTrue(Bool(deny, "evidenceEnvelopeCreated"));
        Assert.AreEqual("REQUIRE_MANUAL_APPROVAL", Str(approval, "decision"));
        Assert.IsTrue(Bool(approval, "approvalGateCreated"));
        Assert.IsFalse(Bool(approval, "actualExecutionPerformed"));
    }

    [TestMethod]
    public void NoRealExecutorOrSideEffectSinkIsInvoked()
    {
        foreach (var path in new[] { NoRealExecutorPath, NoSideEffectSinkPath, NoSideEffectPath })
        {
            using var doc = ReadJson(path);
            var root = doc.RootElement;
            Assert.AreEqual("PROVEN", root.GetProperty("status").GetString(), path);
            Assert.IsFalse(root.GetProperty("realExecutorInvoked").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("providerClientInvoked").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("filesystemWriterInvoked").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("browserAutomationInvoked").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("capabilityUnlockInvoked").GetBoolean(), path);
        }

        using var sink = ReadJson(NoSideEffectSinkPath);
        Assert.AreEqual(0, sink.RootElement.GetProperty("sideEffectSinkInvocations").GetInt32());
    }

    [TestMethod]
    public void RuntimeReleaseStoreProductBridgeAndCspRemainBlocked()
    {
        using var consolidated = ReadJson(ConsolidatedPath);
        using var productBridge = ReadJson(ProductBridgeCspPath);

        Assert.AreEqual("DISABLED", Str(consolidated, "runtimeProductiveExecution"));
        Assert.AreEqual("DISABLED", Str(consolidated, "providerCloudLiveCalls"));
        Assert.AreEqual("DISABLED", Str(consolidated, "filesystemBrowserCapabilityUnlock"));
        Assert.AreEqual("NO-GO", Str(consolidated, "publicRelease"));
        Assert.AreEqual("NO-GO", Str(consolidated, "chromeWebStore"));
        Assert.IsFalse(Bool(productBridge, "productFilesModified"));
        Assert.IsFalse(Bool(productBridge, "bridgeModified"));
        Assert.IsFalse(Bool(productBridge, "cspModified"));
        Assert.IsFalse(Bool(productBridge, "bridgeCspModified"));
    }

    [TestMethod]
    public void NextMilestoneIsEvidenceLedgerIntegrationAuditGate()
    {
        using var doc = ReadJson(NextMilestonePath);

        Assert.AreEqual(
            "M788-M790 — Simulated Runtime Evidence/Ledger Integration Tests + Audit Gate",
            Str(doc, "recommendedNextMilestone"));
        Assert.AreEqual("simulated runtime/tests only", Str(doc, "scope"));
        Assert.IsFalse(Bool(doc, "productiveUnlockAllowed"));
        Assert.IsFalse(Bool(doc, "publicReleaseAllowed"));
        Assert.IsFalse(Bool(doc, "storeSubmissionAllowed"));
    }

    [TestMethod]
    public void FinalDecisionIsSimulatedRuntimeEnforcementReady()
    {
        using var doc = ReadJson(ConsolidatedPath);

        Assert.AreEqual("SIMULATED_RUNTIME_IN_MEMORY_EXECUTION_ENFORCEMENT_PROOF_READY", Str(doc, "decision"));
        Assert.AreEqual("READY", Str(doc, "simulatedRuntimeInMemoryExecutionTests"));
        Assert.AreEqual("READY", Str(doc, "enforcementProof"));
        Assert.AreEqual("PROVEN", Str(doc, "noRealExecutorWired"));
        Assert.AreEqual("PROVEN", Str(doc, "noSideEffectSinkInvocation"));
        Assert.AreEqual("PROHIBITED", Str(doc, "productiveEnabled"));
        Assert.AreEqual(0, doc.RootElement.GetProperty("sideEffectSinkInvocations").GetInt32());
    }

    private static void AssertNoExecutionProof(NoExecutionProof proof)
    {
        Assert.IsTrue(proof.SimulationOnly);
        Assert.IsFalse(proof.RealExecutorInvoked);
        Assert.IsFalse(proof.ProviderClientInvoked);
        Assert.IsFalse(proof.FilesystemWriterInvoked);
        Assert.IsFalse(proof.BrowserAutomationInvoked);
        Assert.IsFalse(proof.CapabilityUnlockInvoked);
        Assert.IsFalse(proof.PublicReleaseInvoked);
        Assert.IsFalse(proof.StoreSubmissionInvoked);
        Assert.IsFalse(proof.SignedZipCreated);
        Assert.IsFalse(proof.ProductFilesModified);
        Assert.IsFalse(proof.BridgeCspModified);
    }

    private static void AssertNoExecutionFlags(JsonDocument doc)
    {
        Assert.IsFalse(Bool(doc, "actualExecutionPerformed"));
        Assert.IsFalse(Bool(doc, "liveCallPerformed"));
        Assert.IsFalse(Bool(doc, "filesystemWritePerformed"));
        Assert.IsFalse(Bool(doc, "browserAutomationPerformed"));
        Assert.IsFalse(Bool(doc, "capabilityUnlocked"));
        Assert.IsFalse(Bool(doc, "publicReleasePerformed"));
        Assert.IsFalse(Bool(doc, "storeSubmissionPerformed"));
        Assert.IsFalse(Bool(doc, "signedPublicZipCreated"));
    }

    private static void AssertSinkUntouched(RecordingSideEffectSink sink)
    {
        Assert.IsFalse(sink.AnyInvoked);
        Assert.IsFalse(sink.RealExecutorInvoked);
        Assert.IsFalse(sink.ProviderClientInvoked);
        Assert.IsFalse(sink.FilesystemWriterInvoked);
        Assert.IsFalse(sink.BrowserAutomationInvoked);
        Assert.IsFalse(sink.CapabilityUnlockInvoked);
        Assert.IsFalse(sink.PublicReleaseInvoked);
        Assert.IsFalse(sink.StoreSubmissionInvoked);
        Assert.IsFalse(sink.SignedZipCreated);
    }
}
