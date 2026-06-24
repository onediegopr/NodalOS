using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedNegativeFlow")]
[TestCategory("M773")]
[TestCategory("M774")]
[TestCategory("M775")]
public sealed class NodalOsSimulatedNegativeFlowM773M775Tests
{
    private static readonly string[] RequiredCases =
    [
        "orchestrator_attempt_live_provider_call",
        "orchestrator_attempt_provider_credential_use",
        "orchestrator_attempt_filesystem_write",
        "orchestrator_attempt_browser_action",
        "orchestrator_attempt_credential_captcha_2fa_bypass",
        "orchestrator_attempt_capability_unlock",
        "orchestrator_attempt_public_release",
        "orchestrator_attempt_chrome_web_store_submission",
        "orchestrator_attempt_signed_public_zip_creation",
        "orchestrator_attempt_product_file_modification",
        "orchestrator_attempt_bridge_csp_modification",
        "orchestrator_attempt_productive_enabled"
    ];

    private static readonly string[] RequiredLedgerEvents =
    [
        "SIMULATED_DRY_RUN_REQUESTED",
        "SIMULATED_POLICY_GATE_EVALUATED",
        "SIMULATED_ACTION_DENIED",
        "SIMULATED_EVIDENCE_ENVELOPE_CREATED",
        "SIMULATED_NO_EXECUTION_PROOF_CREATED"
    ];

