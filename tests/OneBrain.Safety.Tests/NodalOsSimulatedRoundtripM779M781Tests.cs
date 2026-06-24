using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedRoundtrip")]
[TestCategory("M779")]
[TestCategory("M780")]
[TestCategory("M781")]
public sealed class NodalOsSimulatedRoundtripM779M781Tests
{
    private static readonly string[] RequiredSequence =
    [
        "SIMULATED_REQUEST_RECEIVED",
        "SIMULATED_FIXTURE_LOADED",
        "SIMULATED_POLICY_GATE_EVALUATED",
        "SIMULATED_MANUAL_APPROVAL_EVALUATED_IF_REQUIRED",
        "SIMULATED_DECISION_EMITTED",
        "SIMULATED_EVIDENCE_ENVELOPE_CREATED",
        "SIMULATED_REDACTION_PROOF_CREATED",
        "SIMULATED_LEDGER_EVENTS_PROJECTED",
        "SIMULATED_NO_EXECUTION_PROOF_CREATED",
        "SIMULATED_RESPONSE_RETURNED"
    ];

    private static readonly string[] ForbiddenFields =
    [
        "secrets",
        "credentials",
        "tokens",
        "cookies",
        "rawUserData",
        "rawLogs",
        "providerKeys",
        "privateKeys",
        "browserSessionData"
    ];

