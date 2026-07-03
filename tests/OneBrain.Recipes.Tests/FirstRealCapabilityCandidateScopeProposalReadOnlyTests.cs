using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class FirstRealCapabilityCandidateScopeProposalReadOnlyTests
{
    [TestMethod]
    public void ScopeProposal_FixtureIsDeterministicAndReadOnly()
    {
        var first = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();
        var second = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();

        Assert.AreEqual(first.PacketId, second.PacketId);
        Assert.AreEqual(first.GeneratedAtUtc, second.GeneratedAtUtc);
        Assert.AreEqual(first.SelectedCandidateId, second.SelectedCandidateId);
        Assert.AreEqual(first.CandidateMatrix.Count, second.CandidateMatrix.Count);
        Assert.IsTrue(first.PassesReadOnlySafetyProof);
        Assert.IsTrue(second.PassesReadOnlySafetyProof);
    }

    [TestMethod]
    public void ScopeProposal_SelectedCandidateIsDurableAuditTrailOnlyAndStillBlocked()
    {
        var packet = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();

        Assert.AreEqual("DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL_SCOPE_PROPOSAL_READ_ONLY", packet.SelectedCandidateId);
        StringAssert.Contains(packet.SelectedCandidateScope.FutureHitoName, "DURABLE_AUDIT_TRAIL");
        StringAssert.Contains(packet.SelectedCandidateScope.PreImplementationExternalAudit, "EXTERNAL_AUDIT");
        Assert.AreEqual("BLOCKED_NOT_EXECUTABLE", packet.FutureImplementationPromptStatus);
        Assert.IsFalse(packet.NoGoStatus.SafeToImplementNow);
        Assert.IsFalse(packet.NoGoStatus.RuntimeReady);
        Assert.AreEqual("SAFE_TO_PREPARE_EXTERNAL_AUDIT_FOR_SELECTED_CANDIDATE", packet.NoGoStatus.MaxDecisionAllowed);
    }

    [TestMethod]
    public void ScopeProposal_ScopeExcludesRuntimeExecutionMutationExportAndRegistration()
    {
        var scope = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture().SelectedCandidateScope;

        CollectionAssert.Contains(scope.OutOfScope.ToList(), "runtime/live");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "approval execution");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "approval mutation");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "physical export");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "redaction runtime");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "retention/deletion runtime");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "DB/migration");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "service registration");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "command handler");
        CollectionAssert.Contains(scope.OutOfScope.ToList(), "release/commercial readiness");
    }

    [TestMethod]
    public void ScopeProposal_NegativeTestsCoverSelectedDurableAuditTrailBoundaries()
    {
        var packet = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();
        var testIds = packet.RequiredNegativeTests.Select(test => test.TestId).ToList();

        CollectionAssert.Contains(testIds, "no-append-without-gate");
        CollectionAssert.Contains(testIds, "no-arbitrary-event-writes");
        CollectionAssert.Contains(testIds, "no-mutation-store");
        CollectionAssert.Contains(testIds, "no-lifecycle-delete-retain");
        CollectionAssert.Contains(testIds, "deterministic-fixture-only");
        Assert.IsTrue(packet.RequiredNegativeTests.All(test => test.RequiredBeforeImplementation && !test.ImplementedNow));
    }

    [TestMethod]
    public void ScopeProposal_WordingDoesNotClaimImplementationReadiness()
    {
        var packet = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.Decision,
            packet.FutureImplementationPromptStatus,
            packet.HumanOperatorRecommendation,
            packet.SelectedCandidateScope.WordingPolicy,
            string.Join("\n", packet.CandidateMatrix.Select(candidate => $"{candidate.Name} {candidate.WhyNotNow} {candidate.Decision}")),
            string.Join("\n", packet.BlockedCapabilities));

        var forbidden = new[]
        {
            "READY_FOR_RUNTIME",
            "APPROVED_FOR_IMPLEMENTATION",
            "RELEASE_READY",
            "COMMERCIAL_READY",
            "safe to implement now",
            "runtime ready",
            "release ready",
            "commercial ready"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }
}
