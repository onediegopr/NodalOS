using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RuntimeDryRun")]
[TestCategory("M755")]
[TestCategory("M756")]
[TestCategory("M757")]
public sealed class NodalOsRuntimeDryRunM755M757Tests
{
    private const string MatrixPath = "artifacts/agent-operations/m755/runtime-dry-run-contract-matrix.json";
    private const string ContractsPath = "artifacts/agent-operations/m755/dry-run-contracts-by-capability.json";
    private const string PreconditionsPath = "artifacts/agent-operations/m755/dry-run-preconditions.json";
    private const string EvidenceContractsPath = "artifacts/agent-operations/m755/dry-run-evidence-contracts.json";
    private const string DisallowedActionsPath = "artifacts/agent-operations/m755/dry-run-disallowed-actions.json";
    private const string ApprovalGatesPath = "artifacts/agent-operations/m756/capability-approval-gates.json";
    private const string ApprovalByCapabilityPath = "artifacts/agent-operations/m756/approval-gates-by-capability.json";
    private const string ManualApprovalUiPath = "artifacts/agent-operations/m756/manual-approval-ui-contract.json";
    private const string PolicyGatePath = "artifacts/agent-operations/m756/policy-gate-decision-contract.json";
    private const string RollbackBoundaryPath = "artifacts/agent-operations/m756/approval-denial-and-rollback-boundary.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m757/dry-run-next-decision.json";
    private const string NoRuntimeUnlockPath = "artifacts/agent-operations/m757/no-productive-runtime-unlock-proof.json";
    private const string NoProviderCloudPath = "artifacts/agent-operations/m757/no-provider-cloud-live-call-proof.json";
    private const string NoFilesystemBrowserPath = "artifacts/agent-operations/m757/no-filesystem-browser-capability-unlock-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m757/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m757/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m755-m757/runtime-dry-run-approval-gates-go-no-go.json";

    private static readonly string[] HighRiskCapabilities =
    [
        "provider/cloud calls",
        "filesystem write",
        "browser automation",
        "capability unlock"
    ];

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
    public void RuntimeDryRunArtifactsExist()
    {
        foreach (var path in new[]
        {
            MatrixPath, ContractsPath, PreconditionsPath, EvidenceContractsPath, DisallowedActionsPath,
            ApprovalGatesPath, ApprovalByCapabilityPath, ManualApprovalUiPath, PolicyGatePath, RollbackBoundaryPath,
            NextDecisionPath, NoRuntimeUnlockPath, NoProviderCloudPath, NoFilesystemBrowserPath,
            ProductBridgeCspPath, NextMilestonePath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void DryRunContractsExistForAllHighRiskCapabilitiesAndNeverAllowProductiveExecution()
    {
        using var doc = ReadJson(ContractsPath);
        var contracts = doc.RootElement.GetProperty("contracts").EnumerateArray().ToArray();
        foreach (var capability in HighRiskCapabilities)
        {
            var contract = contracts.Single(c => c.GetProperty("capabilityName").GetString() == capability);
            Assert.IsTrue(contract.GetProperty("dryRunEligibleFuture").GetBoolean(), capability);
            Assert.IsFalse(contract.GetProperty("productiveExecutionAllowed").GetBoolean(), capability);
            Assert.IsTrue(contract.GetProperty("evidenceRequired").GetBoolean(), capability);
            Assert.IsTrue(contract.GetProperty("manualApprovalRequired").GetBoolean(), capability);
        }

        foreach (var contract in contracts)
            Assert.IsFalse(contract.GetProperty("productiveExecutionAllowed").GetBoolean(), contract.GetProperty("capabilityName").GetString() ?? string.Empty);
    }

    [TestMethod]
    public void RuntimeProviderFilesystemBrowserAndCapabilityRemainDisabled()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("filesystemUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("browserAutomationUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("capabilityUnlock").GetString());
    }

    [TestMethod]
    public void PublicReleaseAndChromeWebStoreRemainNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("chromeWebStore").GetString());
    }

    [TestMethod]
    public void HighRiskDryRunsHaveRequiredSafetyBoundaries()
    {
        using var doc = ReadJson(ContractsPath);
        var contracts = doc.RootElement.GetProperty("contracts").EnumerateArray().ToArray();
        var provider = contracts.Single(c => c.GetProperty("capabilityName").GetString() == "provider/cloud calls");
        var filesystem = contracts.Single(c => c.GetProperty("capabilityName").GetString() == "filesystem write");
        var browser = contracts.Single(c => c.GetProperty("capabilityName").GetString() == "browser automation");

        Assert.IsTrue(provider.GetProperty("secretsBoundaryRequired").GetBoolean());
        Assert.IsTrue(filesystem.GetProperty("jailRequired").GetBoolean());
        Assert.IsTrue(filesystem.GetProperty("rollbackOrCompensationRequired").GetBoolean());
        Assert.IsTrue(browser.GetProperty("humanHandoffRequired").GetBoolean());
        Assert.IsFalse(browser.GetProperty("credentialCaptcha2faBypassAllowed").GetBoolean());
    }

    [TestMethod]
    public void EachHighRiskCapabilityHasApprovalEvidenceAndAuditGate()
    {
        using var doc = ReadJson(ApprovalByCapabilityPath);
        var gates = doc.RootElement.GetProperty("gates").EnumerateArray().ToArray();
        foreach (var capability in HighRiskCapabilities)
        {
            var gate = gates.Single(g => g.GetProperty("capabilityName").GetString() == capability);
            Assert.IsTrue(gate.GetProperty("approvalRequired").GetBoolean(), capability);
            Assert.IsTrue(gate.GetProperty("expectedEvidenceRequired").GetBoolean(), capability);
            Assert.IsTrue(gate.GetProperty("auditEventRequired").GetBoolean(), capability);
            Assert.IsFalse(gate.GetProperty("productiveUnlockAllowed").GetBoolean(), capability);
        }
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
    public void NextMilestoneIsDryRunEvidenceEnvelope()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual("M758-M760 Dry-Run Evidence Envelope + Runtime Promotion Criteria", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("dry-run/contracts/tests only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsRuntimeDryRunApprovalGatesReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("RUNTIME_DRY_RUN_CONTRACT_MATRIX_APPROVAL_GATES_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("prohibitedReleaseStateReturned").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("prohibitedStoreStateReturned").GetBoolean());
    }
}
