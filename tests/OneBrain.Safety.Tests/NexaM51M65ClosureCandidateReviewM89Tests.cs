using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaM51M65ClosureCandidateReviewM89Tests
{
    [TestMethod]
    public async Task M51M65ClosureCandidateReviewNoProofDoesNotClose()
    {
        var review = await ReviewAsync(optIn: false, executeNetwork: false);

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.LiveProofSkippedNoOptIn, review.FinalDecision);
        StringAssert.Contains(review.M51Recommendation, "do not close M51");
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewDryRunOnlyDoesNotClose()
    {
        var review = await ReviewAsync(optIn: true, executeNetwork: false);

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.DoNotClose, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewFailedProofDoesNotClose()
    {
        var proof = await new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(500, 200, "NEXA")).RunAsync(optIn: true, executeNetwork: true);
        var review = new NexaM51M65ClosureCandidateReviewer().Review(proof);

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.LiveProofFailed, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewSecretsDetectedDoesNotClose()
    {
        var proof = await PassedProof();
        var unsafeProof = proof with
        {
            EvidencePack = proof.EvidencePack with
            {
                PolicyDecisions = ["opaque-token-value-123456789"]
            }
        };
        var review = new NexaM51M65ClosureCandidateReviewer().Review(unsafeProof);

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.DoNotClose, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewMutationDetectedDoesNotClose()
    {
        var proof = await PassedProof();
        var unsafeProof = proof with
        {
            EvidencePack = proof.EvidencePack with
            {
                Status = NexaExternalReadOnlyEvidencePackStatus.BlockedPolicyViolation,
                CandidateForM51M65Closure = false
            }
        };
        var review = new NexaM51M65ClosureCandidateReviewer().Review(unsafeProof);

        Assert.AreNotEqual(NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51AndM65, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewVerifiedPassedReadOnlyProofIsCandidate()
    {
        var review = new NexaM51M65ClosureCandidateReviewer().Review(await PassedProof());

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51AndM65, review.FinalDecision);
        StringAssert.Contains(review.M51Recommendation, "candidate close M51");
        StringAssert.Contains(review.M65Recommendation, "candidate close M65");
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewCandidateDoesNotOpenRealSurfaces()
    {
        var review = new NexaM51M65ClosureCandidateReviewer().Review(await PassedProof());

        Assert.IsTrue(review.PublicSaasStillDisabled);
        Assert.IsTrue(review.RealBillingStillDisabled);
        Assert.IsTrue(review.RealEmailStillDisabled);
        Assert.IsTrue(review.RealCredentialsStillBlocked);
        Assert.IsTrue(review.SensitiveSurfacesStillBlocked);
    }

    [TestMethod]
    public async Task PrivatePreviewReadinessReflectsReviewCandidateWithoutExternalLiveAutoGo()
    {
        var proof = await PassedProof();
        var dashboard = new NexaPrivatePreviewReadinessDashboardService().Build(
            new NexaSkippedTestsAuditReporter().CreateReport(),
            new NexaPrivatePreviewGoNoGoService().Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria(), []),
            GuardAllowed(),
            proof.EvidencePack);

        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
        Assert.IsFalse(dashboard.M65Blocked);
        StringAssert.Contains(dashboard.Decision.GoNoGoExternalLive, "closure review");
    }

    private static async Task<NexaM51M65ClosureCandidateReview> ReviewAsync(bool optIn, bool executeNetwork)
    {
        var proof = await new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(200, 200, "NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit")).RunAsync(optIn, executeNetwork);
        return new NexaM51M65ClosureCandidateReviewer().Review(proof);
    }

    private static Task<NexaFirstReadOnlyLiveProofResult> PassedProof() =>
        new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(200, 200, "NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit")).RunAsync(optIn: true, executeNetwork: true);

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
