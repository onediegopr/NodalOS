using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("DryRunEligibility")]
[TestCategory("M761")]
[TestCategory("M762")]
[TestCategory("M763")]
public sealed class NodalOsDryRunEligibilityM761M763Tests
{
    private const string ClassificationPath = "artifacts/agent-operations/m761/dry-run-eligibility-classification.json";
    private const string ClassificationByCapabilityPath = "artifacts/agent-operations/m761/eligibility-classification-by-capability.json";
    private const string FutureCandidatesPath = "artifacts/agent-operations/m761/dry-run-eligible-future-candidates.json";
    private const string BlockedPath = "artifacts/agent-operations/m761/dry-run-blocked-capabilities.json";
    private const string DisabledPath = "artifacts/agent-operations/m761/dry-run-disabled-capabilities.json";
    private const string GapsPath = "artifacts/agent-operations/m761/eligibility-gaps-by-capability.json";
    private const string FutureRuntimeGatePath = "artifacts/agent-operations/m762/future-runtime-gate.json";
    private const string FutureRuntimeRequirementsPath = "artifacts/agent-operations/m762/future-runtime-gate-requirements.json";
    private const string OwnerApprovalPath = "artifacts/agent-operations/m762/future-owner-approval-requirements.json";
    private const string SafetyPreconditionsPath = "artifacts/agent-operations/m762/future-runtime-safety-preconditions.json";
    private const string HardBlockersPath = "artifacts/agent-operations/m762/future-runtime-hard-blockers.json";
    private const string ProductiveProhibitedPath = "artifacts/agent-operations/m762/productive-enable-still-prohibited.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m763/eligibility-next-decision.json";
    private const string NoRuntimeUnlockPath = "artifacts/agent-operations/m763/no-productive-runtime-unlock-proof.json";
    private const string NoProviderCloudPath = "artifacts/agent-operations/m763/no-provider-cloud-live-call-proof.json";
    private const string NoFilesystemBrowserPath = "artifacts/agent-operations/m763/no-filesystem-browser-capability-unlock-proof.json";
    private const string NoReleaseStorePath = "artifacts/agent-operations/m763/no-public-release-store-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m763/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m763/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m761-m763/dry-run-eligibility-future-runtime-gate-go-no-go.json";

    private static readonly string[] ExpectedCapabilities =
    [
        "provider/cloud calls",
        "local provider/model",
        "filesystem read",
        "filesystem write",
        "browser automation",
        "extension bridge",
        "WebSocket bridge",
        "evidence ledger",
        "timeline/reporting",
        "policy gate",
        "manual approval gate",
        "redaction layer",
        "capability unlock",
        "public release",
        "Chrome Web Store"
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
    public void DryRunEligibilityArtifactsExist()
    {
        foreach (var path in new[]
        {
            ClassificationPath, ClassificationByCapabilityPath, FutureCandidatesPath, BlockedPath,
            DisabledPath, GapsPath, FutureRuntimeGatePath, FutureRuntimeRequirementsPath,
            OwnerApprovalPath, SafetyPreconditionsPath, HardBlockersPath, ProductiveProhibitedPath,
            NextDecisionPath, NoRuntimeUnlockPath, NoProviderCloudPath, NoFilesystemBrowserPath,
            NoReleaseStorePath, ProductBridgeCspPath, NextMilestonePath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void EligibilityClassificationExistsForAllListedCapabilities()
    {
        using var doc = ReadJson(ClassificationByCapabilityPath);
        var names = doc.RootElement.GetProperty("classifications")
            .EnumerateArray()
            .Select(item => item.GetProperty("capabilityName").GetString())
            .ToArray();

        foreach (var capability in ExpectedCapabilities)
            CollectionAssert.Contains(names, capability);
    }

    [TestMethod]
    public void NoCapabilityIsProductiveEnabled()
    {
        using var doc = ReadJson(ClassificationByCapabilityPath);
        foreach (var item in doc.RootElement.GetProperty("classifications").EnumerateArray())
        {
            Assert.AreNotEqual("PRODUCTIVE_ENABLED", item.GetProperty("currentStatus").GetString());
            Assert.AreNotEqual("PRODUCTIVE_ENABLED", item.GetProperty("eligibility").GetString());
            Assert.IsFalse(item.GetProperty("productiveExecutionAllowed").GetBoolean(), item.GetProperty("capabilityName").GetString() ?? string.Empty);
        }
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
    public void FutureRuntimeGateRequiresApprovalEvidenceAndRedactionProofs()
    {
        using var requirements = ReadJson(FutureRuntimeRequirementsPath);
        using var owner = ReadJson(OwnerApprovalPath);
        var values = requirements.RootElement.GetProperty("requirements").EnumerateArray().Select(x => x.GetString()).ToArray();

        CollectionAssert.Contains(values, "owner explicit approval");
        CollectionAssert.Contains(values, "dry-run evidence envelope");
        CollectionAssert.Contains(values, "redaction proof");
        CollectionAssert.Contains(values, "no secrets proof");
        Assert.IsTrue(owner.RootElement.GetProperty("ownerExplicitApprovalRequired").GetBoolean());
        Assert.IsTrue(owner.RootElement.GetProperty("separateFutureGateRequiredForProductiveEnable").GetBoolean());
    }

    [TestMethod]
    public void FutureRuntimeGateBlocksLiveCallsWritesBrowserActionsAndBypass()
    {
        using var doc = ReadJson(HardBlockersPath);
        var blockers = doc.RootElement.GetProperty("hardBlockers").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.Contains(blockers, "live call requested");
        CollectionAssert.Contains(blockers, "filesystem write requested");
        CollectionAssert.Contains(blockers, "browser action requested");
        CollectionAssert.Contains(blockers, "credential/CAPTCHA/2FA bypass path exists");
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
    public void NextMilestoneIsDryRunSimulationHarnessPlan()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual("M764-M766 Dry-Run Simulation Harness Plan + Evidence Ledger Readiness", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("simulation/harness/contracts/tests only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsDryRunEligibilityFutureRuntimeGateReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DRY_RUN_ELIGIBILITY_CLASSIFICATION_FUTURE_RUNTIME_GATE_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.AreEqual("PROHIBITED", doc.RootElement.GetProperty("productiveEnabled").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("prohibitedProductiveStateReturned").GetBoolean());
    }
}
