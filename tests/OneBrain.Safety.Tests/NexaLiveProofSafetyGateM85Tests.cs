using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaLiveProofSafetyGateM85Tests
{
    [TestMethod]
    public void LiveProofSafetyGateNotConfiguredBlocks()
    {
        var decision = Gate(null, Target(), optIn: true);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.LiveProofNotConfigured, decision.Status);
        Assert.IsFalse(decision.ReadyForReadOnlyLiveProof);
    }

    [TestMethod]
    public void LiveProofSafetyGateDnsPendingBlocks()
    {
        var decision = Gate(new NexaTargetBindingReadinessEvaluator().CreateDefault(NexaTargetBindingDnsMode.Unknown, NexaTargetBindingVerificationStatus.DnsPending), Target(), optIn: true);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.DnsPending, decision.Status);
    }

    [TestMethod]
    public void LiveProofSafetyGateHttpsPendingBlocks()
    {
        var decision = Gate(new NexaTargetBindingReadinessEvaluator().CreateDefault(NexaTargetBindingDnsMode.CnameOnly, NexaTargetBindingVerificationStatus.NotConfigured), Target(), optIn: true);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.HttpsPending, decision.Status);
    }

    [TestMethod]
    public void LiveProofSafetyGateOwnershipPendingBlocks()
    {
        var decision = Gate(new NexaTargetBindingReadinessEvaluator().CreateDefault(NexaTargetBindingDnsMode.CnameOnly, NexaTargetBindingVerificationStatus.HttpsReady), Target(), optIn: true);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.OwnershipPending, decision.Status);
    }

    [TestMethod]
    public void LiveProofSafetyGateOptInMissingBlocks()
    {
        var decision = Gate(NexaDomainBindingReadinessM84Tests.ReadyConfig(), Target(), optIn: false);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.HarnessOptInMissing, decision.Status);
    }

    [TestMethod]
    public void LiveProofSafetyGateOperatorApprovalMissingBlocksWhenRequired()
    {
        var decision = Gate(NexaDomainBindingReadinessM84Tests.ReadyConfig(), Target(), optIn: true, operatorApprovalRequired: true, operatorApprovalRef: null);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.OperatorApprovalMissing, decision.Status);
    }

    [TestMethod]
    public void LiveProofSafetyGatePolicyRejectedBlocks()
    {
        var target = Target() with { CredentialPolicy = NexaExternalTargetCredentialPolicy.RealCredentialsBlocked };
        var decision = Gate(NexaDomainBindingReadinessM84Tests.ReadyConfig(), target, optIn: true);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.TargetPolicyRejected, decision.Status);
    }

    [TestMethod]
    public void LiveProofSafetyGateAllConditionsMetIsReadyForReadOnlyProof()
    {
        var decision = Gate(NexaDomainBindingReadinessM84Tests.ReadyConfig(), Target(), optIn: true);

        Assert.AreEqual(NexaLiveProofSafetyGateStatus.ReadyForReadOnlyLiveProof, decision.Status);
        Assert.IsTrue(decision.ReadyForReadOnlyLiveProof);
        Assert.IsFalse(decision.ExecutesNetwork);
    }

    [TestMethod]
    public void LiveProofSafetyGateReadyDoesNotCloseM51M65()
    {
        var decision = Gate(NexaDomainBindingReadinessM84Tests.ReadyConfig(), Target(), optIn: true);

        Assert.IsFalse(decision.ClosesM51M65);
    }

    [TestMethod]
    public void LiveProofSafetyGatePassedEvidencePackFutureCouldBeClosureCandidate()
    {
        var request = new NexaExternalProofHarnessRequest(true, Target(), "nexa-lab.nodalos.com.ar", "/health", "GET", false, false, false, false, "operator");
        var harness = new NexaExternalProofHarness().Evaluate(request, DateTimeOffset.UtcNow);
        var pack = new NexaExternalReadOnlyEvidencePackBuilder().Build(harness, request, runtimeExecuted: true, runtimePassed: true);

        Assert.IsTrue(pack.CandidateForM51M65Closure);
        Assert.AreEqual(NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof, pack.Status);
    }

    [TestMethod]
    public void PrivatePreviewReadinessShowsLiveGateReadyIsNotExternalProofPassed()
    {
        var liveGate = Gate(NexaDomainBindingReadinessM84Tests.ReadyConfig(), Target(), optIn: true);
        var dashboard = new NexaPrivatePreviewReadinessDashboardService().Build(
            new NexaSkippedTestsAuditReporter().CreateReport(),
            new NexaPrivatePreviewGoNoGoService().Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria(), []),
            GuardAllowed(),
            externalEvidencePack: null,
            liveGate);

        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
        StringAssert.Contains(dashboard.Decision.GoNoGoExternalLive, "proof not executed");
        Assert.IsTrue(dashboard.M65Blocked);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateModelsVercelReadinessWithoutLiveExecution()
    {
        var state = BrowserVaultMinimalM23Tests.SafeState() with
        {
            VercelTestOwnedTargetAppDefined = true,
            DomainBindingReadinessDefined = true,
            LiveProofSafetyGateDefined = true,
            LiveProofSafetyGateReadyDoesNotCloseM51M65 = true,
            LiveProofSafetyGateRunsWithoutOptIn = false,
            LiveProofSafetyGateAllowsSensitiveSurface = false
        };

        Assert.IsTrue(state.VercelTestOwnedTargetReadinessAllowed);
    }

    private static NexaLiveProofSafetyGateDecision Gate(
        NexaTargetBindingConfig? binding,
        NexaExternalTestOwnedTarget target,
        bool optIn,
        bool operatorApprovalRequired = false,
        string? operatorApprovalRef = "approval:operator") =>
        new NexaLiveProofSafetyGate().Evaluate(
            new NexaLiveProofSafetyGateRequest(
                binding,
                target,
                optIn,
                "nexa-lab.nodalos.com.ar",
                "/health",
                "GET",
                WouldUseCredentials: false,
                WouldPersistPersonalCookies: false,
                WouldCaptureSensitiveHeaderValues: false,
                WouldCaptureBodies: false,
                WouldSubmit: false,
                WouldMutate: false,
                WouldUsePaymentOrCheckoutOrRealLogin: false,
                EvidencePackReady: true,
                operatorApprovalRequired,
                operatorApprovalRef),
            DateTimeOffset.UtcNow);

    private static NexaExternalTestOwnedTarget Target() =>
        NexaExternalTestOwnedTargetM77Tests.ApprovedTarget("https://nexa-lab.nodalos.com.ar/health") with
        {
            AllowedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "/",
                "/products",
                "/document",
                "/report",
                "/disabled-form",
                "/blocked-login",
                "/blocked-checkout",
                "/blocked-destructive-action",
                "/health",
                "/ownership"
            },
            DeniedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/api", "/login", "/checkout/submit", "/submit", "/delete", "/pay" },
            ApprovalRef = "approval:nexa-lab-readonly"
        };

    private static NexaCanonicalWorkspaceGuardResult GuardAllowed() =>
        new(
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
            ModifiedWorkspace: false);
}
