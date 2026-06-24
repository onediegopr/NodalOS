using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedPositiveFlow")]
[TestCategory("M776")]
[TestCategory("M777")]
[TestCategory("M778")]
public sealed class NodalOsSimulatedPositiveFlowM776M778Tests
{
    private static readonly string[] RequiredCases =
    [
        "simulated_local_provider_model_response",
        "simulated_filesystem_read_metadata",
        "simulated_extension_bridge_event",
        "simulated_websocket_bridge_event",
        "simulated_evidence_ledger_append",
        "simulated_timeline_reporting_projection",
        "simulated_policy_gate_allow",
        "simulated_manual_approval_allow",
        "simulated_redaction_proof_creation"
    ];

    private static readonly string[] RequiredLedgerEvents =
    [
        "SIMULATED_DRY_RUN_REQUESTED",
        "SIMULATED_POLICY_GATE_EVALUATED",
        "SIMULATED_ACTION_ALLOWED_FOR_DRY_RUN",
        "SIMULATED_EVIDENCE_ENVELOPE_CREATED",
        "SIMULATED_REDACTION_PROOF_CREATED",
        "SIMULATED_NO_EXECUTION_PROOF_CREATED"
    ];

    private static readonly string[] ForbiddenOutputFlags =
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

    private const string PositiveContractPath = "artifacts/agent-operations/m776/simulated-positive-dry-run-flow-contract.json";
    private const string PositiveCasesPath = "artifacts/agent-operations/m776/positive-flow-cases.json";
    private const string AllowedCapabilitiesPath = "artifacts/agent-operations/m776/positive-flow-allowed-capabilities.json";
    private const string PolicyApprovalResultsPath = "artifacts/agent-operations/m776/positive-flow-policy-approval-results.json";
    private const string NoExecutionByCasePath = "artifacts/agent-operations/m776/positive-flow-no-execution-proof.json";
    private const string EvidenceProjectionPath = "artifacts/agent-operations/m777/evidence-envelope-projection.json";
    private const string EvidenceEnvelopesPath = "artifacts/agent-operations/m777/positive-flow-evidence-envelopes.json";
    private const string RedactionProofsPath = "artifacts/agent-operations/m777/positive-flow-redaction-proofs.json";
    private const string LedgerBindingsPath = "artifacts/agent-operations/m777/positive-flow-ledger-bindings.json";
    private const string EnvelopeNoExecutionPath = "artifacts/agent-operations/m777/evidence-envelope-no-execution-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m778/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m778/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m776-m778/simulated-positive-flow-evidence-go-no-go.json";

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
    public void PositiveFlowArtifactsExist()
    {
        foreach (var path in new[]
        {
            PositiveContractPath,
            PositiveCasesPath,
            AllowedCapabilitiesPath,
            PolicyApprovalResultsPath,
            NoExecutionByCasePath,
            EvidenceProjectionPath,
            EvidenceEnvelopesPath,
            RedactionProofsPath,
            LedgerBindingsPath,
            EnvelopeNoExecutionPath,
            ProductBridgeCspPath,
            NextMilestonePath,
            ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void AllPositiveCasesExistAndAreSimulationOnly()
    {
        using var doc = ReadJson(PositiveCasesPath);
        var cases = doc.RootElement.GetProperty("cases").EnumerateArray().Select(x => x.GetString()).ToArray();

        foreach (var requiredCase in RequiredCases)
            CollectionAssert.Contains(cases, requiredCase);

        Assert.IsTrue(doc.RootElement.GetProperty("simulationOnly").GetBoolean());
        var excluded = doc.RootElement.GetProperty("excludedFromPositiveCases").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.Contains(excluded, "provider/cloud live calls");
        CollectionAssert.Contains(excluded, "filesystem write");
        CollectionAssert.Contains(excluded, "browser automation");
        CollectionAssert.Contains(excluded, "capability unlock");
    }

    [TestMethod]
    public void PositiveCasesUseOnlyEligibleSimulatedCapabilities()
    {
        using var doc = ReadJson(AllowedCapabilitiesPath);
        var root = doc.RootElement;
        var capabilities = root.GetProperty("allowedCapabilities").EnumerateArray().Select(x => x.GetString()).ToArray();

        CollectionAssert.Contains(capabilities, "local provider/model");
        CollectionAssert.Contains(capabilities, "filesystem read metadata");
        CollectionAssert.Contains(capabilities, "extension bridge event");
        CollectionAssert.Contains(capabilities, "WebSocket bridge event");
        CollectionAssert.Contains(capabilities, "evidence ledger append projection");
        CollectionAssert.Contains(capabilities, "timeline/reporting projection");
        Assert.IsFalse(root.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("liveCallAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemWriteAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("browserAutomationAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("capabilityUnlockAllowed").GetBoolean());
    }

    [TestMethod]
    public void EachPositiveCaseAllowsSimulatedDryRunOnly()
    {
        using var doc = ReadJson(PolicyApprovalResultsPath);
        var results = doc.RootElement.GetProperty("caseResults").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
        {
            var result = results.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            Assert.AreEqual("ALLOW_SIMULATED_DRY_RUN", result.GetProperty("decision").GetString(), requiredCase);
            Assert.AreEqual("SIMULATED_DRY_RUN", result.GetProperty("requestedMode").GetString(), requiredCase);
            Assert.IsTrue(result.GetProperty("simulationOnly").GetBoolean(), requiredCase);
            Assert.AreEqual("ALLOW_SIMULATED_DRY_RUN", result.GetProperty("policyGateDecision").GetString(), requiredCase);
            var approval = result.GetProperty("approvalDecision").GetString();
            Assert.IsTrue(approval is "APPROVED_SIMULATED" or "NOT_REQUIRED", requiredCase);
            Assert.IsTrue(result.GetProperty("evidenceEnvelopeRequired").GetBoolean(), requiredCase);
            Assert.IsTrue(result.GetProperty("ledgerProjectionRequired").GetBoolean(), requiredCase);
            Assert.IsTrue(result.GetProperty("redactionProofRequired").GetBoolean(), requiredCase);
            AssertNoExecutionFlagsFalse(result, requiredCase);
        }
    }

    [TestMethod]
    public void PositiveFlowNoExecutionProofStaysFalse()
    {
        using var doc = ReadJson(NoExecutionByCasePath);
        var results = doc.RootElement.GetProperty("caseFlags").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
            AssertNoExecutionFlagsFalse(results.Single(x => x.GetProperty("caseId").GetString() == requiredCase), requiredCase);
    }

    [TestMethod]
    public void EvidenceEnvelopesExistForEveryPositiveCase()
    {
        using var doc = ReadJson(EvidenceEnvelopesPath);
        var envelopes = doc.RootElement.GetProperty("envelopes").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
        {
            var envelope = envelopes.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            Assert.AreEqual("ALLOW_SIMULATED_DRY_RUN", envelope.GetProperty("decision").GetString(), requiredCase);
            Assert.IsTrue(envelope.GetProperty("simulationOnly").GetBoolean(), requiredCase);
            Assert.IsTrue(envelope.GetProperty("affectedResourcesRedacted").GetBoolean(), requiredCase);
            Assert.IsTrue(envelope.GetProperty("ledgerEventRefs").EnumerateArray().Any(), requiredCase);
            AssertForbiddenFieldsExcluded(envelope, requiredCase);
            AssertEnvelopeNoExecutionFlagsFalse(envelope, requiredCase);
        }
    }

    [TestMethod]
    public void RedactionProofsExcludeAllForbiddenFields()
    {
        using var doc = ReadJson(RedactionProofsPath);
        var outputFlags = doc.RootElement.GetProperty("outputFlags");

        Assert.IsTrue(doc.RootElement.GetProperty("redactionProofsReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("appliesToEveryPositiveCase").GetBoolean());
        foreach (var flag in ForbiddenOutputFlags)
            Assert.IsFalse(outputFlags.GetProperty(flag).GetBoolean(), flag);
    }

    [TestMethod]
    public void LedgerBindingsExistForEachPositiveCase()
    {
        using var doc = ReadJson(LedgerBindingsPath);
        var bindings = doc.RootElement.GetProperty("caseBindings").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredCases)
        {
            var item = bindings.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            var events = item.GetProperty("events").EnumerateArray().Select(x => x.GetString()).ToArray();
            foreach (var requiredEvent in RequiredLedgerEvents)
                CollectionAssert.Contains(events, requiredEvent, requiredCase);
        }

        var manual = bindings.Single(x => x.GetProperty("caseId").GetString() == "simulated_manual_approval_allow");
        var manualEvents = manual.GetProperty("events").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.Contains(manualEvents, "SIMULATED_MANUAL_APPROVAL_EVALUATED");
    }

    [TestMethod]
    public void EvidenceEnvelopeNoExecutionProofIsGlobal()
    {
        using var doc = ReadJson(EnvelopeNoExecutionPath);
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("appliesToEveryEnvelope").GetBoolean());
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
    public void NextMilestoneIsSimulatedDryRunRoundtrip()
    {
        using var doc = ReadJson(NextMilestonePath);

        Assert.AreEqual(
            "M779-M781 Simulated Dry-Run Roundtrip Contract + Audit Summary",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("simulation/contracts/tests only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsSimulatedPositiveFlowEvidenceProjectionReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var root = doc.RootElement;

        Assert.AreEqual("SIMULATED_POSITIVE_DRY_RUN_FLOW_EVIDENCE_PROJECTION_READY", root.GetProperty("decision").GetString());
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

    private static void AssertForbiddenFieldsExcluded(JsonElement item, string context)
    {
        foreach (var flag in ForbiddenOutputFlags)
            Assert.IsFalse(item.GetProperty(flag).GetBoolean(), context);
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

    private static void AssertEnvelopeNoExecutionFlagsFalse(JsonElement item, string context)
    {
        Assert.IsFalse(item.GetProperty("actualExecutionPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("liveCallPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("filesystemWritePerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("browserAutomationPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("capabilityUnlocked").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("publicReleasePerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("storeSubmissionPerformed").GetBoolean(), context);
        Assert.IsFalse(item.GetProperty("signedPublicZipCreated").GetBoolean(), context);
    }
}
