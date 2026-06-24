using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedRouting")]
[TestCategory("M797")]
[TestCategory("M798")]
[TestCategory("M799")]
public sealed class NodalOsRoutingDenylistM797M799Tests
{
    private const string RouterContractPath = "artifacts/agent-operations/m797/simulated-router-contract.json";
    private const string AllowedRoutingPath = "artifacts/agent-operations/m797/allowed-executor-routing-table.json";
    private const string RoutingResultPath = "artifacts/agent-operations/m797/routing-result-contract.json";
    private const string RoutingBindingPath = "artifacts/agent-operations/m797/routing-evidence-ledger-binding.json";
    private const string DenylistedPath = "artifacts/agent-operations/m798/denylisted-capabilities.json";
    private const string DenylistResultsPath = "artifacts/agent-operations/m798/denylist-routing-results.json";
    private const string DenylistAuditPath = "artifacts/agent-operations/m798/denylist-audit-events.json";
    private const string DenylistNoExecutionPath = "artifacts/agent-operations/m798/denylist-no-execution-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m799/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m799/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m797-m799/simulated-routing-denylist-go-no-go.json";

    private static readonly string[] DenylistedCapabilities =
    [
        "provider_cloud_live_call",
        "provider_credential_use",
        "filesystem_write",
        "browser_automation",
        "credential_captcha_2fa_bypass",
        "capability_unlock",
        "public_release",
        "chrome_web_store_submission",
        "signed_public_zip_creation",
        "product_file_modification",
        "bridge_csp_modification",
        "productive_enabled"
    ];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static string ReadAll(string relativePath) => File.ReadAllText(FullPath(relativePath));

    [TestMethod]
    public void SimulatedRouterContractAndAllowedRoutingTableExist()
    {
        foreach (var path in new[]
        {
            RouterContractPath,
            AllowedRoutingPath,
            RoutingResultPath,
            RoutingBindingPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }
    }

    [TestMethod]
    public void AllowedRoutingTableMapsOnlyAllowedExecutors()
    {
        var content = ReadAll(AllowedRoutingPath);

        StringAssert.Contains(content, "\"local_provider_model\": \"FakeLocalModelExecutor\"");
        StringAssert.Contains(content, "\"filesystem_read_metadata\": \"FakeFilesystemReadMetadataExecutor\"");
        StringAssert.Contains(content, "\"ledger_append\": \"FakeLedgerAppendExecutor\"");
    }

    [TestMethod]
    public void AllowedRoutesReturnAllowAndCollectorObjects()
    {
        var router = new SimulatedCapabilityRouter();
        var expected = new Dictionary<string, string>
        {
            ["local_provider_model"] = "FakeLocalModelExecutor",
            ["filesystem_read_metadata"] = "FakeFilesystemReadMetadataExecutor",
            ["ledger_append"] = "FakeLedgerAppendExecutor"
        };

        foreach (var item in expected)
        {
            var result = router.Route(item.Key);

            Assert.AreEqual(item.Value, result.SelectedExecutor);
            Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, result.Decision);
            Assert.AreEqual(SimulatedDryRunOrchestrator.RuntimeType, result.RuntimeType);
            Assert.AreEqual(SimulatedDryRunOrchestrator.RequiredFixtureType, result.FixtureType);
            Assert.IsNotNull(result.EvidenceEnvelope);
            Assert.IsNotNull(result.LedgerEvents);
            Assert.IsTrue(result.LedgerEvents.Count >= 2);
            Assert.IsNotNull(result.RedactionProof);
            Assert.IsNotNull(result.NoExecutionProof);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void DenylistedCapabilitiesArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(DenylistedPath)));
        var content = ReadAll(DenylistedPath);

