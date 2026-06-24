using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PolicyDecisionNormalization")]
[TestCategory("M803")]
[TestCategory("M804")]
[TestCategory("M805")]
public sealed class NodalOsPolicyDecisionM803M805Tests
{
    private const string DecisionContractPath = "artifacts/agent-operations/m803/policy-decision-normalization.json";
    private const string ReasonCodesPath = "artifacts/agent-operations/m803/policy-reason-codes.json";
    private const string UnsupportedGuardPath = "artifacts/agent-operations/m804/unsupported-capability-guard.json";
    private const string UnsupportedCasesPath = "artifacts/agent-operations/m804/unsupported-capability-cases.json";
    private const string FinalPath = "artifacts/agent-operations/m803-m805/policy-decision-unsupported-guard-go-no-go.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m805/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m805/next-milestone-recommendation.json";

    private static readonly string[] UnsupportedCases =
    [
        "unknown_future_capability",
        "typo_local_provider_modell",
        "",
        "MiXeD_Case_Unsupported_Capability",
        "provider_cloud_future_unknown",
        "public_release_candidate_not_registered"
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
    public void AllowedCapabilityReturnsAllowSimulatedDryRun()
    {
        var result = new SimulatedCapabilityRouter().Route("local_provider_model");

        Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, result.Decision);
        Assert.AreEqual(SimulatedPolicyDecisionType.AllowSimulatedDryRun, result.PolicyDecisionType);
        Assert.AreEqual(SimulatedPolicyReasonCodes.AllowedSimulatedFakeExecutor, result.ReasonCode);
        Assert.AreEqual("FakeLocalModelExecutor", result.SelectedExecutor);
        AssertCleanRoute(result);
    }

    [TestMethod]
    public void DenylistedCapabilityReturnsDenyDenylistedCapability()
    {
        var result = new SimulatedCapabilityRouter().Route("provider_cloud_live_call");

        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        Assert.AreEqual(SimulatedPolicyDecisionType.DenyDenylistedCapability, result.PolicyDecisionType);
        Assert.AreEqual(SimulatedPolicyReasonCodes.DeniedDenylistedCapability, result.ReasonCode);
        Assert.IsNull(result.SelectedExecutor);
        AssertCleanRoute(result);
    }

    [TestMethod]
    public void UnknownCapabilityReturnsDenyUnsupportedCapability()
    {
        var result = new SimulatedCapabilityRouter().Route("unknown_future_capability");

        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        Assert.AreEqual(SimulatedPolicyDecisionType.DenyUnsupportedCapability, result.PolicyDecisionType);
        Assert.AreEqual(SimulatedPolicyReasonCodes.DeniedUnsupportedCapability, result.ReasonCode);
        Assert.IsNull(result.SelectedExecutor);
        AssertCleanRoute(result);
    }

    [TestMethod]
    public void PolicyViolationReturnsDenyPolicyViolation()
    {
        var result = new SimulatedCapabilityRouter().Route(SimulatedRuntimeRoutingMatrix.PolicyViolationCapability);

        Assert.AreEqual(SimulatedDecision.Deny, result.Decision);
        Assert.AreEqual(SimulatedPolicyDecisionType.DenyPolicyViolation, result.PolicyDecisionType);
        Assert.AreEqual(SimulatedPolicyReasonCodes.DeniedPolicyViolation, result.ReasonCode);
        Assert.IsNull(result.SelectedExecutor);
        AssertCleanRoute(result);
    }

    [TestMethod]
    public void HighRiskSimulatedCaseReturnsRequireManualApprovalSimulated()
    {
        var result = new SimulatedCapabilityRouter().Route(SimulatedRuntimeRoutingMatrix.ManualApprovalCapability);

        Assert.AreEqual(SimulatedDecision.RequireManualApproval, result.Decision);
        Assert.AreEqual(SimulatedPolicyDecisionType.RequireManualApprovalSimulated, result.PolicyDecisionType);
        Assert.AreEqual(SimulatedPolicyReasonCodes.RequiresManualApprovalSimulated, result.ReasonCode);
        Assert.IsNull(result.SelectedExecutor);
        CollectionAssert.Contains(result.LedgerEvents.Select(x => x.EventType).ToArray(), "SIMULATED_MANUAL_APPROVAL_EVALUATED");
        AssertCleanRoute(result);
    }

