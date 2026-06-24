using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("FakeExecutors")]
[TestCategory("M794")]
[TestCategory("M795")]
[TestCategory("M796")]
public sealed class NodalOsFakeExecutorsM794M796Tests
{
    private const string PlanPath = "artifacts/agent-operations/m794/capability-specific-fake-executors-plan.json";
    private const string MatrixPath = "artifacts/agent-operations/m794/fake-executor-capability-matrix.json";
    private const string LocalModelBoundaryPath = "artifacts/agent-operations/m794/fake-local-model-executor-boundary.json";
    private const string FilesystemReadBoundaryPath = "artifacts/agent-operations/m794/fake-filesystem-read-executor-boundary.json";
    private const string LedgerAppendBoundaryPath = "artifacts/agent-operations/m794/fake-ledger-append-executor-boundary.json";
    private const string DisallowedPath = "artifacts/agent-operations/m794/fake-executor-disallowed-actions.json";
    private const string LocalModelProofPath = "artifacts/agent-operations/m795/fake-local-model-collector-proof.json";
    private const string FilesystemReadProofPath = "artifacts/agent-operations/m795/fake-filesystem-read-collector-proof.json";
    private const string LedgerAppendProofPath = "artifacts/agent-operations/m795/fake-ledger-append-collector-proof.json";
    private const string NoSideEffectProofPath = "artifacts/agent-operations/m795/fake-executor-no-side-effect-proof.json";
    private const string RedactionProofPath = "artifacts/agent-operations/m795/fake-executor-redaction-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m796/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m796/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m794-m796/fake-executor-collector-go-no-go.json";

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
    public void FakeExecutorPlanAndBoundariesExist()
    {
        foreach (var path in new[]
        {
            PlanPath,
            MatrixPath,
            LocalModelBoundaryPath,
            FilesystemReadBoundaryPath,
            LedgerAppendBoundaryPath,
            DisallowedPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }
    }

    [TestMethod]
    public void DisallowedFakeExecutorsRemainBlocked()
    {
        var content = ReadAll(MatrixPath) + ReadAll(DisallowedPath);

        foreach (var blocked in new[]
        {
            "FakeProviderCloudExecutor",
            "FakeFilesystemWriteExecutor",
            "FakeBrowserAutomationExecutor",
            "FakeCapabilityUnlockExecutor",
            "FakePublicReleaseExecutor",
            "FakeChromeWebStoreExecutor"
        })
        {
            StringAssert.Contains(content, blocked);
            StringAssert.Contains(content, "\"status\": \"BLOCKED\"");
        }
    }

    [TestMethod]
    public void AllowedFakeExecutorsAreTestOnlyInMemoryFake()
    {
        var results = ExecuteAllowedFakeExecutors();

        foreach (var result in results)
        {
            Assert.AreEqual(TestOnlyInMemoryFakeExecutor.TestOnlyExecutorType, result.ExecutorType);
            Assert.AreEqual(SimulatedDryRunOrchestrator.RuntimeType, result.RuntimeType);
            Assert.IsFalse(result.RealExecutionAllowed);
            Assert.IsFalse(result.LiveCallAllowed);
            Assert.IsFalse(result.CredentialUseAllowed);
            Assert.IsFalse(result.FilesystemWriteAllowed);
            Assert.IsFalse(result.BrowserActionAllowed);
            Assert.IsFalse(result.CapabilityUnlockAllowed);
            Assert.IsFalse(result.PublicReleaseAllowed);
            Assert.IsFalse(result.StoreSubmissionAllowed);
            Assert.IsFalse(result.ProductFilesModificationAllowed);
            Assert.IsFalse(result.BridgeCspModificationAllowed);
        }
    }

    [TestMethod]
    public void EachAllowedFakeExecutorEmitsCollectorObjects()
    {
        foreach (var result in ExecuteAllowedFakeExecutors())
        {
            Assert.IsNotNull(result.EvidenceEnvelope);
            Assert.IsNotNull(result.LedgerEvents);
            Assert.IsTrue(result.LedgerEvents.Count >= 2);
            Assert.IsNotNull(result.RedactionProof);
            Assert.IsNotNull(result.NoExecutionProof);
            CollectionAssert.Contains(result.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_ACTION_ALLOWED_FOR_DRY_RUN");
        }
    }

    [TestMethod]
    public void FakeLocalModelExecutorSimulatesWithoutProviderOrCloud()
    {
        var result = new FakeLocalModelExecutor().Execute();

        Assert.AreEqual("FakeLocalModelExecutor", result.ExecutorName);
        Assert.IsFalse(result.ProviderClientInvoked);
        Assert.IsFalse(result.NoExecutionProof.LiveCallPerformed);
        AssertCleanFakeExecutor(result);
    }

    [TestMethod]
    public void FakeFilesystemReadMetadataExecutorDoesNotReadOrWriteRealFilesystem()
    {
        var result = new FakeFilesystemReadMetadataExecutor().Execute();

        Assert.AreEqual("FakeFilesystemReadMetadataExecutor", result.ExecutorName);
        Assert.IsFalse(result.FilesystemReaderRealInvoked);
        Assert.IsFalse(result.FilesystemWriterInvoked);
        Assert.IsFalse(result.NoExecutionProof.FilesystemWritePerformed);
        AssertCleanFakeExecutor(result);
    }

    [TestMethod]
    public void FakeLedgerAppendExecutorUsesInMemoryLedgerOnly()
    {
        var result = new FakeLedgerAppendExecutor().Execute();

        Assert.AreEqual("FakeLedgerAppendExecutor", result.ExecutorName);
        Assert.IsTrue(result.InMemoryLedgerOnly);
        Assert.IsFalse(result.NoExecutionProof.FilesystemWritePerformed);
        AssertCleanFakeExecutor(result);
    }

    [TestMethod]
    public void CollectorProofArtifactsRecordNoSideEffectsAndRedaction()
    {
        foreach (var path in new[]
        {
            LocalModelProofPath,
            FilesystemReadProofPath,
            LedgerAppendProofPath,
            NoSideEffectProofPath,
            RedactionProofPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        var noSideEffect = ReadAll(NoSideEffectProofPath);
        StringAssert.Contains(noSideEffect, "\"sideEffectSinkInvocations\": 0");
        StringAssert.Contains(noSideEffect, "\"providerClientInvoked\": false");
        StringAssert.Contains(noSideEffect, "\"filesystemWriterInvoked\": false");
        StringAssert.Contains(noSideEffect, "\"browserAutomationInvoked\": false");

        var redaction = ReadAll(RedactionProofPath);
        foreach (var field in new[]
        {
            "secretsIncluded",
            "credentialsIncluded",
            "tokensIncluded",
            "cookiesIncluded",
            "rawUserDataIncluded",
            "rawLogsIncluded",
            "providerKeysIncluded",
            "privateKeysIncluded",
            "browserSessionDataIncluded"
        })
        {
            StringAssert.Contains(redaction, $"\"{field}\": false");
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
    public void NextMilestoneRecommendsRoutingAndDenylistEnforcement()
    {
        var content = ReadAll(NextMilestonePath);

        StringAssert.Contains(content, "M797-M799");
        StringAssert.Contains(content, "Simulated Capability Executor Routing + Denylist Enforcement");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
    }

    [TestMethod]
    public void FinalDecisionIsFakeExecutorCollectorReady()
    {
        var content = ReadAll(ConsolidatedPath);

        StringAssert.Contains(content, "CAPABILITY_SPECIFIC_FAKE_EXECUTORS_COLLECTOR_ENFORCEMENT_READY");
        StringAssert.Contains(content, "\"fakeExecutors\": \"READY_TEST_ONLY\"");
        StringAssert.Contains(content, "\"collectorEnforcement\": \"READY\"");
        StringAssert.Contains(content, "\"inMemoryEvidenceLedger\": \"READY\"");
    }

    private static IReadOnlyList<FakeExecutorExecutionResult> ExecuteAllowedFakeExecutors() =>
    [
        new FakeLocalModelExecutor().Execute(),
        new FakeFilesystemReadMetadataExecutor().Execute(),
        new FakeLedgerAppendExecutor().Execute()
    ];

    private static void AssertCleanFakeExecutor(FakeExecutorExecutionResult result)
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
