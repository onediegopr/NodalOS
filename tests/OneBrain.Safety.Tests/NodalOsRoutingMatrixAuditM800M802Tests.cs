using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RoutingMatrixAudit")]
[TestCategory("M800")]
[TestCategory("M801")]
[TestCategory("M802")]
public sealed class NodalOsRoutingMatrixAuditM800M802Tests
{
    private const string CanonicalMatrixPath = "artifacts/agent-operations/m800/simulated-runtime-routing-matrix.json";
    private const string MatrixResultPath = "artifacts/agent-operations/m800/routing-matrix-results-by-capability.json";
    private const string UnknownBehaviorPath = "artifacts/agent-operations/m800/unknown-capability-deny-proof.json";
    private const string CaveatGatePath = "artifacts/agent-operations/m801/full-suite-caveat-audit-gate.json";
    private const string ValidationPlanPath = "artifacts/agent-operations/m801/audit-gate-validation-plan.json";
    private const string FinalPath = "artifacts/agent-operations/m800-m802/routing-matrix-audit-go-no-go.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m802/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m802/next-milestone-recommendation.json";

    private static readonly Dictionary<string, string> ExpectedAllowed = new(StringComparer.Ordinal)
    {
        ["local_provider_model"] = "FakeLocalModelExecutor",
        ["filesystem_read_metadata"] = "FakeFilesystemReadMetadataExecutor",
        ["ledger_append"] = "FakeLedgerAppendExecutor"
    };

    private static readonly string[] ExpectedDenied =
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
    public void MatrixContainsAllAllowedCapabilities()
    {
        CollectionAssert.AreEquivalent(
            ExpectedAllowed.Keys.ToArray(),
            SimulatedRuntimeRoutingMatrix.AllowedRoutingTable.Keys.ToArray());
    }

    [TestMethod]
    public void MatrixContainsAllDenylistedCapabilities()
    {
        CollectionAssert.AreEquivalent(
            ExpectedDenied,
            SimulatedRuntimeRoutingMatrix.DenylistedCapabilities.ToArray());
    }

    [TestMethod]
    public void AllowedCapabilitiesRouteToExpectedFakeExecutors()
    {
        var router = new SimulatedCapabilityRouter();

        foreach (var expected in ExpectedAllowed)
        {
            var result = router.Route(expected.Key);
            Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, result.Decision);
            Assert.AreEqual(expected.Value, result.SelectedExecutor);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void DeniedCapabilitiesNeverSelectExecutor()
    {
        var router = new SimulatedCapabilityRouter();

        foreach (var capability in ExpectedDenied)
        {
            var result = router.Route(capability);
            Assert.AreEqual(SimulatedDecision.Deny, result.Decision, capability);
            Assert.IsNull(result.SelectedExecutor, capability);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void UnknownCapabilityIsDenied()
    {
        var result = new SimulatedCapabilityRouter().Route("unknown_future_capability");

        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        Assert.IsNull(result.SelectedExecutor);
        StringAssert.Contains(result.DenyReason, "unsupported capability denied");
        AssertCleanRoute(result);
    }

    [TestMethod]
    public void DenylistIsEvaluatedBeforeAllowedRouting()
    {
        var allowed = new Dictionary<string, string>(SimulatedRuntimeRoutingMatrix.AllowedRoutingTable, StringComparer.Ordinal)
        {
            ["provider_cloud_live_call"] = "FakeLocalModelExecutor"
        };
        var denied = new HashSet<string>(SimulatedRuntimeRoutingMatrix.DenylistedCapabilities, StringComparer.Ordinal);
        var result = new SimulatedCapabilityRouter(allowed, denied).Route("provider_cloud_live_call");

        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        Assert.IsNull(result.SelectedExecutor);
        AssertCleanRoute(result);
    }

    [TestMethod]
    public void AllMatrixResultsCreateEvidenceLedgerRedactionAndNoExecutionProof()
    {
        var router = new SimulatedCapabilityRouter();
        foreach (var capability in ExpectedAllowed.Keys.Concat(ExpectedDenied).Append("unknown_future_capability"))
        {
            var result = router.Route(capability);
            Assert.IsNotNull(result.EvidenceEnvelope, capability);
            Assert.IsNotNull(result.LedgerEvents, capability);
            Assert.IsTrue(result.LedgerEvents.Count >= 2, capability);
            Assert.IsNotNull(result.RedactionProof, capability);
            Assert.IsNotNull(result.NoExecutionProof, capability);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void MatrixArtifactsExistAndDescribeCanonicalRouting()
    {
        foreach (var path in new[] { CanonicalMatrixPath, MatrixResultPath, UnknownBehaviorPath })
            Assert.IsTrue(File.Exists(FullPath(path)), path);

        var matrix = ReadAll(CanonicalMatrixPath);
        foreach (var expected in ExpectedAllowed)
        {
            StringAssert.Contains(matrix, expected.Key);
            StringAssert.Contains(matrix, expected.Value);
        }

        foreach (var denied in ExpectedDenied)
            StringAssert.Contains(matrix, denied);
    }

    [TestMethod]
    public void AuditGateDocumentsInheritedFullSuiteCaveat()
    {
        Assert.IsTrue(File.Exists(FullPath(CaveatGatePath)));
        Assert.IsTrue(File.Exists(FullPath(ValidationPlanPath)));
        var caveat = ReadAll(CaveatGatePath);

        StringAssert.Contains(caveat, "BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture");
        StringAssert.Contains(caveat, "WebSocket aborted");
        StringAssert.Contains(caveat, "BrowserRuntimeSmokeTests isolated passed 17/17");
        StringAssert.Contains(caveat, "fullSuiteClean");
    }

    [TestMethod]
    public void FinalDecisionIsCleanOrConditionalFlakyOnly()
    {
        Assert.IsTrue(File.Exists(FullPath(FinalPath)));
        var content = ReadAll(FinalPath);

        Assert.IsTrue(
            content.Contains("SIMULATED_RUNTIME_ROUTING_MATRIX_AUDIT_GATE_READY", StringComparison.Ordinal) ||
            content.Contains("SIMULATED_RUNTIME_ROUTING_MATRIX_AUDIT_GATE_CONDITIONAL_READY_FULL_SUITE_FLAKY", StringComparison.Ordinal));
        StringAssert.Contains(content, "\"productiveEnabled\": \"PROHIBITED\"");
        StringAssert.Contains(content, "\"routingMatrix\": \"READY\"");
        StringAssert.Contains(content, "\"denylistEnforcement\": \"READY\"");
    }

    [TestMethod]
    public void RuntimeReleaseStoreProductBridgeAndCspRemainBlocked()
    {
        var final = ReadAll(FinalPath);
        var productBridge = ReadAll(ProductBridgeCspPath);

        StringAssert.Contains(final, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(final, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(final, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(final, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(final, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(productBridge, "\"productFilesModified\": false");
        StringAssert.Contains(productBridge, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMilestoneRecommendsPolicyDecisionNormalizationGuard()
    {
        var content = ReadAll(NextMilestonePath);

        StringAssert.Contains(content, "M803-M805");
        StringAssert.Contains(content, "Simulated Runtime Policy Decision Normalization + Unsupported Capability Guard");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
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
