using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedOrchestrator")]
[TestCategory("M770")]
[TestCategory("M771")]
[TestCategory("M772")]
public sealed class NodalOsSimulatedOrchestratorM770M772Tests
{
    private static readonly string[] RequiredEventTypes =
    [
        "SIMULATED_DRY_RUN_REQUESTED",
        "SIMULATED_POLICY_GATE_EVALUATED",
        "SIMULATED_MANUAL_APPROVAL_EVALUATED",
        "SIMULATED_ACTION_DENIED",
        "SIMULATED_ACTION_ALLOWED_FOR_DRY_RUN",
        "SIMULATED_EVIDENCE_ENVELOPE_CREATED",
        "SIMULATED_REDACTION_PROOF_CREATED",
        "SIMULATED_NO_EXECUTION_PROOF_CREATED"
    ];

    private static readonly string[] ProhibitedDecisions =
    [
        "ALLOW_REAL_EXECUTION",
        "PRODUCTIVE_ENABLED",
        "LIVE_CALL_ENABLED",
        "FILESYSTEM_WRITE_ENABLED",
        "BROWSER_AUTOMATION_ENABLED",
        "CAPABILITY_UNLOCKED",
        "PUBLIC_RELEASE_READY",
        "CHROME_WEB_STORE_READY"
    ];

