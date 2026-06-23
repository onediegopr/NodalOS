using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulationHarness")]
[TestCategory("M764")]
[TestCategory("M765")]
[TestCategory("M766")]
public sealed class NodalOsSimulationHarnessM764M766Tests
{
    private const string HarnessPlanPath = "artifacts/agent-operations/m764/dry-run-simulation-harness-plan.json";
    private const string ScopePath = "artifacts/agent-operations/m764/simulation-harness-scope.json";
    private const string SimulatedCapabilitiesPath = "artifacts/agent-operations/m764/simulated-capabilities.json";
    private const string DisallowedActionsPath = "artifacts/agent-operations/m764/simulation-disallowed-actions.json";
    private const string FixturesPlanPath = "artifacts/agent-operations/m764/simulation-fixtures-plan.json";
    private const string LedgerReadinessPath = "artifacts/agent-operations/m765/evidence-ledger-readiness.json";
    private const string LedgerSchemaPath = "artifacts/agent-operations/m765/ledger-event-schema-readiness.json";
    private const string EnvelopeBindingPath = "artifacts/agent-operations/m765/simulation-evidence-envelope-binding.json";
    private const string NoExecutionFlagsPath = "artifacts/agent-operations/m765/no-execution-proof-flags.json";
    private const string RedactionBindingPath = "artifacts/agent-operations/m765/redaction-proof-binding.json";
    private const string LedgerBlockersPath = "artifacts/agent-operations/m765/ledger-readiness-blockers.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m766/simulation-harness-next-decision.json";
    private const string NoRuntimeUnlockPath = "artifacts/agent-operations/m766/no-productive-runtime-unlock-proof.json";
    private const string NoProviderCloudPath = "artifacts/agent-operations/m766/no-provider-cloud-live-call-proof.json";
    private const string NoFilesystemBrowserPath = "artifacts/agent-operations/m766/no-filesystem-browser-capability-unlock-proof.json";
    private const string NoReleaseStorePath = "artifacts/agent-operations/m766/no-public-release-store-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m766/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m766/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m764-m766/simulation-harness-ledger-readiness-go-no-go.json";

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
    public void SimulationHarnessArtifactsExist()
    {
        foreach (var path in new[]
        {
            HarnessPlanPath, ScopePath, SimulatedCapabilitiesPath, DisallowedActionsPath, FixturesPlanPath,
            LedgerReadinessPath, LedgerSchemaPath, EnvelopeBindingPath, NoExecutionFlagsPath,
            RedactionBindingPath, LedgerBlockersPath, NextDecisionPath, NoRuntimeUnlockPath,
            NoProviderCloudPath, NoFilesystemBrowserPath, NoReleaseStorePath, ProductBridgeCspPath,
            NextMilestonePath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void SimulationHarnessScopeAndCapabilitiesExist()
    {
        using var scope = ReadJson(ScopePath);
        using var capabilities = ReadJson(SimulatedCapabilitiesPath);
        Assert.AreEqual("READY", scope.RootElement.GetProperty("scopeStatus").GetString());
        Assert.IsTrue(scope.RootElement.GetProperty("simulationScope").EnumerateArray().Any(x => x.GetString() == "fake provider response"));
        Assert.IsTrue(scope.RootElement.GetProperty("simulationScope").EnumerateArray().Any(x => x.GetString() == "fake evidence ledger append event"));
        Assert.IsTrue(capabilities.RootElement.GetProperty("capabilities").EnumerateArray().Any());
    }

    [TestMethod]
    public void SimulationDisallowedActionsIncludeRequiredBoundaries()
    {
        using var doc = ReadJson(DisallowedActionsPath);
        var actions = doc.RootElement.GetProperty("disallowedActions").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.Contains(actions, "live provider/cloud call");
        CollectionAssert.Contains(actions, "filesystem write");
        CollectionAssert.Contains(actions, "real browser automation");
        CollectionAssert.Contains(actions, "public release");
        CollectionAssert.Contains(actions, "Chrome Web Store submission");
        CollectionAssert.Contains(actions, "signed public ZIP");
        CollectionAssert.Contains(actions, "product file modification");
        CollectionAssert.Contains(actions, "Bridge/CSP modification");
    }

    [TestMethod]
    public void EachSimulatedCapabilityForbidsRealExecution()
    {
        using var doc = ReadJson(SimulatedCapabilitiesPath);
        foreach (var item in doc.RootElement.GetProperty("capabilities").EnumerateArray())
        {
            var capabilityName = item.GetProperty("capabilityName").GetString() ?? string.Empty;
            Assert.IsTrue(item.GetProperty("simulationAllowed").GetBoolean(), capabilityName);
            Assert.IsFalse(item.GetProperty("realExecutionAllowed").GetBoolean(), capabilityName);
            Assert.IsFalse(item.GetProperty("liveCallAllowed").GetBoolean(), capabilityName);
            Assert.IsFalse(item.GetProperty("filesystemWriteAllowed").GetBoolean(), capabilityName);
            Assert.IsFalse(item.GetProperty("browserActionAllowed").GetBoolean(), capabilityName);
            Assert.IsTrue(item.GetProperty("evidenceRequired").GetBoolean(), capabilityName);
            Assert.IsTrue(item.GetProperty("redactionRequired").GetBoolean(), capabilityName);
            Assert.IsTrue(item.GetProperty("auditEventRequired").GetBoolean(), capabilityName);
        }
    }

    [TestMethod]
    public void LedgerEventSchemaIncludesNoExecutionProofFlags()
    {
        using var schema = ReadJson(LedgerSchemaPath);
        var fields = schema.RootElement.GetProperty("requiredFields").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.Contains(fields, "actualExecutionPerformed");
        CollectionAssert.Contains(fields, "liveCallPerformed");
        CollectionAssert.Contains(fields, "filesystemWritePerformed");
        CollectionAssert.Contains(fields, "browserAutomationPerformed");
        CollectionAssert.Contains(fields, "capabilityUnlocked");
        CollectionAssert.Contains(fields, "publicReleasePerformed");
        CollectionAssert.Contains(fields, "storeSubmissionPerformed");
    }

    [TestMethod]
    public void NoExecutionProofFlagsRemainFalse()
    {
        using var doc = ReadJson(NoExecutionFlagsPath);
        Assert.IsFalse(doc.RootElement.GetProperty("secretsIncluded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("credentialsIncluded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("tokensIncluded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionPerformed").GetBoolean());
    }

    [TestMethod]
    public void RuntimeProviderFilesystemBrowserCapabilityReleaseAndStoreRemainBlocked()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("filesystemUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("browserAutomationUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("capabilityUnlock").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("chromeWebStore").GetString());
    }

    [TestMethod]
    public void ProductFilesBridgeAndCspAreUnchanged()
    {
        using var doc = ReadJson(ProductBridgeCspPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestsModified").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneIsSimulationHarnessFixtures()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual("M767-M769 Simulation Harness Test Fixtures + No-Execution Proof Suite", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("fixtures/tests/contracts only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsSimulationHarnessLedgerReadinessReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DRY_RUN_SIMULATION_HARNESS_PLAN_LEDGER_READINESS_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.AreEqual("PROHIBITED", doc.RootElement.GetProperty("productiveEnabled").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionPerformed").GetBoolean());
    }
}
