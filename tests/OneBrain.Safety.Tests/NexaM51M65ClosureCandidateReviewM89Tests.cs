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

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.DoNotClose, review.FinalDecision);
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

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.DoNotClose, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewSecretsDetectedDoesNotClose()
    {
        var proof = await PersistedRealHttpProof();
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

        Assert.AreNotEqual(NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51Only, review.FinalDecision);
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewVerifiedPassedReadOnlyProofIsCandidate()
    {
        var review = new NexaM51M65ClosureCandidateReviewer().Review(await PersistedRealHttpProof());

        Assert.AreEqual(NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51Only, review.FinalDecision);
        StringAssert.Contains(review.M51Recommendation, "candidate close M51");
        StringAssert.Contains(review.M65Recommendation, "M65 deferred");
    }

    [TestMethod]
    public async Task M51M65ClosureCandidateReviewCandidateDoesNotOpenRealSurfaces()
    {
        var review = new NexaM51M65ClosureCandidateReviewer().Review(await PersistedRealHttpProof());

        Assert.IsTrue(review.PublicSaasStillDisabled);
        Assert.IsTrue(review.RealBillingStillDisabled);
        Assert.IsTrue(review.RealEmailStillDisabled);
        Assert.IsTrue(review.RealCredentialsStillBlocked);
        Assert.IsTrue(review.SensitiveSurfacesStillBlocked);
    }

    [TestMethod]
    public async Task PrivatePreviewReadinessReflectsReviewCandidateWithoutExternalLiveAutoGo()
    {
        var proof = await PersistedRealHttpProof();
        var dashboard = new NexaPrivatePreviewReadinessDashboardService().Build(
            new NexaSkippedTestsAuditReporter().CreateReport(),
            new NexaPrivatePreviewGoNoGoService().Evaluate(NexaPrivatePreviewGoNoGoService.SafeCriteria(), []),
            GuardAllowed(),
            proof.EvidencePack);

        Assert.IsFalse(dashboard.Decision.ExternalLiveAllowed);
        Assert.IsTrue(dashboard.M65Blocked);
        StringAssert.Contains(dashboard.Decision.GoNoGoExternalLive, "M51 closure review");
        StringAssert.Contains(dashboard.Decision.GoNoGoExternalLive, "M65 remains deferred");
    }

    private static async Task<NexaM51M65ClosureCandidateReview> ReviewAsync(bool optIn, bool executeNetwork)
    {
        var proof = await new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(200, 200, "NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit")).RunAsync(optIn, executeNetwork);
        return new NexaM51M65ClosureCandidateReviewer().Review(proof);
    }

    private static Task<NexaFirstReadOnlyLiveProofResult> PassedProof() =>
        new NexaFirstReadOnlyLiveProofRunner(new NexaHttpsOwnershipVerificationM87Tests.FakeProbe(200, 200, "NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit")).RunAsync(optIn: true, executeNetwork: true);

    private static async Task<NexaFirstReadOnlyLiveProofResult> PersistedRealHttpProof()
    {
        using var temp = new TempDirectory();
        var ledger = TestLedger(temp.Path);
        var proof = await new NexaFirstReadOnlyLiveProofRunner(new NexaHttpClientReadOnlyProbe(new HttpClient(new StaticHttpHandler()))).RunAsync(optIn: true, executeNetwork: true, ledger);
        return proof;
    }

    private static BrowserPersistentAuditLedger TestLedger(string path) =>
        new(
            new BrowserAuditLedgerPolicy(path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)),
            BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("nodal-m90-explicit-test-fixture-hmac-key"));

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

    private sealed class StaticHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("NEXA test-owned read-only no-real-users no-real-credentials no-real-payments no-submit")
            });
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nodal-m90-{Guid.NewGuid():N}");

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