    private const string OrchestratorContractPath = "artifacts/agent-operations/m770/simulated-dry-run-orchestrator-contract.json";
    private const string RequestContractPath = "artifacts/agent-operations/m770/orchestrator-request-contract.json";
    private const string ResponseContractPath = "artifacts/agent-operations/m770/orchestrator-response-contract.json";
    private const string PolicyApprovalFlowPath = "artifacts/agent-operations/m770/orchestrator-policy-approval-flow.json";
    private const string DisallowedActionsPath = "artifacts/agent-operations/m770/orchestrator-disallowed-actions.json";
    private const string LedgerProjectionPath = "artifacts/agent-operations/m771/ledger-event-projection.json";
    private const string LedgerSchemaPath = "artifacts/agent-operations/m771/projected-ledger-event-schema.json";
    private const string LedgerBindingPath = "artifacts/agent-operations/m771/orchestrator-to-ledger-binding.json";
    private const string LedgerEventTypesPath = "artifacts/agent-operations/m771/ledger-event-types.json";
    private const string LedgerNoExecutionProofPath = "artifacts/agent-operations/m771/ledger-no-execution-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m772/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m772/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m770-m772/simulated-orchestrator-ledger-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    [TestMethod]
    public void SimulatedOrchestratorArtifactsExist()
    {
        foreach (var path in new[]
        {
            OrchestratorContractPath,
            RequestContractPath,
            ResponseContractPath,
            PolicyApprovalFlowPath,
            DisallowedActionsPath,
            LedgerProjectionPath,
            LedgerSchemaPath,
            LedgerBindingPath,
            LedgerEventTypesPath,
            LedgerNoExecutionProofPath,
            ProductBridgeCspPath,
            NextMilestonePath,
            ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void RequestContractRequiresSimulatedDryRunMode()
    {
        using var doc = ReadJson(RequestContractPath);
        var root = doc.RootElement;

        Assert.AreEqual("SIMULATED_DRY_RUN", root.GetProperty("requestedMode").GetString());
        Assert.IsTrue(root.GetProperty("policyGateRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("manualApprovalRequiredWhenHighRisk").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceEnvelopeRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("ledgerProjectionRequired").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveExecutionAllowed").GetBoolean());
    }

    [TestMethod]
    public void ResponseContractIncludesNoExecutionFlags()
    {
        using var doc = ReadJson(ResponseContractPath);
        var root = doc.RootElement;

        Assert.IsFalse(root.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(root.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("storeSubmissionPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("signedPublicZipCreated").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveExecutionAllowed").GetBoolean());
    }

    [TestMethod]
    public void AllowedDecisionsAreOnlySimulationDenyOrManualApproval()
    {
        using var doc = ReadJson(ResponseContractPath);
        var allowed = doc.RootElement.GetProperty("allowedDecisions").EnumerateArray().Select(x => x.GetString()).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "ALLOW_SIMULATED_DRY_RUN", "DENY", "REQUIRE_MANUAL_APPROVAL" },
            allowed);
    }

    [TestMethod]
    public void RealExecutionDecisionsAreProhibited()
    {
        using var response = ReadJson(ResponseContractPath);
        using var disallowed = ReadJson(DisallowedActionsPath);
        var prohibited = response.RootElement.GetProperty("prohibitedDecisions").EnumerateArray().Select(x => x.GetString()).ToArray();
        var disallowedActions = disallowed.RootElement.GetProperty("disallowedActions").EnumerateArray().Select(x => x.GetString()).ToArray();

        foreach (var decision in ProhibitedDecisions)
        {
            CollectionAssert.Contains(prohibited, decision);
            CollectionAssert.Contains(disallowedActions, decision);
        }

        Assert.IsTrue(disallowed.RootElement.GetProperty("denyOnDisallowedAction").GetBoolean());
        Assert.IsFalse(disallowed.RootElement.GetProperty("actualExecutionPerformed").GetBoolean());
    }

    [TestMethod]
    public void LedgerEventProjectionAndSchemaAreSimulationOnly()
    {
        using var projection = ReadJson(LedgerProjectionPath);
        using var schema = ReadJson(LedgerSchemaPath);

        Assert.AreEqual("READY", projection.RootElement.GetProperty("ledgerEventProjection").GetString());
        Assert.IsTrue(schema.RootElement.GetProperty("simulationOnly").GetBoolean());
        Assert.IsFalse(schema.RootElement.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(schema.RootElement.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(schema.RootElement.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(schema.RootElement.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(schema.RootElement.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(schema.RootElement.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(schema.RootElement.GetProperty("storeSubmissionPerformed").GetBoolean());
    }

    [TestMethod]
    public void LedgerEventTypesIncludeAllRequiredSimulatedEvents()
    {
        using var doc = ReadJson(LedgerEventTypesPath);
        var eventTypes = doc.RootElement.GetProperty("eventTypes").EnumerateArray().Select(x => x.GetString()).ToArray();

        foreach (var eventType in RequiredEventTypes)
            CollectionAssert.Contains(eventTypes, eventType);

        Assert.IsFalse(doc.RootElement.GetProperty("realExecutionEventTypesAllowed").GetBoolean());
    }

    [TestMethod]
    public void OrchestratorToLedgerBindingRequiresEvidenceRedactionAndNoExecutionProof()
    {
        using var doc = ReadJson(LedgerBindingPath);
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("evidenceEnvelopeRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("redactionProofRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("noExecutionProofRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("simulationOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("actualExecutionPerformed").GetBoolean());
    }

    [TestMethod]
    public void LedgerNoExecutionProofKeepsAllExecutionFlagsFalse()
    {
        using var doc = ReadJson(LedgerNoExecutionProofPath);
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("simulationOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(root.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("storeSubmissionPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("signedPublicZipCreated").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveExecutionAllowed").GetBoolean());
    }

    [TestMethod]
    public void RuntimeReleaseStoreProductBridgeAndCspRemainBlocked()
    {
        using var consolidated = ReadJson(ConsolidatedPath);
        using var productBridge = ReadJson(ProductBridgeCspPath);

        Assert.AreEqual("DISABLED", consolidated.RootElement.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("DISABLED", consolidated.RootElement.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("DISABLED", consolidated.RootElement.GetProperty("filesystemBrowserCapabilityUnlock").GetString());
        Assert.AreEqual("NO-GO", consolidated.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", consolidated.RootElement.GetProperty("chromeWebStore").GetString());
        Assert.IsFalse(productBridge.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("bridgeCspModified").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneIsNegativeFlowTestsAndLedgerProjectionProof()
    {
        using var doc = ReadJson(NextMilestonePath);

        Assert.AreEqual(
            "M773-M775 Simulated Orchestrator Negative Flow Tests + Ledger Projection Proof",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("tests/contracts/simulation only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsSimulatedOrchestratorLedgerProjectionReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var root = doc.RootElement;

        Assert.AreEqual("SIMULATED_DRY_RUN_ORCHESTRATOR_LEDGER_PROJECTION_READY", root.GetProperty("decision").GetString());
        Assert.AreEqual("PROHIBITED", root.GetProperty("productiveEnabled").GetString());
        Assert.IsFalse(root.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(root.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(root.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("storeSubmissionPerformed").GetBoolean());
        Assert.IsFalse(root.GetProperty("signedPublicZipCreated").GetBoolean());
    }
}