        foreach (var capability in DenylistedCapabilities)
            StringAssert.Contains(content, capability);
    }

    [TestMethod]
    public void EveryDenylistedCapabilityReturnsDenyWithNoExecutor()
    {
        var router = new SimulatedCapabilityRouter();

        foreach (var capability in DenylistedCapabilities)
        {
            var result = router.Route(capability);

            Assert.AreEqual(SimulatedDecision.Deny, result.Decision, capability);
            Assert.IsNull(result.SelectedExecutor, capability);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.DenyReason), capability);
            Assert.IsTrue(result.AuditEventCreated, capability);
            Assert.IsNotNull(result.EvidenceEnvelope, capability);
            Assert.IsNotNull(result.LedgerEvents, capability);
            Assert.IsTrue(result.LedgerEvents.Count >= 2, capability);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void DenylistArtifactsRecordRoutingResultsAuditEventsAndNoExecution()
    {
        foreach (var path in new[]
        {
            DenylistResultsPath,
            DenylistAuditPath,
            DenylistNoExecutionPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        var results = ReadAll(DenylistResultsPath);
        StringAssert.Contains(results, "\"decision\": \"DENY\"");
        StringAssert.Contains(results, "\"selectedExecutor\": null");

        var audit = ReadAll(DenylistAuditPath);
        StringAssert.Contains(audit, "\"auditEventCreated\": true");
        StringAssert.Contains(audit, "\"evidenceEnvelopeCreated\": true");

        var noExecution = ReadAll(DenylistNoExecutionPath);
        foreach (var expected in new[]
        {
            "\"actualExecutionPerformed\": false",
            "\"liveCallPerformed\": false",
            "\"filesystemWritePerformed\": false",
            "\"browserAutomationPerformed\": false",
            "\"capabilityUnlocked\": false",
            "\"publicReleasePerformed\": false",
            "\"storeSubmissionPerformed\": false",
            "\"signedPublicZipCreated\": false",
            "\"productFilesModified\": false",
            "\"bridgeCspModified\": false",
            "\"sideEffectSinkInvocations\": 0"
        })
        {
            StringAssert.Contains(noExecution, expected);
        }
    }

    [TestMethod]
    public void RuntimeReleaseStoreProductBridgeAndCspRemainBlocked()
    {
        var consolidated = ReadAll(ConsolidatedPath);
        var productBridge = ReadAll(ProductBridgeCspPath);

        StringAssert.Contains(consolidated, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(consolidated, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(consolidated, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(consolidated, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(consolidated, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(consolidated, "\"productiveEnabled\": \"PROHIBITED\"");
        StringAssert.Contains(productBridge, "\"productFilesModified\": false");
        StringAssert.Contains(productBridge, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMilestoneRecommendsRoutingMatrixConsolidationAuditGate()
    {
        var content = ReadAll(NextMilestonePath);

        StringAssert.Contains(content, "M800-M802");
        StringAssert.Contains(content, "Simulated Runtime Routing Matrix Consolidation + Audit Gate");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
    }

    [TestMethod]
    public void FinalDecisionIsSimulatedRoutingDenylistReady()
    {
        var content = ReadAll(ConsolidatedPath);

        StringAssert.Contains(content, "SIMULATED_CAPABILITY_ROUTING_DENYLIST_ENFORCEMENT_READY");
        StringAssert.Contains(content, "\"simulatedRouting\": \"READY\"");
        StringAssert.Contains(content, "\"denylistEnforcement\": \"READY\"");
        StringAssert.Contains(content, "\"blockedCapabilitiesDenied\": \"READY\"");
    }

    private static void AssertCleanRoute(SimulatedRoutingResult result)
    {
        Assert.AreEqual(0, result.SideEffectSinkInvocations);
        Assert.IsFalse(result.RealExecutorInvoked);
        Assert.IsFalse(result.ProviderClientInvoked);
        Assert.IsFalse(result.FilesystemWriterInvoked);
        Assert.IsFalse(result.BrowserAutomationInvoked);
        Assert.IsFalse(result.CapabilityUnlockInvoked);
        Assert.IsFalse(result.PublicReleaseInvoked);
        Assert.IsFalse(result.StoreSubmissionInvoked);
        Assert.IsFalse(result.SignedZipCreated);
        Assert.IsFalse(result.ProductFilesModified);
        Assert.IsFalse(result.BridgeCspModified);
        Assert.IsFalse(result.NoExecutionProof.ActualExecutionPerformed);
        Assert.IsFalse(result.NoExecutionProof.LiveCallPerformed);
        Assert.IsFalse(result.NoExecutionProof.FilesystemWritePerformed);
        Assert.IsFalse(result.NoExecutionProof.BrowserAutomationPerformed);
        Assert.IsFalse(result.NoExecutionProof.CapabilityUnlocked);
        Assert.IsFalse(result.NoExecutionProof.PublicReleasePerformed);
        Assert.IsFalse(result.NoExecutionProof.StoreSubmissionPerformed);
        Assert.IsFalse(result.NoExecutionProof.SignedPublicZipCreated);
    }
}