    [TestMethod]
    public void DenyDecisionsNeverSelectExecutor()
    {
        foreach (var capability in new[]
        {
            "provider_cloud_live_call",
            "unknown_future_capability",
            SimulatedRuntimeRoutingMatrix.PolicyViolationCapability
        })
        {
            var result = new SimulatedCapabilityRouter().Route(capability);
            Assert.AreEqual(SimulatedDecision.Deny, result.Decision, capability);
            Assert.IsNull(result.SelectedExecutor, capability);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void RequireManualApprovalDoesNotExecute()
    {
        var result = new SimulatedCapabilityRouter().Route(SimulatedRuntimeRoutingMatrix.ManualApprovalCapability);

        Assert.AreEqual(0, result.SideEffectSinkInvocations);
        Assert.IsFalse(result.NoExecutionProof.ActualExecutionPerformed);
        Assert.IsFalse(result.NoExecutionProof.LiveCallPerformed);
        Assert.IsFalse(result.NoExecutionProof.FilesystemWritePerformed);
        Assert.IsFalse(result.NoExecutionProof.BrowserAutomationPerformed);
        Assert.IsFalse(result.NoExecutionProof.CapabilityUnlocked);
        Assert.IsFalse(result.NoExecutionProof.PublicReleasePerformed);
        Assert.IsFalse(result.NoExecutionProof.StoreSubmissionPerformed);
    }

    [TestMethod]
    public void EveryDecisionHasReasonEvidenceLedgerRedactionAndNoExecutionProof()
    {
        foreach (var capability in new[]
        {
            "local_provider_model",
            "provider_cloud_live_call",
            "unknown_future_capability",
            SimulatedRuntimeRoutingMatrix.PolicyViolationCapability,
            SimulatedRuntimeRoutingMatrix.ManualApprovalCapability
        })
        {
            var result = new SimulatedCapabilityRouter().Route(capability);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ReasonCode), capability);
            Assert.IsNotNull(result.EvidenceEnvelope, capability);
            Assert.IsNotNull(result.LedgerEvents, capability);
            Assert.IsTrue(result.LedgerEvents.Count >= 2, capability);
            Assert.IsNotNull(result.RedactionProof, capability);
            Assert.IsNotNull(result.NoExecutionProof, capability);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void UnsupportedCapabilityGuardCoversMalformedAndFutureCases()
    {
        var router = new SimulatedCapabilityRouter();

        foreach (var capability in UnsupportedCases)
        {
            var result = router.Route(capability);
            Assert.AreEqual(SimulatedDecision.Deny, result.Decision, capability);
            Assert.AreEqual(SimulatedPolicyDecisionType.DenyUnsupportedCapability, result.PolicyDecisionType, capability);
            Assert.AreEqual(SimulatedPolicyReasonCodes.DeniedUnsupportedCapability, result.ReasonCode, capability);
            Assert.IsNull(result.SelectedExecutor, capability);
            Assert.IsTrue(result.AuditEventCreated, capability);
            AssertCleanRoute(result);
        }
    }

    [TestMethod]
    public void PolicyDecisionArtifactsExist()
    {
        foreach (var path in new[] { DecisionContractPath, ReasonCodesPath, UnsupportedGuardPath, UnsupportedCasesPath })
            Assert.IsTrue(File.Exists(FullPath(path)), path);

        var decision = ReadAll(DecisionContractPath);
        foreach (var expected in new[]
        {
            "ALLOW_SIMULATED_DRY_RUN",
            "DENY_DENYLISTED_CAPABILITY",
            "DENY_UNSUPPORTED_CAPABILITY",
            "DENY_POLICY_VIOLATION",
            "REQUIRE_MANUAL_APPROVAL_SIMULATED"
        })
        {
            StringAssert.Contains(decision, expected);
        }
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
        StringAssert.Contains(final, "\"productiveEnabled\": \"PROHIBITED\"");
        StringAssert.Contains(productBridge, "\"productFilesModified\": false");
        StringAssert.Contains(productBridge, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMilestoneRecommendsManualApprovalBoundary()
    {
        var content = ReadAll(NextMilestonePath);

        StringAssert.Contains(content, "M806-M808");
        StringAssert.Contains(content, "Simulated Runtime Manual Approval Decision Boundary + Audit Projection");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
    }

    [TestMethod]
    public void FinalDecisionIsReadyWithOptionalFullSuiteFlakyCaveat()
    {
        var content = ReadAll(FinalPath);

        Assert.IsTrue(
            content.Contains("SIMULATED_POLICY_DECISION_NORMALIZATION_UNSUPPORTED_GUARD_READY_FULL_SUITE_FLAKY", StringComparison.Ordinal) ||
            content.Contains("SIMULATED_POLICY_DECISION_NORMALIZATION_UNSUPPORTED_GUARD_READY", StringComparison.Ordinal));
        StringAssert.Contains(content, "\"policyDecisionNormalization\": \"READY\"");
        StringAssert.Contains(content, "\"unsupportedCapabilityGuard\": \"READY\"");
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
