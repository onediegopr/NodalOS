using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RuntimeCapability")]
[TestCategory("M752")]
[TestCategory("M753")]
[TestCategory("M754")]
public sealed class NodalOsRuntimeCapabilityM752M754Tests
{
    private const string InventoryPath = "artifacts/agent-operations/m752/runtime-capability-inventory.json";
    private const string MatrixPath = "artifacts/agent-operations/m752/capability-status-matrix.json";
    private const string DisabledPath = "artifacts/agent-operations/m752/runtime-disabled-capabilities.json";
    private const string PlanningOnlyPath = "artifacts/agent-operations/m752/runtime-planning-only-capabilities.json";
    private const string RiskPath = "artifacts/agent-operations/m752/capability-risk-classification.json";
    private const string PolicyPlanPath = "artifacts/agent-operations/m753/policy-evidence-re-entry-plan.json";
    private const string PolicyRequirementsPath = "artifacts/agent-operations/m753/policy-gate-requirements-by-capability.json";
    private const string EvidenceRequirementsPath = "artifacts/agent-operations/m753/evidence-requirements-by-capability.json";
    private const string ApprovalRequirementsPath = "artifacts/agent-operations/m753/manual-approval-requirements-by-capability.json";
    private const string RedactionBoundaryPath = "artifacts/agent-operations/m753/redaction-secrets-boundary-by-capability.json";
    private const string BlockersPath = "artifacts/agent-operations/m753/reentry-blockers-register.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m754/runtime-re-entry-next-decision.json";
    private const string NoRuntimeUnlockPath = "artifacts/agent-operations/m754/no-runtime-unlock-proof.json";
    private const string NoProviderCloudPath = "artifacts/agent-operations/m754/no-provider-cloud-live-call-proof.json";
    private const string NoFilesystemBrowserPath = "artifacts/agent-operations/m754/no-filesystem-browser-capability-unlock-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m754/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m754/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m752-m754/runtime-capability-policy-evidence-go-no-go.json";

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
    public void RuntimeCapabilityArtifactsExist()
    {
        foreach (var path in new[]
        {
            InventoryPath, MatrixPath, DisabledPath, PlanningOnlyPath, RiskPath,
            PolicyPlanPath, PolicyRequirementsPath, EvidenceRequirementsPath,
            ApprovalRequirementsPath, RedactionBoundaryPath, BlockersPath,
            NextDecisionPath, NoRuntimeUnlockPath, NoProviderCloudPath,
            NoFilesystemBrowserPath, ProductBridgeCspPath, NextMilestonePath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ProductiveRuntimeProviderFilesystemBrowserAndCapabilityRemainDisabled()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("filesystemUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("browserAutomationUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("capabilityUnlock").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseAndChromeWebStoreRemainNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("chromeWebStore").GetString());
    }

    [TestMethod]
    public void HighRiskCapabilitiesHavePolicyEvidenceAndManualApproval()
    {
        using var doc = ReadJson(PolicyRequirementsPath);
        foreach (var item in doc.RootElement.GetProperty("requirements").EnumerateArray())
        {
            var capabilityName = item.GetProperty("capabilityName").GetString() ?? string.Empty;
            Assert.AreEqual("HIGH", item.GetProperty("riskLevel").GetString());
            Assert.IsTrue(item.GetProperty("policyGateRequired").GetBoolean(), capabilityName);
            Assert.IsTrue(item.GetProperty("evidenceRequired").GetBoolean(), capabilityName);
            Assert.IsTrue(item.GetProperty("manualApprovalRequired").GetBoolean(), capabilityName);
            Assert.IsFalse(item.GetProperty("productiveUnlockAllowed").GetBoolean(), capabilityName);
        }
    }

    [TestMethod]
    public void ProviderCloudFilesystemWriteAndBrowserAutomationHaveRequiredNextGates()
    {
        using var doc = ReadJson(PolicyRequirementsPath);
        var requirements = doc.RootElement.GetProperty("requirements").EnumerateArray().ToArray();
        var provider = requirements.Single(r => r.GetProperty("capabilityName").GetString() == "provider/cloud live calls");
        var filesystem = requirements.Single(r => r.GetProperty("capabilityName").GetString() == "filesystem write");
        var browser = requirements.Single(r => r.GetProperty("capabilityName").GetString() == "browser automation");
        Assert.AreEqual("required", provider.GetProperty("secretsBoundary").GetString());
        Assert.AreEqual("filesystem jail + dry-run write contract + rollback proof", filesystem.GetProperty("nextGateRequired").GetString());
        Assert.AreEqual("browser automation dry-run contract + human handoff policy", browser.GetProperty("nextGateRequired").GetString());
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
    public void NextMilestoneIsDryRunContractMatrix()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual("M755-M757 Runtime Dry-Run Contract Matrix + Capability Approval Gates", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("dry-run/contracts/tests only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsRuntimeCapabilityPolicyEvidenceReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("RUNTIME_CAPABILITY_INVENTORY_POLICY_EVIDENCE_REENTRY_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
    }
}
