using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedRuntimeEvidenceLedger")]
[TestCategory("M788")]
[TestCategory("M789")]
[TestCategory("M790")]
public sealed class NodalOsSimulatedRuntimeEvidenceLedgerM788M790Tests
{
    private static readonly string[] AllowCases =
    [
        "allow_simulated_local_model_creates_evidence_and_ledger",
        "allow_simulated_filesystem_read_metadata_creates_evidence_and_ledger"
    ];

    private static readonly string[] DenyCases =
    [
        "deny_provider_live_call_creates_denial_evidence_and_ledger",
        "deny_filesystem_write_creates_denial_evidence_and_ledger",
        "deny_browser_action_creates_denial_evidence_and_ledger"
    ];

    private static readonly string[] ForbiddenFlags =
    [
        "secretsIncluded",
        "credentialsIncluded",
        "tokensIncluded",
        "cookiesIncluded",
        "rawUserDataIncluded",
        "rawLogsIncluded",
        "providerKeysIncluded",
        "privateKeysIncluded",
        "browserSessionDataIncluded"
    ];

    private const string IntegrationTestsPath = "artifacts/agent-operations/m788/simulated-runtime-evidence-ledger-integration-tests.json";
    private const string AllowPathPath = "artifacts/agent-operations/m788/allow-path-evidence-ledger-integration.json";
    private const string DenyPathPath = "artifacts/agent-operations/m788/deny-path-evidence-ledger-integration.json";
    private const string RequireApprovalPath = "artifacts/agent-operations/m788/require-approval-evidence-ledger-integration.json";
    private const string IntegrationNoSideEffectPath = "artifacts/agent-operations/m788/integration-no-side-effect-proof.json";
    private const string IntegratedProofPath = "artifacts/agent-operations/m789/integrated-evidence-ledger-proof.json";
    private const string EnvelopeProofPath = "artifacts/agent-operations/m789/integrated-evidence-envelope-proof.json";
    private const string LedgerProofPath = "artifacts/agent-operations/m789/integrated-ledger-event-proof.json";
    private const string RedactionProofPath = "artifacts/agent-operations/m789/integrated-redaction-proof.json";
    private const string NoExecutionProofPath = "artifacts/agent-operations/m789/integrated-no-execution-proof.json";
    private const string AuditSummaryProofPath = "artifacts/agent-operations/m789/integrated-audit-summary-proof.json";
    private const string AuditGatePath = "artifacts/agent-operations/m790/audit-gate-next-decision.json";
    private const string PostAuditGatePath = "artifacts/agent-operations/m790/post-simulated-runtime-audit-gate.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m790/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m790/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m788-m790/simulated-runtime-evidence-ledger-audit-go-no-go.json";

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
    public void EvidenceLedgerIntegrationArtifactsExist()
    {
        foreach (var path in new[]
        {
            IntegrationTestsPath,
            AllowPathPath,
            DenyPathPath,
            RequireApprovalPath,
            IntegrationNoSideEffectPath,
            IntegratedProofPath,
            EnvelopeProofPath,
            LedgerProofPath,
            RedactionProofPath,
            NoExecutionProofPath,
            AuditSummaryProofPath,
            AuditGatePath,
            PostAuditGatePath,
            ProductBridgeCspPath,
            NextMilestonePath,
            ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void AllowDenyAndRequireApprovalIntegrationCasesExist()
    {
        using var allow = ReadJson(AllowPathPath);
        using var deny = ReadJson(DenyPathPath);
        using var approval = ReadJson(RequireApprovalPath);

        var allowCases = allow.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();
        foreach (var item in AllowCases)
            CollectionAssert.Contains(allowCases, item);

        var denyCases = deny.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();
        foreach (var item in DenyCases)
            CollectionAssert.Contains(denyCases, item);

        var approvalCases = approval.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.Contains(approvalCases, "require_approval_high_risk_creates_approval_gate_evidence_and_ledger");
    }

    [TestMethod]
    public void EachIntegrationCaseCreatesEvidenceLedgerRedactionNoExecutionAndAudit()
    {
        foreach (var path in new[] { IntegrationTestsPath, AllowPathPath, DenyPathPath, RequireApprovalPath })
        {
            using var doc = ReadJson(path);
            Assert.AreEqual("SIMULATED_FAKE_ONLY_IN_MEMORY", Str(doc, "runtimeType"), path);
            Assert.IsTrue(Bool(doc, "evidenceEnvelopeCreated"), path);
            Assert.IsTrue(Bool(doc, "ledgerProjectionCreated"), path);
            Assert.IsTrue(Bool(doc, "redactionProofCreated"), path);
            Assert.IsTrue(Bool(doc, "noExecutionProofCreated"), path);
            Assert.IsTrue(Bool(doc, "auditSummaryCreated"), path);
            AssertNoExecutionFlags(doc);
            Assert.AreEqual(0, doc.RootElement.GetProperty("sideEffectSinkInvocations").GetInt32(), path);
        }
    }

    [TestMethod]
    public void InMemoryRuntimeExecutionProjectsEvidenceAndLedgerWithoutSideEffects()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);

        foreach (var item in AllowCases)
        {
            var result = orchestrator.Process(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", item, false));
            Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, result.Decision, item);
            AssertRuntimeProjection(result);
        }

        foreach (var item in DenyCases)
        {
            var result = orchestrator.Process(new SimulatedRequest("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", item, true));
            Assert.AreEqual(SimulatedDecision.Deny, result.Decision, item);
            AssertRuntimeProjection(result);
        }

        var approval = orchestrator.Process(new SimulatedRequest(
            "SIMULATED_DRY_RUN",
            "SIMULATED_FAKE_ONLY",
            "require_approval_high_risk_creates_approval_gate_evidence_and_ledger",
            false,
            RequiresManualApproval: true,
            ManualApprovalGranted: false));
        Assert.AreEqual(SimulatedDecision.RequireManualApproval, approval.Decision);
        AssertRuntimeProjection(approval);
        AssertSinkUntouched(sink);
    }

    [TestMethod]
    public void IntegratedEvidenceEnvelopeProofHasRequiredFields()
    {
        using var doc = ReadJson(EnvelopeProofPath);
        var root = doc.RootElement;

        Assert.AreEqual("READY", Str(doc, "status"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("envelopeId").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("dryRunId").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("requestId").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("capabilityName").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("decision").GetString()));
        Assert.IsTrue(Bool(doc, "simulationOnly"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("redactionProofRef").GetString()));
        Assert.IsTrue(root.GetProperty("ledgerEventRefs").GetArrayLength() > 0);
        AssertNoExecutionFlags(doc);
    }

    [TestMethod]
    public void IntegratedLedgerEventProofHasRequiredFields()
    {
        using var doc = ReadJson(LedgerProofPath);
        var root = doc.RootElement;

        Assert.AreEqual("READY", Str(doc, "status"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("eventId").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("eventType").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("requestId").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("dryRunId").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("capabilityName").GetString()));
        Assert.IsTrue(Bool(doc, "simulationOnly"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("evidenceEnvelopeRef").GetString()));
        AssertNoExecutionFlags(doc);
    }

    [TestMethod]
    public void IntegratedRedactionProofCoversAllForbiddenFields()
    {
        using var doc = ReadJson(RedactionProofPath);
        Assert.AreEqual("READY", Str(doc, "status"));
        foreach (var flag in ForbiddenFlags)
            Assert.IsFalse(Bool(doc, flag), flag);
    }

    [TestMethod]
    public void IntegratedNoExecutionProofHasAllInvocationsFalse()
    {
        using var doc = ReadJson(NoExecutionProofPath);
        var root = doc.RootElement;

        Assert.AreEqual("PROVEN", Str(doc, "status"));
        Assert.AreEqual(0, root.GetProperty("sideEffectSinkInvocations").GetInt32());
        Assert.IsFalse(Bool(doc, "realExecutorInvoked"));
        Assert.IsFalse(Bool(doc, "providerClientInvoked"));
        Assert.IsFalse(Bool(doc, "filesystemWriterInvoked"));
        Assert.IsFalse(Bool(doc, "browserAutomationInvoked"));
        Assert.IsFalse(Bool(doc, "capabilityUnlockInvoked"));
        Assert.IsFalse(Bool(doc, "publicReleaseInvoked"));
        Assert.IsFalse(Bool(doc, "storeSubmissionInvoked"));
        Assert.IsFalse(Bool(doc, "signedZipCreated"));
        Assert.IsFalse(Bool(doc, "productFilesModified"));
        Assert.IsFalse(Bool(doc, "bridgeCspModified"));
    }

    [TestMethod]
    public void IntegratedAuditSummaryProofIsReady()
    {
        using var doc = ReadJson(AuditSummaryProofPath);
        Assert.AreEqual("READY", Str(doc, "status"));
        Assert.IsTrue(Bool(doc, "allowPathCovered"));
        Assert.IsTrue(Bool(doc, "denyPathCovered"));
        Assert.IsTrue(Bool(doc, "requireApprovalPathCovered"));
        Assert.IsTrue(Bool(doc, "evidenceEnvelopeCovered"));
        Assert.IsTrue(Bool(doc, "ledgerEventsCovered"));
        Assert.IsTrue(Bool(doc, "redactionProofCovered"));
        Assert.IsTrue(Bool(doc, "noExecutionProofCovered"));
        Assert.AreEqual(0, doc.RootElement.GetProperty("sideEffectSinkInvocations").GetInt32());
    }

    [TestMethod]
    public void AuditGateRecommendsClaudeAuditPostM790()
    {
        using var audit = ReadJson(AuditGatePath);
        using var gate = ReadJson(PostAuditGatePath);
        using var next = ReadJson(NextMilestonePath);

        Assert.AreEqual("READY", Str(audit, "auditGate"));
        StringAssert.Contains(Str(audit, "recommendedAudit"), "AUDIT POST M790");
        StringAssert.Contains(Str(audit, "preferredRecommendation"), "CLAUDE AUDIT POST M790");
        StringAssert.Contains(Str(gate, "recommendedAudit"), "AUDIT POST M790");
        StringAssert.Contains(Str(next, "preferredNextMilestone"), "CLAUDE AUDIT POST M790");
        StringAssert.Contains(Str(next, "fallbackNextMilestone"), "M791-M793");
        Assert.IsFalse(Bool(next, "productiveUnlockAllowed"));
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
    public void FinalDecisionIsEvidenceLedgerAuditGateReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("SIMULATED_RUNTIME_EVIDENCE_LEDGER_INTEGRATION_AUDIT_GATE_READY", Str(doc, "decision"));
        Assert.AreEqual("READY", Str(doc, "simulatedRuntimeEvidenceLedgerIntegrationTests"));
        Assert.AreEqual("READY", Str(doc, "integratedEvidenceLedgerProof"));
        Assert.AreEqual("READY", Str(doc, "auditGate"));
        Assert.AreEqual("PROHIBITED", Str(doc, "productiveEnabled"));
        Assert.AreEqual(0, doc.RootElement.GetProperty("sideEffectSinkInvocations").GetInt32());
    }

    private static void AssertRuntimeProjection(SimulatedRuntimeResult result)
    {
        Assert.IsTrue(result.EvidenceEnvelopeCreated);
        Assert.IsTrue(result.LedgerProjected);
        Assert.IsTrue(result.RedactionProofCreated);
        Assert.IsTrue(result.Proof.SimulationOnly);
        Assert.IsFalse(result.Proof.RealExecutorInvoked);
        Assert.IsFalse(result.Proof.ProviderClientInvoked);
        Assert.IsFalse(result.Proof.FilesystemWriterInvoked);
        Assert.IsFalse(result.Proof.BrowserAutomationInvoked);
        Assert.IsFalse(result.Proof.CapabilityUnlockInvoked);
        Assert.IsFalse(result.Proof.PublicReleaseInvoked);
        Assert.IsFalse(result.Proof.StoreSubmissionInvoked);
        Assert.IsFalse(result.Proof.SignedZipCreated);
        Assert.IsFalse(result.Proof.ProductFilesModified);
        Assert.IsFalse(result.Proof.BridgeCspModified);
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