    private const string RoundtripContractPath = "artifacts/agent-operations/m779/simulated-dry-run-roundtrip-contract.json";
    private const string SequencePath = "artifacts/agent-operations/m779/roundtrip-sequence-contract.json";
    private const string NegativeBindingPath = "artifacts/agent-operations/m779/roundtrip-negative-flow-binding.json";
    private const string PositiveBindingPath = "artifacts/agent-operations/m779/roundtrip-positive-flow-binding.json";
    private const string InvariantsPath = "artifacts/agent-operations/m779/roundtrip-no-execution-invariants.json";
    private const string AuditSummaryPath = "artifacts/agent-operations/m780/roundtrip-ledger-evidence-audit-summary.json";
    private const string LedgerCoveragePath = "artifacts/agent-operations/m780/roundtrip-ledger-event-coverage.json";
    private const string EvidenceCoveragePath = "artifacts/agent-operations/m780/roundtrip-evidence-envelope-coverage.json";
    private const string RedactionCoveragePath = "artifacts/agent-operations/m780/roundtrip-redaction-proof-coverage.json";
    private const string ForbiddenFieldsPath = "artifacts/agent-operations/m780/roundtrip-forbidden-fields-proof.json";
    private const string NoExecutionCoveragePath = "artifacts/agent-operations/m780/roundtrip-no-execution-proof-coverage.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m781/roundtrip-next-decision-audit-recommendation.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m781/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m781/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m779-m781/simulated-roundtrip-audit-go-no-go.json";

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
    public void RoundtripArtifactsExist()
    {
        foreach (var path in new[]
        {
            RoundtripContractPath,
            SequencePath,
            NegativeBindingPath,
            PositiveBindingPath,
            InvariantsPath,
            AuditSummaryPath,
            LedgerCoveragePath,
            EvidenceCoveragePath,
            RedactionCoveragePath,
            ForbiddenFieldsPath,
            NoExecutionCoveragePath,
            NextDecisionPath,
            ProductBridgeCspPath,
            NextMilestonePath,
            ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void RoundtripContractIsSimulationOnly()
    {
        using var doc = ReadJson(RoundtripContractPath);
        var root = doc.RootElement;

        Assert.AreEqual("READY", root.GetProperty("status").GetString());
        Assert.AreEqual("SIMULATED_DRY_RUN_ONLY", root.GetProperty("roundtripType").GetString());
        Assert.AreEqual("SIMULATED_DRY_RUN", root.GetProperty("requestedMode").GetString());
        Assert.IsTrue(root.GetProperty("simulationOnly").GetBoolean());
        AssertNoExecutionFlagsFalse(root);
    }

    [TestMethod]
    public void RoundtripSequenceIncludesRequiredSteps()
    {
        using var doc = ReadJson(SequencePath);
        var root = doc.RootElement;
        var sequence = root.GetProperty("sequence").EnumerateArray().Select(x => x.GetString()).ToArray();

        foreach (var requiredStep in RequiredSequence)
            CollectionAssert.Contains(sequence, requiredStep);

        Assert.AreEqual("SIMULATED_DRY_RUN", root.GetProperty("requestedMode").GetString());
        Assert.IsTrue(root.GetProperty("simulationOnly").GetBoolean());
        AssertNoExecutionFlagsFalse(root);
    }

    [TestMethod]
    public void NegativeAndPositiveFlowBindingsAreReady()
    {
        using var negative = ReadJson(NegativeBindingPath);
        using var positive = ReadJson(PositiveBindingPath);

        Assert.AreEqual("READY", negative.RootElement.GetProperty("bindingStatus").GetString());
        Assert.AreEqual("DENY", negative.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("DENY", negative.RootElement.GetProperty("policyGateDecision").GetString());
        Assert.IsTrue(negative.RootElement.GetProperty("ledgerProjectionRequired").GetBoolean());
        Assert.IsTrue(negative.RootElement.GetProperty("evidenceEnvelopeRequired").GetBoolean());
        AssertNoExecutionFlagsFalse(negative.RootElement);

        Assert.AreEqual("READY", positive.RootElement.GetProperty("bindingStatus").GetString());
        Assert.AreEqual("ALLOW_SIMULATED_DRY_RUN", positive.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("ALLOW_SIMULATED_DRY_RUN", positive.RootElement.GetProperty("policyGateDecision").GetString());
        Assert.IsTrue(positive.RootElement.GetProperty("ledgerProjectionRequired").GetBoolean());
        Assert.IsTrue(positive.RootElement.GetProperty("evidenceEnvelopeRequired").GetBoolean());
        Assert.IsTrue(positive.RootElement.GetProperty("redactionProofRequired").GetBoolean());
        AssertNoExecutionFlagsFalse(positive.RootElement);
    }

    [TestMethod]
    public void RoundtripNoExecutionInvariantsExist()
    {
        using var doc = ReadJson(InvariantsPath);
        var root = doc.RootElement;

        Assert.AreEqual("READY", root.GetProperty("status").GetString());
        Assert.AreEqual("SIMULATED_DRY_RUN", root.GetProperty("requestedMode").GetString());
        Assert.IsTrue(root.GetProperty("simulationOnly").GetBoolean());
        AssertNoExecutionFlagsFalse(root);
    }

    [TestMethod]
    public void LedgerEvidenceAuditSummaryIsReady()
    {
        using var doc = ReadJson(AuditSummaryPath);
        var root = doc.RootElement;

        Assert.AreEqual("READY", root.GetProperty("status").GetString());
        Assert.AreEqual("READY", root.GetProperty("negativeFlows").GetString());
        Assert.AreEqual("READY", root.GetProperty("positiveSimulatedFlows").GetString());
        Assert.AreEqual("READY", root.GetProperty("ledgerEventProjection").GetString());
        Assert.AreEqual("READY", root.GetProperty("evidenceEnvelopes").GetString());
        Assert.AreEqual("READY", root.GetProperty("redactionProofs").GetString());
        Assert.AreEqual("READY", root.GetProperty("forbiddenFields").GetString());
        Assert.AreEqual("READY", root.GetProperty("noExecutionProofFlags").GetString());
        Assert.AreEqual("READY", root.GetProperty("policyGateDecisions").GetString());
        Assert.AreEqual("READY", root.GetProperty("manualApprovalDecisions").GetString());
        AssertCoverageFlags(root);
    }

    [TestMethod]
    public void CoverageFilesAreReady()
    {
        foreach (var path in new[] { LedgerCoveragePath, EvidenceCoveragePath, RedactionCoveragePath, NoExecutionCoveragePath })
        {
            using var doc = ReadJson(path);
            var root = doc.RootElement;
            Assert.AreEqual("READY", root.GetProperty("coverageStatus").GetString(), path);
            AssertCoverageFlags(root);
        }
    }

    [TestMethod]
    public void ForbiddenFieldsProofCoversAllNineFields()
    {
        using var doc = ReadJson(ForbiddenFieldsPath);
        var root = doc.RootElement;
        var fields = root.GetProperty("forbiddenFields").EnumerateArray().Select(x => x.GetString()).ToArray();

        Assert.AreEqual("READY", root.GetProperty("coverageStatus").GetString());
        foreach (var field in ForbiddenFields)
            CollectionAssert.Contains(fields, field);

        Assert.IsFalse(root.GetProperty("secretsIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("credentialsIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("tokensIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("cookiesIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawUserDataIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("rawLogsIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerKeysIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("privateKeysIncluded").GetBoolean());
        Assert.IsFalse(root.GetProperty("browserSessionDataIncluded").GetBoolean());
        AssertCoverageFlags(root);
    }

    [TestMethod]
    public void RuntimeReleaseStoreProductBridgeAndCspRemainBlocked()
    {
        using var consolidated = ReadJson(ConsolidatedPath);
        using var productBridge = ReadJson(ProductBridgeCspPath);
        var root = consolidated.RootElement;

        Assert.AreEqual("DISABLED", root.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("DISABLED", root.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("DISABLED", root.GetProperty("filesystemBrowserCapabilityUnlock").GetString());
        Assert.AreEqual("NO-GO", root.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", root.GetProperty("chromeWebStore").GetString());
        Assert.IsFalse(productBridge.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("bridgeCspModified").GetBoolean());
    }

    [TestMethod]
    public void NextRecommendationPrefersClaudeAudit()
    {
        using var doc = ReadJson(NextMilestonePath);
        var root = doc.RootElement;

        Assert.AreEqual(
            "AUDIT POST M781 before simulated implementation re-entry",
            root.GetProperty("preferredNextMilestone").GetString());
        Assert.AreEqual(
            "M782-M784 Simulated Dry-Run Runtime Boundary Implementation Plan",
            root.GetProperty("fallbackNextMilestone").GetString());
        Assert.IsFalse(root.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsSimulatedRoundtripAuditSummaryReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var root = doc.RootElement;

        Assert.AreEqual("SIMULATED_DRY_RUN_ROUNDTRIP_AUDIT_SUMMARY_READY", root.GetProperty("decision").GetString());
        Assert.AreEqual("READY", root.GetProperty("simulatedDryRunRoundtripContract").GetString());
        Assert.AreEqual("READY", root.GetProperty("roundtripLedgerEvidenceAuditSummary").GetString());
        Assert.AreEqual("PROHIBITED", root.GetProperty("productiveEnabled").GetString());
        Assert.IsFalse(root.GetProperty("evidenceInvented").GetBoolean());
        AssertNoExecutionFlagsFalse(root);
    }

    private static void AssertNoExecutionFlagsFalse(JsonElement item)
    {
        Assert.IsFalse(item.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(item.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(item.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("storeSubmissionPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("signedPublicZipCreated").GetBoolean());
        Assert.IsFalse(item.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(item.GetProperty("bridgeCspModified").GetBoolean());
    }

    private static void AssertCoverageFlags(JsonElement item)
    {
        Assert.IsFalse(item.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(item.GetProperty("realExecutionPerformed").GetBoolean());
        Assert.IsFalse(item.GetProperty("productiveUnlockAllowed").GetBoolean());
    }
}
