using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaExternalReadOnlyEvidencePackM79Tests
{
    [TestMethod]
    public void ExternalReadOnlyEvidencePackPreparedDoesNotCloseM51M65()
    {
        var pack = Pack(AllowedDecision(), runtimeExecuted: false, runtimePassed: false);

        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PreparedButNotExecuted, pack.Status);
        Assert.IsFalse(pack.CandidateForM51M65Closure);
    }

    [TestMethod]
    public void ExternalReadOnlyEvidencePackNoOptInIsSkipped()
    {
        var decision = new NexaExternalProofHarness().Evaluate(NexaExternalProofHarnessM78Tests.Request(false, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget()), DateTimeOffset.UtcNow);
        var pack = Pack(decision, runtimeExecuted: false, runtimePassed: false);

        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.SkippedNoOptIn, pack.Status);
    }

    [TestMethod]
    public void ExternalReadOnlyEvidencePackNoTargetIsBlocked()
    {
        var decision = new NexaExternalProofHarness().Evaluate(NexaExternalProofHarnessM78Tests.Request(true, null), DateTimeOffset.UtcNow);
        var pack = Pack(decision, runtimeExecuted: false, runtimePassed: false);

        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.BlockedNoTarget, pack.Status);
    }

    [TestMethod]
    public void ExternalReadOnlyEvidencePackPolicyViolationIsBlocked()
    {
        var decision = new NexaExternalProofHarness().Evaluate(NexaExternalProofHarnessM78Tests.Request(true, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget(), method: "DELETE"), DateTimeOffset.UtcNow);
        var pack = Pack(decision, runtimeExecuted: false, runtimePassed: false);

        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.BlockedPolicyViolation, pack.Status);
    }

    [TestMethod]
    public void ExternalReadOnlyEvidencePackReadOnlyApprovedTargetProducesCandidateProof()
    {
        var pack = Pack(AllowedDecision(), runtimeExecuted: true, runtimePassed: true);

        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof, pack.Status);
        Assert.IsTrue(pack.CandidateForM51M65Closure);
        Assert.IsTrue(pack.FinalGoNoGo.Contains("M51/M65 still require explicit closure decision", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExternalReadOnlyEvidencePackRedactsSensitiveFields()
    {
        var request = NexaExternalProofHarnessM78Tests.Request(true, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget(), path: "/status?token=opaque-token-value-123456789");
        var decision = new NexaExternalProofHarness().Evaluate(request, DateTimeOffset.UtcNow);
        var pack = new NexaExternalReadOnlyEvidencePackBuilder().Build(decision, request, runtimeExecuted: true, runtimePassed: true);
        var serialized = System.Text.Json.JsonSerializer.Serialize(pack);

        Assert.IsTrue(pack.Redacted);
        Assert.IsFalse(serialized.Contains("opaque-token-value-123456789", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PrivatePreviewReadinessReflectsExternalLiveBlockedWithoutExecutedProof()
    {
        var dashboard = Dashboard(null);

        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
        Assert.IsTrue(dashboard.M65Blocked);
    }

    [TestMethod]
    public void PrivatePreviewReadinessReflectsCandidateProofPassedWithoutClosingExternalLive()
    {
        var dashboard = Dashboard(Pack(AllowedDecision(), runtimeExecuted: true, runtimePassed: true));

        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
        Assert.IsFalse(dashboard.M65Blocked);
        Assert.IsTrue(dashboard.Decision.GoNoGoExternalLive.Contains("closure review", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateModelsExternalProofPreparation()
    {
        var state = BrowserVaultMinimalM23Tests.SafeState() with
        {
            ExternalTestOwnedTargetContractDefined = true,
            ExternalTestOwnedTargetPolicySafe = true,
            ExternalProofHarnessDefined = true,
            ExternalProofHarnessOptInOnly = true,
            ExternalProofHarnessRunsWithoutApprovedTarget = false,
            ExternalProofHarnessCapturesSensitiveMaterial = false,
            ExternalReadOnlyEvidencePackDefined = true,
            ExternalReadOnlyEvidencePackClosesM51M65WithoutReview = false,
            ExternalReadOnlyEvidencePackLeaksSecrets = false
        };

        Assert.IsTrue(state.ExternalProofPreparationAllowed);
    }

    private static NexaExternalProofHarnessDecision AllowedDecision() =>
        new NexaExternalProofHarness().Evaluate(NexaExternalProofHarnessM78Tests.Request(true, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget()), DateTimeOffset.UtcNow);

    private static NexaExternalReadOnlyEvidencePack Pack(NexaExternalProofHarnessDecision decision, bool runtimeExecuted, bool runtimePassed) =>
        new NexaExternalReadOnlyEvidencePackBuilder().Build(decision, NexaExternalProofHarnessM78Tests.Request(true, NexaExternalTestOwnedTargetM77Tests.ApprovedTarget()), runtimeExecuted, runtimePassed);

    private static NexaPrivatePreviewReadinessDashboard Dashboard(NexaExternalReadOnlyEvidencePack? pack) =>
        new NexaPrivatePreviewReadinessDashboardService().Build(
            new NexaSkippedTestsAuditReporter().CreateReport(),
            new NexaPrivatePreviewGoNoGoService().Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria(), []),
            new NexaCanonicalWorkspaceGuardResult(
                NexaCanonicalWorkspaceGuardDecisionKind.Allowed,
                "canonical",
                "canonical",
                "head",
                "head",
                "origin/chrome-lab-001-extension-local-ai-bridge",
                IsDirty: false,
                IsLegacyPath: false,
                MatchesRemoteHead: true,
                DetachedHeadAccepted: true,
                BlockingReasons: [],
                OperatorMessage: "allowed",
                ModifiedWorkspace: false),
            pack);
}