    private const string NegativeFlowTestsPath = "artifacts/agent-operations/m773/simulated-orchestrator-negative-flow-tests.json";
    private const string NegativeCasesPath = "artifacts/agent-operations/m773/negative-flow-cases.json";
    private const string ExpectedDecisionsPath = "artifacts/agent-operations/m773/negative-flow-expected-decisions.json";
    private const string PolicyApprovalResultsPath = "artifacts/agent-operations/m773/negative-flow-policy-approval-results.json";
    private const string NoExecutionByCasePath = "artifacts/agent-operations/m773/negative-flow-no-execution-proof.json";
    private const string LedgerProjectionProofPath = "artifacts/agent-operations/m774/ledger-projection-proof.json";
    private const string NegativeFlowLedgerEventsPath = "artifacts/agent-operations/m774/negative-flow-ledger-events.json";
    private const string LedgerEventSequencePath = "artifacts/agent-operations/m774/ledger-event-sequence-by-case.json";
    private const string LedgerSimulationOnlyProofPath = "artifacts/agent-operations/m774/ledger-simulation-only-proof.json";
    private const string LedgerNoExecutionByEventPath = "artifacts/agent-operations/m774/ledger-no-execution-proof-by-event.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m775/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m775/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m773-m775/simulated-negative-flow-ledger-go-no-go.json";

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
    public void NegativeFlowArtifactsExist()
    {
        foreach (var path in new[]
        {
            NegativeFlowTestsPath,
            NegativeCasesPath,
            ExpectedDecisionsPath,
            PolicyApprovalResultsPath,
            NoExecutionByCasePath,
            LedgerProjectionProofPath,
            NegativeFlowLedgerEventsPath,
            LedgerEventSequencePath,
            LedgerSimulationOnlyProofPath,
            LedgerNoExecutionByEventPath,
            ProductBridgeCspPath,
            NextMilestonePath,
            ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void AllRequiredNegativeCasesExist()
    {
        using var doc = ReadJson(NegativeCasesPath);
        var cases = doc.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();

        foreach (var requiredCase in RequiredCases)
            CollectionAssert.Contains(cases, requiredCase);

        Assert.IsTrue(doc.RootElement.GetProperty("simulationOnly").GetBoolean());
    }

    [TestMethod]
    public void EachNegativeCaseIsDeniedByPolicyGate()
    {
        using var doc = ReadJson(ExpectedDecisionsPath);
        var results = doc.RootElement.GetProperty("caseResults").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
        {
            var result = results.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            Assert.AreEqual("DENY", result.GetProperty("decision").GetString(), requiredCase);
            Assert.AreEqual("DENY", result.GetProperty("policyGateDecision").GetString(), requiredCase);
            Assert.IsTrue(result.GetProperty("ledgerProjectionRequired").GetBoolean(), requiredCase);
            Assert.IsTrue(result.GetProperty("evidenceEnvelopeRequired").GetBoolean(), requiredCase);
        }
    }

    [TestMethod]
    public void EachNegativeCasePreservesNoExecutionFlags()
    {
        using var decisions = ReadJson(ExpectedDecisionsPath);
        using var proof = ReadJson(NoExecutionByCasePath);
        var decisionResults = decisions.RootElement.GetProperty("caseResults").EnumerateArray().ToArray();
        var proofResults = proof.RootElement.GetProperty("caseFlags").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
        {
            AssertNoExecutionFlagsFalse(decisionResults.Single(x => x.GetProperty("caseId").GetString() == requiredCase), requiredCase);
            AssertNoExecutionFlagsFalse(proofResults.Single(x => x.GetProperty("caseId").GetString() == requiredCase), requiredCase);
        }
    }

    [TestMethod]
    public void PolicyApprovalResultsRemainDeniedOrNotApplicable()
    {
        using var doc = ReadJson(PolicyApprovalResultsPath);
        var results = doc.RootElement.GetProperty("caseResults").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
        {
            var result = results.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            Assert.AreEqual("DENY", result.GetProperty("policyGateDecision").GetString(), requiredCase);
            var approval = result.GetProperty("approvalDecision").GetString();
            Assert.IsTrue(approval is "DENIED" or "NOT_APPLICABLE", requiredCase);
        }
    }

    [TestMethod]
    public void LedgerProjectionProofExists()
    {
        using var doc = ReadJson(LedgerProjectionProofPath);
        var root = doc.RootElement;

        Assert.AreEqual("READY", root.GetProperty("ledgerProjectionProof").GetString());
        Assert.IsTrue(root.GetProperty("everyNegativeCaseHasLedgerEvents").GetBoolean());
        Assert.IsTrue(root.GetProperty("simulationOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("actualExecutionPerformed").GetBoolean());
    }

    [TestMethod]
    public void EachNegativeCaseHasRequiredLedgerEventSequence()
    {
        using var doc = ReadJson(NegativeFlowLedgerEventsPath);
        var caseEvents = doc.RootElement.GetProperty("caseEvents").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
        {
            var item = caseEvents.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            var events = item.GetProperty("events").EnumerateArray().Select(x => x.GetString()).ToArray();
            foreach (var requiredEvent in RequiredLedgerEvents)
                CollectionAssert.Contains(events, requiredEvent, requiredCase);
        }
    }

    [TestMethod]
    public void LedgerEventDefaultsAreSimulationOnlyAndNoExecution()
    {
        using var events = ReadJson(NegativeFlowLedgerEventsPath);
        using var proof = ReadJson(LedgerNoExecutionByEventPath);
        var eventDefaults = events.RootElement.GetProperty("eventDefaults");
        var proofDefaults = proof.RootElement.GetProperty("eventProofDefaults");

        AssertLedgerEventFlags(eventDefaults);
        AssertLedgerEventFlags(proofDefaults);
        Assert.IsTrue(proof.RootElement.GetProperty("appliesToEveryProjectedEvent").GetBoolean());
    }

    [TestMethod]
    public void LedgerSimulationOnlyProofIsGlobal()
    {
        using var doc = ReadJson(LedgerSimulationOnlyProofPath);
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("simulationOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("appliesToEveryProjectedEvent").GetBoolean());
        Assert.IsFalse(root.GetProperty("realExecutionEventTypesAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("actualExecutionPerformed").GetBoolean());
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
    public void NextMilestoneIsSimulatedPositiveDryRunFlowContract()
    {
        using var doc = ReadJson(NextMilestonePath);

        Assert.AreEqual(
            "M776-M778 Simulated Positive Dry-Run Flow Contract + Evidence Envelope Projection",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("simulation/contracts/tests only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsSimulatedNegativeFlowLedgerProofReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var root = doc.RootElement;

        Assert.AreEqual("SIMULATED_ORCHESTRATOR_NEGATIVE_FLOW_LEDGER_PROOF_READY", root.GetProperty("decision").GetString());
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

    private static void AssertNoExecutionFlagsFalse(JsonElement item, string context)
    {
        Assert.IsFalse(item.GetProperty("actualExecutionPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("liveCallPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("filesystemWritePerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("browserAutomationPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("capabilityUnlocked").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("publicReleasePerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("storeSubmissionPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("signedPublicZipCreated").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("productFilesModified").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("bridgeCspModified").GetBoolean(), context);
    }

    private static void AssertLedgerEventFlags(JsonElement item)
    {
        Assert.IsTrue(item.GetProperty("simulationOnly").GetBoolean());
        Assert.IsFalse(item.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(item.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("storeSubmissionPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("signedPublicZipCreated").GetBoolean());
    }
}
