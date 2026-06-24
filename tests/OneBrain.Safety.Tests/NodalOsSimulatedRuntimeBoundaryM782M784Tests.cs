using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulatedRuntimeBoundary")]
[TestCategory("M782")]
[TestCategory("M783")]
[TestCategory("M784")]
public sealed class NodalOsSimulatedRuntimeBoundaryM782M784Tests
{
    private const string PlanPath = "artifacts/agent-operations/m782/simulated-runtime-boundary-implementation-plan.json";
    private const string ScopePath = "artifacts/agent-operations/m782/simulated-runtime-boundary-scope.json";
    private const string NoExecutorPlanPath = "artifacts/agent-operations/m782/no-real-executor-wired-plan.json";
    private const string FakeOnlyBoundaryPath = "artifacts/agent-operations/m782/fake-only-runtime-boundary.json";
    private const string DisallowedPathsPath = "artifacts/agent-operations/m782/simulated-runtime-disallowed-paths.json";
    private const string M782GoNoGoPath = "artifacts/agent-operations/m782/m782-go-no-go.json";
    private const string OrchestratorBoundaryPath = "artifacts/agent-operations/m783/simulated-executable-orchestrator-boundary.json";
    private const string OrchestratorContractPath = "artifacts/agent-operations/m783/simulated-orchestrator-runtime-contract.json";
    private const string NoExecutorProofPath = "artifacts/agent-operations/m783/no-real-executor-wired-proof.json";
    private const string AllowPathProofPath = "artifacts/agent-operations/m783/simulated-allow-path-proof.json";
    private const string DenyPathProofPath = "artifacts/agent-operations/m783/simulated-deny-path-proof.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m784/simulated-runtime-boundary-next-decision.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m784/next-milestone-recommendation.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m784/product-bridge-csp-unchanged-proof.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m782-m784/simulated-runtime-boundary-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static JsonDocument ReadJson(string relativePath) => JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));
    private static bool Bool(JsonDocument d, string p) => d.RootElement.GetProperty(p).GetBoolean();
    private static string Str(JsonDocument d, string p) => d.RootElement.GetProperty(p).GetString() ?? "";

    private static SimulatedRequest ValidAllowRequest(string capability) =>
        new("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", capability, IsProhibitedAction: false);

    private static SimulatedRequest ProhibitedRequest(string capability) =>
        new("SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", capability, IsProhibitedAction: true);

    private static void AssertSinkUntouched(RecordingSideEffectSink sink)
    {
        Assert.IsFalse(sink.AnyInvoked, "no side effect must be invoked on any branch");
        Assert.IsFalse(sink.RealExecutorInvoked);
        Assert.IsFalse(sink.ProviderClientInvoked);
        Assert.IsFalse(sink.FilesystemWriterInvoked);
        Assert.IsFalse(sink.BrowserAutomationInvoked);
        Assert.IsFalse(sink.CapabilityUnlockInvoked);
        Assert.IsFalse(sink.PublicReleaseInvoked);
        Assert.IsFalse(sink.StoreSubmissionInvoked);
        Assert.IsFalse(sink.SignedZipCreated);
    }

    // 1
    [TestMethod]
    public void SimulatedRuntimeBoundaryPlanExists() => Assert.IsTrue(File.Exists(FullPath(PlanPath)), PlanPath);

    // 2
    [TestMethod]
    public void RuntimeTypeIsSimulatedFakeOnlyInMemory()
    {
        using var d = ReadJson(PlanPath);
        Assert.AreEqual("SIMULATED_FAKE_ONLY_IN_MEMORY", Str(d, "runtimeType"));
    }

    // 3-10
    [TestMethod]
    public void PlanWiringFlagsAllFalse()
    {
        using var d = ReadJson(PlanPath);
        Assert.IsFalse(Bool(d, "realExecutorWired"));        // 3
        Assert.IsFalse(Bool(d, "providerClientWired"));      // 4
        Assert.IsFalse(Bool(d, "filesystemWriterWired"));    // 5
        Assert.IsFalse(Bool(d, "browserAutomationWired"));   // 6
        Assert.IsFalse(Bool(d, "capabilityUnlockWired"));    // 7
        Assert.IsFalse(Bool(d, "publicReleaseWired"));       // 8
        Assert.IsFalse(Bool(d, "storeSubmissionWired"));     // 9
        Assert.IsFalse(Bool(d, "productiveExecutionAllowed")); // 10
    }

    // 11
    [TestMethod]
    public void DisallowedPathsIncludeAllForbiddenSurfaces()
    {
        using var d = ReadJson(DisallowedPathsPath);
        var paths = d.RootElement.GetProperty("disallowedPaths").EnumerateArray().Select(x => x.GetString()).ToArray();
        foreach (var required in new[]
        {
            "real executor", "provider/cloud client", "filesystem writer", "browser automation adapter",
            "capability unlock executor", "public release procedure", "Chrome Web Store submission procedure",
            "signed ZIP creation", "product files mutation", "Bridge/CSP mutation"
        })
            CollectionAssert.Contains(paths, required);
    }

    // 12
    [TestMethod]
    public void SimulatedExecutableOrchestratorBoundaryExists()
    {
        Assert.IsTrue(File.Exists(FullPath(OrchestratorBoundaryPath)), OrchestratorBoundaryPath);
        using var d = ReadJson(OrchestratorBoundaryPath);
        Assert.IsFalse(Bool(d, "isProductCode"));
        Assert.IsFalse(Bool(d, "wiredIntoBridge"));
    }

    // 13 (executable) — ALLOW_SIMULATED_DRY_RUN does not invoke real executor
    [TestMethod]
    public void AllowSimulatedDryRunDoesNotInvokeRealExecutor()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);
        var result = orchestrator.Process(ValidAllowRequest("local provider/model (simulated)"));
        Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, result.Decision);
        Assert.IsTrue(result.LedgerProjected);
        Assert.IsTrue(result.EvidenceEnvelopeCreated);
        Assert.IsTrue(result.Proof.SimulationOnly);
        AssertSinkUntouched(sink);
    }

    // 14 (executable) — DENY does not invoke real executor
    [TestMethod]
    public void DenyDoesNotInvokeRealExecutor()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);
        var result = orchestrator.Process(ProhibitedRequest("attempt_capability_unlock"));
        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        Assert.IsTrue(result.LedgerProjected);
        Assert.IsTrue(result.EvidenceEnvelopeCreated);
        AssertSinkUntouched(sink);
    }

    [TestMethod]
    public void NonSimulatedModeIsDeniedWithoutSideEffects()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);
        var result = orchestrator.Process(new SimulatedRequest("REAL_EXECUTION", "SIMULATED_FAKE_ONLY", "x", false));
        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        AssertSinkUntouched(sink);
    }

    [TestMethod]
    public void ManualApprovalRequiredPathInvokesNoSideEffects()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);
        var result = orchestrator.Process(new SimulatedRequest(
            "SIMULATED_DRY_RUN", "SIMULATED_FAKE_ONLY", "manual approval (simulated)",
            IsProhibitedAction: false, RequiresManualApproval: true, ManualApprovalGranted: false));
        Assert.AreEqual(SimulatedDecision.RequireManualApproval, result.Decision);
        AssertSinkUntouched(sink);
    }

    // 15-22 (executable, across every branch) + artifact proof
    [TestMethod]
    public void NoSideEffectInvokedAcrossEveryBranch()
    {
        var sink = new RecordingSideEffectSink();
        var orchestrator = new SimulatedDryRunOrchestrator(sink);
        orchestrator.Process(ValidAllowRequest("filesystem read metadata (simulated)"));
        orchestrator.Process(ProhibitedRequest("attempt_filesystem_write"));
        orchestrator.Process(ProhibitedRequest("attempt_live_provider_call"));
        orchestrator.Process(ProhibitedRequest("attempt_browser_action"));
        orchestrator.Process(ProhibitedRequest("attempt_public_release"));
        orchestrator.Process(ProhibitedRequest("attempt_store_submission"));
        orchestrator.Process(new SimulatedRequest("SIMULATED_DRY_RUN", "REAL_FIXTURE", "x", false));
        AssertSinkUntouched(sink);

        using var d = ReadJson(NoExecutorProofPath);
        Assert.AreEqual("PROVEN", Str(d, "noRealExecutorWiredProof"));
        Assert.IsFalse(Bool(d, "realExecutorInvoked"));         // 15
        Assert.IsFalse(Bool(d, "providerClientInvoked"));       // 16
        Assert.IsFalse(Bool(d, "filesystemWriterInvoked"));     // 17
        Assert.IsFalse(Bool(d, "browserAutomationInvoked"));    // 18
        Assert.IsFalse(Bool(d, "capabilityUnlockInvoked"));     // 19
        Assert.IsFalse(Bool(d, "publicReleaseInvoked"));        // 20
        Assert.IsFalse(Bool(d, "storeSubmissionInvoked"));      // 21
        Assert.IsFalse(Bool(d, "signedZipCreated"));            // 22
        Assert.IsFalse(Bool(d, "productFilesModified"));        // 23
        Assert.IsFalse(Bool(d, "bridgeCspModified"));           // 24
    }

    // 25-29 (consolidated invariants)
    [TestMethod]
    public void ConsolidatedInvariantsRemainBlocked()
    {
        using var d = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DISABLED", Str(d, "runtimeProductiveExecution"));        // 25
        Assert.AreEqual("DISABLED", Str(d, "providerCloudLiveCalls"));            // 26
        Assert.AreEqual("DISABLED", Str(d, "filesystemBrowserCapabilityUnlock")); // 27
        Assert.AreEqual("NO-GO", Str(d, "publicRelease"));                        // 28
        Assert.AreEqual("NO-GO", Str(d, "chromeWebStore"));                       // 29
        Assert.AreEqual("PROHIBITED", Str(d, "productiveEnabled"));
        Assert.AreEqual("PROVEN", Str(d, "noRealExecutorWired"));
    }

    // 30-31 (product/bridge/csp unchanged)
    [TestMethod]
    public void ProductBridgeAndCspNotModified()
    {
        using var d = ReadJson(ProductBridgeCspPath);
        Assert.IsFalse(Bool(d, "productFilesModified")); // 30
        Assert.IsFalse(Bool(d, "bridgeModified"));       // 31
        Assert.IsFalse(Bool(d, "cspModified"));
        Assert.IsFalse(Bool(d, "serviceWorkerModified"));
    }

    // 32 (next milestone)
    [TestMethod]
    public void NextMilestoneIsSimulatedInMemoryExecutionTests()
    {
        using var d = ReadJson(NextMilestonePath);
        StringAssert.Contains(Str(d, "recommendedNextMilestone"), "M785-M787");
        StringAssert.Contains(Str(d, "recommendedNextMilestone"), "Enforcement Proof");
        Assert.IsFalse(Bool(d, "productiveUnlockAllowed"));
    }

    // 33 (no productive unlock allowed in this block)
    [TestMethod]
    public void NoProductiveUnlockAllowedInThisBlock()
    {
        using var plan = ReadJson(PlanPath);
        using var next = ReadJson(NextDecisionPath);
        Assert.IsFalse(Bool(plan, "productiveExecutionAllowed"));
        Assert.AreEqual("PROHIBITED", Str(next, "productiveEnabled"));
        Assert.AreEqual("DISABLED", Str(next, "runtimeProductiveExecution"));
    }

    [TestMethod]
    public void AllExpectedArtifactsExist()
    {
        foreach (var path in new[]
        {
            PlanPath, ScopePath, NoExecutorPlanPath, FakeOnlyBoundaryPath, DisallowedPathsPath, M782GoNoGoPath,
            OrchestratorBoundaryPath, OrchestratorContractPath, NoExecutorProofPath, AllowPathProofPath, DenyPathProofPath,
            NextDecisionPath, NextMilestonePath, ProductBridgeCspPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsImplementationPlanReady()
    {
        using var d = ReadJson(ConsolidatedPath);
        Assert.AreEqual("SIMULATED_RUNTIME_BOUNDARY_IMPLEMENTATION_PLAN_READY", Str(d, "decision"));
        Assert.IsFalse(Bool(d, "evidenceInvented"));
    }
}
